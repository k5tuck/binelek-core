using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Binah.Ontology.DTOs;

/// <summary>
/// DTO for creating a new pattern template
/// </summary>
public class CreatePatternTemplateDto
{
    /// <summary>
    /// Display name of the template
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this template does
    /// </summary>
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Category of the template (data-model, workflow, pipeline, query, dashboard)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Type of content (ontology, action, pipeline, canvas)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The template content
    /// </summary>
    [Required]
    public Dictionary<string, object> Content { get; set; } = new();

    /// <summary>
    /// Tags for categorization and search
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Thumbnail/preview image URL
    /// </summary>
    [MaxLength(500)]
    public string? ThumbnailUrl { get; set; }
}

/// <summary>
/// DTO for updating a pattern template
/// </summary>
public class UpdatePatternTemplateDto
{
    /// <summary>
    /// Display name of the template
    /// </summary>
    [MaxLength(200)]
    public string? Name { get; set; }

    /// <summary>
    /// Description of what this template does
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Category of the template
    /// </summary>
    [MaxLength(50)]
    public string? Category { get; set; }

    /// <summary>
    /// Type of content
    /// </summary>
    [MaxLength(50)]
    public string? Type { get; set; }

    /// <summary>
    /// The template content
    /// </summary>
    public Dictionary<string, object>? Content { get; set; }

    /// <summary>
    /// Tags for categorization and search
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Thumbnail/preview image URL
    /// </summary>
    [MaxLength(500)]
    public string? ThumbnailUrl { get; set; }
}

/// <summary>
/// DTO for pattern template response
/// </summary>
public class PatternTemplateDto
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Tenant that owns this template
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Display name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Category
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Type
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Template content
    /// </summary>
    public Dictionary<string, object> Content { get; set; } = new();

    /// <summary>
    /// Is publicly shared
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// Is official Binelek template
    /// </summary>
    public bool IsOfficial { get; set; }

    /// <summary>
    /// Usage count
    /// </summary>
    public int UsageCount { get; set; }

    /// <summary>
    /// Average rating
    /// </summary>
    public double Rating { get; set; }

    /// <summary>
    /// Number of ratings
    /// </summary>
    public int RatingCount { get; set; }

    /// <summary>
    /// Tags
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Thumbnail URL
    /// </summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Creator
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Updated timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for sharing a template to marketplace
/// </summary>
public class ShareTemplateDto
{
    /// <summary>
    /// Whether to share publicly
    /// </summary>
    public bool ShareToMarketplace { get; set; } = true;
}

/// <summary>
/// DTO for using/instantiating a template
/// </summary>
public class UseTemplateDto
{
    /// <summary>
    /// New name for the instantiated item
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional customizations to apply
    /// </summary>
    public Dictionary<string, object>? Customizations { get; set; }
}

/// <summary>
/// DTO for template instantiation result
/// </summary>
public class TemplateInstantiationResultDto
{
    /// <summary>
    /// Whether instantiation succeeded
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// ID of the created item
    /// </summary>
    public string? CreatedItemId { get; set; }

    /// <summary>
    /// Type of the created item
    /// </summary>
    public string? CreatedItemType { get; set; }

    /// <summary>
    /// Message
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// DTO for rating a template
/// </summary>
public class RateTemplateDto
{
    /// <summary>
    /// Rating value (1-5)
    /// </summary>
    [Required]
    [Range(1, 5)]
    public int Rating { get; set; }

    /// <summary>
    /// Optional review comment
    /// </summary>
    [MaxLength(500)]
    public string? Comment { get; set; }
}

/// <summary>
/// DTO for template list query parameters
/// </summary>
public class TemplateQueryDto
{
    /// <summary>
    /// Filter by category
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Filter by type
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Search term
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Filter by tags
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Sort by field (name, usage, rating, createdAt)
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Sort direction (asc, desc)
    /// </summary>
    public string? SortDirection { get; set; }

    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// DTO for paginated template list response
/// </summary>
public class PaginatedTemplatesDto
{
    /// <summary>
    /// Templates in this page
    /// </summary>
    public List<PatternTemplateDto> Items { get; set; } = new();

    /// <summary>
    /// Total count
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total pages
    /// </summary>
    public int TotalPages { get; set; }
}

/// <summary>
/// DTO for template category
/// </summary>
public class TemplateCategoryDto
{
    /// <summary>
    /// Category key
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Display label
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Icon name
    /// </summary>
    public string Icon { get; set; } = string.Empty;
}
