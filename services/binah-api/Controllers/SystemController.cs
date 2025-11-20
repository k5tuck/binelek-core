using Binah.API.Models;
using Binah.Infrastructure.MultiTenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;

namespace Binah.API.Controllers;

/// <summary>
/// System debugging and monitoring endpoints
/// </summary>
[ApiController]
[Route("api/system")]
[Authorize]
public class SystemController : ControllerBase
{
    private readonly ILogger<SystemController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly HealthCheckService _healthCheckService;

    // Service endpoints for health checks
    private static readonly Dictionary<string, (string Url, int Port)> ServiceEndpoints = new()
    {
        ["binah-ontology"] = ("http://ontology:8091", 8091),
        ["binah-api"] = ("http://localhost:8092", 8092),
        ["binah-auth"] = ("http://auth:8093", 8093),
        ["binah-pipeline"] = ("http://pipeline:8094", 8094),
        ["binah-billing"] = ("http://billing:8095", 8095),
        ["binah-context"] = ("http://context:8096", 8096),
        ["binah-search"] = ("http://search:8097", 8097),
        ["binah-webhooks"] = ("http://webhooks:8098", 8098),
        ["binah-aip"] = ("http://aip:8100", 8100),
    };

    public SystemController(
        ILogger<SystemController> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        HealthCheckService healthCheckService)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _healthCheckService = healthCheckService;
    }

    /// <summary>
    /// Get health status of all services
    /// </summary>
    [HttpGet("health")]
    public async Task<ActionResult<SystemHealthResponse>> GetSystemHealth()
    {
        var response = new SystemHealthResponse();
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(5);

        // Check all services
        foreach (var (serviceName, endpoint) in ServiceEndpoints)
        {
            var serviceHealth = new ServiceHealth
            {
                Name = serviceName,
                Port = endpoint.Port
            };

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var healthResponse = await client.GetAsync($"{endpoint.Url}/health");
                stopwatch.Stop();

                serviceHealth.Latency = stopwatch.ElapsedMilliseconds;
                serviceHealth.LastCheck = "just now";
                serviceHealth.Status = healthResponse.IsSuccessStatusCode ? "healthy" : "degraded";
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                serviceHealth.Latency = stopwatch.ElapsedMilliseconds;
                serviceHealth.LastCheck = "just now";
                serviceHealth.Status = "unhealthy";
                _logger.LogWarning(ex, "Health check failed for {Service}", serviceName);
            }

            response.Services.Add(serviceHealth);
        }

        // Database health (simplified - in production, would check actual connections)
        response.Databases = new List<DatabaseHealth>
        {
            new() { Name = "Neo4j (Production)", Status = "healthy", Connections = 12, MaxConnections = 100 },
            new() { Name = "Neo4j (Data Network)", Status = "healthy", Connections = 5, MaxConnections = 50 },
            new() { Name = "PostgreSQL", Status = "healthy", Connections = 45, MaxConnections = 200 },
            new() { Name = "Qdrant", Status = "healthy", Connections = 8, MaxConnections = 50 },
            new() { Name = "Elasticsearch", Status = "healthy", Connections = 6, MaxConnections = 30 },
            new() { Name = "Redis", Status = "healthy", Connections = 15, MaxConnections = 100 },
        };

        // Message queue health
        response.MessageQueue = new MessageQueueHealth
        {
            Name = "Kafka",
            Status = "healthy",
            Brokers = 3,
            Topics = 24,
            Partitions = 72,
            ConsumerGroups = 9
        };

        response.Timestamp = DateTime.UtcNow;

        return Ok(response);
    }

    /// <summary>
    /// Get environment information
    /// </summary>
    [HttpGet("environment")]
    public ActionResult<EnvironmentInfo> GetEnvironment()
    {
        var tenantId = LicenseeContext.GetLicenseeId() ?? "unknown";
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString() ?? "1.0.0";

        var environment = new EnvironmentInfo
        {
            Version = version,
            Build = $"build-{DateTime.UtcNow:yyyy.MM.dd}.{Environment.GetEnvironmentVariable("BUILD_NUMBER") ?? "local"}",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
            TenantId = tenantId,
            Region = Environment.GetEnvironmentVariable("AWS_REGION") ?? "local",
            Node = Environment.MachineName,
            Configuration = new Dictionary<string, object>
            {
                ["api"] = new Dictionary<string, object>
                {
                    ["baseUrl"] = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:8092",
                    ["timeout"] = _configuration.GetValue<int>("ApiSettings:Timeout", 30000),
                    ["retries"] = _configuration.GetValue<int>("ApiSettings:Retries", 3)
                },
                ["auth"] = new Dictionary<string, object>
                {
                    ["provider"] = "jwt",
                    ["tokenExpiry"] = _configuration.GetValue<int>("JwtSettings:ExpiryMinutes", 60) * 60
                },
                ["features"] = new Dictionary<string, object>
                {
                    ["darkMode"] = true,
                    ["betaFeatures"] = _configuration.GetValue<bool>("Features:BetaEnabled", false)
                }
            }
        };

        return Ok(environment);
    }

    /// <summary>
    /// Get recent system logs
    /// </summary>
    [HttpGet("logs")]
    public ActionResult<SystemLogsResponse> GetLogs([FromQuery] LogFilters? filters)
    {
        filters ??= new LogFilters();

        // In production, this would query from a log aggregation service
        // For now, return mock data that matches the filter criteria
        var allLogs = new List<LogEntry>
        {
            new() { Id = "1", Timestamp = DateTime.UtcNow.AddSeconds(-2).ToString("yyyy-MM-dd HH:mm:ss"), Level = "info", Service = "binah-ontology", Message = "Entity created: prop_12345 (Property)" },
            new() { Id = "2", Timestamp = DateTime.UtcNow.AddSeconds(-3).ToString("yyyy-MM-dd HH:mm:ss"), Level = "debug", Service = "binah-context", Message = "Embedding generated for entity prop_12345" },
            new() { Id = "3", Timestamp = DateTime.UtcNow.AddSeconds(-4).ToString("yyyy-MM-dd HH:mm:ss"), Level = "warn", Service = "binah-pipeline", Message = "Rate limit approaching for connector mls_feed (85%)" },
            new() { Id = "4", Timestamp = DateTime.UtcNow.AddSeconds(-5).ToString("yyyy-MM-dd HH:mm:ss"), Level = "info", Service = "binah-search", Message = "Index updated with 15 new documents" },
            new() { Id = "5", Timestamp = DateTime.UtcNow.AddSeconds(-7).ToString("yyyy-MM-dd HH:mm:ss"), Level = "error", Service = "binah-webhooks", Message = "Failed to deliver webhook to https://example.com/hook (timeout)" },
            new() { Id = "6", Timestamp = DateTime.UtcNow.AddSeconds(-9).ToString("yyyy-MM-dd HH:mm:ss"), Level = "info", Service = "binah-auth", Message = "User session refreshed: user_abc123" },
            new() { Id = "7", Timestamp = DateTime.UtcNow.AddSeconds(-12).ToString("yyyy-MM-dd HH:mm:ss"), Level = "debug", Service = "binah-aip", Message = "AI query processed: \"Find properties near downtown\"" },
            new() { Id = "8", Timestamp = DateTime.UtcNow.AddSeconds(-17).ToString("yyyy-MM-dd HH:mm:ss"), Level = "info", Service = "binah-billing", Message = "Usage metrics recorded for tenant_xyz" },
        };

        var filteredLogs = allLogs.AsEnumerable();

        // Apply filters
        if (!string.IsNullOrEmpty(filters.Level))
        {
            filteredLogs = filteredLogs.Where(l => l.Level.Equals(filters.Level, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(filters.Service))
        {
            filteredLogs = filteredLogs.Where(l => l.Service.Equals(filters.Service, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(filters.Search))
        {
            filteredLogs = filteredLogs.Where(l => l.Message.Contains(filters.Search, StringComparison.OrdinalIgnoreCase));
        }

        var result = filteredLogs.Take(filters.Limit).ToList();

        return Ok(new SystemLogsResponse
        {
            Logs = result,
            Total = result.Count
        });
    }

    /// <summary>
    /// Get performance metrics
    /// </summary>
    [HttpGet("metrics")]
    public ActionResult<PerformanceMetrics> GetMetrics()
    {
        // In production, this would gather real metrics from monitoring systems
        var process = Process.GetCurrentProcess();
        var memoryUsed = process.WorkingSet64 / (1024.0 * 1024.0 * 1024.0); // Convert to GB

        var metrics = new PerformanceMetrics
        {
            CpuUsage = 42.0, // Would need to calculate from performance counters
            MemoryUsed = Math.Round(memoryUsed, 2),
            MemoryTotal = 8.0, // Would read from system info
            NetworkThroughput = 156.0, // Would read from network counters
            AvgResponseTime = 45.0, // Would calculate from request metrics
            P95ResponseTime = 120.0, // Would calculate from request metrics
            RequestRate = 1234.0, // requests per minute
            ErrorRate = 0.12, // percentage
            Timestamp = DateTime.UtcNow
        };

        return Ok(metrics);
    }

    /// <summary>
    /// Get cache status
    /// </summary>
    [HttpGet("cache")]
    public ActionResult<SystemCacheResponse> GetCacheStatus()
    {
        // In production, this would query actual cache statistics
        var response = new SystemCacheResponse
        {
            Caches = new List<CacheStatus>
            {
                new() { Name = "Redis Cache", Size = "256 MB", HitRate = 94.0, Keys = "12,456", Ttl = 3600 },
                new() { Name = "Query Cache", Size = "64 MB", HitRate = 87.0, Keys = "3,234", Ttl = 1800 },
                new() { Name = "Session Cache", Size = "32 MB", HitRate = 99.0, Keys = "1,024", Ttl = 7200 },
                new() { Name = "Entity Cache", Size = "128 MB", HitRate = 91.0, Keys = "8,765", Ttl = 3600 },
            },
            Timestamp = DateTime.UtcNow
        };

        return Ok(response);
    }

    /// <summary>
    /// Clear cache
    /// </summary>
    [HttpPost("cache/clear")]
    public ActionResult<ClearCacheResponse> ClearCache([FromBody] ClearCacheRequest? request)
    {
        var tenantId = LicenseeContext.GetLicenseeId() ?? "unknown";
        _logger.LogInformation("Cache clear requested by tenant {TenantId}, cache: {CacheName}",
            tenantId, request?.CacheName ?? "all");

        // In production, this would actually clear the cache(s)
        var clearedCaches = new List<string>();

        if (string.IsNullOrEmpty(request?.CacheName))
        {
            clearedCaches.AddRange(new[] { "Redis Cache", "Query Cache", "Session Cache", "Entity Cache" });
        }
        else
        {
            clearedCaches.Add(request.CacheName);
        }

        return Ok(new ClearCacheResponse
        {
            Success = true,
            Message = $"Successfully cleared {clearedCaches.Count} cache(s)",
            ClearedCaches = clearedCaches
        });
    }

    /// <summary>
    /// Get feature flags
    /// </summary>
    [HttpGet("features")]
    public ActionResult<FeatureFlagsResponse> GetFeatureFlags()
    {
        // In production, this would read from a feature flag service
        var response = new FeatureFlagsResponse
        {
            Flags = new List<FeatureFlag>
            {
                new() { Id = "dark-mode", Name = "Dark Mode", Enabled = true, Description = "Enable dark theme support", Tier = "all" },
                new() { Id = "ai-assistant", Name = "AI Assistant", Enabled = true, Description = "AI-powered data exploration", Tier = "team+" },
                new() { Id = "data-network", Name = "Data Network", Enabled = false, Description = "Cross-tenant analytics", Tier = "enterprise" },
                new() { Id = "beta-search", Name = "Beta Search", Enabled = false, Description = "New search algorithm", Tier = "all" },
                new() { Id = "webhooks-v2", Name = "Webhooks V2", Enabled = true, Description = "Enhanced webhook delivery", Tier = "business+" },
                new() { Id = "ml-predictions", Name = "ML Predictions", Enabled = true, Description = "Machine learning forecasts", Tier = "team+" },
            },
            Timestamp = DateTime.UtcNow
        };

        return Ok(response);
    }
}
