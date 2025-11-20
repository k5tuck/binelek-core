using System.Text.Json.Serialization;
using Binah.Ontology.Models.Base;
using Binah.Ontology.Models.Relationship;
namespace Binah.Ontology.Models.Lineage;

/// <summary>
/// Complete lineage history for an entity
/// </summary>
public class EntityLineage
{
    [JsonPropertyName("entityId")]
    public string EntityId { get; set; } = string.Empty;

    [JsonPropertyName("entityType")]
    public string EntityType { get; set; } = string.Empty;

    [JsonPropertyName("currentVersion")]
    public string CurrentVersion { get; set; } = string.Empty;

    [JsonPropertyName("versions")]
    public List<EntityVersion> Versions { get; set; } = new();

    [JsonPropertyName("relationships")]
    public List<Relationship.Relationship> Relationships { get; set; } = new();

    [JsonPropertyName("dependentEntities")]
    public List<Entity> DependentEntities { get; set; } = new();
    public int VersionCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdatedAt { get; set; }
}