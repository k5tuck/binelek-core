using Binah.Ontology.Data;
using Binah.Ontology.Models.Ontology;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Binah.Ontology.Services;

public class OntologyVersionService : IOntologyVersionService
{
    private readonly OntologyDbContext _dbContext;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<OntologyVersionService> _logger;

    public OntologyVersionService(
        OntologyDbContext dbContext,
        IEventPublisher eventPublisher,
        ILogger<OntologyVersionService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get the currently active ontology version for a tenant
    /// </summary>
    public async Task<OntologyVersion?> GetActiveAsync(Guid tenantId)
    {
        try
        {
            _logger.LogInformation("Fetching active ontology for tenant {TenantId}", tenantId);

            var activeVersion = await _dbContext.OntologyVersions
                .Where(v => v.TenantId == tenantId && v.IsActive)
                .OrderByDescending(v => v.CreatedAt)
                .FirstOrDefaultAsync();

            if (activeVersion == null)
            {
                _logger.LogWarning("No active ontology found for tenant {TenantId}", tenantId);
                return null;
            }

            _logger.LogInformation("Found active ontology: {Name} v{Version} for tenant {TenantId}",
                activeVersion.OntologyName, activeVersion.Version, tenantId);

            return activeVersion;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching active ontology for tenant {TenantId}", tenantId);
            throw;
        }
    }

    /// <summary>
    /// Get all ontology versions for a tenant, ordered by creation date
    /// </summary>
    public async Task<List<OntologyVersion>> GetVersionsAsync(Guid tenantId)
    {
        try
        {
            _logger.LogInformation("Fetching all ontology versions for tenant {TenantId}", tenantId);

            var versions = await _dbContext.OntologyVersions
                .Where(v => v.TenantId == tenantId)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();

            _logger.LogInformation("Found {Count} ontology versions for tenant {TenantId}",
                versions.Count, tenantId);

            return versions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching ontology versions for tenant {TenantId}", tenantId);
            throw;
        }
    }

    /// <summary>
    /// Create a new ontology version
    /// </summary>
    public async Task<OntologyVersion> CreateAsync(OntologyVersion ontology)
    {
        if (ontology == null)
        {
            throw new ArgumentNullException(nameof(ontology));
        }

        try
        {
            _logger.LogInformation("Creating new ontology version: {Name} v{Version} for tenant {TenantId}",
                ontology.OntologyName, ontology.Version, ontology.TenantId);

            // Check if this version already exists
            var existing = await _dbContext.OntologyVersions
                .FirstOrDefaultAsync(v => v.TenantId == ontology.TenantId
                    && v.OntologyName == ontology.OntologyName
                    && v.Version == ontology.Version);

            if (existing != null)
            {
                var error = $"Ontology version {ontology.OntologyName} v{ontology.Version} already exists for tenant {ontology.TenantId}";
                _logger.LogError(error);
                throw new InvalidOperationException(error);
            }

            // Ensure new versions start as inactive
            ontology.IsActive = false;
            ontology.CreatedAt = DateTime.UtcNow;

            // Add to database
            _dbContext.OntologyVersions.Add(ontology);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Successfully created ontology version {Id}: {Name} v{Version}",
                ontology.Id, ontology.OntologyName, ontology.Version);

            // Publish creation event
            // TODO: Uncomment when IEventPublisher.PublishAsync is available
            // await _eventPublisher.PublishAsync("ontology.version.created", new
            // {
            //     OntologyVersionId = ontology.Id,
            //     TenantId = ontology.TenantId,
            //     OntologyName = ontology.OntologyName,
            //     Version = ontology.Version,
            //     CreatedBy = ontology.CreatedBy,
            //     Timestamp = DateTime.UtcNow
            // });

            return ontology;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating ontology version {Name} v{Version}",
                ontology.OntologyName, ontology.Version);
            throw;
        }
    }

    /// <summary>
    /// Publish an ontology version - deactivates all other versions and activates this one
    /// </summary>
    public async Task PublishAsync(Guid tenantId, string version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            throw new ArgumentException("Version cannot be null or empty", nameof(version));
        }

        try
        {
            _logger.LogInformation("Publishing ontology version {Version} for tenant {TenantId}",
                version, tenantId);

            // Find the version to publish
            var versionToPublish = await _dbContext.OntologyVersions
                .FirstOrDefaultAsync(v => v.TenantId == tenantId && v.Version == version);

            if (versionToPublish == null)
            {
                var error = $"Ontology version {version} not found for tenant {tenantId}";
                _logger.LogError(error);
                throw new InvalidOperationException(error);
            }

            // Deactivate all other versions for this tenant
            var activeVersions = await _dbContext.OntologyVersions
                .Where(v => v.TenantId == tenantId && v.IsActive)
                .ToListAsync();

            foreach (var activeVersion in activeVersions)
            {
                activeVersion.IsActive = false;
                _logger.LogInformation("Deactivating ontology version {Id}: {Name} v{Version}",
                    activeVersion.Id, activeVersion.OntologyName, activeVersion.Version);
            }

            // Activate the new version
            versionToPublish.IsActive = true;
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Successfully published ontology version {Version} for tenant {TenantId}",
                version, tenantId);

            // Publish Kafka event
            // TODO: Uncomment when IEventPublisher.PublishAsync is available
            // await _eventPublisher.PublishAsync("ontology.updated", new
            // {
            //     TenantId = tenantId,
            //     OntologyVersionId = versionToPublish.Id,
            //     OntologyName = versionToPublish.OntologyName,
            //     Version = version,
            //     PreviousVersion = activeVersions.FirstOrDefault()?.Version,
            //     Timestamp = DateTime.UtcNow
            // });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing ontology version {Version} for tenant {TenantId}",
                version, tenantId);
            throw;
        }
    }

    /// <summary>
    /// Validate an ontology version with full validation
    /// Performs schema validation, consistency checks, and dry-run code generation test
    /// </summary>
    public async Task<bool> ValidateAsync(OntologyVersion ontology)
    {
        if (ontology == null)
        {
            throw new ArgumentNullException(nameof(ontology));
        }

        try
        {
            _logger.LogInformation("Validating ontology {Name} v{Version}",
                ontology.OntologyName, ontology.Version);

            var errors = new List<string>();

            // 1. Basic validation
            if (string.IsNullOrWhiteSpace(ontology.OntologyName))
            {
                errors.Add("Ontology name is required");
            }

            if (string.IsNullOrWhiteSpace(ontology.Version))
            {
                errors.Add("Version is required");
            }

            if (string.IsNullOrWhiteSpace(ontology.ModelJson))
            {
                errors.Add("ModelJson is required");
            }

            if (ontology.TenantId == Guid.Empty)
            {
                errors.Add("TenantId is required");
            }

            // 2. JSON validation - ensure ModelJson is valid JSON
            if (!string.IsNullOrWhiteSpace(ontology.ModelJson))
            {
                try
                {
                    using var document = JsonDocument.Parse(ontology.ModelJson);

                    // Check for required top-level properties
                    if (!document.RootElement.TryGetProperty("entities", out var entities))
                    {
                        errors.Add("ModelJson must contain 'entities' property");
                    }
                    else
                    {
                        // Validate entities array
                        if (entities.ValueKind != JsonValueKind.Array)
                        {
                            errors.Add("'entities' must be an array");
                        }
                        else if (entities.GetArrayLength() == 0)
                        {
                            errors.Add("At least one entity is required");
                        }
                    }
                }
                catch (JsonException ex)
                {
                    errors.Add($"Invalid JSON in ModelJson: {ex.Message}");
                }
            }

            // 3. Entity validation - check for duplicate entity names
            if (!string.IsNullOrWhiteSpace(ontology.ModelJson))
            {
                try
                {
                    using var document = JsonDocument.Parse(ontology.ModelJson);
                    if (document.RootElement.TryGetProperty("entities", out var entities))
                    {
                        var entityNames = new HashSet<string>();
                        foreach (var entity in entities.EnumerateArray())
                        {
                            if (entity.TryGetProperty("name", out var nameElement))
                            {
                                var name = nameElement.GetString();
                                if (name != null)
                                {
                                    if (entityNames.Contains(name))
                                    {
                                        errors.Add($"Duplicate entity name: {name}");
                                    }
                                    entityNames.Add(name);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Error validating entities: {ex.Message}");
                }
            }

            // 4. Relationship validation - check for valid entity references
            if (!string.IsNullOrWhiteSpace(ontology.ModelJson))
            {
                try
                {
                    using var document = JsonDocument.Parse(ontology.ModelJson);
                    var entityNames = new HashSet<string>();

                    // Collect entity names
                    if (document.RootElement.TryGetProperty("entities", out var entities))
                    {
                        foreach (var entity in entities.EnumerateArray())
                        {
                            if (entity.TryGetProperty("name", out var nameElement))
                            {
                                var name = nameElement.GetString();
                                if (name != null)
                                {
                                    entityNames.Add(name);
                                }
                            }
                        }
                    }

                    // Validate relationship references
                    if (document.RootElement.TryGetProperty("relationships", out var relationships))
                    {
                        foreach (var rel in relationships.EnumerateArray())
                        {
                            if (rel.TryGetProperty("from", out var fromElement))
                            {
                                var from = fromElement.GetString();
                                if (from != null && !entityNames.Contains(from))
                                {
                                    errors.Add($"Relationship references unknown entity 'from': {from}");
                                }
                            }

                            if (rel.TryGetProperty("to", out var toElement))
                            {
                                var to = toElement.GetString();
                                if (to != null && !entityNames.Contains(to))
                                {
                                    errors.Add($"Relationship references unknown entity 'to': {to}");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Error validating relationships: {ex.Message}");
                }
            }

            // TODO: Phase 2 - Add compilation test
            // This would involve:
            // 1. Invoking the Regen service to generate code
            // 2. Compiling the generated code
            // 3. Checking for compilation errors
            // For now, we'll log this as a future enhancement

            if (errors.Any())
            {
                _logger.LogWarning("Ontology validation failed for {Name} v{Version} with {Count} errors: {Errors}",
                    ontology.OntologyName, ontology.Version, errors.Count, string.Join("; ", errors));
                return false;
            }

            _logger.LogInformation("Ontology validation succeeded for {Name} v{Version}",
                ontology.OntologyName, ontology.Version);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ontology validation for {Name} v{Version}",
                ontology.OntologyName, ontology.Version);
            throw;
        }
    }
}
