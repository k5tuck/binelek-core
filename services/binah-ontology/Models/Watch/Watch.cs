using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Binah.Ontology.Models.Watch;

/// <summary>
/// Represents a watch for monitoring entities based on conditions
/// </summary>
[Table("Watches")]
public class Watch
{
    [Key]
    public string Id { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Entity types to monitor
    /// </summary>
    public string? EntityTypes { get; set; }

    /// <summary>
    /// Condition expression as JSON that triggers the watch
    /// </summary>
    [Required]
    public string Condition { get; set; } = "{}";

    /// <summary>
    /// Notification configuration as JSON
    /// </summary>
    [Required]
    public string NotificationConfig { get; set; } = "{}";

    /// <summary>
    /// How often to check the condition (in minutes)
    /// 0 means real-time (event-driven)
    /// </summary>
    public int CheckIntervalMinutes { get; set; }

    public WatchStatus Status { get; set; } = WatchStatus.Active;

    /// <summary>
    /// Severity level for triggered notifications
    /// </summary>
    public WatchSeverity Severity { get; set; } = WatchSeverity.Medium;

    [Required]
    public string TenantId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    [MaxLength(100)]
    public string? DeletedBy { get; set; }

    /// <summary>
    /// Last time this watch was checked
    /// </summary>
    public DateTime? LastCheckedAt { get; set; }

    /// <summary>
    /// Last time this watch was triggered
    /// </summary>
    public DateTime? LastTriggeredAt { get; set; }

    /// <summary>
    /// Total number of times this watch has been triggered
    /// </summary>
    public int TriggerCount { get; set; }

    /// <summary>
    /// Number of entities currently being watched
    /// </summary>
    public int WatchedEntityCount { get; set; }
}

/// <summary>
/// Status of a watch
/// </summary>
public enum WatchStatus
{
    Active = 0,
    Paused = 1,
    Disabled = 2
}

/// <summary>
/// Severity level for watch notifications
/// </summary>
public enum WatchSeverity
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}
