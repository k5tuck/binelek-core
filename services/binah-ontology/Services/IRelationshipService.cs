using Binah.Ontology.Models;
using Binah.Ontology.Models.Relationship;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Binah.Ontology.Services;

/// <summary>
/// Service interface for managing relationships between ontology entities
/// </summary>
public interface IRelationshipService
{
    /// <summary>
    /// Creates a new relationship between two entities
    /// </summary>
    /// <param name="type">Relationship type (e.g., HAS_CONTRACTOR, INVESTS_IN)</param>
    /// <param name="fromEntityId">Source entity ID</param>
    /// <param name="toEntityId">Target entity ID</param>
    /// <param name="properties">Optional relationship properties</param>
    /// <param name="createdBy">User or system identifier</param>
    /// <returns>The created relationship</returns>
    /// <exception cref="EntityNotFoundException">Thrown when either entity is not found</exception>
    /// <exception cref="RelationshipCreationException">Thrown when relationship creation fails</exception>
    Task<Relationship> CreateRelationshipAsync(
        string type,
        string fromEntityId,
        string toEntityId,
        Dictionary<string, object>? properties = null,
        string? createdBy = null
    );

    /// <summary>
    /// Retrieves all relationships for a given entity
    /// </summary>
    /// <param name="entityId">The entity ID</param>
    /// <param name="direction">Relationship direction (incoming, outgoing, both)</param>
    /// <param name="relationshipType">Optional filter by relationship type</param>
    /// <returns>List of relationships</returns>
    Task<List<Relationship>> GetRelationshipsAsync(
        string entityId,
        RelationshipDirection direction = RelationshipDirection.Both,
        string? relationshipType = null
    );

    /// <summary>
    /// Deletes a relationship between two entities
    /// </summary>
    /// <param name="type">Relationship type</param>
    /// <param name="fromEntityId">Source entity ID</param>
    /// <param name="toEntityId">Target entity ID</param>
    /// <param name="deletedBy">User or system identifier</param>
    /// <returns>True if deletion was successful</returns>
    /// <exception cref="RelationshipNotFoundException">Thrown when relationship is not found</exception>
    Task<bool> DeleteRelationshipAsync(
        string type,
        string fromEntityId,
        string toEntityId,
        string? deletedBy = null
    );

    /// <summary>
    /// Checks if a relationship exists between two entities
    /// </summary>
    /// <param name="type">Relationship type</param>
    /// <param name="fromEntityId">Source entity ID</param>
    /// <param name="toEntityId">Target entity ID</param>
    /// <returns>True if relationship exists</returns>
    Task<bool> RelationshipExistsAsync(string type, string fromEntityId, string toEntityId);
}

/// <summary>
/// Direction of relationship traversal
/// </summary>
public enum RelationshipDirection
{
    /// <summary>Incoming relationships (pointing to the entity)</summary>
    Incoming,
    /// <summary>Outgoing relationships (pointing from the entity)</summary>
    Outgoing,
    /// <summary>Both incoming and outgoing relationships</summary>
    Both
}
