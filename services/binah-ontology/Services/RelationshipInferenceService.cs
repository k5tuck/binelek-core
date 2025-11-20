using Binah.Ontology.Repositories;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Binah.Ontology.Services;

/// <summary>
/// Implementation of relationship inference service
/// </summary>
public class RelationshipInferenceService : IRelationshipInferenceService
{
    private readonly IDriver _neo4jDriver;
    private readonly IRelationshipRepository _relationshipRepository;
    private readonly IEntityRepository _entityRepository;
    private readonly ILogger<RelationshipInferenceService> _logger;

    public RelationshipInferenceService(
        IDriver neo4jDriver,
        IRelationshipRepository relationshipRepository,
        IEntityRepository entityRepository,
        ILogger<RelationshipInferenceService> logger)
    {
        _neo4jDriver = neo4jDriver ?? throw new ArgumentNullException(nameof(neo4jDriver));
        _relationshipRepository = relationshipRepository ?? throw new ArgumentNullException(nameof(relationshipRepository));
        _entityRepository = entityRepository ?? throw new ArgumentNullException(nameof(entityRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<int> InferRelationshipsAsync(string tenantId, List<string> entityIds)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
        if (entityIds == null || entityIds.Count == 0)
            throw new ArgumentException("Entity IDs list cannot be null or empty", nameof(entityIds));

        _logger.LogInformation(
            "Inferring relationships for {Count} entities in tenant {TenantId}",
            entityIds.Count, tenantId);

        var totalRelationships = 0;

        foreach (var entityId in entityIds)
        {
            try
            {
                var count = await InferRelationshipsForEntityAsync(tenantId, entityId);
                totalRelationships += count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to infer relationships for entity {EntityId}",
                    entityId);
                // Continue with other entities
            }
        }

        _logger.LogInformation(
            "Inferred {Count} total relationships for {EntityCount} entities",
            totalRelationships, entityIds.Count);

        return totalRelationships;
    }

    /// <inheritdoc/>
    public async Task<int> InferRelationshipsForEntityAsync(string tenantId, string entityId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(entityId))
            throw new ArgumentException("Entity ID cannot be null or empty", nameof(entityId));

        _logger.LogDebug("Inferring relationships for entity {EntityId}", entityId);

        var totalCount = 0;

        // Infer different types of relationships
        totalCount += await InferOwnershipRelationshipsAsync(tenantId, entityId);
        totalCount += await InferSpatialRelationshipsAsync(tenantId, entityId);
        totalCount += await InferTemporalRelationshipsAsync(tenantId, entityId);

        _logger.LogDebug(
            "Inferred {Count} relationships for entity {EntityId}",
            totalCount, entityId);

        return totalCount;
    }

    /// <inheritdoc/>
    public async Task<int> InferOwnershipRelationshipsAsync(string tenantId, string entityId)
    {
        _logger.LogDebug("Inferring ownership relationships for entity {EntityId}", entityId);

        await using var session = _neo4jDriver.AsyncSession();

        // Query to infer ownership based on owner_id or similar properties
        var query = @"
            MATCH (e:Entity {id: $entityId, tenantId: $tenantId})
            WHERE e.owner_id IS NOT NULL OR e.ownerId IS NOT NULL

            WITH e, COALESCE(e.owner_id, e.ownerId) AS ownerId

            MATCH (owner:Entity {id: ownerId, tenantId: $tenantId})
            WHERE NOT EXISTS((owner)-[:OWNS]->(e))

            CREATE (owner)-[r:OWNS {
                tenantId: $tenantId,
                created_at: datetime(),
                inferred: true,
                confidence: 0.95
            }]->(e)

            RETURN count(r) AS count
        ";

        try
        {
            var result = await session.RunAsync(query, new { entityId, tenantId });
            var records = await result.ToListAsync();
            var count = records.Count > 0 ? records[0]["count"].As<int>() : 0;

            _logger.LogDebug("Inferred {Count} ownership relationships", count);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to infer ownership relationships");
            return 0;
        }
    }

    /// <inheritdoc/>
    public async Task<int> InferSpatialRelationshipsAsync(string tenantId, string entityId)
    {
        _logger.LogDebug("Inferring spatial relationships for entity {EntityId}", entityId);

        await using var session = _neo4jDriver.AsyncSession();

        // Query to infer spatial relationships based on geographic proximity
        var query = @"
            MATCH (e:Entity {id: $entityId, tenantId: $tenantId})
            WHERE e.latitude IS NOT NULL AND e.longitude IS NOT NULL

            MATCH (nearby:Entity {tenantId: $tenantId})
            WHERE nearby.id <> e.id
              AND nearby.latitude IS NOT NULL
              AND nearby.longitude IS NOT NULL
              AND NOT EXISTS((e)-[:NEAR]->(nearby))

            WITH e, nearby,
                 point.distance(
                     point({latitude: toFloat(e.latitude), longitude: toFloat(e.longitude)}),
                     point({latitude: toFloat(nearby.latitude), longitude: toFloat(nearby.longitude)})
                 ) AS distance

            WHERE distance < 1000  // Within 1km

            CREATE (e)-[r:NEAR {
                tenantId: $tenantId,
                distance_meters: distance,
                created_at: datetime(),
                inferred: true,
                confidence: 0.90
            }]->(nearby)

            RETURN count(r) AS count
        ";

        try
        {
            var result = await session.RunAsync(query, new { entityId, tenantId });
            var records = await result.ToListAsync();
            var count = records.Count > 0 ? records[0]["count"].As<int>() : 0;

            _logger.LogDebug("Inferred {Count} spatial relationships", count);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to infer spatial relationships");
            return 0;
        }
    }

    /// <inheritdoc/>
    public async Task<int> InferTemporalRelationshipsAsync(string tenantId, string entityId)
    {
        _logger.LogDebug("Inferring temporal relationships for entity {EntityId}", entityId);

        await using var session = _neo4jDriver.AsyncSession();

        // Query to infer temporal relationships based on timestamps
        var query = @"
            MATCH (e:Entity {id: $entityId, tenantId: $tenantId})
            WHERE e.created_at IS NOT NULL

            MATCH (related:Entity {tenantId: $tenantId})
            WHERE related.id <> e.id
              AND related.type = e.type
              AND related.created_at IS NOT NULL
              AND related.created_at < e.created_at
              AND NOT EXISTS((related)-[:PRECEDES]->(e))

            WITH e, related
            ORDER BY related.created_at DESC
            LIMIT 5  // Only connect to 5 most recent predecessors

            CREATE (related)-[r:PRECEDES {
                tenantId: $tenantId,
                created_at: datetime(),
                inferred: true,
                confidence: 0.85
            }]->(e)

            RETURN count(r) AS count
        ";

        try
        {
            var result = await session.RunAsync(query, new { entityId, tenantId });
            var records = await result.ToListAsync();
            var count = records.Count > 0 ? records[0]["count"].As<int>() : 0;

            _logger.LogDebug("Inferred {Count} temporal relationships", count);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to infer temporal relationships");
            return 0;
        }
    }
}
