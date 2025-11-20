using Binah.Contracts.Common;
using Binah.Core.Models;
using Binah.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Binah.Auth.Controllers;

/// <summary>
/// Controller for audit logging operations
/// </summary>
[ApiController]
[Route("api/audit")]
[Authorize]
public class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;
    private readonly ILogger<AuditController> _logger;

    public AuditController(IAuditService auditService, ILogger<AuditController> logger)
    {
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get audit logs with optional filtering
    /// </summary>
    [HttpGet("logs")]
    [ProducesResponseType(typeof(ApiResponse<AuditLogsResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AuditLogsResponse>>> GetAuditLogs(
        [FromQuery] string? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] string? resource = null,
        [FromQuery] string? resourceId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value ?? "system";

        var (logs, totalCount) = await _auditService.GetAuditLogsAsync(
            tenantId, userId, action, resource, resourceId, startDate, endDate, skip, take);

        var response = new AuditLogsResponse
        {
            Logs = logs,
            TotalCount = totalCount,
            Skip = skip,
            Take = take
        };

        return Ok(ApiResponse<AuditLogsResponse>.Ok(response));
    }

    /// <summary>
    /// Get specific audit log entry
    /// </summary>
    [HttpGet("logs/{id}")]
    [ProducesResponseType(typeof(ApiResponse<AuditLog>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<AuditLog>>> GetAuditLog(Guid id)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value ?? "system";

        var (logs, _) = await _auditService.GetAuditLogsAsync(tenantId);
        var log = logs.FirstOrDefault(l => l.Id == id);

        if (log == null)
        {
            return NotFound();
        }

        return Ok(ApiResponse<AuditLog>.Ok(log));
    }

    /// <summary>
    /// Get user activity logs
    /// </summary>
    [HttpGet("users/{userId}")]
    [ProducesResponseType(typeof(ApiResponse<List<AuditLog>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<AuditLog>>>> GetUserActivity(
        string userId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value ?? "system";

        var logs = await _auditService.GetUserActivityAsync(tenantId, userId, skip, take);

        return Ok(ApiResponse<List<AuditLog>>.Ok(logs));
    }

    /// <summary>
    /// Get resource history
    /// </summary>
    [HttpGet("resources/{resource}/{resourceId}")]
    [ProducesResponseType(typeof(ApiResponse<List<AuditLog>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<AuditLog>>>> GetResourceHistory(
        string resource,
        string resourceId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value ?? "system";

        var logs = await _auditService.GetResourceHistoryAsync(tenantId, resource, resourceId, skip, take);

        return Ok(ApiResponse<List<AuditLog>>.Ok(logs));
    }

    /// <summary>
    /// Export audit logs to CSV
    /// </summary>
    [HttpGet("export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportAuditLogs(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value ?? "system";

        var csvData = await _auditService.ExportAuditLogsAsync(tenantId, startDate, endDate);

        return File(csvData, "text/csv", $"audit_logs_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.csv");
    }

    /// <summary>
    /// Get audit statistics
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<AuditStats>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AuditStats>>> GetAuditStats(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value ?? "system";

        var stats = await _auditService.GetAuditStatsAsync(tenantId, startDate, endDate);

        return Ok(ApiResponse<AuditStats>.Ok(stats));
    }
}

/// <summary>
/// Response model for paginated audit logs
/// </summary>
public class AuditLogsResponse
{
    public List<AuditLog> Logs { get; set; } = new();
    public int TotalCount { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
}
