using Binah.Ontology.Models;
using Binah.Ontology.Models.Base;
using Binah.Ontology.Models.Exceptions;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Binah.Ontology.Repositories;

/// <summary>
/// Neo4j repository implementation for entity data access
/// </summary>
public class EntityRepository : IEntityRepository
{
    private readonly IDriver _driver;
    private readonly string _database;
    private readonly ILogger<EntityRepository> _logger;

    public EntityRepository(
        IDriver driver,
        string database,
        ILogger<EntityRepository> logger)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<Entity> CreateAsync(Entity entity)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        var session = _driver.AsyncSession(config => config.WithDatabase(_database));
        try
        {
            var query = $@"
                CREATE (e:{entity.Type} $properties)
                RETURN e
            ";

            var parameters = new Dictionary<string, object>
            {
                { "properties", EntityToNeo4jProperties(entity) }
            };

            var cursor = await session.RunAsync(query, parameters);
            var result = await cursor.SingleAsync();

            if (result == null)
            {
                throw new DatabaseConnectionException("Failed to create entity in Neo4j");
            }

            var node = result["e"].As<Neo4j.Driver.INode>();
            return Neo4jNodeToEntity(node);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating entity {EntityId} in Neo4j", entity.Id);
            throw new DatabaseConnectionException("Failed to create entity", ex);
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    /// <inheritdoc/>
    public async Task<Entity?> GetByIdAsync(string entityId)
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArgumentException("Entity ID cannot be null or empty", nameof(entityId));
        }

        var session = _driver.AsyncSession(config => config.WithDatabase(_database));
        try
        {
            var query = @"
                MATCH (e)
                WHERE e.id = $entityId
                RETURN e
            ";

            var parameters = new Dictionary<string, object>
            {
                { "entityId", entityId }
            };

            var cursor = await session.RunAsync(query, parameters);
            var records = await cursor.ToListAsync();
            var result = records.Count > 0 ? records[0] : null;

            if (result == null)
            {
                return null;
            }

            var node = result["e"].As<Neo4j.Driver.INode>();
            return Neo4jNodeToEntity(node);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving entity {EntityId} from Neo4j", entityId);
            throw new DatabaseConnectionException($"Failed to retrieve entity {entityId}", ex);
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    /// <inheritdoc/>
    public async Task<List<Entity>> GetByTypeAsync(string type, int skip, int limit)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentException("Type cannot be null or empty", nameof(type));
        }

        var session = _driver.AsyncSession(config => config.WithDatabase(_database));
        try
        {
            var query = $@"
                MATCH (e:{type})
                RETURN e
                ORDER BY e.created_at DESC
                SKIP $skip
                LIMIT $limit
            ";

            var parameters = new Dictionary<string, object>
            {
                { "skip", skip },
                { "limit", limit }
            };

            var cursor = await session.RunAsync(query, parameters);
            var results = await cursor.ToListAsync();

            var entities = results.Select(record =>
            {
                var node = record["e"].As<Neo4j.Driver.INode>();
                return Neo4jNodeToEntity(node);
            }).ToList();

            return entities;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving entities of type {Type} from Neo4j", type);
            throw new DatabaseConnectionException($"Failed to retrieve entities of type {type}", ex);
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    /// <inheritdoc/>
    public async Task<Entity> UpdateAsync(Entity entity)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        var session = _driver.AsyncSession(config => config.WithDatabase(_database));
        try
        {
            var query = @"
                MATCH (e)
                WHERE e.id = $entityId
                SET e = $properties
                RETURN e
            ";

            var parameters = new Dictionary<string, object>
            {
                { "entityId", entity.Id },
                { "properties", EntityToNeo4jProperties(entity) }
            };

            var cursor = await session.RunAsync(query, parameters);
            var records = await cursor.ToListAsync();
            var result = records.Count > 0 ? records[0] : null;

            if (result == null)
            {
                throw new EntityNotFoundException(entity.Id);
            }

            var node = result["e"].As<Neo4j.Driver.INode>();
            return Neo4jNodeToEntity(node);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating entity {EntityId} in Neo4j", entity.Id);
            throw new DatabaseConnectionException($"Failed to update entity {entity.Id}", ex);
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    /// <inheritdoc/>
    public async Task<List<Entity>> SearchAsync(string searchTerm, string? type, int limit)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            throw new ArgumentException("Search term cannot be null or empty", nameof(searchTerm));
        }

        var session = _driver.AsyncSession(config => config.WithDatabase(_database));
        try
        {
            // Use full-text search index if type is specified, otherwise search across all types
            string query;
            var parameters = new Dictionary<string, object>
            {
                { "searchTerm", searchTerm },
                { "limit", limit }
            };

            if (!string.IsNullOrWhiteSpace(type))
            {
                query = $@"
                    CALL db.index.fulltext.queryNodes('entity_search', $searchTerm)
                    YIELD node, score
                    WHERE '{type}' IN labels(node)
                    RETURN node
                    ORDER BY score DESC
                    LIMIT $limit
                ";
            }
            else
            {
                query = @"
                    CALL db.index.fulltext.queryNodes('entity_search', $searchTerm)
                    YIELD node, score
                    RETURN node
                    ORDER BY score DESC
                    LIMIT $limit
                ";
            }

            var cursor = await session.RunAsync(query, parameters);
            var results = await cursor.ToListAsync();

            var entities = results.Select(record =>
            {
                var node = record["node"].As<Neo4j.Driver.INode>();
                return Neo4jNodeToEntity(node);
            }).ToList();

            return entities;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching entities with term '{SearchTerm}' in Neo4j", searchTerm);
            throw new DatabaseConnectionException($"Failed to search entities", ex);
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(string entityId)
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArgumentException("Entity ID cannot be null or empty", nameof(entityId));
        }

        var entity = await GetByIdAsync(entityId);
        return entity != null;
    }

