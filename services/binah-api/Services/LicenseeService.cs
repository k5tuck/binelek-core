using Binah.API.Exceptions;
using Binah.API.Models;
using Binah.API.Repositories;
using System.Security.Cryptography;
using System.Text;

namespace Binah.API.Services;

/// <summary>
/// Implementation of licensee service
/// </summary>
public class LicenseeService : ILicenseeService
{
    private readonly ILicenseeRepository _repository;
    private readonly ILogger<LicenseeService> _logger;
    private readonly string _hmacSecret;

    public LicenseeService(
        ILicenseeRepository repository,
        ILogger<LicenseeService> logger,
        IConfiguration configuration)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Get HMAC secret from configuration (should be stored securely)
        _hmacSecret = configuration["License:HmacSecret"]
            ?? throw new InvalidOperationException("License:HmacSecret not configured");
    }

    public async Task<Licensee?> GetByIdAsync(Guid id)
    {
        _logger.LogDebug("Getting licensee by ID: {LicenseeId}", id);
        return await _repository.GetByIdAsync(id);
    }

    public async Task<Licensee?> ValidateLicenseKeyAsync(string licenseKey)
    {
        if (string.IsNullOrWhiteSpace(licenseKey))
        {
            _logger.LogWarning("Empty license key provided");
            return null;
        }

        try
        {
            // Hash the provided license key
            var keyHash = HashLicenseKey(licenseKey);

            // Find licensee by key hash
            var licensee = await _repository.GetByLicenseKeyHashAsync(keyHash);

            if (licensee == null)
            {
                _logger.LogWarning("No licensee found for provided license key");
                return null;
            }

            // Validate licensee status
            if (!licensee.IsValid())
            {
                _logger.LogWarning("Licensee {LicenseeId} has invalid status: {Status}, Expired: {IsExpired}",
                    licensee.Id, licensee.Status, licensee.IsExpired());
                return null;
            }

            _logger.LogInformation("Successfully validated license key for licensee: {LicenseeName} ({LicenseeId})",
                licensee.Name, licensee.Id);

            return licensee;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating license key");
            return null;
        }
    }

    public async Task<List<Licensee>> GetAllAsync()
    {
        _logger.LogDebug("Getting all licensees");
        return await _repository.GetAllAsync();
    }

    public async Task<Licensee> CreateAsync(Licensee licensee)
    {
        if (licensee == null)
        {
            throw new ArgumentNullException(nameof(licensee));
        }

        _logger.LogInformation("Creating new licensee: {LicenseeName}", licensee.Name);

        // Set defaults
        if (licensee.Id == Guid.Empty)
        {
            licensee.Id = Guid.NewGuid();
        }

        licensee.CreatedAt = DateTime.UtcNow;

        // Hash the license key if not already hashed
        if (!IsHashed(licensee.LicenseKeyHash))
        {
            _logger.LogDebug("Hashing license key for licensee {LicenseeId}", licensee.Id);
            licensee.LicenseKeyHash = HashLicenseKey(licensee.LicenseKeyHash);
        }

        var created = await _repository.CreateAsync(licensee);

        _logger.LogInformation("Created licensee: {LicenseeName} ({LicenseeId})", created.Name, created.Id);

        return created;
    }

    public async Task<Licensee> UpdateAsync(Licensee licensee)
    {
        if (licensee == null)
        {
            throw new ArgumentNullException(nameof(licensee));
        }

        _logger.LogInformation("Updating licensee: {LicenseeId}", licensee.Id);

        // Check if licensee exists
        var existing = await _repository.GetByIdAsync(licensee.Id);
        if (existing == null)
        {
            throw new LicenseeNotFoundException(licensee.Id);
        }

        var updated = await _repository.UpdateAsync(licensee);

        _logger.LogInformation("Updated licensee: {LicenseeId}", updated.Id);

        return updated;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        _logger.LogInformation("Deleting licensee: {LicenseeId}", id);

        var result = await _repository.DeleteAsync(id);

        if (result)
        {
            _logger.LogInformation("Deleted licensee: {LicenseeId}", id);
        }
        else
        {
            _logger.LogWarning("Failed to delete licensee: {LicenseeId}", id);
        }

        return result;
    }

    public async Task<bool> SuspendAsync(Guid id)
    {
        _logger.LogInformation("Suspending licensee: {LicenseeId}", id);

        var licensee = await _repository.GetByIdAsync(id);
        if (licensee == null)
        {
            throw new LicenseeNotFoundException(id);
        }

        licensee.Status = LicenseeStatus.Suspended;
        await _repository.UpdateAsync(licensee);

        _logger.LogInformation("Suspended licensee: {LicenseeId}", id);

        return true;
    }

    public async Task<bool> ReactivateAsync(Guid id)
    {
        _logger.LogInformation("Reactivating licensee: {LicenseeId}", id);

        var licensee = await _repository.GetByIdAsync(id);
        if (licensee == null)
        {
            throw new LicenseeNotFoundException(id);
        }

        licensee.Status = LicenseeStatus.Active;
        await _repository.UpdateAsync(licensee);

        _logger.LogInformation("Reactivated licensee: {LicenseeId}", id);

        return true;
    }

    public async Task<bool> IsFeatureEnabledAsync(Guid licenseeId, string featureName)
    {
        var licensee = await _repository.GetByIdAsync(licenseeId);
        if (licensee == null)
        {
            return false;
        }

        return licensee.HasFeature(featureName);
    }

    public async Task<bool> IsDomainAllowedAsync(Guid licenseeId, string domainId)
    {
        var licensee = await _repository.GetByIdAsync(licenseeId);
        if (licensee == null)
        {
            return false;
        }

        return licensee.Features.IsDomainAllowed(domainId);
    }

    /// <summary>
    /// Hash a license key using HMAC-SHA256
    /// </summary>
    private string HashLicenseKey(string licenseKey)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_hmacSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(licenseKey));
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Check if a string is already a hashed value (base64 format)
    /// </summary>
    private bool IsHashed(string value)
    {
        // HMAC-SHA256 produces 32 bytes = 44 characters in base64
        if (string.IsNullOrEmpty(value) || value.Length != 44)
        {
            return false;
        }

        try
        {
            Convert.FromBase64String(value);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
