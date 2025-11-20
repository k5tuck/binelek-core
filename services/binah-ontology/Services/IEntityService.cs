using Binah.Ontology.Models;
using Binah.Ontology.Models.Base;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Binah.Ontology.Services;

/// <summary>
/// Service interface for managing ontology entities with CRUD operations
/// </summary>
public interface IEntityService
{
    /// <summary>
    /// Creates a new entity in the graph database
    /// </summary>
    /// <param name="type">The entity type (e.g., Project, Contractor, Property)</param>
    /// <param name="properties">Dictionary of entity properties</param>
    /// <param name="createdBy">User or system identifier who created the entity</param>
    /// <param name="tenantId">Multi-tenancy identifier</param>
    /// <returns>The created entity with generated ID and metadata</returns>
    /// <exception cref="EntityCreationException">Thrown when entity creation fails</exception>
    Task<Entity> CreateEntityAsync(
        string type,
        Dictionary<string, object> properties,
        string? createdBy = null,
        string? tenantId = null
    );

    /// <summary>
    /// Retrieves a single entity by its unique identifier
    /// </summary>
    /// <param name="entityId">The unique entity identifier</param>
    /// <returns>The entity if found, null otherwise</returns>
    /// <exception cref="EntityNotFoundException">Thrown when entity is not found</exception>
    Task<Entity?> GetEntityByIdAsync(string entityId);

    /// <summary>
    /// Retrieves all entities of a specific type with pagination support
    /// </summary>
    /// <param name="type">The entity type to filter by</param>
    /// <param name="skip">Number of records to skip for pagination</param>
    /// <param name="limit">Maximum number of records to return</param>
    /// <returns>List of entities matching the type</returns>
    Task<List<Entity>> GetEntitiesByTypeAsync(string type, int skip = 0, int limit = 100);

    /// <summary>
    /// Updates an existing entity's properties
    /// </summary>
    /// <param name="entityId">The unique entity identifier</param>
    /// <param name="properties">Dictionary of properties to update</param>
    /// <param name="updatedBy">User or system identifier who updated the entity</param>
    /// <returns>The updated entity with new version</returns>
    /// <exception cref="EntityNotFoundException">Thrown when entity is not found</exception>
    /// <exception cref="EntityUpdateException">Thrown when update fails</exception>
    Task<Entity> UpdateEntityAsync(
        string entityId,
        Dictionary<string, object> properties,
        string? updatedBy = null
    );

    /// <summary>
    /// Soft deletes an entity (marks as deleted without removing from database)
    /// </summary>
    /// <param name="entityId">The unique entity identifier</param>
    /// <param name="deletedBy">User or system identifier who deleted the entity</param>
    /// <returns>True if deletion was successful</returns>
    /// <exception cref="EntityNotFoundException">Thrown when entity is not found</exception>
    /// <exception cref="EntityDeletionException">Thrown when deletion fails</exception>
    Task<bool> DeleteEntityAsync(string entityId, string? deletedBy = null);

    /// <summary>
    /// Performs full-text search across entities
    /// </summary>
    /// <param name="searchTerm">The search term to match against name and description</param>
    /// <param name="type">Optional entity type filter</param>
    /// <param name="limit">Maximum number of results</param>
    /// <returns>List of entities matching the search criteria</returns>
    Task<List<Entity>> SearchEntitiesAsync(string searchTerm, string? type = null, int limit = 50);

    /// <summary>
    /// Checks if an entity exists by ID
    /// </summary>
    /// <param name="entityId">The unique entity identifier</param>
    /// <returns>True if entity exists, false otherwise</returns>
    Task<bool> EntityExistsAsync(string entityId);
}
