using Binah.Auth.Models;
using System.Security.Claims;

namespace Binah.Auth.Services;

/// <summary>
/// Service interface for JWT token operations
/// </summary>
public interface ITokenService
{
    string GenerateAccessToken(User user);
    RefreshToken GenerateRefreshToken(string userId, string? ipAddress = null);
    ClaimsPrincipal? ValidateToken(string token);
    string? GetUserIdFromToken(string token);
}
