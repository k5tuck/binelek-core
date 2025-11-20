using System.Text.Json.Serialization;
namespace Binah.Ontology.Models.AIPrediction;

public class CostPredictionResponse
{
    [JsonPropertyName("projectId")]
    public string ProjectId { get; set; } = string.Empty;

    [JsonPropertyName("predictedCost")]
    public decimal PredictedCost { get; set; }

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    [JsonPropertyName("riskLevel")]
    public string RiskLevel { get; set; } = "low";

    [JsonPropertyName("insightRef")]
    public string? InsightRef { get; set; }

    [JsonPropertyName("factors")]
    public Dictionary<string, double> Factors { get; set; } = new();
}