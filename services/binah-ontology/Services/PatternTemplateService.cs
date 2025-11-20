using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Binah.Ontology.DTOs;
using Binah.Ontology.Models.PatternTemplate;
using Binah.Ontology.Repositories;
using Microsoft.Extensions.Logging;

namespace Binah.Ontology.Services;

/// <summary>
/// Service implementation for pattern template operations
/// </summary>
public class PatternTemplateService : IPatternTemplateService
{
    private readonly IPatternTemplateRepository _repository;
    private readonly ILogger<PatternTemplateService> _logger;

    public PatternTemplateService(
        IPatternTemplateRepository repository,
        ILogger<PatternTemplateService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<PatternTemplateDto> CreateTemplateAsync(
        string tenantId,
        CreatePatternTemplateDto request,
        string? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("Tenant ID is required", nameof(tenantId));

        if (request == null)
            throw new ArgumentNullException(nameof(request));

        _logger.LogInformation("Creating pattern template '{Name}' for tenant {TenantId}",
            request.Name, tenantId);

        var template = new PatternTemplate
        {
            TenantId = tenantId,
            Name = request.Name,
            Description = request.Description ?? string.Empty,
            Category = request.Category,
            Type = request.Type,
            Content = JsonSerializer.Serialize(request.Content),
            Tags = JsonSerializer.Serialize(request.Tags ?? new List<string>()),
            ThumbnailUrl = request.ThumbnailUrl,
            CreatedBy = createdBy
        };

        var created = await _repository.CreateAsync(template);
        return MapToDto(created);
    }

    /// <inheritdoc/>
    public async Task<PatternTemplateDto?> GetTemplateByIdAsync(Guid id, string tenantId)
    {
        var template = await _repository.GetByIdAsync(id);

        if (template == null)
            return null;

        // Check access: user can access their own templates, public templates, or official templates
        if (template.TenantId != tenantId && !template.IsPublic && !template.IsOfficial)
            return null;

        return MapToDto(template);
    }

    /// <inheritdoc/>
    public async Task<PaginatedTemplatesDto> GetTemplatesAsync(
        string tenantId,
        TemplateQueryDto query)
    {
        var (items, totalCount) = await _repository.GetByTenantAsync(
            tenantId,
            query.Category,
            query.Type,
            query.Search,
            query.Tags,
            query.SortBy,
            query.SortDirection,
            (query.Page - 1) * query.PageSize,
            query.PageSize);

        return new PaginatedTemplatesDto
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / query.PageSize)
        };
    }

    /// <inheritdoc/>
    public async Task<PaginatedTemplatesDto> GetMarketplaceTemplatesAsync(TemplateQueryDto query)
    {
        var (items, totalCount) = await _repository.GetMarketplaceAsync(
            query.Category,
            query.Type,
            query.Search,
            query.Tags,
            query.SortBy ?? "rating",
            query.SortDirection ?? "desc",
            (query.Page - 1) * query.PageSize,
            query.PageSize);

        return new PaginatedTemplatesDto
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / query.PageSize)
        };
    }

    /// <inheritdoc/>
    public async Task<List<PatternTemplateDto>> GetOfficialTemplatesAsync(
        string? category = null,
        string? type = null)
    {
        var templates = await _repository.GetOfficialAsync(category, type);
        return templates.Select(MapToDto).ToList();
    }

    /// <inheritdoc/>
    public async Task<PatternTemplateDto> UpdateTemplateAsync(
        Guid id,
        string tenantId,
        UpdatePatternTemplateDto request,
        string? updatedBy = null)
    {
        var template = await _repository.GetByIdAsync(id);

        if (template == null)
            throw new InvalidOperationException($"Template {id} not found");

        if (template.TenantId != tenantId)
            throw new UnauthorizedAccessException("Cannot update template from another tenant");

        _logger.LogInformation("Updating pattern template {TemplateId}", id);

        if (request.Name != null)
            template.Name = request.Name;

        if (request.Description != null)
            template.Description = request.Description;

        if (request.Category != null)
            template.Category = request.Category;

        if (request.Type != null)
            template.Type = request.Type;

        if (request.Content != null)
            template.Content = JsonSerializer.Serialize(request.Content);

        if (request.Tags != null)
            template.Tags = JsonSerializer.Serialize(request.Tags);

        if (request.ThumbnailUrl != null)
            template.ThumbnailUrl = request.ThumbnailUrl;

        template.UpdatedBy = updatedBy;

        var updated = await _repository.UpdateAsync(template);
        return MapToDto(updated);
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteTemplateAsync(Guid id, string tenantId, string? deletedBy = null)
    {
        var template = await _repository.GetByIdAsync(id);

        if (template == null)
            return false;

        if (template.TenantId != tenantId)
            throw new UnauthorizedAccessException("Cannot delete template from another tenant");

        _logger.LogInformation("Deleting pattern template {TemplateId}", id);

        return await _repository.DeleteAsync(id, deletedBy);
    }

    /// <inheritdoc/>
    public async Task<TemplateInstantiationResultDto> UseTemplateAsync(
        Guid templateId,
        string tenantId,
        UseTemplateDto request,
        string? userId = null)
    {
        var template = await _repository.GetByIdAsync(templateId);

        if (template == null)
        {
            return new TemplateInstantiationResultDto
            {
                Success = false,
                Message = "Template not found"
            };
        }

        // Check access
        if (template.TenantId != tenantId && !template.IsPublic && !template.IsOfficial)
        {
            return new TemplateInstantiationResultDto
            {
                Success = false,
                Message = "Access denied to template"
            };
        }

        _logger.LogInformation("Using template {TemplateId} for tenant {TenantId}", templateId, tenantId);

        // Increment usage count
        await _repository.IncrementUsageAsync(templateId);

        // TODO: Implement actual instantiation based on template type
        // For now, return success with placeholder
        // In a real implementation, this would:
        // - Parse template content
        // - Create the appropriate entity (action, pipeline, canvas, etc.)
        // - Apply customizations
        // - Return the created entity ID

        var createdId = Guid.NewGuid().ToString();

        return new TemplateInstantiationResultDto
        {
            Success = true,
            CreatedItemId = createdId,
            CreatedItemType = template.Type,
            Message = $"Successfully created {template.Type} from template"
        };
    }

    /// <inheritdoc/>
    public async Task<PatternTemplateDto> ShareTemplateAsync(
        Guid id,
        string tenantId,
        ShareTemplateDto request,
        string? userId = null)
    {
        var template = await _repository.GetByIdAsync(id);

        if (template == null)
            throw new InvalidOperationException($"Template {id} not found");

        if (template.TenantId != tenantId)
            throw new UnauthorizedAccessException("Cannot share template from another tenant");

        _logger.LogInformation("Sharing template {TemplateId} to marketplace: {IsPublic}",
            id, request.ShareToMarketplace);

        template.IsPublic = request.ShareToMarketplace;
        template.UpdatedBy = userId;

        var updated = await _repository.UpdateAsync(template);
        return MapToDto(updated);
    }

    /// <inheritdoc/>
    public async Task<PatternTemplateDto> RateTemplateAsync(
        Guid templateId,
        string tenantId,
        RateTemplateDto request,
        string userId)
    {
        var template = await _repository.GetByIdAsync(templateId);

        if (template == null)
            throw new InvalidOperationException($"Template {templateId} not found");

        // Can only rate public or official templates (not your own)
        if (!template.IsPublic && !template.IsOfficial)
            throw new InvalidOperationException("Can only rate public templates");

        if (template.TenantId == tenantId)
            throw new InvalidOperationException("Cannot rate your own template");

        _logger.LogInformation("Rating template {TemplateId} with {Rating} stars", templateId, request.Rating);

        // Calculate new average rating
        var newRatingCount = template.RatingCount + 1;
        var newRating = ((template.Rating * template.RatingCount) + request.Rating) / newRatingCount;

        await _repository.UpdateRatingAsync(templateId, newRating, newRatingCount);

        // Refresh and return
        template = await _repository.GetByIdAsync(templateId);
        return MapToDto(template!);
    }

    /// <inheritdoc/>
    public Task<List<TemplateCategoryDto>> GetCategoriesAsync()
    {
        var categories = new List<TemplateCategoryDto>
        {
            new() { Key = TemplateCategories.DataModel, Label = "Data Model", Description = "Entity and relationship templates", Icon = "Database" },
            new() { Key = TemplateCategories.Workflow, Label = "Workflow", Description = "Automated workflow templates", Icon = "Workflow" },
            new() { Key = TemplateCategories.Pipeline, Label = "Pipeline", Description = "Data pipeline templates", Icon = "GitBranch" },
            new() { Key = TemplateCategories.Query, Label = "Query", Description = "Saved query templates", Icon = "Search" },
            new() { Key = TemplateCategories.Dashboard, Label = "Dashboard", Description = "Dashboard layout templates", Icon = "LayoutDashboard" }
        };

        return Task.FromResult(categories);
    }

    /// <inheritdoc/>
    public async Task<List<string>> GetPopularTagsAsync(int limit = 20)
    {
        return await _repository.GetPopularTagsAsync(limit);
    }

    private PatternTemplateDto MapToDto(PatternTemplate template)
    {
        return new PatternTemplateDto
        {
            Id = template.Id,
            TenantId = template.TenantId,
            Name = template.Name,
            Description = template.Description,
            Category = template.Category,
            Type = template.Type,
            Content = JsonSerializer.Deserialize<Dictionary<string, object>>(template.Content) ?? new(),
            IsPublic = template.IsPublic,
            IsOfficial = template.IsOfficial,
            UsageCount = template.UsageCount,
            Rating = template.Rating,
            RatingCount = template.RatingCount,
            Tags = JsonSerializer.Deserialize<List<string>>(template.Tags) ?? new(),
            ThumbnailUrl = template.ThumbnailUrl,
            CreatedAt = template.CreatedAt,
            CreatedBy = template.CreatedBy,
            UpdatedAt = template.UpdatedAt
        };
    }
}
