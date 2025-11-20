using Binah.Auth.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Binah.Auth.Repositories;

/// <summary>
/// Repository interface for company operations
/// </summary>
public interface ICompanyRepository
{
    Task<Company?> GetByIdAsync(string companyId);
    Task<Company?> GetByNameAsync(string name);
    Task<Company?> GetByEmailDomainAsync(string emailDomain);
    Task<List<Company>> GetAllAsync(int page = 0, int pageSize = 50);
    Task<Company> CreateAsync(Company company);
    Task<Company> UpdateAsync(Company company);
    Task<bool> DeleteAsync(string companyId);
    Task<bool> ExistsAsync(string name);
    Task<List<User>> GetCompanyUsersAsync(string companyId);
}
