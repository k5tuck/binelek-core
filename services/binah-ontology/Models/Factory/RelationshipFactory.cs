using Binah.Ontology.Models.Relationship;

namespace Binah.Ontology.Models.Factory;

/// <summary>
/// Factory class for creating relationship instances based on type
/// </summary>
public static class RelationshipFactory
{
    /// <summary>
    /// Creates a relationship instance based on type string
    /// </summary>
    public static Binah.Ontology.Models.Relationship.Relationship CreateRelationship(string type, string fromEntityId, string toEntityId)
    {
        var relationship = type.ToUpperInvariant() switch
        {
            "OWNS" => new OwnsRelationship(),
            "FINANCED_BY" => new FinancedByRelationship(),
            "LOCATED_IN" => new LocatedInRelationship(),
            "ADJACENT_TO" => new AdjacentToRelationship(),
            "HAS_PERMIT" => new HasPermitRelationship(),
            "DEVELOPED_BY" => new DevelopedByRelationship(),
            "CONTROLS" => new ControlsRelationship(),
            "PRINCIPAL" => new PrincipalRelationship(),
            "RELATED_TO" => new RelatedToRelationship(),
            _ => new Binah.Ontology.Models.Relationship.Relationship { Type = type }
        };

        relationship.Id = Guid.NewGuid().ToString();
        relationship.FromEntityId = fromEntityId;
        relationship.ToEntityId = toEntityId;
        relationship.CreatedAt = DateTime.UtcNow;

        return relationship;
    }

    /// <summary>
    /// Creates a relationship with initial properties
    /// </summary>
    public static Binah.Ontology.Models.Relationship.Relationship CreateRelationship(
        string type,
        string fromEntityId,
        string toEntityId,
        Dictionary<string, object>? properties = null,
        double confidenceScore = 1.0,
        DateTime? sinceDate = null)
    {
        var relationship = CreateRelationship(type, fromEntityId, toEntityId);

        if (properties != null)
        {
            relationship.Properties = properties;
        }

        relationship.ConfidenceScore = confidenceScore;
        relationship.SinceDate = sinceDate;

        return relationship;
    }
}
