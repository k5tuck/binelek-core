using Binah.Ontology.Models;
using Binah.Ontology.Models.Base;
using Binah.Ontology.Models.Exceptions;
using Binah.Ontology.Repositories;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Binah.Ontology.Services;

/// <summary>
/// Implementation of lineage service for entity version tracking
/// </summary>
public class LineageService : ILineageService
{
    private readonly IDriver _driver;
    private readonly string _database;
    private readonly IEntityRepository _entityRepository;
    private readonly ILogger<LineageService> _logger;

    public LineageService(
        IDriver driver,
        string database,
        IEntityRepository entityRepository,
        ILogger<LineageService> logger)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _entityRepository = entityRepository ?? throw new ArgumentNullException(nameof(entityRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<EntityLineage> GetEntityLineageAsync(string entityId)
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArgumentException("Entity ID cannot be null or empty", nameof(entityId));
        }

        var session = _driver.AsyncSession(config => config.WithDatabase(_database));

        try
        {
            _logger.LogInformation("Retrieving lineage for entity {EntityId}", entityId);

            var query = @"
                MATCH (e)
                WHERE e.id = $entityId
                OPTIONAL MATCH (e)-[:HAS_VERSION]->(v:EntityVersion)
                RETURN e.id AS entityId,
                       e.type AS entityType,
                       e.version AS currentVersion,
                       e.created_at AS createdAt,
                       e.updated_at AS updatedAt,
                       collect(v {
                           version: v.version,
                           timestamp: v.timestamp,
                           changed_by: v.changed_by,
                           previous_version: v.previous_version,
                           changed_properties: v.changed_properties,
                           change_description: v.change_description
                       }) AS versions
                ORDER BY v.timestamp ASC
            ";

            var parameters = new Dictionary<string, object>
            {
                { "entityId", entityId }
            };

            var cursor = await session.RunAsync(query, parameters);
            var records = await cursor.ToListAsync();
            var result = records.Count > 0 ? records[0] : null;

            if (result == null)
            {
                throw new EntityNotFoundException(entityId);
            }

            var versions = result["versions"].As<List<Dictionary<string, object>>>()
                .Where(v => v.ContainsKey("version") && v["version"] != null)
                .Select(v => new EntityVersion
                {
                    Version = v["version"].ToString()!,
                    Timestamp = DateTime.Parse(v["timestamp"].ToString()!),
                    ChangedBy = v["changed_by"]?.ToString(),
                    PreviousVersion = v["previous_version"]?.ToString(),
                    ChangedProperties = v.ContainsKey("changed_properties") && v["changed_properties"] != null
                        ? JsonSerializer.Deserialize<Dictionary<string, object>>(v["changed_properties"].ToString()!)
                        : new Dictionary<string, object>(),
                    ChangeDescription = v["change_description"]?.ToString()
                })
                .OrderBy(v => v.Timestamp)
                .ToList();

            var lineage = new EntityLineage
            {
                EntityId = entityId,
                EntityType = result["entityType"].ToString()!,
                CurrentVersion = result["currentVersion"].ToString()!,
                Versions = versions,
                VersionCount = versions.Count,
                CreatedAt = DateTime.Parse(result["createdAt"].ToString()!),
                LastUpdatedAt = DateTime.Parse(result["updatedAt"].ToString()!)
            };

            _logger.LogInformation(
                "Retrieved lineage for {EntityId}: {Count} versions",
                entityId, lineage.VersionCount);

            return lineage;
        }
        catch (EntityNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving lineage for entity {EntityId}", entityId);
            throw new DatabaseConnectionException($"Failed to retrieve lineage for {entityId}", ex);
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    /// <inheritdoc/>
    public async Task<Entity?> GetEntityVersionAsync(string entityId, string version)
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArgumentException("Entity ID cannot be null or empty", nameof(entityId));
        }

        if (string.IsNullOrWhiteSpace(version))
        {
            throw new ArgumentException("Version cannot be null or empty", nameof(version));
        }

        var session = _driver.AsyncSession(config => config.WithDatabase(_database));

        try
        {
            _logger.LogDebug("Retrieving entity {EntityId} at version {Version}", entityId, version);

            var query = @"
                MATCH (v:EntityVersion)
                WHERE v.entity_id = $entityId AND v.version = $version
                RETURN v
            ";

            var parameters = new Dictionary<string, object>
            {
                { "entityId", entityId },
                { "version", version }
            };

            var cursor = await session.RunAsync(query, parameters);
            var records = await cursor.ToListAsync();
            var result = records.Count > 0 ? records[0] : null;

            if (result == null)
            {
                throw new VersionNotFoundException(entityId, version);
            }

            var versionNode = result["v"].As<Neo4j.Driver.INode>();

            // Reconstruct entity from version snapshot
            var entity = new Entity
            {
                Id = entityId,
                Version = version,
                Properties = versionNode.Properties.ContainsKey("snapshot")
                    ? JsonSerializer.Deserialize<Dictionary<string, object>>(
                        versionNode.Properties["snapshot"].ToString()!)
                    : new Dictionary<string, object>(),
                CreatedAt = DateTime.Parse(versionNode.Properties["timestamp"].ToString()!)
            };

            return entity;
        }
        catch (VersionNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving version {Version} for entity {EntityId}", version, entityId);
            return null;
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    /// <inheritdoc/>
    public async Task<EntityVersion> RecordVersionAsync(
        string entityId,
        string previousVersion,
        string newVersion,
        Dictionary<string, object> changedProperties,
        string? changedBy = null)
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArgumentException("Entity ID cannot be null or empty", nameof(entityId));
        }

        if (string.IsNullOrWhiteSpace(newVersion))
        {
            throw new ArgumentException("New version cannot be null or empty", nameof(newVersion));
        }

        var session = _driver.AsyncSession(config => config.WithDatabase(_database));

        try
        {
            _logger.LogInformation(
                "Recording version {Version} for entity {EntityId}",
                newVersion, entityId);

            // Get current entity snapshot
            var entity = await _entityRepository.GetByIdAsync(entityId);
            if (entity == null)
            {
                throw new EntityNotFoundException(entityId);
            }

            var versionId = $"{entityId}-v{newVersion}";
            var timestamp = DateTime.UtcNow;

            var query = @"
                MATCH (e)
                WHERE e.id = $entityId
                CREATE (v:EntityVersion {
                    version_id: $versionId,
                    entity_id: $entityId,
                    version: $newVersion,
                    previous_version: $previousVersion,
                    timestamp: $timestamp,
                    changed_by: $changedBy,
                    changed_properties: $changedProperties,
                    snapshot: $snapshot,
                    change_description: $changeDescription
                })
                CREATE (e)-[:HAS_VERSION]->(v)
                RETURN v
            ";

            var changeDescription = GenerateChangeDescription(changedProperties);

            var parameters = new Dictionary<string, object>
            {
                { "entityId", entityId },
                { "versionId", versionId },
                { "newVersion", newVersion },
                { "previousVersion", previousVersion },
                { "timestamp", timestamp.ToString("O") },
                { "changedBy", changedBy ?? "system" },
                { "changedProperties", JsonSerializer.Serialize(changedProperties) },
                { "snapshot", JsonSerializer.Serialize(entity.Properties) },
                { "changeDescription", changeDescription }
            };

            var cursor = await session.RunAsync(query, parameters);
            await cursor.ConsumeAsync();

            // Create audit record
            await CreateAuditRecordAsync(
                entityId,
                "UPDATE",
                changedBy,
                timestamp,
                previousVersion,
                newVersion,
                changedProperties);

            _logger.LogInformation(
                "Recorded version {Version} for entity {EntityId}",
                newVersion, entityId);

            return new EntityVersion
            {
                Version = newVersion,
                Timestamp = timestamp,
                ChangedBy = changedBy,
                PreviousVersion = previousVersion,
                ChangedProperties = changedProperties,
                ChangeDescription = changeDescription
            };
        }
        catch (EntityNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording version for entity {EntityId}", entityId);
            throw new DatabaseConnectionException($"Failed to record version for {entityId}", ex);
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    /// <inheritdoc/>
    public async Task<VersionDiff> CompareVersionsAsync(
        string entityId,
        string versionA,
        string versionB)
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArgumentException("Entity ID cannot be null or empty", nameof(entityId));
        }

        try
        {
            _logger.LogInformation(
                "Comparing versions {VersionA} and {VersionB} for entity {EntityId}",
                versionA, versionB, entityId);

            var entityA = await GetEntityVersionAsync(entityId, versionA);
            var entityB = await GetEntityVersionAsync(entityId, versionB);

            if (entityA == null || entityB == null)
            {
                throw new VersionNotFoundException(
                    entityId,
                    entityA == null ? versionA : versionB);
            }

            var propsA = entityA.Properties;
            var propsB = entityB.Properties;

            var diff = new VersionDiff
            {
                VersionA = versionA,
                VersionB = versionB,
                AddedProperties = new Dictionary<string, object>(),
                RemovedProperties = new Dictionary<string, object>(),
                ModifiedProperties = new Dictionary<string, PropertyChange>(),
                UnchangedProperties = new Dictionary<string, object>()
            };

            // Find added properties (in B but not in A)
            foreach (var prop in propsB)
            {
                if (!propsA.ContainsKey(prop.Key))
                {
                    diff.AddedProperties[prop.Key] = prop.Value;
                }
            }

            // Find removed properties (in A but not in B)
            foreach (var prop in propsA)
            {
                if (!propsB.ContainsKey(prop.Key))
                {
                    diff.RemovedProperties[prop.Key] = prop.Value;
                }
            }

            // Find modified and unchanged properties
            foreach (var prop in propsA)
            {
                if (propsB.ContainsKey(prop.Key))
                {
                    var valueA = prop.Value;
                    var valueB = propsB[prop.Key];

                    if (!AreValuesEqual(valueA, valueB))
                    {
                        diff.ModifiedProperties[prop.Key] = new PropertyChange
                        {
                            PropertyName = prop.Key,
                            OldValue = valueA,
                            NewValue = valueB
                        };
                    }
                    else
                    {
                        diff.UnchangedProperties[prop.Key] = valueA;
                    }
                }
            }

            _logger.LogInformation(
                "Version comparison complete: {Added} added, {Removed} removed, {Modified} modified",
                diff.AddedProperties.Count,
                diff.RemovedProperties.Count,
                diff.ModifiedProperties.Count);

            return diff;
        }
        catch (VersionNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing versions");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<AuditRecord>> GetAuditTrailAsync(
        string entityId,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArgumentException("Entity ID cannot be null or empty", nameof(entityId));
        }

        var session = _driver.AsyncSession(config => config.WithDatabase(_database));

        try
        {
            _logger.LogInformation("Retrieving audit trail for entity {EntityId}", entityId);

            var query = @"
                MATCH (e)-[:HAS_AUDIT]->(a:AuditRecord)
                WHERE e.id = $entityId
                AND ($startDate IS NULL OR a.timestamp >= $startDate)
                AND ($endDate IS NULL OR a.timestamp <= $endDate)
                RETURN a
                ORDER BY a.timestamp DESC
            ";

            var parameters = new Dictionary<string, object>
            {
                { "entityId", entityId },
                { "startDate", startDate?.ToString("O") ?? null! },
                { "endDate", endDate?.ToString("O") ?? null! }
            };

            var cursor = await session.RunAsync(query, parameters);
            var results = await cursor.ToListAsync();

            var auditRecords = results.Select(record =>
            {
                var node = record["a"].As<Neo4j.Driver.INode>();
                return new AuditRecord
                {
                    Id = node.Properties["audit_id"].ToString()!,
                    EntityId = entityId,
                    Action = node.Properties["action"].ToString()!,
                    PerformedBy = node.Properties["performed_by"]?.ToString(),
                    Timestamp = DateTime.Parse(node.Properties["timestamp"].ToString()!),
                    VersionBefore = node.Properties["version_before"]?.ToString(),
                    VersionAfter = node.Properties["version_after"]?.ToString(),
                    ChangeDetails = node.Properties.ContainsKey("change_details")
                        ? JsonSerializer.Deserialize<Dictionary<string, object>>(
                            node.Properties["change_details"].ToString()!)
                        : new Dictionary<string, object>(),
                    Source = node.Properties["source_ip"]?.ToString()
                };
            }).ToList();

            _logger.LogInformation(
                "Retrieved {Count} audit records for entity {EntityId}",
                auditRecords.Count, entityId);

            return auditRecords;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit trail");
            throw new DatabaseConnectionException("Failed to retrieve audit trail", ex);
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    /// <inheritdoc/>
    public async Task<Entity> RollbackToVersionAsync(
        string entityId,
        string targetVersion,
        string? rolledBackBy = null)
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArgumentException("Entity ID cannot be null or empty", nameof(entityId));
        }

        if (string.IsNullOrWhiteSpace(targetVersion))
        {
            throw new ArgumentException("Target version cannot be null or empty", nameof(targetVersion));
        }

        try
        {
            _logger.LogWarning(
                "Rolling back entity {EntityId} to version {Version}",
                entityId, targetVersion);

            // Get the target version
            var targetEntity = await GetEntityVersionAsync(entityId, targetVersion);
            if (targetEntity == null)
            {
                throw new VersionNotFoundException(entityId, targetVersion);
            }

            // Get current entity to determine next version
            var currentEntity = await _entityRepository.GetByIdAsync(entityId);
            if (currentEntity == null)
            {
                throw new EntityNotFoundException(entityId);
            }

            // Create new version with rolled-back properties
            var currentVersion = currentEntity.Version;
            var newVersion = IncrementVersion(currentVersion);

            // Update entity with target version's properties
            currentEntity.Properties = new Dictionary<string, object>(targetEntity.Properties);
            currentEntity.Version = newVersion;
            currentEntity.UpdatedAt = DateTime.UtcNow;
            currentEntity.UpdatedBy = rolledBackBy;

            var updatedEntity = await _entityRepository.UpdateAsync(currentEntity);

            // Record version with rollback note
            await RecordVersionAsync(
                entityId,
                currentVersion,
                newVersion,
                targetEntity.Properties,
                rolledBackBy);

            // Create audit record for rollback
            await CreateAuditRecordAsync(
                entityId,
                "ROLLBACK",
                rolledBackBy,
                DateTime.UtcNow,
                currentVersion,
                newVersion,
                new Dictionary<string, object>
                {
                    { "rolled_back_to", targetVersion },
                    { "reason", "Manual rollback operation" }
                });

            _logger.LogWarning(
                "Successfully rolled back entity {EntityId} to version {Version}",
                entityId, targetVersion);

            return updatedEntity;
        }
        catch (VersionNotFoundException)
        {
            throw;
        }
        catch (EntityNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rolling back entity {EntityId}", entityId);
            throw;
        }
    }

