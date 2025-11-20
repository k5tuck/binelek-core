using System.Text.Json.Serialization;

namespace Binah.Ontology.Models.Lineage;


/// <summary>
/// Single version of an entity
/// </summary>
public class EntityVersion
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("changes")]
    public Dictionary<string, object> Changes { get; set; } = new();

    [JsonPropertyName("changedBy")]
    public string ChangedBy { get; set; } = "system";

    [JsonPropertyName("previousVersion")]
    public string? PreviousVersion { get; set; }

    [JsonPropertyName("changedProperties")]
    public Dictionary<string, object> ChangedProperties { get; set; } = new();

    [JsonPropertyName("changeDescription")]
    public string? ChangeDescription { get; set; }
}