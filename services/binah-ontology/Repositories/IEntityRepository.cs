using Binah.Ontology.Models;
using Binah.Ontology.Models.Base;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Binah.Ontology.Repositories;

/// <summary>
/// Repository interface for entity data access
/// </summary>
public interface IEntityRepository
{
    /// <summary>
    /// Creates a new entity in Neo4j
    /// </summary>
    Task<Entity> CreateAsync(Entity entity);

    /// <summary>
    /// Retrieves an entity by ID
    /// </summary>
    Task<Entity?> GetByIdAsync(string entityId);

    /// <summary>
    /// Retrieves entities by type with pagination
    /// </summary>
    Task<List<Entity>> GetByTypeAsync(string type, int skip, int limit);

    /// <summary>
    /// Updates an existing entity
    /// </summary>
    Task<Entity> UpdateAsync(Entity entity);

    /// <summary>
    /// Searches entities using full-text search
    /// </summary>
    Task<List<Entity>> SearchAsync(string searchTerm, string? type, int limit);

    /// <summary>
    /// Checks if an entity exists
    /// </summary>
    Task<bool> ExistsAsync(string entityId);
}
