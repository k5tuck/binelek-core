
using Binah.Ontology.Models.Base;
namespace Binah.Ontology.Models.Domain;


public class RiskEvent : Entity
{
    public RiskEvent()
    {
        Type = "RiskEvent";
    }

    public string RiskCategory
    {
        get => Properties.TryGetValue("risk_category", out var value) ? value?.ToString() ?? "operational" : "operational";
        set => Properties["risk_category"] = value;
    }

    public double Severity
    {
        get => Properties.TryGetValue("severity", out var value) ? Convert.ToDouble(value) : 0.0;
        set => Properties["severity"] = value;
    }

    public string? Description
    {
        get => Properties.TryGetValue("description", out var value) ? value?.ToString() : null;
        set => Properties["description"] = value;
    }

    public DateTime DetectedAt
    {
        get => Properties.TryGetValue("detected_at", out var value) 
            ? DateTime.Parse(value?.ToString() ?? DateTime.UtcNow.ToString("O")) 
            : DateTime.UtcNow;
        set => Properties["detected_at"] = value.ToString("O");
    }

    public bool IsResolved
    {
        get => Properties.TryGetValue("is_resolved", out var value) ? Convert.ToBoolean(value) : false;
        set => Properties["is_resolved"] = value;
    }
}