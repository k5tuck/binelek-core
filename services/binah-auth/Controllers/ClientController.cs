using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Binah.Auth.Models;

namespace Binah.Auth.Controllers;

/// <summary>
/// Controller for managing clients within a tenant
/// Uses actual database queries via AuthDbContext
/// </summary>
[ApiController]
[Route("api/clients")]
[Authorize]
public class ClientController : ControllerBase
{
    private readonly AuthDbContext _dbContext;
    private readonly ILogger<ClientController> _logger;

    public ClientController(AuthDbContext dbContext, ILogger<ClientController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    private string? GetTenantId() => User.FindFirst("tenant_id")?.Value;
    private string? GetUserId() => User.FindFirst("sub")?.Value;

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

        var query = _dbContext.Clients
            .Where(c => c.TenantId == tenantId);

        // Apply status filter
        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(c => c.Status == status);
        }

        // Apply search filter
        if (!string.IsNullOrEmpty(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(c =>
                c.FirstName.ToLower().Contains(searchLower) ||
                c.LastName.ToLower().Contains(searchLower) ||
                c.Email.ToLower().Contains(searchLower) ||
                (c.Company != null && c.Company.ToLower().Contains(searchLower)));
        }

        var total = await query.CountAsync();

        var clients = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip(skip)
            .Take(limit)
            .Select(c => new ClientResponse
            {
                Id = c.Id,
                FirstName = c.FirstName,
                LastName = c.LastName,
                Email = c.Email,
                Phone = c.Phone,
                Company = c.Company,
                Address = c.Address,
                Notes = c.Notes,
                Status = c.Status,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .ToListAsync();

        return Ok(new ClientListResponse
        {
            Clients = clients,
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

        var client = await _dbContext.Clients
            .Where(c => c.TenantId == tenantId && c.Id == id)
            .Select(c => new ClientResponse
            {
                Id = c.Id,
                FirstName = c.FirstName,
                LastName = c.LastName,
                Email = c.Email,
                Phone = c.Phone,
                Company = c.Company,
                Address = c.Address,
                Notes = c.Notes,
                Status = c.Status,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (client == null)
        {
            return NotFound(new { error = $"Client {id} not found" });
        }

        return Ok(client);
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

        // Validate required fields
        if (string.IsNullOrEmpty(request.FirstName) || string.IsNullOrEmpty(request.LastName))
        {
            return BadRequest(new { error = "First name and last name are required" });
        }
        if (string.IsNullOrEmpty(request.Email))
        {
            return BadRequest(new { error = "Email is required" });
        }

        // Check for duplicate email within tenant
        var existingClient = await _dbContext.Clients
            .Where(c => c.TenantId == tenantId && c.Email == request.Email)
            .FirstOrDefaultAsync();

        if (existingClient != null)
        {
            return Conflict(new { error = "A client with this email already exists" });
        }

        var client = new Client
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            Company = request.Company,
            Address = request.Address,
            Notes = request.Notes,
            Status = "active",
            CreatedBy = GetUserId(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Clients.Add(client);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Created client {ClientId} for tenant {TenantId}", client.Id, tenantId);

        var response = new ClientResponse
        {
            Id = client.Id,
            FirstName = client.FirstName,
            LastName = client.LastName,
            Email = client.Email,
            Phone = client.Phone,
            Company = client.Company,
            Address = client.Address,
            Notes = client.Notes,
            Status = client.Status,
            CreatedAt = client.CreatedAt,
            UpdatedAt = client.UpdatedAt
        };

        return CreatedAtAction(nameof(GetClient), new { id = client.Id }, response);
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

        var client = await _dbContext.Clients
            .Where(c => c.TenantId == tenantId && c.Id == id)
            .FirstOrDefaultAsync();

        if (client == null)
        {
            return NotFound(new { error = $"Client {id} not found" });
        }

        // Update fields if provided
        if (!string.IsNullOrEmpty(request.FirstName)) client.FirstName = request.FirstName;
        if (!string.IsNullOrEmpty(request.LastName)) client.LastName = request.LastName;
        if (!string.IsNullOrEmpty(request.Email)) client.Email = request.Email;
        if (request.Phone != null) client.Phone = request.Phone;
        if (request.Company != null) client.Company = request.Company;
        if (request.Address != null) client.Address = request.Address;
        if (request.Notes != null) client.Notes = request.Notes;
        if (!string.IsNullOrEmpty(request.Status)) client.Status = request.Status;

        client.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Updated client {ClientId} for tenant {TenantId}", id, tenantId);

        return Ok(new ClientResponse
        {
            Id = client.Id,
            FirstName = client.FirstName,
            LastName = client.LastName,
            Email = client.Email,
            Phone = client.Phone,
            Company = client.Company,
            Address = client.Address,
            Notes = client.Notes,
            Status = client.Status,
            CreatedAt = client.CreatedAt,
            UpdatedAt = client.UpdatedAt
        });
    }

    /// <summary>
    /// Delete client (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteClient(string id)
    {
        var tenantId = GetTenantId();
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized(new { error = "Tenant ID not found in token" });
        }

        var client = await _dbContext.Clients
            .IgnoreQueryFilters() // Include soft-deleted
            .Where(c => c.TenantId == tenantId && c.Id == id)
            .FirstOrDefaultAsync();

        if (client == null)
        {
            return NotFound(new { error = $"Client {id} not found" });
        }

        // Soft delete
        client.DeletedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Deleted client {ClientId} for tenant {TenantId}", id, tenantId);

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

        var clients = _dbContext.Clients.Where(c => c.TenantId == tenantId);

        var totalClients = await clients.CountAsync();
        var activeClients = await clients.CountAsync(c => c.Status == "active");
        var inactiveClients = await clients.CountAsync(c => c.Status == "inactive");

        var firstOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var newClientsThisMonth = await clients.CountAsync(c => c.CreatedAt >= firstOfMonth);

        // Get growth data for last 6 months
        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
        var monthlyGrowth = await clients
            .Where(c => c.CreatedAt >= sixMonthsAgo)
            .GroupBy(c => new { c.CreatedAt.Year, c.CreatedAt.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Count = g.Count()
            })
            .OrderBy(g => g.Year)
            .ThenBy(g => g.Month)
            .ToListAsync();

        var clientGrowth = monthlyGrowth.Select(g => new ClientGrowthPoint
        {
            Month = new DateTime(g.Year, g.Month, 1).ToString("MMM"),
            Count = g.Count
        }).ToList();

        return Ok(new ClientStatsResponse
        {
            TotalClients = totalClients,
            ActiveClients = activeClients,
            InactiveClients = inactiveClients,
            NewClientsThisMonth = newClientsThisMonth,
            ClientsByStatus = new Dictionary<string, int>
            {
                { "active", activeClients },
                { "inactive", inactiveClients }
            },
            ClientGrowth = clientGrowth
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
