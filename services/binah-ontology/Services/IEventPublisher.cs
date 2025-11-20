using System.Threading.Tasks;

namespace Binah.Ontology.Services;

/// <summary>
/// Service interface for publishing events to Kafka message bus
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes an entity created event
    /// </summary>
    /// <param name="event">The event data</param>
    /// <returns>True if published successfully</returns>
    Task<bool> PublishEntityCreatedAsync(EntityCreatedEvent @event);

    /// <summary>
    /// Publishes an entity updated event
    /// </summary>
    /// <param name="event">The event data</param>
    /// <returns>True if published successfully</returns>
    Task<bool> PublishEntityUpdatedAsync(EntityUpdatedEvent @event);

    /// <summary>
    /// Publishes an entity deleted event
    /// </summary>
    /// <param name="event">The event data</param>
    /// <returns>True if published successfully</returns>
    Task<bool> PublishEntityDeletedAsync(EntityDeletedEvent @event);

    /// <summary>
    /// Publishes a relationship created event
    /// </summary>
    /// <param name="event">The event data</param>
    /// <returns>True if published successfully</returns>
    Task<bool> PublishRelationshipCreatedAsync(RelationshipCreatedEvent @event);

    /// <summary>
    /// Publishes a relationship deleted event
    /// </summary>
    /// <param name="event">The event data</param>
    /// <returns>True if published successfully</returns>
    Task<bool> PublishRelationshipDeletedAsync(RelationshipDeletedEvent @event);
}

/// <summary>
/// Base event class for all ontology events
/// </summary>
public abstract class OntologyEvent
{
    /// <summary>Unique event ID</summary>
    public string EventId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Event type</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>Timestamp when event was created</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Correlation ID for tracing</summary>
    public string? CorrelationId { get; set; }

    /// <summary>User or system that triggered the event</summary>
    public string? TriggeredBy { get; set; }

    /// <summary>Tenant ID for multi-tenancy</summary>
    public string? TenantId { get; set; }
}

/// <summary>
/// Event published when an entity is created
/// </summary>
public class EntityCreatedEvent : OntologyEvent
{
    public EntityCreatedEvent()
    {
        EventType = "entity.created";
    }

    /// <summary>Created entity ID</summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>Entity type</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Entity properties</summary>
    public Dictionary<string, object> Properties { get; set; } = new();

    /// <summary>Initial version</summary>
    public string Version { get; set; } = "1.0";
}

/// <summary>
/// Event published when an entity is updated
/// </summary>
public class EntityUpdatedEvent : OntologyEvent
{
    public EntityUpdatedEvent()
    {
        EventType = "entity.updated";
    }

    /// <summary>Updated entity ID</summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>Entity type</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Properties that changed</summary>
    public Dictionary<string, object> ChangedProperties { get; set; } = new();

    /// <summary>Previous version</summary>
    public string PreviousVersion { get; set; } = string.Empty;

    /// <summary>New version</summary>
    public string NewVersion { get; set; } = string.Empty;
}

/// <summary>
/// Event published when an entity is deleted
/// </summary>
public class EntityDeletedEvent : OntologyEvent
{
    public EntityDeletedEvent()
    {
        EventType = "entity.deleted";
    }

    /// <summary>Deleted entity ID</summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>Entity type</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Final version before deletion</summary>
    public string Version { get; set; } = string.Empty;
}

/// <summary>
/// Event published when a relationship is created
/// </summary>
public class RelationshipCreatedEvent : OntologyEvent
{
    public RelationshipCreatedEvent()
    {
        EventType = "relationship.created";
    }

    /// <summary>Relationship type</summary>
    public string RelationshipType { get; set; } = string.Empty;

    /// <summary>Source entity ID</summary>
    public string FromEntityId { get; set; } = string.Empty;

    /// <summary>Target entity ID</summary>
    public string ToEntityId { get; set; } = string.Empty;

    /// <summary>Relationship properties</summary>
    public Dictionary<string, object>? Properties { get; set; }
}

/// <summary>
/// Event published when a relationship is deleted
/// </summary>
public class RelationshipDeletedEvent : OntologyEvent
{
    public RelationshipDeletedEvent()
    {
        EventType = "relationship.deleted";
    }

    /// <summary>Relationship type</summary>
    public string RelationshipType { get; set; } = string.Empty;

    /// <summary>Source entity ID</summary>
    public string FromEntityId { get; set; } = string.Empty;

    /// <summary>Target entity ID</summary>
    public string ToEntityId { get; set; } = string.Empty;
}
