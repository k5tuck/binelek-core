using Binah.Auth.Models;

namespace Binah.Auth.Services;

/// <summary>
/// Interface for API key management operations
/// </summary>
public interface IApiKeyService
{
    /// <summary>
    /// Create a new API key (returns plaintext key once)
    /// </summary>
    Task<ApiKeyCreateResponse> CreateKeyAsync(string tenantId, string userId, string name, List<string> scopes, string environment = "live", DateTime? expiresAt = null);

    /// <summary>
    /// List all API keys for a tenant
    /// </summary>
    Task<List<ApiKeyDto>> ListKeysAsync(string tenantId);

    /// <summary>
    /// Get API key details by ID
    /// </summary>
    Task<ApiKeyDto?> GetKeyAsync(string tenantId, string keyId);

    /// <summary>
    /// Validate an API key and return associated tenant/user
    /// </summary>
    Task<ApiKeyValidationResult?> ValidateKeyAsync(string plaintextKey);

    /// <summary>
    /// Revoke an API key
    /// </summary>
    Task<bool> RevokeKeyAsync(string tenantId, string keyId, string revokedBy, string? reason = null);

    /// <summary>
    /// Update API key (name, scopes, etc.)
    /// </summary>
    Task<ApiKeyDto?> UpdateKeyAsync(string tenantId, string keyId, string? name = null, List<string>? scopes = null, DateTime? expiresAt = null);

    /// <summary>
    /// Record API key usage
    /// </summary>
    Task RecordUsageAsync(string keyId, string httpMethod, string requestPath, int statusCode, long durationMs, string? ipAddress = null, string? userAgent = null);

    /// <summary>
    /// Get usage statistics for an API key
    /// </summary>
    Task<ApiKeyUsageStats> GetUsageStatsAsync(string tenantId, string keyId, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// Rotate an API key (create new, revoke old)
    /// </summary>
    Task<ApiKeyCreateResponse> RotateKeyAsync(string tenantId, string keyId, string userId);
}

/// <summary>
/// Result of API key validation
/// </summary>
public class ApiKeyValidationResult
{
    public string KeyId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public List<string> Scopes { get; set; } = new();
    public string Environment { get; set; } = string.Empty;
}
