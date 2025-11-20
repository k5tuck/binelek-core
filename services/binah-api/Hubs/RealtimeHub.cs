using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Binah.API.Hubs;

/// <summary>
/// SignalR Hub for real-time communication between backend and frontend
/// Supports multi-tenant isolation and role-based broadcasting
/// </summary>
[Authorize]
public class RealtimeHub : Hub
{
    private readonly ILogger<RealtimeHub> _logger;

    public RealtimeHub(ILogger<RealtimeHub> _logger)
    {
        this._logger = _logger;
    }

    /// <summary>
    /// Called when a client connects
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var tenantId = Context.User?.FindFirst("TenantId")?.Value;

        _logger.LogInformation("User {UserId} from tenant {TenantId} connected. ConnectionId: {ConnectionId}",
            userId, tenantId, Context.ConnectionId);

        // Add to tenant-specific group for multi-tenant isolation
        if (!string.IsNullOrEmpty(tenantId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant_{tenantId}");
            _logger.LogDebug("Added connection to tenant group: tenant_{TenantId}", tenantId);
        }

        // Add to user-specific group for personal notifications
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var tenantId = Context.User?.FindFirst("TenantId")?.Value;

        _logger.LogInformation("User {UserId} from tenant {TenantId} disconnected. ConnectionId: {ConnectionId}",
            userId, tenantId, Context.ConnectionId);

        if (exception != null)
        {
            _logger.LogError(exception, "Connection disconnected with error");
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Client joins a specific channel/room
    /// </summary>
    public async Task JoinChannel(string channelName)
    {
        var tenantId = Context.User?.FindFirst("TenantId")?.Value;

        // Ensure channel is tenant-scoped
        var scopedChannel = $"tenant_{tenantId}_{channelName}";

        await Groups.AddToGroupAsync(Context.ConnectionId, scopedChannel);
        _logger.LogInformation("Connection {ConnectionId} joined channel {Channel}",
            Context.ConnectionId, scopedChannel);

        await Clients.Group(scopedChannel).SendAsync("UserJoined", new
        {
            ConnectionId = Context.ConnectionId,
            Channel = channelName,
            JoinedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Client leaves a specific channel/room
    /// </summary>
    public async Task LeaveChannel(string channelName)
    {
        var tenantId = Context.User?.FindFirst("TenantId")?.Value;
        var scopedChannel = $"tenant_{tenantId}_{channelName}";

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, scopedChannel);
        _logger.LogInformation("Connection {ConnectionId} left channel {Channel}",
            Context.ConnectionId, scopedChannel);

        await Clients.Group(scopedChannel).SendAsync("UserLeft", new
        {
            ConnectionId = Context.ConnectionId,
            Channel = channelName,
            LeftAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Ping/Pong for connection health check
    /// </summary>
    public async Task Ping()
    {
        await Clients.Caller.SendAsync("Pong", new
        {
            ServerTime = DateTime.UtcNow,
            ConnectionId = Context.ConnectionId
        });
    }
}

/// <summary>
/// Service for broadcasting real-time events to connected clients
/// </summary>
public interface IRealtimeNotificationService
{
    Task NotifyPipelineStatusChanged(string tenantId, string pipelineId, string status, object? metadata = null);
    Task NotifyOntologyEntityCreated(string tenantId, string entityId, string entityType, object? entity = null);
    Task NotifyOntologyEntityUpdated(string tenantId, string entityId, string entityType, object? changes = null);
    Task NotifyOntologyEntityDeleted(string tenantId, string entityId, string entityType);
    Task NotifyUserToUser(string userId, string eventType, object? data = null);
    Task NotifyTenant(string tenantId, string eventType, object? data = null);

    // Ontology Discovery Events (Phase 3: Manual Review)
    Task NotifyReviewRequired(string tenantId, object reviewItem);
    Task NotifyOntologyUpdated(string tenantId, object changeDetails);
    Task NotifyReviewApproved(string tenantId, string reviewId, object? details = null);
    Task NotifyReviewRejected(string tenantId, string reviewId, object? details = null);
}

public class RealtimeNotificationService : IRealtimeNotificationService
{
    private readonly IHubContext<RealtimeHub> _hubContext;
    private readonly ILogger<RealtimeNotificationService> _logger;

    public RealtimeNotificationService(
        IHubContext<RealtimeHub> hubContext,
        ILogger<RealtimeNotificationService> logger)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task NotifyPipelineStatusChanged(string tenantId, string pipelineId, string status, object? metadata = null)
    {
        _logger.LogInformation("Broadcasting pipeline status change: Pipeline {PipelineId} → {Status}",
            pipelineId, status);

        await _hubContext.Clients.Group($"tenant_{tenantId}").SendAsync("PipelineStatusChanged", new
        {
            PipelineId = pipelineId,
            Status = status,
            Timestamp = DateTime.UtcNow,
            Metadata = metadata
        });
    }

    public async Task NotifyOntologyEntityCreated(string tenantId, string entityId, string entityType, object? entity = null)
    {
        _logger.LogInformation("Broadcasting entity created: {EntityType} {EntityId}",
            entityType, entityId);

        await _hubContext.Clients.Group($"tenant_{tenantId}").SendAsync("OntologyEntityCreated", new
        {
            EntityId = entityId,
            EntityType = entityType,
            Timestamp = DateTime.UtcNow,
            Entity = entity
        });
    }

    public async Task NotifyOntologyEntityUpdated(string tenantId, string entityId, string entityType, object? changes = null)
    {
        _logger.LogInformation("Broadcasting entity updated: {EntityType} {EntityId}",
            entityType, entityId);

        await _hubContext.Clients.Group($"tenant_{tenantId}").SendAsync("OntologyEntityUpdated", new
        {
            EntityId = entityId,
            EntityType = entityType,
            Timestamp = DateTime.UtcNow,
            Changes = changes
        });
    }

    public async Task NotifyOntologyEntityDeleted(string tenantId, string entityId, string entityType)
    {
        _logger.LogInformation("Broadcasting entity deleted: {EntityType} {EntityId}",
            entityType, entityId);

        await _hubContext.Clients.Group($"tenant_{tenantId}").SendAsync("OntologyEntityDeleted", new
        {
            EntityId = entityId,
            EntityType = entityType,
            Timestamp = DateTime.UtcNow
        });
    }

    public async Task NotifyUserToUser(string userId, string eventType, object? data = null)
    {
        _logger.LogInformation("Sending notification to user {UserId}: {EventType}",
            userId, eventType);

        await _hubContext.Clients.Group($"user_{userId}").SendAsync("UserNotification", new
        {
            EventType = eventType,
            Timestamp = DateTime.UtcNow,
            Data = data
        });
    }

    public async Task NotifyTenant(string tenantId, string eventType, object? data = null)
    {
        _logger.LogInformation("Broadcasting to tenant {TenantId}: {EventType}",
            tenantId, eventType);

        await _hubContext.Clients.Group($"tenant_{tenantId}").SendAsync("TenantNotification", new
        {
            EventType = eventType,
            Timestamp = DateTime.UtcNow,
            Data = data
        });
    }

    // Ontology Discovery Events (Phase 3: Manual Review)

    public async Task NotifyReviewRequired(string tenantId, object reviewItem)
    {
        _logger.LogInformation("Broadcasting review required for tenant {TenantId}", tenantId);

        await _hubContext.Clients.Group($"tenant_{tenantId}").SendAsync("ReviewRequired", new
        {
            Timestamp = DateTime.UtcNow,
            ReviewItem = reviewItem
        });
    }

    public async Task NotifyOntologyUpdated(string tenantId, object changeDetails)
    {
        _logger.LogInformation("Broadcasting ontology updated for tenant {TenantId}", tenantId);

        await _hubContext.Clients.Group($"tenant_{tenantId}").SendAsync("OntologyUpdated", new
        {
            Timestamp = DateTime.UtcNow,
            ChangeDetails = changeDetails
        });
    }

    public async Task NotifyReviewApproved(string tenantId, string reviewId, object? details = null)
    {
        _logger.LogInformation("Broadcasting review approved: {ReviewId} for tenant {TenantId}",
            reviewId, tenantId);

        await _hubContext.Clients.Group($"tenant_{tenantId}").SendAsync("ReviewApproved", new
        {
            ReviewId = reviewId,
            Timestamp = DateTime.UtcNow,
            Details = details
        });
    }

    public async Task NotifyReviewRejected(string tenantId, string reviewId, object? details = null)
    {
        _logger.LogInformation("Broadcasting review rejected: {ReviewId} for tenant {TenantId}",
            reviewId, tenantId);

        await _hubContext.Clients.Group($"tenant_{tenantId}").SendAsync("ReviewRejected", new
        {
            ReviewId = reviewId,
            Timestamp = DateTime.UtcNow,
            Details = details
        });
    }
}
