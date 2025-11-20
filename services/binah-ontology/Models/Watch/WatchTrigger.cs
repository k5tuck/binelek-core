using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Binah.Ontology.Models.Watch;

/// <summary>
/// Represents a trigger event for a watch
/// </summary>
[Table("WatchTriggers")]
public class WatchTrigger
{
    [Key]
    public string Id { get; set; } = string.Empty;

    [Required]
    public string WatchId { get; set; } = string.Empty;

    [Required]
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Entity that triggered the watch (if applicable)
    /// </summary>
    public string? EntityId { get; set; }

    /// <summary>
    /// Type of the entity that triggered the watch
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// What condition was met that triggered this
    /// </summary>
    public string? ConditionMet { get; set; }

    /// <summary>
    /// Previous value before change (as JSON)
    /// </summary>
    public string? PreviousValue { get; set; }

    /// <summary>
    /// Current value that triggered the watch (as JSON)
    /// </summary>
    public string? CurrentValue { get; set; }

    /// <summary>
    /// Notification sent as result
    /// </summary>
    public string? NotificationSent { get; set; }

    public WatchSeverity Severity { get; set; }

    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the notification was successfully delivered
    /// </summary>
    public bool NotificationDelivered { get; set; }

    /// <summary>
    /// Error message if notification failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Whether this trigger has been acknowledged
    /// </summary>
    public bool Acknowledged { get; set; }

    public DateTime? AcknowledgedAt { get; set; }

    [MaxLength(100)]
    public string? AcknowledgedBy { get; set; }

    /// <summary>
    /// Correlation ID for tracing
    /// </summary>
    [MaxLength(100)]
    public string? CorrelationId { get; set; }
}
