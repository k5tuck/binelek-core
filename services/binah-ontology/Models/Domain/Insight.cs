
using Binah.Ontology.Models.Base;
namespace Binah.Ontology.Models.Domain;

public class Insight : Entity
{
    public Insight()
    {
        Type = "Insight";
    }

    public string InsightType
    {
        get => Properties.TryGetValue("insight_type", out var value) ? value?.ToString() ?? "prediction" : "prediction";
        set => Properties["insight_type"] = value;
    }

    public string ModelName
    {
        get => Properties.TryGetValue("model_name", out var value) ? value?.ToString() ?? string.Empty : string.Empty;
        set => Properties["model_name"] = value;
    }

    public string ModelVersion
    {
        get => Properties.TryGetValue("model_version", out var value) ? value?.ToString() ?? "1.0" : "1.0";
        set => Properties["model_version"] = value;
    }

    public double Confidence
    {
        get => Properties.TryGetValue("confidence", out var value) ? Convert.ToDouble(value) : 0.0;
        set => Properties["confidence"] = value;
    }

    public object? PredictedValue
    {
        get => Properties.TryGetValue("predicted_value", out var value) ? value : null;
        set => Properties["predicted_value"] = value;
    }

    public string? Explanation
    {
        get => Properties.TryGetValue("explanation", out var value) ? value?.ToString() : null;
        set => Properties["explanation"] = value;
    }
}