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
/// Proxy controller for Ontology Service
/// Routes requests to the Ontology microservice
/// </summary>
[Authorize]
[ApiController]
[Route("api/ontology")]
public class OntologyProxyController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OntologyProxyController> _logger;

    public OntologyProxyController(
        IHttpClientFactory httpClientFactory,
        ILogger<OntologyProxyController> logger)
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
    /// Get all entities of a specific type
    /// </summary>
    [HttpGet("entities/{type}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetEntities(
        string type,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        try
        {
            // Extract tenant ID from JWT
            var tenantId = GetTenantIdFromJwt();
            if (tenantId == null)
            {
                return Unauthorized(new { error = "Tenant ID not found in token" });
            }

            var client = _httpClientFactory.CreateClient("OntologyService");

            // Create request with tenant header
            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/ontology/entities/{type}?skip={skip}&take={take}");
            request.Headers.Add("Authorization", Request.Headers["Authorization"].ToString());
            request.Headers.Add("X-Tenant-Id", tenantId);

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proxying request to Ontology service");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to communicate with Ontology service" });
        }
    }

    /// <summary>
    /// Get a specific entity by ID
    /// </summary>
    [HttpGet("entities/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEntity(string id)
    {
        try
        {
            // Extract tenant ID from JWT
            var tenantId = GetTenantIdFromJwt();
            if (tenantId == null)
            {
                return Unauthorized(new { error = "Tenant ID not found in token" });
            }

            var client = _httpClientFactory.CreateClient("OntologyService");

            // Create request with tenant header
            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/ontology/entities/{id}");
            request.Headers.Add("Authorization", Request.Headers["Authorization"].ToString());
            request.Headers.Add("X-Tenant-Id", tenantId);

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting entity {EntityId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to retrieve entity" });
        }
    }

    /// <summary>
    /// Create a new entity
    /// </summary>
    [HttpPost("entities")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateEntity([FromBody] object request)
    {
        try
        {
            // Extract tenant ID from JWT
            var tenantId = GetTenantIdFromJwt();
            if (tenantId == null)
            {
                return Unauthorized(new { error = "Tenant ID not found in token" });
            }

            var client = _httpClientFactory.CreateClient("OntologyService");

            // Create request with tenant header
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/ontology/entities")
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
            _logger.LogError(ex, "Error creating entity");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to create entity" });
        }
    }

    /// <summary>
    /// Update an entity
    /// </summary>
    [HttpPut("entities/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEntity(string id, [FromBody] object request)
    {
        try
        {
            // Extract tenant ID from JWT
            var tenantId = GetTenantIdFromJwt();
            if (tenantId == null)
            {
                return Unauthorized(new { error = "Tenant ID not found in token" });
            }

            var client = _httpClientFactory.CreateClient("OntologyService");

            // Create request with tenant header
            var httpRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/ontology/entities/{id}")
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
            _logger.LogError(ex, "Error updating entity {EntityId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to update entity" });
        }
    }

    /// <summary>
    /// Delete an entity
    /// </summary>
    [HttpDelete("entities/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEntity(string id)
    {
        try
        {
            // Extract tenant ID from JWT
            var tenantId = GetTenantIdFromJwt();
            if (tenantId == null)
            {
                return Unauthorized(new { error = "Tenant ID not found in token" });
            }

            var client = _httpClientFactory.CreateClient("OntologyService");

            // Create request with tenant header
            var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/ontology/entities/{id}");
            request.Headers.Add("Authorization", Request.Headers["Authorization"].ToString());
            request.Headers.Add("X-Tenant-Id", tenantId);

            var response = await client.SendAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                return NoContent();
            }

            var content = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting entity {EntityId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to delete entity" });
        }
    }

    /// <summary>
    /// Create a relationship between entities
    /// </summary>
    [HttpPost("relationships")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRelationship([FromBody] object request)
    {
        try
        {
            // Extract tenant ID from JWT
            var tenantId = GetTenantIdFromJwt();
            if (tenantId == null)
            {
                return Unauthorized(new { error = "Tenant ID not found in token" });
            }

            var client = _httpClientFactory.CreateClient("OntologyService");

            // Create request with tenant header
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/ontology/relationships")
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
            _logger.LogError(ex, "Error creating relationship");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to create relationship" });
        }
    }

    /// <summary>
    /// Get relationships for an entity
    /// </summary>
    [HttpGet("entities/{id}/relationships")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEntityRelationships(
        string id,
        [FromQuery] string? direction = null)
    {
        try
        {
            // Extract tenant ID from JWT
            var tenantId = GetTenantIdFromJwt();
            if (tenantId == null)
            {
                return Unauthorized(new { error = "Tenant ID not found in token" });
            }

            var client = _httpClientFactory.CreateClient("OntologyService");
            var url = $"/api/ontology/entities/{id}/relationships";
            if (!string.IsNullOrEmpty(direction))
            {
                url += $"?direction={direction}";
            }

            // Create request with tenant header
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", Request.Headers["Authorization"].ToString());
            request.Headers.Add("X-Tenant-Id", tenantId);

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting relationships for entity {EntityId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to retrieve relationships" });
        }
    }

    /// <summary>
    /// Execute a Cypher query
    /// </summary>
    [HttpPost("query")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExecuteQuery([FromBody] object request)
    {
        try
        {
            // Extract tenant ID from JWT
            var tenantId = GetTenantIdFromJwt();
            if (tenantId == null)
            {
                return Unauthorized(new { error = "Tenant ID not found in token" });
            }

            var client = _httpClientFactory.CreateClient("OntologyService");

            // Create request with tenant header
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/ontology/query")
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
            _logger.LogError(ex, "Error executing Cypher query");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to execute query" });
        }
    }
}
