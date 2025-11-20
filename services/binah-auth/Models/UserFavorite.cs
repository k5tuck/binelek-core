using System;
using System.ComponentModel.DataAnnotations;

namespace Binah.Auth.Models;

/// <summary>
/// Represents a user's favorited/pinned sidebar item
/// </summary>
public class UserFavorite
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// The route path (e.g., "/canvas", "/graph", "/pipelines")
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Route { get; set; } = string.Empty;

    /// <summary>
    /// Display label for the favorite
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Icon name from lucide-react
    /// </summary>
    [MaxLength(50)]
    public string Icon { get; set; } = string.Empty;

    /// <summary>
    /// Order in the favorites list
    /// </summary>
    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class CreateFavoriteRequest
{
    [Required]
    public string Route { get; set; } = string.Empty;

    [Required]
    public string Label { get; set; } = string.Empty;

    public string Icon { get; set; } = string.Empty;
}

public class UpdateFavoriteOrderRequest
{
    [Required]
    public List<string> FavoriteIds { get; set; } = new();
}

public class FavoriteDto
{
    public string Id { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
