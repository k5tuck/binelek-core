using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Binah.Ontology.Models.Watch;

/// <summary>
/// Represents an entity being tracked by a watch
/// </summary>
[Table("WatchEntities")]
public class WatchEntity
{
    [Key]
    public string Id { get; set; } = string.Empty;

    [Required]
    public string WatchId { get; set; } = string.Empty;

    [Required]
    public string EntityId { get; set; } = string.Empty;

    [Required]
    public string EntityType { get; set; } = string.Empty;

    [Required]
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// When this entity was added to the watch
    /// </summary>
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? AddedBy { get; set; }

    /// <summary>
    /// Last time this entity triggered the watch
    /// </summary>
    public DateTime? LastTriggeredAt { get; set; }

    /// <summary>
    /// Number of times this entity has triggered the watch
    /// </summary>
    public int TriggerCount { get; set; }

    /// <summary>
    /// Current state snapshot as JSON for comparison
    /// </summary>
    public string? StateSnapshot { get; set; }
}
