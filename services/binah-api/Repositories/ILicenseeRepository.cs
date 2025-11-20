using Binah.API.Models;

namespace Binah.API.Repositories;

/// <summary>
/// Repository interface for licensee data access
/// </summary>
public interface ILicenseeRepository
{
    /// <summary>
    /// Get a licensee by ID
    /// </summary>
    Task<Licensee?> GetByIdAsync(Guid id);

    /// <summary>
    /// Get a licensee by license key hash
    /// </summary>
    Task<Licensee?> GetByLicenseKeyHashAsync(string licenseKeyHash);

    /// <summary>
    /// Get all licensees
    /// </summary>
    Task<List<Licensee>> GetAllAsync();

    /// <summary>
    /// Get licensees by status
    /// </summary>
    Task<List<Licensee>> GetByStatusAsync(LicenseeStatus status);

    /// <summary>
    /// Create a new licensee
    /// </summary>
    Task<Licensee> CreateAsync(Licensee licensee);

    /// <summary>
    /// Update an existing licensee
    /// </summary>
    Task<Licensee> UpdateAsync(Licensee licensee);

    /// <summary>
    /// Delete a licensee
    /// </summary>
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// Check if a licensee exists
    /// </summary>
    Task<bool> ExistsAsync(Guid id);
}
