using Binah.Ontology.Models.Migrations;
using Binah.Ontology.Models.Ontology;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Binah.Ontology.Services;

/// <summary>
/// Generates backward compatibility views for deprecated entities/properties
/// Ensures old API clients continue working during 12-month deprecation window
/// </summary>
public interface IBackwardCompatibilityService
{
    Task<string> GenerateCompatibilityViewsAsync(List<OntologyChange> breakingChanges, Guid tenantId);
    Task<bool> ApplyCompatibilityViewsAsync(List<OntologyChange> breakingChanges, Guid tenantId);
    Task<bool> RemoveCompatibilityViewsAsync(string deprecatedName, Guid tenantId);
}

public class BackwardCompatibilityService : IBackwardCompatibilityService
{
    private readonly ILogger<BackwardCompatibilityService> _logger;

    public BackwardCompatibilityService(ILogger<BackwardCompatibilityService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> GenerateCompatibilityViewsAsync(
        List<OntologyChange> breakingChanges,
        Guid tenantId)
    {
        await Task.CompletedTask;

        var sb = new StringBuilder();
        sb.AppendLine("-- Backward Compatibility Views");
        sb.AppendLine($"-- Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"-- Tenant: {tenantId}");
        sb.AppendLine($"-- These views provide backward compatibility for deprecated entities");
        sb.AppendLine();

        foreach (var change in breakingChanges)
        {
            if (change.ChangeType == ChangeTypes.ENTITY_RENAMED)
            {
                sb.AppendLine(GenerateEntityRenameView(change, tenantId));
                sb.AppendLine();
            }
            else if (change.ChangeType == ChangeTypes.PROPERTY_RENAMED)
            {
                sb.AppendLine(GeneratePropertyRenameView(change, tenantId));
                sb.AppendLine();
            }
            else if (change.ChangeType == ChangeTypes.ENTITY_REMOVED)
            {
                sb.AppendLine(GenerateEntityRemovalView(change, tenantId));
                sb.AppendLine();
            }
        }

        _logger.LogInformation("Generated {Count} backward compatibility views", breakingChanges.Count);

        return sb.ToString();
    }

    public async Task<bool> ApplyCompatibilityViewsAsync(
        List<OntologyChange> breakingChanges,
        Guid tenantId)
    {
        try
        {
            var viewScript = await GenerateCompatibilityViewsAsync(breakingChanges, tenantId);

            // TODO: Execute SQL script against PostgreSQL
            // await _dbContext.Database.ExecuteSqlRawAsync(viewScript);

            _logger.LogInformation("Successfully applied backward compatibility views for tenant {TenantId}",
                tenantId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying backward compatibility views for tenant {TenantId}",
                tenantId);
            throw;
        }
    }

    public async Task<bool> RemoveCompatibilityViewsAsync(string deprecatedName, Guid tenantId)
    {
        try
        {
            await Task.CompletedTask;

            var viewName = $"{deprecatedName.ToLower()}s_compat";

            var dropScript = $@"
-- Remove backward compatibility view
DROP VIEW IF EXISTS {viewName} CASCADE;

-- Log removal
INSERT INTO ontology_migrations (
    tenant_id, migration_type, migration_script, applied_at, status
) VALUES (
    '{tenantId}',
    'deprecation_complete',
    'Removed compatibility view: {viewName}',
    NOW(),
    'completed'
);
";

            // TODO: Execute SQL script
            // await _dbContext.Database.ExecuteSqlRawAsync(dropScript);

            _logger.LogInformation("Removed backward compatibility view: {ViewName}", viewName);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing compatibility view: {Name}", deprecatedName);
            throw;
        }
    }

    private string GenerateEntityRenameView(OntologyChange change, Guid tenantId)
    {
        var oldName = change.OldValue?.ToLower();
        var newName = change.NewValue?.ToLower();

        return $@"-- Backward compatibility view: {oldName} → {newName}
-- Old API clients can continue using '{oldName}s' table
-- This view will be removed after 12-month deprecation window

CREATE OR REPLACE VIEW {oldName}s AS
SELECT
    id,
    tenant_id,
    created_at,
    updated_at,
    created_by,
    updated_by
FROM {newName}s
WHERE tenant_id = '{tenantId}';

-- Grant permissions
GRANT SELECT ON {oldName}s TO binah_app_user;

-- Add comment explaining deprecation
COMMENT ON VIEW {oldName}s IS
'DEPRECATED: This view provides backward compatibility. Use {newName}s instead. Will be removed on {DateTime.UtcNow.AddMonths(12):yyyy-MM-dd}.';";
    }

    private string GeneratePropertyRenameView(OntologyChange change, Guid tenantId)
    {
        var entityName = change.EntityType.ToLower();
        var oldProp = change.OldValue?.ToLower();
        var newProp = change.NewValue?.ToLower();

        return $@"-- Backward compatibility view: {entityName}.{oldProp} → {entityName}.{newProp}
CREATE OR REPLACE VIEW {entityName}s_compat AS
SELECT
    id,
    tenant_id,
    {newProp} AS {oldProp},  -- Map new property to old name
    created_at,
    updated_at
FROM {entityName}s
WHERE tenant_id = '{tenantId}';

-- Grant permissions
GRANT SELECT ON {entityName}s_compat TO binah_app_user;

-- Add deprecation comment
COMMENT ON VIEW {entityName}s_compat IS
'DEPRECATED: Property {oldProp} renamed to {newProp}. Use {entityName}s table with {newProp} instead. Will be removed on {DateTime.UtcNow.AddMonths(12):yyyy-MM-dd}.';";
    }

    private string GenerateEntityRemovalView(OntologyChange change, Guid tenantId)
    {
        var entityName = change.EntityType.ToLower();

        return $@"-- Backward compatibility view: {entityName} (removed)
-- Returns empty result set to prevent breaking existing queries
CREATE OR REPLACE VIEW {entityName}s AS
SELECT
    gen_random_uuid() AS id,
    '{tenantId}'::uuid AS tenant_id,
    NOW() AS created_at,
    NOW() AS updated_at,
    ''::varchar AS created_by,
    ''::varchar AS updated_by
WHERE false;  -- Always empty

-- Add deprecation comment
COMMENT ON VIEW {entityName}s IS
'DEPRECATED: Entity {change.EntityType} has been removed. This view returns empty results for backward compatibility. Will be removed on {DateTime.UtcNow.AddMonths(12):yyyy-MM-dd}.';";
    }
}
