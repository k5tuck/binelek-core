using Binah.Auth.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Binah.Auth.Repositories;

/// <summary>
/// Repository implementation for user operations
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly AuthDbContext _context;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(AuthDbContext context, ILogger<UserRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<User?> GetByIdAsync(string userId)
    {
        try
        {
            return await _context.Users
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by ID {UserId}", userId);
            throw;
        }
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        try
        {
            return await _context.Users
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.Username == username);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by username {Username}", username);
            throw;
        }
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        try
        {
            return await _context.Users
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.Email == email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by email {Email}", email);
            throw;
        }
    }

    public async Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail)
    {
        try
        {
            return await _context.Users
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.Username == usernameOrEmail || u.Email == usernameOrEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by username or email");
            throw;
        }
    }

    public async Task<List<User>> GetByTenantIdAsync(string tenantId)
    {
        try
        {
            return await _context.Users
                .Where(u => u.TenantId == tenantId)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users by tenant ID {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<List<User>> GetAllAsync(int page = 0, int pageSize = 50)
    {
        try
        {
            return await _context.Users
                .OrderBy(u => u.CreatedAt)
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");
            throw;
        }
    }

    public async Task<User> CreateAsync(User user)
    {
        try
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Created user {UserId}", user.Id);
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            throw;
        }
    }

    public async Task<User> UpdateAsync(User user)
    {
        try
        {
            user.UpdatedAt = DateTime.UtcNow;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated user {UserId}", user.Id);
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", user.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(string userId)
    {
        try
        {
            var user = await GetByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Deleted user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string username, string email)
    {
        try
        {
            return await _context.Users
                .AnyAsync(u => u.Username == username || u.Email == email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user exists");
            throw;
        }
    }

    public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
    {
        try
        {
            return await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving refresh token");
            throw;
        }
    }

    public async Task<RefreshToken> CreateRefreshTokenAsync(RefreshToken refreshToken)
    {
        try
        {
            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Created refresh token for user {UserId}", refreshToken.UserId);
            return refreshToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating refresh token");
            throw;
        }
    }

    public async Task RevokeRefreshTokenAsync(string token)
    {
        try
        {
            var refreshToken = await GetRefreshTokenAsync(token);
            if (refreshToken != null)
            {
                refreshToken.IsRevoked = true;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Revoked refresh token");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking refresh token");
            throw;
        }
    }

    public async Task RevokeAllUserTokensAsync(string userId)
    {
        try
        {
            var tokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                .ToListAsync();

            foreach (var token in tokens)
            {
                token.IsRevoked = true;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Revoked all tokens for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking user tokens");
            throw;
        }
    }
}
