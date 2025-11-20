using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Binah.Ontology.Models;
using Binah.Ontology.Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Binah.Ontology.Controllers;

/// <summary>
/// REST API controller for managing relationship canvases
/// </summary>
[ApiController]
[Route("api/tenants/{tenantId}/canvases")]
[Produces("application/json")]
[Authorize]
public class CanvasController : ControllerBase
{
    private readonly ICanvasRepository _canvasRepository;
    private readonly ILogger<CanvasController> _logger;

    public CanvasController(
        ICanvasRepository canvasRepository,
        ILogger<CanvasController> logger)
    {
        _canvasRepository = canvasRepository ?? throw new ArgumentNullException(nameof(canvasRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Lists all canvases for a tenant
    /// </summary>
    /// <param name="tenantId">Tenant ID from route</param>
    /// <param name="skip">Number of records to skip</param>
    /// <param name="limit">Maximum number of records to return</param>
    /// <returns>List of canvases</returns>
    /// <response code="200">Canvases retrieved successfully</response>
    /// <response code="401">Unauthorized - missing or invalid token</response>
    /// <response code="403">Forbidden - tenant ID mismatch</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<Canvas>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<Canvas>>> ListCanvases(
        Guid tenantId,
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 100)
    {
        try
        {
            // Validate tenant from JWT
            if (!ValidateTenantAccess(tenantId, out var errorResult))
            {
                return errorResult!;
            }

            _logger.LogInformation("Listing canvases for tenant {TenantId}", tenantId);

            var canvases = await _canvasRepository.GetByTenantAsync(tenantId, skip, limit);

            return Ok(canvases);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing canvases for tenant {TenantId}", tenantId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while listing canvases",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Retrieves a canvas by ID
    /// </summary>
    /// <param name="tenantId">Tenant ID from route</param>
    /// <param name="id">Canvas ID</param>
    /// <returns>Canvas if found</returns>
    /// <response code="200">Canvas retrieved successfully</response>
    /// <response code="404">Canvas not found</response>
    /// <response code="401">Unauthorized - missing or invalid token</response>
    /// <response code="403">Forbidden - tenant ID mismatch</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Canvas), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Canvas>> GetCanvas(Guid tenantId, Guid id)
    {
        try
        {
            // Validate tenant from JWT
            if (!ValidateTenantAccess(tenantId, out var errorResult))
            {
                return errorResult!;
            }

            _logger.LogInformation("Getting canvas {CanvasId} for tenant {TenantId}", id, tenantId);

            var canvas = await _canvasRepository.GetByIdAsync(id, tenantId);

            if (canvas == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Canvas Not Found",
                    Detail = $"Canvas with ID '{id}' was not found",
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                });
            }

            return Ok(canvas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving canvas {CanvasId} for tenant {TenantId}", id, tenantId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while retrieving the canvas",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Creates a new canvas
    /// </summary>
    /// <param name="tenantId">Tenant ID from route</param>
    /// <param name="request">Canvas creation request</param>
    /// <returns>Created canvas with generated ID</returns>
    /// <response code="201">Canvas created successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Unauthorized - missing or invalid token</response>
    /// <response code="403">Forbidden - tenant ID mismatch</response>
    [HttpPost]
    [ProducesResponseType(typeof(Canvas), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Canvas>> CreateCanvas(Guid tenantId, [FromBody] CreateCanvasRequest request)
    {
        try
        {
            // Validate tenant from JWT
            if (!ValidateTenantAccess(tenantId, out var errorResult))
            {
                return errorResult!;
            }

            // Get user ID from JWT
            var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst("user_id")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new ProblemDetails
                {
                    Title = "Unauthorized",
                    Detail = "User ID not found in token",
                    Status = StatusCodes.Status401Unauthorized,
                    Instance = HttpContext.Request.Path
                });
            }

            // Validate request
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Request",
                    Detail = "Canvas name is required",
                    Status = StatusCodes.Status400BadRequest,
                    Instance = HttpContext.Request.Path
                });
            }

            _logger.LogInformation("Creating canvas '{CanvasName}' for tenant {TenantId} by user {UserId}",
                request.Name, tenantId, userId);

            var canvas = new Canvas
            {
                TenantId = tenantId,
                Name = request.Name,
                Description = request.Description,
                Entities = request.Entities ?? new List<CanvasEntity>(),
                Connections = request.Connections ?? new List<CanvasConnection>(),
                Viewport = request.Viewport ?? new CanvasViewport(),
                IsShared = request.IsShared,
                SharedWith = request.SharedWith ?? new List<Guid>(),
                CreatedBy = userId
            };

            var created = await _canvasRepository.CreateAsync(canvas);

            _logger.LogInformation("Canvas {CanvasId} created successfully", created.Id);

            return CreatedAtAction(
                nameof(GetCanvas),
                new { tenantId = tenantId, id = created.Id },
                created
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating canvas for tenant {TenantId}", tenantId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while creating the canvas",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Updates an existing canvas
    /// </summary>
    /// <param name="tenantId">Tenant ID from route</param>
    /// <param name="id">Canvas ID</param>
    /// <param name="request">Canvas update request</param>
    /// <returns>Updated canvas</returns>
    /// <response code="200">Canvas updated successfully</response>
    /// <response code="404">Canvas not found</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Unauthorized - missing or invalid token</response>
    /// <response code="403">Forbidden - tenant ID mismatch or not owner</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Canvas), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Canvas>> UpdateCanvas(
        Guid tenantId,
        Guid id,
        [FromBody] UpdateCanvasRequest request)
    {
        try
        {
            // Validate tenant from JWT
            if (!ValidateTenantAccess(tenantId, out var errorResult))
            {
                return errorResult!;
            }

            // Get user ID from JWT
            var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst("user_id")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new ProblemDetails
                {
                    Title = "Unauthorized",
                    Detail = "User ID not found in token",
                    Status = StatusCodes.Status401Unauthorized,
                    Instance = HttpContext.Request.Path
                });
            }

            // Check if canvas exists
            var existing = await _canvasRepository.GetByIdAsync(id, tenantId);
            if (existing == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Canvas Not Found",
                    Detail = $"Canvas with ID '{id}' was not found",
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                });
            }

            // Check ownership or shared access
            if (existing.CreatedBy != userId && !existing.SharedWith.Contains(userId))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
                {
                    Title = "Forbidden",
                    Detail = "You do not have permission to update this canvas",
                    Status = StatusCodes.Status403Forbidden,
                    Instance = HttpContext.Request.Path
                });
            }

