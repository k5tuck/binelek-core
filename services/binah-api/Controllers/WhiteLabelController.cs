using Binah.API.Models;
using Binah.API.Services;
using Binah.Infrastructure.MultiTenancy;
using Microsoft.AspNetCore.Mvc;

namespace Binah.API.Controllers;

[ApiController]
[Route("api/whitelabel")]
public class WhiteLabelController : ControllerBase
{
    private readonly IWhiteLabelConfigService _service;
    private readonly ILogger<WhiteLabelController> _logger;

    public WhiteLabelController(IWhiteLabelConfigService service, ILogger<WhiteLabelController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("config")]
    public async Task<ActionResult<WhiteLabelConfig>> GetConfig()
    {
        var licenseeId = LicenseeContext.GetRequiredLicenseeId();
        var config = await _service.GetByLicenseeIdAsync(licenseeId);

        if (config == null)
        {
            return NotFound(new { message = "White-label configuration not found for this licensee" });
        }

        return Ok(config);
    }

    [HttpPut("config")]
    public async Task<ActionResult<WhiteLabelConfig>> UpdateConfig([FromBody] UpdateWhiteLabelConfigRequest request)
    {
        var licenseeId = LicenseeContext.GetRequiredLicenseeId();
        var existing = await _service.GetByLicenseeIdAsync(licenseeId);

        var config = existing ?? new WhiteLabelConfig
        {
            LicenseeId = licenseeId,
            CompanyName = request.CompanyName ?? "My Company",
            Colors = request.Colors ?? ThemeColors.CreateDefault()
        };

        if (request.CompanyName != null) config.CompanyName = request.CompanyName;
        if (request.LogoUrl != null) config.LogoUrl = request.LogoUrl;
        if (request.FaviconUrl != null) config.FaviconUrl = request.FaviconUrl;
        if (request.CustomDomain != null) config.CustomDomain = request.CustomDomain;
        if (request.Colors != null) config.Colors = request.Colors;
        if (request.CustomText != null) config.CustomText = request.CustomText;
        if (request.Fonts != null) config.Fonts = request.Fonts;

        var result = await _service.CreateOrUpdateAsync(config);
        return Ok(result);
    }
}
