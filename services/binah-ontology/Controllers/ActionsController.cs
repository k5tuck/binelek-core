using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Binah.Ontology.Models.Action;
using Binah.Ontology.Services;
using Binah.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Binah.Ontology.Controllers;

/// <summary>
/// REST API controller for managing workflow actions
/// </summary>
[ApiController]
[Route("api/ontology/actions")]
[Produces("application/json")]
[Authorize]
public class ActionsController : ControllerBase
{
    private readonly IActionService _actionService;
    private readonly ILogger<ActionsController> _logger;

    public ActionsController(
        IActionService actionService,
        ILogger<ActionsController> logger)
    {
        _actionService = actionService ?? throw new ArgumentNullException(nameof(actionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new action
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ActionResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<ActionResponse>>> CreateAction([FromBody] CreateActionRequest request)
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

        _logger.LogInformation("Creating action {ActionName} for tenant {TenantId}", request.Name, tenantId);

        try
        {
            var action = await _actionService.CreateActionAsync(request, tenantId, userId);

            return CreatedAtAction(
                nameof(GetActionById),
                new { id = action.Id },
                ApiResponse<ActionResponse>.Ok(action)
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
            _logger.LogError(ex, "Failed to create action");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while creating the action",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Retrieves an action by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ActionResponse>> GetActionById(string id)
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
            var action = await _actionService.GetActionByIdAsync(id, tenantId);

            if (action == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Action Not Found",
                    Detail = $"Action with ID '{id}' was not found",
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                });
            }

            return Ok(action);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving action {ActionId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while retrieving the action",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Retrieves all actions for the tenant
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<ActionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ActionResponse>>> GetActions(
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 100,
        [FromQuery] ActionStatus? status = null)
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
            var actions = await _actionService.GetActionsAsync(tenantId, skip, limit, status);
            return Ok(actions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving actions");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while retrieving actions",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Updates an existing action
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ActionResponse>> UpdateAction(string id, [FromBody] UpdateActionRequest request)
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
            var action = await _actionService.UpdateActionAsync(id, request, tenantId, userId);

            if (action == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Action Not Found",
                    Detail = $"Action with ID '{id}' was not found",
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                });
            }

            return Ok(action);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating action {ActionId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while updating the action",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Deletes an action
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAction(string id)
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
            var result = await _actionService.DeleteActionAsync(id, tenantId, userId);

            if (!result)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Action Not Found",
                    Detail = $"Action with ID '{id}' was not found",
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting action {ActionId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while deleting the action",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Manually runs an action
    /// </summary>
    [HttpPost("{id}/run")]
    [ProducesResponseType(typeof(ActionRunResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ActionRunResponse>> RunAction(string id, [FromBody] RunActionRequest? request = null)
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
            var run = await _actionService.RunActionAsync(id, tenantId, userId, request?.InputData);

            if (run == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Action Not Found",
                    Detail = $"Action with ID '{id}' was not found",
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                });
            }

            return Ok(run);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Cannot Run Action",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running action {ActionId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while running the action",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Pauses an action
    /// </summary>
    [HttpPost("{id}/pause")]
    [ProducesResponseType(typeof(ActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ActionResponse>> PauseAction(string id)
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
            var action = await _actionService.PauseActionAsync(id, tenantId, userId);

            if (action == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Action Not Found",
                    Detail = $"Action with ID '{id}' was not found",
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                });
            }

            return Ok(action);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing action {ActionId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while pausing the action",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Resumes a paused action
    /// </summary>
    [HttpPost("{id}/resume")]
    [ProducesResponseType(typeof(ActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ActionResponse>> ResumeAction(string id)
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
            var action = await _actionService.ResumeActionAsync(id, tenantId, userId);

            if (action == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Action Not Found",
                    Detail = $"Action with ID '{id}' was not found",
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                });
            }

            return Ok(action);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming action {ActionId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while resuming the action",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Gets execution history for an action
    /// </summary>
    [HttpGet("{id}/runs")]
    [ProducesResponseType(typeof(List<ActionRunResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ActionRunResponse>>> GetActionRuns(
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
            var runs = await _actionService.GetActionRunsAsync(id, tenantId, skip, limit);
            return Ok(runs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving runs for action {ActionId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while retrieving action runs",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }
}
