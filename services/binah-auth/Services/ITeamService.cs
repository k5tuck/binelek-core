using Binah.Auth.Models;

namespace Binah.Auth.Services;

/// <summary>
/// Interface for team management operations
/// </summary>
public interface ITeamService
{
    // Team Members
    Task<List<TeamMember>> GetMembersAsync(string tenantId);
    Task<TeamMember?> GetMemberAsync(string tenantId, string userId);
    Task<TeamMember> AddMemberAsync(string tenantId, TeamMember member);
    Task<TeamMember> UpdateMemberAsync(string tenantId, string userId, TeamMember member);
    Task<bool> RemoveMemberAsync(string tenantId, string userId);
    Task<bool> UpdateMemberRoleAsync(string tenantId, string userId, string role);

    // Invitations
    Task<List<TeamInvitation>> GetInvitationsAsync(string tenantId);
    Task<TeamInvitation> CreateInvitationAsync(string tenantId, TeamInvitation invitation);
    Task<TeamInvitation?> GetInvitationByTokenAsync(string token);
    Task<TeamMember> AcceptInvitationAsync(string token, string userId);
    Task<bool> RevokeInvitationAsync(string tenantId, string invitationId);
    Task<TeamInvitation> ResendInvitationAsync(string tenantId, string invitationId);

    // Shared Resources
    Task<List<SharedResource>> GetSharedResourcesAsync(string tenantId, string? userId = null);
    Task<SharedResource> ShareResourceAsync(string tenantId, SharedResource resource);
    Task<SharedResource> UpdateSharedResourceAsync(string tenantId, string resourceId, SharedResource resource);
    Task<bool> UnshareResourceAsync(string tenantId, string resourceId);
    Task<bool> HasAccessAsync(string tenantId, string userId, string resourceType, string resourceId);

    // Activity Feed
    Task<List<TeamActivity>> GetActivityFeedAsync(string tenantId, int skip = 0, int take = 50);
    Task<TeamActivity> LogActivityAsync(string tenantId, TeamActivity activity);
}
