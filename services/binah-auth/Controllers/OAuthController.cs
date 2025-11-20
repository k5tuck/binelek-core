using Binah.Auth.Models;
using Binah.Auth.Services;
using Binah.Contracts.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Binah.Auth.Controllers;

/// <summary>
/// Controller for OAuth authentication
/// </summary>
[ApiController]
[Route("api/auth/oauth")]
public class OAuthController : ControllerBase
{
    private readonly IOAuthService _oauthService;
    private readonly ILogger<OAuthController> _logger;

    public OAuthController(IOAuthService oauthService, ILogger<OAuthController> logger)
    {
        _oauthService = oauthService ?? throw new ArgumentNullException(nameof(oauthService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// OAuth callback endpoint - handles user info from OAuth provider
    /// This endpoint receives user data from the OAuth provider after successful authentication
    /// </summary>
    /// <remarks>
    /// Flow:
    /// 1. User clicks "Login with Google/Microsoft/etc" on frontend
    /// 2. Frontend redirects to OAuth provider
    /// 3. User authenticates with provider
    /// 4. Provider redirects back with authorization code
    /// 5. Frontend exchanges code for user info
    /// 6. Frontend calls this endpoint with user info
    /// 7. Backend creates/updates user and provisions tenant
    /// 8. Returns JWT tokens
    /// </remarks>
    [HttpPost("callback")]
    [ProducesResponseType(typeof(ApiResponse<TokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<TokenResponse>>> OAuthCallback([FromBody] OAuthCallbackRequest request)
    {
        try
        {
            _logger.LogInformation("OAuth callback received for {Email} from {Provider}",
                request.Email, request.Provider);

            var oauthUser = new OAuthUserInfo
            {
                ProviderId = request.ProviderId,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                DisplayName = request.DisplayName,
                AvatarUrl = request.AvatarUrl,
                Provider = request.Provider
            };

            var response = await _oauthService.LoginOrRegisterWithOAuthAsync(oauthUser);

            return Ok(ApiResponse<TokenResponse>.Ok(response));
        }
        catch (Core.Exceptions.ValidationException ex)
        {
            _logger.LogWarning("OAuth callback validation error: {Message}", ex.Message);
            return BadRequest(new ProblemDetails
            {
                Title = "OAuth Authentication Failed",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OAuth callback error");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred during OAuth authentication",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Get OAuth provider configuration (client IDs, redirect URIs, etc.)
    /// Returns public configuration needed by frontend
    /// </summary>
    [HttpGet("config")]
    [ProducesResponseType(typeof(ApiResponse<OAuthConfig>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<OAuthConfig>> GetOAuthConfig()
    {
        // In production, these should come from appsettings or environment variables
        var config = new OAuthConfig
        {
            Google = new OAuthProviderConfig
            {
                ClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") ?? "",
                Enabled = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID")),
                RedirectUri = Environment.GetEnvironmentVariable("GOOGLE_REDIRECT_URI") ?? "http://localhost:3000/auth/google/callback"
            },
            Microsoft = new OAuthProviderConfig
            {
                ClientId = Environment.GetEnvironmentVariable("MICROSOFT_CLIENT_ID") ?? "",
                Enabled = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MICROSOFT_CLIENT_ID")),
                RedirectUri = Environment.GetEnvironmentVariable("MICROSOFT_REDIRECT_URI") ?? "http://localhost:3000/auth/microsoft/callback"
            },
            GitHub = new OAuthProviderConfig
            {
                ClientId = Environment.GetEnvironmentVariable("GITHUB_CLIENT_ID") ?? "",
                Enabled = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_CLIENT_ID")),
                RedirectUri = Environment.GetEnvironmentVariable("GITHUB_REDIRECT_URI") ?? "http://localhost:3000/auth/github/callback"
            }
        };

        return Ok(ApiResponse<OAuthConfig>.Ok(config));
    }
}

/// <summary>
/// Request from frontend containing OAuth user information
/// </summary>
public class OAuthCallbackRequest
{
    public string ProviderId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public OAuthProvider Provider { get; set; }
}

/// <summary>
/// OAuth configuration for frontend
/// </summary>
public class OAuthConfig
{
    public OAuthProviderConfig? Google { get; set; }
    public OAuthProviderConfig? Microsoft { get; set; }
    public OAuthProviderConfig? GitHub { get; set; }
}

public class OAuthProviderConfig
{
    public string ClientId { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string RedirectUri { get; set; } = string.Empty;
}
