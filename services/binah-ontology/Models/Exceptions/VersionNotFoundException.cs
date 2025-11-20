namespace Binah.Ontology.Models.Exceptions;

/// <summary>
/// Exception thrown when a version is not found
/// </summary>
public class VersionNotFoundException : OntologyException
{
    public string EntityId { get; set; }
    public string Version { get; set; }

    public VersionNotFoundException(string entityId, string version)
        : base($"Version '{version}' not found for entity '{entityId}'", "VERSION_NOT_FOUND")
    {
        EntityId = entityId;
        Version = version;
    }
}