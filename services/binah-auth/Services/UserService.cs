using Binah.Auth.Models;
using Binah.Auth.Repositories;
using Binah.Contracts.Common;
using Binah.Core.Exceptions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Binah.Auth.Services;

/// <summary>
/// Service for user management operations
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<UserDto?> GetUserByIdAsync(string userId)
    {
        _logger.LogDebug("Getting user {UserId}", userId);

        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            return null;
        }

        return MapToUserDto(user);
    }

    public async Task<PagedResult<UserDto>> GetAllUsersAsync(int page = 0, int pageSize = 50)
    {
        _logger.LogDebug("Getting all users, page {Page}, size {PageSize}", page, pageSize);

        var users = await _userRepository.GetAllAsync(page, pageSize);

        return new PagedResult<UserDto>
        {
            Items = users.Select(MapToUserDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = users.Count // In production, get actual count from repository
        };
    }

    public async Task<UserDto> UpdateUserAsync(string userId, UpdateUserRequest request)
    {
        _logger.LogInformation("Updating user {UserId}", userId);

        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            throw new EntityNotFoundException(userId);
        }

        // Update fields if provided
        if (!string.IsNullOrEmpty(request.FirstName))
        {
            user.FirstName = request.FirstName;
        }

        if (!string.IsNullOrEmpty(request.LastName))
        {
            user.LastName = request.LastName;
        }

        if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
        {
            // Check if email is already taken
            var existingUser = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUser != null && existingUser.Id != userId)
            {
                throw new ValidationException("Email is already taken");
            }
            user.Email = request.Email;
            user.EmailVerified = false; // Re-verify email
        }

        if (request.Roles != null)
        {
            user.Roles = request.Roles;
        }

        if (request.IsActive.HasValue)
        {
            user.IsActive = request.IsActive.Value;
        }

        user = await _userRepository.UpdateAsync(user);

        _logger.LogInformation("User {UserId} updated successfully", userId);

        return MapToUserDto(user);
    }

    public async Task<bool> DeleteUserAsync(string userId)
    {
        _logger.LogInformation("Deleting user {UserId}", userId);

        var result = await _userRepository.DeleteAsync(userId);

        if (result)
        {
            _logger.LogInformation("User {UserId} deleted successfully", userId);
        }

        return result;
    }

    public async Task<bool> AssignRolesAsync(string userId, List<string> roles)
    {
        _logger.LogInformation("Assigning roles to user {UserId}", userId);

        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            throw new EntityNotFoundException(userId);
        }

        user.Roles = roles;
        await _userRepository.UpdateAsync(user);

        _logger.LogInformation("Roles assigned to user {UserId}", userId);

        return true;
    }

    public async Task<bool> ActivateUserAsync(string userId)
    {
        _logger.LogInformation("Activating user {UserId}", userId);

        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            throw new EntityNotFoundException(userId);
        }

        user.IsActive = true;
        await _userRepository.UpdateAsync(user);

        _logger.LogInformation("User {UserId} activated", userId);

        return true;
    }

    public async Task<bool> DeactivateUserAsync(string userId)
    {
        _logger.LogInformation("Deactivating user {UserId}", userId);

        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            throw new EntityNotFoundException(userId);
        }

        user.IsActive = false;

        // Revoke all tokens
        await _userRepository.RevokeAllUserTokensAsync(userId);
        await _userRepository.UpdateAsync(user);

        _logger.LogInformation("User {UserId} deactivated", userId);

        return true;
    }

    private UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = user.Roles,
            TenantId = user.TenantId,
            IsActive = user.IsActive,
            EmailVerified = user.EmailVerified,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };
    }
}
