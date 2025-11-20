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
/// Proxy controller for Context Service
/// Routes requests to the Context microservice
/// </summary>
[Authorize]
[ApiController]
[Route("api/context")]
public class ContextProxyController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ContextProxyController> _logger;

    public ContextProxyController(
        IHttpClientFactory httpClientFactory,
        ILogger<ContextProxyController> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Extract and validate tenant ID from JWT claims
    /// </summary>
    private string? GetTenantIdFromJwt()
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(tenantId))
        {
            _logger.LogWarning("JWT token missing tenant_id claim");
            return null;
        }

        return tenantId;
    }

    /// <summary>
    /// Create embedding for an entity
    /// </summary>
    [HttpPost("embeddings")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateEmbedding([FromBody] object request)
    {
        try
        {
            // Extract tenant ID from JWT
            var tenantId = GetTenantIdFromJwt();
            if (tenantId == null)
            {
                return Unauthorized(new { error = "Tenant ID not found in token" });
            }

            var client = _httpClientFactory.CreateClient("ContextService");

            // Create request with tenant header
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/context/embeddings")
            {
                Content = JsonContent.Create(request)
            };
            httpRequest.Headers.Add("Authorization", Request.Headers["Authorization"].ToString());
            httpRequest.Headers.Add("X-Tenant-Id", tenantId);

            var response = await client.SendAsync(httpRequest);
            var content = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating embedding");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to create embedding" });
        }
    }

    /// <summary>
    /// Create embeddings in batch
    /// </summary>
    [HttpPost("embeddings/batch")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateBatchEmbeddings([FromBody] object request)
    {
        try
        {
            var tenantId = GetTenantIdFromJwt();
            if (tenantId == null) return Unauthorized(new { error = "Tenant ID not found in token" });

            var client = _httpClientFactory.CreateClient("ContextService");
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/context/embeddings/batch") { Content = JsonContent.Create(request) };
            httpRequest.Headers.Add("Authorization", Request.Headers["Authorization"].ToString());
            httpRequest.Headers.Add("X-Tenant-Id", tenantId);

            var response = await client.SendAsync(httpRequest);
            var content = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating batch embeddings");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to create batch embeddings" });
        }
    }

    /// <summary>
    /// Enrich an entity with contextual information
    /// </summary>
    [HttpPost("enrich")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> EnrichEntity([FromBody] object request)
    {
        try
        {
            var tenantId = GetTenantIdFromJwt();
            if (tenantId == null) return Unauthorized(new { error = "Tenant ID not found in token" });

            var client = _httpClientFactory.CreateClient("ContextService");
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/context/enrich") { Content = JsonContent.Create(request) };
            httpRequest.Headers.Add("Authorization", Request.Headers["Authorization"].ToString());
            httpRequest.Headers.Add("X-Tenant-Id", tenantId);

            var response = await client.SendAsync(httpRequest);
            var content = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enriching entity");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to enrich entity" });
        }
    }

    /// <summary>
    /// Search for similar entities
    /// </summary>
    [HttpPost("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchEntities([FromBody] object request)
    {
        try
        {
            var tenantId = GetTenantIdFromJwt();
            if (tenantId == null) return Unauthorized(new { error = "Tenant ID not found in token" });

            var client = _httpClientFactory.CreateClient("ContextService");
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/context/search") { Content = JsonContent.Create(request) };
            httpRequest.Headers.Add("Authorization", Request.Headers["Authorization"].ToString());
            httpRequest.Headers.Add("X-Tenant-Id", tenantId);

            var response = await client.SendAsync(httpRequest);
            var content = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching entities");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to search entities" });
        }
    }

    /// <summary>
    /// Delete embedding for an entity
    /// </summary>
    [HttpDelete("embeddings/{entityId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEmbedding(string entityId)
    {
        try
        {
            var tenantId = GetTenantIdFromJwt();
            if (tenantId == null) return Unauthorized(new { error = "Tenant ID not found in token" });

            var client = _httpClientFactory.CreateClient("ContextService");
            var httpRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/context/embeddings/{entityId}");
            httpRequest.Headers.Add("Authorization", Request.Headers["Authorization"].ToString());
            httpRequest.Headers.Add("X-Tenant-Id", tenantId);

            var response = await client.SendAsync(httpRequest);
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent) return NoContent();

            var content = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting embedding for entity {EntityId}", entityId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to delete embedding" });
        }
    }

    /// <summary>
    /// Get embedding metadata for an entity
    /// </summary>
    [HttpGet("embeddings/{entityId}/metadata")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEmbeddingMetadata(string entityId)
    {
        try
        {
            var tenantId = GetTenantIdFromJwt();
            if (tenantId == null) return Unauthorized(new { error = "Tenant ID not found in token" });

            var client = _httpClientFactory.CreateClient("ContextService");
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/context/embeddings/{entityId}/metadata");
            httpRequest.Headers.Add("Authorization", Request.Headers["Authorization"].ToString());
            httpRequest.Headers.Add("X-Tenant-Id", tenantId);

            var response = await client.SendAsync(httpRequest);
            var content = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting embedding metadata for entity {EntityId}", entityId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to retrieve embedding metadata" });
        }
    }

    /// <summary>
    /// Get enrichment statistics
    /// </summary>
    [HttpGet("statistics/enrichment")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEnrichmentStatistics([FromQuery] string? tenantId = null)
    {
        try
        {
            var jwtTenantId = GetTenantIdFromJwt();
            if (jwtTenantId == null) return Unauthorized(new { error = "Tenant ID not found in token" });

            var client = _httpClientFactory.CreateClient("ContextService");
            var url = "/api/context/statistics/enrichment";
            if (!string.IsNullOrEmpty(tenantId)) url += $"?tenantId={tenantId}";

            var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
            httpRequest.Headers.Add("Authorization", Request.Headers["Authorization"].ToString());
            httpRequest.Headers.Add("X-Tenant-Id", jwtTenantId);

            var response = await client.SendAsync(httpRequest);
            var content = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting enrichment statistics");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to retrieve statistics" });
        }
    }

    /// <summary>
    /// Health check for Context service
    /// </summary>
    [AllowAnonymous]
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Health()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("ContextService");
            var response = await client.GetAsync("/api/context/health");

            var content = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Context service health check failed");
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "Context service is unavailable" });
        }
    }
}
