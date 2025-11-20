using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Binah.API.Controllers;

/// <summary>
/// Proxy controller for Auth Service
/// Routes authentication requests to the Auth microservice
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthProxyController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AuthProxyController> _logger;

    public AuthProxyController(
        IHttpClientFactory httpClientFactory,
        ILogger<AuthProxyController> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] object request)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("AuthService");
            var response = await client.PostAsJsonAsync("/api/auth/register", request);

            var content = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to register user" });
        }
    }

    /// <summary>
    /// Login with username/email and password
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] object request)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("AuthService");
            var response = await client.PostAsJsonAsync("/api/auth/login", request);

            var content = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging in");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to login" });
        }
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] object request)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("AuthService");
            var response = await client.PostAsJsonAsync("/api/auth/refresh", request);

            var content = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to refresh token" });
        }
    }

    /// <summary>
    /// Revoke refresh token
    /// </summary>
    [Authorize]
    [HttpPost("revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeToken([FromBody] object request)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("AuthService");
            
            // Forward the authorization header
            if (Request.Headers.ContainsKey("Authorization"))
            {
                client.DefaultRequestHeaders.Add("Authorization", Request.Headers["Authorization"].ToString());
            }

            var response = await client.PostAsJsonAsync("/api/auth/revoke", request);

            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                return NoContent();
            }

            var content = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking token");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to revoke token" });
        }
    }

    /// <summary>
    /// Get current user information
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("AuthService");
            
            // Forward the authorization header
            if (Request.Headers.ContainsKey("Authorization"))
            {
                client.DefaultRequestHeaders.Add("Authorization", Request.Headers["Authorization"].ToString());
            }

            var response = await client.GetAsync("/api/auth/me");

            var content = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to retrieve user information" });
        }
    }

    /// <summary>
    /// Change password
    /// </summary>
    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] object request)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("AuthService");
            
            // Forward the authorization header
            if (Request.Headers.ContainsKey("Authorization"))
            {
                client.DefaultRequestHeaders.Add("Authorization", Request.Headers["Authorization"].ToString());
            }

            var response = await client.PostAsJsonAsync("/api/auth/change-password", request);

            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                return NoContent();
            }

            var content = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to change password" });
        }
    }

    /// <summary>
    /// Request password reset
    /// </summary>
    [AllowAnonymous]
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] object request)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("AuthService");
            var response = await client.PostAsJsonAsync("/api/auth/reset-password", request);

            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                return NoContent();
            }

            var content = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to reset password" });
        }
    }
}
