// NOTE: This webhook delivery implementation should be moved to a separate binah-webhooks service
// FUTURE: Use a message queue (RabbitMQ/Kafka) for reliable async delivery with retry logic

using Binah.Auth.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

namespace Binah.Auth.Services;

/// <summary>
/// Webhook delivery service with retry logic
/// TODO: Move to separate service with message queue integration
/// </summary>
public class WebhookDeliveryService : IWebhookDeliveryService
{
    private readonly AuthDbContext _context;
    private readonly ILogger<WebhookDeliveryService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public WebhookDeliveryService(
        AuthDbContext context,
        ILogger<WebhookDeliveryService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    public async Task DeliverWebhookAsync(string eventType, object payload, string tenantId)
    {
        // Find all active subscriptions for this tenant and event type
        var subscriptions = await _context.WebhookSubscriptions
            .Where(s => s.TenantId == tenantId && s.Active)
            .ToListAsync();

        foreach (var subscription in subscriptions)
        {
            // Check if subscription is listening to this event
            var events = JsonSerializer.Deserialize<List<string>>(subscription.Events) ?? new List<string>();

            if (events.Contains(eventType) || events.Contains("*"))
            {
                // Fire and forget - in production, this should be queued
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await SendWebhookWithRetryAsync(subscription, eventType, payload);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to deliver webhook for subscription {SubscriptionId}", subscription.Id);
                    }
                });
            }
        }
    }

    public async Task<WebhookDelivery> SendWebhookAsync(WebhookSubscription subscription, string eventType, object payload)
    {
        var delivery = new WebhookDelivery
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscription.Id,
            EventType = eventType,
            Payload = JsonSerializer.Serialize(payload),
            AttemptNumber = 1,
            DeliveredAt = DateTime.UtcNow
        };

        try
        {
            var httpClient = _httpClientFactory.CreateClient("webhook");
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var webhookPayload = new
            {
                @event = eventType,
                timestamp = DateTime.UtcNow,
                data = payload
            };

            var payloadJson = JsonSerializer.Serialize(webhookPayload);
            var signature = GenerateSignature(payloadJson, subscription.Secret);

            var request = new HttpRequestMessage(HttpMethod.Post, subscription.Url)
            {
                Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
            };

            // Add standard webhook headers
            request.Headers.Add("X-Webhook-Signature", signature);
            request.Headers.Add("X-Webhook-Event", eventType);
            request.Headers.Add("X-Webhook-Delivery-Id", delivery.Id.ToString());

            // Add custom headers from subscription
            var customHeaders = JsonSerializer.Deserialize<Dictionary<string, string>>(subscription.Headers) ?? new();
            foreach (var header in customHeaders)
            {
                request.Headers.Add(header.Key, header.Value);
            }

            var response = await httpClient.SendAsync(request);

            delivery.ResponseCode = (int)response.StatusCode;
            delivery.ResponseStatus = response.IsSuccessStatusCode ? "success" : "failed";
            delivery.ResponseBody = await response.Content.ReadAsStringAsync();

            if (delivery.ResponseBody.Length > 1000)
            {
                delivery.ResponseBody = delivery.ResponseBody.Substring(0, 1000) + "...";
            }

            _logger.LogInformation(
                "Webhook delivered to {Url} for event {EventType}: {StatusCode}",
                subscription.Url, eventType, delivery.ResponseCode);
        }
        catch (Exception ex)
        {
            delivery.ResponseStatus = "failed";
            delivery.ResponseCode = 0;
            delivery.ResponseBody = ex.Message;

            _logger.LogError(ex, "Failed to deliver webhook to {Url} for event {EventType}", subscription.Url, eventType);
        }

        _context.WebhookDeliveries.Add(delivery);
        await _context.SaveChangesAsync();

        return delivery;
    }

    private async Task SendWebhookWithRetryAsync(WebhookSubscription subscription, string eventType, object payload)
    {
        var maxAttempts = subscription.RetryCount + 1; // Initial attempt + retries
        var delay = TimeSpan.FromSeconds(1);

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var delivery = await SendWebhookAsync(subscription, eventType, payload);
            delivery.AttemptNumber = attempt;

            if (delivery.ResponseStatus == "success")
            {
                return; // Success, no need to retry
            }

            if (attempt < maxAttempts)
            {
                // Exponential backoff: 1s, 2s, 4s, 8s, 16s
                await Task.Delay(delay);
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 30));

                _logger.LogInformation(
                    "Retrying webhook delivery (attempt {Attempt}/{MaxAttempts}) for subscription {SubscriptionId}",
                    attempt + 1, maxAttempts, subscription.Id);
            }
            else
            {
                _logger.LogWarning(
                    "Webhook delivery failed after {MaxAttempts} attempts for subscription {SubscriptionId}",
                    maxAttempts, subscription.Id);
            }
        }
    }

    public string GenerateSignature(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLower();
    }
}
