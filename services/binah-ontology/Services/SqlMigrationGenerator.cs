using Binah.Ontology.Models.Migrations;
using Binah.Ontology.Models.Ontology;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Binah.Ontology.Services;

/// <summary>
/// Generates PostgreSQL migration scripts from ontology changes
/// All migrations are idempotent and transactional
/// </summary>
public interface ISqlMigrationGenerator
{
    Task<string> GenerateMigrationAsync(OntologyVersion from, OntologyVersion to, List<OntologyChange> changes);
    Task<string> GenerateRollbackAsync(OntologyVersion from, OntologyVersion to, List<OntologyChange> changes);
}

public class SqlMigrationGenerator : ISqlMigrationGenerator
{
    private readonly ILogger<SqlMigrationGenerator> _logger;

    public SqlMigrationGenerator(ILogger<SqlMigrationGenerator> logger)
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
        sb.AppendLine($"-- Migration: {from.Version} → {to.Version}");
        sb.AppendLine($"-- Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"-- Tenant: {to.TenantId}");
        sb.AppendLine($"-- Total Changes: {changes.Count}");
        sb.AppendLine();

        sb.AppendLine("BEGIN TRANSACTION;");
        sb.AppendLine();

        foreach (var change in changes.OrderBy(c => GetChangePriority(c.ChangeType)))
        {
            sb.AppendLine($"-- Change: {change.ChangeType} - {change.Description}");
            sb.AppendLine($"-- Impact: {change.VersionImpact} (Confidence: {change.ConfidenceScore:F2}%)");

            var script = change.ChangeType switch
            {
                ChangeTypes.ENTITY_ADDED => GenerateAddEntity(change),
                ChangeTypes.ENTITY_REMOVED => GenerateRemoveEntity(change),
                ChangeTypes.ENTITY_RENAMED => GenerateRenameEntity(change),
                ChangeTypes.PROPERTY_ADDED => GenerateAddProperty(change),
                ChangeTypes.PROPERTY_REMOVED => GenerateRemoveProperty(change),
                ChangeTypes.PROPERTY_RENAMED => GenerateRenameProperty(change),
                ChangeTypes.PROPERTY_TYPE_CHANGED => GenerateChangePropertyType(change),
                ChangeTypes.VALIDATION_ADDED => GenerateAddValidation(change),
                ChangeTypes.VALIDATION_REMOVED => GenerateRemoveValidation(change),
                _ => $"-- Unknown change type: {change.ChangeType}"
            };

            sb.AppendLine(script);
            sb.AppendLine();
        }

        // Record migration in history
        sb.AppendLine("-- Record migration in history");
        sb.AppendLine($@"INSERT INTO ontology_migrations (
    tenant_id, from_version, to_version, migration_type,
    migration_script, applied_at, applied_by, status
) VALUES (
    '{to.TenantId}',
    '{from.Version}',
    '{to.Version}',
    'forward',
    'Migration from {from.Version} to {to.Version}',
    NOW(),
    '{to.CreatedBy}',
    'completed'
);");
        sb.AppendLine();

        sb.AppendLine("COMMIT;");

        _logger.LogInformation("Generated SQL migration script: {ChangeCount} changes, {LineCount} lines",
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
        sb.AppendLine($"-- Rollback: {from.Version} → {to.Version}");
        sb.AppendLine($"-- Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"-- WARNING: This will revert database to previous state");
        sb.AppendLine();

        sb.AppendLine("BEGIN TRANSACTION;");
        sb.AppendLine();

        // Reverse the changes in reverse order
        foreach (var change in changes.OrderByDescending(c => GetChangePriority(c.ChangeType)))
        {
            sb.AppendLine($"-- Rollback: {change.ChangeType} - {change.Description}");

            var script = change.ChangeType switch
            {
                ChangeTypes.ENTITY_ADDED => GenerateRemoveEntity(change),
                ChangeTypes.ENTITY_REMOVED => GenerateAddEntity(change),
                ChangeTypes.ENTITY_RENAMED => GenerateRenameEntity(new OntologyChange
                {
                    EntityType = change.EntityType,
                    OldValue = change.NewValue, // Swap old and new
                    NewValue = change.OldValue
                }),
                ChangeTypes.PROPERTY_ADDED => GenerateRemoveProperty(change),
                ChangeTypes.PROPERTY_REMOVED => GenerateAddProperty(change),
                ChangeTypes.PROPERTY_RENAMED => GenerateRenameProperty(new OntologyChange
                {
                    EntityType = change.EntityType,
                    PropertyName = change.PropertyName,
                    OldValue = change.NewValue, // Swap old and new
                    NewValue = change.OldValue
                }),
                _ => $"-- Cannot rollback change type: {change.ChangeType}"
            };

            sb.AppendLine(script);
            sb.AppendLine();
        }

        // Record rollback in history
        sb.AppendLine("-- Record rollback in history");
        sb.AppendLine($@"INSERT INTO ontology_migrations (
    tenant_id, from_version, to_version, migration_type,
    migration_script, applied_at, status
) VALUES (
    '{to.TenantId}',
    '{from.Version}',
    '{to.Version}',
    'rollback',
    'Rollback from {from.Version} to {to.Version}',
    NOW(),
    'completed'
);");
        sb.AppendLine();

        sb.AppendLine("COMMIT;");

        return sb.ToString();
    }

    private string GenerateAddEntity(OntologyChange change)
    {
        var tableName = $"{change.EntityType.ToLower()}s";
        return $@"-- Add entity: {change.EntityType}
CREATE TABLE IF NOT EXISTS {tableName} (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    created_by VARCHAR(100),
    updated_by VARCHAR(100)
);

CREATE INDEX IF NOT EXISTS idx_{tableName}_tenant ON {tableName}(tenant_id);
CREATE INDEX IF NOT EXISTS idx_{tableName}_created ON {tableName}(created_at);";
    }

    private string GenerateRemoveEntity(OntologyChange change)
    {
        var tableName = $"{change.EntityType.ToLower()}s";
        return $@"-- Remove entity: {change.EntityType} (WARNING: Data will be lost)
DROP TABLE IF EXISTS {tableName} CASCADE;";
    }

    private string GenerateRenameEntity(OntologyChange change)
    {
        var oldName = $"{change.OldValue?.ToLower()}s";
        var newName = $"{change.NewValue?.ToLower()}s";
        return $@"-- Rename entity: {change.OldValue} → {change.NewValue}
ALTER TABLE {oldName} RENAME TO {newName};

-- Rename indexes
ALTER INDEX IF EXISTS idx_{oldName}_tenant RENAME TO idx_{newName}_tenant;
ALTER INDEX IF EXISTS idx_{oldName}_created RENAME TO idx_{newName}_created;";
    }

    private string GenerateAddProperty(OntologyChange change)
    {
        var tableName = $"{change.EntityType.ToLower()}s";
        var dataType = InferSqlDataType(change.NewValue ?? "string");
        return $@"-- Add property: {change.PropertyName} to {change.EntityType}
ALTER TABLE {tableName}
ADD COLUMN IF NOT EXISTS {change.PropertyName?.ToLower()} {dataType};

-- Add index if property is commonly queried
CREATE INDEX IF NOT EXISTS idx_{tableName}_{change.PropertyName?.ToLower()}
ON {tableName}({change.PropertyName?.ToLower()});";
    }

    private string GenerateRemoveProperty(OntologyChange change)
    {
        var tableName = $"{change.EntityType.ToLower()}s";
        return $@"-- Remove property: {change.PropertyName} from {change.EntityType}
ALTER TABLE {tableName}
DROP COLUMN IF EXISTS {change.PropertyName?.ToLower()} CASCADE;";
    }

    private string GenerateRenameProperty(OntologyChange change)
    {
        var tableName = $"{change.EntityType.ToLower()}s";
        return $@"-- Rename property: {change.OldValue} → {change.NewValue}
ALTER TABLE {tableName}
RENAME COLUMN {change.OldValue?.ToLower()} TO {change.NewValue?.ToLower()};";
    }

    private string GenerateChangePropertyType(OntologyChange change)
    {
        var tableName = $"{change.EntityType.ToLower()}s";
        var newType = InferSqlDataType(change.NewValue ?? "string");
        return $@"-- Change property type: {change.PropertyName} from {change.OldValue} to {change.NewValue}
ALTER TABLE {tableName}
ALTER COLUMN {change.PropertyName?.ToLower()} TYPE {newType} USING {change.PropertyName?.ToLower()}::{newType};";
    }

    private string GenerateAddValidation(OntologyChange change)
    {
        var tableName = $"{change.EntityType.ToLower()}s";
        return $@"-- Add validation: {change.Description}
ALTER TABLE {tableName}
ADD CONSTRAINT chk_{tableName}_{change.PropertyName?.ToLower()}
CHECK ({change.NewValue});";
    }

    private string GenerateRemoveValidation(OntologyChange change)
    {
        var tableName = $"{change.EntityType.ToLower()}s";
        return $@"-- Remove validation: {change.Description}
ALTER TABLE {tableName}
DROP CONSTRAINT IF EXISTS chk_{tableName}_{change.PropertyName?.ToLower()};";
    }

    private string InferSqlDataType(string ontologyType)
    {
        return ontologyType.ToLower() switch
        {
            "string" => "VARCHAR(255)",
            "text" => "TEXT",
            "integer" => "INTEGER",
            "biginteger" => "BIGINT",
            "decimal" => "DECIMAL(18, 2)",
            "double" => "DOUBLE PRECISION",
            "boolean" => "BOOLEAN",
            "date" => "DATE",
            "datetime" => "TIMESTAMP",
            "uuid" => "UUID",
            "json" => "JSONB",
            _ => "VARCHAR(255)"
        };
    }

    private int GetChangePriority(string changeType)
    {
        // Order: entities first, then properties, then validations
        return changeType switch
        {
            ChangeTypes.ENTITY_ADDED => 1,
            ChangeTypes.ENTITY_RENAMED => 2,
            ChangeTypes.PROPERTY_ADDED => 3,
            ChangeTypes.PROPERTY_RENAMED => 4,
            ChangeTypes.PROPERTY_TYPE_CHANGED => 5,
            ChangeTypes.VALIDATION_ADDED => 6,
            ChangeTypes.VALIDATION_REMOVED => 7,
            ChangeTypes.PROPERTY_REMOVED => 8,
            ChangeTypes.ENTITY_REMOVED => 9,
            _ => 10
        };
    }
}
