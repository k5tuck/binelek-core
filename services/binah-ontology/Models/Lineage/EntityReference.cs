using System.Text.Json.Serialization;
namespace Binah.Ontology.Models.Lineage;

public class EntityReference
{
    [JsonPropertyName("entityId")]
    public string EntityId { get; set; } = string.Empty;

    [JsonPropertyName("entityType")]
    public string EntityType { get; set; } = string.Empty;

    [JsonPropertyName("relevance")]
    public double Relevance { get; set; }
}