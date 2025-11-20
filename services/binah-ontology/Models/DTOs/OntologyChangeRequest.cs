namespace Binah.Ontology.Models.DTOs;

/// <summary>
/// Request to trigger ontology refactoring PR
/// </summary>
public class OntologyChangeRequest
{
    public string TenantId { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public List<PropertyChange> AddedProperties { get; set; } = new();
    public List<PropertyChange> UpdatedProperties { get; set; } = new();
    public List<string> RemovedProperties { get; set; } = new();
    public List<RelationshipChange> AddedRelationships { get; set; } = new();
    public string ChangeReason { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
}

public class PropertyChange
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool Required { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? DefaultValue { get; set; }
}

public class RelationshipChange
{
    public string Name { get; set; } = string.Empty;
    public string TargetEntity { get; set; } = string.Empty;
    public string Cardinality { get; set; } = "ONE_TO_MANY";
    public string Description { get; set; } = string.Empty;
}

public class OntologyRefactoringResponse
{
    public bool Success { get; set; }
    public int PrNumber { get; set; }
    public string PrUrl { get; set; } = string.Empty;
    public string RefactoringId { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public List<string> FilesChanged { get; set; } = new();
}
