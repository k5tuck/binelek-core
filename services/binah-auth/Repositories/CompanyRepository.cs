using Binah.Auth.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Binah.Auth.Repositories;

/// <summary>
/// Repository implementation for company operations
/// </summary>
public class CompanyRepository : ICompanyRepository
{
    private readonly AuthDbContext _context;
    private readonly ILogger<CompanyRepository> _logger;

    public CompanyRepository(AuthDbContext context, ILogger<CompanyRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Company?> GetByIdAsync(string companyId)
    {
        try
        {
            return await _context.Companies
                .Include(c => c.Users)
                .FirstOrDefaultAsync(c => c.Id == companyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving company by ID {CompanyId}", companyId);
            throw;
        }
    }

    public async Task<Company?> GetByNameAsync(string name)
    {
        try
        {
            return await _context.Companies
                .Include(c => c.Users)
                .FirstOrDefaultAsync(c => c.Name == name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving company by name {Name}", name);
            throw;
        }
    }

    public async Task<Company?> GetByEmailDomainAsync(string emailDomain)
    {
        try
        {
            return await _context.Companies
                .Include(c => c.Users)
                .FirstOrDefaultAsync(c => c.EmailDomain == emailDomain.ToLowerInvariant());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving company by email domain {EmailDomain}", emailDomain);
            throw;
        }
    }

    public async Task<List<Company>> GetAllAsync(int page = 0, int pageSize = 50)
    {
        try
        {
            return await _context.Companies
                .Include(c => c.Users)
                .OrderBy(c => c.Name)
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving companies");
            throw;
        }
    }

    public async Task<Company> CreateAsync(Company company)
    {
        try
        {
            _context.Companies.Add(company);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Created company {CompanyId}", company.Id);
            return company;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating company");
            throw;
        }
    }

    public async Task<Company> UpdateAsync(Company company)
    {
        try
        {
            company.UpdatedAt = DateTime.UtcNow;
            _context.Companies.Update(company);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated company {CompanyId}", company.Id);
            return company;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating company {CompanyId}", company.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(string companyId)
    {
        try
        {
            var company = await GetByIdAsync(companyId);
            if (company == null)
            {
                return false;
            }

            _context.Companies.Remove(company);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Deleted company {CompanyId}", companyId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting company {CompanyId}", companyId);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string name)
    {
        try
        {
            return await _context.Companies.AnyAsync(c => c.Name == name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if company exists");
            throw;
        }
    }

    public async Task<List<User>> GetCompanyUsersAsync(string companyId)
    {
        try
        {
            return await _context.Users
                .Where(u => u.TenantId == companyId)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users for company {CompanyId}", companyId);
            throw;
        }
    }
}
