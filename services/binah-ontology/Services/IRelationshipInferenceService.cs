using System.Collections.Generic;
using System.Threading.Tasks;

namespace Binah.Ontology.Services;

/// <summary>
/// Service interface for inferring relationships between entities
/// </summary>
public interface IRelationshipInferenceService
{
    /// <summary>
    /// Infers relationships between newly created entities and existing entities
    /// </summary>
    /// <param name="tenantId">Tenant ID for isolation</param>
    /// <param name="entityIds">List of entity IDs to infer relationships for</param>
    /// <returns>Number of relationships created</returns>
    Task<int> InferRelationshipsAsync(string tenantId, List<string> entityIds);

    /// <summary>
    /// Infers relationships for a single entity
    /// </summary>
    /// <param name="tenantId">Tenant ID for isolation</param>
    /// <param name="entityId">Entity ID to infer relationships for</param>
    /// <returns>Number of relationships created</returns>
    Task<int> InferRelationshipsForEntityAsync(string tenantId, string entityId);

    /// <summary>
    /// Infers ownership relationships based on entity properties
    /// </summary>
    Task<int> InferOwnershipRelationshipsAsync(string tenantId, string entityId);

    /// <summary>
    /// Infers spatial relationships (contains, adjacent_to, near) based on geospatial data
    /// </summary>
    Task<int> InferSpatialRelationshipsAsync(string tenantId, string entityId);

    /// <summary>
    /// Infers temporal relationships (follows, precedes) based on timestamps
    /// </summary>
    Task<int> InferTemporalRelationshipsAsync(string tenantId, string entityId);
}
