using Binah.Auth.Models;
using Binah.Core.Models;
using Binah.Core.Services;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Binah.Auth.Services;

/// <summary>
/// Implementation of audit logging service
/// </summary>
public class AuditService : IAuditService
{
    private readonly AuthDbContext _context;

    public AuditService(AuthDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task LogActionAsync(AuditLog log)
    {
        if (log == null) throw new ArgumentNullException(nameof(log));

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<(List<AuditLog> logs, int totalCount)> GetAuditLogsAsync(
        string tenantId,
        string? userId = null,
        string? action = null,
        string? resource = null,
        string? resourceId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int skip = 0,
        int take = 50)
    {
        if (string.IsNullOrEmpty(tenantId))
            throw new ArgumentException("TenantId is required", nameof(tenantId));

        var query = _context.AuditLogs
            .Where(a => a.TenantId == tenantId);

        if (!string.IsNullOrEmpty(userId))
            query = query.Where(a => a.UserId == userId);

        if (!string.IsNullOrEmpty(action))
            query = query.Where(a => a.Action == action);

        if (!string.IsNullOrEmpty(resource))
            query = query.Where(a => a.Resource == resource);

        if (!string.IsNullOrEmpty(resourceId))
            query = query.Where(a => a.ResourceId == resourceId);

        if (startDate.HasValue)
            query = query.Where(a => a.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(a => a.Timestamp <= endDate.Value);

        var totalCount = await query.CountAsync();

        var logs = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        return (logs, totalCount);
    }

    public async Task<List<AuditLog>> GetUserActivityAsync(
        string tenantId,
        string userId,
        int skip = 0,
        int take = 50)
    {
        if (string.IsNullOrEmpty(tenantId))
            throw new ArgumentException("TenantId is required", nameof(tenantId));
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("UserId is required", nameof(userId));

        return await _context.AuditLogs
            .Where(a => a.TenantId == tenantId && a.UserId == userId)
            .OrderByDescending(a => a.Timestamp)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<List<AuditLog>> GetResourceHistoryAsync(
        string tenantId,
        string resource,
        string resourceId,
        int skip = 0,
        int take = 50)
    {
        if (string.IsNullOrEmpty(tenantId))
            throw new ArgumentException("TenantId is required", nameof(tenantId));
        if (string.IsNullOrEmpty(resource))
            throw new ArgumentException("Resource is required", nameof(resource));
        if (string.IsNullOrEmpty(resourceId))
            throw new ArgumentException("ResourceId is required", nameof(resourceId));

        return await _context.AuditLogs
            .Where(a => a.TenantId == tenantId && a.Resource == resource && a.ResourceId == resourceId)
            .OrderByDescending(a => a.Timestamp)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<byte[]> ExportAuditLogsAsync(
        string tenantId,
        DateTime startDate,
        DateTime endDate)
    {
        if (string.IsNullOrEmpty(tenantId))
            throw new ArgumentException("TenantId is required", nameof(tenantId));

        var logs = await _context.AuditLogs
            .Where(a => a.TenantId == tenantId && a.Timestamp >= startDate && a.Timestamp <= endDate)
            .OrderBy(a => a.Timestamp)
            .ToListAsync();

        var csv = new StringBuilder();
        csv.AppendLine("Timestamp,UserId,Action,Resource,ResourceId,IpAddress,UserAgent,RequestPath,HttpMethod,StatusCode");

        foreach (var log in logs)
        {
            csv.AppendLine($"{log.Timestamp:O}," +
                          $"\"{log.UserId ?? ""}\"," +
                          $"\"{log.Action}\"," +
                          $"\"{log.Resource ?? ""}\"," +
                          $"\"{log.ResourceId ?? ""}\"," +
                          $"\"{log.IpAddress ?? ""}\"," +
                          $"\"{EscapeCsv(log.UserAgent)}\"," +
                          $"\"{log.RequestPath ?? ""}\"," +
                          $"\"{log.HttpMethod ?? ""}\"," +
                          $"{log.StatusCode}");
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    public async Task<AuditStats> GetAuditStatsAsync(
        string tenantId,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        if (string.IsNullOrEmpty(tenantId))
            throw new ArgumentException("TenantId is required", nameof(tenantId));

        var query = _context.AuditLogs.Where(a => a.TenantId == tenantId);

        if (startDate.HasValue)
            query = query.Where(a => a.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(a => a.Timestamp <= endDate.Value);

        var logs = await query.ToListAsync();

        var stats = new AuditStats
        {
            TotalActions = logs.Count,
            UniqueUsers = logs.Where(a => !string.IsNullOrEmpty(a.UserId))
                             .Select(a => a.UserId)
                             .Distinct()
                             .Count(),
            ActionCounts = logs.GroupBy(a => a.Action)
                              .ToDictionary(g => g.Key, g => g.Count()),
            ResourceCounts = logs.Where(a => !string.IsNullOrEmpty(a.Resource))
                                .GroupBy(a => a.Resource!)
                                .ToDictionary(g => g.Key, g => g.Count()),
            DailyActivity = logs.GroupBy(a => a.Timestamp.Date.ToString("yyyy-MM-dd"))
                               .ToDictionary(g => g.Key, g => g.Count())
        };

        return stats;
    }

    private string? EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        // Escape double quotes and remove newlines
        return value.Replace("\"", "\"\"").Replace("\n", " ").Replace("\r", "");
    }
}
