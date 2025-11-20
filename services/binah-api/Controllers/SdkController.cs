using Binah.API.Models;
using Binah.Infrastructure.MultiTenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Binah.API.Controllers;

/// <summary>
/// SDK downloads and user settings endpoints
/// </summary>
[ApiController]
[Route("api/sdk")]
[Authorize]
public class SdkController : ControllerBase
{
    private readonly ILogger<SdkController> _logger;
    private readonly IConfiguration _configuration;

    // Static SDK information (in production, this would come from a database or file storage)
    private static readonly List<SdkInfo> AvailableSdks = new()
    {
        new SdkInfo
        {
            Id = "csharp",
            Name = "Binelek SDK for .NET",
            Language = "C#",
            Version = "1.2.0",
            Size = "2.4 MB",
            Checksum = "sha256:a1b2c3d4e5f6789012345678901234567890abcdef",
            DownloadUrl = "/sdk/downloads/binelek-sdk-dotnet-1.2.0.zip",
            LastUpdated = new DateTime(2025, 11, 1, 0, 0, 0, DateTimeKind.Utc),
            Description = "Official .NET SDK for Binelek platform integration",
            Features = new List<string>
            {
                "Full API coverage",
                "Async/await support",
                "Strong typing",
                "Entity Framework integration"
            },
            DocumentationUrl = "https://docs.binelek.com/sdk/csharp",
            PackageManager = "NuGet",
            InstallCommand = "dotnet add package Binelek.SDK"
        },
        new SdkInfo
        {
            Id = "typescript",
            Name = "Binelek SDK for TypeScript",
            Language = "TypeScript",
            Version = "1.2.0",
            Size = "1.8 MB",
            Checksum = "sha256:b2c3d4e5f6789012345678901234567890abcdef01",
            DownloadUrl = "/sdk/downloads/binelek-sdk-typescript-1.2.0.zip",
            LastUpdated = new DateTime(2025, 11, 1, 0, 0, 0, DateTimeKind.Utc),
            Description = "Official TypeScript/Node.js SDK for Binelek platform integration",
            Features = new List<string>
            {
                "Full TypeScript support",
                "Browser and Node.js compatible",
                "Promise-based API",
                "Real-time subscriptions"
            },
            DocumentationUrl = "https://docs.binelek.com/sdk/typescript",
            PackageManager = "npm",
            InstallCommand = "npm install @binelek/sdk"
        },
        new SdkInfo
        {
            Id = "python",
            Name = "Binelek SDK for Python",
            Language = "Python",
            Version = "1.1.0",
            Size = "1.2 MB",
            Checksum = "sha256:c3d4e5f6789012345678901234567890abcdef0123",
            DownloadUrl = "/sdk/downloads/binelek-sdk-python-1.1.0.zip",
            LastUpdated = new DateTime(2025, 10, 15, 0, 0, 0, DateTimeKind.Utc),
            Description = "Official Python SDK for Binelek platform integration",
            Features = new List<string>
            {
                "Python 3.8+ support",
                "Async support with asyncio",
                "Type hints included",
                "Pandas/NumPy integration"
            },
            DocumentationUrl = "https://docs.binelek.com/sdk/python",
            PackageManager = "pip",
            InstallCommand = "pip install binelek-sdk"
        }
    };

    public SdkController(
        ILogger<SdkController> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Get list of available SDKs for download
    /// </summary>
    [HttpGet("downloads")]
    public ActionResult<SdkListResponse> GetAvailableSdks()
    {
        var tenantId = LicenseeContext.GetLicenseeId() ?? "unknown";
        _logger.LogInformation("SDK list requested by tenant {TenantId}", tenantId);

        return Ok(new SdkListResponse
        {
            Sdks = AvailableSdks,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Get download URL for a specific SDK
    /// </summary>
    [HttpGet("downloads/{id}")]
    public ActionResult<SdkDownloadResponse> GetSdkDownload(string id)
    {
        var tenantId = LicenseeContext.GetLicenseeId() ?? "unknown";
        var sdk = AvailableSdks.FirstOrDefault(s => s.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

        if (sdk == null)
        {
            _logger.LogWarning("SDK download requested for unknown SDK {SdkId} by tenant {TenantId}", id, tenantId);
            return NotFound(new { message = $"SDK '{id}' not found" });
        }

        _logger.LogInformation("SDK download URL requested for {SdkId} by tenant {TenantId}", id, tenantId);

        // In production, this would generate a signed URL with expiration
        var baseUrl = _configuration["SdkSettings:StorageBaseUrl"] ?? "https://storage.binelek.com";
        var downloadUrl = $"{baseUrl}{sdk.DownloadUrl}?token={Guid.NewGuid()}";

        return Ok(new SdkDownloadResponse
        {
            Id = sdk.Id,
            DownloadUrl = downloadUrl,
            Checksum = sdk.Checksum,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        });
    }

    /// <summary>
    /// Get user settings
    /// </summary>
    [HttpGet("settings")]
    public ActionResult<UserSettings> GetUserSettings()
    {
        var tenantId = LicenseeContext.GetLicenseeId() ?? "unknown";
        var userId = User.FindFirst("sub")?.Value ?? "unknown";

        _logger.LogInformation("User settings requested by user {UserId} in tenant {TenantId}", userId, tenantId);

        // In production, this would retrieve from database
        // For now, return default settings
        var settings = new UserSettings
        {
            UserId = userId,
            Theme = "system",
            Notifications = new NotificationSettings
            {
                EmailAlerts = true,
                PushNotifications = true,
                ActionNotifications = true,
                WeeklyDigest = false,
                TeamUpdates = true
            },
            Language = "en",
            Timezone = "UTC",
            DateFormat = "mdy",
            UpdatedAt = DateTime.UtcNow
        };

        return Ok(settings);
    }

    /// <summary>
    /// Update user settings
    /// </summary>
    [HttpPut("settings")]
    public ActionResult<UserSettings> UpdateUserSettings([FromBody] UpdateUserSettingsRequest request)
    {
        var tenantId = LicenseeContext.GetLicenseeId() ?? "unknown";
        var userId = User.FindFirst("sub")?.Value ?? "unknown";

        _logger.LogInformation("User settings updated by user {UserId} in tenant {TenantId}", userId, tenantId);

        // In production, this would persist to database
        var settings = new UserSettings
        {
            UserId = userId,
            Theme = request.Theme ?? "system",
            Notifications = request.Notifications ?? new NotificationSettings(),
            Language = request.Language ?? "en",
            Timezone = request.Timezone ?? "UTC",
            DateFormat = request.DateFormat ?? "mdy",
            UpdatedAt = DateTime.UtcNow
        };

        return Ok(settings);
    }
}