    #region Helper Methods

    private async Task CreateAuditRecordAsync(
        string entityId,
        string action,
        string? performedBy,
        DateTime timestamp,
        string? versionBefore,
        string? versionAfter,
        Dictionary<string, object> changeDetails)
    {
        var session = _driver.AsyncSession(config => config.WithDatabase(_database));

        try
        {
            var auditId = Guid.NewGuid().ToString();

            var query = @"
                MATCH (e)
                WHERE e.id = $entityId
                CREATE (a:AuditRecord {
                    audit_id: $auditId,
                    entity_id: $entityId,
                    action: $action,
                    performed_by: $performedBy,
                    timestamp: $timestamp,
                    version_before: $versionBefore,
                    version_after: $versionAfter,
                    change_details: $changeDetails,
                    source_ip: $sourceIp
                })
                CREATE (e)-[:HAS_AUDIT]->(a)
            ";

            var parameters = new Dictionary<string, object>
            {
                { "auditId", auditId },
                { "entityId", entityId },
                { "action", action },
                { "performedBy", performedBy ?? "system" },
                { "timestamp", timestamp.ToString("O") },
                { "versionBefore", versionBefore ?? string.Empty },
                { "versionAfter", versionAfter ?? string.Empty },
                { "changeDetails", JsonSerializer.Serialize(changeDetails) },
                { "sourceIp", "unknown" }
            };

            await session.RunAsync(query, parameters);
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    private string GenerateChangeDescription(Dictionary<string, object> changedProperties)
    {
        if (changedProperties.Count == 0)
        {
            return "No properties changed";
        }

        var propNames = string.Join(", ", changedProperties.Keys.Take(3));
        var count = changedProperties.Count;

        if (count <= 3)
        {
            return $"Updated: {propNames}";
        }
        else
        {
            return $"Updated: {propNames} and {count - 3} more";
        }
    }

    private bool AreValuesEqual(object? valueA, object? valueB)
    {
        if (valueA == null && valueB == null) return true;
        if (valueA == null || valueB == null) return false;

        // Use JSON serialization for comparison
        var jsonA = JsonSerializer.Serialize(valueA);
        var jsonB = JsonSerializer.Serialize(valueB);

        return jsonA == jsonB;
    }

    private string IncrementVersion(string currentVersion)
    {
        var parts = currentVersion.Split('.');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var major) || !int.TryParse(parts[1], out var minor))
        {
            return "1.0";
        }

        minor++;
        if (minor >= 100)
        {
            major++;
            minor = 0;
        }

        return $"{major}.{minor}";
    }

    #endregion
}
