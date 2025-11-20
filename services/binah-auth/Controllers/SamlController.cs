using Binah.Auth.Services;
using Binah.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Binah.Auth.Controllers;

/// <summary>
/// Controller for SAML SSO operations
/// </summary>
[ApiController]
[Route("api/auth/saml")]
public class SamlController : ControllerBase
{
    private readonly ISamlService _samlService;
    private readonly ILogger<SamlController> _logger;

    public SamlController(ISamlService samlService, ILogger<SamlController> logger)
    {
        _samlService = samlService ?? throw new ArgumentNullException(nameof(samlService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Configure SAML for a tenant
    /// </summary>
    [HttpPost("configure")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<SamlConfigurationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<SamlConfigurationResponse>>> ConfigureSaml(
        [FromBody] SamlConfigurationRequest request)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(tenantId))
        {
            return BadRequest(ApiResponse<SamlConfigurationResponse>.WithError("Tenant ID not found in token"));
        }

        try
        {
            var config = await _samlService.ConfigureSamlAsync(tenantId, request);

            var response = new SamlConfigurationResponse
            {
                Id = config.Id,
                TenantId = config.TenantId,
                EntityId = config.EntityId,
                SsoUrl = config.SsoUrl,
                Enabled = config.Enabled,
                CreatedAt = config.CreatedAt,
                UpdatedAt = config.UpdatedAt
            };

            return Ok(ApiResponse<SamlConfigurationResponse>.Ok(response));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<SamlConfigurationResponse>.WithError(ex.Message));
        }
    }

    /// <summary>
    /// Get SAML configuration for the tenant
    /// </summary>
    [HttpGet("config")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<SamlConfigurationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SamlConfigurationResponse>>> GetConfig()
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(tenantId))
        {
            return BadRequest(ApiResponse<SamlConfigurationResponse>.WithError("Tenant ID not found in token"));
        }

        var config = await _samlService.GetSamlConfigurationAsync(tenantId);

        if (config == null)
        {
            return NotFound(ApiResponse<SamlConfigurationResponse>.WithError("SAML not configured"));
        }

        var response = new SamlConfigurationResponse
        {
            Id = config.Id,
            TenantId = config.TenantId,
            EntityId = config.EntityId,
            SsoUrl = config.SsoUrl,
            Enabled = config.Enabled,
            CreatedAt = config.CreatedAt,
            UpdatedAt = config.UpdatedAt
        };

        return Ok(ApiResponse<SamlConfigurationResponse>.Ok(response));
    }

    /// <summary>
    /// Update SAML configuration
    /// </summary>
    [HttpPut("configure")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<SamlConfigurationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<SamlConfigurationResponse>>> UpdateConfig(
        [FromBody] SamlConfigurationRequest request)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(tenantId))
        {
            return BadRequest(ApiResponse<SamlConfigurationResponse>.WithError("Tenant ID not found in token"));
        }

        try
        {
            var config = await _samlService.UpdateSamlConfigurationAsync(tenantId, request);

            var response = new SamlConfigurationResponse
            {
                Id = config.Id,
                TenantId = config.TenantId,
                EntityId = config.EntityId,
                SsoUrl = config.SsoUrl,
                Enabled = config.Enabled,
                CreatedAt = config.CreatedAt,
                UpdatedAt = config.UpdatedAt
            };

            return Ok(ApiResponse<SamlConfigurationResponse>.Ok(response));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<SamlConfigurationResponse>.WithError(ex.Message));
        }
    }

    /// <summary>
    /// Disable SAML for the tenant
    /// </summary>
    [HttpDelete("configure")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> DisableSaml()
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(tenantId))
        {
            return BadRequest(ApiResponse<bool>.WithError("Tenant ID not found in token"));
        }

        await _samlService.DisableSamlAsync(tenantId);

        return Ok(ApiResponse<bool>.Ok(true));
    }

    /// <summary>
    /// Initiate SSO login
    /// </summary>
    [HttpPost("sso")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<SsoInitiationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SsoInitiationResponse>>> InitiateSso(
        [FromBody] SsoInitiationRequest request)
    {
        try
        {
            var response = await _samlService.InitiateSsoAsync(request.TenantId, request.ReturnUrl);

            return Ok(ApiResponse<SsoInitiationResponse>.Ok(response));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<SsoInitiationResponse>.WithError(ex.Message));
        }
    }

    /// <summary>
    /// Assertion Consumer Service - handles SAML response from IdP
    /// </summary>
    [HttpPost("acs")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AssertionConsumerService([FromForm] string SAMLResponse, [FromForm] string? RelayState)
    {
        try
        {
            var validationResult = await _samlService.ValidateSamlResponseAsync(SAMLResponse);

            if (!validationResult.IsValid)
            {
                return BadRequest(new { error = validationResult.ErrorMessage });
            }

            // In production, you would:
            // 1. Create or update the user based on SAML attributes
            // 2. Generate JWT token
            // 3. Redirect to the application with the token

            var returnUrl = RelayState ?? "/";
            return Redirect(returnUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing SAML response");
            return BadRequest(new { error = "Failed to process SAML response" });
        }
    }

    /// <summary>
    /// Get Service Provider metadata XML
    /// </summary>
    [HttpGet("metadata")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMetadata([FromQuery] string tenantId)
    {
        try
        {
            var metadata = await _samlService.GetServiceProviderMetadataAsync(tenantId);

            return Content(metadata, "application/samlmetadata+xml");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

/// <summary>
/// SAML configuration response (without sensitive data)
/// </summary>
public class SamlConfigurationResponse
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string SsoUrl { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// SSO initiation request
/// </summary>
public class SsoInitiationRequest
{
    public string TenantId { get; set; } = string.Empty;
    public string? ReturnUrl { get; set; }
}
