using System.Text.Json.Serialization;
namespace Binah.Ontology.Models.AIPrediction;

public class CostPredictionRequest
{
    [JsonPropertyName("projectId")]
    public string ProjectId { get; set; } = string.Empty;

    [JsonPropertyName("contractorId")]
    public string? ContractorId { get; set; }

    [JsonPropertyName("budget")]
    public decimal Budget { get; set; }

    [JsonPropertyName("materials")]
    public List<string> Materials { get; set; } = new();

    [JsonPropertyName("region")]
    public string? Region { get; set; }

    [JsonPropertyName("projectType")]
    public string ProjectType { get; set; } = "residential";
}