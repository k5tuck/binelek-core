using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Binah.Ontology.Models.PatternTemplate;

namespace Binah.Ontology.Repositories;

/// <summary>
/// Repository interface for pattern template data access
/// </summary>
public interface IPatternTemplateRepository
{
    /// <summary>
    /// Creates a new template
    /// </summary>
    Task<PatternTemplate> CreateAsync(PatternTemplate template);

    /// <summary>
    /// Gets a template by ID
    /// </summary>
    Task<PatternTemplate?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets templates for a tenant with pagination
    /// </summary>
    Task<(List<PatternTemplate> Items, int TotalCount)> GetByTenantAsync(
        string tenantId,
        string? category = null,
        string? type = null,
        string? search = null,
        List<string>? tags = null,
        string? sortBy = null,
        string? sortDirection = null,
        int skip = 0,
        int take = 20);

    /// <summary>
    /// Gets public marketplace templates
    /// </summary>
    Task<(List<PatternTemplate> Items, int TotalCount)> GetMarketplaceAsync(
        string? category = null,
        string? type = null,
        string? search = null,
        List<string>? tags = null,
        string? sortBy = null,
        string? sortDirection = null,
        int skip = 0,
        int take = 20);

    /// <summary>
    /// Gets official Binelek templates
    /// </summary>
    Task<List<PatternTemplate>> GetOfficialAsync(
        string? category = null,
        string? type = null);

    /// <summary>
    /// Updates a template
    /// </summary>
    Task<PatternTemplate> UpdateAsync(PatternTemplate template);

    /// <summary>
    /// Soft deletes a template
    /// </summary>
    Task<bool> DeleteAsync(Guid id, string? deletedBy = null);

    /// <summary>
    /// Increments usage count
    /// </summary>
    Task IncrementUsageAsync(Guid id);

    /// <summary>
    /// Updates template rating
    /// </summary>
    Task UpdateRatingAsync(Guid id, double newRating, int ratingCount);

    /// <summary>
    /// Gets available categories with counts
    /// </summary>
    Task<Dictionary<string, int>> GetCategoriesWithCountAsync(string? tenantId = null);

    /// <summary>
    /// Gets popular tags
    /// </summary>
    Task<List<string>> GetPopularTagsAsync(int limit = 20);
}
