namespace Binah.Auth.Models;

/// <summary>
/// SAML SSO configuration for a tenant
/// </summary>
public class SamlConfiguration
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Tenant identifier
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Identity Provider Entity ID
    /// </summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Identity Provider SSO URL
    /// </summary>
    public string SsoUrl { get; set; } = string.Empty;

    /// <summary>
    /// Identity Provider X.509 Certificate (PEM format)
    /// </summary>
    public string X509Certificate { get; set; } = string.Empty;

    /// <summary>
    /// Whether SAML is enabled for this tenant
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Attribute mapping from SAML assertions to user properties
    /// JSON format: { "email": "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", ... }
    /// </summary>
    public string AttributeMapping { get; set; } = "{}";

    /// <summary>
    /// When the configuration was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the configuration was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
