using Binah.Auth.Models;

namespace Binah.Auth.Services;

/// <summary>
/// Interface for webhook management service
/// </summary>
public interface IWebhookService
{
    Task<WebhookSubscription> CreateSubscriptionAsync(string tenantId, WebhookSubscriptionRequest request);
    Task<List<WebhookSubscription>> GetSubscriptionsAsync(string tenantId);
    Task<WebhookSubscription?> GetSubscriptionAsync(Guid id, string tenantId);
    Task<WebhookSubscription> UpdateSubscriptionAsync(Guid id, string tenantId, WebhookSubscriptionRequest request);
    Task DeleteSubscriptionAsync(Guid id, string tenantId);
    Task<bool> TestWebhookAsync(Guid id, string tenantId);
    Task<List<WebhookDelivery>> GetDeliveriesAsync(Guid subscriptionId, string tenantId, int skip = 0, int take = 50);
    Task<List<string>> GetAvailableEventsAsync();
}

/// <summary>
/// Interface for webhook delivery service
/// </summary>
public interface IWebhookDeliveryService
{
    Task DeliverWebhookAsync(string eventType, object payload, string tenantId);
    Task<WebhookDelivery> SendWebhookAsync(WebhookSubscription subscription, string eventType, object payload);
    string GenerateSignature(string payload, string secret);
}

/// <summary>
/// Webhook subscription request
/// </summary>
public class WebhookSubscriptionRequest
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public List<string> Events { get; set; } = new();
    public bool Active { get; set; } = true;
    public Dictionary<string, string> Headers { get; set; } = new();
    public int RetryCount { get; set; } = 3;
}
