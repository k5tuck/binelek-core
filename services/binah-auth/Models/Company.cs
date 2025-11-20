using System;
using System.Collections.Generic;

namespace Binah.Auth.Models;

/// <summary>
/// Company entity for multi-tenancy
/// </summary>
public class Company
{
    /// <summary>
    /// Unique company identifier
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Company name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Company description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Company email
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Email domain for this company (e.g., "acme.com")
    /// Used for automatic tenant assignment
    /// </summary>
    public string? EmailDomain { get; set; }

    /// <summary>
    /// Company phone
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Company address
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Company city
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// Company state/province
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// Company country
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// Company postal code
    /// </summary>
    public string? PostalCode { get; set; }

    /// <summary>
    /// Whether the company is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Company creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// User who created the company
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Users belonging to this company
    /// </summary>
    public List<User> Users { get; set; } = new();
}

/// <summary>
/// DTO for Company
/// </summary>
public class CompanyDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int UserCount { get; set; }
}

/// <summary>
/// Request to create a company
/// </summary>
public class CreateCompanyRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
}

/// <summary>
/// Request to update a company
/// </summary>
public class UpdateCompanyRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    public bool? IsActive { get; set; }
}
