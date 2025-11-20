using Binah.Auth.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

namespace Binah.Auth.Services;

/// <summary>
/// Implementation of SAML SSO service
/// Note: This is a basic implementation. For production use, integrate with
/// a full SAML library like Sustainsys.Saml2 or ITfoxtec.Identity.Saml2
/// </summary>
public class SamlService : ISamlService
{
    private readonly AuthDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SamlService> _logger;

    public SamlService(
        AuthDbContext context,
        IConfiguration configuration,
        ILogger<SamlService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SamlConfiguration> ConfigureSamlAsync(string tenantId, SamlConfigurationRequest request)
    {
        if (string.IsNullOrEmpty(tenantId))
            throw new ArgumentException("TenantId is required", nameof(tenantId));

        // Check if configuration already exists
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

        // Generate SAML AuthnRequest
        var requestId = "_" + Guid.NewGuid().ToString();
        var issueInstant = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var acsUrl = $"{_configuration["AppBaseUrl"]}/api/auth/saml/acs";

        var samlRequest = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<samlp:AuthnRequest xmlns:samlp=""urn:oasis:names:tc:SAML:2.0:protocol""
                    xmlns:saml=""urn:oasis:names:tc:SAML:2.0:assertion""
                    ID=""{requestId}""
                    Version=""2.0""
                    IssueInstant=""{issueInstant}""
                    Destination=""{config.SsoUrl}""
                    AssertionConsumerServiceURL=""{acsUrl}"">
    <saml:Issuer>binah-auth</saml:Issuer>
</samlp:AuthnRequest>";

        // Base64 encode the request
        var encodedRequest = Convert.ToBase64String(Encoding.UTF8.GetBytes(samlRequest));

        // Create redirect URL
        var redirectUrl = $"{config.SsoUrl}?SAMLRequest={Uri.EscapeDataString(encodedRequest)}";
        if (!string.IsNullOrEmpty(returnUrl))
        {
            redirectUrl += $"&RelayState={Uri.EscapeDataString(returnUrl)}";
        }

        _logger.LogInformation("SSO initiated for tenant {TenantId}", tenantId);

        return new SsoInitiationResponse
        {
            RedirectUrl = redirectUrl,
            SamlRequest = encodedRequest,
            RelayState = returnUrl ?? string.Empty
        };
    }

    public async Task<SamlValidationResult> ValidateSamlResponseAsync(string samlResponse)
    {
        // NOTE: This is a placeholder implementation
        // In production, you would:
        // 1. Decode the SAML response
        // 2. Verify the signature using the IdP certificate
        // 3. Validate timestamps and conditions
        // 4. Extract user attributes
        // 5. Map attributes to user properties

        try
        {
            // Decode base64 SAML response
            var decodedResponse = Encoding.UTF8.GetString(Convert.FromBase64String(samlResponse));

            _logger.LogInformation("Validating SAML response");

            // For now, return a placeholder result
            // In production, implement full SAML response validation
            return new SamlValidationResult
            {
                IsValid = false,
                ErrorMessage = "SAML validation not fully implemented. Please integrate with Sustainsys.Saml2 or similar library."
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

        var acsUrl = $"{_configuration["AppBaseUrl"]}/api/auth/saml/acs";
        var entityId = "binah-auth";

        var metadata = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<md:EntityDescriptor xmlns:md=""urn:oasis:names:tc:SAML:2.0:metadata""
                     entityID=""{entityId}"">
    <md:SPSSODescriptor protocolSupportEnumeration=""urn:oasis:names:tc:SAML:2.0:protocol"">
        <md:AssertionConsumerService Binding=""urn:oasis:names:tc:SAML:2.0:bindings:HTTP-POST""
                                    Location=""{acsUrl}""
                                    index=""0""
                                    isDefault=""true""/>
    </md:SPSSODescriptor>
</md:EntityDescriptor>";

        return metadata;
    }
}
