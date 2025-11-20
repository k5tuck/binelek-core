using Binah.Ontology.Models;
using Binah.Ontology.Models.Exceptions;
using Binah.Ontology.Services;
using Binah.Ontology.Models.Relationship;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Binah.Ontology.Repositories;

/// <summary>
/// Neo4j repository implementation for relationship data access
/// </summary>
public class RelationshipRepository : IRelationshipRepository
{
    private readonly IDriver _driver;
    private readonly string _database;
    private readonly ILogger<RelationshipRepository> _logger;

    public RelationshipRepository(
        IDriver driver,
        string database,
        ILogger<RelationshipRepository> logger)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<Relationship> CreateAsync(Relationship relationship)
    {
        if (relationship == null)
        {
            throw new ArgumentNullException(nameof(relationship));
        }

        var session = _driver.AsyncSession(config => config.WithDatabase(_database));
        try
        {
            var query = $@"
                MATCH (from), (to)
                WHERE from.id = $fromId AND to.id = $toId
                CREATE (from)-[r:{relationship.Type} $properties]->(to)
                RETURN r, from.id AS fromId, to.id AS toId
            ";

            var parameters = new Dictionary<string, object>
            {
                { "fromId", relationship.FromEntityId },
                { "toId", relationship.ToEntityId },
                { "properties", RelationshipToNeo4jProperties(relationship) }
            };

            var cursor = await session.RunAsync(query, parameters);
            var result = await cursor.SingleAsync();

            if (result == null)
            {
                throw new DatabaseConnectionException("Failed to create relationship in Neo4j");
            }

            var rel = result["r"].As<IRelationship>();
            var fromId = result["fromId"].As<string>();
            var toId = result["toId"].As<string>();

            return Neo4jRelationshipToRelationship(rel, fromId, toId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating relationship {Type} in Neo4j", relationship.Type);
            throw new DatabaseConnectionException("Failed to create relationship", ex);
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    /// <inheritdoc/>
    public async Task<Relationship?> GetAsync(string type, string fromEntityId, string toEntityId)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentException("Type cannot be null or empty", nameof(type));
        }

        if (string.IsNullOrWhiteSpace(fromEntityId))
        {
            throw new ArgumentException("From entity ID cannot be null or empty", nameof(fromEntityId));
        }

        if (string.IsNullOrWhiteSpace(toEntityId))
        {
            throw new ArgumentException("To entity ID cannot be null or empty", nameof(toEntityId));
        }

        var session = _driver.AsyncSession(config => config.WithDatabase(_database));
        try
        {
            var query = $@"
                MATCH (from)-[r:{type}]->(to)
                WHERE from.id = $fromId AND to.id = $toId
                RETURN r, from.id AS fromId, to.id AS toId
            ";

            var parameters = new Dictionary<string, object>
            {
                { "fromId", fromEntityId },
                { "toId", toEntityId }
            };

            var cursor = await session.RunAsync(query, parameters);
            var records = await cursor.ToListAsync();
            var result = records.Count > 0 ? records[0] : null;

            if (result == null)
            {
                return null;
            }

            var rel = result["r"].As<IRelationship>();
            var fromId = result["fromId"].As<string>();
            var toId = result["toId"].As<string>();

            return Neo4jRelationshipToRelationship(rel, fromId, toId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving relationship {Type} from Neo4j", type);
            throw new DatabaseConnectionException($"Failed to retrieve relationship {type}", ex);
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    /// <inheritdoc/>
    public async Task<List<Relationship>> GetForEntityAsync(
        string entityId,
        RelationshipDirection direction,
        string? relationshipType)
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArgumentException("Entity ID cannot be null or empty", nameof(entityId));
        }

        var session = _driver.AsyncSession(config => config.WithDatabase(_database));
        try
        {
            string query;
            var typeFilter = string.IsNullOrWhiteSpace(relationshipType) ? "" : $":{relationshipType}";

            switch (direction)
            {
                case RelationshipDirection.Outgoing:
                    query = $@"
                        MATCH (e)-[r{typeFilter}]->(target)
                        WHERE e.id = $entityId
                        RETURN r, e.id AS fromId, target.id AS toId
                    ";
                    break;

                case RelationshipDirection.Incoming:
                    query = $@"
                        MATCH (source)-[r{typeFilter}]->(e)
                        WHERE e.id = $entityId
                        RETURN r, source.id AS fromId, e.id AS toId
                    ";
                    break;

                case RelationshipDirection.Both:
                default:
                    query = $@"
                        MATCH (e)-[r{typeFilter}]-(other)
                        WHERE e.id = $entityId
                        RETURN r,
                               CASE WHEN startNode(r).id = e.id
                                    THEN startNode(r).id
                                    ELSE endNode(r).id
                               END AS fromId,
                               CASE WHEN startNode(r).id = e.id
                                    THEN endNode(r).id
                                    ELSE startNode(r).id
                               END AS toId
                    ";
                    break;
            }

            var parameters = new Dictionary<string, object>
            {
                { "entityId", entityId }
            };

            var cursor = await session.RunAsync(query, parameters);
            var results = await cursor.ToListAsync();

            var relationships = results.Select(record =>
            {
                var rel = record["r"].As<IRelationship>();
                var fromId = record["fromId"].As<string>();
                var toId = record["toId"].As<string>();
                return Neo4jRelationshipToRelationship(rel, fromId, toId);
            }).ToList();

            return relationships;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving relationships for entity {EntityId} from Neo4j", entityId);
            throw new DatabaseConnectionException($"Failed to retrieve relationships for entity {entityId}", ex);
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(string type, string fromEntityId, string toEntityId)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentException("Type cannot be null or empty", nameof(type));
        }

        if (string.IsNullOrWhiteSpace(fromEntityId))
        {
            throw new ArgumentException("From entity ID cannot be null or empty", nameof(fromEntityId));
        }

        if (string.IsNullOrWhiteSpace(toEntityId))
        {
            throw new ArgumentException("To entity ID cannot be null or empty", nameof(toEntityId));
        }

        var session = _driver.AsyncSession(config => config.WithDatabase(_database));
        try
        {
            var query = $@"
                MATCH (from)-[r:{type}]->(to)
                WHERE from.id = $fromId AND to.id = $toId
                DELETE r
                RETURN count(r) AS deletedCount
            ";

            var parameters = new Dictionary<string, object>
            {
                { "fromId", fromEntityId },
                { "toId", toEntityId }
            };

            var cursor = await session.RunAsync(query, parameters);
            var result = await cursor.SingleAsync();
            var deletedCount = result["deletedCount"].As<int>();

            return deletedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting relationship {Type} from Neo4j", type);
            throw new DatabaseConnectionException($"Failed to delete relationship {type}", ex);
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    /// <summary>
    /// Converts a Relationship object to Neo4j properties dictionary
    /// </summary>
    private Dictionary<string, object> RelationshipToNeo4jProperties(Relationship relationship)
    {
        var properties = new Dictionary<string, object>
        {
            { "created_at", relationship.CreatedAt.ToString("O") }
        };

        if (!string.IsNullOrWhiteSpace(relationship.CreatedBy))
        {
            properties["created_by"] = relationship.CreatedBy;
        }

        if (!string.IsNullOrWhiteSpace(relationship.TenantId))
        {
            properties["tenant_id"] = relationship.TenantId;
        }

        // Serialize custom properties as JSON
        if (relationship.Properties != null && relationship.Properties.Count > 0)
        {
            properties["properties"] = JsonSerializer.Serialize(relationship.Properties);
        }

        return properties;
    }

    /// <summary>
    /// Converts a Neo4j relationship to a Relationship object
    /// </summary>
    private Relationship Neo4jRelationshipToRelationship(IRelationship rel, string fromId, string toId)
    {
        var relationship = new Relationship
        {
            Type = rel.Type,
            FromEntityId = fromId,
            ToEntityId = toId
        };

        if (rel.Properties.ContainsKey("created_at"))
        {
            relationship.CreatedAt = DateTime.Parse(rel.Properties["created_at"].As<string>());
        }

        if (rel.Properties.ContainsKey("created_by"))
        {
            relationship.CreatedBy = rel.Properties["created_by"].As<string>();
        }

        if (rel.Properties.ContainsKey("tenant_id"))
        {
            relationship.TenantId = rel.Properties["tenant_id"].As<string>();
        }

        // Deserialize custom properties from JSON
        if (rel.Properties.ContainsKey("properties"))
        {
            var propertiesJson = rel.Properties["properties"].As<string>();
            relationship.Properties = JsonSerializer.Deserialize<Dictionary<string, object>>(propertiesJson);
        }

        return relationship;
    }
}
