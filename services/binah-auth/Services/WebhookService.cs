// NOTE: This webhook implementation is currently part of binah-auth for expedient delivery.
// FUTURE: Extract to a separate binah-webhooks microservice with its own database and event bus integration.
// The service should be horizontally scalable and handle webhook delivery asynchronously via a queue.

using Binah.Auth.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Binah.Auth.Services;

/// <summary>
/// Webhook management service
/// TODO: Extract to separate binah-webhooks microservice
/// </summary>
public class WebhookService : IWebhookService
{
    private readonly AuthDbContext _context;
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(AuthDbContext context, ILogger<WebhookService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<WebhookSubscription> CreateSubscriptionAsync(string tenantId, WebhookSubscriptionRequest request)
    {
        ValidateWebhookUrl(request.Url);

        var subscription = new WebhookSubscription
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = request.Name,
            Url = request.Url,
            Events = JsonSerializer.Serialize(request.Events),
            Secret = GenerateSecret(),
            Active = request.Active,
            Headers = JsonSerializer.Serialize(request.Headers),
            RetryCount = Math.Min(request.RetryCount, 5), // Max 5 retries
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.WebhookSubscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Webhook subscription created for tenant {TenantId}: {Name}", tenantId, request.Name);

        return subscription;
    }

    public async Task<List<WebhookSubscription>> GetSubscriptionsAsync(string tenantId)
    {
        return await _context.WebhookSubscriptions
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<WebhookSubscription?> GetSubscriptionAsync(Guid id, string tenantId)
    {
        return await _context.WebhookSubscriptions
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId);
    }

    public async Task<WebhookSubscription> UpdateSubscriptionAsync(Guid id, string tenantId, WebhookSubscriptionRequest request)
    {
        var subscription = await GetSubscriptionAsync(id, tenantId);

        if (subscription == null)
        {
            throw new InvalidOperationException($"Webhook subscription {id} not found");
        }

        ValidateWebhookUrl(request.Url);

        subscription.Name = request.Name;
        subscription.Url = request.Url;
        subscription.Events = JsonSerializer.Serialize(request.Events);
        subscription.Active = request.Active;
        subscription.Headers = JsonSerializer.Serialize(request.Headers);
        subscription.RetryCount = Math.Min(request.RetryCount, 5);
        subscription.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Webhook subscription updated: {Id}", id);

        return subscription;
    }

    public async Task DeleteSubscriptionAsync(Guid id, string tenantId)
    {
        var subscription = await GetSubscriptionAsync(id, tenantId);

        if (subscription != null)
        {
            _context.WebhookSubscriptions.Remove(subscription);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Webhook subscription deleted: {Id}", id);
        }
    }

    public async Task<bool> TestWebhookAsync(Guid id, string tenantId)
    {
        var subscription = await GetSubscriptionAsync(id, tenantId);

        if (subscription == null)
        {
            return false;
        }

        var testPayload = new
        {
            @event = "webhook.test",
            timestamp = DateTime.UtcNow,
            data = new { message = "This is a test webhook" }
        };

        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var payloadJson = JsonSerializer.Serialize(testPayload);
            var signature = GenerateSignatureFromService(payloadJson, subscription.Secret);

            var request = new HttpRequestMessage(HttpMethod.Post, subscription.Url)
            {
                Content = new StringContent(payloadJson, System.Text.Encoding.UTF8, "application/json")
            };

            request.Headers.Add("X-Webhook-Signature", signature);
            request.Headers.Add("X-Webhook-Event", "webhook.test");

            var response = await httpClient.SendAsync(request);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test webhook {Id}", id);
            return false;
        }
    }

    public async Task<List<WebhookDelivery>> GetDeliveriesAsync(Guid subscriptionId, string tenantId, int skip = 0, int take = 50)
    {
        // Verify subscription belongs to tenant
        var subscription = await GetSubscriptionAsync(subscriptionId, tenantId);

        if (subscription == null)
        {
            return new List<WebhookDelivery>();
        }

        return await _context.WebhookDeliveries
            .Where(d => d.SubscriptionId == subscriptionId)
            .OrderByDescending(d => d.DeliveredAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<List<string>> GetAvailableEventsAsync()
    {
        // Return list of available webhook events
        return new List<string>
        {
            "user.created",
            "user.updated",
            "user.deleted",
            "user.login",
            "user.logout",
            "property.created",
            "property.updated",
            "property.deleted",
            "entity.created",
            "entity.updated",
            "entity.deleted",
            "ontology.published",
            "pipeline.executed",
            "subscription.created",
            "subscription.updated",
            "subscription.cancelled"
        };
    }

    private void ValidateWebhookUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("Invalid webhook URL");
        }

        if (uri.Scheme != "https" && uri.Scheme != "http")
        {
            throw new ArgumentException("Webhook URL must use HTTP or HTTPS");
        }

        // Prevent webhooks to localhost or private IPs in production
        if (uri.Host == "localhost" || uri.Host == "127.0.0.1" || uri.Host.StartsWith("192.168.") || uri.Host.StartsWith("10."))
        {
            _logger.LogWarning("Webhook URL points to private network: {Url}", url);
        }
    }

    private string GenerateSecret()
    {
        return Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
    }

    private string GenerateSignatureFromService(string payload, string secret)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLower();
    }
}
