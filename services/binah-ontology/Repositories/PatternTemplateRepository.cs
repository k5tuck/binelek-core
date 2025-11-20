using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Binah.Ontology.Data;
using Binah.Ontology.Models.PatternTemplate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Binah.Ontology.Repositories;

/// <summary>
/// PostgreSQL repository implementation for pattern templates
/// </summary>
public class PatternTemplateRepository : IPatternTemplateRepository
{
    private readonly OntologyDbContext _context;
    private readonly ILogger<PatternTemplateRepository> _logger;

    public PatternTemplateRepository(
        OntologyDbContext context,
        ILogger<PatternTemplateRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<PatternTemplate> CreateAsync(PatternTemplate template)
    {
        if (template == null)
            throw new ArgumentNullException(nameof(template));

        template.Id = Guid.NewGuid();
        template.CreatedAt = DateTime.UtcNow;
        template.UpdatedAt = DateTime.UtcNow;

        _context.PatternTemplates.Add(template);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created pattern template {TemplateId} for tenant {TenantId}",
            template.Id, template.TenantId);

        return template;
    }

    /// <inheritdoc/>
    public async Task<PatternTemplate?> GetByIdAsync(Guid id)
    {
        return await _context.PatternTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
    }

    /// <inheritdoc/>
    public async Task<(List<PatternTemplate> Items, int TotalCount)> GetByTenantAsync(
        string tenantId,
        string? category = null,
        string? type = null,
        string? search = null,
        List<string>? tags = null,
        string? sortBy = null,
        string? sortDirection = null,
        int skip = 0,
        int take = 20)
    {
        var query = _context.PatternTemplates
            .AsNoTracking()
            .Where(t => t.TenantId == tenantId && !t.IsDeleted);

        query = ApplyFilters(query, category, type, search, tags);
        query = ApplySorting(query, sortBy, sortDirection);

        var totalCount = await query.CountAsync();
        var items = await query.Skip(skip).Take(take).ToListAsync();

        return (items, totalCount);
    }

    /// <inheritdoc/>
    public async Task<(List<PatternTemplate> Items, int TotalCount)> GetMarketplaceAsync(
        string? category = null,
        string? type = null,
        string? search = null,
        List<string>? tags = null,
        string? sortBy = null,
        string? sortDirection = null,
        int skip = 0,
        int take = 20)
    {
        var query = _context.PatternTemplates
            .AsNoTracking()
            .Where(t => t.IsPublic && !t.IsDeleted);

        query = ApplyFilters(query, category, type, search, tags);
        query = ApplySorting(query, sortBy, sortDirection);

        var totalCount = await query.CountAsync();
        var items = await query.Skip(skip).Take(take).ToListAsync();

        return (items, totalCount);
    }

    /// <inheritdoc/>
    public async Task<List<PatternTemplate>> GetOfficialAsync(
        string? category = null,
        string? type = null)
    {
        var query = _context.PatternTemplates
            .AsNoTracking()
            .Where(t => t.IsOfficial && !t.IsDeleted);

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(t => t.Category == category);

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(t => t.Type == type);

        return await query.OrderBy(t => t.Name).ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<PatternTemplate> UpdateAsync(PatternTemplate template)
    {
        if (template == null)
            throw new ArgumentNullException(nameof(template));

        template.UpdatedAt = DateTime.UtcNow;

        _context.PatternTemplates.Update(template);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated pattern template {TemplateId}", template.Id);

        return template;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(Guid id, string? deletedBy = null)
    {
        var template = await _context.PatternTemplates.FirstOrDefaultAsync(t => t.Id == id);

        if (template == null)
            return false;

        template.IsDeleted = true;
        template.DeletedAt = DateTime.UtcNow;
        template.DeletedBy = deletedBy;
        template.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Soft deleted pattern template {TemplateId}", id);

        return true;
    }

    /// <inheritdoc/>
    public async Task IncrementUsageAsync(Guid id)
    {
        var template = await _context.PatternTemplates.FirstOrDefaultAsync(t => t.Id == id);

        if (template != null)
        {
            template.UsageCount++;
            template.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogDebug("Incremented usage count for template {TemplateId} to {Count}",
                id, template.UsageCount);
        }
    }

    /// <inheritdoc/>
    public async Task UpdateRatingAsync(Guid id, double newRating, int ratingCount)
    {
        var template = await _context.PatternTemplates.FirstOrDefaultAsync(t => t.Id == id);

        if (template != null)
        {
            template.Rating = newRating;
            template.RatingCount = ratingCount;
            template.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogDebug("Updated rating for template {TemplateId} to {Rating} ({Count} ratings)",
                id, newRating, ratingCount);
        }
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, int>> GetCategoriesWithCountAsync(string? tenantId = null)
    {
        var query = _context.PatternTemplates
            .AsNoTracking()
            .Where(t => !t.IsDeleted);

        if (!string.IsNullOrWhiteSpace(tenantId))
            query = query.Where(t => t.TenantId == tenantId || t.IsPublic);

        var categories = await query
            .GroupBy(t => t.Category)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .ToListAsync();

        return categories.ToDictionary(c => c.Category, c => c.Count);
    }

    /// <inheritdoc/>
    public async Task<List<string>> GetPopularTagsAsync(int limit = 20)
    {
        // Get all tags from public templates
        var templates = await _context.PatternTemplates
            .AsNoTracking()
            .Where(t => t.IsPublic && !t.IsDeleted)
            .Select(t => t.Tags)
            .ToListAsync();

        // Parse and count tags
        var tagCounts = new Dictionary<string, int>();
        foreach (var tagsJson in templates)
        {
            try
            {
                var tags = JsonSerializer.Deserialize<List<string>>(tagsJson) ?? new List<string>();
                foreach (var tag in tags)
                {
                    if (!tagCounts.ContainsKey(tag))
                        tagCounts[tag] = 0;
                    tagCounts[tag]++;
                }
            }
            catch
            {
                // Ignore invalid JSON
            }
        }

        return tagCounts
            .OrderByDescending(kv => kv.Value)
            .Take(limit)
            .Select(kv => kv.Key)
            .ToList();
    }

    private IQueryable<PatternTemplate> ApplyFilters(
        IQueryable<PatternTemplate> query,
        string? category,
        string? type,
        string? search,
        List<string>? tags)
    {
        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(t => t.Category == category);

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(t => t.Type == type);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(t =>
                t.Name.ToLower().Contains(searchLower) ||
                t.Description.ToLower().Contains(searchLower));
        }

        if (tags != null && tags.Count > 0)
        {
            // Filter by tags (JSON contains check)
            foreach (var tag in tags)
            {
                query = query.Where(t => t.Tags.Contains(tag));
            }
        }

        return query;
    }

    private IQueryable<PatternTemplate> ApplySorting(
        IQueryable<PatternTemplate> query,
        string? sortBy,
        string? sortDirection)
    {
        var isDescending = sortDirection?.ToLower() == "desc";

        return sortBy?.ToLower() switch
        {
            "name" => isDescending
                ? query.OrderByDescending(t => t.Name)
                : query.OrderBy(t => t.Name),
            "usage" => isDescending
                ? query.OrderByDescending(t => t.UsageCount)
                : query.OrderBy(t => t.UsageCount),
            "rating" => isDescending
                ? query.OrderByDescending(t => t.Rating)
                : query.OrderBy(t => t.Rating),
            "createdat" => isDescending
                ? query.OrderByDescending(t => t.CreatedAt)
                : query.OrderBy(t => t.CreatedAt),
            _ => query.OrderByDescending(t => t.UpdatedAt)
        };
    }
}
