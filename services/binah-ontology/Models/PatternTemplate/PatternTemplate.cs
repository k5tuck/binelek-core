using System;
using System.Collections.Generic;

namespace Binah.Ontology.Models.PatternTemplate;

/// <summary>
/// Pattern template entity for reusable data patterns, workflows, and configurations
/// </summary>
public class PatternTemplate
{
    /// <summary>
    /// Unique identifier for the template
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Tenant that owns this template
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the template
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this template does
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Category of the template (data-model, workflow, pipeline, query, dashboard)
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Type of content (ontology, action, pipeline, canvas)
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The template content stored as JSON
    /// </summary>
    public string Content { get; set; } = "{}";

    /// <summary>
    /// Whether this template is shared publicly in the marketplace
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// Whether this is an official Binelek template
    /// </summary>
    public bool IsOfficial { get; set; }

    /// <summary>
    /// Number of times this template has been used
    /// </summary>
    public int UsageCount { get; set; }

    /// <summary>
    /// Average rating (0-5)
    /// </summary>
    public double Rating { get; set; }

    /// <summary>
    /// Number of ratings received
    /// </summary>
    public int RatingCount { get; set; }

    /// <summary>
    /// Tags for categorization and search
    /// </summary>
    public string Tags { get; set; } = "[]";

    /// <summary>
    /// Thumbnail/preview image URL
    /// </summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// When the template was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Who created the template
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// When the template was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Who last updated the template
    /// </summary>
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Whether the template is soft deleted
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// When the template was deleted
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Who deleted the template
    /// </summary>
    public string? DeletedBy { get; set; }
}

/// <summary>
/// Template rating entity
/// </summary>
public class PatternTemplateRating
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Template being rated
    /// </summary>
    public Guid TemplateId { get; set; }

    /// <summary>
    /// Tenant of the rater
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// User who rated
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Rating value (1-5)
    /// </summary>
    public int Rating { get; set; }

    /// <summary>
    /// Optional review comment
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// When the rating was given
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Template categories
/// </summary>
public static class TemplateCategories
{
    public const string DataModel = "data-model";
    public const string Workflow = "workflow";
    public const string Pipeline = "pipeline";
    public const string Query = "query";
    public const string Dashboard = "dashboard";

    public static readonly string[] All = new[]
    {
        DataModel,
        Workflow,
        Pipeline,
        Query,
        Dashboard
    };
}

/// <summary>
/// Template types
/// </summary>
public static class TemplateTypes
{
    public const string Ontology = "ontology";
    public const string Action = "action";
    public const string Pipeline = "pipeline";
    public const string Canvas = "canvas";

    public static readonly string[] All = new[]
    {
        Ontology,
        Action,
        Pipeline,
        Canvas
    };
}
