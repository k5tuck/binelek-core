
using Binah.Ontology.Models.Base;
namespace Binah.Ontology.Models.Domain;

/// <summary>
    /// Represents a monitoring alert or notification
    /// Label: Alert
    /// </summary>
    public class Alert : Entity
    {
        public Alert()
        {
            Type = "Alert";
        }

        /// <summary>Canonical alert identifier (UUID)</summary>
        public string Uid
        {
            get => Id;
            set => Id = value;
        }

        /// <summary>Alert type: price_change, ownership_transfer, permit_filed, lien_filed, assessment_change, market_shift, risk_detected</summary>
        public string AlertType
        {
            get => GetPropertyValue<string>("alert_type") ?? "general";
            set => SetPropertyValue("alert_type", value);
        }

        /// <summary>Alert severity: low, medium, high, critical</summary>
        public string Severity
        {
            get => GetPropertyValue<string>("severity") ?? "medium";
            set => SetPropertyValue("severity", value);
        }

        /// <summary>Alert title/summary</summary>
        public string Title
        {
            get => GetPropertyValue<string>("title") ?? string.Empty;
            set => SetPropertyValue("title", value);
        }

        /// <summary>Detailed alert message</summary>
        public string? Message
        {
            get => GetPropertyValue<string>("message");
            set => SetPropertyValue("message", value);
        }

        /// <summary>Entity ID that triggered the alert</summary>
        public string? TriggeredByEntityId
        {
            get => GetPropertyValue<string>("triggered_by_entity_id");
            set => SetPropertyValue("triggered_by_entity_id", value);
        }

        /// <summary>Entity type that triggered the alert</summary>
        public string? TriggeredByEntityType
        {
            get => GetPropertyValue<string>("triggered_by_entity_type");
            set => SetPropertyValue("triggered_by_entity_type", value);
        }

        /// <summary>When the alert was triggered</summary>
        public DateTime TriggeredAt
        {
            get => GetPropertyValue<DateTime>("triggered_at", DateTime.UtcNow);
            set => SetPropertyValue("triggered_at", value);
        }

        /// <summary>Alert status: new, acknowledged, resolved, dismissed</summary>
        public string Status
        {
            get => GetPropertyValue<string>("status") ?? "new";
            set => SetPropertyValue("status", value);
        }

        /// <summary>User ID who acknowledged the alert</summary>
        public string? AcknowledgedBy
        {
            get => GetPropertyValue<string>("acknowledged_by");
            set => SetPropertyValue("acknowledged_by", value);
        }

        /// <summary>When the alert was acknowledged</summary>
        public DateTime? AcknowledgedAt
        {
            get => GetPropertyValue<DateTime?>("acknowledged_at");
            set => SetPropertyValue("acknowledged_at", value);
        }

        /// <summary>User ID who resolved the alert</summary>
        public string? ResolvedBy
        {
            get => GetPropertyValue<string>("resolved_by");
            set => SetPropertyValue("resolved_by", value);
        }

        /// <summary>When the alert was resolved</summary>
        public DateTime? ResolvedAt
        {
            get => GetPropertyValue<DateTime?>("resolved_at");
            set => SetPropertyValue("resolved_at", value);
        }

        /// <summary>Additional context data for the alert</summary>
        public Dictionary<string, object>? Context
        {
            get => GetPropertyValue<Dictionary<string, object>>("context");
            set => SetPropertyValue("context", value);
        }

        /// <summary>Notification channels used: email, sms, in_app, webhook</summary>
        public List<string>? NotificationChannels
        {
            get => GetPropertyValue<List<string>>("notification_channels");
            set => SetPropertyValue("notification_channels", value);
        }

        /// <summary>Whether notification was successfully sent</summary>
        public bool NotificationSent
        {
            get => GetPropertyValue<bool>("notification_sent", false);
            set => SetPropertyValue("notification_sent", value);
        }
    }