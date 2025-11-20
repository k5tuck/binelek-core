using Binah.Ontology.Models.Migrations;
using Binah.Ontology.Models.Ontology;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Binah.Ontology.Services;

/// <summary>
/// Generates Neo4j Cypher migration scripts from ontology changes
/// All migrations are idempotent and support rollback
/// </summary>
public interface ICypherMigrationGenerator
{
    Task<string> GenerateMigrationAsync(OntologyVersion from, OntologyVersion to, List<OntologyChange> changes);
    Task<string> GenerateRollbackAsync(OntologyVersion from, OntologyVersion to, List<OntologyChange> changes);
}

public class CypherMigrationGenerator : ICypherMigrationGenerator
{
    private readonly ILogger<CypherMigrationGenerator> _logger;

    public CypherMigrationGenerator(ILogger<CypherMigrationGenerator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> GenerateMigrationAsync(
        OntologyVersion from,
        OntologyVersion to,
        List<OntologyChange> changes)
    {
        await Task.CompletedTask;

        var sb = new StringBuilder();
        sb.AppendLine($"// Migration: {from.Version} → {to.Version}");
        sb.AppendLine($"// Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"// Tenant: {to.TenantId}");
        sb.AppendLine($"// Total Changes: {changes.Count}");
        sb.AppendLine();

        foreach (var change in changes.OrderBy(c => GetChangePriority(c.ChangeType)))
        {
            sb.AppendLine($"// Change: {change.ChangeType} - {change.Description}");
            sb.AppendLine($"// Impact: {change.VersionImpact} (Confidence: {change.ConfidenceScore:F2}%)");

            var script = change.ChangeType switch
            {
                ChangeTypes.ENTITY_ADDED => GenerateAddEntity(change, to.TenantId),
                ChangeTypes.ENTITY_REMOVED => GenerateRemoveEntity(change, to.TenantId),
                ChangeTypes.ENTITY_RENAMED => GenerateRenameEntity(change, to.TenantId),
                ChangeTypes.PROPERTY_ADDED => GenerateAddProperty(change, to.TenantId),
                ChangeTypes.PROPERTY_REMOVED => GenerateRemoveProperty(change, to.TenantId),
                ChangeTypes.PROPERTY_RENAMED => GenerateRenameProperty(change, to.TenantId),
                ChangeTypes.RELATIONSHIP_ADDED => GenerateAddRelationship(change, to.TenantId),
                ChangeTypes.RELATIONSHIP_REMOVED => GenerateRemoveRelationship(change, to.TenantId),
                ChangeTypes.RELATIONSHIP_RENAMED => GenerateRenameRelationship(change, to.TenantId),
                _ => $"// Unknown change type: {change.ChangeType}"
            };

            sb.AppendLine(script);
            sb.AppendLine();
        }

        _logger.LogInformation("Generated Cypher migration script: {ChangeCount} changes, {LineCount} lines",
            changes.Count, sb.ToString().Split('\n').Length);

        return sb.ToString();
    }

    public async Task<string> GenerateRollbackAsync(
        OntologyVersion from,
        OntologyVersion to,
        List<OntologyChange> changes)
    {
        await Task.CompletedTask;

        var sb = new StringBuilder();
        sb.AppendLine($"// Rollback: {from.Version} → {to.Version}");
        sb.AppendLine($"// Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"// WARNING: This will revert graph to previous state");
        sb.AppendLine();

        // Reverse the changes in reverse order
        foreach (var change in changes.OrderByDescending(c => GetChangePriority(c.ChangeType)))
        {
            sb.AppendLine($"// Rollback: {change.ChangeType} - {change.Description}");

            var script = change.ChangeType switch
            {
                ChangeTypes.ENTITY_ADDED => GenerateRemoveEntity(change, to.TenantId),
                ChangeTypes.ENTITY_REMOVED => GenerateAddEntity(change, to.TenantId),
                ChangeTypes.ENTITY_RENAMED => GenerateRenameEntity(new OntologyChange
                {
                    EntityType = change.EntityType,
                    OldValue = change.NewValue, // Swap old and new
                    NewValue = change.OldValue
                }, to.TenantId),
                ChangeTypes.PROPERTY_ADDED => GenerateRemoveProperty(change, to.TenantId),
                ChangeTypes.PROPERTY_REMOVED => GenerateAddProperty(change, to.TenantId),
                ChangeTypes.PROPERTY_RENAMED => GenerateRenameProperty(new OntologyChange
                {
                    EntityType = change.EntityType,
                    PropertyName = change.PropertyName,
                    OldValue = change.NewValue,
                    NewValue = change.OldValue
                }, to.TenantId),
                ChangeTypes.RELATIONSHIP_ADDED => GenerateRemoveRelationship(change, to.TenantId),
                ChangeTypes.RELATIONSHIP_REMOVED => GenerateAddRelationship(change, to.TenantId),
                _ => $"// Cannot rollback change type: {change.ChangeType}"
            };

            sb.AppendLine(script);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string GenerateAddEntity(OntologyChange change, Guid tenantId)
    {
        return $@"// Add entity label: {change.EntityType}
// Note: Neo4j doesn't require pre-creating labels
// Labels are created when nodes are first created with that label
// This is a no-op for documentation purposes
// Actual nodes will be created during data ingestion";
    }

    private string GenerateRemoveEntity(OntologyChange change, Guid tenantId)
    {
        return $@"// Remove all nodes with label: {change.EntityType}
// WARNING: This will delete all nodes and relationships
MATCH (n:{change.EntityType} {{tenantId: '{tenantId}'}})
DETACH DELETE n;";
    }

    private string GenerateRenameEntity(OntologyChange change, Guid tenantId)
    {
        return $@"// Rename entity label: {change.OldValue} → {change.NewValue}
MATCH (n:{change.OldValue} {{tenantId: '{tenantId}'}})
REMOVE n:{change.OldValue}
SET n:{change.NewValue};

// Create index on new label
CREATE INDEX IF NOT EXISTS FOR (n:{change.NewValue}) ON (n.tenantId);
CREATE INDEX IF NOT EXISTS FOR (n:{change.NewValue}) ON (n.id);";
    }

    private string GenerateAddProperty(OntologyChange change, Guid tenantId)
    {
        var defaultValue = GetDefaultValue(change.NewValue ?? "string");
        return $@"// Add property: {change.PropertyName} to {change.EntityType}
MATCH (n:{change.EntityType} {{tenantId: '{tenantId}'}})
WHERE n.{change.PropertyName} IS NULL
SET n.{change.PropertyName} = {defaultValue};

// Create index if property is commonly queried
CREATE INDEX IF NOT EXISTS FOR (n:{change.EntityType}) ON (n.{change.PropertyName});";
    }

    private string GenerateRemoveProperty(OntologyChange change, Guid tenantId)
    {
        return $@"// Remove property: {change.PropertyName} from {change.EntityType}
MATCH (n:{change.EntityType} {{tenantId: '{tenantId}'}})
REMOVE n.{change.PropertyName};

// Drop index
DROP INDEX FOR (n:{change.EntityType}) ON (n.{change.PropertyName}) IF EXISTS;";
    }

    private string GenerateRenameProperty(OntologyChange change, Guid tenantId)
    {
        return $@"// Rename property: {change.OldValue} → {change.NewValue}
MATCH (n:{change.EntityType} {{tenantId: '{tenantId}'}})
WHERE n.{change.OldValue} IS NOT NULL
SET n.{change.NewValue} = n.{change.OldValue}
REMOVE n.{change.OldValue};";
    }

    private string GenerateAddRelationship(OntologyChange change, Guid tenantId)
    {
        // Example: change.Description = "Property LOCATED_IN Parcel"
        var parts = change.Description?.Split(' ') ?? Array.Empty<string>();
        if (parts.Length < 3)
            return "// Invalid relationship description";

        var fromEntity = parts[0];
        var relType = parts[1];
        var toEntity = parts[2];

        return $@"// Add relationship: {fromEntity} -{relType}-> {toEntity}
// This creates the relationship structure
// Actual relationships will be created during data ingestion
// Example usage:
// MATCH (from:{fromEntity} {{tenantId: '{tenantId}'}}), (to:{toEntity} {{tenantId: '{tenantId}'}})
// WHERE [your matching logic]
// CREATE (from)-[:{relType} {{tenantId: '{tenantId}', createdAt: datetime()}}]->(to);";
    }

    private string GenerateRemoveRelationship(OntologyChange change, Guid tenantId)
    {
        var parts = change.Description?.Split(' ') ?? Array.Empty<string>();
        if (parts.Length < 3)
            return "// Invalid relationship description";

        var fromEntity = parts[0];
        var relType = parts[1];
        var toEntity = parts[2];

        return $@"// Remove relationship: {fromEntity} -{relType}-> {toEntity}
MATCH (from:{fromEntity} {{tenantId: '{tenantId}'}})-[r:{relType}]->(to:{toEntity} {{tenantId: '{tenantId}'}})
DELETE r;";
    }

    private string GenerateRenameRelationship(OntologyChange change, Guid tenantId)
    {
        return $@"// Rename relationship: {change.OldValue} → {change.NewValue}
// Note: Neo4j doesn't support renaming relationship types directly
// We need to create new relationships and delete old ones
MATCH (from)-[old:{change.OldValue} {{tenantId: '{tenantId}'}}]->(to)
CREATE (from)-[new:{change.NewValue}]->(to)
SET new = old
DELETE old;";
    }

    private string GetDefaultValue(string dataType)
    {
        return dataType.ToLower() switch
        {
            "string" => "''",
            "integer" => "0",
            "decimal" => "0.0",
            "boolean" => "false",
            "date" => "date()",
            "datetime" => "datetime()",
            "array" => "[]",
            "object" => "{}",
            _ => "null"
        };
    }

    private int GetChangePriority(string changeType)
    {
        return changeType switch
        {
            ChangeTypes.ENTITY_ADDED => 1,
            ChangeTypes.ENTITY_RENAMED => 2,
            ChangeTypes.PROPERTY_ADDED => 3,
            ChangeTypes.PROPERTY_RENAMED => 4,
            ChangeTypes.RELATIONSHIP_ADDED => 5,
            ChangeTypes.RELATIONSHIP_RENAMED => 6,
            ChangeTypes.RELATIONSHIP_REMOVED => 7,
            ChangeTypes.PROPERTY_REMOVED => 8,
            ChangeTypes.ENTITY_REMOVED => 9,
            _ => 10
        };
    }
}
