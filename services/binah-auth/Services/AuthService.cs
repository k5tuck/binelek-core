using Binah.Auth.Models;
using Binah.Auth.Repositories;
using Binah.Core.Exceptions;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using BCrypt.Net;

namespace Binah.Auth.Services;

/// <summary>
/// Service for authentication operations
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly ITenantProvisioningService _tenantProvisioningService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        ITokenService tokenService,
        ITenantProvisioningService tenantProvisioningService,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _tenantProvisioningService = tenantProvisioningService ?? throw new ArgumentNullException(nameof(tenantProvisioningService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TokenResponse> RegisterAsync(RegisterRequest request)
    {
        _logger.LogInformation("Registering new user {Username}", request.Username);

        // Check if user already exists
        if (await _userRepository.ExistsAsync(request.Username, request.Email))
        {
            throw new ValidationException("Username or email already exists");
        }

        // Provision tenant automatically if not provided
        string tenantId;
        string role;

        if (string.IsNullOrEmpty(request.TenantId))
        {
            // Automatic tenant provisioning based on email domain
            _logger.LogInformation("No tenant provided, auto-provisioning for email {Email}", request.Email);
            (tenantId, role) = await _tenantProvisioningService.ProvisionTenantForUserAsync(request.Email);
            _logger.LogInformation("Auto-provisioned tenant {TenantId} with role {Role}", tenantId, role);
        }
        else
        {
            // Use provided tenant ID (for manual assignment)
            tenantId = request.TenantId;
            role = Roles.User; // Default to User role when manually assigned
            _logger.LogInformation("Using provided tenant {TenantId}", tenantId);
        }

        // Create new user
        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            TenantId = tenantId,
            Roles = new() { role }, // Role determined by tenant provisioning
            IsActive = true,
            EmailVerified = false
        };

        user = await _userRepository.CreateAsync(user);

        // Generate tokens
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken(user.Id);

        await _userRepository.CreateRefreshTokenAsync(refreshToken);

        _logger.LogInformation("User {UserId} registered successfully", user.Id);

        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15), // From JWT settings
            User = MapToUserDto(user)
        };
    }

    public async Task<TokenResponse> LoginAsync(LoginRequest request, string? ipAddress = null)
    {
        _logger.LogInformation("Login attempt for {UsernameOrEmail}", request.UsernameOrEmail);

        var user = await _userRepository.GetByUsernameOrEmailAsync(request.UsernameOrEmail);

        if (user == null)
        {
            _logger.LogWarning("Login failed: User not found");
            throw new ValidationException("Invalid username/email or password");
        }

        // Check if account is locked
        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
        {
            _logger.LogWarning("Login failed: Account locked for user {UserId}", user.Id);
            throw new ValidationException($"Account is locked until {user.LockoutEnd.Value}");
        }

        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            // Increment failed login attempts
            user.FailedLoginAttempts++;

            // Lock account after 5 failed attempts
            if (user.FailedLoginAttempts >= 5)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(30);
                _logger.LogWarning("Account locked for user {UserId} after {Attempts} failed attempts",
                    user.Id, user.FailedLoginAttempts);
            }

            await _userRepository.UpdateAsync(user);

            _logger.LogWarning("Login failed: Invalid password for user {UserId}", user.Id);
            throw new ValidationException("Invalid username/email or password");
        }

        // Check if account is active
        if (!user.IsActive)
        {
            _logger.LogWarning("Login failed: Account inactive for user {UserId}", user.Id);
            throw new ValidationException("Account is inactive");
        }

        // Reset failed login attempts
        user.FailedLoginAttempts = 0;
        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        // Generate tokens
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken(user.Id, ipAddress);

        // Revoke old tokens if not "remember me"
        if (!request.RememberMe)
        {
            await _userRepository.RevokeAllUserTokensAsync(user.Id);
        }

        await _userRepository.CreateRefreshTokenAsync(refreshToken);

        _logger.LogInformation("User {UserId} logged in successfully", user.Id);

        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresAt = refreshToken.ExpiresAt,
            User = MapToUserDto(user)
        };
    }

    public async Task<TokenResponse> RefreshTokenAsync(string refreshToken, string? ipAddress = null)
    {
        _logger.LogDebug("Refreshing token");

        var token = await _userRepository.GetRefreshTokenAsync(refreshToken);

        if (token == null || !token.IsActive)
        {
            _logger.LogWarning("Refresh token invalid or expired");
            throw new ValidationException("Invalid or expired refresh token");
        }

        var user = await _userRepository.GetByIdAsync(token.UserId);

        if (user == null || !user.IsActive)
        {
            _logger.LogWarning("User not found or inactive");
            throw new ValidationException("User not found or inactive");
        }

        // Revoke old refresh token
        await _userRepository.RevokeRefreshTokenAsync(refreshToken);

        // Generate new tokens
        var accessToken = _tokenService.GenerateAccessToken(user);
        var newRefreshToken = _tokenService.GenerateRefreshToken(user.Id, ipAddress);

        await _userRepository.CreateRefreshTokenAsync(newRefreshToken);

        _logger.LogInformation("Token refreshed for user {UserId}", user.Id);

        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken.Token,
            ExpiresAt = newRefreshToken.ExpiresAt,
            User = MapToUserDto(user)
        };
    }

    public async Task RevokeTokenAsync(string refreshToken)
    {
        _logger.LogInformation("Revoking token");

        await _userRepository.RevokeRefreshTokenAsync(refreshToken);
    }

    public async Task<bool> ChangePasswordAsync(string userId, ChangePasswordRequest request)
    {
        _logger.LogInformation("Changing password for user {UserId}", userId);

        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            throw new EntityNotFoundException(userId);
        }

        // Verify current password
        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
        {
            _logger.LogWarning("Password change failed: Invalid current password");
            throw new ValidationException("Current password is incorrect");
        }

        // Update password
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _userRepository.UpdateAsync(user);

        // Revoke all refresh tokens for security
        await _userRepository.RevokeAllUserTokensAsync(userId);

        _logger.LogInformation("Password changed successfully for user {UserId}", userId);

        return true;
    }

    public async Task<bool> ResetPasswordRequestAsync(string email)
    {
        _logger.LogInformation("Password reset requested for email {Email}", email);

        var user = await _userRepository.GetByEmailAsync(email);

        if (user == null)
        {
            // Don't reveal if email exists
            _logger.LogWarning("Password reset requested for non-existent email");
            return true;
        }

        // In production, send password reset email
        // For now, just log
        _logger.LogInformation("Password reset email would be sent to {Email}", email);

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
