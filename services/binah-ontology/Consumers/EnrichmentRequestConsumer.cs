using Binah.Contracts.Events;
using Binah.Ontology.Models.Base;
using Binah.Ontology.Repositories;
using Binah.Ontology.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Binah.Ontology.Consumers;

/// <summary>
/// Kafka consumer for enrichment request events
/// Enriches entities with external data sources and updates Neo4j
/// </summary>
public class EnrichmentRequestConsumer : BaseKafkaConsumer<EnrichmentRequestEvent>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IDriver _neo4jDriver;

    public EnrichmentRequestConsumer(
        IConfiguration configuration,
        ILogger<EnrichmentRequestConsumer> logger,
        IServiceScopeFactory serviceScopeFactory,
        IDriver neo4jDriver)
        : base(
            configuration,
            logger,
            configuration["Kafka:Topics:EnrichmentRequests"] ?? "binah.ontology.enrichment.requested",
            configuration["Kafka:ConsumerGroups:EnrichmentRequests"] ?? "binah-ontology-enrichment")
    {
        _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        _neo4jDriver = neo4jDriver ?? throw new ArgumentNullException(nameof(neo4jDriver));
    }

    /// <summary>
    /// Processes enrichment request event
    /// </summary>
    protected override async Task ProcessEventAsync(EnrichmentRequestEvent @event, CancellationToken cancellationToken)
    {
        if (@event == null)
            throw new ArgumentNullException(nameof(@event));

        Logger.LogInformation(
            "Processing enrichment request for entity {EntityId} with enrichment type {EnrichmentType} (tenant: {TenantId})",
            @event.EntityId, @event.EnrichmentType, @event.TenantId);

        // Validate event
        if (string.IsNullOrWhiteSpace(@event.EntityId))
        {
            Logger.LogWarning("Enrichment request event has no entity ID. Skipping.");
            return;
        }

        if (string.IsNullOrWhiteSpace(@event.EnrichmentType))
        {
            Logger.LogWarning("Enrichment request event has no enrichment type. Skipping.");
            return;
        }

        var tenantId = @event.TenantId ?? "core";

        try
        {
            // Create scope for scoped services
            using var scope = _serviceScopeFactory.CreateScope();
            var enrichmentService = scope.ServiceProvider.GetRequiredService<IEnrichmentService>();
            var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            // Step 1: Fetch entity from Neo4j
            Logger.LogDebug("Fetching entity {EntityId} from Neo4j", @event.EntityId);
            var entity = await FetchEntityAsync(tenantId, @event.EntityId);

            if (entity == null)
            {
                Logger.LogWarning(
                    "Entity {EntityId} not found in Neo4j for tenant {TenantId}",
                    @event.EntityId, tenantId);
                return;
            }

            // Step 2: Perform enrichment
            Logger.LogDebug(
                "Enriching entity {EntityId} with type {EnrichmentType}",
                @event.EntityId, @event.EnrichmentType);

            var enrichedData = await enrichmentService.EnrichAsync(
                entity,
                @event.EnrichmentType,
                @event.Parameters);

            if (enrichedData == null || enrichedData.Count == 0)
            {
                Logger.LogWarning(
                    "No enriched data returned for entity {EntityId}",
                    @event.EntityId);
                return;
            }

            Logger.LogInformation(
                "Enriched entity {EntityId} with {Count} properties",
                @event.EntityId, enrichedData.Count);

            // Step 3: Update entity in Neo4j
            await UpdateEntityAsync(tenantId, @event.EntityId, enrichedData);

            // Step 4: Publish entity updated event
            var updatedEvent = new Services.EntityUpdatedEvent
            {
                TenantId = tenantId,
                EntityId = @event.EntityId,
                EntityType = entity.Type,
                ChangedProperties = enrichedData,
                TriggeredBy = "enrichment-service",
                CorrelationId = @event.CorrelationId
            };

            await eventPublisher.PublishEntityUpdatedAsync(updatedEvent);

            Logger.LogInformation(
                "Successfully enriched and updated entity {EntityId}",
                @event.EntityId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex,
                "Failed to process enrichment request for entity {EntityId}",
                @event.EntityId);
            throw; // Re-throw to trigger retry logic
        }
    }

    /// <summary>
    /// Fetches entity from Neo4j by ID and tenant ID
    /// </summary>
    private async Task<Entity?> FetchEntityAsync(string tenantId, string entityId)
    {
        await using var session = _neo4jDriver.AsyncSession();

        var query = @"
            MATCH (e:Entity {id: $id, tenantId: $tenantId})
            WHERE NOT e.is_deleted
            RETURN e
        ";

        try
        {
            var result = await session.RunAsync(query, new { id = entityId, tenantId });
            var records = await result.ToListAsync();

            if (records.Count == 0)
                return null;

            var node = records[0]["e"].As<Neo4j.Driver.INode>();
            return MapToEntity(node);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex,
                "Failed to fetch entity {EntityId} from Neo4j",
                entityId);
            throw;
        }
    }

    /// <summary>
    /// Updates entity in Neo4j with enriched data
    /// </summary>
    private async Task UpdateEntityAsync(
        string tenantId,
        string entityId,
        Dictionary<string, object> enrichedData)
    {
        await using var session = _neo4jDriver.AsyncSession();

        // Build SET clause dynamically
        var setClauses = new List<string>();
        var parameters = new Dictionary<string, object>
        {
            { "id", entityId },
            { "tenantId", tenantId }
        };

        foreach (var kvp in enrichedData)
        {
            var paramName = $"prop_{kvp.Key}";
            setClauses.Add($"e.{kvp.Key} = ${paramName}");
            parameters[paramName] = kvp.Value;
        }

        // Always update the updated_at timestamp
        setClauses.Add("e.updated_at = datetime()");

        var setClause = string.Join(", ", setClauses);
        var query = $@"
            MATCH (e:Entity {{id: $id, tenantId: $tenantId}})
            SET {setClause}
            RETURN e
        ";

        try
        {
            await session.RunAsync(query, parameters);
            Logger.LogDebug(
                "Updated entity {EntityId} in Neo4j with {Count} enriched properties",
                entityId, enrichedData.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex,
                "Failed to update entity {EntityId} in Neo4j",
                entityId);
            throw;
        }
    }

    /// <summary>
    /// Maps Neo4j node to Entity model
    /// </summary>
    private Entity MapToEntity(Neo4j.Driver.INode node)
    {
        var properties = new Dictionary<string, object>();

        foreach (var prop in node.Properties)
        {
            // Skip system properties
            if (prop.Key == "id" || prop.Key == "type" || prop.Key == "tenantId" ||
                prop.Key == "created_at" || prop.Key == "updated_at" || prop.Key == "version" ||
                prop.Key == "source" || prop.Key == "is_deleted")
            {
                continue;
            }

            properties[prop.Key] = prop.Value;
        }

        return new Entity
        {
            Id = node.Properties["id"].As<string>(),
            Type = node.Properties["type"].As<string>(),
            TenantId = node.Properties.GetValueOrDefault("tenantId")?.As<string>(),
            Properties = properties,
            Version = node.Properties.GetValueOrDefault("version")?.As<string>() ?? "1.0",
            CreatedAt = node.Properties.GetValueOrDefault("created_at")?.As<DateTime>() ?? DateTime.UtcNow,
            UpdatedAt = node.Properties.GetValueOrDefault("updated_at")?.As<DateTime>() ?? DateTime.UtcNow,
            Source = node.Properties.GetValueOrDefault("source")?.As<string>() ?? "Unknown",
            IsDeleted = node.Properties.GetValueOrDefault("is_deleted")?.As<bool>() ?? false
        };
    }

    /// <summary>
    /// Increments semantic version (e.g., "1.0" -> "1.1")
    /// </summary>
    private string IncrementVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return "1.0";

        var parts = version.Split('.');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var major) || !int.TryParse(parts[1], out var minor))
        {
            return "1.0";
        }

        return $"{major}.{minor + 1}";
    }
}
