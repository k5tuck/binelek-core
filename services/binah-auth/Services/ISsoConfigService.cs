using Binah.Auth.Models;

namespace Binah.Auth.Services;

/// <summary>
/// Interface for SSO configuration management
/// </summary>
public interface ISsoConfigService
{
    /// <summary>
    /// Get SSO configuration for a tenant
    /// </summary>
    Task<SsoConfigDto?> GetConfigAsync(string tenantId);

    /// <summary>
    /// Create or update SSO configuration
    /// </summary>
    Task<SsoConfigDto> SaveConfigAsync(string tenantId, SsoConfig config);

    /// <summary>
    /// Delete SSO configuration
    /// </summary>
    Task<bool> DeleteConfigAsync(string tenantId);

    /// <summary>
    /// Enable or disable SSO
    /// </summary>
    Task<bool> SetEnabledAsync(string tenantId, bool enabled);

    /// <summary>
    /// Test SSO connection
    /// </summary>
    Task<SsoTestResult> TestConnectionAsync(string tenantId);

    /// <summary>
    /// Add a domain for verification
    /// </summary>
    Task<DomainVerification> AddDomainAsync(string tenantId, string domain);

    /// <summary>
    /// Verify a domain
    /// </summary>
    Task<DomainVerification> VerifyDomainAsync(string tenantId, string domain);

    /// <summary>
    /// Remove a domain
    /// </summary>
    Task<bool> RemoveDomainAsync(string tenantId, string domain);

    /// <summary>
    /// Get all domains for a tenant
    /// </summary>
    Task<List<DomainVerification>> GetDomainsAsync(string tenantId);

    /// <summary>
    /// Get SSO configuration by verified domain
    /// </summary>
    Task<SsoConfig?> GetConfigByDomainAsync(string emailDomain);
}
