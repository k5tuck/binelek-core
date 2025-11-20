using System.Text.Json.Serialization;
namespace Binah.Ontology.Models.AIPrediction;

public class RiskAssessmentResponse
{
    [JsonPropertyName("entityId")]
    public string EntityId { get; set; } = string.Empty;

    [JsonPropertyName("riskScore")]
    public double RiskScore { get; set; }

    [JsonPropertyName("riskLevel")]
    public string RiskLevel { get; set; } = "low";

    [JsonPropertyName("riskCategories")]
    public Dictionary<string, double> RiskCategories { get; set; } = new();

    [JsonPropertyName("mitigationStrategies")]
    public List<string> MitigationStrategies { get; set; } = new();
}
