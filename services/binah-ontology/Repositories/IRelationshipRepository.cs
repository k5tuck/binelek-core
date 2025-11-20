using Binah.Ontology.Models;
using Binah.Ontology.Services;
using Binah.Ontology.Models.Relationship;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Binah.Ontology.Repositories;

/// <summary>
/// Repository interface for relationship data access
/// </summary>
public interface IRelationshipRepository
{
    /// <summary>
    /// Creates a new relationship in Neo4j
    /// </summary>
    Task<Relationship> CreateAsync(Relationship relationship);

    /// <summary>
    /// Retrieves a specific relationship
    /// </summary>
    Task<Relationship?> GetAsync(string type, string fromEntityId, string toEntityId);

    /// <summary>
    /// Retrieves all relationships for an entity
    /// </summary>
    Task<List<Relationship>> GetForEntityAsync(
        string entityId,
        RelationshipDirection direction,
        string? relationshipType);

    /// <summary>
    /// Deletes a relationship
    /// </summary>
    Task<bool> DeleteAsync(string type, string fromEntityId, string toEntityId);
}
