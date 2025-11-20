using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Binah.Ontology.Models.Watch;
using Binah.Ontology.Services;
using Binah.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Binah.Ontology.Controllers;

/// <summary>
/// REST API controller for managing entity watches
/// </summary>
[ApiController]
[Route("api/ontology/watches")]
[Produces("application/json")]
[Authorize]
public class WatchesController : ControllerBase
{
    private readonly IWatchService _watchService;
    private readonly ILogger<WatchesController> _logger;

    public WatchesController(
        IWatchService watchService,
        ILogger<WatchesController> logger)
    {
        _watchService = watchService ?? throw new ArgumentNullException(nameof(watchService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new watch
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<WatchResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<WatchResponse>>> CreateWatch([FromBody] CreateWatchRequest request)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "Tenant ID not found in token",
                Status = StatusCodes.Status401Unauthorized,
                Instance = HttpContext.Request.Path
            });
        }

        var userId = User.FindFirst("sub")?.Value;

        _logger.LogInformation("Creating watch {WatchName} for tenant {TenantId}", request.Name, tenantId);

        try
        {
            var watch = await _watchService.CreateWatchAsync(request, tenantId, userId);

            return CreatedAtAction(
                nameof(GetWatchById),
                new { id = watch.Id },
                ApiResponse<WatchResponse>.Ok(watch)
            );
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create watch");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while creating the watch",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Retrieves a watch by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(WatchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WatchResponse>> GetWatchById(string id)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "Tenant ID not found in token",
                Status = StatusCodes.Status401Unauthorized,
                Instance = HttpContext.Request.Path
            });
        }

        try
        {
            var watch = await _watchService.GetWatchByIdAsync(id, tenantId);

            if (watch == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Watch Not Found",
                    Detail = $"Watch with ID '{id}' was not found",
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                });
            }

            return Ok(watch);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving watch {WatchId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while retrieving the watch",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Retrieves all watches for the tenant
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<WatchResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<WatchResponse>>> GetWatches(
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 100,
        [FromQuery] WatchStatus? status = null)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "Tenant ID not found in token",
                Status = StatusCodes.Status401Unauthorized,
                Instance = HttpContext.Request.Path
            });
        }

        try
        {
            var watches = await _watchService.GetWatchesAsync(tenantId, skip, limit, status);
            return Ok(watches);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving watches");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while retrieving watches",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Updates an existing watch
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(WatchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WatchResponse>> UpdateWatch(string id, [FromBody] UpdateWatchRequest request)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "Tenant ID not found in token",
                Status = StatusCodes.Status401Unauthorized,
                Instance = HttpContext.Request.Path
            });
        }

        var userId = User.FindFirst("sub")?.Value;

        try
        {
            var watch = await _watchService.UpdateWatchAsync(id, request, tenantId, userId);

            if (watch == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Watch Not Found",
                    Detail = $"Watch with ID '{id}' was not found",
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                });
            }

            return Ok(watch);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating watch {WatchId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while updating the watch",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Deletes a watch
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteWatch(string id)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "Tenant ID not found in token",
                Status = StatusCodes.Status401Unauthorized,
                Instance = HttpContext.Request.Path
            });
        }

        var userId = User.FindFirst("sub")?.Value;

        try
        {
            var result = await _watchService.DeleteWatchAsync(id, tenantId, userId);

            if (!result)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Watch Not Found",
                    Detail = $"Watch with ID '{id}' was not found",
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting watch {WatchId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while deleting the watch",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Pauses a watch
    /// </summary>
    [HttpPost("{id}/pause")]
    [ProducesResponseType(typeof(WatchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WatchResponse>> PauseWatch(string id)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "Tenant ID not found in token",
                Status = StatusCodes.Status401Unauthorized,
                Instance = HttpContext.Request.Path
            });
        }

        var userId = User.FindFirst("sub")?.Value;

        try
        {
            var watch = await _watchService.PauseWatchAsync(id, tenantId, userId);

            if (watch == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Watch Not Found",
                    Detail = $"Watch with ID '{id}' was not found",
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                });
            }

            return Ok(watch);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing watch {WatchId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while pausing the watch",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Resumes a paused watch
    /// </summary>
    [HttpPost("{id}/resume")]
    [ProducesResponseType(typeof(WatchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WatchResponse>> ResumeWatch(string id)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "Tenant ID not found in token",
                Status = StatusCodes.Status401Unauthorized,
                Instance = HttpContext.Request.Path
            });
        }

        var userId = User.FindFirst("sub")?.Value;

        try
        {
            var watch = await _watchService.ResumeWatchAsync(id, tenantId, userId);

            if (watch == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Watch Not Found",
                    Detail = $"Watch with ID '{id}' was not found",
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                });
            }

            return Ok(watch);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming watch {WatchId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while resuming the watch",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Gets entities being watched
    /// </summary>
    [HttpGet("{id}/entities")]
    [ProducesResponseType(typeof(List<WatchEntityResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<WatchEntityResponse>>> GetWatchEntities(
        string id,
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 100)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "Tenant ID not found in token",
                Status = StatusCodes.Status401Unauthorized,
                Instance = HttpContext.Request.Path
            });
        }

        try
        {
            var entities = await _watchService.GetWatchEntitiesAsync(id, tenantId, skip, limit);
            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving entities for watch {WatchId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while retrieving watch entities",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Adds an entity to the watch
    /// </summary>
    [HttpPost("{id}/entities")]
    [ProducesResponseType(typeof(WatchEntityResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WatchEntityResponse>> AddWatchEntity(string id, [FromBody] AddWatchEntityRequest request)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "Tenant ID not found in token",
                Status = StatusCodes.Status401Unauthorized,
                Instance = HttpContext.Request.Path
            });
        }

        var userId = User.FindFirst("sub")?.Value;

        try
        {
            var entity = await _watchService.AddWatchEntityAsync(id, request, tenantId, userId);

            if (entity == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Watch Not Found",
                    Detail = $"Watch with ID '{id}' was not found",
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                });
            }

            return Created($"/api/ontology/watches/{id}/entities/{entity.Id}", entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding entity to watch {WatchId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while adding entity to watch",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Removes an entity from the watch
    /// </summary>
    [HttpDelete("{id}/entities/{entityId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveWatchEntity(string id, string entityId)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "Tenant ID not found in token",
                Status = StatusCodes.Status401Unauthorized,
                Instance = HttpContext.Request.Path
            });
        }

        try
        {
            var result = await _watchService.RemoveWatchEntityAsync(id, entityId, tenantId);

            if (!result)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Entity Not Found",
                    Detail = $"Entity with ID '{entityId}' was not found in watch '{id}'",
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing entity {EntityId} from watch {WatchId}", entityId, id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while removing entity from watch",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Gets trigger history for a watch
    /// </summary>
    [HttpGet("{id}/triggers")]
    [ProducesResponseType(typeof(List<WatchTriggerResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<WatchTriggerResponse>>> GetWatchTriggers(
        string id,
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 100)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "Tenant ID not found in token",
                Status = StatusCodes.Status401Unauthorized,
                Instance = HttpContext.Request.Path
            });
        }

        try
        {
            var triggers = await _watchService.GetWatchTriggersAsync(id, tenantId, skip, limit);
            return Ok(triggers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving triggers for watch {WatchId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while retrieving watch triggers",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }
}
