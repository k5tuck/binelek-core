using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Binah.Ontology.Models.Action;

/// <summary>
/// Represents a single execution of an action
/// </summary>
[Table("ActionRuns")]
public class ActionRun
{
    [Key]
    public string Id { get; set; } = string.Empty;

    [Required]
    public string ActionId { get; set; } = string.Empty;

    [Required]
    public string TenantId { get; set; } = string.Empty;

    public ActionRunStatus Status { get; set; } = ActionRunStatus.Pending;

    /// <summary>
    /// What triggered this run
    /// </summary>
    public ActionTriggerType TriggerType { get; set; }

    /// <summary>
    /// User who triggered manual runs
    /// </summary>
    [MaxLength(100)]
    public string? TriggeredBy { get; set; }

    /// <summary>
    /// Input data for the action as JSON
    /// </summary>
    public string? InputData { get; set; }

    /// <summary>
    /// Output/result data as JSON
    /// </summary>
    public string? OutputData { get; set; }

    /// <summary>
    /// Error message if failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Stack trace for debugging
    /// </summary>
    public string? StackTrace { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Duration in milliseconds
    /// </summary>
    public long? DurationMs { get; set; }

    /// <summary>
    /// Number of entities affected by this run
    /// </summary>
    public int EntitiesAffected { get; set; }

    /// <summary>
    /// Correlation ID for tracing
    /// </summary>
    [MaxLength(100)]
    public string? CorrelationId { get; set; }
}

/// <summary>
/// Status of an action run
/// </summary>
public enum ActionRunStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}
