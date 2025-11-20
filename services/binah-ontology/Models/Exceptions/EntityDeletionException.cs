namespace Binah.Ontology.Models.Exceptions;

/// <summary>
/// Exception thrown when entity deletion fails
/// </summary>
public class EntityDeletionException : OntologyException
{
    public string EntityId { get; set; }

    public EntityDeletionException(string entityId, string message)
        : base($"Failed to delete entity '{entityId}': {message}", "ENTITY_DELETION_FAILED")
    {
        EntityId = entityId;
    }

    public EntityDeletionException(string entityId, string message, System.Exception innerException)
        : base($"Failed to delete entity '{entityId}': {message}", innerException)
    {
        EntityId = entityId;
        ErrorCode = "ENTITY_DELETION_FAILED";
    }
}