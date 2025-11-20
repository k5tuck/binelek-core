using Binah.Auth.Services;
using Binah.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Binah.Auth.Controllers;

/// <summary>
/// Controller for webhook management
/// </summary>
[ApiController]
[Route("api/webhooks")]
[Authorize]
public class WebhooksController : ControllerBase
{
    private readonly IWebhookService _webhookService;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(IWebhookService webhookService, ILogger<WebhooksController> logger)
    {
        _webhookService = webhookService ?? throw new ArgumentNullException(nameof(webhookService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create a new webhook subscription
    /// </summary>
    [HttpPost("subscriptions")]
    [ProducesResponseType(typeof(ApiResponse<WebhookSubscriptionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<WebhookSubscriptionResponse>>> CreateSubscription(
        [FromBody] WebhookSubscriptionRequest request)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(tenantId))
        {
            return BadRequest(ApiResponse<WebhookSubscriptionResponse>.WithError("Tenant ID not found"));
        }

        try
        {
            var subscription = await _webhookService.CreateSubscriptionAsync(tenantId, request);

            var response = MapToResponse(subscription);

            return Ok(ApiResponse<WebhookSubscriptionResponse>.Ok(response));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<WebhookSubscriptionResponse>.WithError(ex.Message));
        }
    }

    /// <summary>
    /// Get all webhook subscriptions for the tenant
    /// </summary>
    [HttpGet("subscriptions")]
    [ProducesResponseType(typeof(ApiResponse<List<WebhookSubscriptionResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<WebhookSubscriptionResponse>>>> GetSubscriptions()
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(tenantId))
        {
            return BadRequest(ApiResponse<List<WebhookSubscriptionResponse>>.WithError("Tenant ID not found"));
        }

        var subscriptions = await _webhookService.GetSubscriptionsAsync(tenantId);

        var response = subscriptions.Select(MapToResponse).ToList();

        return Ok(ApiResponse<List<WebhookSubscriptionResponse>>.Ok(response));
    }

    /// <summary>
    /// Get a specific webhook subscription
    /// </summary>
    [HttpGet("subscriptions/{id}")]
    [ProducesResponseType(typeof(ApiResponse<WebhookSubscriptionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<WebhookSubscriptionResponse>>> GetSubscription(Guid id)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(tenantId))
        {
            return BadRequest(ApiResponse<WebhookSubscriptionResponse>.WithError("Tenant ID not found"));
        }

        var subscription = await _webhookService.GetSubscriptionAsync(id, tenantId);

        if (subscription == null)
        {
            return NotFound(ApiResponse<WebhookSubscriptionResponse>.WithError("Webhook not found"));
        }

        var response = MapToResponse(subscription);

        return Ok(ApiResponse<WebhookSubscriptionResponse>.Ok(response));
    }

    /// <summary>
    /// Update a webhook subscription
    /// </summary>
    [HttpPut("subscriptions/{id}")]
    [ProducesResponseType(typeof(ApiResponse<WebhookSubscriptionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<WebhookSubscriptionResponse>>> UpdateSubscription(
        Guid id,
        [FromBody] WebhookSubscriptionRequest request)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(tenantId))
        {
            return BadRequest(ApiResponse<WebhookSubscriptionResponse>.WithError("Tenant ID not found"));
        }

        try
        {
            var subscription = await _webhookService.UpdateSubscriptionAsync(id, tenantId, request);

            var response = MapToResponse(subscription);

            return Ok(ApiResponse<WebhookSubscriptionResponse>.Ok(response));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiResponse<WebhookSubscriptionResponse>.WithError(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<WebhookSubscriptionResponse>.WithError(ex.Message));
        }
    }

    /// <summary>
    /// Delete a webhook subscription
    /// </summary>
    [HttpDelete("subscriptions/{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteSubscription(Guid id)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(tenantId))
        {
            return BadRequest(ApiResponse<bool>.WithError("Tenant ID not found"));
        }

        await _webhookService.DeleteSubscriptionAsync(id, tenantId);

        return Ok(ApiResponse<bool>.Ok(true));
    }

    /// <summary>
    /// Test a webhook subscription
    /// </summary>
    [HttpPost("subscriptions/{id}/test")]
    [ProducesResponseType(typeof(ApiResponse<WebhookTestResult>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<WebhookTestResult>>> TestWebhook(Guid id)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(tenantId))
        {
            return BadRequest(ApiResponse<WebhookTestResult>.WithError("Tenant ID not found"));
        }

        var success = await _webhookService.TestWebhookAsync(id, tenantId);

        var result = new WebhookTestResult
        {
            Success = success,
            Message = success ? "Webhook test successful" : "Webhook test failed"
        };

        return Ok(ApiResponse<WebhookTestResult>.Ok(result));
    }

    /// <summary>
    /// Get delivery history for a webhook
    /// </summary>
    [HttpGet("deliveries")]
    [ProducesResponseType(typeof(ApiResponse<List<WebhookDeliveryResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<WebhookDeliveryResponse>>>> GetDeliveries(
        [FromQuery] Guid subscriptionId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(tenantId))
        {
            return BadRequest(ApiResponse<List<WebhookDeliveryResponse>>.WithError("Tenant ID not found"));
        }

        var deliveries = await _webhookService.GetDeliveriesAsync(subscriptionId, tenantId, skip, take);

        var response = deliveries.Select(d => new WebhookDeliveryResponse
        {
            Id = d.Id,
            SubscriptionId = d.SubscriptionId,
            EventType = d.EventType,
            ResponseStatus = d.ResponseStatus,
            ResponseCode = d.ResponseCode,
            ResponseBody = d.ResponseBody,
            AttemptNumber = d.AttemptNumber,
            DeliveredAt = d.DeliveredAt
        }).ToList();

        return Ok(ApiResponse<List<WebhookDeliveryResponse>>.Ok(response));
    }

    /// <summary>
    /// Get available webhook events
    /// </summary>
    [HttpGet("events")]
    [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetEvents()
    {
        var events = await _webhookService.GetAvailableEventsAsync();

        return Ok(ApiResponse<List<string>>.Ok(events));
    }

    private WebhookSubscriptionResponse MapToResponse(Models.WebhookSubscription subscription)
    {
        return new WebhookSubscriptionResponse
        {
            Id = subscription.Id,
            Name = subscription.Name,
            Url = subscription.Url,
            Events = System.Text.Json.JsonSerializer.Deserialize<List<string>>(subscription.Events) ?? new(),
            Active = subscription.Active,
            RetryCount = subscription.RetryCount,
            CreatedAt = subscription.CreatedAt,
            UpdatedAt = subscription.UpdatedAt,
            Secret = subscription.Secret.Substring(0, 8) + "..." // Only show partial secret
        };
    }
}

/// <summary>
/// Webhook subscription response
/// </summary>
public class WebhookSubscriptionResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public List<string> Events { get; set; } = new();
    public bool Active { get; set; }
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Secret { get; set; } = string.Empty;
}

/// <summary>
/// Webhook delivery response
/// </summary>
public class WebhookDeliveryResponse
{
    public Guid Id { get; set; }
    public Guid SubscriptionId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string ResponseStatus { get; set; } = string.Empty;
    public int ResponseCode { get; set; }
    public string? ResponseBody { get; set; }
    public int AttemptNumber { get; set; }
    public DateTime DeliveredAt { get; set; }
}

/// <summary>
/// Webhook test result
/// </summary>
public class WebhookTestResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
