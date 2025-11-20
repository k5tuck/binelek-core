using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Binah.Ontology.Models.Watch;

/// <summary>
/// DTO for creating a new watch
/// </summary>
public class CreateWatchRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Entity types to monitor
    /// </summary>
    public List<string>? EntityTypes { get; set; }

    /// <summary>
    /// Condition expression that triggers the watch
    /// </summary>
    [Required]
    public Dictionary<string, object> Condition { get; set; } = new();

    /// <summary>
    /// Notification configuration
    /// </summary>
    [Required]
    public Dictionary<string, object> NotificationConfig { get; set; } = new();

    /// <summary>
    /// How often to check the condition (in minutes)
    /// 0 means real-time (event-driven)
    /// </summary>
    public int CheckIntervalMinutes { get; set; }

    /// <summary>
    /// Severity level for triggered notifications
    /// </summary>
    public WatchSeverity Severity { get; set; } = WatchSeverity.Medium;
}

/// <summary>
/// DTO for updating a watch
/// </summary>
public class UpdateWatchRequest
{
    [MaxLength(200)]
    public string? Name { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    public List<string>? EntityTypes { get; set; }

    public Dictionary<string, object>? Condition { get; set; }

    public Dictionary<string, object>? NotificationConfig { get; set; }

    public int? CheckIntervalMinutes { get; set; }

    public WatchSeverity? Severity { get; set; }
}

/// <summary>
/// DTO for watch response
/// </summary>
public class WatchResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string>? EntityTypes { get; set; }
    public Dictionary<string, object>? Condition { get; set; }
    public Dictionary<string, object>? NotificationConfig { get; set; }
    public int CheckIntervalMinutes { get; set; }
    public WatchStatus Status { get; set; }
    public WatchSeverity Severity { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? LastCheckedAt { get; set; }
    public DateTime? LastTriggeredAt { get; set; }
    public int TriggerCount { get; set; }
    public int WatchedEntityCount { get; set; }
}

/// <summary>
/// DTO for watch entity response
/// </summary>
public class WatchEntityResponse
{
    public string Id { get; set; } = string.Empty;
    public string WatchId { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; }
    public string? AddedBy { get; set; }
    public DateTime? LastTriggeredAt { get; set; }
    public int TriggerCount { get; set; }
}

/// <summary>
/// DTO for adding entity to watch
/// </summary>
public class AddWatchEntityRequest
{
    [Required]
    public string EntityId { get; set; } = string.Empty;

    [Required]
    public string EntityType { get; set; } = string.Empty;
}

/// <summary>
/// DTO for watch trigger response
/// </summary>
public class WatchTriggerResponse
{
    public string Id { get; set; } = string.Empty;
    public string WatchId { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? EntityType { get; set; }
    public string? ConditionMet { get; set; }
    public Dictionary<string, object>? PreviousValue { get; set; }
    public Dictionary<string, object>? CurrentValue { get; set; }
    public WatchSeverity Severity { get; set; }
    public DateTime TriggeredAt { get; set; }
    public bool NotificationDelivered { get; set; }
    public string? ErrorMessage { get; set; }
    public bool Acknowledged { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public string? AcknowledgedBy { get; set; }
}
