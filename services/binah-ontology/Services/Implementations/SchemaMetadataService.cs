using Binah.Ontology.Models;
using Binah.Ontology.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;

namespace Binah.Ontology.Services.Implementations;

/// <summary>
/// Service for accessing ontology schema metadata at runtime
/// Reads schema from Neo4j and YAML configurations
/// </summary>
public class SchemaMetadataService : ISchemaMetadataService
{
    private readonly IDriver _neo4jDriver;
    private readonly ILogger<SchemaMetadataService> _logger;
    private readonly Dictionary<string, SchemaDefinition> _schemaCache = new();
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    public SchemaMetadataService(IDriver neo4jDriver, ILogger<SchemaMetadataService> logger)
    {
        _neo4jDriver = neo4jDriver;
        _logger = logger;
    }

    public async Task<SchemaDefinition> GetSchemaAsync(string tenantId)
    {
        try
        {
            // Check cache first
            await _cacheLock.WaitAsync();
            try
            {
                if (_schemaCache.TryGetValue(tenantId, out var cachedSchema))
                {
                    _logger.LogDebug("Returning cached schema for tenant {TenantId}", tenantId);
                    return cachedSchema;
                }
            }
            finally
            {
                _cacheLock.Release();
            }

            _logger.LogInformation("Loading schema for tenant {TenantId} from Neo4j", tenantId);

            var schema = new SchemaDefinition
            {
                TenantId = tenantId,
                LastModified = DateTime.UtcNow
            };

            // Load entity schemas
            schema.Entities = await LoadEntitySchemasAsync(tenantId);

            // Load relationship schemas
            schema.Relationships = await LoadRelationshipSchemasAsync(tenantId);

            // Get schema version
            schema.Version = await GetSchemaVersionAsync(tenantId);

            // Cache the schema
            await _cacheLock.WaitAsync();
            try
            {
                _schemaCache[tenantId] = schema;
            }
            finally
            {
                _cacheLock.Release();
            }

            return schema;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading schema for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<EntitySchema> GetEntitySchemaAsync(string tenantId, string entityType)
    {
        var schema = await GetSchemaAsync(tenantId);
        var entitySchema = schema.Entities.FirstOrDefault(e => e.EntityType == entityType);

        if (entitySchema == null)
        {
            throw new InvalidOperationException($"Entity type '{entityType}' not found in schema for tenant '{tenantId}'");
        }

        return entitySchema;
    }

    public async Task<List<string>> GetEntityTypesAsync(string tenantId)
    {
        var schema = await GetSchemaAsync(tenantId);
        return schema.Entities.Select(e => e.EntityType).ToList();
    }

    public async Task<List<RelationshipSchema>> GetRelationshipSchemasAsync(string tenantId)
    {
        var schema = await GetSchemaAsync(tenantId);
        return schema.Relationships;
    }

    public async Task<List<ValidationRule>> GetValidationRulesAsync(string tenantId, string entityType)
    {
        var entitySchema = await GetEntitySchemaAsync(tenantId, entityType);
        return entitySchema.ValidationRules;
    }

    public async Task<UIConfiguration> GetUIConfigurationAsync(string tenantId, string entityType)
    {
        var entitySchema = await GetEntitySchemaAsync(tenantId, entityType);
        return entitySchema.UIConfig;
    }

    public async Task<int> GetSchemaVersionAsync(string tenantId)
    {
        await using var session = _neo4jDriver.AsyncSession();

        var query = @"
            MATCH (v:SchemaVersion {tenantId: $tenantId})
            RETURN v.version AS version
            ORDER BY v.timestamp DESC
            LIMIT 1";

        var result = await session.RunAsync(query, new { tenantId });
        var record = await result.SingleOrDefaultAsync();

        return record?["version"].As<int>() ?? 1;
    }

    /// <summary>
    /// Invalidate cache when schema changes
    /// </summary>
    public async Task InvalidateCacheAsync(string tenantId)
    {
        await _cacheLock.WaitAsync();
        try
        {
            _schemaCache.Remove(tenantId);
            _logger.LogInformation("Schema cache invalidated for tenant {TenantId}", tenantId);
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    private async Task<List<EntitySchema>> LoadEntitySchemasAsync(string tenantId)
    {
        await using var session = _neo4jDriver.AsyncSession();

        var query = @"
            MATCH (e:EntityType)
            WHERE e.tenantId = $tenantId OR e.tenantId = 'core'
            OPTIONAL MATCH (e)-[:HAS_PROPERTY]->(p:PropertyDefinition)
            OPTIONAL MATCH (e)-[:EXTENDS]->(parent:EntityType)
            RETURN e, collect(p) AS properties, parent.name AS extendsEntity";

        var result = await session.RunAsync(query, new { tenantId });
        var entities = new List<EntitySchema>();

        await result.ForEachAsync(record =>
        {
            var entityNode = record["e"].As<INode>();
            var propertyNodes = record["properties"].As<List<INode>>();
            var extendsEntity = record["extendsEntity"].As<string?>();

            var entitySchema = new EntitySchema
            {
                EntityType = entityNode.Properties["name"].As<string>(),
                Label = entityNode.Properties.ContainsKey("label")
                    ? entityNode.Properties["label"].As<string>()
                    : entityNode.Properties["name"].As<string>(),
                Description = entityNode.Properties.ContainsKey("description")
                    ? entityNode.Properties["description"].As<string>()
                    : string.Empty,
                IsCore = entityNode.Properties["tenantId"].As<string>() == "core",
                ExtendsEntity = extendsEntity,
                Properties = propertyNodes.Select(p => new PropertySchema
                {
                    Name = p.Properties["name"].As<string>(),
                    Type = p.Properties["type"].As<string>(),
                    Required = p.Properties.ContainsKey("required") && p.Properties["required"].As<bool>(),
                    Indexed = p.Properties.ContainsKey("indexed") && p.Properties["indexed"].As<bool>(),
                    Description = p.Properties.ContainsKey("description") ? p.Properties["description"].As<string>() : null,
                    DefaultValue = p.Properties.ContainsKey("defaultValue") ? p.Properties["defaultValue"] : null
                }).ToList(),
                ValidationRules = LoadValidationRulesFromNode(entityNode),
                UIConfig = LoadUIConfigFromNode(entityNode)
            };

            entities.Add(entitySchema);
        });

        return entities;
    }

    private async Task<List<RelationshipSchema>> LoadRelationshipSchemasAsync(string tenantId)
    {
        await using var session = _neo4jDriver.AsyncSession();

        var query = @"
            MATCH (r:RelationshipType)
            WHERE r.tenantId = $tenantId OR r.tenantId = 'core'
            RETURN r";

        var result = await session.RunAsync(query, new { tenantId });
        var relationships = new List<RelationshipSchema>();

        await result.ForEachAsync(record =>
        {
            var relNode = record["r"].As<INode>();

            var relationshipSchema = new RelationshipSchema
            {
                Type = relNode.Properties["type"].As<string>(),
                FromEntity = relNode.Properties["fromEntity"].As<string>(),
                ToEntity = relNode.Properties["toEntity"].As<string>(),
                Description = relNode.Properties.ContainsKey("description")
                    ? relNode.Properties["description"].As<string>()
                    : string.Empty,
                Required = relNode.Properties.ContainsKey("required") && relNode.Properties["required"].As<bool>(),
                Cardinality = relNode.Properties.ContainsKey("cardinality")
                    ? relNode.Properties["cardinality"].As<string>()
                    : "one-to-many"
            };

            relationships.Add(relationshipSchema);
        });

        return relationships;
    }

    private List<ValidationRule> LoadValidationRulesFromNode(INode entityNode)
    {
        var rules = new List<ValidationRule>();

        if (!entityNode.Properties.ContainsKey("validationRules"))
            return rules;

        try
        {
            var validationData = entityNode.Properties["validationRules"].As<List<Dictionary<string, object>>>();

            foreach (var ruleData in validationData)
            {
                rules.Add(new ValidationRule
                {
                    PropertyName = ruleData.ContainsKey("propertyName") ? ruleData["propertyName"].ToString()! : string.Empty,
                    ValidatorType = ruleData.ContainsKey("validatorType") ? ruleData["validatorType"].ToString()! : string.Empty,
                    Parameters = ruleData.ContainsKey("parameters") ? ruleData["parameters"] as Dictionary<string, object> : null,
                    ErrorMessage = ruleData.ContainsKey("errorMessage") ? ruleData["errorMessage"].ToString() : null
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing validation rules from entity node");
        }

        return rules;
    }

    private UIConfiguration LoadUIConfigFromNode(INode entityNode)
    {
        var config = new UIConfiguration();

        if (!entityNode.Properties.ContainsKey("uiConfig"))
            return config;

        try
        {
            var uiData = entityNode.Properties["uiConfig"].As<Dictionary<string, object>>();

            config.Icon = uiData.ContainsKey("icon") ? uiData["icon"].ToString() : null;
            config.Color = uiData.ContainsKey("color") ? uiData["color"].ToString() : null;

            if (uiData.ContainsKey("displayFields"))
                config.DisplayFields = ((List<object>)uiData["displayFields"]).Select(f => f.ToString()!).ToList();

            if (uiData.ContainsKey("searchableFields"))
                config.SearchableFields = ((List<object>)uiData["searchableFields"]).Select(f => f.ToString()!).ToList();

            if (uiData.ContainsKey("customSettings"))
                config.CustomSettings = uiData["customSettings"] as Dictionary<string, object>;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing UI configuration from entity node");
        }

        return config;
    }
}