            _logger.LogInformation("Updating canvas {CanvasId} for tenant {TenantId}", id, tenantId);

            // Update canvas properties
            existing.Name = request.Name ?? existing.Name;
            existing.Description = request.Description ?? existing.Description;
            existing.Entities = request.Entities ?? existing.Entities;
            existing.Connections = request.Connections ?? existing.Connections;
            existing.Viewport = request.Viewport ?? existing.Viewport;

            // Only owner can change sharing settings
            if (existing.CreatedBy == userId)
            {
                if (request.IsShared.HasValue)
                    existing.IsShared = request.IsShared.Value;
                if (request.SharedWith != null)
                    existing.SharedWith = request.SharedWith;
            }

            var updated = await _canvasRepository.UpdateAsync(existing);

            _logger.LogInformation("Canvas {CanvasId} updated successfully", id);

            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating canvas {CanvasId} for tenant {TenantId}", id, tenantId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while updating the canvas",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Deletes a canvas
    /// </summary>
    /// <param name="tenantId">Tenant ID from route</param>
    /// <param name="id">Canvas ID</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Canvas deleted successfully</response>
    /// <response code="404">Canvas not found</response>
    /// <response code="401">Unauthorized - missing or invalid token</response>
    /// <response code="403">Forbidden - tenant ID mismatch or not owner</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteCanvas(Guid tenantId, Guid id)
    {
        try
        {
            // Validate tenant from JWT
            if (!ValidateTenantAccess(tenantId, out var errorResult))
            {
                return errorResult!;
            }

            // Get user ID from JWT
            var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst("user_id")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new ProblemDetails
                {
                    Title = "Unauthorized",
                    Detail = "User ID not found in token",
                    Status = StatusCodes.Status401Unauthorized,
                    Instance = HttpContext.Request.Path
                });
            }

            // Check if canvas exists
            var existing = await _canvasRepository.GetByIdAsync(id, tenantId);
            if (existing == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Canvas Not Found",
                    Detail = $"Canvas with ID '{id}' was not found",
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                });
            }

            // Only owner can delete
            if (existing.CreatedBy != userId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
                {
                    Title = "Forbidden",
                    Detail = "Only the canvas owner can delete this canvas",
                    Status = StatusCodes.Status403Forbidden,
                    Instance = HttpContext.Request.Path
                });
            }

            _logger.LogInformation("Deleting canvas {CanvasId} for tenant {TenantId}", id, tenantId);

            var result = await _canvasRepository.DeleteAsync(id, tenantId);

            if (!result)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Canvas Not Found",
                    Detail = $"Canvas with ID '{id}' was not found",
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                });
            }

            _logger.LogInformation("Canvas {CanvasId} deleted successfully", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting canvas {CanvasId} for tenant {TenantId}", id, tenantId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while deleting the canvas",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Validates that the tenant ID from the route matches the JWT claim
    /// </summary>
    private bool ValidateTenantAccess(Guid routeTenantId, out ActionResult? errorResult)
    {
        errorResult = null;

        var tenantIdClaim = User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(tenantIdClaim))
        {
            errorResult = Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "Tenant ID not found in token",
                Status = StatusCodes.Status401Unauthorized,
                Instance = HttpContext.Request.Path
            });
            return false;
        }

        if (!Guid.TryParse(tenantIdClaim, out var jwtTenantId))
        {
            errorResult = Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "Invalid tenant ID format in token",
                Status = StatusCodes.Status401Unauthorized,
                Instance = HttpContext.Request.Path
            });
            return false;
        }

        if (routeTenantId != jwtTenantId)
        {
            errorResult = StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Forbidden",
                Detail = "Tenant ID mismatch - you can only access your own tenant's resources",
                Status = StatusCodes.Status403Forbidden,
                Instance = HttpContext.Request.Path
            });
            return false;
        }

        return true;
    }
}

/// <summary>
/// Request model for creating a canvas
/// </summary>
public class CreateCanvasRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<CanvasEntity>? Entities { get; set; }
    public List<CanvasConnection>? Connections { get; set; }
    public CanvasViewport? Viewport { get; set; }
    public bool IsShared { get; set; } = false;
    public List<Guid>? SharedWith { get; set; }
}

/// <summary>
/// Request model for updating a canvas
/// </summary>
public class UpdateCanvasRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<CanvasEntity>? Entities { get; set; }
    public List<CanvasConnection>? Connections { get; set; }
    public CanvasViewport? Viewport { get; set; }
    public bool? IsShared { get; set; }
    public List<Guid>? SharedWith { get; set; }
}
