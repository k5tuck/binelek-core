using Neo4j.Driver;
using System.Text.Json;

namespace Binah.Ontology.Repositories;

/// <summary>
/// Repository for storing entities and relationships in Neo4j
/// </summary>
public class Neo4jRepository : IAsyncDisposable
{
    private readonly IDriver _driver;
    private readonly ILogger<Neo4jRepository> _logger;

    public Neo4jRepository(string uri, string username, string password, ILogger<Neo4jRepository> logger)
    {
        _driver = GraphDatabase.Driver(uri, AuthTokens.Basic(username, password));
        _logger = logger;
    }

    /// <summary>
    /// Store an entity in Neo4j
    /// </summary>
    public async Task<bool> StoreEntityAsync(
        Guid tenantId,
        string entityType,
        Dictionary<string, object> properties)
    {
        try
        {
            await using var session = _driver.AsyncSession();

            var result = await session.ExecuteWriteAsync(async tx =>
            {
                // Create or merge entity node
                var query = @"
                    MERGE (e:Entity {id: $id, tenantId: $tenantId, entityType: $entityType})
                    SET e += $properties
                    SET e.updatedAt = datetime()
                    RETURN e";

                var parameters = new
                {
                    id = properties.ContainsKey("id") ? properties["id"].ToString() : Guid.NewGuid().ToString(),
                    tenantId = tenantId.ToString(),
                    entityType,
                    properties = ConvertToNeo4jProperties(properties)
                };

                var cursor = await tx.RunAsync(query, parameters);
                return await cursor.ConsumeAsync();
            });

            _logger.LogInformation(
                "Stored entity {EntityType} for tenant {TenantId}",
                entityType, tenantId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to store entity {EntityType} for tenant {TenantId}",
                entityType, tenantId);
            return false;
        }
    }

    /// <summary>
    /// Create a relationship between two entities
    /// </summary>
    public async Task<bool> CreateRelationshipAsync(
        Guid tenantId,
        string sourceEntityId,
        string targetEntityId,
        string relationshipType,
        Dictionary<string, object>? properties = null)
    {
        try
        {
            await using var session = _driver.AsyncSession();

            var result = await session.ExecuteWriteAsync(async tx =>
            {
                var query = $@"
                    MATCH (source:Entity {{id: $sourceId, tenantId: $tenantId}})
                    MATCH (target:Entity {{id: $targetId, tenantId: $tenantId}})
                    MERGE (source)-[r:{relationshipType}]->(target)
                    SET r += $properties
                    SET r.createdAt = datetime()
                    RETURN r";

                var parameters = new
                {
                    sourceId = sourceEntityId,
                    targetId = targetEntityId,
                    tenantId = tenantId.ToString(),
                    properties = properties != null ? ConvertToNeo4jProperties(properties) : new Dictionary<string, object>()
                };

                var cursor = await tx.RunAsync(query, parameters);
                return await cursor.ConsumeAsync();
            });

            _logger.LogInformation(
                "Created relationship {RelationshipType} from {SourceId} to {TargetId} for tenant {TenantId}",
                relationshipType, sourceEntityId, targetEntityId, tenantId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to create relationship {RelationshipType} for tenant {TenantId}",
                relationshipType, tenantId);
            return false;
        }
    }

    /// <summary>
    /// Query entities by type
    /// </summary>
    public async Task<List<Dictionary<string, object>>> QueryEntitiesByTypeAsync(
        Guid tenantId,
        string entityType,
        int limit = 100)
    {
        try
        {
            await using var session = _driver.AsyncSession();

            var entities = await session.ExecuteReadAsync(async tx =>
            {
                var query = @"
                    MATCH (e:Entity {tenantId: $tenantId, entityType: $entityType})
                    RETURN e
                    LIMIT $limit";

                var parameters = new
                {
                    tenantId = tenantId.ToString(),
                    entityType,
                    limit
                };

                var cursor = await tx.RunAsync(query, parameters);
                var records = await cursor.ToListAsync();

                return records.Select(record =>
                {
                    var node = record["e"].As<Neo4j.Driver.INode>();
                    return ConvertFromNeo4jProperties(node.Properties);
                }).ToList();
            });

            return entities;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to query entities {EntityType} for tenant {TenantId}",
                entityType, tenantId);
            return new List<Dictionary<string, object>>();
        }
    }

    /// <summary>
    /// Delete an entity
    /// </summary>
    public async Task<bool> DeleteEntityAsync(Guid tenantId, string entityId)
    {
        try
        {
            await using var session = _driver.AsyncSession();

            await session.ExecuteWriteAsync(async tx =>
            {
                var query = @"
                    MATCH (e:Entity {id: $id, tenantId: $tenantId})
                    DETACH DELETE e";

                var parameters = new
                {
                    id = entityId,
                    tenantId = tenantId.ToString()
                };

                var cursor = await tx.RunAsync(query, parameters);
                return await cursor.ConsumeAsync();
            });

            _logger.LogInformation(
                "Deleted entity {EntityId} for tenant {TenantId}",
                entityId, tenantId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to delete entity {EntityId} for tenant {TenantId}",
                entityId, tenantId);
            return false;
        }
    }

    /// <summary>
    /// Convert dictionary to Neo4j-compatible properties
    /// </summary>
    private Dictionary<string, object> ConvertToNeo4jProperties(Dictionary<string, object> properties)
    {
        var neo4jProps = new Dictionary<string, object>();

        foreach (var (key, value) in properties)
        {
            // Skip metadata object - flatten it instead
            if (key == "_metadata" && value is Dictionary<string, object> metadata)
            {
                foreach (var (metaKey, metaValue) in metadata)
                {
                    neo4jProps[$"meta_{metaKey}"] = ConvertValue(metaValue);
                }
                continue;
            }

            // Skip context metadata
            if (key == "_enrichedAt" || key == "_contextMetadata")
            {
                continue;
            }

            neo4jProps[key] = ConvertValue(value);
        }

        return neo4jProps;
    }

    /// <summary>
    /// Convert value to Neo4j-compatible type
    /// </summary>
    private object ConvertValue(object value)
    {
        return value switch
        {
            Dictionary<string, object> dict => JsonSerializer.Serialize(dict),
            IEnumerable<object> list => JsonSerializer.Serialize(list),
            DateTime dt => dt.ToString("o"),
            Guid guid => guid.ToString(),
            JsonElement element => element.ToString(),
            _ => value
        };
    }

    /// <summary>
    /// Convert Neo4j properties back to dictionary
    /// </summary>
    private Dictionary<string, object> ConvertFromNeo4jProperties(IReadOnlyDictionary<string, object> properties)
    {
        var result = new Dictionary<string, object>();

        foreach (var (key, value) in properties)
        {
            result[key] = value;
        }

        return result;
    }

    public async ValueTask DisposeAsync()
    {
        await _driver.DisposeAsync();
    }
}
