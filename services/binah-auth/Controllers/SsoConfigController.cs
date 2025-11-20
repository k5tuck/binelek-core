using Binah.Auth.Models;
using Binah.Auth.Services;
using Binah.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Binah.Auth.Controllers;

/// <summary>
/// Controller for SSO configuration management
/// </summary>
[ApiController]
[Route("api/sso")]
[Authorize]
public class SsoConfigController : ControllerBase
{
    private readonly ISsoConfigService _ssoConfigService;
    private readonly ILogger<SsoConfigController> _logger;

    public SsoConfigController(ISsoConfigService ssoConfigService, ILogger<SsoConfigController> logger)
    {
        _ssoConfigService = ssoConfigService ?? throw new ArgumentNullException(nameof(ssoConfigService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private string GetTenantId() => User.FindFirst("tenant_id")?.Value ?? "system";

    /// <summary>
    /// Get SSO configuration
    /// </summary>
    [HttpGet("config")]
    [ProducesResponseType(typeof(ApiResponse<SsoConfigDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SsoConfigDto>>> GetConfig()
    {
        var config = await _ssoConfigService.GetConfigAsync(GetTenantId());
        if (config == null)
            return NotFound();

        return Ok(ApiResponse<SsoConfigDto>.Ok(config));
    }

    /// <summary>
    /// Create or update SSO configuration
    /// </summary>
    [HttpPut("config")]
    [ProducesResponseType(typeof(ApiResponse<SsoConfigDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<SsoConfigDto>>> SaveConfig([FromBody] SaveSsoConfigRequest request)
    {
        try
        {
            var config = new SsoConfig
            {
                ProviderType = request.ProviderType,
                Name = request.Name,
                IsEnabled = request.IsEnabled,
                IsRequired = request.IsRequired,

                // SAML
                SamlEntityId = request.SamlEntityId,
                SamlSsoUrl = request.SamlSsoUrl,
                SamlSloUrl = request.SamlSloUrl,
                SamlCertificate = request.SamlCertificate,
                SamlAttributeMapping = request.SamlAttributeMapping,

                // OIDC
                OidcClientId = request.OidcClientId,
                OidcClientSecret = request.OidcClientSecret,
                OidcIssuer = request.OidcIssuer,
                OidcAuthorizationEndpoint = request.OidcAuthorizationEndpoint,
                OidcTokenEndpoint = request.OidcTokenEndpoint,
                OidcUserInfoEndpoint = request.OidcUserInfoEndpoint,
                OidcScopes = request.OidcScopes,
                OidcClaimMapping = request.OidcClaimMapping,

                // Settings
                DefaultRole = request.DefaultRole,
                AutoProvision = request.AutoProvision,
                UpdateOnLogin = request.UpdateOnLogin
            };

            var saved = await _ssoConfigService.SaveConfigAsync(GetTenantId(), config);
            return Ok(ApiResponse<SsoConfigDto>.Ok(saved));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save SSO configuration");
            return BadRequest(new ProblemDetails
            {
                Title = "Failed to save SSO configuration",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    /// <summary>
    /// Delete SSO configuration
    /// </summary>
    [HttpDelete("config")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteConfig()
    {
        var result = await _ssoConfigService.DeleteConfigAsync(GetTenantId());
        if (!result)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Enable or disable SSO
    /// </summary>
    [HttpPatch("config/enabled")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetEnabled([FromBody] SetEnabledRequest request)
    {
        var result = await _ssoConfigService.SetEnabledAsync(GetTenantId(), request.Enabled);
        if (!result)
            return NotFound();

        return Ok(new { message = request.Enabled ? "SSO enabled" : "SSO disabled" });
    }

    /// <summary>
    /// Test SSO connection
    /// </summary>
    [HttpPost("config/test")]
    [ProducesResponseType(typeof(ApiResponse<SsoTestResult>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SsoTestResult>>> TestConnection()
    {
        var result = await _ssoConfigService.TestConnectionAsync(GetTenantId());
        return Ok(ApiResponse<SsoTestResult>.Ok(result));
    }

    #region Domain Management

    /// <summary>
    /// Get all domains
    /// </summary>
    [HttpGet("domains")]
    [ProducesResponseType(typeof(ApiResponse<List<DomainVerification>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<DomainVerification>>>> GetDomains()
    {
        var domains = await _ssoConfigService.GetDomainsAsync(GetTenantId());
        return Ok(ApiResponse<List<DomainVerification>>.Ok(domains));
    }

    /// <summary>
    /// Add a domain for verification
    /// </summary>
    [HttpPost("domains")]
    [ProducesResponseType(typeof(ApiResponse<DomainVerification>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<DomainVerification>>> AddDomain([FromBody] AddDomainRequest request)
    {
        try
        {
            var domain = await _ssoConfigService.AddDomainAsync(GetTenantId(), request.Domain);
            return CreatedAtAction(nameof(GetDomains), ApiResponse<DomainVerification>.Ok(domain));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Failed to add domain",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    /// <summary>
    /// Verify a domain
    /// </summary>
    [HttpPost("domains/{domain}/verify")]
    [ProducesResponseType(typeof(ApiResponse<DomainVerification>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<DomainVerification>>> VerifyDomain(string domain)
    {
        try
        {
            var verification = await _ssoConfigService.VerifyDomainAsync(GetTenantId(), domain);
            return Ok(ApiResponse<DomainVerification>.Ok(verification));
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Remove a domain
    /// </summary>
    [HttpDelete("domains/{domain}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveDomain(string domain)
    {
        var result = await _ssoConfigService.RemoveDomainAsync(GetTenantId(), domain);
        if (!result)
            return NotFound();

        return NoContent();
    }

    #endregion
}

#region Request DTOs

public class SaveSsoConfigRequest
{
    public string ProviderType { get; set; } = "saml";
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = false;
    public bool IsRequired { get; set; } = false;

    // SAML
    public string? SamlEntityId { get; set; }
    public string? SamlSsoUrl { get; set; }
    public string? SamlSloUrl { get; set; }
    public string? SamlCertificate { get; set; }
    public Dictionary<string, string> SamlAttributeMapping { get; set; } = new();

    // OIDC
    public string? OidcClientId { get; set; }
    public string? OidcClientSecret { get; set; }
    public string? OidcIssuer { get; set; }
    public string? OidcAuthorizationEndpoint { get; set; }
    public string? OidcTokenEndpoint { get; set; }
    public string? OidcUserInfoEndpoint { get; set; }
    public List<string> OidcScopes { get; set; } = new() { "openid", "email", "profile" };
    public Dictionary<string, string> OidcClaimMapping { get; set; } = new();

    // Settings
    public string DefaultRole { get; set; } = "User";
    public bool AutoProvision { get; set; } = true;
    public bool UpdateOnLogin { get; set; } = true;
}

public class SetEnabledRequest
{
    public bool Enabled { get; set; }
}

public class AddDomainRequest
{
    public string Domain { get; set; } = string.Empty;
}

#endregion
