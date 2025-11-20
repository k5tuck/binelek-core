using Binah.Ontology.Models;
using Binah.Ontology.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Binah.Ontology.Controllers;

/// <summary>
/// API endpoints for runtime schema introspection
/// Enables dynamic UI rendering and self-healing documentation
/// </summary>
[ApiController]
[Route("api/schema")]
[Authorize]
public class SchemaController : ControllerBase
{
    private readonly ISchemaMetadataService _schemaService;
    private readonly ILogger<SchemaController> _logger;

    public SchemaController(ISchemaMetadataService schemaService, ILogger<SchemaController> logger)
    {
        _schemaService = schemaService;
        _logger = logger;
    }

    /// <summary>
    /// Get complete schema for current tenant
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(SchemaDefinition), 200)]
    public async Task<IActionResult> GetSchema()
    {
        var tenantId = GetTenantId();
        _logger.LogInformation("Getting schema for tenant {TenantId}", tenantId);

        var schema = await _schemaService.GetSchemaAsync(tenantId);
        return Ok(schema);
    }

    /// <summary>
    /// Get schema for a specific entity type
    /// </summary>
    [HttpGet("entities/{entityType}")]
    [ProducesResponseType(typeof(EntitySchema), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetEntitySchema(string entityType)
    {
        var tenantId = GetTenantId();
        _logger.LogInformation("Getting entity schema for {EntityType}, tenant {TenantId}", entityType, tenantId);

        try
        {
            var entitySchema = await _schemaService.GetEntitySchemaAsync(tenantId, entityType);
            return Ok(entitySchema);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Entity type {EntityType} not found for tenant {TenantId}", entityType, tenantId);
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get all entity types for current tenant
    /// </summary>
    [HttpGet("entities")]
    [ProducesResponseType(typeof(List<string>), 200)]
    public async Task<IActionResult> GetEntityTypes()
    {
        var tenantId = GetTenantId();
        _logger.LogInformation("Getting entity types for tenant {TenantId}", tenantId);

        var entityTypes = await _schemaService.GetEntityTypesAsync(tenantId);
        return Ok(entityTypes);
    }

    /// <summary>
    /// Get all relationship schemas for current tenant
    /// </summary>
    [HttpGet("relationships")]
    [ProducesResponseType(typeof(List<RelationshipSchema>), 200)]
    public async Task<IActionResult> GetRelationships()
    {
        var tenantId = GetTenantId();
        _logger.LogInformation("Getting relationship schemas for tenant {TenantId}", tenantId);

        var relationships = await _schemaService.GetRelationshipSchemasAsync(tenantId);
        return Ok(relationships);
    }

    /// <summary>
    /// Get validation rules for a specific entity type
    /// </summary>
    [HttpGet("entities/{entityType}/validation")]
    [ProducesResponseType(typeof(List<ValidationRule>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetValidationRules(string entityType)
    {
        var tenantId = GetTenantId();
        _logger.LogInformation("Getting validation rules for {EntityType}, tenant {TenantId}", entityType, tenantId);

        try
        {
            var rules = await _schemaService.GetValidationRulesAsync(tenantId, entityType);
            return Ok(rules);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Entity type {EntityType} not found for tenant {TenantId}", entityType, tenantId);
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get UI configuration for a specific entity type
    /// </summary>
    [HttpGet("entities/{entityType}/ui-config")]
    [ProducesResponseType(typeof(UIConfiguration), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetUIConfiguration(string entityType)
    {
        var tenantId = GetTenantId();
        _logger.LogInformation("Getting UI configuration for {EntityType}, tenant {TenantId}", entityType, tenantId);

        try
        {
            var uiConfig = await _schemaService.GetUIConfigurationAsync(tenantId, entityType);
            return Ok(uiConfig);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Entity type {EntityType} not found for tenant {TenantId}", entityType, tenantId);
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get current schema version
    /// </summary>
    [HttpGet("version")]
    [ProducesResponseType(typeof(int), 200)]
    public async Task<IActionResult> GetSchemaVersion()
    {
        var tenantId = GetTenantId();
        _logger.LogInformation("Getting schema version for tenant {TenantId}", tenantId);

        var version = await _schemaService.GetSchemaVersionAsync(tenantId);
        return Ok(new { version, tenantId });
    }

    private string GetTenantId()
    {
        // Try to get from header first
        if (Request.Headers.TryGetValue("X-Tenant-Id", out var tenantIdHeader))
        {
            return tenantIdHeader.ToString();
        }

        // Try to get from JWT claims
        var tenantIdClaim = User.FindFirst("tenant_id")?.Value;
        if (!string.IsNullOrEmpty(tenantIdClaim))
        {
            return tenantIdClaim;
        }

        // Default to core if not found (shouldn't happen with proper auth)
        _logger.LogWarning("Tenant ID not found in request, defaulting to 'core'");
        return "core";
    }
}
