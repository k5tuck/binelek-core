using Binah.Ontology.Models;
using Binah.Ontology.Models.Base;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Binah.Ontology.Services;

/// <summary>
/// Service interface for tracking entity version history and lineage
/// </summary>
public interface ILineageService
{
    /// <summary>
    /// Gets the complete version history for an entity
    /// </summary>
    /// <param name="entityId">The entity ID</param>
    /// <returns>Lineage information including all versions</returns>
    /// <exception cref="EntityNotFoundException">Thrown when entity is not found</exception>
    Task<EntityLineage> GetEntityLineageAsync(string entityId);

    /// <summary>
    /// Gets a specific version of an entity
    /// </summary>
    /// <param name="entityId">The entity ID</param>
    /// <param name="version">The version number (e.g., "1.0", "2.3")</param>
    /// <returns>The entity at the specified version, null if version not found</returns>
    Task<Entity?> GetEntityVersionAsync(string entityId, string version);

    /// <summary>
    /// Records a new version of an entity (called internally during updates)
    /// </summary>
    /// <param name="entityId">The entity ID</param>
    /// <param name="previousVersion">The previous version number</param>
    /// <param name="newVersion">The new version number</param>
    /// <param name="changedProperties">Dictionary of changed property names and their new values</param>
    /// <param name="changedBy">User or system identifier who made the change</param>
    /// <returns>The created version record</returns>
    Task<EntityVersion> RecordVersionAsync(
        string entityId,
        string previousVersion,
        string newVersion,
        Dictionary<string, object> changedProperties,
        string? changedBy = null
    );

    /// <summary>
    /// Compares two versions of an entity
    /// </summary>
    /// <param name="entityId">The entity ID</param>
    /// <param name="versionA">First version to compare</param>
    /// <param name="versionB">Second version to compare</param>
    /// <returns>Diff result showing changes between versions</returns>
    Task<VersionDiff> CompareVersionsAsync(string entityId, string versionA, string versionB);

    /// <summary>
    /// Gets audit trail for an entity (who changed what and when)
    /// </summary>
    /// <param name="entityId">The entity ID</param>
    /// <param name="startDate">Optional start date filter</param>
    /// <param name="endDate">Optional end date filter</param>
    /// <returns>List of audit records</returns>
    Task<List<AuditRecord>> GetAuditTrailAsync(
        string entityId,
        DateTime? startDate = null,
        DateTime? endDate = null
    );

    /// <summary>
    /// Rolls back an entity to a previous version
    /// </summary>
    /// <param name="entityId">The entity ID</param>
    /// <param name="targetVersion">The version to roll back to</param>
    /// <param name="rolledBackBy">User or system identifier performing the rollback</param>
    /// <returns>The entity after rollback</returns>
    /// <exception cref="EntityNotFoundException">Thrown when entity is not found</exception>
    /// <exception cref="VersionNotFoundException">Thrown when target version is not found</exception>
    Task<Entity> RollbackToVersionAsync(
        string entityId,
        string targetVersion,
        string? rolledBackBy = null
    );
}

/// <summary>
/// Complete lineage information for an entity
/// </summary>
public class EntityLineage
{
    /// <summary>Entity ID</summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>Entity type</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Current version</summary>
    public string CurrentVersion { get; set; } = string.Empty;

    /// <summary>All version records ordered by date</summary>
    public List<EntityVersion> Versions { get; set; } = new();

    /// <summary>Total number of versions</summary>
    public int VersionCount { get; set; }

    /// <summary>Date entity was created</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Date of last update</summary>
    public DateTime LastUpdatedAt { get; set; }
}

/// <summary>
/// Represents a single version of an entity
/// </summary>
public class EntityVersion
{
    /// <summary>Version number</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>Timestamp of this version</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>User or system who created this version</summary>
    public string? ChangedBy { get; set; }

    /// <summary>Properties that changed in this version</summary>
    public Dictionary<string, object> ChangedProperties { get; set; } = new();

    /// <summary>Previous version number</summary>
    public string? PreviousVersion { get; set; }

    /// <summary>Change description or comment</summary>
    public string? ChangeDescription { get; set; }
}

/// <summary>
/// Diff between two versions
/// </summary>
public class VersionDiff
{
    /// <summary>First version being compared</summary>
    public string VersionA { get; set; } = string.Empty;

    /// <summary>Second version being compared</summary>
    public string VersionB { get; set; } = string.Empty;

    /// <summary>Properties added in version B</summary>
    public Dictionary<string, object> AddedProperties { get; set; } = new();

    /// <summary>Properties removed in version B</summary>
    public Dictionary<string, object> RemovedProperties { get; set; } = new();

    /// <summary>Properties changed between versions</summary>
    public Dictionary<string, PropertyChange> ModifiedProperties { get; set; } = new();

    /// <summary>Properties unchanged</summary>
    public Dictionary<string, object> UnchangedProperties { get; set; } = new();
}

/// <summary>
/// Represents a property change between versions
/// </summary>
public class PropertyChange
{
    /// <summary>Property name</summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>Old value</summary>
    public object? OldValue { get; set; }

    /// <summary>New value</summary>
    public object? NewValue { get; set; }
}

/// <summary>
/// Audit record for entity changes
/// </summary>
public class AuditRecord
{
    /// <summary>Audit record ID</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Entity ID</summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>Action type (create, update, delete)</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>User or system who performed the action</summary>
    public string? PerformedBy { get; set; }

    /// <summary>Timestamp of the action</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>Version before the action</summary>
    public string? VersionBefore { get; set; }

    /// <summary>Version after the action</summary>
    public string? VersionAfter { get; set; }

    /// <summary>Details of what changed</summary>
    public Dictionary<string, object> ChangeDetails { get; set; } = new();

    /// <summary>IP address or system identifier</summary>
    public string? Source { get; set; }
}
