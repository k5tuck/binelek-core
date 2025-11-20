using Binah.API.Exceptions;
using Binah.API.Models;
using Binah.API.Services;
using Binah.Infrastructure.MultiTenancy;
using Microsoft.Extensions.Caching.Memory;

namespace Binah.API.Middleware;

/// <summary>
/// Middleware to extract and validate licensee context from requests
/// Ensures all requests are associated with a valid licensee
/// </summary>
public class LicenseeContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LicenseeContextMiddleware> _logger;
    private readonly IMemoryCache _cache;

    public LicenseeContextMiddleware(
        RequestDelegate next,
        ILogger<LicenseeContextMiddleware> logger,
        IMemoryCache cache)
    {
        _next = next;
        _logger = logger;
        _cache = cache;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ILicenseeService licenseeService)
    {
        // Skip licensee validation for health check and public endpoints
        if (IsPublicEndpoint(context.Request.Path))
        {
            await _next(context);
            return;
        }

        try
        {
            // Extract licensee ID and license key from request
            var licenseeId = ExtractLicenseeId(context);
            var licenseKey = ExtractLicenseKey(context);

            if (licenseeId == null && licenseKey == null)
            {
                _logger.LogWarning("No licensee ID or license key provided in request");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Unauthorized",
                    message = "Licensee ID or license key is required. Provide either X-Licensee-Id header or X-License-Key header.",
                    hint = "Platform licensees must authenticate with X-License-Key header"
                });
                return;
            }

            Licensee? licensee = null;

            // Strategy 1: License key authentication (preferred for licensees)
            if (!string.IsNullOrEmpty(licenseKey))
            {
                licensee = await ValidateAndGetLicenseeByKeyAsync(licenseKey, licenseeService);
            }
            // Strategy 2: Licensee ID (for internal services or pre-authenticated requests)
            else if (licenseeId.HasValue)
            {
                licensee = await GetLicenseeByIdAsync(licenseeId.Value, licenseeService);
            }

            if (licensee == null)
            {
                _logger.LogWarning("Invalid licensee credentials provided");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Unauthorized",
                    message = "Invalid licensee credentials"
                });
                return;
            }

            // Check license status
            if (licensee.Status == LicenseeStatus.Suspended)
            {
                _logger.LogWarning("Licensee {LicenseeId} is suspended", licensee.Id);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "License Suspended",
                    message = "Your license has been suspended. Please contact support.",
                    licensee_id = licensee.Id
                });
                return;
            }

            if (licensee.IsExpired())
            {
                _logger.LogWarning("Licensee {LicenseeId} license expired on {ExpiresAt}",
                    licensee.Id, licensee.ExpiresAt);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "License Expired",
                    message = $"Your license expired on {licensee.ExpiresAt:yyyy-MM-dd}. Please renew your license.",
                    licensee_id = licensee.Id,
                    expired_at = licensee.ExpiresAt
                });
                return;
            }

            // Store licensee in HttpContext for downstream use
            context.Items["Licensee"] = licensee;
            context.Items["LicenseeId"] = licensee.Id;

            // Set licensee context for async flow (shared across all services)
            LicenseeContext.LicenseeId = licensee.Id;

            // Add response headers for debugging
            context.Response.Headers.Add("X-Licensee-Id", licensee.Id.ToString());
            context.Response.Headers.Add("X-Licensee-Name", licensee.Name);
            context.Response.Headers.Add("X-License-Status", licensee.Status.ToString());

            _logger.LogInformation(
                "Request authenticated for licensee: {LicenseeName} ({LicenseeId}), Status: {Status}",
                licensee.Name, licensee.Id, licensee.Status);

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in LicenseeContextMiddleware");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Internal Server Error",
                message = "An error occurred while processing licensee context"
            });
        }
    }

    /// <summary>
    /// Extract licensee ID from request headers, route, or query
    /// </summary>
    private Guid? ExtractLicenseeId(HttpContext context)
    {
        // Strategy 1: X-Licensee-Id header (preferred)
        if (context.Request.Headers.TryGetValue("X-Licensee-Id", out var headerValue))
        {
            if (Guid.TryParse(headerValue, out var licenseeId))
            {
                return licenseeId;
            }
        }

        // Strategy 2: Route parameter
        if (context.Request.RouteValues.TryGetValue("licenseeId", out var routeValue))
        {
            if (Guid.TryParse(routeValue?.ToString(), out var licenseeId))
            {
                return licenseeId;
            }
        }

        // Strategy 3: Query parameter
        if (context.Request.Query.TryGetValue("licenseeId", out var queryValue))
        {
            if (Guid.TryParse(queryValue, out var licenseeId))
            {
                return licenseeId;
            }
        }

        return null;
    }

    /// <summary>
    /// Extract license key from request header
    /// </summary>
    private string? ExtractLicenseKey(HttpContext context)
    {
        // X-License-Key header
        if (context.Request.Headers.TryGetValue("X-License-Key", out var licenseKey))
        {
            return licenseKey.ToString();
        }

        // Authorization: License {key} header (alternative)
        if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var auth = authHeader.ToString();
            if (auth.StartsWith("License ", StringComparison.OrdinalIgnoreCase))
            {
                return auth.Substring("License ".Length).Trim();
            }
        }

        return null;
    }

    /// <summary>
    /// Validate license key and get licensee (with caching)
    /// </summary>
    private async Task<Licensee?> ValidateAndGetLicenseeByKeyAsync(
        string licenseKey,
        ILicenseeService licenseeService)
    {
        var cacheKey = $"licensee:key:{licenseKey.GetHashCode()}";

        // Check cache first (cache for 5 minutes)
        if (_cache.TryGetValue(cacheKey, out Licensee? cachedLicensee))
        {
            return cachedLicensee;
        }

        // Validate with service
        var licensee = await licenseeService.ValidateLicenseKeyAsync(licenseKey);

        if (licensee != null && licensee.IsValid())
        {
            // Cache valid licensees
            _cache.Set(cacheKey, licensee, TimeSpan.FromMinutes(5));
        }

        return licensee;
    }

    /// <summary>
    /// Get licensee by ID (with caching)
    /// </summary>
    private async Task<Licensee?> GetLicenseeByIdAsync(
        Guid licenseeId,
        ILicenseeService licenseeService)
    {
        var cacheKey = $"licensee:id:{licenseeId}";

        // Check cache first (cache for 5 minutes)
        if (_cache.TryGetValue(cacheKey, out Licensee? cachedLicensee))
        {
            return cachedLicensee;
        }

        // Get from service
        var licensee = await licenseeService.GetByIdAsync(licenseeId);

        if (licensee != null && licensee.IsValid())
        {
            // Cache valid licensees
            _cache.Set(cacheKey, licensee, TimeSpan.FromMinutes(5));
        }

        return licensee;
    }

    /// <summary>
    /// Check if the endpoint is public and doesn't require licensee authentication
    /// </summary>
    private bool IsPublicEndpoint(PathString path)
    {
        var publicPaths = new[]
        {
            "/health",
            "/healthz",
            "/ready",
            "/metrics",
            "/swagger",
            "/api/auth/login",
            "/api/auth/register",
            "/api/public"
        };

        return publicPaths.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Extension methods for LicenseeContextMiddleware
/// </summary>
public static class LicenseeContextMiddlewareExtensions
{
    /// <summary>
    /// Add Licensee Context Middleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UseLicenseeContext(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<LicenseeContextMiddleware>();
    }

    /// <summary>
    /// Get the current licensee from HttpContext
    /// </summary>
    public static Licensee? GetLicensee(this HttpContext context)
    {
        if (context.Items.TryGetValue("Licensee", out var licensee))
        {
            return licensee as Licensee;
        }
        return null;
    }

    /// <summary>
    /// Get the current licensee ID from HttpContext
    /// </summary>
    public static Guid? GetLicenseeId(this HttpContext context)
    {
        if (context.Items.TryGetValue("LicenseeId", out var licenseeId))
        {
            return licenseeId as Guid?;
        }
        return null;
    }

    /// <summary>
    /// Get the current licensee or throw exception if not found
    /// </summary>
    public static Licensee GetLicenseeOrThrow(this HttpContext context)
    {
        var licensee = context.GetLicensee();
        if (licensee == null)
        {
            throw new LicenseeContextMissingException();
        }
        return licensee;
    }
}
