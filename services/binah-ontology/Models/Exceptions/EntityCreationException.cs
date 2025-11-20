
using Binah.Ontology.Models.Base;
namespace Binah.Ontology.Models.Exceptions;

/// <summary>
/// Exception thrown when entity creation fails
/// </summary>
public class EntityCreationException : OntologyException
{
    public string EntityType { get; set; }
    public Dictionary<string, object>? Properties { get; set; }

    public EntityCreationException(string entityType, string message)
        : base($"Failed to create entity of type '{entityType}': {message}", "ENTITY_CREATION_FAILED")
    {
        EntityType = entityType;
    }

    public EntityCreationException(string entityType, string message, System.Exception innerException)
        : base($"Failed to create entity of type '{entityType}': {message}", innerException)
    {
        EntityType = entityType;
        ErrorCode = "ENTITY_CREATION_FAILED";
    }
}