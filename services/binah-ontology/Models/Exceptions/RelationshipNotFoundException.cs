namespace Binah.Ontology.Models.Exceptions;

/// <summary>
/// Exception thrown when a relationship is not found
/// </summary>
public class RelationshipNotFoundException : OntologyException
{
    public string RelationshipType { get; set; }
    public string FromEntityId { get; set; }
    public string ToEntityId { get; set; }

    public RelationshipNotFoundException(string relationshipType, string fromEntityId, string toEntityId)
        : base($"Relationship '{relationshipType}' from '{fromEntityId}' to '{toEntityId}' was not found",
               "RELATIONSHIP_NOT_FOUND")
    {
        RelationshipType = relationshipType;
        FromEntityId = fromEntityId;
        ToEntityId = toEntityId;
    }
}