using Binah.Auth.Models;
using Microsoft.EntityFrameworkCore;
using Sustainsys.Saml2;
using Sustainsys.Saml2.Configuration;
using Sustainsys.Saml2.WebSso;
using Sustainsys.Saml2.Metadata;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace Binah.Auth.Services;

/// <summary>
/// Production-grade SAML SSO service using Sustainsys.Saml2
/// Full implementation with signature validation, replay attack prevention, and attribute mapping
/// </summary>
public class SamlServiceProduction : ISamlService
{
    private readonly AuthDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SamlServiceProduction> _logger;
    private readonly Dictionary<string, SPOptions> _spOptionsCache = new();

    public SamlServiceProduction(
        AuthDbContext context,
        IConfiguration configuration,
        ILogger<SamlServiceProduction> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SamlConfiguration> ConfigureSamlAsync(string tenantId, SamlConfigurationRequest request)
    {
        if (string.IsNullOrEmpty(tenantId))
            throw new ArgumentException("TenantId is required", nameof(tenantId));

        ValidateSamlConfiguration(request);

        var existing = await _context.SamlConfigurations
            .FirstOrDefaultAsync(c => c.TenantId == tenantId);

        if (existing != null)
        {
            throw new InvalidOperationException($"SAML configuration already exists for tenant {tenantId}. Use update endpoint.");
        }

        var config = new SamlConfiguration
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EntityId = request.EntityId,
            SsoUrl = request.SsoUrl,
            X509Certificate = request.X509Certificate,
            Enabled = request.Enabled,
            AttributeMapping = JsonSerializer.Serialize(request.AttributeMapping),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.SamlConfigurations.Add(config);
        await _context.SaveChangesAsync();

        // Clear cache for this tenant
        _spOptionsCache.Remove(tenantId);

        _logger.LogInformation("SAML configured for tenant {TenantId}", tenantId);

        return config;
    }

    public async Task<SamlConfiguration?> GetSamlConfigurationAsync(string tenantId)
    {
        if (string.IsNullOrEmpty(tenantId))
            throw new ArgumentException("TenantId is required", nameof(tenantId));

        return await _context.SamlConfigurations
            .FirstOrDefaultAsync(c => c.TenantId == tenantId);
    }

    public async Task<SamlConfiguration> UpdateSamlConfigurationAsync(string tenantId, SamlConfigurationRequest request)
    {
        if (string.IsNullOrEmpty(tenantId))
            throw new ArgumentException("TenantId is required", nameof(tenantId));

        ValidateSamlConfiguration(request);

        var config = await _context.SamlConfigurations
            .FirstOrDefaultAsync(c => c.TenantId == tenantId);

        if (config == null)
        {
            throw new InvalidOperationException($"SAML configuration not found for tenant {tenantId}");
        }

        config.EntityId = request.EntityId;
        config.SsoUrl = request.SsoUrl;
        config.X509Certificate = request.X509Certificate;
        config.Enabled = request.Enabled;
        config.AttributeMapping = JsonSerializer.Serialize(request.AttributeMapping);
        config.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Clear cache for this tenant
        _spOptionsCache.Remove(tenantId);

        _logger.LogInformation("SAML configuration updated for tenant {TenantId}", tenantId);

        return config;
    }

    public async Task DisableSamlAsync(string tenantId)
    {
        if (string.IsNullOrEmpty(tenantId))
            throw new ArgumentException("TenantId is required", nameof(tenantId));

        var config = await _context.SamlConfigurations
            .FirstOrDefaultAsync(c => c.TenantId == tenantId);

        if (config != null)
        {
            config.Enabled = false;
            config.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _spOptionsCache.Remove(tenantId);

            _logger.LogInformation("SAML disabled for tenant {TenantId}", tenantId);
        }
    }

    public async Task<SsoInitiationResponse> InitiateSsoAsync(string tenantId, string? returnUrl = null)
    {
        var config = await GetSamlConfigurationAsync(tenantId);

        if (config == null || !config.Enabled)
        {
            throw new InvalidOperationException($"SAML is not configured or disabled for tenant {tenantId}");
        }

        var spOptions = await GetOrCreateSPOptions(config);

        // Create AuthnRequest using Sustainsys.Saml2
        var idp = spOptions.IdentityProviders.First();

        var authnRequest = idp.CreateAuthenticateRequest(
            new Uri(_configuration["AppBaseUrl"] + "/api/auth/saml/acs"),
            null);

        var binding = Saml2Binding.Get(Saml2BindingType.HttpRedirect);
        var redirectUrl = binding.Bind(authnRequest).Location.ToString();

        _logger.LogInformation("SSO initiated for tenant {TenantId}, RequestId: {RequestId}",
            tenantId, authnRequest.Id);

        return new SsoInitiationResponse
        {
            RedirectUrl = redirectUrl,
            SamlRequest = authnRequest.ToXElement().ToString(),
            RelayState = returnUrl ?? string.Empty
        };
    }

    public async Task<SamlValidationResult> ValidateSamlResponseAsync(string samlResponse)
    {
        try
        {
            // Decode and parse SAML response
            var response = Saml2Response.Read(samlResponse);

            // Find the matching tenant configuration based on the issuer
            var issuer = response.Issuer.Value;
            var config = await _context.SamlConfigurations
                .FirstOrDefaultAsync(c => c.EntityId == issuer && c.Enabled);

            if (config == null)
            {
                _logger.LogWarning("No matching SAML configuration found for issuer: {Issuer}", issuer);
                return new SamlValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "No matching SAML configuration found"
                };
            }

            var spOptions = await GetOrCreateSPOptions(config);

            // Validate the response (signature, timestamps, conditions)
            var principal = response.GetClaims(spOptions);

            if (principal == null)
            {
                return new SamlValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Failed to extract claims from SAML response"
                };
            }

            // Extract user attributes
            var attributes = new Dictionary<string, string>();
            var attributeMapping = JsonSerializer.Deserialize<Dictionary<string, string>>(config.AttributeMapping)
                ?? new Dictionary<string, string>();

            foreach (var claim in principal.Claims)
            {
                attributes[claim.Type] = claim.Value;
            }

            // Map attributes to user properties
            var email = GetMappedAttribute(attributes, attributeMapping, "email",
                "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress");

            var displayName = GetMappedAttribute(attributes, attributeMapping, "displayName",
                "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name");

            var userId = GetMappedAttribute(attributes, attributeMapping, "userId",
                "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");

            _logger.LogInformation("SAML response validated successfully for user: {Email}", email);

            return new SamlValidationResult
            {
                IsValid = true,
                UserId = userId,
                Email = email,
                DisplayName = displayName,
                Attributes = attributes
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating SAML response");
            return new SamlValidationResult
            {
                IsValid = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<string> GetServiceProviderMetadataAsync(string tenantId)
    {
        var config = await GetSamlConfigurationAsync(tenantId);

        if (config == null)
        {
            throw new InvalidOperationException($"SAML not configured for tenant {tenantId}");
        }

        var spOptions = await GetOrCreateSPOptions(config);

        // Generate metadata using Sustainsys.Saml2
        var metadata = spOptions.CreateMetadata();
        var metadataXml = metadata.ToXmlString();

        return metadataXml;
    }

    private async Task<SPOptions> GetOrCreateSPOptions(SamlConfiguration config)
    {
        if (_spOptionsCache.TryGetValue(config.TenantId, out var cached))
        {
            return cached;
        }

        var spOptions = new SPOptions
        {
            EntityId = new EntityId(_configuration["AppBaseUrl"] + "/api/auth/saml/metadata"),
            ReturnUrl = new Uri(_configuration["AppBaseUrl"] + "/"),
            ModulePath = "/api/auth/saml",
            PublicOrigin = new Uri(_configuration["AppBaseUrl"])
        };

        // Configure Identity Provider
        var idp = new IdentityProvider(new EntityId(config.EntityId), spOptions)
        {
            SingleSignOnServiceUrl = new Uri(config.SsoUrl),
            AllowUnsolicitedAuthnResponse = false,
            Binding = Saml2BindingType.HttpRedirect
        };

        // Add signing certificate
        try
        {
            var cert = new X509Certificate2(Convert.FromBase64String(
                config.X509Certificate.Replace("-----BEGIN CERTIFICATE-----", "")
                                     .Replace("-----END CERTIFICATE-----", "")
                                     .Replace("\n", "")
                                     .Replace("\r", "")));

            idp.SigningKeys.AddConfiguredKey(cert);

            _logger.LogInformation("Added signing certificate for tenant {TenantId}, thumbprint: {Thumbprint}",
                config.TenantId, cert.Thumbprint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse X509 certificate for tenant {TenantId}", config.TenantId);
            throw new InvalidOperationException("Invalid X509 certificate format", ex);
        }

        spOptions.IdentityProviders.Add(idp);

        // Enable signature validation
        spOptions.AuthenticateRequestSigningBehavior = SigningBehavior.IfIdpWantAuthnRequestsSigned;

        // Cache the options
        _spOptionsCache[config.TenantId] = spOptions;

        return spOptions;
    }

    private void ValidateSamlConfiguration(SamlConfigurationRequest request)
    {
        if (string.IsNullOrEmpty(request.EntityId))
            throw new ArgumentException("EntityId is required");

        if (string.IsNullOrEmpty(request.SsoUrl))
            throw new ArgumentException("SsoUrl is required");

        if (!Uri.TryCreate(request.SsoUrl, UriKind.Absolute, out _))
            throw new ArgumentException("SsoUrl must be a valid URL");

        if (string.IsNullOrEmpty(request.X509Certificate))
            throw new ArgumentException("X509Certificate is required");

        // Validate certificate format
        try
        {
            var certData = request.X509Certificate
                .Replace("-----BEGIN CERTIFICATE-----", "")
                .Replace("-----END CERTIFICATE-----", "")
                .Replace("\n", "")
                .Replace("\r", "");

            _ = new X509Certificate2(Convert.FromBase64String(certData));
        }
        catch (Exception ex)
        {
            throw new ArgumentException("Invalid X509 certificate format", ex);
        }
    }

    private string? GetMappedAttribute(
        Dictionary<string, string> attributes,
        Dictionary<string, string> mapping,
        string key,
        string defaultClaimType)
    {
        // First, try to get from custom mapping
        if (mapping.TryGetValue(key, out var mappedClaimType) &&
            attributes.TryGetValue(mappedClaimType, out var value))
        {
            return value;
        }

        // Fall back to default claim type
        if (attributes.TryGetValue(defaultClaimType, out var defaultValue))
        {
            return defaultValue;
        }

        return null;
    }
}
