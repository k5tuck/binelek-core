using System;
using System.Collections.Generic;

namespace Binah.Auth.Models;

/// <summary>
/// API key for programmatic access
/// </summary>
public class ApiKey
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Tenant ID for multi-tenancy
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// User ID who created the key
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Name/description of the API key
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Key prefix for display (e.g., "bk_live_abc")
    /// </summary>
    public string KeyPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Hashed key value (SHA256)
    /// </summary>
    public string KeyHash { get; set; } = string.Empty;

    /// <summary>
    /// Scopes/permissions for this key
    /// </summary>
    public List<string> Scopes { get; set; } = new();

    /// <summary>
    /// Whether this is a test or live key
    /// </summary>
    public string Environment { get; set; } = "live";

    /// <summary>
    /// Whether the key is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Expiration date (null = no expiration)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Last time the key was used
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// IP address last used from
    /// </summary>
    public string? LastUsedIp { get; set; }

    /// <summary>
    /// Total number of times the key was used
    /// </summary>
    public long UsageCount { get; set; } = 0;

    /// <summary>
    /// Rate limit per minute (null = use default)
    /// </summary>
    public int? RateLimitPerMinute { get; set; }

    /// <summary>
    /// Allowed IP addresses (empty = all IPs allowed)
    /// </summary>
    public List<string> AllowedIps { get; set; } = new();

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Revocation timestamp
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// User ID who revoked the key
    /// </summary>
    public string? RevokedBy { get; set; }

    /// <summary>
    /// Reason for revocation
    /// </summary>
    public string? RevocationReason { get; set; }
}

/// <summary>
/// API key usage log entry
/// </summary>
public class ApiKeyUsage
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// API key ID
    /// </summary>
    public string ApiKeyId { get; set; } = string.Empty;

    /// <summary>
    /// Tenant ID
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// HTTP method
    /// </summary>
    public string HttpMethod { get; set; } = string.Empty;

    /// <summary>
    /// Request path
    /// </summary>
    public string RequestPath { get; set; } = string.Empty;

    /// <summary>
    /// Response status code
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Request duration in milliseconds
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Client IP address
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Timestamp
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Available API key scopes
/// </summary>
public static class ApiKeyScopes
{
    public const string ReadEntities = "entities:read";
    public const string WriteEntities = "entities:write";
    public const string DeleteEntities = "entities:delete";
    public const string ReadRelationships = "relationships:read";
    public const string WriteRelationships = "relationships:write";
    public const string Search = "search:read";
    public const string Analytics = "analytics:read";
    public const string Export = "export:read";
    public const string Admin = "admin:all";

    public static readonly List<string> All = new()
    {
        ReadEntities,
        WriteEntities,
        DeleteEntities,
        ReadRelationships,
        WriteRelationships,
        Search,
        Analytics,
        Export,
        Admin
    };
}

/// <summary>
/// API key creation response (includes plaintext key)
/// </summary>
public class ApiKeyCreateResponse
{
    /// <summary>
    /// The API key details
    /// </summary>
    public ApiKeyDto Key { get; set; } = null!;

    /// <summary>
    /// The plaintext API key (only returned once at creation)
    /// </summary>
    public string PlaintextKey { get; set; } = string.Empty;
}

/// <summary>
/// API key DTO (without sensitive data)
/// </summary>
public class ApiKeyDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = string.Empty;
    public List<string> Scopes { get; set; } = new();
    public string Environment { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public long UsageCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// API key usage statistics
/// </summary>
public class ApiKeyUsageStats
{
    public string ApiKeyId { get; set; } = string.Empty;
    public long TotalRequests { get; set; }
    public long SuccessfulRequests { get; set; }
    public long FailedRequests { get; set; }
    public double AverageDurationMs { get; set; }
    public Dictionary<string, int> RequestsByEndpoint { get; set; } = new();
    public Dictionary<string, int> RequestsByDay { get; set; } = new();
}
