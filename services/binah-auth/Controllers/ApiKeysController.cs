using Binah.Auth.Models;
using Binah.Auth.Services;
using Binah.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Binah.Auth.Controllers;

/// <summary>
/// Controller for API key management
/// </summary>
[ApiController]
[Route("api/keys")]
[Authorize]
public class ApiKeysController : ControllerBase
{
    private readonly IApiKeyService _apiKeyService;
    private readonly ILogger<ApiKeysController> _logger;

    public ApiKeysController(IApiKeyService apiKeyService, ILogger<ApiKeysController> logger)
    {
        _apiKeyService = apiKeyService ?? throw new ArgumentNullException(nameof(apiKeyService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private string GetTenantId() => User.FindFirst("tenant_id")?.Value ?? "system";
    private string GetUserId() => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

    /// <summary>
    /// Create a new API key (returns plaintext key once)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ApiKeyCreateResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<ApiKeyCreateResponse>>> CreateKey([FromBody] CreateApiKeyRequest request)
    {
        try
        {
            // Validate scopes
            foreach (var scope in request.Scopes)
            {
                if (!ApiKeyScopes.All.Contains(scope))
                {
                    return BadRequest(new ProblemDetails
                    {
                        Title = "Invalid scope",
                        Detail = $"Unknown scope: {scope}",
                        Status = StatusCodes.Status400BadRequest
                    });
                }
            }

            var response = await _apiKeyService.CreateKeyAsync(
                GetTenantId(),
                GetUserId(),
                request.Name,
                request.Scopes,
                request.Environment,
                request.ExpiresAt
            );

            _logger.LogInformation("Created API key {KeyId} for tenant {TenantId}", response.Key.Id, GetTenantId());

            return CreatedAtAction(nameof(GetKey), new { keyId = response.Key.Id }, ApiResponse<ApiKeyCreateResponse>.Ok(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create API key");
            return BadRequest(new ProblemDetails
            {
                Title = "Failed to create API key",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    /// <summary>
    /// List all API keys for the tenant
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<ApiKeyDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<ApiKeyDto>>>> ListKeys()
    {
        var keys = await _apiKeyService.ListKeysAsync(GetTenantId());
        return Ok(ApiResponse<List<ApiKeyDto>>.Ok(keys));
    }

    /// <summary>
    /// Get a specific API key
    /// </summary>
    [HttpGet("{keyId}")]
    [ProducesResponseType(typeof(ApiResponse<ApiKeyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ApiKeyDto>>> GetKey(string keyId)
    {
        var key = await _apiKeyService.GetKeyAsync(GetTenantId(), keyId);
        if (key == null)
            return NotFound();

        return Ok(ApiResponse<ApiKeyDto>.Ok(key));
    }

    /// <summary>
    /// Update an API key
    /// </summary>
    [HttpPut("{keyId}")]
    [ProducesResponseType(typeof(ApiResponse<ApiKeyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ApiKeyDto>>> UpdateKey(string keyId, [FromBody] UpdateApiKeyRequest request)
    {
        var key = await _apiKeyService.UpdateKeyAsync(
            GetTenantId(),
            keyId,
            request.Name,
            request.Scopes,
            request.ExpiresAt
        );

        if (key == null)
            return NotFound();

        return Ok(ApiResponse<ApiKeyDto>.Ok(key));
    }

    /// <summary>
    /// Revoke an API key
    /// </summary>
    [HttpDelete("{keyId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeKey(string keyId, [FromQuery] string? reason = null)
    {
        var result = await _apiKeyService.RevokeKeyAsync(GetTenantId(), keyId, GetUserId(), reason);
        if (!result)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Rotate an API key (create new, revoke old)
    /// </summary>
    [HttpPost("{keyId}/rotate")]
    [ProducesResponseType(typeof(ApiResponse<ApiKeyCreateResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ApiKeyCreateResponse>>> RotateKey(string keyId)
    {
        try
        {
            var response = await _apiKeyService.RotateKeyAsync(GetTenantId(), keyId, GetUserId());
            return Ok(ApiResponse<ApiKeyCreateResponse>.Ok(response));
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Get usage statistics for an API key
    /// </summary>
    [HttpGet("{keyId}/usage")]
    [ProducesResponseType(typeof(ApiResponse<ApiKeyUsageStats>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ApiKeyUsageStats>>> GetKeyUsage(
        string keyId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var key = await _apiKeyService.GetKeyAsync(GetTenantId(), keyId);
        if (key == null)
            return NotFound();

        var stats = await _apiKeyService.GetUsageStatsAsync(GetTenantId(), keyId, startDate, endDate);
        return Ok(ApiResponse<ApiKeyUsageStats>.Ok(stats));
    }

    /// <summary>
    /// Get available scopes
    /// </summary>
    [HttpGet("scopes")]
    [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<List<string>>> GetAvailableScopes()
    {
        return Ok(ApiResponse<List<string>>.Ok(ApiKeyScopes.All));
    }
}

#region Request DTOs

public class CreateApiKeyRequest
{
    public string Name { get; set; } = string.Empty;
    public List<string> Scopes { get; set; } = new();
    public string Environment { get; set; } = "live";
    public DateTime? ExpiresAt { get; set; }
}

public class UpdateApiKeyRequest
{
    public string? Name { get; set; }
    public List<string>? Scopes { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

#endregion
