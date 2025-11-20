using Binah.Ontology.Models;
using Binah.Ontology.Models.Exceptions;
using Binah.Ontology.Repositories;
using Binah.Ontology.Models.Relationship;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Binah.Ontology.Services;

/// <summary>
/// Implementation of relationship management service
/// </summary>
public class RelationshipService : IRelationshipService
{
    private readonly IRelationshipRepository _relationshipRepository;
    private readonly IEntityRepository _entityRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<RelationshipService> _logger;

    public RelationshipService(
        IRelationshipRepository relationshipRepository,
        IEntityRepository entityRepository,
        IEventPublisher eventPublisher,
        ILogger<RelationshipService> logger)
    {
        _relationshipRepository = relationshipRepository ?? throw new ArgumentNullException(nameof(relationshipRepository));
        _entityRepository = entityRepository ?? throw new ArgumentNullException(nameof(entityRepository));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<Relationship> CreateRelationshipAsync(
        string type,
        string fromEntityId,
        string toEntityId,
        Dictionary<string, object>? properties = null,
        string? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentException("Relationship type cannot be null or empty", nameof(type));
        }

        if (string.IsNullOrWhiteSpace(fromEntityId))
        {
            throw new ArgumentException("From entity ID cannot be null or empty", nameof(fromEntityId));
        }

        if (string.IsNullOrWhiteSpace(toEntityId))
        {
            throw new ArgumentException("To entity ID cannot be null or empty", nameof(toEntityId));
        }

        try
        {
            _logger.LogInformation("Creating relationship {Type} from {FromId} to {ToId}",
                type, fromEntityId, toEntityId);

            // Verify both entities exist
            var fromEntityExists = await _entityRepository.ExistsAsync(fromEntityId);
            if (!fromEntityExists)
            {
                throw new EntityNotFoundException(fromEntityId, $"Source entity '{fromEntityId}' not found");
            }

            var toEntityExists = await _entityRepository.ExistsAsync(toEntityId);
            if (!toEntityExists)
            {
                throw new EntityNotFoundException(toEntityId, $"Target entity '{toEntityId}' not found");
            }

            // Check if relationship already exists
            var existingRelationship = await _relationshipRepository.GetAsync(type, fromEntityId, toEntityId);
            if (existingRelationship != null)
            {
                throw new RelationshipCreationException(
                    type, fromEntityId, toEntityId,
                    "Relationship already exists");
            }

            // Create relationship
            var relationship = new Relationship
            {
                Type = type,
                FromEntityId = fromEntityId,
                ToEntityId = toEntityId,
                Properties = properties,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy
            };

            var createdRelationship = await _relationshipRepository.CreateAsync(relationship);

            if (createdRelationship == null)
            {
                throw new RelationshipCreationException(
                    type, fromEntityId, toEntityId,
                    "Failed to persist relationship to database");
            }

            // Publish relationship created event
            var eventPublished = await _eventPublisher.PublishRelationshipCreatedAsync(
                new RelationshipCreatedEvent
                {
                    RelationshipType = type,
                    FromEntityId = fromEntityId,
                    ToEntityId = toEntityId,
                    Properties = properties,
                    TriggeredBy = createdBy
                });

            if (!eventPublished)
            {
                _logger.LogWarning("Failed to publish relationship created event for {Type}", type);
            }

            _logger.LogInformation("Successfully created relationship {Type} from {FromId} to {ToId}",
                type, fromEntityId, toEntityId);

            return createdRelationship;
        }
        catch (EntityNotFoundException)
        {
            throw;
        }
        catch (RelationshipCreationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating relationship {Type}", type);
            throw new RelationshipCreationException(
                type, fromEntityId, toEntityId,
                "An unexpected error occurred during relationship creation", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<List<Relationship>> GetRelationshipsAsync(
        string entityId,
        RelationshipDirection direction = RelationshipDirection.Both,
        string? relationshipType = null)
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArgumentException("Entity ID cannot be null or empty", nameof(entityId));
        }

        try
        {
            _logger.LogDebug("Retrieving relationships for entity {EntityId} (direction: {Direction}, type: {Type})",
                entityId, direction, relationshipType ?? "all");

            // Verify entity exists
            var entityExists = await _entityRepository.ExistsAsync(entityId);
            if (!entityExists)
            {
                throw new EntityNotFoundException(entityId);
            }

            var relationships = await _relationshipRepository.GetForEntityAsync(entityId, direction, relationshipType);

            _logger.LogDebug("Found {Count} relationships for entity {EntityId}",
                relationships.Count, entityId);

            return relationships;
        }
        catch (EntityNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving relationships for entity {EntityId}", entityId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteRelationshipAsync(
        string type,
        string fromEntityId,
        string toEntityId,
        string? deletedBy = null)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentException("Relationship type cannot be null or empty", nameof(type));
        }

        if (string.IsNullOrWhiteSpace(fromEntityId))
        {
            throw new ArgumentException("From entity ID cannot be null or empty", nameof(fromEntityId));
        }

        if (string.IsNullOrWhiteSpace(toEntityId))
        {
            throw new ArgumentException("To entity ID cannot be null or empty", nameof(toEntityId));
        }

        try
        {
            _logger.LogInformation("Deleting relationship {Type} from {FromId} to {ToId}",
                type, fromEntityId, toEntityId);

            // Verify relationship exists
            var existingRelationship = await _relationshipRepository.GetAsync(type, fromEntityId, toEntityId);
            if (existingRelationship == null)
            {
                throw new RelationshipNotFoundException(type, fromEntityId, toEntityId);
            }

            // Delete the relationship
            var result = await _relationshipRepository.DeleteAsync(type, fromEntityId, toEntityId);

            if (!result)
            {
                _logger.LogWarning("Failed to delete relationship {Type} from {FromId} to {ToId}",
                    type, fromEntityId, toEntityId);
                return false;
            }

            // Publish relationship deleted event
            var eventPublished = await _eventPublisher.PublishRelationshipDeletedAsync(
                new RelationshipDeletedEvent
                {
                    RelationshipType = type,
                    FromEntityId = fromEntityId,
                    ToEntityId = toEntityId,
                    TriggeredBy = deletedBy
                });

            if (!eventPublished)
            {
                _logger.LogWarning("Failed to publish relationship deleted event for {Type}", type);
            }

            _logger.LogInformation("Successfully deleted relationship {Type} from {FromId} to {ToId}",
                type, fromEntityId, toEntityId);

            return true;
        }
        catch (RelationshipNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting relationship {Type}", type);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> RelationshipExistsAsync(string type, string fromEntityId, string toEntityId)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentException("Relationship type cannot be null or empty", nameof(type));
        }

        if (string.IsNullOrWhiteSpace(fromEntityId))
        {
            throw new ArgumentException("From entity ID cannot be null or empty", nameof(fromEntityId));
        }

        if (string.IsNullOrWhiteSpace(toEntityId))
        {
            throw new ArgumentException("To entity ID cannot be null or empty", nameof(toEntityId));
        }

        try
        {
            var relationship = await _relationshipRepository.GetAsync(type, fromEntityId, toEntityId);
            return relationship != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if relationship exists");
            return false;
        }
    }
}
