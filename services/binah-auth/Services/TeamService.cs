using Binah.Auth.Models;
using Microsoft.EntityFrameworkCore;

namespace Binah.Auth.Services;

/// <summary>
/// Team management service implementation
/// </summary>
public class TeamService : ITeamService
{
    private readonly AuthDbContext _context;
    private readonly ILogger<TeamService> _logger;
    private readonly IEmailService _emailService;

    public TeamService(AuthDbContext context, ILogger<TeamService> logger, IEmailService emailService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
    }

    // Team Members
    public async Task<List<TeamMember>> GetMembersAsync(string tenantId)
    {
        return await _context.TeamMembers
            .Where(m => m.TenantId == tenantId && m.Status != TeamMemberStatus.Removed)
            .OrderBy(m => m.Role)
            .ThenBy(m => m.DisplayName)
            .ToListAsync();
    }

    public async Task<TeamMember?> GetMemberAsync(string tenantId, string userId)
    {
        return await _context.TeamMembers
            .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.UserId == userId);
    }

    public async Task<TeamMember> AddMemberAsync(string tenantId, TeamMember member)
    {
        member.TenantId = tenantId;
        member.CreatedAt = DateTime.UtcNow;
        member.UpdatedAt = DateTime.UtcNow;

        _context.TeamMembers.Add(member);
        await _context.SaveChangesAsync();

        await LogActivityAsync(tenantId, new TeamActivity
        {
            TenantId = tenantId,
            UserId = member.InvitedBy ?? "system",
            ActivityType = "member_added",
            Description = $"Added {member.DisplayName ?? member.Email} to the team as {member.Role}",
            Metadata = new Dictionary<string, object>
            {
                { "memberId", member.Id },
                { "memberUserId", member.UserId },
                { "role", member.Role }
            }
        });

        _logger.LogInformation("Added team member {UserId} to tenant {TenantId} with role {Role}",
            member.UserId, tenantId, member.Role);

        return member;
    }

    public async Task<TeamMember> UpdateMemberAsync(string tenantId, string userId, TeamMember member)
    {
        var existing = await GetMemberAsync(tenantId, userId);
        if (existing == null)
            throw new InvalidOperationException($"Team member {userId} not found");

        existing.Role = member.Role;
        existing.Status = member.Status;
        existing.DisplayName = member.DisplayName;
        existing.Permissions = member.Permissions;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return existing;
    }

    public async Task<bool> RemoveMemberAsync(string tenantId, string userId)
    {
        var member = await GetMemberAsync(tenantId, userId);
        if (member == null) return false;

        member.Status = TeamMemberStatus.Removed;
        member.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await LogActivityAsync(tenantId, new TeamActivity
        {
            TenantId = tenantId,
            UserId = userId,
            ActivityType = "member_removed",
            Description = $"{member.DisplayName ?? member.Email} was removed from the team"
        });

        return true;
    }

    public async Task<bool> UpdateMemberRoleAsync(string tenantId, string userId, string role)
    {
        var member = await GetMemberAsync(tenantId, userId);
        if (member == null) return false;

        var oldRole = member.Role;
        member.Role = role;
        member.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await LogActivityAsync(tenantId, new TeamActivity
        {
            TenantId = tenantId,
            UserId = userId,
            ActivityType = "role_changed",
            Description = $"{member.DisplayName ?? member.Email}'s role changed from {oldRole} to {role}"
        });

        return true;
    }

    // Invitations
    public async Task<List<TeamInvitation>> GetInvitationsAsync(string tenantId)
    {
        return await _context.TeamInvitations
            .Where(i => i.TenantId == tenantId && i.Status == "pending")
            .OrderByDescending(i => i.SentAt)
            .ToListAsync();
    }

    public async Task<TeamInvitation> CreateInvitationAsync(string tenantId, TeamInvitation invitation)
    {
        invitation.TenantId = tenantId;
        invitation.Token = Guid.NewGuid().ToString();
        invitation.Status = "pending";
        invitation.SentAt = DateTime.UtcNow;
        invitation.ExpiresAt = DateTime.UtcNow.AddDays(7);

        _context.TeamInvitations.Add(invitation);
        await _context.SaveChangesAsync();

        // Send invitation email
        // Note: inviterName and companyName could be looked up from the tenant/user
        await _emailService.SendTeamInvitationEmailAsync(invitation.Email, "Team Admin", tenantId, invitation.Token);

        await LogActivityAsync(tenantId, new TeamActivity
        {
            TenantId = tenantId,
            UserId = invitation.InvitedBy,
            ActivityType = "invitation_sent",
            Description = $"Invitation sent to {invitation.Email} with role {invitation.Role}"
        });

        _logger.LogInformation("Created invitation for {Email} to tenant {TenantId}", invitation.Email, tenantId);

        return invitation;
    }

    public async Task<TeamInvitation?> GetInvitationByTokenAsync(string token)
    {
        return await _context.TeamInvitations
            .FirstOrDefaultAsync(i => i.Token == token && i.Status == "pending" && i.ExpiresAt > DateTime.UtcNow);
    }

    public async Task<TeamMember> AcceptInvitationAsync(string token, string userId)
    {
        var invitation = await GetInvitationByTokenAsync(token);
        if (invitation == null)
            throw new InvalidOperationException("Invalid or expired invitation");

        invitation.Status = "accepted";
        invitation.AcceptedAt = DateTime.UtcNow;

        var member = new TeamMember
        {
            TenantId = invitation.TenantId,
            UserId = userId,
            Email = invitation.Email,
            Role = invitation.Role,
            Status = TeamMemberStatus.Active,
            InvitedBy = invitation.InvitedBy,
            JoinedAt = DateTime.UtcNow
        };

        _context.TeamMembers.Add(member);
        await _context.SaveChangesAsync();

        await LogActivityAsync(invitation.TenantId, new TeamActivity
        {
            TenantId = invitation.TenantId,
            UserId = userId,
            ActivityType = "member_joined",
            Description = $"{invitation.Email} joined the team as {invitation.Role}"
        });

        return member;
    }

    public async Task<bool> RevokeInvitationAsync(string tenantId, string invitationId)
    {
        var invitation = await _context.TeamInvitations
            .FirstOrDefaultAsync(i => i.TenantId == tenantId && i.Id == invitationId);

        if (invitation == null) return false;

        invitation.Status = "revoked";
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<TeamInvitation> ResendInvitationAsync(string tenantId, string invitationId)
    {
        var invitation = await _context.TeamInvitations
            .FirstOrDefaultAsync(i => i.TenantId == tenantId && i.Id == invitationId);

        if (invitation == null)
            throw new InvalidOperationException("Invitation not found");

        invitation.Token = Guid.NewGuid().ToString();
        invitation.SentAt = DateTime.UtcNow;
        invitation.ExpiresAt = DateTime.UtcNow.AddDays(7);
        invitation.ResendCount++;

        await _context.SaveChangesAsync();

        await _emailService.SendTeamInvitationEmailAsync(invitation.Email, "Team Admin", tenantId, invitation.Token);

        return invitation;
    }

    // Shared Resources
    public async Task<List<SharedResource>> GetSharedResourcesAsync(string tenantId, string? userId = null)
    {
        var query = _context.SharedResources
            .Where(r => r.TenantId == tenantId);

        if (!string.IsNullOrEmpty(userId))
        {
            query = query.Where(r =>
                r.IsPublic ||
                r.SharedBy == userId ||
                r.SharedWithUsers.Contains(userId));
        }

        return await query
            .OrderByDescending(r => r.SharedAt)
            .ToListAsync();
    }

    public async Task<SharedResource> ShareResourceAsync(string tenantId, SharedResource resource)
    {
        resource.TenantId = tenantId;
        resource.SharedAt = DateTime.UtcNow;
        resource.CreatedAt = DateTime.UtcNow;
        resource.UpdatedAt = DateTime.UtcNow;

        _context.SharedResources.Add(resource);
        await _context.SaveChangesAsync();

        await LogActivityAsync(tenantId, new TeamActivity
        {
            TenantId = tenantId,
            UserId = resource.SharedBy,
            ActivityType = "resource_shared",
            Description = $"Shared {resource.ResourceType} '{resource.Name}'",
            ResourceType = resource.ResourceType,
            ResourceId = resource.ResourceId,
            ResourceName = resource.Name
        });

        return resource;
    }

    public async Task<SharedResource> UpdateSharedResourceAsync(string tenantId, string resourceId, SharedResource resource)
    {
        var existing = await _context.SharedResources
            .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Id == resourceId);

        if (existing == null)
            throw new InvalidOperationException("Shared resource not found");

        existing.Name = resource.Name;
        existing.Description = resource.Description;
        existing.AccessLevel = resource.AccessLevel;
        existing.SharedWithUsers = resource.SharedWithUsers;
        existing.SharedWithRoles = resource.SharedWithRoles;
        existing.IsPublic = resource.IsPublic;
        existing.ExpiresAt = resource.ExpiresAt;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return existing;
    }

    public async Task<bool> UnshareResourceAsync(string tenantId, string resourceId)
    {
        var resource = await _context.SharedResources
            .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Id == resourceId);

        if (resource == null) return false;

        _context.SharedResources.Remove(resource);
        await _context.SaveChangesAsync();

        await LogActivityAsync(tenantId, new TeamActivity
        {
            TenantId = tenantId,
            UserId = resource.SharedBy,
            ActivityType = "resource_unshared",
            Description = $"Unshared {resource.ResourceType} '{resource.Name}'",
            ResourceType = resource.ResourceType,
            ResourceId = resource.ResourceId
        });

        return true;
    }

    public async Task<bool> HasAccessAsync(string tenantId, string userId, string resourceType, string resourceId)
    {
        var resource = await _context.SharedResources
            .FirstOrDefaultAsync(r =>
                r.TenantId == tenantId &&
                r.ResourceType == resourceType &&
                r.ResourceId == resourceId);

        if (resource == null) return false;
        if (resource.IsPublic) return true;
        if (resource.SharedBy == userId) return true;
        if (resource.SharedWithUsers.Contains(userId)) return true;

        // Check role-based access
        var member = await GetMemberAsync(tenantId, userId);
        if (member != null && resource.SharedWithRoles.Contains(member.Role))
            return true;

        return false;
    }

    // Activity Feed
    public async Task<List<TeamActivity>> GetActivityFeedAsync(string tenantId, int skip = 0, int take = 50)
    {
        return await _context.TeamActivities
            .Where(a => a.TenantId == tenantId)
            .OrderByDescending(a => a.Timestamp)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<TeamActivity> LogActivityAsync(string tenantId, TeamActivity activity)
    {
        activity.TenantId = tenantId;
        activity.Timestamp = DateTime.UtcNow;

        _context.TeamActivities.Add(activity);
        await _context.SaveChangesAsync();

        return activity;
    }
}
