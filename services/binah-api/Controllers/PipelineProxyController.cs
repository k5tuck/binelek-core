using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Binah.API.Controllers;

/// <summary>
/// Proxy controller for Pipeline Service
/// Routes pipeline requests to the Pipeline microservice
/// </summary>
[Authorize]
[ApiController]
[Route("api/tenants/{tenantId}/pipelines")]
public class PipelineProxyController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PipelineProxyController> _logger;

    public PipelineProxyController(
        IHttpClientFactory httpClientFactory,
        ILogger<PipelineProxyController> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validate that the route tenantId matches the JWT tenant_id claim
    /// </summary>
    private bool ValidateTenantId(Guid routeTenantId, out string? jwtTenantId)
    {
        jwtTenantId = User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(jwtTenantId))
        {
            _logger.LogWarning("JWT token missing tenant_id claim");
            return false;
        }

        if (routeTenantId.ToString() != jwtTenantId)
        {
            _logger.LogWarning("Tenant forgery attempt: JWT={JwtTenantId}, Route={RouteTenantId}",
                jwtTenantId, routeTenantId);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Create a new pipeline
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreatePipeline(
        Guid tenantId,
        [FromBody] object request)
    {
        try
        {
            // SECURITY: Validate tenant ID from JWT matches route parameter
            if (!ValidateTenantId(tenantId, out var jwtTenantId))
            {
                return string.IsNullOrEmpty(jwtTenantId)
                    ? Unauthorized(new { error = "Tenant ID not found in token" })
                    : Forbid();
            }

            var client = _httpClientFactory.CreateClient("PipelineService");

            // Forward request with tenant header
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/tenants/{tenantId}/pipelines")
            {
                Content = JsonContent.Create(request)
            };
            httpRequest.Headers.Add("Authorization", Request.Headers["Authorization"].ToString());
            httpRequest.Headers.Add("X-Tenant-Id", jwtTenantId);

            var response = await client.SendAsync(httpRequest);
            var content = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating pipeline for tenant {TenantId}", tenantId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to create pipeline" });
        }
    }

    /// <summary>
    /// Get pipeline by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPipeline(Guid tenantId, Guid id)
    {
        try
        {
            if (!ValidateTenantId(tenantId, out var jwtTenantId))
                return string.IsNullOrEmpty(jwtTenantId) ? Unauthorized(new { error = "Tenant ID not found in token" }) : Forbid();

            var client = _httpClientFactory.CreateClient("PipelineService");
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/tenants/{tenantId}/pipelines/{id}");
            httpRequest.Headers.Add("Authorization", Request.Headers["Authorization"].ToString());
            httpRequest.Headers.Add("X-Tenant-Id", jwtTenantId);

            var response = await client.SendAsync(httpRequest);
            var content = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pipeline {PipelineId} for tenant {TenantId}", id, tenantId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to retrieve pipeline" });
        }
    }

    /// <summary>
    /// List all pipelines for tenant
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListPipelines(
        Guid tenantId,
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 50)
    {
        try
        {
            if (!ValidateTenantId(tenantId, out var jwtTenantId))
                return string.IsNullOrEmpty(jwtTenantId) ? Unauthorized(new { error = "Tenant ID not found in token" }) : Forbid();

            var client = _httpClientFactory.CreateClient("PipelineService");
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/tenants/{tenantId}/pipelines?skip={skip}&limit={limit}");
            httpRequest.Headers.Add("Authorization", Request.Headers["Authorization"].ToString());
            httpRequest.Headers.Add("X-Tenant-Id", jwtTenantId);

            var response = await client.SendAsync(httpRequest);
            var content = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing pipelines for tenant {TenantId}", tenantId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to list pipelines" });
        }
    }

    /// <summary>
    /// Update pipeline
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdatePipeline(
        Guid tenantId,
        Guid id,
        [FromBody] object request)
    {
        try
        {
            if (!ValidateTenantId(tenantId, out var jwtTenantId))
                return string.IsNullOrEmpty(jwtTenantId) ? Unauthorized(new { error = "Tenant ID not found in token" }) : Forbid();

            var client = _httpClientFactory.CreateClient("PipelineService");
            var httpRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/tenants/{tenantId}/pipelines/{id}") { Content = JsonContent.Create(request) };
            httpRequest.Headers.Add("Authorization", Request.Headers["Authorization"].ToString());
            httpRequest.Headers.Add("X-Tenant-Id", jwtTenantId);

            var response = await client.SendAsync(httpRequest);
            var content = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating pipeline {PipelineId} for tenant {TenantId}", id, tenantId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to update pipeline" });
        }
    }

    /// <summary>
    /// Delete pipeline
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePipeline(Guid tenantId, Guid id)
    {
        try
        {
            if (!ValidateTenantId(tenantId, out var jwtTenantId))
                return string.IsNullOrEmpty(jwtTenantId) ? Unauthorized(new { error = "Tenant ID not found in token" }) : Forbid();

            var client = _httpClientFactory.CreateClient("PipelineService");
            var httpRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/tenants/{tenantId}/pipelines/{id}");
            httpRequest.Headers.Add("Authorization", Request.Headers["Authorization"].ToString());
            httpRequest.Headers.Add("X-Tenant-Id", jwtTenantId);

            var response = await client.SendAsync(httpRequest);
            if (response.IsSuccessStatusCode) return NoContent();

            var content = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting pipeline {PipelineId} for tenant {TenantId}", id, tenantId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to delete pipeline" });
        }
    }

    /// <summary>
    /// Execute pipeline
    /// </summary>
    [HttpPost("{id}/execute")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExecutePipeline(
        Guid tenantId,
        Guid id,
        [FromBody] object? request = null)
    {
        try
        {
            if (!ValidateTenantId(tenantId, out var jwtTenantId))
                return string.IsNullOrEmpty(jwtTenantId) ? Unauthorized(new { error = "Tenant ID not found in token" }) : Forbid();

            var client = _httpClientFactory.CreateClient("PipelineService");
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/tenants/{tenantId}/pipelines/{id}/execute") { Content = JsonContent.Create(request ?? new { }) };
            httpRequest.Headers.Add("Authorization", Request.Headers["Authorization"].ToString());
            httpRequest.Headers.Add("X-Tenant-Id", jwtTenantId);

            var response = await client.SendAsync(httpRequest);
            var content = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing pipeline {PipelineId} for tenant {TenantId}", id, tenantId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to execute pipeline" });
        }
    }

    /// <summary>
    /// Get execution history
    /// </summary>
    [HttpGet("{id}/executions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExecutions(
        Guid tenantId,
        Guid id,
        [FromQuery] int limit = 20)
    {
        try
        {
            if (!ValidateTenantId(tenantId, out var jwtTenantId))
                return string.IsNullOrEmpty(jwtTenantId) ? Unauthorized(new { error = "Tenant ID not found in token" }) : Forbid();

            var client = _httpClientFactory.CreateClient("PipelineService");
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/tenants/{tenantId}/pipelines/{id}/executions?limit={limit}");
            httpRequest.Headers.Add("Authorization", Request.Headers["Authorization"].ToString());
            httpRequest.Headers.Add("X-Tenant-Id", jwtTenantId);

            var response = await client.SendAsync(httpRequest);
            var content = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting executions for pipeline {PipelineId}, tenant {TenantId}", id, tenantId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to retrieve execution history" });
        }
    }

    /// <summary>
    /// Get specific execution
    /// </summary>
    [HttpGet("{id}/executions/{executionId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExecution(
        Guid tenantId,
        Guid id,
        Guid executionId)
    {
        try
        {
            if (!ValidateTenantId(tenantId, out var jwtTenantId))
                return string.IsNullOrEmpty(jwtTenantId) ? Unauthorized(new { error = "Tenant ID not found in token" }) : Forbid();

            var client = _httpClientFactory.CreateClient("PipelineService");
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/tenants/{tenantId}/pipelines/{id}/executions/{executionId}");
            httpRequest.Headers.Add("Authorization", Request.Headers["Authorization"].ToString());
            httpRequest.Headers.Add("X-Tenant-Id", jwtTenantId);

            var response = await client.SendAsync(httpRequest);
            var content = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting execution {ExecutionId} for pipeline {PipelineId}, tenant {TenantId}",
                executionId, id, tenantId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to retrieve execution" });
        }
    }

    /// <summary>
    /// Create or update pipeline schedule
    /// </summary>
    [HttpPut("{id}/schedule")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSchedule(
        Guid tenantId,
        Guid id,
        [FromBody] object request)
    {
        try
        {
            if (!ValidateTenantId(tenantId, out var jwtTenantId))
                return string.IsNullOrEmpty(jwtTenantId) ? Unauthorized(new { error = "Tenant ID not found in token" }) : Forbid();

            var client = _httpClientFactory.CreateClient("PipelineService");
            var httpRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/tenants/{tenantId}/pipelines/{id}/schedule") { Content = JsonContent.Create(request) };
            httpRequest.Headers.Add("Authorization", Request.Headers["Authorization"].ToString());
            httpRequest.Headers.Add("X-Tenant-Id", jwtTenantId);

            var response = await client.SendAsync(httpRequest);
            var content = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating schedule for pipeline {PipelineId}, tenant {TenantId}", id, tenantId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to update schedule" });
        }
    }

    /// <summary>
    /// Get pipeline schedule
    /// </summary>
    [HttpGet("{id}/schedule")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSchedule(Guid tenantId, Guid id)
    {
        try
        {
            if (!ValidateTenantId(tenantId, out var jwtTenantId))
                return string.IsNullOrEmpty(jwtTenantId) ? Unauthorized(new { error = "Tenant ID not found in token" }) : Forbid();

            var client = _httpClientFactory.CreateClient("PipelineService");
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/tenants/{tenantId}/pipelines/{id}/schedule");
            httpRequest.Headers.Add("Authorization", Request.Headers["Authorization"].ToString());
            httpRequest.Headers.Add("X-Tenant-Id", jwtTenantId);

            var response = await client.SendAsync(httpRequest);
            var content = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting schedule for pipeline {PipelineId}, tenant {TenantId}", id, tenantId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to retrieve schedule" });
        }
    }

    /// <summary>
    /// Delete pipeline schedule
    /// </summary>
    [HttpDelete("{id}/schedule")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSchedule(Guid tenantId, Guid id)
    {
        try
        {
            if (!ValidateTenantId(tenantId, out var jwtTenantId))
                return string.IsNullOrEmpty(jwtTenantId) ? Unauthorized(new { error = "Tenant ID not found in token" }) : Forbid();

            var client = _httpClientFactory.CreateClient("PipelineService");
            var httpRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/tenants/{tenantId}/pipelines/{id}/schedule");
            httpRequest.Headers.Add("Authorization", Request.Headers["Authorization"].ToString());
            httpRequest.Headers.Add("X-Tenant-Id", jwtTenantId);

            var response = await client.SendAsync(httpRequest);
            if (response.IsSuccessStatusCode) return NoContent();

            var content = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting schedule for pipeline {PipelineId}, tenant {TenantId}", id, tenantId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to delete schedule" });
        }
    }
}
