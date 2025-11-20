namespace Binah.Ontology.Models.Exceptions;

/// <summary>
/// Exception thrown when entity update fails
/// </summary>
public class EntityUpdateException : OntologyException
{
    public string EntityId { get; set; }

    public EntityUpdateException(string entityId, string message)
        : base($"Failed to update entity '{entityId}': {message}", "ENTITY_UPDATE_FAILED")
    {
        EntityId = entityId;
    }

    public EntityUpdateException(string entityId, string message, System.Exception innerException)
        : base($"Failed to update entity '{entityId}': {message}", innerException)
    {
        EntityId = entityId;
        ErrorCode = "ENTITY_UPDATE_FAILED";
    }
}