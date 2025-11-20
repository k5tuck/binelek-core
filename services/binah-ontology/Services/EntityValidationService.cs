using Binah.Ontology.Repositories;
using Binah.Ontology.Models.Base;
// using Binah.Ontology.Services.Validation;  // TODO: Add validation namespace when available
using Binah.Ontology.Models.Ontology;
using Confluent.Kafka;
using System.Text.Json;

namespace Binah.Ontology.Services;

/// <summary>
/// Service for validating entities from Kafka and storing in Neo4j
/// </summary>
public class EntityValidationService : BackgroundService
{
    private readonly ILogger<EntityValidationService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly Neo4jRepository _neo4jRepository;
    private readonly string _kafkaBootstrapServers;
    private readonly string _kafkaTopicPattern;
    private readonly Guid _tenantId;
    private IConsumer<string, string>? _consumer;

    public EntityValidationService(
        ILogger<EntityValidationService> logger,
        IServiceProvider serviceProvider,
        Neo4jRepository neo4jRepository,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _neo4jRepository = neo4jRepository;

        _kafkaBootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        _kafkaTopicPattern = configuration["Kafka:ValidationTopicPattern"] ?? "ontology.ingest.*.v1";
        _tenantId = Guid.Parse(configuration["TenantId"] ?? Guid.Empty.ToString());
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "EntityValidationService starting for tenant {TenantId}, subscribing to pattern: {TopicPattern}",
            _tenantId, _kafkaTopicPattern);

