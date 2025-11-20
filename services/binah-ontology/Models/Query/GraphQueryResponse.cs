using System.Text.Json.Serialization;
namespace Binah.Ontology.Models.Query;

public class GraphQueryResponse
{
    [JsonPropertyName("results")]
    public List<Dictionary<string, object>> Results { get; set; } = new();

    [JsonPropertyName("executionTime")]
    public long ExecutionTimeMs { get; set; }

    [JsonPropertyName("recordCount")]
    public int RecordCount { get; set; }
}