namespace Binah.API.Models;

/// <summary>
/// Service health status information
/// </summary>
public class ServiceHealth
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "healthy"; // healthy, degraded, unhealthy
    public double Latency { get; set; }
    public string LastCheck { get; set; } = string.Empty;
    public int Port { get; set; }
}

/// <summary>
/// Database connection health information
/// </summary>
public class DatabaseHealth
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "healthy";
    public int Connections { get; set; }
    public int MaxConnections { get; set; }
}

/// <summary>
/// Message queue health information
/// </summary>
public class MessageQueueHealth
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "healthy";
    public int Brokers { get; set; }
    public int Topics { get; set; }
    public int Partitions { get; set; }
    public int ConsumerGroups { get; set; }
}

/// <summary>
/// Complete system health response
/// </summary>
public class SystemHealthResponse
{
    public List<ServiceHealth> Services { get; set; } = new();
    public List<DatabaseHealth> Databases { get; set; } = new();
    public MessageQueueHealth MessageQueue { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Environment information
/// </summary>
public class EnvironmentInfo
{
    public string Version { get; set; } = string.Empty;
    public string Build { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Node { get; set; } = string.Empty;
    public Dictionary<string, object> Configuration { get; set; } = new();
}

/// <summary>
/// Log entry
/// </summary>
public class LogEntry
{
    public string Id { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty; // info, warn, error, debug
    public string Service { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Log filters for querying logs
/// </summary>
public class LogFilters
{
    public string? Level { get; set; }
    public string? Service { get; set; }
    public string? Search { get; set; }
    public int Limit { get; set; } = 100;
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
}

/// <summary>
/// System logs response
/// </summary>
public class SystemLogsResponse
{
    public List<LogEntry> Logs { get; set; } = new();
    public int Total { get; set; }
}

/// <summary>
/// Performance metrics
/// </summary>
public class PerformanceMetrics
{
    public double CpuUsage { get; set; }
    public double MemoryUsed { get; set; }
    public double MemoryTotal { get; set; }
    public double NetworkThroughput { get; set; }
    public double AvgResponseTime { get; set; }
    public double P95ResponseTime { get; set; }
    public double RequestRate { get; set; }
    public double ErrorRate { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Cache status information
/// </summary>
public class CacheStatus
{
    public string Name { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public double HitRate { get; set; }
    public string Keys { get; set; } = string.Empty;
    public int Ttl { get; set; }
}

/// <summary>
/// System cache response
/// </summary>
public class SystemCacheResponse
{
    public List<CacheStatus> Caches { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Cache clear request
/// </summary>
public class ClearCacheRequest
{
    public string? CacheName { get; set; } // null means clear all
}

/// <summary>
/// Cache clear response
/// </summary>
public class ClearCacheResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> ClearedCaches { get; set; } = new();
}

/// <summary>
/// Feature flag
/// </summary>
public class FeatureFlag
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Tier { get; set; } = string.Empty;
}

/// <summary>
/// Feature flags response
/// </summary>
public class FeatureFlagsResponse
{
    public List<FeatureFlag> Flags { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
