using Binah.Auth.Models;

namespace Binah.Auth.Services;

/// <summary>
/// Interface for SAML SSO service
/// </summary>
public interface ISamlService
{
    /// <summary>
    /// Configure SAML for a tenant
    /// </summary>
    Task<SamlConfiguration> ConfigureSamlAsync(string tenantId, SamlConfigurationRequest request);

    /// <summary>
    /// Get SAML configuration for a tenant
    /// </summary>
    Task<SamlConfiguration?> GetSamlConfigurationAsync(string tenantId);

    /// <summary>
    /// Update SAML configuration for a tenant
    /// </summary>
    Task<SamlConfiguration> UpdateSamlConfigurationAsync(string tenantId, SamlConfigurationRequest request);

    /// <summary>
    /// Disable SAML for a tenant
    /// </summary>
    Task DisableSamlAsync(string tenantId);

    /// <summary>
    /// Initiate SSO login
    /// </summary>
    Task<SsoInitiationResponse> InitiateSsoAsync(string tenantId, string? returnUrl = null);

    /// <summary>
    /// Validate SAML response (Assertion Consumer Service)
    /// </summary>
    Task<SamlValidationResult> ValidateSamlResponseAsync(string samlResponse);

    /// <summary>
    /// Get SP metadata XML
    /// </summary>
    Task<string> GetServiceProviderMetadataAsync(string tenantId);
}

/// <summary>
/// SAML configuration request
/// </summary>
public class SamlConfigurationRequest
{
    public string EntityId { get; set; } = string.Empty;
    public string SsoUrl { get; set; } = string.Empty;
    public string X509Certificate { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public Dictionary<string, string> AttributeMapping { get; set; } = new();
}

/// <summary>
/// SSO initiation response
/// </summary>
public class SsoInitiationResponse
{
    public string RedirectUrl { get; set; } = string.Empty;
    public string SamlRequest { get; set; } = string.Empty;
    public string RelayState { get; set; } = string.Empty;
}

/// <summary>
/// SAML validation result
/// </summary>
public class SamlValidationResult
{
    public bool IsValid { get; set; }
    public string? UserId { get; set; }
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public Dictionary<string, string> Attributes { get; set; } = new();
    public string? ErrorMessage { get; set; }
}
