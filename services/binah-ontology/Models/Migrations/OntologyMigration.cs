using Binah.Ontology.Models.Base;

namespace Binah.Ontology.Models.Migrations;

/// <summary>
/// Represents a database migration for ontology changes
/// Tracks all migrations (forward and rollback) with full audit trail
/// </summary>
public class OntologyMigration : Entity
{
    public Guid TenantId { get; set; }
    public string FromVersion { get; set; } = string.Empty;
    public string ToVersion { get; set; } = string.Empty;
    public string MigrationType { get; set; } = string.Empty; // "forward" or "rollback"
    public string MigrationScript { get; set; } = string.Empty; // SQL script
    public string CypherScript { get; set; } = string.Empty; // Neo4j Cypher script
    public string RollbackScript { get; set; } = string.Empty; // SQL rollback
    public string CypherRollbackScript { get; set; } = string.Empty; // Neo4j rollback
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    public string AppliedBy { get; set; } = string.Empty;
    public string Status { get; set; } = "pending"; // pending, completed, failed, rolled_back
    public string? ErrorMessage { get; set; }
    public List<OntologyChange> Changes { get; set; } = new();
}

/// <summary>
/// Represents a single change in an ontology migration
/// </summary>
public class OntologyChange
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MigrationId { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? PropertyName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string VersionImpact { get; set; } = "PATCH"; // MAJOR, MINOR, PATCH
    public string Description { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
}

/// <summary>
/// Change types for ontology migrations
/// </summary>
public static class ChangeTypes
{
    // Entity changes
    public const string ENTITY_ADDED = "ENTITY_ADDED";
    public const string ENTITY_REMOVED = "ENTITY_REMOVED";
    public const string ENTITY_RENAMED = "ENTITY_RENAMED";

    // Property changes
    public const string PROPERTY_ADDED = "PROPERTY_ADDED";
    public const string PROPERTY_REMOVED = "PROPERTY_REMOVED";
    public const string PROPERTY_RENAMED = "PROPERTY_RENAMED";
    public const string PROPERTY_TYPE_CHANGED = "PROPERTY_TYPE_CHANGED";

    // Relationship changes
    public const string RELATIONSHIP_ADDED = "RELATIONSHIP_ADDED";
    public const string RELATIONSHIP_REMOVED = "RELATIONSHIP_REMOVED";
    public const string RELATIONSHIP_RENAMED = "RELATIONSHIP_RENAMED";

    // Validation changes
    public const string VALIDATION_ADDED = "VALIDATION_ADDED";
    public const string VALIDATION_REMOVED = "VALIDATION_REMOVED";
    public const string VALIDATION_UPDATED = "VALIDATION_UPDATED";
}

/// <summary>
/// Version impact for semantic versioning
/// </summary>
public static class VersionImpact
{
    public const string MAJOR = "MAJOR"; // Breaking changes (rename, remove)
    public const string MINOR = "MINOR"; // Non-breaking additions
    public const string PATCH = "PATCH"; // Validation refinements, bug fixes
}
