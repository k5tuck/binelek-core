namespace Binah.API.Models;

/// <summary>
/// Represents a platform licensee - a customer who licenses the Binelek platform
/// </summary>
public class Licensee
{
    /// <summary>
    /// Unique licensee identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Licensee name (company/organization name)
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Hashed license key (NEVER store plaintext)
    /// </summary>
    public required string LicenseKeyHash { get; set; }

    /// <summary>
    /// Licensee status
    /// </summary>
    public LicenseeStatus Status { get; set; } = LicenseeStatus.Trial;

    /// <summary>
    /// Feature flags and limits for this licensee
    /// </summary>
    public LicenseFeatures Features { get; set; } = new();

    /// <summary>
    /// When the licensee was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the license expires (null = never expires)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Primary contact email
    /// </summary>
    public string? ContactEmail { get; set; }

    /// <summary>
    /// Primary contact name
    /// </summary>
    public string? ContactName { get; set; }

    /// <summary>
    /// Company name (may differ from licensee name)
    /// </summary>
    public string? CompanyName { get; set; }

    /// <summary>
    /// Additional metadata (custom fields, notes, etc.)
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Check if the license is currently valid
    /// </summary>
    public bool IsValid()
    {
        return Status == LicenseeStatus.Active &&
               (ExpiresAt == null || ExpiresAt > DateTime.UtcNow);
    }

    /// <summary>
    /// Check if the license is expired
    /// </summary>
    public bool IsExpired()
    {
        return ExpiresAt.HasValue && ExpiresAt <= DateTime.UtcNow;
    }

    /// <summary>
    /// Check if a specific feature is enabled
    /// </summary>
    public bool HasFeature(string featureName)
    {
        return Features.IsFeatureEnabled(featureName);
    }
}

/// <summary>
/// Licensee status enumeration
/// </summary>
public enum LicenseeStatus
{
    /// <summary>
    /// Active license - full access
    /// </summary>
    Active,

    /// <summary>
    /// Suspended license - temporarily disabled
    /// </summary>
    Suspended,

    /// <summary>
    /// Expired license - past expiration date
    /// </summary>
    Expired,

    /// <summary>
    /// Trial license - limited time evaluation
    /// </summary>
    Trial
}

/// <summary>
/// Feature flags and limits for a licensee
/// </summary>
public class LicenseFeatures
{
    // Platform features (boolean flags)
    public bool MultiDomain { get; set; } = false;
    public bool AiPlatform { get; set; } = false;
    public bool ApiAccess { get; set; } = true;
    public bool CustomBranding { get; set; } = false;
    public bool SsoEnabled { get; set; } = false;
    public bool WebhooksEnabled { get; set; } = true;
    public bool AdvancedAnalytics { get; set; } = false;
    public bool DataExport { get; set; } = true;

    // Limits (numeric)
    public int MaxUsers { get; set; } = 10;
    public int MaxEntities { get; set; } = 10000;
    public int ApiRateLimit { get; set; } = 1000; // requests per hour
    public int StorageLimit { get; set; } = 10; // GB

    // Domain restrictions (list of allowed domain IDs)
    public List<string> AllowedDomains { get; set; } = new() { "real-estate" };

    /// <summary>
    /// Check if a specific feature is enabled
    /// </summary>
    public bool IsFeatureEnabled(string featureName)
    {
        return featureName.ToLowerInvariant() switch
        {
            "multi_domain" or "multidomain" => MultiDomain,
            "ai_platform" or "aiplatform" => AiPlatform,
            "api_access" or "apiaccess" => ApiAccess,
            "custom_branding" or "custombranding" => CustomBranding,
            "sso_enabled" or "ssoenabled" or "sso" => SsoEnabled,
            "webhooks" or "webhooks_enabled" => WebhooksEnabled,
            "advanced_analytics" or "advancedanalytics" => AdvancedAnalytics,
            "data_export" or "dataexport" => DataExport,
            _ => false
        };
    }

    /// <summary>
    /// Check if a domain is allowed for this licensee
    /// </summary>
    public bool IsDomainAllowed(string domainId)
    {
        if (MultiDomain)
            return true; // Multi-domain license has access to all domains

        return AllowedDomains.Contains(domainId, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Create a default trial license feature set
    /// </summary>
    public static LicenseFeatures CreateTrialFeatures()
    {
        return new LicenseFeatures
        {
            MultiDomain = false,
            AiPlatform = false,
            ApiAccess = true,
            CustomBranding = false,
            SsoEnabled = false,
            WebhooksEnabled = true,
            AdvancedAnalytics = false,
            DataExport = true,
            MaxUsers = 5,
            MaxEntities = 1000,
            ApiRateLimit = 100,
            StorageLimit = 1,
            AllowedDomains = new List<string> { "real-estate" }
        };
    }

    /// <summary>
    /// Create a full enterprise license feature set
    /// </summary>
    public static LicenseFeatures CreateEnterpriseFeatures()
    {
        return new LicenseFeatures
        {
            MultiDomain = true,
            AiPlatform = true,
            ApiAccess = true,
            CustomBranding = true,
            SsoEnabled = true,
            WebhooksEnabled = true,
            AdvancedAnalytics = true,
            DataExport = true,
            MaxUsers = 999999,
            MaxEntities = 999999,
            ApiRateLimit = 999999,
            StorageLimit = 999999,
            AllowedDomains = new List<string>() // Empty = all domains allowed when MultiDomain = true
        };
    }
}
