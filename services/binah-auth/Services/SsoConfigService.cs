using Binah.Auth.Models;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace Binah.Auth.Services;

/// <summary>
/// SSO configuration management service implementation
/// </summary>
public class SsoConfigService : ISsoConfigService
{
    private readonly AuthDbContext _context;
    private readonly ILogger<SsoConfigService> _logger;
    private readonly HttpClient _httpClient;

    public SsoConfigService(AuthDbContext context, ILogger<SsoConfigService> logger, IHttpClientFactory httpClientFactory)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClientFactory.CreateClient();
    }

    public async Task<SsoConfigDto?> GetConfigAsync(string tenantId)
    {
        var config = await _context.SsoConfigs
            .FirstOrDefaultAsync(c => c.TenantId == tenantId);

        return config == null ? null : ToDto(config);
    }

    public async Task<SsoConfigDto> SaveConfigAsync(string tenantId, SsoConfig config)
    {
        var existing = await _context.SsoConfigs
            .FirstOrDefaultAsync(c => c.TenantId == tenantId);

        if (existing == null)
        {
            config.TenantId = tenantId;
            config.CreatedAt = DateTime.UtcNow;
            config.UpdatedAt = DateTime.UtcNow;
            _context.SsoConfigs.Add(config);
        }
        else
        {
            // Update existing
            existing.ProviderType = config.ProviderType;
            existing.Name = config.Name;
            existing.IsEnabled = config.IsEnabled;
            existing.IsRequired = config.IsRequired;

            // SAML
            existing.SamlEntityId = config.SamlEntityId;
            existing.SamlSsoUrl = config.SamlSsoUrl;
            existing.SamlSloUrl = config.SamlSloUrl;
            if (!string.IsNullOrEmpty(config.SamlCertificate))
                existing.SamlCertificate = config.SamlCertificate;
            existing.SamlAttributeMapping = config.SamlAttributeMapping;

            // OIDC
            existing.OidcClientId = config.OidcClientId;
            if (!string.IsNullOrEmpty(config.OidcClientSecret))
                existing.OidcClientSecret = config.OidcClientSecret;
            existing.OidcIssuer = config.OidcIssuer;
            existing.OidcAuthorizationEndpoint = config.OidcAuthorizationEndpoint;
            existing.OidcTokenEndpoint = config.OidcTokenEndpoint;
            existing.OidcUserInfoEndpoint = config.OidcUserInfoEndpoint;
            existing.OidcScopes = config.OidcScopes;
            existing.OidcClaimMapping = config.OidcClaimMapping;

            // Settings
            existing.DefaultRole = config.DefaultRole;
            existing.AutoProvision = config.AutoProvision;
            existing.UpdateOnLogin = config.UpdateOnLogin;
            existing.UpdatedAt = DateTime.UtcNow;

            config = existing;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Saved SSO configuration for tenant {TenantId}", tenantId);

        return ToDto(config);
    }

    public async Task<bool> DeleteConfigAsync(string tenantId)
    {
        var config = await _context.SsoConfigs
            .FirstOrDefaultAsync(c => c.TenantId == tenantId);

        if (config == null) return false;

        // Also remove domain verifications
        var domains = await _context.DomainVerifications
            .Where(d => d.TenantId == tenantId)
            .ToListAsync();

        _context.DomainVerifications.RemoveRange(domains);
        _context.SsoConfigs.Remove(config);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted SSO configuration for tenant {TenantId}", tenantId);

        return true;
    }

    public async Task<bool> SetEnabledAsync(string tenantId, bool enabled)
    {
        var config = await _context.SsoConfigs
            .FirstOrDefaultAsync(c => c.TenantId == tenantId);

        if (config == null) return false;

        config.IsEnabled = enabled;
        config.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Set SSO enabled={Enabled} for tenant {TenantId}", enabled, tenantId);

        return true;
    }

    public async Task<SsoTestResult> TestConnectionAsync(string tenantId)
    {
        var config = await _context.SsoConfigs
            .FirstOrDefaultAsync(c => c.TenantId == tenantId);

        if (config == null)
        {
            return new SsoTestResult
            {
                Success = false,
                Message = "SSO configuration not found"
            };
        }

        var result = new SsoTestResult();
        var details = new Dictionary<string, string>();

        try
        {
            if (config.ProviderType == "saml")
            {
                // Test SAML configuration
                if (string.IsNullOrEmpty(config.SamlSsoUrl))
                {
                    result.Success = false;
                    result.Message = "SAML SSO URL is not configured";
                    return result;
                }

                // Try to fetch metadata or SSO endpoint
                var response = await _httpClient.GetAsync(config.SamlSsoUrl);
                details["sso_url_status"] = response.StatusCode.ToString();

                if (!string.IsNullOrEmpty(config.SamlCertificate))
                {
                    details["certificate_configured"] = "true";
                    // Validate certificate format
                    try
                    {
                        var certBytes = Convert.FromBase64String(
                            config.SamlCertificate
                                .Replace("-----BEGIN CERTIFICATE-----", "")
                                .Replace("-----END CERTIFICATE-----", "")
                                .Replace("\n", "")
                                .Replace("\r", "")
                        );
                        details["certificate_valid"] = "true";
                    }
                    catch
                    {
                        details["certificate_valid"] = "false";
                        result.Success = false;
                        result.Message = "SAML certificate format is invalid";
                        return result;
                    }
                }

                result.Success = true;
                result.Message = "SAML configuration is valid";
            }
            else if (config.ProviderType == "oidc")
            {
                // Test OIDC configuration
                if (string.IsNullOrEmpty(config.OidcIssuer))
                {
                    result.Success = false;
                    result.Message = "OIDC Issuer URL is not configured";
                    return result;
                }

                // Try to fetch .well-known/openid-configuration
                var wellKnownUrl = config.OidcIssuer.TrimEnd('/') + "/.well-known/openid-configuration";
                var response = await _httpClient.GetAsync(wellKnownUrl);
                details["well_known_status"] = response.StatusCode.ToString();

                if (response.IsSuccessStatusCode)
                {
                    details["openid_configuration"] = "discovered";
                    result.Success = true;
                    result.Message = "OIDC configuration is valid";
                }
                else
                {
                    result.Success = false;
                    result.Message = $"Failed to fetch OpenID configuration: {response.StatusCode}";
                }
            }
            else
            {
                result.Success = false;
                result.Message = $"Unknown provider type: {config.ProviderType}";
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Connection test failed: {ex.Message}";
            _logger.LogError(ex, "SSO connection test failed for tenant {TenantId}", tenantId);
        }

        result.Details = details;
        result.TestedAt = DateTime.UtcNow;

        // Update last test result
        config.LastTestAt = result.TestedAt;
        config.LastTestResult = result.Success ? "success" : "failed";
        await _context.SaveChangesAsync();

        return result;
    }

    public async Task<DomainVerification> AddDomainAsync(string tenantId, string domain)
    {
        var ssoConfig = await _context.SsoConfigs
            .FirstOrDefaultAsync(c => c.TenantId == tenantId);

        if (ssoConfig == null)
            throw new InvalidOperationException("SSO configuration not found");

        // Check if domain already exists
        var existing = await _context.DomainVerifications
            .FirstOrDefaultAsync(d => d.Domain == domain.ToLowerInvariant());

        if (existing != null)
            throw new InvalidOperationException($"Domain {domain} is already registered");

        var verification = new DomainVerification
        {
            TenantId = tenantId,
            SsoConfigId = ssoConfig.Id,
            Domain = domain.ToLowerInvariant(),
            VerificationToken = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow
        };

        _context.DomainVerifications.Add(verification);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added domain {Domain} for verification in tenant {TenantId}", domain, tenantId);

        return verification;
    }

    public async Task<DomainVerification> VerifyDomainAsync(string tenantId, string domain)
    {
        var verification = await _context.DomainVerifications
            .FirstOrDefaultAsync(d => d.TenantId == tenantId && d.Domain == domain.ToLowerInvariant());

        if (verification == null)
            throw new InvalidOperationException($"Domain {domain} not found");

        verification.AttemptCount++;
        verification.LastCheckAt = DateTime.UtcNow;

        try
        {
            bool verified = false;

            if (verification.VerificationMethod == "dns_txt")
            {
                // Check for TXT record
                var txtRecord = $"binah-verification={verification.VerificationToken}";
                try
                {
                    var dnsRecords = await Dns.GetHostEntryAsync($"_binah-verification.{domain}");
                    // In real implementation, check for TXT record
                    // For now, we'll simulate
                    verified = false; // Would check actual DNS
                }
                catch
                {
                    // DNS lookup failed
                }
            }

            if (verified)
            {
                verification.IsVerified = true;
                verification.VerifiedAt = DateTime.UtcNow;

                // Add to SSO config verified domains
                var ssoConfig = await _context.SsoConfigs.FindAsync(verification.SsoConfigId);
                if (ssoConfig != null && !ssoConfig.VerifiedDomains.Contains(domain.ToLowerInvariant()))
                {
                    ssoConfig.VerifiedDomains.Add(domain.ToLowerInvariant());
                }

                _logger.LogInformation("Verified domain {Domain} for tenant {TenantId}", domain, tenantId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Domain verification failed for {Domain}", domain);
        }

        await _context.SaveChangesAsync();

        return verification;
    }

    public async Task<bool> RemoveDomainAsync(string tenantId, string domain)
    {
        var verification = await _context.DomainVerifications
            .FirstOrDefaultAsync(d => d.TenantId == tenantId && d.Domain == domain.ToLowerInvariant());

        if (verification == null) return false;

        // Remove from SSO config verified domains
        var ssoConfig = await _context.SsoConfigs.FindAsync(verification.SsoConfigId);
        if (ssoConfig != null)
        {
            ssoConfig.VerifiedDomains.Remove(domain.ToLowerInvariant());
        }

        _context.DomainVerifications.Remove(verification);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Removed domain {Domain} from tenant {TenantId}", domain, tenantId);

        return true;
    }

    public async Task<List<DomainVerification>> GetDomainsAsync(string tenantId)
    {
        return await _context.DomainVerifications
            .Where(d => d.TenantId == tenantId)
            .OrderBy(d => d.Domain)
            .ToListAsync();
    }

    public async Task<SsoConfig?> GetConfigByDomainAsync(string emailDomain)
    {
        var verification = await _context.DomainVerifications
            .FirstOrDefaultAsync(d => d.Domain == emailDomain.ToLowerInvariant() && d.IsVerified);

        if (verification == null) return null;

        return await _context.SsoConfigs
            .FirstOrDefaultAsync(c => c.Id == verification.SsoConfigId && c.IsEnabled);
    }

    private static SsoConfigDto ToDto(SsoConfig config)
    {
        return new SsoConfigDto
        {
            Id = config.Id,
            ProviderType = config.ProviderType,
            Name = config.Name,
            IsEnabled = config.IsEnabled,
            IsRequired = config.IsRequired,

            SamlEntityId = config.SamlEntityId,
            SamlSsoUrl = config.SamlSsoUrl,
            SamlSloUrl = config.SamlSloUrl,
            HasSamlCertificate = !string.IsNullOrEmpty(config.SamlCertificate),
            SamlAttributeMapping = config.SamlAttributeMapping,

            OidcClientId = config.OidcClientId,
            HasOidcClientSecret = !string.IsNullOrEmpty(config.OidcClientSecret),
            OidcIssuer = config.OidcIssuer,
            OidcScopes = config.OidcScopes,
            OidcClaimMapping = config.OidcClaimMapping,

            VerifiedDomains = config.VerifiedDomains,
            DefaultRole = config.DefaultRole,
            AutoProvision = config.AutoProvision,
            UpdateOnLogin = config.UpdateOnLogin,

            LastTestAt = config.LastTestAt,
            LastTestResult = config.LastTestResult,
            CreatedAt = config.CreatedAt,
            UpdatedAt = config.UpdatedAt
        };
    }
}
