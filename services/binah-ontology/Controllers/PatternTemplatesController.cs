using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Binah.Ontology.DTOs;
using Binah.Ontology.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Binah.Ontology.Controllers;

/// <summary>
/// REST API controller for managing pattern templates
/// </summary>
[ApiController]
[Produces("application/json")]
public class PatternTemplatesController : ControllerBase
{
    private readonly IPatternTemplateService _templateService;
    private readonly ILogger<PatternTemplatesController> _logger;

    public PatternTemplatesController(
        IPatternTemplateService templateService,
        ILogger<PatternTemplatesController> logger)
    {
        _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets templates for the current tenant
    /// </summary>
    [HttpGet("api/tenants/{tenantId}/templates")]
    [Authorize]
    [ProducesResponseType(typeof(PaginatedTemplatesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PaginatedTemplatesDto>> GetTemplates(
        string tenantId,
        [FromQuery] TemplateQueryDto query)
    {
        var tokenTenantId = User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(tokenTenantId) || tokenTenantId != tenantId)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "Tenant ID mismatch",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var result = await _templateService.GetTemplatesAsync(tenantId, query);
        return Ok(result);
    }

    /// <summary>
    /// Gets a single template by ID
    /// </summary>
    [HttpGet("api/tenants/{tenantId}/templates/{id}")]
    [Authorize]
    [ProducesResponseType(typeof(PatternTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PatternTemplateDto>> GetTemplate(string tenantId, Guid id)
    {
        var tokenTenantId = User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(tokenTenantId) || tokenTenantId != tenantId)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "Tenant ID mismatch",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var result = await _templateService.GetTemplateByIdAsync(id, tenantId);

        if (result == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Detail = $"Template {id} not found",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Creates a new template
    /// </summary>
    [HttpPost("api/tenants/{tenantId}/templates")]
    [Authorize]
    [ProducesResponseType(typeof(PatternTemplateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PatternTemplateDto>> CreateTemplate(
        string tenantId,
        [FromBody] CreatePatternTemplateDto request)
    {
        var tokenTenantId = User.FindFirst("tenant_id")?.Value;
        var userId = User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(tokenTenantId) || tokenTenantId != tenantId)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "Tenant ID mismatch",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        try
        {
            var result = await _templateService.CreateTemplateAsync(tenantId, request, userId);
            return CreatedAtAction(
                nameof(GetTemplate),
                new { tenantId, id = result.Id },
                result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating template");
            return BadRequest(new ProblemDetails
            {
                Title = "Bad Request",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    /// <summary>
    /// Updates a template
    /// </summary>
    [HttpPut("api/tenants/{tenantId}/templates/{id}")]
    [Authorize]
    [ProducesResponseType(typeof(PatternTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PatternTemplateDto>> UpdateTemplate(
        string tenantId,
        Guid id,
        [FromBody] UpdatePatternTemplateDto request)
    {
        var tokenTenantId = User.FindFirst("tenant_id")?.Value;
        var userId = User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(tokenTenantId) || tokenTenantId != tenantId)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "Tenant ID mismatch",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        try
        {
            var result = await _templateService.UpdateTemplateAsync(id, tenantId, request, userId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// Deletes a template
    /// </summary>
    [HttpDelete("api/tenants/{tenantId}/templates/{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTemplate(string tenantId, Guid id)
    {
        var tokenTenantId = User.FindFirst("tenant_id")?.Value;
        var userId = User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(tokenTenantId) || tokenTenantId != tenantId)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "Tenant ID mismatch",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        try
        {
            var result = await _templateService.DeleteTemplateAsync(id, tenantId, userId);

            if (!result)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Not Found",
                    Detail = $"Template {id} not found",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// Uses/instantiates a template
    /// </summary>
    [HttpPost("api/tenants/{tenantId}/templates/{id}/use")]
    [Authorize]
    [ProducesResponseType(typeof(TemplateInstantiationResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TemplateInstantiationResultDto>> UseTemplate(
        string tenantId,
        Guid id,
        [FromBody] UseTemplateDto request)
    {
        var tokenTenantId = User.FindFirst("tenant_id")?.Value;
        var userId = User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(tokenTenantId) || tokenTenantId != tenantId)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "Tenant ID mismatch",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var result = await _templateService.UseTemplateAsync(id, tenantId, request, userId);

        if (!result.Success)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Bad Request",
                Detail = result.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Shares a template to marketplace
    /// </summary>
    [HttpPost("api/tenants/{tenantId}/templates/{id}/share")]
    [Authorize]
    [ProducesResponseType(typeof(PatternTemplateDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PatternTemplateDto>> ShareTemplate(
        string tenantId,
        Guid id,
        [FromBody] ShareTemplateDto request)
    {
        var tokenTenantId = User.FindFirst("tenant_id")?.Value;
        var userId = User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(tokenTenantId) || tokenTenantId != tenantId)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "Tenant ID mismatch",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        try
        {
            var result = await _templateService.ShareTemplateAsync(id, tenantId, request, userId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// Rates a template
    /// </summary>
    [HttpPost("api/tenants/{tenantId}/templates/{id}/rate")]
    [Authorize]
    [ProducesResponseType(typeof(PatternTemplateDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PatternTemplateDto>> RateTemplate(
        string tenantId,
        Guid id,
        [FromBody] RateTemplateDto request)
    {
        var tokenTenantId = User.FindFirst("tenant_id")?.Value;
        var userId = User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(tokenTenantId) || tokenTenantId != tenantId)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "Tenant ID mismatch",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "User ID not found in token",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        try
        {
            var result = await _templateService.RateTemplateAsync(id, tenantId, request, userId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Bad Request",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    // === PUBLIC ENDPOINTS (No tenant required) ===

    /// <summary>
    /// Gets marketplace templates (public)
    /// </summary>
    [HttpGet("api/templates/marketplace")]
    [ProducesResponseType(typeof(PaginatedTemplatesDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedTemplatesDto>> GetMarketplaceTemplates(
        [FromQuery] TemplateQueryDto query)
    {
        var result = await _templateService.GetMarketplaceTemplatesAsync(query);
        return Ok(result);
    }

    /// <summary>
    /// Gets official Binelek templates
    /// </summary>
    [HttpGet("api/templates/official")]
    [ProducesResponseType(typeof(List<PatternTemplateDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PatternTemplateDto>>> GetOfficialTemplates(
        [FromQuery] string? category = null,
        [FromQuery] string? type = null)
    {
        var result = await _templateService.GetOfficialTemplatesAsync(category, type);
        return Ok(result);
    }

    /// <summary>
    /// Gets available template categories
    /// </summary>
    [HttpGet("api/templates/categories")]
    [ProducesResponseType(typeof(List<TemplateCategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TemplateCategoryDto>>> GetCategories()
    {
        var result = await _templateService.GetCategoriesAsync();
        return Ok(result);
    }

    /// <summary>
    /// Gets popular tags
    /// </summary>
    [HttpGet("api/templates/tags")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<string>>> GetPopularTags([FromQuery] int limit = 20)
    {
        var result = await _templateService.GetPopularTagsAsync(limit);
        return Ok(result);
    }
}
