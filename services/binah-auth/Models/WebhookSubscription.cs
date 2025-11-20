namespace Binah.Auth.Models;

/// <summary>
/// Webhook subscription for event notifications
/// </summary>
public class WebhookSubscription
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Tenant identifier
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Friendly name for the webhook
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Target URL for webhook delivery
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// List of events to subscribe to (JSON array)
    /// </summary>
    public string Events { get; set; } = "[]";

    /// <summary>
    /// Secret for HMAC signature generation
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// Whether the webhook is active
    /// </summary>
    public bool Active { get; set; }

    /// <summary>
    /// Custom headers to include in webhook requests (JSON)
    /// </summary>
    public string Headers { get; set; } = "{}";

    /// <summary>
    /// Number of retry attempts
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// When the subscription was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the subscription was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Webhook delivery record
/// </summary>
public class WebhookDelivery
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the webhook subscription
    /// </summary>
    public Guid SubscriptionId { get; set; }

    /// <summary>
    /// Event type that triggered the webhook
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Payload sent to the webhook (JSON)
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Response status (success, failed, pending)
    /// </summary>
    public string ResponseStatus { get; set; } = string.Empty;

    /// <summary>
    /// HTTP response code
    /// </summary>
    public int ResponseCode { get; set; }

    /// <summary>
    /// Response body from the webhook endpoint
    /// </summary>
    public string? ResponseBody { get; set; }

    /// <summary>
    /// Attempt number (for retries)
    /// </summary>
    public int AttemptNumber { get; set; }

    /// <summary>
    /// When the delivery was attempted
    /// </summary>
    public DateTime DeliveredAt { get; set; }
}
