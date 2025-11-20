namespace Binah.Ontology.Models.Exceptions;

/// <summary>
/// Exception thrown when relationship creation fails
/// </summary>
public class RelationshipCreationException : OntologyException
{
    public string RelationshipType { get; set; }
    public string FromEntityId { get; set; }
    public string ToEntityId { get; set; }

    public RelationshipCreationException(string relationshipType, string message)
        : base($"Failed to create relationship '{relationshipType}': {message}",
               "RELATIONSHIP_CREATION_FAILED")
    {
        RelationshipType = relationshipType;
        FromEntityId = string.Empty;
        ToEntityId = string.Empty;
    }

    public RelationshipCreationException(string relationshipType, string message, System.Exception innerException)
        : base($"Failed to create relationship '{relationshipType}': {message}", innerException)
    {
        RelationshipType = relationshipType;
        FromEntityId = string.Empty;
        ToEntityId = string.Empty;
        ErrorCode = "RELATIONSHIP_CREATION_FAILED";
    }

    public RelationshipCreationException(
        string relationshipType,
        string fromEntityId,
        string toEntityId,
        string message)
        : base($"Failed to create relationship '{relationshipType}' from '{fromEntityId}' to '{toEntityId}': {message}",
               "RELATIONSHIP_CREATION_FAILED")
    {
        RelationshipType = relationshipType;
        FromEntityId = fromEntityId;
        ToEntityId = toEntityId;
    }

    public RelationshipCreationException(
        string relationshipType,
        string fromEntityId,
        string toEntityId,
        string message,
        System.Exception innerException)
        : base($"Failed to create relationship '{relationshipType}' from '{fromEntityId}' to '{toEntityId}': {message}",
               innerException)
    {
        RelationshipType = relationshipType;
        FromEntityId = fromEntityId;
        ToEntityId = toEntityId;
        ErrorCode = "RELATIONSHIP_CREATION_FAILED";
    }
}