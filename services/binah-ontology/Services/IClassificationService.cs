using Binah.Ontology.Models.Base;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Binah.Ontology.Services;

/// <summary>
/// Service interface for entity classification operations
/// </summary>
public interface IClassificationService
{
    /// <summary>
    /// Classifies an entity based on its properties and relationships
    /// </summary>
    /// <param name="tenantId">Tenant ID for isolation</param>
    /// <param name="entityId">Entity ID to classify</param>
    /// <returns>Classification results</returns>
    Task<Dictionary<string, object>> ClassifyEntityAsync(string tenantId, string entityId);

    /// <summary>
    /// Classifies an entity into one or more categories
    /// </summary>
    /// <param name="entity">The entity to classify</param>
    /// <returns>List of category labels</returns>
    Task<List<string>> GetEntityCategoriesAsync(Entity entity);

    /// <summary>
    /// Determines entity risk level based on properties and relationships
    /// </summary>
    /// <param name="entity">The entity to assess</param>
    /// <returns>Risk level (low, medium, high, critical)</returns>
    Task<string> DetermineRiskLevelAsync(Entity entity);

    /// <summary>
    /// Scores entity quality based on completeness and accuracy
    /// </summary>
    /// <param name="entity">The entity to score</param>
    /// <returns>Quality score (0-100)</returns>
    Task<int> CalculateQualityScoreAsync(Entity entity);

    /// <summary>
    /// Tags entity with automatic tags based on content analysis
    /// </summary>
    /// <param name="entity">The entity to tag</param>
    /// <returns>List of auto-generated tags</returns>
    Task<List<string>> GenerateAutoTagsAsync(Entity entity);
}
