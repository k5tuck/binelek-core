namespace Binah.Ontology.Models.Relationship;

/// <summary>
/// HAS_ASSESSMENT relationship: Property has an Assessment
/// Direction: Property -> Assessment
/// </summary>
public class HasAssessmentRelationship : Relationship
{
    public HasAssessmentRelationship()
    {
        Type = "HAS_ASSESSMENT";
    }

    /// <summary>Tax year for this assessment</summary>
    public int? TaxYear
    {
        get => GetPropertyValue<int?>("tax_year");
        set => SetPropertyValue("tax_year", value);
    }

    /// <summary>Whether this is the current active assessment</summary>
    public bool IsCurrent
    {
        get => GetPropertyValue<bool>("is_current", false);
        set => SetPropertyValue("is_current", value);
    }
}