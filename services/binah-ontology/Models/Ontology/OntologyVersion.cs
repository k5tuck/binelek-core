using Binah.Ontology.Models.Base;

namespace Binah.Ontology.Models.Ontology;

public class OntologyVersion : Entity
{
    public Guid TenantId { get; set; }
    public string OntologyName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ModelJson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string Branch { get; set; } = "main";
    
    // Navigation properties
    public List<EntityDefinition> Entities { get; set; } = new();
    public List<RelationshipDefinition> Relationships { get; set; } = new();
}
