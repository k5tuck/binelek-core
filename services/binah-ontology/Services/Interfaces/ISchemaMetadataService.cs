using Binah.Ontology.Models;

namespace Binah.Ontology.Services.Interfaces;

/// <summary>
/// Service for accessing ontology schema metadata at runtime
/// Enables dynamic UI rendering and self-healing documentation
/// </summary>
public interface ISchemaMetadataService
{
    /// <summary>
    /// Get complete schema for a tenant
    /// </summary>
    Task<SchemaDefinition> GetSchemaAsync(string tenantId);

    /// <summary>
    /// Get schema for a specific entity type
    /// </summary>
    Task<EntitySchema> GetEntitySchemaAsync(string tenantId, string entityType);

    /// <summary>
    /// Get all entity types for a tenant
    /// </summary>
    Task<List<string>> GetEntityTypesAsync(string tenantId);

    /// <summary>
    /// Get all relationship types between entities
    /// </summary>
    Task<List<RelationshipSchema>> GetRelationshipSchemasAsync(string tenantId);

    /// <summary>
    /// Get validation rules for an entity type
    /// </summary>
    Task<List<ValidationRule>> GetValidationRulesAsync(string tenantId, string entityType);

    /// <summary>
    /// Get UI configuration for an entity type
    /// </summary>
    Task<UIConfiguration> GetUIConfigurationAsync(string tenantId, string entityType);

    /// <summary>
    /// Watch for schema changes (returns version number)
    /// </summary>
    Task<int> GetSchemaVersionAsync(string tenantId);
}
