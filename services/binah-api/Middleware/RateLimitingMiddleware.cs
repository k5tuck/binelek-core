using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Binah.API.Middleware;

/// <summary>
/// Simple rate limiting middleware
/// Uses in-memory storage (for production, consider using Redis)
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private static readonly ConcurrentDictionary<string, ClientRequestInfo> _clients = new();
    private readonly int _requestsPerMinute;
    private readonly int _cleanupIntervalSeconds;

    public RateLimitingMiddleware(
        RequestDelegate next,
        ILogger<RateLimitingMiddleware> logger,
        int requestsPerMinute = 100,
        int cleanupIntervalSeconds = 300)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _requestsPerMinute = requestsPerMinute;
        _cleanupIntervalSeconds = cleanupIntervalSeconds;

        // Start background cleanup task
        StartCleanupTask();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientId = GetClientIdentifier(context);
        var now = DateTime.UtcNow;

        var clientInfo = _clients.GetOrAdd(clientId, _ => new ClientRequestInfo());

        bool rateLimitExceeded;
        int remaining;

        lock (clientInfo)
        {
            // Remove requests older than 1 minute
            clientInfo.Requests.RemoveAll(t => (now - t).TotalMinutes > 1);

            // Check if limit exceeded
            rateLimitExceeded = clientInfo.Requests.Count >= _requestsPerMinute;

            if (!rateLimitExceeded)
            {
                // Add current request
                clientInfo.Requests.Add(now);
                remaining = _requestsPerMinute - clientInfo.Requests.Count;
            }
            else
            {
                remaining = 0;
            }
        }

        if (rateLimitExceeded)
        {
            _logger.LogWarning("Rate limit exceeded for client {ClientId}", clientId);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.Add("Retry-After", "60");
            context.Response.Headers.Add("X-RateLimit-Limit", _requestsPerMinute.ToString());
            context.Response.Headers.Add("X-RateLimit-Remaining", "0");
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Rate limit exceeded",
                message = $"Maximum {_requestsPerMinute} requests per minute allowed"
            });
            return;
        }

        // Add rate limit headers
        context.Response.Headers.Add("X-RateLimit-Limit", _requestsPerMinute.ToString());
        context.Response.Headers.Add("X-RateLimit-Remaining", remaining.ToString());

        await _next(context);
    }

    private string GetClientIdentifier(HttpContext context)
    {
        // Try to get user ID from claims
        var userId = context.User?.Claims?.FirstOrDefault(c => c.Type == "sub" || c.Type == "userId")?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            return $"user:{userId}";
        }

        // Fall back to IP address
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"ip:{ipAddress}";
    }

    private void StartCleanupTask()
    {
        Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(_cleanupIntervalSeconds));

                var now = DateTime.UtcNow;
                var keysToRemove = _clients
                    .Where(kvp =>
                    {
                        lock (kvp.Value)
                        {
                            kvp.Value.Requests.RemoveAll(t => (now - t).TotalMinutes > 5);
                            return kvp.Value.Requests.Count == 0;
                        }
                    })
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    _clients.TryRemove(key, out _);
                }

                if (keysToRemove.Count > 0)
                {
                    _logger.LogDebug("Cleaned up {Count} inactive clients from rate limiter", keysToRemove.Count);
                }
            }
        });
    }

    private class ClientRequestInfo
    {
        public List<DateTime> Requests { get; set; } = new();
    }
}

/// <summary>
/// Extension methods for adding rate limiting middleware
/// </summary>
public static class RateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimiting(
        this IApplicationBuilder builder,
        int requestsPerMinute = 100)
    {
        return builder.UseMiddleware<RateLimitingMiddleware>(requestsPerMinute);
    }
}
