using Binah.Ontology.Events;
using Binah.Ontology.Services.Interfaces;
using Confluent.Kafka;
using System.Text.Json;

namespace Binah.Ontology.Services;

/// <summary>
/// Publishes schema change events to Kafka for real-time updates
/// </summary>
public class SchemaChangePublisher : ISchemaChangePublisher
{
    private readonly IProducer<string, string> _kafkaProducer;
    private readonly ILogger<SchemaChangePublisher> _logger;
    private const string SchemaChangedTopic = "ontology.schema.changed.v1";

    public SchemaChangePublisher(IProducer<string, string> kafkaProducer, ILogger<SchemaChangePublisher> logger)
    {
        _kafkaProducer = kafkaProducer;
        _logger = logger;
    }

    public async Task PublishSchemaChangedAsync(
        string tenantId,
        int newVersion,
        int previousVersion,
        string changeType,
        string entityType,
        Dictionary<string, object>? changes = null,
        string? changedBy = null,
        string? reason = null)
    {
        try
        {
            var @event = new SchemaChangedEvent
            {
                TenantId = tenantId,
                CorrelationId = Guid.NewGuid().ToString(),
                Payload = new SchemaChangePayload
                {
                    NewVersion = newVersion,
                    PreviousVersion = previousVersion,
                    ChangeType = changeType,
                    EntityType = entityType,
                    Changes = changes ?? new Dictionary<string, object>(),
                    ChangedBy = changedBy ?? "system",
                    Reason = reason ?? string.Empty
                }
            };

            var message = JsonSerializer.Serialize(@event);

            var result = await _kafkaProducer.ProduceAsync(
                SchemaChangedTopic,
                new Message<string, string>
                {
                    Key = tenantId,
                    Value = message
                }
            );

            _logger.LogInformation(
                "Published schema changed event for tenant {TenantId}, version {NewVersion}, change: {ChangeType} on {EntityType}",
                tenantId, newVersion, changeType, entityType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing schema changed event for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task PublishEntityAddedAsync(string tenantId, string entityType, Dictionary<string, object>? metadata = null)
    {
        var version = await GetNextVersionAsync(tenantId);
        await PublishSchemaChangedAsync(
            tenantId,
            version,
            version - 1,
            "EntityAdded",
            entityType,
            metadata,
            reason: $"Added new entity type: {entityType}"
        );
    }

    public async Task PublishEntityModifiedAsync(string tenantId, string entityType, Dictionary<string, object>? changes = null)
    {
        var version = await GetNextVersionAsync(tenantId);
        await PublishSchemaChangedAsync(
            tenantId,
            version,
            version - 1,
            "EntityModified",
            entityType,
            changes,
            reason: $"Modified entity type: {entityType}"
        );
    }

    public async Task PublishEntityRemovedAsync(string tenantId, string entityType)
    {
        var version = await GetNextVersionAsync(tenantId);
        await PublishSchemaChangedAsync(
            tenantId,
            version,
            version - 1,
            "EntityRemoved",
            entityType,
            reason: $"Removed entity type: {entityType}"
        );
    }

    public async Task PublishRelationshipAddedAsync(string tenantId, string relationshipType, string fromEntity, string toEntity)
    {
        var version = await GetNextVersionAsync(tenantId);
        await PublishSchemaChangedAsync(
            tenantId,
            version,
            version - 1,
            "RelationshipAdded",
            relationshipType,
            new Dictionary<string, object>
            {
                { "fromEntity", fromEntity },
                { "toEntity", toEntity }
            },
            reason: $"Added relationship: {fromEntity} -> {toEntity}"
        );
    }

    private async Task<int> GetNextVersionAsync(string tenantId)
    {
        // This should query the database for the current version and increment it
        // For now, returning a simple incrementing value
        // TODO: Implement proper version tracking in database
        return await Task.FromResult(DateTime.UtcNow.Ticks.GetHashCode() % 10000);
    }
}

/// <summary>
/// Interface for schema change publisher
/// </summary>
public interface ISchemaChangePublisher
{
    Task PublishSchemaChangedAsync(
        string tenantId,
        int newVersion,
        int previousVersion,
        string changeType,
        string entityType,
        Dictionary<string, object>? changes = null,
        string? changedBy = null,
        string? reason = null);

    Task PublishEntityAddedAsync(string tenantId, string entityType, Dictionary<string, object>? metadata = null);
    Task PublishEntityModifiedAsync(string tenantId, string entityType, Dictionary<string, object>? changes = null);
    Task PublishEntityRemovedAsync(string tenantId, string entityType);
    Task PublishRelationshipAddedAsync(string tenantId, string relationshipType, string fromEntity, string toEntity);
}
