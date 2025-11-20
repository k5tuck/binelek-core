using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Binah.Ontology.DTOs;

namespace Binah.Ontology.Services;

/// <summary>
/// Service interface for pattern template operations
/// </summary>
public interface IPatternTemplateService
{
    /// <summary>
    /// Creates a new pattern template
    /// </summary>
    Task<PatternTemplateDto> CreateTemplateAsync(
        string tenantId,
        CreatePatternTemplateDto request,
        string? createdBy = null);

    /// <summary>
    /// Gets a template by ID
    /// </summary>
    Task<PatternTemplateDto?> GetTemplateByIdAsync(Guid id, string tenantId);

    /// <summary>
    /// Gets templates for a tenant
    /// </summary>
    Task<PaginatedTemplatesDto> GetTemplatesAsync(
        string tenantId,
        TemplateQueryDto query);

    /// <summary>
    /// Gets marketplace templates
    /// </summary>
    Task<PaginatedTemplatesDto> GetMarketplaceTemplatesAsync(TemplateQueryDto query);

    /// <summary>
    /// Gets official Binelek templates
    /// </summary>
    Task<List<PatternTemplateDto>> GetOfficialTemplatesAsync(
        string? category = null,
        string? type = null);

    /// <summary>
    /// Updates a template
    /// </summary>
    Task<PatternTemplateDto> UpdateTemplateAsync(
        Guid id,
        string tenantId,
        UpdatePatternTemplateDto request,
        string? updatedBy = null);

    /// <summary>
    /// Deletes a template
    /// </summary>
    Task<bool> DeleteTemplateAsync(Guid id, string tenantId, string? deletedBy = null);

    /// <summary>
    /// Uses/instantiates a template
    /// </summary>
    Task<TemplateInstantiationResultDto> UseTemplateAsync(
        Guid templateId,
        string tenantId,
        UseTemplateDto request,
        string? userId = null);

    /// <summary>
    /// Shares a template to marketplace
    /// </summary>
    Task<PatternTemplateDto> ShareTemplateAsync(
        Guid id,
        string tenantId,
        ShareTemplateDto request,
        string? userId = null);

    /// <summary>
    /// Rates a template
    /// </summary>
    Task<PatternTemplateDto> RateTemplateAsync(
        Guid templateId,
        string tenantId,
        RateTemplateDto request,
        string userId);

    /// <summary>
    /// Gets available categories
    /// </summary>
    Task<List<TemplateCategoryDto>> GetCategoriesAsync();

    /// <summary>
    /// Gets popular tags
    /// </summary>
    Task<List<string>> GetPopularTagsAsync(int limit = 20);
}
