using Binah.Ontology.Models;
using Binah.Ontology.Models.Base;
using Binah.Ontology.Models.Exceptions;
using Binah.Ontology.Repositories;
using Binah.Ontology.Pipelines.DataNetwork;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Binah.Ontology.Services;

/// <summary>
/// Implementation of entity management service
/// </summary>
public class EntityService : IEntityService
{
    private readonly IEntityRepository _entityRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILineageService _lineageService;
    private readonly ILogger<EntityService> _logger;
    private readonly IDataNetworkPipeline? _dataNetworkPipeline;

    public EntityService(
        IEntityRepository entityRepository,
        IEventPublisher eventPublisher,
        ILineageService lineageService,
        ILogger<EntityService> logger,
        IDataNetworkPipeline? dataNetworkPipeline = null)
    {
        _entityRepository = entityRepository ?? throw new ArgumentNullException(nameof(entityRepository));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _lineageService = lineageService ?? throw new ArgumentNullException(nameof(lineageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dataNetworkPipeline = dataNetworkPipeline; // Optional dependency
    }

    /// <inheritdoc/>
    public async Task<Entity> CreateEntityAsync(
        string type,
        Dictionary<string, object> properties,
        string? createdBy = null,
        string? tenantId = null)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentException("Entity type cannot be null or empty", nameof(type));
        }

        if (properties == null || properties.Count == 0)
        {
            throw new ArgumentException("Entity properties cannot be null or empty", nameof(properties));
        }

        try
        {
            _logger.LogInformation("Creating entity of type {EntityType}", type);

            // Generate unique ID
            var entityId = $"{type.ToLower()}-{Guid.NewGuid().ToString("N")[..12]}";

            // Create entity object
            var entity = new Entity
            {
                Id = entityId,
                Type = type,
                Properties = properties,
                Version = "1.0",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                TenantId = tenantId,
                Source = "Binah.Ontology"
            };

            // Persist to Neo4j
            var createdEntity = await _entityRepository.CreateAsync(entity);

            if (createdEntity == null)
            {
                throw new EntityCreationException(type, "Failed to persist entity to database");
            }

            // Record initial version in lineage
            await _lineageService.RecordVersionAsync(
                entityId,
                "0.0",
                "1.0",
                properties,
                createdBy
            );

            // Publish entity created event
            var eventPublished = await _eventPublisher.PublishEntityCreatedAsync(new EntityCreatedEvent
            {
                EntityId = createdEntity.Id,
                EntityType = createdEntity.Type,
                Properties = createdEntity.Properties,
                Version = createdEntity.Version,
                TriggeredBy = createdBy,
                TenantId = tenantId
            });

            if (!eventPublished)
            {
                _logger.LogWarning("Failed to publish entity created event for {EntityId}", entityId);
            }

            // Fire-and-forget: Contribute to data network if pipeline is available
            // This runs asynchronously and doesn't block the entity creation response
            // NOTE: Using stub implementation until Regen generates Finance domain code
            if (_dataNetworkPipeline != null && !string.IsNullOrEmpty(tenantId))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _dataNetworkPipeline.ProcessEntityAsync(createdEntity);
                    }
                    catch (Exception pipelineEx)
                    {
                        // Log but don't throw - data network contribution failures shouldn't affect entity creation
                        _logger.LogWarning(pipelineEx,
                            "Data network contribution failed for entity {EntityId}. Entity created successfully.",
                            entityId);
                    }
                });
            }

            _logger.LogInformation("Successfully created entity {EntityId} of type {EntityType}", entityId, type);

            return createdEntity;
        }
        catch (EntityCreationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating entity of type {EntityType}", type);
            throw new EntityCreationException(type, "An unexpected error occurred during entity creation", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<Entity?> GetEntityByIdAsync(string entityId)
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArgumentException("Entity ID cannot be null or empty", nameof(entityId));
        }

        try
        {
            _logger.LogDebug("Retrieving entity {EntityId}", entityId);

            var entity = await _entityRepository.GetByIdAsync(entityId);

            if (entity == null)
            {
                _logger.LogWarning("Entity {EntityId} not found", entityId);
                return null;
            }

            // Filter out soft-deleted entities
            if (entity.IsDeleted)
            {
                _logger.LogWarning("Entity {EntityId} is deleted", entityId);
                return null;
            }

            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving entity {EntityId}", entityId);
            throw new EntityNotFoundException(entityId, $"Failed to retrieve entity: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<List<Entity>> GetEntitiesByTypeAsync(string type, int skip = 0, int limit = 100)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentException("Entity type cannot be null or empty", nameof(type));
        }

        if (skip < 0)
        {
            throw new ArgumentException("Skip value cannot be negative", nameof(skip));
        }

        if (limit <= 0 || limit > 1000)
        {
            throw new ArgumentException("Limit must be between 1 and 1000", nameof(limit));
        }

        try
        {
            _logger.LogDebug("Retrieving entities of type {EntityType} (skip: {Skip}, limit: {Limit})",
                type, skip, limit);

            var entities = await _entityRepository.GetByTypeAsync(type, skip, limit);

            // Filter out soft-deleted entities
            var activeEntities = entities.Where(e => !e.IsDeleted).ToList();

            _logger.LogDebug("Retrieved {Count} entities of type {EntityType}", activeEntities.Count, type);

            return activeEntities;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving entities of type {EntityType}", type);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Entity> UpdateEntityAsync(
        string entityId,
        Dictionary<string, object> properties,
        string? updatedBy = null)
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArgumentException("Entity ID cannot be null or empty", nameof(entityId));
        }

        if (properties == null || properties.Count == 0)
        {
            throw new ArgumentException("Properties cannot be null or empty", nameof(properties));
        }

        try
        {
            _logger.LogInformation("Updating entity {EntityId}", entityId);

            // Get existing entity
            var existingEntity = await GetEntityByIdAsync(entityId);

            if (existingEntity == null)
            {
                throw new EntityNotFoundException(entityId);
            }

            // Increment version
            var currentVersion = existingEntity.Version;
            var newVersion = IncrementVersion(currentVersion);

            // Merge properties
            var updatedProperties = new Dictionary<string, object>(existingEntity.Properties);
            foreach (var prop in properties)
            {
                updatedProperties[prop.Key] = prop.Value;
            }

            // Update entity
            existingEntity.Properties = updatedProperties;
            existingEntity.Version = newVersion;
            existingEntity.UpdatedAt = DateTime.UtcNow;
            existingEntity.UpdatedBy = updatedBy;

            // Persist to database
            var updatedEntity = await _entityRepository.UpdateAsync(existingEntity);

            if (updatedEntity == null)
            {
                throw new EntityUpdateException(entityId, "Failed to persist updated entity to database");
            }

            // Record version in lineage
            await _lineageService.RecordVersionAsync(
                entityId,
                currentVersion,
                newVersion,
                properties,
                updatedBy
            );

            // Publish entity updated event
            var eventPublished = await _eventPublisher.PublishEntityUpdatedAsync(new EntityUpdatedEvent
            {
                EntityId = updatedEntity.Id,
                EntityType = updatedEntity.Type,
                ChangedProperties = properties,
                PreviousVersion = currentVersion,
                NewVersion = newVersion,
                TriggeredBy = updatedBy,
                TenantId = existingEntity.TenantId
            });

            if (!eventPublished)
            {
                _logger.LogWarning("Failed to publish entity updated event for {EntityId}", entityId);
            }

            _logger.LogInformation("Successfully updated entity {EntityId} to version {Version}",
                entityId, newVersion);

            return updatedEntity;
        }
        catch (EntityNotFoundException)
        {
            throw;
        }
        catch (EntityUpdateException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating entity {EntityId}", entityId);
            throw new EntityUpdateException(entityId, "An unexpected error occurred during entity update", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteEntityAsync(string entityId, string? deletedBy = null)
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArgumentException("Entity ID cannot be null or empty", nameof(entityId));
        }

        try
        {
            _logger.LogInformation("Deleting entity {EntityId} (soft delete)", entityId);

            // Get existing entity
            var existingEntity = await GetEntityByIdAsync(entityId);

            if (existingEntity == null)
            {
                throw new EntityNotFoundException(entityId);
            }

            // Mark as deleted
            existingEntity.IsDeleted = true;
            existingEntity.DeletedAt = DateTime.UtcNow;
            existingEntity.DeletedBy = deletedBy;
            existingEntity.UpdatedAt = DateTime.UtcNow;

            // Persist to database
            var result = await _entityRepository.UpdateAsync(existingEntity);

            if (result == null)
            {
                throw new EntityDeletionException(entityId, "Failed to mark entity as deleted in database");
            }

            // Publish entity deleted event
            var eventPublished = await _eventPublisher.PublishEntityDeletedAsync(new EntityDeletedEvent
            {
                EntityId = existingEntity.Id,
                EntityType = existingEntity.Type,
                Version = existingEntity.Version,
                TriggeredBy = deletedBy,
                TenantId = existingEntity.TenantId
            });

            if (!eventPublished)
            {
                _logger.LogWarning("Failed to publish entity deleted event for {EntityId}", entityId);
            }

            _logger.LogInformation("Successfully deleted entity {EntityId}", entityId);

            return true;
        }
        catch (EntityNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting entity {EntityId}", entityId);
            throw new EntityDeletionException(entityId, "An unexpected error occurred during entity deletion", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<List<Entity>> SearchEntitiesAsync(string searchTerm, string? type = null, int limit = 50)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            throw new ArgumentException("Search term cannot be null or empty", nameof(searchTerm));
        }

        if (limit <= 0 || limit > 1000)
        {
            throw new ArgumentException("Limit must be between 1 and 1000", nameof(limit));
        }

        try
        {
            _logger.LogDebug("Searching entities with term '{SearchTerm}' (type: {Type}, limit: {Limit})",
                searchTerm, type ?? "all", limit);

            var entities = await _entityRepository.SearchAsync(searchTerm, type, limit);

            // Filter out soft-deleted entities
            var activeEntities = entities.Where(e => !e.IsDeleted).ToList();

            _logger.LogDebug("Found {Count} entities matching search term '{SearchTerm}'",
                activeEntities.Count, searchTerm);

            return activeEntities;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching entities with term '{SearchTerm}'", searchTerm);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> EntityExistsAsync(string entityId)
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArgumentException("Entity ID cannot be null or empty", nameof(entityId));
        }

        try
        {
            var entity = await GetEntityByIdAsync(entityId);
            return entity != null;
        }
        catch (EntityNotFoundException)
        {
            return false;
        }
    }

    /// <summary>
    /// Increments version number (e.g., "1.0" -> "1.1")
    /// </summary>
    private string IncrementVersion(string currentVersion)
    {
        var parts = currentVersion.Split('.');

        if (parts.Length != 2 || !int.TryParse(parts[0], out var major) || !int.TryParse(parts[1], out var minor))
        {
            return "1.0";
        }

        minor++;

        // Roll over to next major version after 99 minor versions
        if (minor >= 100)
        {
            major++;
            minor = 0;
        }

        return $"{major}.{minor}";
    }
}
