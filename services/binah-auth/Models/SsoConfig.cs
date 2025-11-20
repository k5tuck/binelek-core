using System;
using System.Collections.Generic;

namespace Binah.Auth.Models;

/// <summary>
/// SSO configuration for a tenant
/// </summary>
public class SsoConfig
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Tenant ID for multi-tenancy
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// SSO provider type (saml, oidc)
    /// </summary>
    public string ProviderType { get; set; } = "saml";

    /// <summary>
    /// Display name for the SSO configuration
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Whether SSO is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = false;

    /// <summary>
    /// Whether SSO is required (no password login)
    /// </summary>
    public bool IsRequired { get; set; } = false;

    // SAML Configuration
    /// <summary>
    /// SAML Entity ID (Issuer)
    /// </summary>
    public string? SamlEntityId { get; set; }

    /// <summary>
    /// SAML SSO URL
    /// </summary>
    public string? SamlSsoUrl { get; set; }

    /// <summary>
    /// SAML SLO (Single Logout) URL
    /// </summary>
    public string? SamlSloUrl { get; set; }

    /// <summary>
    /// SAML X.509 Certificate
    /// </summary>
    public string? SamlCertificate { get; set; }

    /// <summary>
    /// SAML attribute mapping
    /// </summary>
    public Dictionary<string, string> SamlAttributeMapping { get; set; } = new();

    // OIDC Configuration
    /// <summary>
    /// OIDC Client ID
    /// </summary>
    public string? OidcClientId { get; set; }

    /// <summary>
    /// OIDC Client Secret (encrypted)
    /// </summary>
    public string? OidcClientSecret { get; set; }

    /// <summary>
    /// OIDC Issuer URL
    /// </summary>
    public string? OidcIssuer { get; set; }

    /// <summary>
    /// OIDC Authorization endpoint
    /// </summary>
    public string? OidcAuthorizationEndpoint { get; set; }

    /// <summary>
    /// OIDC Token endpoint
    /// </summary>
    public string? OidcTokenEndpoint { get; set; }

    /// <summary>
    /// OIDC UserInfo endpoint
    /// </summary>
    public string? OidcUserInfoEndpoint { get; set; }

    /// <summary>
    /// OIDC scopes to request
    /// </summary>
    public List<string> OidcScopes { get; set; } = new() { "openid", "email", "profile" };

    /// <summary>
    /// OIDC claim mapping
    /// </summary>
    public Dictionary<string, string> OidcClaimMapping { get; set; } = new();

    // Domain verification
    /// <summary>
    /// Verified domains for this SSO configuration
    /// </summary>
    public List<string> VerifiedDomains { get; set; } = new();

    /// <summary>
    /// Default role for new SSO users
    /// </summary>
    public string DefaultRole { get; set; } = "User";

    /// <summary>
    /// Auto-provision users on first login
    /// </summary>
    public bool AutoProvision { get; set; } = true;

    /// <summary>
    /// Update user attributes on each login
    /// </summary>
    public bool UpdateOnLogin { get; set; } = true;

    /// <summary>
    /// Test mode (don't actually authenticate, just validate)
    /// </summary>
    public bool TestMode { get; set; } = false;

    /// <summary>
    /// Last successful connection test
    /// </summary>
    public DateTime? LastTestAt { get; set; }

    /// <summary>
    /// Result of last connection test
    /// </summary>
    public string? LastTestResult { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Domain verification record
/// </summary>
public class DomainVerification
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Tenant ID
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// SSO Config ID
    /// </summary>
    public string SsoConfigId { get; set; } = string.Empty;

    /// <summary>
    /// Domain to verify
    /// </summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// Verification method (dns_txt, dns_cname, meta_tag)
    /// </summary>
    public string VerificationMethod { get; set; } = "dns_txt";

    /// <summary>
    /// Verification token/value
    /// </summary>
    public string VerificationToken { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Whether the domain is verified
    /// </summary>
    public bool IsVerified { get; set; } = false;

    /// <summary>
    /// When the domain was verified
    /// </summary>
    public DateTime? VerifiedAt { get; set; }

    /// <summary>
    /// Last verification attempt
    /// </summary>
    public DateTime? LastCheckAt { get; set; }

    /// <summary>
    /// Number of verification attempts
    /// </summary>
    public int AttemptCount { get; set; } = 0;

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// SSO configuration DTO (without secrets)
/// </summary>
public class SsoConfigDto
{
    public string Id { get; set; } = string.Empty;
    public string ProviderType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public bool IsRequired { get; set; }

    // SAML (safe fields)
    public string? SamlEntityId { get; set; }
    public string? SamlSsoUrl { get; set; }
    public string? SamlSloUrl { get; set; }
    public bool HasSamlCertificate { get; set; }
    public Dictionary<string, string> SamlAttributeMapping { get; set; } = new();

    // OIDC (safe fields)
    public string? OidcClientId { get; set; }
    public bool HasOidcClientSecret { get; set; }
    public string? OidcIssuer { get; set; }
    public List<string> OidcScopes { get; set; } = new();
    public Dictionary<string, string> OidcClaimMapping { get; set; } = new();

    // Domain
    public List<string> VerifiedDomains { get; set; } = new();
    public string DefaultRole { get; set; } = string.Empty;
    public bool AutoProvision { get; set; }
    public bool UpdateOnLogin { get; set; }

    // Status
    public DateTime? LastTestAt { get; set; }
    public string? LastTestResult { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// SSO connection test result
/// </summary>
public class SsoTestResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, string> Details { get; set; } = new();
    public DateTime TestedAt { get; set; } = DateTime.UtcNow;
}
