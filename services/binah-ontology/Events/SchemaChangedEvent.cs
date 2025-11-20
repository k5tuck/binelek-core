namespace Binah.Ontology.Events;

/// <summary>
/// Event published when ontology schema changes
/// Enables real-time updates to UI and documentation
/// </summary>
public class SchemaChangedEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public string EventType { get; set; } = "ontology.schema.changed.v1";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string TenantId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public SchemaChangePayload Payload { get; set; } = new();
}

public class SchemaChangePayload
{
    public int NewVersion { get; set; }
    public int PreviousVersion { get; set; }
    public string ChangeType { get; set; } = string.Empty; // EntityAdded, EntityModified, EntityRemoved, RelationshipAdded, etc.
    public string EntityType { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Changes { get; set; } = new();
    public string ChangedBy { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
