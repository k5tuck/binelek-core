using Binah.Auth.Models;
using System.Threading.Tasks;

namespace Binah.Auth.Services;

/// <summary>
/// Service interface for authentication operations
/// </summary>
public interface IAuthService
{
    Task<TokenResponse> RegisterAsync(RegisterRequest request);
    Task<TokenResponse> LoginAsync(LoginRequest request, string? ipAddress = null);
    Task<TokenResponse> RefreshTokenAsync(string refreshToken, string? ipAddress = null);
    Task RevokeTokenAsync(string refreshToken);
    Task<bool> ChangePasswordAsync(string userId, ChangePasswordRequest request);
    Task<bool> ResetPasswordRequestAsync(string email);
}
