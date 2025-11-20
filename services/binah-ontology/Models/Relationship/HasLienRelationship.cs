namespace Binah.Ontology.Models.Relationship;

/// <summary>
/// HAS_LIEN relationship: Property has a Lien
/// Direction: Property -> Lien
/// </summary>
public class HasLienRelationship : Relationship
{
    public HasLienRelationship()
    {
        Type = "HAS_LIEN";
    }

    /// <summary>When the lien was first recorded</summary>
    public DateTime? RecordedDate
    {
        get => GetPropertyValue<DateTime?>("recorded_date");
        set => SetPropertyValue("recorded_date", value);
    }

    /// <summary>Current lien status</summary>
    public string? LienStatus
    {
        get => GetPropertyValue<string>("lien_status");
        set => SetPropertyValue("lien_status", value);
    }
}