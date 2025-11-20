using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Binah.Api.Controllers;

/// <summary>
/// Controller for managing clients within a tenant
/// </summary>
[ApiController]
[Route("api/clients")]
[Authorize]
public class ClientController : ControllerBase
{
    private readonly ILogger<ClientController> _logger;

    public ClientController(ILogger<ClientController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get tenant ID from JWT claim
    /// </summary>
    private string? GetTenantId()
    {
        return User.FindFirst("tenant_id")?.Value;
    }

    /// <summary>
    /// List all clients for current tenant
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ClientListResponse>> ListClients(
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 50,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null)
    {
        var tenantId = GetTenantId();
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized(new { error = "Tenant ID not found in token" });
        }

        _logger.LogInformation("Listing clients for tenant {TenantId}", tenantId);

        // TODO: Replace with actual database query
        // Mock data for now
        var clients = new List<ClientResponse>
        {
            new()
            {
                Id = "client-001",
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                Phone = "+1-555-0101",
                Company = "Acme Corp",
                Status = "active",
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-5)
            },
            new()
            {
                Id = "client-002",
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@example.com",
                Phone = "+1-555-0102",
                Company = "Tech Solutions",
                Status = "active",
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                UpdatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new()
            {
                Id = "client-003",
                FirstName = "Bob",
                LastName = "Johnson",
                Email = "bob.johnson@example.com",
                Phone = "+1-555-0103",
                Company = "Global Industries",
                Status = "inactive",
                CreatedAt = DateTime.UtcNow.AddDays(-60),
                UpdatedAt = DateTime.UtcNow.AddDays(-30)
            }
        };

        // Apply filters
        var filtered = clients.AsEnumerable();
        if (!string.IsNullOrEmpty(status))
        {
            filtered = filtered.Where(c => c.Status == status);
        }
        if (!string.IsNullOrEmpty(search))
        {
            var searchLower = search.ToLower();
            filtered = filtered.Where(c =>
                c.FirstName.ToLower().Contains(searchLower) ||
                c.LastName.ToLower().Contains(searchLower) ||
                c.Email.ToLower().Contains(searchLower) ||
                (c.Company?.ToLower().Contains(searchLower) ?? false));
        }

        var filteredList = filtered.ToList();
        var total = filteredList.Count;
        var paged = filteredList.Skip(skip).Take(limit).ToList();

        return Ok(new ClientListResponse
        {
            Clients = paged,
            Total = total,
            Skip = skip,
            Limit = limit
        });
    }

    /// <summary>
    /// Get client by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ClientResponse>> GetClient(string id)
    {
        var tenantId = GetTenantId();
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized(new { error = "Tenant ID not found in token" });
        }

        _logger.LogInformation("Getting client {ClientId} for tenant {TenantId}", id, tenantId);

        // TODO: Replace with actual database query
        // Mock data
        if (id == "client-001")
        {
            return Ok(new ClientResponse
            {
                Id = "client-001",
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                Phone = "+1-555-0101",
                Company = "Acme Corp",
                Address = "123 Main St, Anytown, USA",
                Notes = "VIP customer, prefers email communication",
                Status = "active",
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-5)
            });
        }

        return NotFound(new { error = $"Client {id} not found" });
    }

    /// <summary>
    /// Create a new client
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ClientResponse>> CreateClient([FromBody] CreateClientRequest request)
    {
        var tenantId = GetTenantId();
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized(new { error = "Tenant ID not found in token" });
        }

        _logger.LogInformation("Creating client {Email} for tenant {TenantId}", request.Email, tenantId);

        // Validate required fields
        if (string.IsNullOrEmpty(request.FirstName) || string.IsNullOrEmpty(request.LastName))
        {
            return BadRequest(new { error = "First name and last name are required" });
        }
        if (string.IsNullOrEmpty(request.Email))
        {
            return BadRequest(new { error = "Email is required" });
        }

        // TODO: Replace with actual database insert
        var client = new ClientResponse
        {
            Id = $"client-{Guid.NewGuid():N}",
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            Company = request.Company,
            Address = request.Address,
            Notes = request.Notes,
            Status = "active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return CreatedAtAction(nameof(GetClient), new { id = client.Id }, client);
    }

    /// <summary>
    /// Update client
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ClientResponse>> UpdateClient(string id, [FromBody] UpdateClientRequest request)
    {
        var tenantId = GetTenantId();
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized(new { error = "Tenant ID not found in token" });
        }

        _logger.LogInformation("Updating client {ClientId} for tenant {TenantId}", id, tenantId);

        // TODO: Replace with actual database update
        var client = new ClientResponse
        {
            Id = id,
            FirstName = request.FirstName ?? "John",
            LastName = request.LastName ?? "Doe",
            Email = request.Email ?? "john.doe@example.com",
            Phone = request.Phone,
            Company = request.Company,
            Address = request.Address,
            Notes = request.Notes,
            Status = request.Status ?? "active",
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow
        };

        return Ok(client);
    }

    /// <summary>
    /// Delete client
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteClient(string id)
    {
        var tenantId = GetTenantId();
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized(new { error = "Tenant ID not found in token" });
        }

        _logger.LogInformation("Deleting client {ClientId} for tenant {TenantId}", id, tenantId);

        // TODO: Replace with actual database delete
        return NoContent();
    }

    /// <summary>
    /// Get client statistics/dashboard data
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<ClientStatsResponse>> GetClientStats()
    {
        var tenantId = GetTenantId();
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized(new { error = "Tenant ID not found in token" });
        }

        _logger.LogInformation("Getting client stats for tenant {TenantId}", tenantId);

        // TODO: Replace with actual database aggregation
        return Ok(new ClientStatsResponse
        {
            TotalClients = 156,
            ActiveClients = 142,
            InactiveClients = 14,
            NewClientsThisMonth = 12,
            ClientsByStatus = new Dictionary<string, int>
            {
                { "active", 142 },
                { "inactive", 14 }
            },
            ClientGrowth = new List<ClientGrowthPoint>
            {
                new() { Month = "Jan", Count = 120 },
                new() { Month = "Feb", Count = 128 },
                new() { Month = "Mar", Count = 135 },
                new() { Month = "Apr", Count = 142 },
                new() { Month = "May", Count = 148 },
                new() { Month = "Jun", Count = 156 }
            }
        });
    }
}

#region DTOs

public class ClientResponse
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = "active";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ClientListResponse
{
    public List<ClientResponse> Clients { get; set; } = new();
    public int Total { get; set; }
    public int Skip { get; set; }
    public int Limit { get; set; }
}

public class CreateClientRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
}

public class UpdateClientRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public string? Status { get; set; }
}

public class ClientStatsResponse
{
    public int TotalClients { get; set; }
    public int ActiveClients { get; set; }
    public int InactiveClients { get; set; }
    public int NewClientsThisMonth { get; set; }
    public Dictionary<string, int> ClientsByStatus { get; set; } = new();
    public List<ClientGrowthPoint> ClientGrowth { get; set; } = new();
}

public class ClientGrowthPoint
{
    public string Month { get; set; } = string.Empty;
    public int Count { get; set; }
}

#endregion
