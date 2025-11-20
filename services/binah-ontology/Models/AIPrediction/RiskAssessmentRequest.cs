using System.Text.Json.Serialization;
namespace Binah.Ontology.Models.AIPrediction;

public class RiskAssessmentRequest
{
    [JsonPropertyName("entityId")]
    public string EntityId { get; set; } = string.Empty;

    [JsonPropertyName("entityType")]
    public string EntityType { get; set; } = string.Empty;

    [JsonPropertyName("assessmentType")]
    public string AssessmentType { get; set; } = "comprehensive";
}