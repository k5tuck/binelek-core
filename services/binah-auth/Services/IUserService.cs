using Binah.Auth.Models;
using Binah.Contracts.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Binah.Auth.Services;

/// <summary>
/// Service interface for user management operations
/// </summary>
public interface IUserService
{
    Task<UserDto?> GetUserByIdAsync(string userId);
    Task<PagedResult<UserDto>> GetAllUsersAsync(int page = 0, int pageSize = 50);
    Task<UserDto> UpdateUserAsync(string userId, UpdateUserRequest request);
    Task<bool> DeleteUserAsync(string userId);
    Task<bool> AssignRolesAsync(string userId, List<string> roles);
    Task<bool> ActivateUserAsync(string userId);
    Task<bool> DeactivateUserAsync(string userId);
}
