using Binah.Contracts.Events;
using Binah.Ontology.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Binah.Ontology.Consumers;

/// <summary>
/// Kafka consumer for pipeline completion events
/// Triggers post-ingestion processing: classification, relationship inference, metadata updates
/// </summary>
public class PipelineCompletionConsumer : BaseKafkaConsumer<PipelineCompletionEvent>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IDriver _neo4jDriver;

    public PipelineCompletionConsumer(
        IConfiguration configuration,
        ILogger<PipelineCompletionConsumer> logger,
        IServiceScopeFactory serviceScopeFactory,
        IDriver neo4jDriver)
        : base(
            configuration,
            logger,
            configuration["Kafka:Topics:PipelineCompletion"] ?? "binah.pipeline.execution.completed",
            configuration["Kafka:ConsumerGroups:PipelineCompletion"] ?? "binah-ontology-pipeline-completion")
    {
        _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        _neo4jDriver = neo4jDriver ?? throw new ArgumentNullException(nameof(neo4jDriver));
    }

    /// <summary>
    /// Processes pipeline completion event
    /// </summary>
    protected override async Task ProcessEventAsync(PipelineCompletionEvent @event, CancellationToken cancellationToken)
    {
        if (@event == null)
            throw new ArgumentNullException(nameof(@event));

        Logger.LogInformation(
            "Processing pipeline completion event: Pipeline={PipelineName}, ExecutionId={ExecutionId}, " +
            "Status={Status}, Entities={EntityCount} (tenant: {TenantId})",
            @event.PipelineName, @event.ExecutionId, @event.Status,
            @event.EntitiesCreated?.Count ?? 0, @event.TenantId);

        // Skip processing if pipeline failed
        if (@event.Status == "failed")
        {
            Logger.LogWarning(
                "Pipeline {PipelineName} execution {ExecutionId} failed: {ErrorMessage}. Skipping post-processing.",
                @event.PipelineName, @event.ExecutionId, @event.ErrorMessage);
            return;
        }

        // Skip if no entities were created
        if (@event.EntitiesCreated == null || @event.EntitiesCreated.Count == 0)
        {
            Logger.LogInformation(
                "Pipeline {PipelineName} execution {ExecutionId} created no entities. Skipping post-processing.",
                @event.PipelineName, @event.ExecutionId);
            return;
        }

        var tenantId = @event.TenantId ?? "core";

        try
        {
            // Create scope for scoped services
            using var scope = _serviceScopeFactory.CreateScope();
            var classificationService = scope.ServiceProvider.GetRequiredService<IClassificationService>();
            var inferenceService = scope.ServiceProvider.GetRequiredService<IRelationshipInferenceService>();

            // Step 1: Classify new entities
            Logger.LogInformation(
                "Classifying {Count} entities from pipeline {PipelineName}",
                @event.EntitiesCreated.Count, @event.PipelineName);

            var classificationResults = new List<Dictionary<string, object>>();
            foreach (var entityId in @event.EntitiesCreated)
            {
                try
                {
                    var result = await classificationService.ClassifyEntityAsync(tenantId, entityId);
                    classificationResults.Add(result);
                    Logger.LogDebug("Classified entity {EntityId}", entityId);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to classify entity {EntityId}", entityId);
                    // Continue with other entities
                }
            }

            Logger.LogInformation(
                "Classified {Count}/{Total} entities successfully",
                classificationResults.Count, @event.EntitiesCreated.Count);

            // Step 2: Infer relationships between new entities and existing ones
            Logger.LogInformation(
                "Inferring relationships for {Count} entities",
                @event.EntitiesCreated.Count);

            var relationshipsCreated = await inferenceService.InferRelationshipsAsync(
                tenantId,
                @event.EntitiesCreated);

            Logger.LogInformation(
                "Inferred {Count} relationships for {EntityCount} entities",
                relationshipsCreated, @event.EntitiesCreated.Count);

            // Step 3: Update pipeline execution metadata in Neo4j
            await UpdatePipelineMetadataAsync(
                tenantId,
                @event.PipelineId,
                @event.ExecutionId,
                @event.EntitiesCreated.Count,
                relationshipsCreated,
                @event.CompletedAt,
                @event.DurationMs);

            Logger.LogInformation(
                "Successfully processed pipeline completion for {PipelineName} (ExecutionId: {ExecutionId})",
                @event.PipelineName, @event.ExecutionId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex,
                "Failed to process pipeline completion for {PipelineName} (ExecutionId: {ExecutionId})",
                @event.PipelineName, @event.ExecutionId);
            throw; // Re-throw to trigger retry logic
        }
    }

    /// <summary>
    /// Updates pipeline execution metadata in Neo4j
    /// </summary>
    private async Task UpdatePipelineMetadataAsync(
        string tenantId,
        string pipelineId,
        string executionId,
        int entityCount,
        int relationshipCount,
        DateTime completedAt,
        long durationMs)
    {
        await using var session = _neo4jDriver.AsyncSession();

        // First, check if pipeline node exists, create if not
        var ensurePipelineQuery = @"
            MERGE (p:Pipeline {id: $pipelineId, tenantId: $tenantId})
            ON CREATE SET
                p.created_at = datetime(),
                p.executions = []
            RETURN p
        ";

        // Then, update with execution metadata
        var updateQuery = @"
            MATCH (p:Pipeline {id: $pipelineId, tenantId: $tenantId})
            SET p.last_execution_id = $executionId,
                p.last_execution_completed_at = datetime($completedAt),
                p.last_execution_entity_count = $entityCount,
                p.last_execution_relationship_count = $relationshipCount,
                p.last_execution_duration_ms = $durationMs,
                p.updated_at = datetime(),
                p.executions = COALESCE(p.executions, []) + [{
                    execution_id: $executionId,
                    completed_at: datetime($completedAt),
                    entity_count: $entityCount,
                    relationship_count: $relationshipCount,
                    duration_ms: $durationMs
                }]
            RETURN p
        ";

        try
        {
            // Ensure pipeline exists
            await session.RunAsync(ensurePipelineQuery, new { pipelineId, tenantId });

            // Update with execution metadata
            await session.RunAsync(updateQuery, new
            {
                pipelineId,
                tenantId,
                executionId,
                completedAt = completedAt.ToString("O"),
                entityCount,
                relationshipCount,
                durationMs
            });

            Logger.LogDebug(
                "Updated pipeline metadata for {PipelineId}: {EntityCount} entities, {RelationshipCount} relationships",
                pipelineId, entityCount, relationshipCount);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex,
                "Failed to update pipeline metadata for {PipelineId}",
                pipelineId);
            // Don't throw - this is metadata update, not critical
        }
    }
}
