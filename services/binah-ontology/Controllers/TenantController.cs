using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Binah.Ontology.Models.DTOs;
using Binah.Ontology.Models.Tenant;
using Binah.Ontology.Repositories.Interfaces;
using Binah.Ontology.Pipelines.DataNetwork;

namespace Binah.Ontology.Controllers;

/// <summary>
/// REST API controller for tenant management and data network consent
/// </summary>
[ApiController]
[Route("api/tenants")]
[Produces("application/json")]
[Authorize]
public class TenantController : ControllerBase
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ILogger<TenantController> _logger;

    // Finance domain entity types - can be made dynamic later
    private static readonly string[] FinanceEntityTypes = new[]
    {
        "Client",
        "Account",
        "Transaction",
        "Holding",
        "Goal",
        "Household",
        "Advisor"
    };

    public TenantController(
        ITenantRepository tenantRepository,
        ILogger<TenantController> logger)
    {
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get tenant's data network consent settings
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <returns>Current consent settings</returns>
    /// <response code="200">Consent settings retrieved successfully</response>
    /// <response code="404">Tenant not found</response>
    [HttpGet("{tenantId}/data-network-consent")]
    [ProducesResponseType(typeof(DataNetworkConsentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DataNetworkConsentResponse>> GetDataNetworkConsent(string tenantId)
    {
        try
        {
            // Validate tenant access
            var jwtTenantId = User.FindFirst("tenant_id")?.Value;

            if (string.IsNullOrEmpty(jwtTenantId))
            {
                return Unauthorized(new ProblemDetails
                {
                    Title = "Unauthorized",
                    Detail = "Tenant ID not found in token",
                    Status = StatusCodes.Status401Unauthorized,
                    Instance = HttpContext.Request.Path
                });
            }

            // Ensure route tenant matches JWT tenant
            if (tenantId != jwtTenantId)
            {
                return Forbid();
            }

            _logger.LogInformation("Retrieving data network consent for tenant {TenantId}", tenantId);

            var tenant = await _tenantRepository.GetByIdAsync(tenantId);

            if (tenant == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Tenant Not Found",
                    Detail = $"Tenant with ID '{tenantId}' was not found",
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                });
            }

            var response = DataNetworkConsentResponse.FromTenant(tenant);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving data network consent for tenant {TenantId}", tenantId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while retrieving consent settings",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Update tenant's data network consent settings
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="request">Updated consent settings</param>
    /// <returns>Updated consent settings</returns>
    /// <response code="200">Consent settings updated successfully</response>
    /// <response code="404">Tenant not found</response>
    /// <response code="400">Invalid request data</response>
    [HttpPut("{tenantId}/data-network-consent")]
    [ProducesResponseType(typeof(DataNetworkConsentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DataNetworkConsentResponse>> UpdateDataNetworkConsent(
        string tenantId,
        [FromBody] UpdateConsentRequest request)
    {
        try
        {
            // Validate tenant access
            var jwtTenantId = User.FindFirst("tenant_id")?.Value;

            if (string.IsNullOrEmpty(jwtTenantId))
            {
                return Unauthorized(new ProblemDetails
                {
                    Title = "Unauthorized",
                    Detail = "Tenant ID not found in token",
                    Status = StatusCodes.Status401Unauthorized,
                    Instance = HttpContext.Request.Path
                });
            }

            // Ensure route tenant matches JWT tenant
            if (tenantId != jwtTenantId)
            {
                return Forbid();
            }

            _logger.LogInformation("Updating data network consent for tenant {TenantId}", tenantId);

            var tenant = await _tenantRepository.GetByIdAsync(tenantId);

            if (tenant == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Tenant Not Found",
                    Detail = $"Tenant with ID '{tenantId}' was not found",
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                });
            }

            // Parse scrubbing level
            if (!Enum.TryParse<ScrubbingLevel>(request.PiiScrubbingLevel, out var scrubbingLevel))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Scrubbing Level",
                    Detail = $"Invalid PII scrubbing level: {request.PiiScrubbingLevel}. Must be Strict, Moderate, or Minimal.",
                    Status = StatusCodes.Status400BadRequest,
                    Instance = HttpContext.Request.Path
                });
            }

            // Update tenant consent settings
            var consentChanged = tenant.DataNetworkConsent != request.DataNetworkConsent;
            tenant.DataNetworkConsent = request.DataNetworkConsent;
            tenant.PiiScrubbingLevel = scrubbingLevel;
            tenant.DataNetworkCategories = request.DataNetworkCategories ?? new List<string>();
            tenant.UpdatedAt = DateTime.UtcNow;

            // Update consent date if consent was just granted
            if (consentChanged && request.DataNetworkConsent)
            {
                tenant.DataNetworkConsentDate = DateTime.UtcNow;
                _logger.LogInformation("Tenant {TenantId} granted data network consent", tenantId);
            }
            else if (consentChanged && !request.DataNetworkConsent)
            {
                tenant.DataNetworkConsentDate = null;
                _logger.LogInformation("Tenant {TenantId} revoked data network consent", tenantId);
            }

            var updated = await _tenantRepository.UpdateAsync(tenant);
            var response = DataNetworkConsentResponse.FromTenant(updated);

            _logger.LogInformation(
                "Data network consent updated for tenant {TenantId} (consent: {Consent}, level: {Level})",
                tenantId, request.DataNetworkConsent, request.PiiScrubbingLevel);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating data network consent for tenant {TenantId}", tenantId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while updating consent settings",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Get available entity types for data network contribution
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <returns>List of available entity types</returns>
    /// <response code="200">Entity types retrieved successfully</response>
    /// <response code="404">Tenant not found</response>
    [HttpGet("{tenantId}/entity-types")]
    [ProducesResponseType(typeof(string[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<string[]>> GetAvailableEntityTypes(string tenantId)
    {
        try
        {
            // Validate tenant access
            var jwtTenantId = User.FindFirst("tenant_id")?.Value;

            if (string.IsNullOrEmpty(jwtTenantId))
            {
                return Unauthorized(new ProblemDetails
                {
                    Title = "Unauthorized",
                    Detail = "Tenant ID not found in token",
                    Status = StatusCodes.Status401Unauthorized,
                    Instance = HttpContext.Request.Path
                });
            }

            // Ensure route tenant matches JWT tenant
            if (tenantId != jwtTenantId)
            {
                return Forbid();
            }

            _logger.LogDebug("Retrieving available entity types for tenant {TenantId}", tenantId);

            // Verify tenant exists
            var tenant = await _tenantRepository.GetByIdAsync(tenantId);

            if (tenant == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Tenant Not Found",
                    Detail = $"Tenant with ID '{tenantId}' was not found",
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                });
            }

            // Return available entity types for Finance domain
            // TODO: Make this dynamic based on loaded ontology definitions
            return Ok(FinanceEntityTypes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving entity types for tenant {TenantId}", tenantId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while retrieving entity types",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }
}