    /// <summary>
    /// Converts an Entity object to Neo4j properties dictionary
    /// </summary>
    private Dictionary<string, object> EntityToNeo4jProperties(Entity entity)
    {
        var properties = new Dictionary<string, object>
        {
            { "id", entity.Id },
            { "type", entity.Type },
            { "version", entity.Version },
            { "created_at", entity.CreatedAt.ToString("O") },
            { "updated_at", entity.UpdatedAt.ToString("O") },
            { "source", entity.Source },
            { "is_deleted", entity.IsDeleted }
        };

        // Add optional properties
        if (!string.IsNullOrWhiteSpace(entity.CreatedBy))
            properties["created_by"] = entity.CreatedBy;

        if (!string.IsNullOrWhiteSpace(entity.UpdatedBy))
            properties["updated_by"] = entity.UpdatedBy;

        if (!string.IsNullOrWhiteSpace(entity.TenantId))
            properties["tenant_id"] = entity.TenantId;

        if (entity.DeletedAt.HasValue)
            properties["deleted_at"] = entity.DeletedAt.Value.ToString("O");

        if (!string.IsNullOrWhiteSpace(entity.DeletedBy))
            properties["deleted_by"] = entity.DeletedBy;

        // Serialize entity properties as JSON
        if (entity.Properties != null && entity.Properties.Count > 0)
        {
            properties["properties"] = JsonSerializer.Serialize(entity.Properties);
        }

        // Serialize metadata as JSON
        if (entity.Metadata != null && entity.Metadata.Count > 0)
        {
            properties["metadata"] = JsonSerializer.Serialize(entity.Metadata);
        }

        return properties;
    }

    /// <summary>
    /// Converts a Neo4j node to an Entity object
    /// </summary>
    private Entity Neo4jNodeToEntity(Neo4j.Driver.INode node)
    {
        var entity = new Entity
        {
            Id = node.Properties["id"].As<string>(),
            Type = node.Labels.FirstOrDefault() ?? node.Properties["type"].As<string>(),
            Version = node.Properties.ContainsKey("version") ? node.Properties["version"].As<string>() : "1.0",
            Source = node.Properties.ContainsKey("source") ? node.Properties["source"].As<string>() : "Binah.Ontology",
            IsDeleted = node.Properties.ContainsKey("is_deleted") && node.Properties["is_deleted"].As<bool>()
        };

        // Parse timestamps
        if (node.Properties.ContainsKey("created_at"))
        {
            entity.CreatedAt = DateTime.Parse(node.Properties["created_at"].As<string>());
        }

        if (node.Properties.ContainsKey("updated_at"))
        {
            entity.UpdatedAt = DateTime.Parse(node.Properties["updated_at"].As<string>());
        }

        if (node.Properties.ContainsKey("deleted_at"))
        {
            entity.DeletedAt = DateTime.Parse(node.Properties["deleted_at"].As<string>());
        }

        // Parse optional properties
        if (node.Properties.ContainsKey("created_by"))
            entity.CreatedBy = node.Properties["created_by"].As<string>();

        if (node.Properties.ContainsKey("updated_by"))
            entity.UpdatedBy = node.Properties["updated_by"].As<string>();

        if (node.Properties.ContainsKey("tenant_id"))
            entity.TenantId = node.Properties["tenant_id"].As<string>();

        if (node.Properties.ContainsKey("deleted_by"))
            entity.DeletedBy = node.Properties["deleted_by"].As<string>();

        // Deserialize entity properties from JSON
        if (node.Properties.ContainsKey("properties"))
        {
            var propertiesJson = node.Properties["properties"].As<string>();
            entity.Properties = JsonSerializer.Deserialize<Dictionary<string, object>>(propertiesJson)
                ?? new Dictionary<string, object>();
        }

        // Deserialize metadata from JSON
        if (node.Properties.ContainsKey("metadata"))
        {
            var metadataJson = node.Properties["metadata"].As<string>();
            entity.Metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(metadataJson);
        }

        return entity;
    }
}
