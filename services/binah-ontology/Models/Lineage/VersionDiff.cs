namespace Binah.Ontology.Models.Lineage;

/// <summary>
/// Difference between two versions
/// </summary>
public class VersionDiff
{
    public string VersionA { get; set; } = string.Empty;
    public string VersionB { get; set; } = string.Empty;
    public Dictionary<string, object> AddedProperties { get; set; } = new();
    public Dictionary<string, object> RemovedProperties { get; set; } = new();
    public Dictionary<string, PropertyChange> ModifiedProperties { get; set; } = new();
    public Dictionary<string, object> UnchangedProperties { get; set; } = new();
}