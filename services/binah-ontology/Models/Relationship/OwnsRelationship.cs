using Binah.Ontology.Models.Exceptions;

namespace Binah.Ontology.Models.Relationship;

/// <summary>
/// OWNS relationship: Entity owns Property
/// Direction: Property -> Entity
/// </summary>
public class OwnsRelationship : Relationship
{
    public OwnsRelationship()
    {
        Type = "OWNS";
    }

    /// <summary>Ownership percentage (0.0 - 1.0)</summary>
    public double? OwnershipPct
    {
        get => GetPropertyValue<double?>("ownership_pct");
        set
        {
            if (value.HasValue && (value < 0 || value > 1))
                throw new RelationshipCreationException("OWNS", "Ownership percentage must be between 0 and 1");
            SetPropertyValue("ownership_pct", value);
        }
    }

    /// <summary>Supporting source documents</summary>
    public List<string>? SourceDocuments
    {
        get => GetPropertyValue<List<string>>("source_documents");
        set => SetPropertyValue("source_documents", value);
    }
}