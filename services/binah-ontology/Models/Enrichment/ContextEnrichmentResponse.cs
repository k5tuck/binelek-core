using System.Text.Json.Serialization;
namespace Binah.Ontology.Models.Enrichment;

public class ContextEnrichmentResponse
{
    [JsonPropertyName("entityId")]
    public string EntityId { get; set; } = string.Empty;

    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();

    [JsonPropertyName("embeddings")]
    public float[]? Embeddings { get; set; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }
}