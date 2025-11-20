using Binah.API.Models;

namespace Binah.API.Services;

/// <summary>
/// Service for managing licensees and license validation
/// </summary>
public interface ILicenseeService
{
    /// <summary>
    /// Get a licensee by ID
    /// </summary>
    Task<Licensee?> GetByIdAsync(Guid id);

    /// <summary>
    /// Get a licensee by license key (validates the key)
    /// </summary>
    Task<Licensee?> ValidateLicenseKeyAsync(string licenseKey);

    /// <summary>
    /// Get all licensees
    /// </summary>
    Task<List<Licensee>> GetAllAsync();

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
    /// Suspend a licensee
    /// </summary>
    Task<bool> SuspendAsync(Guid id);

    /// <summary>
    /// Reactivate a suspended licensee
    /// </summary>
    Task<bool> ReactivateAsync(Guid id);

    /// <summary>
    /// Check if a feature is enabled for a licensee
    /// </summary>
    Task<bool> IsFeatureEnabledAsync(Guid licenseeId, string featureName);

    /// <summary>
    /// Check if a domain is allowed for a licensee
    /// </summary>
    Task<bool> IsDomainAllowedAsync(Guid licenseeId, string domainId);
}
