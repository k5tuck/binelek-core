using System.Text.Json.Serialization;
namespace Binah.Ontology.Models.Query;

public class GraphQueryRequest
{
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public Dictionary<string, object>? Parameters { get; set; }
}