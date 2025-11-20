namespace Binah.Ontology.Models.Lineage;

/// <summary>
/// Audit record for tracking changes
/// </summary>
public class AuditRecord
{
    public string Id { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? PerformedBy { get; set; }
    public DateTime Timestamp { get; set; }
    public string? VersionBefore { get; set; }
    public string? VersionAfter { get; set; }
    public Dictionary<string, object> ChangeDetails { get; set; } = new();
    public string? Source { get; set; }
}