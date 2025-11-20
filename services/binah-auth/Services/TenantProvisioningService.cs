using Binah.Auth.Models;
using Binah.Auth.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Binah.Auth.Services;

/// <summary>
/// Service for automatic tenant creation and management
/// </summary>
public interface ITenantProvisioningService
{
    Task<(string TenantId, string Role)> ProvisionTenantForUserAsync(string email, string? companyName = null);
}

public class TenantProvisioningService : ITenantProvisioningService
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<TenantProvisioningService> _logger;

    public TenantProvisioningService(
        ICompanyRepository companyRepository,
        IUserRepository userRepository,
        ILogger<TenantProvisioningService> logger)
    {
        _companyRepository = companyRepository ?? throw new ArgumentNullException(nameof(companyRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Provisions a tenant for a new user based on their email domain
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <param name="companyName">Optional company name (if not provided, derived from email domain)</param>
    /// <returns>Tuple of (TenantId, Role) where Role is either Admin (first user) or User</returns>
    public async Task<(string TenantId, string Role)> ProvisionTenantForUserAsync(string email, string? companyName = null)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be null or empty", nameof(email));
        }

        _logger.LogInformation("Provisioning tenant for user with email {Email}", email);

        // Extract domain from email
        var emailDomain = GetEmailDomain(email);
        _logger.LogDebug("Extracted domain: {Domain}", emailDomain);

        // Check if a company already exists for this domain
        var existingCompany = await _companyRepository.GetByEmailDomainAsync(emailDomain);

        if (existingCompany != null)
        {
            _logger.LogInformation("Found existing company {CompanyId} for domain {Domain}",
                existingCompany.Id, emailDomain);

            // Check if there are existing users in this company
            var existingUsers = await _userRepository.GetByTenantIdAsync(existingCompany.Id);
            var hasAdmins = existingUsers.Any(u => u.Roles.Contains(Roles.Admin));

            // If no admins exist, make this user an admin, otherwise regular user
            var role = hasAdmins ? Roles.User : Roles.Admin;

            _logger.LogInformation("Assigning role {Role} to user in existing company", role);
            return (existingCompany.Id, role);
        }

        // No existing company - create a new one
        _logger.LogInformation("Creating new company for domain {Domain}", emailDomain);

        var newCompany = new Company
        {
            Name = companyName ?? GenerateCompanyNameFromDomain(emailDomain),
            Email = email,
            EmailDomain = emailDomain,
            Description = $"Auto-created tenant for {emailDomain}",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        newCompany = await _companyRepository.CreateAsync(newCompany);

        _logger.LogInformation("Created new company {CompanyId} with name {CompanyName}",
            newCompany.Id, newCompany.Name);

        // First user in a new company is always Admin
        return (newCompany.Id, Roles.Admin);
    }

    /// <summary>
    /// Extracts the domain from an email address
    /// </summary>
    private string GetEmailDomain(string email)
    {
        var parts = email.Split('@');
        if (parts.Length != 2)
        {
            throw new ArgumentException($"Invalid email format: {email}");
        }

        return parts[1].ToLowerInvariant();
    }

    /// <summary>
    /// Generates a company name from an email domain
    /// Example: acme.com -> Acme
    ///          example.co.uk -> Example
    /// </summary>
    private string GenerateCompanyNameFromDomain(string domain)
    {
        // Remove common TLDs and get the company part
        var domainParts = domain.Split('.');
        var companyPart = domainParts[0];

        // Capitalize first letter
        return char.ToUpperInvariant(companyPart[0]) + companyPart.Substring(1);
    }
}
