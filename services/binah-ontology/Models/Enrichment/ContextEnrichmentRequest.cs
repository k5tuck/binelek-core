using System.Text.Json.Serialization;
namespace Binah.Ontology.Models.Enrichment;

public class ContextEnrichmentRequest
{
    [JsonPropertyName("entityId")]
    public string EntityId { get; set; } = string.Empty;

    [JsonPropertyName("entityType")]
    public string EntityType { get; set; } = string.Empty;

    [JsonPropertyName("rawData")]
    public Dictionary<string, object> RawData { get; set; } = new();

    [JsonPropertyName("enrichmentTypes")]
    public List<string> EnrichmentTypes { get; set; } = new();
}