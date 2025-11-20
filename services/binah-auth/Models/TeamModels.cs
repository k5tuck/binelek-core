using System;
using System.Collections.Generic;

namespace Binah.Auth.Models;

/// <summary>
/// Team member association with role and status
/// </summary>
public class TeamMember
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Tenant ID for multi-tenancy
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// User ID of the team member
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Role in the team (Owner, Admin, Member, Viewer)
    /// </summary>
    public string Role { get; set; } = "Member";

    /// <summary>
    /// Status (active, invited, suspended, removed)
    /// </summary>
    public string Status { get; set; } = "active";

    /// <summary>
    /// User's display name
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// User's email
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Custom permissions for this member
    /// </summary>
    public List<string> Permissions { get; set; } = new();

    /// <summary>
    /// When the member joined
    /// </summary>
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Who invited this member
    /// </summary>
    public string? InvitedBy { get; set; }

    /// <summary>
    /// Last activity timestamp
    /// </summary>
    public DateTime? LastActiveAt { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Shared resource within a team
/// </summary>
public class SharedResource
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Tenant ID for multi-tenancy
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Type of resource (dashboard, report, query, view)
    /// </summary>
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the shared resource
    /// </summary>
    public string ResourceId { get; set; } = string.Empty;

    /// <summary>
    /// Name of the resource
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the resource
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// User ID who shared the resource
    /// </summary>
    public string SharedBy { get; set; } = string.Empty;

    /// <summary>
    /// Access level (view, edit, admin)
    /// </summary>
    public string AccessLevel { get; set; } = "view";

    /// <summary>
    /// Specific user IDs with access (empty = all team members)
    /// </summary>
    public List<string> SharedWithUsers { get; set; } = new();

    /// <summary>
    /// Specific roles with access (empty = all roles)
    /// </summary>
    public List<string> SharedWithRoles { get; set; } = new();

    /// <summary>
    /// Whether the resource is public to all team members
    /// </summary>
    public bool IsPublic { get; set; } = true;

    /// <summary>
    /// When the resource was shared
    /// </summary>
    public DateTime SharedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional expiration date
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Team activity feed entry
/// </summary>
public class TeamActivity
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Tenant ID for multi-tenancy
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// User ID who performed the action
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// User display name
    /// </summary>
    public string? UserDisplayName { get; set; }

    /// <summary>
    /// Type of activity (member_joined, resource_shared, permission_changed, etc.)
    /// </summary>
    public string ActivityType { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Resource type involved (if any)
    /// </summary>
    public string? ResourceType { get; set; }

    /// <summary>
    /// Resource ID involved (if any)
    /// </summary>
    public string? ResourceId { get; set; }

    /// <summary>
    /// Resource name for display
    /// </summary>
    public string? ResourceName { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Activity timestamp
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Team invitation
/// </summary>
public class TeamInvitation
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Tenant ID for multi-tenancy
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Email of the invitee
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Role to assign upon acceptance
    /// </summary>
    public string Role { get; set; } = "Member";

    /// <summary>
    /// Invitation token
    /// </summary>
    public string Token { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// User ID who sent the invitation
    /// </summary>
    public string InvitedBy { get; set; } = string.Empty;

    /// <summary>
    /// Status (pending, accepted, expired, revoked)
    /// </summary>
    public string Status { get; set; } = "pending";

    /// <summary>
    /// Optional message to invitee
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// When the invitation was sent
    /// </summary>
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the invitation expires
    /// </summary>
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(7);

    /// <summary>
    /// When the invitation was accepted (if accepted)
    /// </summary>
    public DateTime? AcceptedAt { get; set; }

    /// <summary>
    /// Number of times invitation was resent
    /// </summary>
    public int ResendCount { get; set; } = 0;
}

/// <summary>
/// Team roles
/// </summary>
public static class TeamRoles
{
    public const string Owner = "Owner";
    public const string Admin = "Admin";
    public const string Member = "Member";
    public const string Viewer = "Viewer";
}

/// <summary>
/// Team member status values
/// </summary>
public static class TeamMemberStatus
{
    public const string Active = "active";
    public const string Invited = "invited";
    public const string Suspended = "suspended";
    public const string Removed = "removed";
}
