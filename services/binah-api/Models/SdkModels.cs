namespace Binah.API.Models;

/// <summary>
/// SDK information for download
/// </summary>
public class SdkInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string Checksum { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public string Description { get; set; } = string.Empty;
    public List<string> Features { get; set; } = new();
    public string DocumentationUrl { get; set; } = string.Empty;
    public string PackageManager { get; set; } = string.Empty;
    public string InstallCommand { get; set; } = string.Empty;
}

/// <summary>
/// Response for SDK list
/// </summary>
public class SdkListResponse
{
    public List<SdkInfo> Sdks { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Response for SDK download URL
/// </summary>
public class SdkDownloadResponse
{
    public string Id { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public string Checksum { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(1);
}

/// <summary>
/// User settings
/// </summary>
public class UserSettings
{
    public string UserId { get; set; } = string.Empty;
    public string Theme { get; set; } = "system"; // light, dark, system
    public NotificationSettings Notifications { get; set; } = new();
    public string Language { get; set; } = "en";
    public string Timezone { get; set; } = "UTC";
    public string DateFormat { get; set; } = "mdy"; // mdy, dmy, ymd
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Notification preferences
/// </summary>
public class NotificationSettings
{
    public bool EmailAlerts { get; set; } = true;
    public bool PushNotifications { get; set; } = true;
    public bool ActionNotifications { get; set; } = true;
    public bool WeeklyDigest { get; set; } = false;
    public bool TeamUpdates { get; set; } = true;
}

/// <summary>
/// Update user settings request
/// </summary>
public class UpdateUserSettingsRequest
{
    public string? Theme { get; set; }
    public NotificationSettings? Notifications { get; set; }
    public string? Language { get; set; }
    public string? Timezone { get; set; }
    public string? DateFormat { get; set; }
}
