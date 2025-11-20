namespace Binah.Ontology.Models.Relationship;

/// <summary>
/// GENERATES_INSIGHT relationship: Entity or analysis generates an Insight
/// Direction: Entity -> Insight
/// </summary>
public class GeneratesInsightRelationship : Relationship
{
    public GeneratesInsightRelationship()
    {
        Type = "GENERATES_INSIGHT";
    }

    /// <summary>Model or algorithm that generated the insight</summary>
    public string? GeneratedBy
    {
        get => GetPropertyValue<string>("generated_by");
        set => SetPropertyValue("generated_by", value);
    }

    /// <summary>Insight generation timestamp</summary>
    public DateTime? GeneratedAt
    {
        get => GetPropertyValue<DateTime?>("generated_at");
        set => SetPropertyValue("generated_at", value);
    }
}
