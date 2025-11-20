using Binah.Auth.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Binah.Auth.Services;

/// <summary>
/// API key management service implementation
/// </summary>
public class ApiKeyService : IApiKeyService
{
    private readonly AuthDbContext _context;
    private readonly ILogger<ApiKeyService> _logger;

    public ApiKeyService(AuthDbContext context, ILogger<ApiKeyService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ApiKeyCreateResponse> CreateKeyAsync(string tenantId, string userId, string name, List<string> scopes, string environment = "live", DateTime? expiresAt = null)
    {
        // Generate the API key
        var prefix = environment == "test" ? "bk_test_" : "bk_live_";
        var randomPart = GenerateRandomKey(32);
        var plaintextKey = prefix + randomPart;

        // Hash the key for storage
        var keyHash = HashKey(plaintextKey);

        var apiKey = new ApiKey
        {
            TenantId = tenantId,
            UserId = userId,
            Name = name,
            KeyPrefix = prefix + randomPart.Substring(0, 4) + "...",
            KeyHash = keyHash,
            Scopes = scopes,
            Environment = environment,
            ExpiresAt = expiresAt,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ApiKeys.Add(apiKey);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created API key {KeyId} for tenant {TenantId}", apiKey.Id, tenantId);

        return new ApiKeyCreateResponse
        {
            Key = ToDto(apiKey),
            PlaintextKey = plaintextKey
        };
    }

    public async Task<List<ApiKeyDto>> ListKeysAsync(string tenantId)
    {
        var keys = await _context.ApiKeys
            .Where(k => k.TenantId == tenantId)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync();

        return keys.Select(ToDto).ToList();
    }

    public async Task<ApiKeyDto?> GetKeyAsync(string tenantId, string keyId)
    {
        var key = await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.TenantId == tenantId && k.Id == keyId);

        return key == null ? null : ToDto(key);
    }

    public async Task<ApiKeyValidationResult?> ValidateKeyAsync(string plaintextKey)
    {
        var keyHash = HashKey(plaintextKey);

        var key = await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash && k.IsActive);

        if (key == null)
            return null;

        // Check expiration
        if (key.ExpiresAt.HasValue && key.ExpiresAt.Value < DateTime.UtcNow)
        {
            _logger.LogWarning("API key {KeyId} has expired", key.Id);
            return null;
        }

        // Check if revoked
        if (key.RevokedAt.HasValue)
        {
            _logger.LogWarning("API key {KeyId} has been revoked", key.Id);
            return null;
        }

        // Update last used
        key.LastUsedAt = DateTime.UtcNow;
        key.UsageCount++;
        await _context.SaveChangesAsync();

        return new ApiKeyValidationResult
        {
            KeyId = key.Id,
            TenantId = key.TenantId,
            UserId = key.UserId,
            Scopes = key.Scopes,
            Environment = key.Environment
        };
    }

    public async Task<bool> RevokeKeyAsync(string tenantId, string keyId, string revokedBy, string? reason = null)
    {
        var key = await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.TenantId == tenantId && k.Id == keyId);

        if (key == null) return false;

        key.IsActive = false;
        key.RevokedAt = DateTime.UtcNow;
        key.RevokedBy = revokedBy;
        key.RevocationReason = reason;
        key.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Revoked API key {KeyId} for tenant {TenantId}", keyId, tenantId);

        return true;
    }

    public async Task<ApiKeyDto?> UpdateKeyAsync(string tenantId, string keyId, string? name = null, List<string>? scopes = null, DateTime? expiresAt = null)
    {
        var key = await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.TenantId == tenantId && k.Id == keyId);

        if (key == null) return null;

        if (name != null) key.Name = name;
        if (scopes != null) key.Scopes = scopes;
        if (expiresAt.HasValue) key.ExpiresAt = expiresAt;

        key.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return ToDto(key);
    }

    public async Task RecordUsageAsync(string keyId, string httpMethod, string requestPath, int statusCode, long durationMs, string? ipAddress = null, string? userAgent = null)
    {
        var key = await _context.ApiKeys.FindAsync(keyId);
        if (key == null) return;

        var usage = new ApiKeyUsage
        {
            ApiKeyId = keyId,
            TenantId = key.TenantId,
            HttpMethod = httpMethod,
            RequestPath = requestPath,
            StatusCode = statusCode,
            DurationMs = durationMs,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Timestamp = DateTime.UtcNow
        };

        _context.ApiKeyUsages.Add(usage);

        // Update last used info on the key
        key.LastUsedAt = DateTime.UtcNow;
        key.LastUsedIp = ipAddress;

        await _context.SaveChangesAsync();
    }

    public async Task<ApiKeyUsageStats> GetUsageStatsAsync(string tenantId, string keyId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.ApiKeyUsages
            .Where(u => u.ApiKeyId == keyId && u.TenantId == tenantId);

        if (startDate.HasValue)
            query = query.Where(u => u.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(u => u.Timestamp <= endDate.Value);

        var usages = await query.ToListAsync();

        return new ApiKeyUsageStats
        {
            ApiKeyId = keyId,
            TotalRequests = usages.Count,
            SuccessfulRequests = usages.Count(u => u.StatusCode >= 200 && u.StatusCode < 300),
            FailedRequests = usages.Count(u => u.StatusCode >= 400),
            AverageDurationMs = usages.Count > 0 ? usages.Average(u => u.DurationMs) : 0,
            RequestsByEndpoint = usages
                .GroupBy(u => u.RequestPath)
                .ToDictionary(g => g.Key, g => g.Count()),
            RequestsByDay = usages
                .GroupBy(u => u.Timestamp.Date.ToString("yyyy-MM-dd"))
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    public async Task<ApiKeyCreateResponse> RotateKeyAsync(string tenantId, string keyId, string userId)
    {
        var oldKey = await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.TenantId == tenantId && k.Id == keyId);

        if (oldKey == null)
            throw new InvalidOperationException("API key not found");

        // Create new key with same settings
        var newKeyResponse = await CreateKeyAsync(
            tenantId,
            userId,
            oldKey.Name + " (rotated)",
            oldKey.Scopes,
            oldKey.Environment,
            oldKey.ExpiresAt
        );

        // Revoke old key
        await RevokeKeyAsync(tenantId, keyId, userId, "Key rotated");

        _logger.LogInformation("Rotated API key {OldKeyId} to {NewKeyId} for tenant {TenantId}",
            keyId, newKeyResponse.Key.Id, tenantId);

        return newKeyResponse;
    }

    private static string GenerateRandomKey(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var bytes = RandomNumberGenerator.GetBytes(length);
        var result = new StringBuilder(length);

        foreach (var b in bytes)
        {
            result.Append(chars[b % chars.Length]);
        }

        return result.ToString();
    }

    private static string HashKey(string key)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return Convert.ToBase64String(bytes);
    }

    private static ApiKeyDto ToDto(ApiKey key)
    {
        return new ApiKeyDto
        {
            Id = key.Id,
            Name = key.Name,
            KeyPrefix = key.KeyPrefix,
            Scopes = key.Scopes,
            Environment = key.Environment,
            IsActive = key.IsActive && !key.RevokedAt.HasValue,
            ExpiresAt = key.ExpiresAt,
            LastUsedAt = key.LastUsedAt,
            UsageCount = key.UsageCount,
            CreatedAt = key.CreatedAt
        };
    }
}
