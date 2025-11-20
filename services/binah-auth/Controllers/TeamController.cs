using Binah.Auth.Models;
using Binah.Auth.Services;
using Binah.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Binah.Auth.Controllers;

/// <summary>
/// Controller for team workspace management
/// </summary>
[ApiController]
[Route("api/team")]
[Authorize]
public class TeamController : ControllerBase
{
    private readonly ITeamService _teamService;
    private readonly ILogger<TeamController> _logger;

    public TeamController(ITeamService teamService, ILogger<TeamController> logger)
    {
        _teamService = teamService ?? throw new ArgumentNullException(nameof(teamService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private string GetTenantId() => User.FindFirst("tenant_id")?.Value ?? "system";
    private string GetUserId() => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

    #region Team Members

    /// <summary>
    /// Get all team members
    /// </summary>
    [HttpGet("members")]
    [ProducesResponseType(typeof(ApiResponse<List<TeamMember>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<TeamMember>>>> GetMembers()
    {
        var members = await _teamService.GetMembersAsync(GetTenantId());
        return Ok(ApiResponse<List<TeamMember>>.Ok(members));
    }

    /// <summary>
    /// Get a specific team member
    /// </summary>
    [HttpGet("members/{userId}")]
    [ProducesResponseType(typeof(ApiResponse<TeamMember>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TeamMember>>> GetMember(string userId)
    {
        var member = await _teamService.GetMemberAsync(GetTenantId(), userId);
        if (member == null)
            return NotFound();

        return Ok(ApiResponse<TeamMember>.Ok(member));
    }

    /// <summary>
    /// Add a new team member
    /// </summary>
    [HttpPost("members")]
    [ProducesResponseType(typeof(ApiResponse<TeamMember>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<TeamMember>>> AddMember([FromBody] AddTeamMemberRequest request)
    {
        try
        {
            var member = new TeamMember
            {
                UserId = request.UserId,
                Email = request.Email,
                DisplayName = request.DisplayName,
                Role = request.Role,
                Permissions = request.Permissions,
                InvitedBy = GetUserId()
            };

            var created = await _teamService.AddMemberAsync(GetTenantId(), member);
            return CreatedAtAction(nameof(GetMember), new { userId = created.UserId }, ApiResponse<TeamMember>.Ok(created));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add team member");
            return BadRequest(new ProblemDetails
            {
                Title = "Failed to add team member",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    /// <summary>
    /// Update a team member
    /// </summary>
    [HttpPut("members/{userId}")]
    [ProducesResponseType(typeof(ApiResponse<TeamMember>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TeamMember>>> UpdateMember(string userId, [FromBody] UpdateTeamMemberRequest request)
    {
        try
        {
            var member = new TeamMember
            {
                Role = request.Role,
                Status = request.Status,
                DisplayName = request.DisplayName,
                Permissions = request.Permissions
            };

            var updated = await _teamService.UpdateMemberAsync(GetTenantId(), userId, member);
            return Ok(ApiResponse<TeamMember>.Ok(updated));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Team member not found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
    }

    /// <summary>
    /// Remove a team member
    /// </summary>
    [HttpDelete("members/{userId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveMember(string userId)
    {
        var result = await _teamService.RemoveMemberAsync(GetTenantId(), userId);
        if (!result)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Update member role
    /// </summary>
    [HttpPatch("members/{userId}/role")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMemberRole(string userId, [FromBody] UpdateRoleRequest request)
    {
        var result = await _teamService.UpdateMemberRoleAsync(GetTenantId(), userId, request.Role);
        if (!result)
            return NotFound();

        return Ok(new { message = "Role updated successfully" });
    }

    #endregion

    #region Invitations

    /// <summary>
    /// Get all pending invitations
    /// </summary>
    [HttpGet("invitations")]
    [ProducesResponseType(typeof(ApiResponse<List<TeamInvitation>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<TeamInvitation>>>> GetInvitations()
    {
        var invitations = await _teamService.GetInvitationsAsync(GetTenantId());
        return Ok(ApiResponse<List<TeamInvitation>>.Ok(invitations));
    }

    /// <summary>
    /// Create a new invitation
    /// </summary>
    [HttpPost("invitations")]
    [ProducesResponseType(typeof(ApiResponse<TeamInvitation>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<TeamInvitation>>> CreateInvitation([FromBody] CreateInvitationRequest request)
    {
        try
        {
            var invitation = new TeamInvitation
            {
                Email = request.Email,
                Role = request.Role,
                Message = request.Message,
                InvitedBy = GetUserId()
            };

            var created = await _teamService.CreateInvitationAsync(GetTenantId(), invitation);
            return CreatedAtAction(nameof(GetInvitations), ApiResponse<TeamInvitation>.Ok(created));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create invitation");
            return BadRequest(new ProblemDetails
            {
                Title = "Failed to create invitation",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    /// <summary>
    /// Accept an invitation
    /// </summary>
    [HttpPost("invitations/accept")]
    [ProducesResponseType(typeof(ApiResponse<TeamMember>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<TeamMember>>> AcceptInvitation([FromBody] AcceptInvitationRequest request)
    {
        try
        {
            var member = await _teamService.AcceptInvitationAsync(request.Token, GetUserId());
            return Ok(ApiResponse<TeamMember>.Ok(member));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid invitation",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    /// <summary>
    /// Revoke an invitation
    /// </summary>
    [HttpDelete("invitations/{invitationId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeInvitation(string invitationId)
    {
        var result = await _teamService.RevokeInvitationAsync(GetTenantId(), invitationId);
        if (!result)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Resend an invitation
    /// </summary>
    [HttpPost("invitations/{invitationId}/resend")]
    [ProducesResponseType(typeof(ApiResponse<TeamInvitation>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TeamInvitation>>> ResendInvitation(string invitationId)
    {
        try
        {
            var invitation = await _teamService.ResendInvitationAsync(GetTenantId(), invitationId);
            return Ok(ApiResponse<TeamInvitation>.Ok(invitation));
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    #endregion

    #region Shared Resources

    /// <summary>
    /// Get shared resources
    /// </summary>
    [HttpGet("resources")]
    [ProducesResponseType(typeof(ApiResponse<List<SharedResource>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<SharedResource>>>> GetSharedResources()
    {
        var resources = await _teamService.GetSharedResourcesAsync(GetTenantId(), GetUserId());
        return Ok(ApiResponse<List<SharedResource>>.Ok(resources));
    }

    /// <summary>
    /// Share a resource
    /// </summary>
    [HttpPost("resources")]
    [ProducesResponseType(typeof(ApiResponse<SharedResource>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<SharedResource>>> ShareResource([FromBody] ShareResourceRequest request)
    {
        var resource = new SharedResource
        {
            ResourceType = request.ResourceType,
            ResourceId = request.ResourceId,
            Name = request.Name,
            Description = request.Description,
            SharedBy = GetUserId(),
            AccessLevel = request.AccessLevel,
            SharedWithUsers = request.SharedWithUsers,
            SharedWithRoles = request.SharedWithRoles,
            IsPublic = request.IsPublic,
            ExpiresAt = request.ExpiresAt
        };

        var created = await _teamService.ShareResourceAsync(GetTenantId(), resource);
        return CreatedAtAction(nameof(GetSharedResources), ApiResponse<SharedResource>.Ok(created));
    }

    /// <summary>
    /// Update a shared resource
    /// </summary>
    [HttpPut("resources/{resourceId}")]
    [ProducesResponseType(typeof(ApiResponse<SharedResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SharedResource>>> UpdateSharedResource(string resourceId, [FromBody] ShareResourceRequest request)
    {
        try
        {
            var resource = new SharedResource
            {
                Name = request.Name,
                Description = request.Description,
                AccessLevel = request.AccessLevel,
                SharedWithUsers = request.SharedWithUsers,
                SharedWithRoles = request.SharedWithRoles,
                IsPublic = request.IsPublic,
                ExpiresAt = request.ExpiresAt
            };

            var updated = await _teamService.UpdateSharedResourceAsync(GetTenantId(), resourceId, resource);
            return Ok(ApiResponse<SharedResource>.Ok(updated));
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Unshare a resource
    /// </summary>
    [HttpDelete("resources/{resourceId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnshareResource(string resourceId)
    {
        var result = await _teamService.UnshareResourceAsync(GetTenantId(), resourceId);
        if (!result)
            return NotFound();

        return NoContent();
    }

    #endregion

    #region Activity Feed

    /// <summary>
    /// Get team activity feed
    /// </summary>
    [HttpGet("activity")]
    [ProducesResponseType(typeof(ApiResponse<List<TeamActivity>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<TeamActivity>>>> GetActivityFeed(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var activities = await _teamService.GetActivityFeedAsync(GetTenantId(), skip, take);
        return Ok(ApiResponse<List<TeamActivity>>.Ok(activities));
    }

    #endregion
}

#region Request DTOs

public class AddTeamMemberRequest
{
    public string UserId { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public string Role { get; set; } = "Member";
    public List<string> Permissions { get; set; } = new();
}

public class UpdateTeamMemberRequest
{
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public List<string> Permissions { get; set; } = new();
}

public class UpdateRoleRequest
{
    public string Role { get; set; } = string.Empty;
}

public class CreateInvitationRequest
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Member";
    public string? Message { get; set; }
}

public class AcceptInvitationRequest
{
    public string Token { get; set; } = string.Empty;
}

public class ShareResourceRequest
{
    public string ResourceType { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string AccessLevel { get; set; } = "view";
    public List<string> SharedWithUsers { get; set; } = new();
    public List<string> SharedWithRoles { get; set; } = new();
    public bool IsPublic { get; set; } = true;
    public DateTime? ExpiresAt { get; set; }
}

#endregion
