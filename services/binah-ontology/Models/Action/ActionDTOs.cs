using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Binah.Ontology.Models.Action;

/// <summary>
/// DTO for creating a new action
/// </summary>
public class CreateActionRequest
{
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
    public Dictionary<string, object>? Configuration { get; set; }

    /// <summary>
    /// Target entity types this action applies to
    /// </summary>
    public List<string>? TargetEntityTypes { get; set; }
}

/// <summary>
/// DTO for updating an action
/// </summary>
public class UpdateActionRequest
{
    [MaxLength(200)]
    public string? Name { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    public ActionTriggerType? TriggerType { get; set; }

    [MaxLength(100)]
    public string? Schedule { get; set; }

    [MaxLength(200)]
    public string? EventTopic { get; set; }

    public string? ConditionExpression { get; set; }

    public Dictionary<string, object>? Configuration { get; set; }

    public List<string>? TargetEntityTypes { get; set; }
}

/// <summary>
/// DTO for action response
/// </summary>
public class ActionResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ActionTriggerType TriggerType { get; set; }
    public string? Schedule { get; set; }
    public string? EventTopic { get; set; }
    public string? ConditionExpression { get; set; }
    public Dictionary<string, object>? Configuration { get; set; }
    public List<string>? TargetEntityTypes { get; set; }
    public ActionStatus Status { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? LastRunAt { get; set; }
    public DateTime? NextRunAt { get; set; }
    public int RunCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
}

/// <summary>
/// DTO for action run response
/// </summary>
public class ActionRunResponse
{
    public string Id { get; set; } = string.Empty;
    public string ActionId { get; set; } = string.Empty;
    public ActionRunStatus Status { get; set; }
    public ActionTriggerType TriggerType { get; set; }
    public string? TriggeredBy { get; set; }
    public Dictionary<string, object>? InputData { get; set; }
    public Dictionary<string, object>? OutputData { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public long? DurationMs { get; set; }
    public int EntitiesAffected { get; set; }
    public string? CorrelationId { get; set; }
}

/// <summary>
/// DTO for manual action run request
/// </summary>
public class RunActionRequest
{
    /// <summary>
    /// Optional input data for the action
    /// </summary>
    public Dictionary<string, object>? InputData { get; set; }
}
