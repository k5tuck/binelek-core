using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Binah.Ontology.Models.Base;
using Binah.Ontology.Services;
using Binah.Ontology.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Binah.Contracts.DTOs.Ontology;
using Binah.Core.Exceptions;
using Binah.Contracts.Common;

namespace Binah.Ontology.Controllers;

/// <summary>
/// REST API controller for managing ontology entities
/// </summary>
[ApiController]
[Route("api/ontology/entities")]
[Produces("application/json")]
[Authorize]
public class EntitiesController : ControllerBase
{
    private readonly IEntityService _entityService;
    private readonly ILogger<EntitiesController> _logger;

    public EntitiesController(
        IEntityService entityService,
        ILogger<EntitiesController> logger)
    {
        _entityService = entityService ?? throw new ArgumentNullException(nameof(entityService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new entity in the ontology graph
    /// </summary>
    /// <param name="request">Entity creation request</param>
    /// <returns>Created entity with generated ID</returns>
    /// <response code="201">Entity created successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="500">Internal server error</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Entity>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<Entity>>> CreateEntity([FromBody] CreateEntityRequest request)
    {
        // Extract tenant_id from JWT claims
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

        // CRITICAL: Prevent tenant ID forgery
        if (!string.IsNullOrEmpty(request.TenantId) && request.TenantId != tenantId)
        {
            return Forbid();
        }

        _logger.LogInformation("Creating entity of type {EntityType} for tenant {TenantId}", request.Type, tenantId);

        var entity = await _entityService.CreateEntityAsync(
            request.Type,
            request.Properties,
            request.CreatedBy,
            tenantId
        );

        _logger.LogInformation("Entity {EntityId} created successfully", entity.Id);

        return CreatedAtAction(
            nameof(GetEntityById),
            new { id = entity.Id },
            ApiResponse<Entity>.Ok(entity)
        );
    }

    /// <summary>
    /// Retrieves a single entity by ID
    /// </summary>
    /// <param name="id">Entity ID</param>
    /// <returns>Entity if found</returns>
    /// <response code="200">Entity found</response>
    /// <response code="404">Entity not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Entity), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Entity>> GetEntityById(string id)
    {
        try
        {
            // Extract tenant_id from JWT claims
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

            var entity = await _entityService.GetEntityByIdAsync(id);

            if (entity == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Entity Not Found",
                    Detail = $"Entity with ID '{id}' was not found",
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                });
            }

            // Verify entity belongs to tenant
            if (entity.TenantId != tenantId && entity.TenantId != "core")
            {
                return Forbid();
            }

            return Ok(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving entity {EntityId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while retrieving the entity",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Retrieves entities by type with pagination
    /// </summary>
    /// <param name="type">Entity type filter</param>
    /// <param name="skip">Number of records to skip</param>
    /// <param name="limit">Maximum number of records to return</param>
    /// <returns>List of entities</returns>
    /// <response code="200">Entities retrieved successfully</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<Entity>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Entity>>> GetEntitiesByType(
        [FromQuery] string? type = null,
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 100)
    {
        try
        {
            // Extract tenant_id from JWT claims
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

            if (string.IsNullOrWhiteSpace(type))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Request",
                    Detail = "Entity type parameter is required",
                    Status = StatusCodes.Status400BadRequest,
                    Instance = HttpContext.Request.Path
                });
            }

            var entities = await _entityService.GetEntitiesByTypeAsync(type, skip, limit);

            // Filter by tenant (including "core" entities)
            var filteredEntities = entities.Where(e =>
                e.TenantId == tenantId || e.TenantId == "core"
            ).ToList();

            return Ok(filteredEntities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving entities of type {EntityType}", type);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while retrieving entities",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Updates an existing entity
    /// </summary>
    /// <param name="id">Entity ID</param>
    /// <param name="request">Update request</param>
    /// <returns>Updated entity</returns>
    /// <response code="200">Entity updated successfully</response>
    /// <response code="404">Entity not found</response>
    /// <response code="400">Invalid request data</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Entity), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Entity>> UpdateEntity(
        string id,
        [FromBody] UpdateEntityRequest request)
    {
        try
        {
            // Extract tenant_id from JWT claims
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

            // First check if entity exists and belongs to tenant
            var existingEntity = await _entityService.GetEntityByIdAsync(id);
            if (existingEntity == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Entity Not Found",
                    Detail = $"Entity with ID '{id}' was not found",
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                });
            }

            // Verify entity belongs to tenant (cannot update core entities)
            if (existingEntity.TenantId != tenantId)
            {
                return Forbid();
            }

            _logger.LogInformation("Updating entity {EntityId} for tenant {TenantId}", id, tenantId);

            var entity = await _entityService.UpdateEntityAsync(
                id,
                request.Properties,
                request.UpdatedBy
            );

            _logger.LogInformation("Entity {EntityId} updated successfully", id);

            return Ok(entity);
        }
        catch (EntityNotFoundException ex)
        {
            _logger.LogWarning(ex, "Entity {EntityId} not found", id);
            return NotFound(new ProblemDetails
            {
                Title = "Entity Not Found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }
        catch (EntityUpdateException ex)
        {
            _logger.LogError(ex, "Failed to update entity {EntityId}", id);
            return BadRequest(new ProblemDetails
            {
                Title = "Entity Update Failed",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating entity {EntityId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while updating the entity",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Deletes an entity (soft delete)
    /// </summary>
    /// <param name="id">Entity ID</param>
    /// <param name="deletedBy">User or system identifier</param>
    /// <returns>Success status</returns>
    /// <response code="204">Entity deleted successfully</response>
    /// <response code="404">Entity not found</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEntity(
        string id,
        [FromQuery] string? deletedBy = null)
    {
        try
        {
            // Extract tenant_id from JWT claims
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

            // First check if entity exists and belongs to tenant
            var existingEntity = await _entityService.GetEntityByIdAsync(id);
            if (existingEntity == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Entity Not Found",
                    Detail = $"Entity with ID '{id}' was not found",
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                });
            }

            // Verify entity belongs to tenant (cannot delete core entities)
            if (existingEntity.TenantId != tenantId)
            {
                return Forbid();
            }

            _logger.LogInformation("Deleting entity {EntityId} for tenant {TenantId}", id, tenantId);

            var result = await _entityService.DeleteEntityAsync(id, deletedBy);

            if (!result)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Entity Not Found",
                    Detail = $"Entity with ID '{id}' was not found",
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                });
            }

            _logger.LogInformation("Entity {EntityId} deleted successfully", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting entity {EntityId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while deleting the entity",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Searches entities using full-text search
    /// </summary>
    /// <param name="q">Search query</param>
    /// <param name="type">Optional entity type filter</param>
    /// <param name="limit">Maximum number of results</param>
    /// <returns>List of matching entities</returns>
    /// <response code="200">Search completed successfully</response>
    [HttpGet("search")]
    [ProducesResponseType(typeof(List<Entity>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Entity>>> SearchEntities(
        [FromQuery] string q,
        [FromQuery] string? type = null,
        [FromQuery] int limit = 50)
    {
        try
        {
            // Extract tenant_id from JWT claims
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

            if (string.IsNullOrWhiteSpace(q))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Request",
                    Detail = "Search query parameter 'q' is required",
                    Status = StatusCodes.Status400BadRequest,
                    Instance = HttpContext.Request.Path
                });
            }

            var entities = await _entityService.SearchEntitiesAsync(q, type, limit);

            // Filter by tenant (including "core" entities)
            var filteredEntities = entities.Where(e =>
                e.TenantId == tenantId || e.TenantId == "core"
            ).ToList();

            return Ok(filteredEntities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching entities with query {Query}", q);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while searching entities",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }
}
