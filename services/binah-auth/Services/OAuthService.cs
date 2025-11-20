using Binah.Auth.Models;
using Binah.Auth.Repositories;
using Binah.Core.Exceptions;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Binah.Auth.Services;

/// <summary>
/// OAuth provider types
/// </summary>
public enum OAuthProvider
{
    Google,
    Microsoft,
    GitHub,
    Apple
}

/// <summary>
/// OAuth user information from external provider
/// </summary>
public class OAuthUserInfo
{
    public string ProviderId { get; set; } = string.Empty; // User ID from OAuth provider
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public OAuthProvider Provider { get; set; }
}

/// <summary>
/// Service for handling OAuth authentication
/// </summary>
public interface IOAuthService
{
    Task<TokenResponse> LoginOrRegisterWithOAuthAsync(OAuthUserInfo oauthUser);
}

public class OAuthService : IOAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly ITenantProvisioningService _tenantProvisioningService;
    private readonly ILogger<OAuthService> _logger;

    public OAuthService(
        IUserRepository userRepository,
        ITokenService tokenService,
        ITenantProvisioningService tenantProvisioningService,
        ILogger<OAuthService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _tenantProvisioningService = tenantProvisioningService ?? throw new ArgumentNullException(nameof(tenantProvisioningService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Login or register a user using OAuth information
    /// Automatically provisions tenant for new users based on email domain
    /// </summary>
    public async Task<TokenResponse> LoginOrRegisterWithOAuthAsync(OAuthUserInfo oauthUser)
    {
        if (string.IsNullOrWhiteSpace(oauthUser.Email))
        {
            throw new ValidationException("Email is required from OAuth provider");
        }

        _logger.LogInformation("OAuth login attempt for {Email} via {Provider}",
            oauthUser.Email, oauthUser.Provider);

        // Try to find existing user by email
        var existingUser = await _userRepository.GetByEmailAsync(oauthUser.Email);

        if (existingUser != null)
        {
            // User already exists - perform login
            _logger.LogInformation("Existing user found for OAuth login: {UserId}", existingUser.Id);

            // Check if account is active
            if (!existingUser.IsActive)
            {
                _logger.LogWarning("OAuth login failed: Account inactive for user {UserId}", existingUser.Id);
                throw new ValidationException("Account is inactive");
            }

            // Update last login
            existingUser.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(existingUser);

            // Generate tokens
            var accessToken = _tokenService.GenerateAccessToken(existingUser);
            var refreshToken = _tokenService.GenerateRefreshToken(existingUser.Id);
            await _userRepository.CreateRefreshTokenAsync(refreshToken);

            _logger.LogInformation("OAuth login successful for user {UserId}", existingUser.Id);

            return new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                ExpiresAt = refreshToken.ExpiresAt,
                User = MapToUserDto(existingUser)
            };
        }

        // User doesn't exist - register new user
        _logger.LogInformation("No existing user found, registering new OAuth user for {Email}", oauthUser.Email);

        // Automatic tenant provisioning based on email domain
        var (tenantId, role) = await _tenantProvisioningService.ProvisionTenantForUserAsync(oauthUser.Email);
        _logger.LogInformation("Auto-provisioned tenant {TenantId} with role {Role} for OAuth user",
            tenantId, role);

        // Generate username from email (everything before @)
        var username = GenerateUsernameFromEmail(oauthUser.Email, oauthUser.Provider);

        // Create new user (no password needed for OAuth)
        var newUser = new User
        {
            Username = username,
            Email = oauthUser.Email,
            PasswordHash = string.Empty, // No password for OAuth users
            FirstName = oauthUser.FirstName ?? ExtractFirstName(oauthUser.DisplayName),
            LastName = oauthUser.LastName ?? ExtractLastName(oauthUser.DisplayName),
            TenantId = tenantId,
            Roles = new() { role }, // Role determined by tenant provisioning
            IsActive = true,
            EmailVerified = true, // OAuth providers already verify emails
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow
        };

        newUser = await _userRepository.CreateAsync(newUser);

        // Generate tokens
        var newAccessToken = _tokenService.GenerateAccessToken(newUser);
        var newRefreshToken = _tokenService.GenerateRefreshToken(newUser.Id);
        await _userRepository.CreateRefreshTokenAsync(newRefreshToken);

        _logger.LogInformation("OAuth user registered successfully: {UserId} in tenant {TenantId} with role {Role}",
            newUser.Id, tenantId, role);

        return new TokenResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken.Token,
            ExpiresAt = newRefreshToken.ExpiresAt,
            User = MapToUserDto(newUser)
        };
    }

    /// <summary>
    /// Generate a unique username from email and provider
    /// Example: john@acme.com + Google → john_acme_google
    /// If username exists, append random suffix
    /// </summary>
    private string GenerateUsernameFromEmail(string email, OAuthProvider provider)
    {
        var localPart = email.Split('@')[0];
        var domain = email.Split('@')[1].Split('.')[0];
        var providerName = provider.ToString().ToLower();

        var baseUsername = $"{localPart}_{domain}_{providerName}";

        // Ensure username is unique
        var username = baseUsername;
        var suffix = 1;

        while (_userRepository.GetByUsernameAsync(username).Result != null)
        {
            username = $"{baseUsername}_{suffix}";
            suffix++;
        }

        return username;
    }

    /// <summary>
    /// Extract first name from display name
    /// Example: "John Doe" → "John"
    /// </summary>
    private string? ExtractFirstName(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return null;

        var parts = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[0] : null;
    }

    /// <summary>
    /// Extract last name from display name
    /// Example: "John Doe" → "Doe"
    /// </summary>
    private string? ExtractLastName(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return null;

        var parts = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 ? string.Join(" ", parts.Skip(1)) : null;
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
