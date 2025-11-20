using System;
using System.Collections.Generic;

namespace Binah.Auth.Models;

/// <summary>
/// User entity for authentication and authorization
/// </summary>
public class User
{
    /// <summary>
    /// Unique user identifier
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Username (unique)
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Email address (unique)
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Hashed password (BCrypt)
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// First name
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Last name
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// User roles
    /// </summary>
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// Tenant ID for multi-tenancy
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Whether the user account is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether the email is verified
    /// </summary>
    public bool EmailVerified { get; set; } = false;

    /// <summary>
    /// Account creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Last login timestamp
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Account lockout end time (null if not locked)
    /// </summary>
    public DateTime? LockoutEnd { get; set; }

    /// <summary>
    /// Failed login attempts
    /// </summary>
    public int FailedLoginAttempts { get; set; } = 0;

    /// <summary>
    /// Refresh tokens for this user
    /// </summary>
    public List<RefreshToken> RefreshTokens { get; set; } = new();
}

/// <summary>
/// Refresh token for extending sessions
/// </summary>
public class RefreshToken
{
    /// <summary>
    /// Token identifier
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// User ID this token belongs to
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Token value
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration timestamp
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Whether the token has been revoked
    /// </summary>
    public bool IsRevoked { get; set; } = false;

    /// <summary>
    /// Token creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// IP address that created the token
    /// </summary>
    public string? CreatedByIp { get; set; }

    /// <summary>
    /// Whether the token is active and valid
    /// </summary>
    public bool IsActive => !IsRevoked && DateTime.UtcNow < ExpiresAt;
}

/// <summary>
/// Role definition
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string User = "User";
    public const string Manager = "Manager";
    public const string Viewer = "Viewer";
}
