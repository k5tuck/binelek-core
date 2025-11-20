namespace Binah.Ontology.Models.Relationship;

 /// <summary>
/// HAS_RISK_EVENT relationship: Entity has an associated RiskEvent
/// Direction: Entity -> RiskEvent
/// </summary>
public class HasRiskEventRelationship : Relationship
{
    public HasRiskEventRelationship()
    {
        Type = "HAS_RISK_EVENT";
    }

    /// <summary>Impact level: low, medium, high, critical</summary>
    public string? ImpactLevel
    {
        get => GetPropertyValue<string>("impact_level");
        set => SetPropertyValue("impact_level", value);
    }

    /// <summary>Whether the risk has been mitigated</summary>
    public bool IsMitigated
    {
        get => GetPropertyValue<bool>("is_mitigated", false);
        set => SetPropertyValue("is_mitigated", value);
    }
}