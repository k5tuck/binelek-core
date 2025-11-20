using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;

namespace Binah.API.Middleware;

/// <summary>
/// Middleware to extract and validate domain context from requests
/// </summary>
public class DomainContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DomainContextMiddleware> _logger;
    private readonly IMemoryCache _cache;
    private readonly HttpClient _domainRegistryClient;

    // Domain Registry Service URL (from configuration)
    private readonly string _domainRegistryUrl;

    public DomainContextMiddleware(
        RequestDelegate next,
        ILogger<DomainContextMiddleware> logger,
        IMemoryCache cache,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _cache = cache;
        _domainRegistryClient = httpClientFactory.CreateClient("DomainRegistry");
        _domainRegistryUrl = configuration["DomainRegistry:Url"] ?? "http://localhost:8095";
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Extract domain from request
        var domainId = ExtractDomainFromRequest(context);

        if (string.IsNullOrEmpty(domainId))
        {
            // Default to "real-estate" if no domain specified (backwards compatibility)
            domainId = "real-estate";
            _logger.LogWarning("No domain specified in request, defaulting to 'real-estate'");
        }

        // Validate domain exists
        var isValid = await ValidateDomainAsync(domainId);

        if (!isValid)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Invalid Domain",
                message = $"Domain '{domainId}' not found or not configured",
                available_domains = await GetAvailableDomainsAsync()
            });
            return;
        }

        // Add domain to HttpContext for downstream use
        context.Items["DomainId"] = domainId;
        context.Response.Headers.Add("X-Domain-Id", domainId);

        _logger.LogInformation("Request processing for domain: {DomainId}", domainId);

        await _next(context);
    }

    /// <summary>
    /// Extract domain ID from request (multiple strategies)
    /// </summary>
    private string? ExtractDomainFromRequest(HttpContext context)
    {
        // Strategy 1: X-Domain header (preferred for API clients)
        if (context.Request.Headers.TryGetValue("X-Domain", out var headerDomain))
        {
            return headerDomain.ToString();
        }

        // Strategy 2: Subdomain (e.g., healthcare.binelek.com)
        var host = context.Request.Host.Host;
        var subdomainMatch = Regex.Match(host, @"^(\w+)\.binelek\.");
        if (subdomainMatch.Success)
        {
            var subdomain = subdomainMatch.Groups[1].Value;
            // Don't treat 'www', 'api', 'app' as domains
            if (subdomain != "www" && subdomain != "api" && subdomain != "app")
            {
                return subdomain;
            }
        }

        // Strategy 3: Path prefix (e.g., /api/domains/healthcare/...)
        var pathMatch = Regex.Match(context.Request.Path, @"^/api/domains/([a-z-]+)/");
        if (pathMatch.Success)
        {
            return pathMatch.Groups[1].Value;
        }

        // Strategy 4: Query parameter (e.g., ?domain=healthcare)
        if (context.Request.Query.TryGetValue("domain", out var queryDomain))
        {
            return queryDomain.ToString();
        }

        // No domain specified
        return null;
    }

    /// <summary>
    /// Validate domain exists and is properly configured
    /// </summary>
    private async Task<bool> ValidateDomainAsync(string domainId)
    {
        var cacheKey = $"domain:valid:{domainId}";

        // Check cache first (cache validation results for 5 minutes)
        if (_cache.TryGetValue(cacheKey, out bool cachedResult))
        {
            return cachedResult;
        }

        try
        {
            // Call Domain Registry Service to validate
            var response = await _domainRegistryClient.GetAsync($"{_domainRegistryUrl}/api/domains/{domainId}");

            if (response.IsSuccessStatusCode)
            {
                // Cache positive result
                _cache.Set(cacheKey, true, TimeSpan.FromMinutes(5));
                return true;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Cache negative result (shorter TTL)
                _cache.Set(cacheKey, false, TimeSpan.FromMinutes(1));
                return false;
            }

            // Domain Registry Service error, log and fail open (allow request)
            _logger.LogWarning("Domain Registry Service error: {StatusCode}", response.StatusCode);
            return true; // Fail open for now
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate domain '{DomainId}'", domainId);
            // Fail open if Domain Registry is unavailable
            return true;
        }
    }

    /// <summary>
    /// Get list of available domains for error message
    /// </summary>
    private async Task<List<string>> GetAvailableDomainsAsync()
    {
        var cacheKey = "domains:available";

        // Check cache first
        if (_cache.TryGetValue(cacheKey, out List<string>? cachedDomains))
        {
            return cachedDomains ?? new List<string>();
        }

        try
        {
            var response = await _domainRegistryClient.GetAsync($"{_domainRegistryUrl}/api/domains");

            if (response.IsSuccessStatusCode)
            {
                var domains = await response.Content.ReadFromJsonAsync<List<DomainSummary>>();
                var domainIds = domains?.Select(d => d.Id).ToList() ?? new List<string>();

                // Cache for 10 minutes
                _cache.Set(cacheKey, domainIds, TimeSpan.FromMinutes(10));

                return domainIds;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch available domains");
        }

        // Return known defaults if Domain Registry is unavailable
        return new List<string> { "real-estate", "healthcare", "finance", "smart-cities", "logistics" };
    }

    private class DomainSummary
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}

/// <summary>
/// Extension methods for DomainContextMiddleware
/// </summary>
public static class DomainContextMiddlewareExtensions
{
    /// <summary>
    /// Add Domain Context Middleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UseDomainContext(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<DomainContextMiddleware>();
    }

    /// <summary>
    /// Get the current domain ID from HttpContext
    /// </summary>
    public static string? GetDomainId(this HttpContext context)
    {
        if (context.Items.TryGetValue("DomainId", out var domainId))
        {
            return domainId as string;
        }
        return null;
    }
}
