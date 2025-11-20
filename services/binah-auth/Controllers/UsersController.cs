using Binah.Auth.Models;
using Binah.Auth.Services;
using Binah.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Binah.Auth.Controllers;

/// <summary>
/// Controller for user management operations
/// </summary>
[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all users with pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<UserDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> GetAll(
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 50)
    {
        var users = await _userService.GetAllUsersAsync(page, pageSize);
        return Ok(ApiResponse<PagedResult<UserDto>>.Ok(users));
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetById(string id)
    {
        var user = await _userService.GetUserByIdAsync(id);

        if (user == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "User Not Found",
                Detail = $"User with ID '{id}' not found",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(ApiResponse<UserDto>.Ok(user));
    }

    /// <summary>
    /// Update user
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserDto>>> Update(string id, [FromBody] UpdateUserRequest request)
    {
        try
        {
            var user = await _userService.UpdateUserAsync(id, request);
            return Ok(ApiResponse<UserDto>.Ok(user));
        }
        catch (Core.Exceptions.EntityNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "User Not Found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (Core.Exceptions.ValidationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation Error",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    /// <summary>
    /// Delete user
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _userService.DeleteUserAsync(id);

        if (!result)
        {
            return NotFound(new ProblemDetails
            {
                Title = "User Not Found",
                Detail = $"User with ID '{id}' not found",
                Status = StatusCodes.Status404NotFound
            });
        }

        return NoContent();
    }

    /// <summary>
    /// Assign roles to user
    /// </summary>
    [HttpPost("{id}/roles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignRoles(string id, [FromBody] List<string> roles)
    {
        try
        {
            await _userService.AssignRolesAsync(id, roles);
            return Ok(new { message = "Roles assigned successfully" });
        }
        catch (Core.Exceptions.EntityNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "User Not Found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
    }

    /// <summary>
    /// Activate user account
    /// </summary>
    [HttpPost("{id}/activate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(string id)
    {
        try
        {
            await _userService.ActivateUserAsync(id);
            return Ok(new { message = "User activated successfully" });
        }
        catch (Core.Exceptions.EntityNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "User Not Found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
    }

    /// <summary>
    /// Deactivate user account
    /// </summary>
    [HttpPost("{id}/deactivate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(string id)
    {
        try
        {
            await _userService.DeactivateUserAsync(id);
            return Ok(new { message = "User deactivated successfully" });
        }
        catch (Core.Exceptions.EntityNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "User Not Found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
    }
}
