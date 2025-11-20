using Binah.Ontology.Models.Base;

namespace Binah.Ontology.Models.Migrations;

/// <summary>
/// Represents a deprecation policy for breaking ontology changes
/// Implements 12-month deprecation window with scheduled notifications
/// </summary>
public class DeprecationPolicy : Entity
{
    public Guid TenantId { get; set; }
    public Guid ChangeId { get; set; }
    public OntologyChange Change { get; set; } = null!;

    public DateTime ScheduledAt { get; set; } = DateTime.UtcNow;
    public DateTime DeprecationDate { get; set; } // 12 months from scheduled date
    public string Status { get; set; } = "scheduled"; // scheduled, active, completed, cancelled

    public string DeprecatedEntityName { get; set; } = string.Empty;
    public string? ReplacementEntityName { get; set; }
    public string DeprecationReason { get; set; } = string.Empty;

    // Notification tracking
    public List<DeprecationNotification> Notifications { get; set; } = new();
}

/// <summary>
/// Scheduled deprecation notification
/// </summary>
public class DeprecationNotification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PolicyId { get; set; }

    public DateTime ScheduledDate { get; set; }
    public string NotificationType { get; set; } = string.Empty; // "6_months", "3_months", "1_month", "7_days", "final"
    public string Message { get; set; } = string.Empty;
    public bool Sent { get; set; }
    public DateTime? SentAt { get; set; }
    public List<string> Recipients { get; set; } = new();
}