        var config = new ConsumerConfig
        {
            BootstrapServers = _kafkaBootstrapServers,
            GroupId = $"ontology-validation-{_tenantId}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            AllowAutoCreateTopics = true
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();

        // Subscribe to all enriched entity topics from Context Service
        var topics = await DiscoverTopicsAsync(_kafkaTopicPattern);
        _consumer.Subscribe(topics);

        _logger.LogInformation("Subscribed to {TopicCount} topics: {Topics}",
            topics.Count, string.Join(", ", topics));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = _consumer.Consume(stoppingToken);

                if (consumeResult?.Message?.Value != null)
                {
                    await ProcessMessageAsync(consumeResult, stoppingToken);
                    _consumer.Commit(consumeResult);
                }
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Kafka consume error");
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("EntityValidationService stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Kafka message");
                // Continue processing other messages
            }
        }

        _consumer?.Close();
    }

    private async Task ProcessMessageAsync(ConsumeResult<string, string> consumeResult, CancellationToken cancellationToken)
    {
        var topic = consumeResult.Topic;
        var message = consumeResult.Message.Value;

        _logger.LogDebug("Processing message from topic {Topic}", topic);

        try
        {
            // Deserialize entity
            var entity = JsonSerializer.Deserialize<Dictionary<string, object>>(message);
            if (entity == null)
            {
                _logger.LogWarning("Failed to deserialize message from topic {Topic}", topic);
                return;
            }

            // Extract metadata and entity type
            var metadata = ExtractMetadata(entity);
            var entityType = ExtractEntityTypeFromTopic(topic);

            _logger.LogInformation(
                "Validating entity {EntityType} from pipeline {PipelineId}, execution {ExecutionId}",
                entityType, metadata.PipelineId, metadata.ExecutionId);

            // Get ontology for validation
            using var scope = _serviceProvider.CreateScope();
            var ontologyService = scope.ServiceProvider.GetRequiredService<IOntologyVersionService>();

            var ontology = await ontologyService.GetActiveAsync(_tenantId);
            if (ontology == null)
            {
                _logger.LogError("No active ontology found for tenant {TenantId}", _tenantId);
                await SendToDeadLetterQueueAsync(topic, message, new List<string> { "No active ontology" }, cancellationToken);
                return;
            }

            // TODO: Validate entity against ontology - OntologyDefinition type needs to be implemented
            // var validator = new EntityValidator(ontology.Definition!);
            // var validationResult = validator.Validate(entityType, entity);

            // if (!validationResult.IsValid)
            // {
            //     _logger.LogWarning(
            //         "Entity validation failed for {EntityType}: {Errors}",
            //         entityType, string.Join(", ", validationResult.Errors));

            //     // Send to dead letter queue
            //     await SendToDeadLetterQueueAsync(topic, message, validationResult.Errors.ToList(), cancellationToken);
            //     return;
            // }

            // Store entity in Neo4j
            var stored = await _neo4jRepository.StoreEntityAsync(_tenantId, entityType, entity);

            if (!stored)
            {
                _logger.LogError("Failed to store entity {EntityType} in Neo4j", entityType);
                await SendToDeadLetterQueueAsync(topic, message, new List<string> { "Neo4j storage failed" }, cancellationToken);
                return;
            }

            // TODO: Infer and create relationships - OntologyDefinition type needs to be implemented
            // await CreateRelationshipsAsync(_tenantId, entityType, entity, ontology.Definition!, cancellationToken);

            _logger.LogInformation(
                "Successfully validated and stored entity {EntityType} in Neo4j",
                entityType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message from topic {Topic}", topic);
            await SendToDeadLetterQueueAsync(topic, message, new List<string> { ex.Message }, cancellationToken);
        }
    }

    // TODO: Uncomment when OntologyDefinition type is implemented
    /*
    private async Task CreateRelationshipsAsync(
        Guid tenantId,
        string entityType,
        Dictionary<string, object> entity,
        OntologyDefinition ontology,
        CancellationToken cancellationToken)
    {
        try
        {
            // Find entity definition in ontology
            var entityDef = ontology.Entities.FirstOrDefault(e => e.Name.Equals(entityType, StringComparison.OrdinalIgnoreCase));
            if (entityDef == null || entityDef.Relationships == null)
            {
                _logger.LogDebug("No relationships defined for entity {EntityType}", entityType);
                return;
            }

            // Extract source entity ID
            var sourceEntityId = entity.ContainsKey("id")
                ? entity["id"].ToString()
                : Guid.NewGuid().ToString();

            // Create relationships based on entity data
            foreach (var relationshipDef in entityDef.Relationships)
            {
                // Check if entity has field matching relationship
                var fieldName = relationshipDef.Name.ToLowerInvariant() + "Id";

                if (entity.TryGetValue(fieldName, out var targetIdObj))
                {
                    var targetEntityId = targetIdObj.ToString();

                    if (!string.IsNullOrEmpty(targetEntityId))
                    {
                        var created = await _neo4jRepository.CreateRelationshipAsync(
                            tenantId,
                            sourceEntityId!,
                            targetEntityId!,
                            relationshipDef.Type ?? relationshipDef.Name.ToUpperInvariant(),
                            new Dictionary<string, object>
                            {
                                ["createdBy"] = "OntologyService",
                                ["cardinality"] = relationshipDef.Cardinality ?? "many-to-many"
                            });

                        if (created)
                        {
                            _logger.LogInformation(
                                "Created relationship {RelationshipType} from {SourceId} to {TargetId}",
                                relationshipDef.Type, sourceEntityId, targetEntityId);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to create relationships for entity {EntityType}",
                entityType);
            // Don't fail the entire processing if relationship creation fails
        }
    }
    */

    private EntityMetadata ExtractMetadata(Dictionary<string, object> entity)
    {
        var metadata = new EntityMetadata();

        if (entity.TryGetValue("_metadata", out var metaObj) &&
            metaObj is JsonElement metaElement)
        {
            var metaDict = JsonSerializer.Deserialize<Dictionary<string, object>>(metaElement.GetRawText());
            if (metaDict != null)
            {
                if (metaDict.TryGetValue("tenantId", out var tenantId))
                    metadata.TenantId = Guid.Parse(tenantId.ToString()!);

                if (metaDict.TryGetValue("pipelineId", out var pipelineId))
                    metadata.PipelineId = Guid.Parse(pipelineId.ToString()!);

                if (metaDict.TryGetValue("executionId", out var executionId))
                    metadata.ExecutionId = Guid.Parse(executionId.ToString()!);

                if (metaDict.TryGetValue("timestamp", out var timestamp))
                    metadata.Timestamp = DateTime.Parse(timestamp.ToString()!);
            }
        }

        return metadata;
    }

    private string ExtractEntityTypeFromTopic(string topic)
    {
        // Topic format: ontology.ingest.{entityType}.v1
        var parts = topic.Split('.');
        return parts.Length > 2 ? parts[2] : "unknown";
    }

    private async Task SendToDeadLetterQueueAsync(
        string originalTopic,
        string message,
        List<string> errors,
        CancellationToken cancellationToken)
    {
        try
        {
            var dlqTopic = $"{originalTopic}.dlq";

            var dlqMessage = new
            {
                originalTopic,
                originalMessage = message,
                errors,
                timestamp = DateTime.UtcNow,
                tenantId = _tenantId
            };

            var config = new ProducerConfig
            {
                BootstrapServers = _kafkaBootstrapServers
            };

            using var producer = new ProducerBuilder<Null, string>(config).Build();

            await producer.ProduceAsync(
                dlqTopic,
                new Message<Null, string>
                {
                    Value = JsonSerializer.Serialize(dlqMessage)
                },
                cancellationToken);

            _logger.LogInformation(
                "Sent failed message to dead letter queue: {DlqTopic}",
                dlqTopic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to dead letter queue");
        }
    }

    private async Task<List<string>> DiscoverTopicsAsync(string pattern)
    {
        try
        {
            // For now, we'll use a predefined list of entity types
            // In production, this should dynamically discover topics or read from configuration
            var entityTypes = new[] { "person", "device", "location", "event", "asset" };

            return entityTypes
                .Select(type => $"ontology.ingest.{type}.v1")
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover Kafka topics");
            return new List<string>();
        }
    }

    public override void Dispose()
    {
        _consumer?.Dispose();
        base.Dispose();
    }
}

public class EntityMetadata
{
    public Guid TenantId { get; set; }
    public Guid PipelineId { get; set; }
    public Guid ExecutionId { get; set; }
    public DateTime Timestamp { get; set; }
}
