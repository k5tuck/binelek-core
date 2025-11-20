namespace Binah.Ontology.Models.Exceptions;

/// <summary>
/// Exception thrown when an entity is not found in the graph
/// </summary>
public class EntityNotFoundException : OntologyException
{
    public string EntityId { get; set; }

    public EntityNotFoundException(string entityId)
        : base($"Entity with ID '{entityId}' was not found", "ENTITY_NOT_FOUND")
    {
        EntityId = entityId;
    }

    public EntityNotFoundException(string entityId, string message)
        : base(message, "ENTITY_NOT_FOUND")
    {
        EntityId = entityId;
    }
}