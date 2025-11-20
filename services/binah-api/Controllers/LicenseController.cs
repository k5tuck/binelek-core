using Binah.API.Models;
using Binah.API.Services;
using Binah.Infrastructure.MultiTenancy;
using Microsoft.AspNetCore.Mvc;

namespace Binah.API.Controllers;

[ApiController]
[Route("api/license")]
public class LicenseController : ControllerBase
{
    private readonly ILicenseeService _licenseeService;
    private readonly ILogger<LicenseController> _logger;

    public LicenseController(ILicenseeService licenseeService, ILogger<LicenseController> logger)
    {
        _licenseeService = licenseeService;
        _logger = logger;
    }

    /// <summary>
    /// Get license information for the current licensee
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<LicenseInfoResponse>> GetLicenseInfo()
    {
        var licenseeId = LicenseeContext.GetRequiredLicenseeId();
        var licensee = await _licenseeService.GetByIdAsync(licenseeId);

        if (licensee == null)
        {
            return NotFound(new { message = "Licensee not found" });
        }

        var response = new LicenseInfoResponse
        {
            Id = licensee.Id,
            Name = licensee.Name,
            LicenseType = GetLicenseType(licensee),
            Status = licensee.Status.ToString().ToLower(),
            IsValid = licensee.IsValid(),
            IsExpired = licensee.IsExpired(),
            CreatedAt = licensee.CreatedAt,
            ExpiresAt = licensee.ExpiresAt,
            ContactEmail = licensee.ContactEmail,
            ContactName = licensee.ContactName,
            CompanyName = licensee.CompanyName,
            Features = new LicenseFeaturesResponse
            {
                MultiDomain = licensee.Features.MultiDomain,
                AiPlatform = licensee.Features.AiPlatform,
                ApiAccess = licensee.Features.ApiAccess,
                CustomBranding = licensee.Features.CustomBranding,
                SsoEnabled = licensee.Features.SsoEnabled,
                WebhooksEnabled = licensee.Features.WebhooksEnabled,
                AdvancedAnalytics = licensee.Features.AdvancedAnalytics,
                DataExport = licensee.Features.DataExport,
            },
            Limits = new LicenseLimitsResponse
            {
                MaxUsers = licensee.Features.MaxUsers,
                MaxEntities = licensee.Features.MaxEntities,
                ApiRateLimit = licensee.Features.ApiRateLimit,
                StorageLimit = licensee.Features.StorageLimit,
            },
            AllowedDomains = licensee.Features.AllowedDomains,
        };

        return Ok(response);
    }

    /// <summary>
    /// Get current usage statistics
    /// </summary>
    [HttpGet("usage")]
    public async Task<ActionResult<LicenseUsageResponse>> GetUsage()
    {
        var licenseeId = LicenseeContext.GetRequiredLicenseeId();
        var licensee = await _licenseeService.GetByIdAsync(licenseeId);

        if (licensee == null)
        {
            return NotFound(new { message = "Licensee not found" });
        }

        // TODO: In a real implementation, these would come from actual usage tracking
        // For now, return placeholder values
        var response = new LicenseUsageResponse
        {
            CurrentUsers = 0,  // Would query from auth service
            MaxUsers = licensee.Features.MaxUsers,
            CurrentEntities = 0,  // Would query from ontology service
            MaxEntities = licensee.Features.MaxEntities,
            ApiCallsThisHour = 0,  // Would query from rate limiter
            ApiRateLimit = licensee.Features.ApiRateLimit,
            StorageUsedGb = 0,  // Would query from storage service
            StorageLimitGb = licensee.Features.StorageLimit,
        };

        return Ok(response);
    }

    private static string GetLicenseType(Licensee licensee)
    {
        if (licensee.Features.MultiDomain && licensee.Features.AiPlatform)
            return "Enterprise";
        if (licensee.Features.CustomBranding || licensee.Features.SsoEnabled)
            return "Professional";
        if (licensee.Status == LicenseeStatus.Trial)
            return "Trial";
        return "Standard";
    }
}

/// <summary>
/// License information response DTO
/// </summary>
public class LicenseInfoResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string LicenseType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public bool IsExpired { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactName { get; set; }
    public string? CompanyName { get; set; }
    public LicenseFeaturesResponse Features { get; set; } = new();
    public LicenseLimitsResponse Limits { get; set; } = new();
    public List<string> AllowedDomains { get; set; } = new();
}

public class LicenseFeaturesResponse
{
    public bool MultiDomain { get; set; }
    public bool AiPlatform { get; set; }
    public bool ApiAccess { get; set; }
    public bool CustomBranding { get; set; }
    public bool SsoEnabled { get; set; }
    public bool WebhooksEnabled { get; set; }
    public bool AdvancedAnalytics { get; set; }
    public bool DataExport { get; set; }
}

public class LicenseLimitsResponse
{
    public int MaxUsers { get; set; }
    public int MaxEntities { get; set; }
    public int ApiRateLimit { get; set; }
    public int StorageLimit { get; set; }
}

/// <summary>
/// License usage response DTO
/// </summary>
public class LicenseUsageResponse
{
    public int CurrentUsers { get; set; }
    public int MaxUsers { get; set; }
    public int CurrentEntities { get; set; }
    public int MaxEntities { get; set; }
    public int ApiCallsThisHour { get; set; }
    public int ApiRateLimit { get; set; }
    public double StorageUsedGb { get; set; }
    public int StorageLimitGb { get; set; }
}
