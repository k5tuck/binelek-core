using Binah.Auth.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Binah.Auth.Repositories;

/// <summary>
/// Repository interface for user operations
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(string userId);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail);
    Task<List<User>> GetByTenantIdAsync(string tenantId);
    Task<List<User>> GetAllAsync(int page = 0, int pageSize = 50);
    Task<User> CreateAsync(User user);
    Task<User> UpdateAsync(User user);
    Task<bool> DeleteAsync(string userId);
    Task<bool> ExistsAsync(string username, string email);
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    Task<RefreshToken> CreateRefreshTokenAsync(RefreshToken refreshToken);
    Task RevokeRefreshTokenAsync(string token);
    Task RevokeAllUserTokensAsync(string userId);
}
