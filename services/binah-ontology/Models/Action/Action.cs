using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Binah.Ontology.Models.Action;

/// <summary>
/// Represents a workflow action for automation
/// </summary>
[Table("Actions")]
public class Action
{
    [Key]
    public string Id { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    public ActionTriggerType TriggerType { get; set; }

    /// <summary>
    /// For schedule triggers - cron expression
    /// </summary>
    [MaxLength(100)]
    public string? Schedule { get; set; }

    /// <summary>
    /// For event triggers - Kafka topic name
    /// </summary>
    [MaxLength(200)]
    public string? EventTopic { get; set; }

    /// <summary>
    /// For condition triggers - JSON expression
    /// </summary>
    public string? ConditionExpression { get; set; }

    /// <summary>
    /// Action configuration as JSON
    /// </summary>
    [Required]
    public string Configuration { get; set; } = "{}";

    /// <summary>
    /// Target entity types this action applies to
    /// </summary>
    public string? TargetEntityTypes { get; set; }

    public ActionStatus Status { get; set; } = ActionStatus.Active;

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
    /// Last time this action was executed
    /// </summary>
    public DateTime? LastRunAt { get; set; }

    /// <summary>
    /// Next scheduled execution time
    /// </summary>
    public DateTime? NextRunAt { get; set; }

    /// <summary>
    /// Total number of times this action has been executed
    /// </summary>
    public int RunCount { get; set; }

    /// <summary>
    /// Number of successful executions
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of failed executions
    /// </summary>
    public int FailureCount { get; set; }
}

/// <summary>
/// Types of triggers that can initiate an action
/// </summary>
public enum ActionTriggerType
{
    /// <summary>
    /// Manual execution by user
    /// </summary>
    Manual = 0,

    /// <summary>
    /// Scheduled execution using cron expression
    /// </summary>
    Schedule = 1,

    /// <summary>
    /// Event-driven execution from Kafka topic
    /// </summary>
    Event = 2,

    /// <summary>
    /// Condition-based execution when criteria are met
    /// </summary>
    Condition = 3
}

/// <summary>
/// Status of an action
/// </summary>
public enum ActionStatus
{
    Active = 0,
    Paused = 1,
    Disabled = 2
}
