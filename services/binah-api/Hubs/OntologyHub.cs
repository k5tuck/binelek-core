using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Binah.Api.Hubs
{
    /// <summary>
    /// SignalR hub for real-time ontology evolution notifications.
    ///
    /// Events:
    /// - EvolutionTriggered: New data batch triggered evolution analysis
    /// - ChangeDetected: Ontology change detected
    /// - OntologyEvolved: Ontology successfully evolved to new version
    /// - EvolutionFailed: Evolution failed (with error details)
    /// - ReviewRequired: Change queued for manual review
    ///
    /// Clients subscribe to tenant-specific groups to receive only their events.
    /// </summary>
    public class OntologyHub : Hub
    {
        private readonly ILogger<OntologyHub> _logger;

        public OntologyHub(ILogger<OntologyHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Client joins tenant-specific group to receive evolution events.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        public async Task JoinTenantGroup(string tenantId)
        {
            try
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, tenantId);
                _logger.LogInformation(
                    "Client {ConnectionId} joined tenant group {TenantId}",
                    Context.ConnectionId,
                    tenantId
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error adding client {ConnectionId} to tenant group {TenantId}",
                    Context.ConnectionId,
                    tenantId
                );
                throw;
            }
        }

        /// <summary>
        /// Client leaves tenant-specific group.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        public async Task LeaveTenantGroup(string tenantId)
        {
            try
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, tenantId);
                _logger.LogInformation(
                    "Client {ConnectionId} left tenant group {TenantId}",
                    Context.ConnectionId,
                    tenantId
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error removing client {ConnectionId} from tenant group {TenantId}",
                    Context.ConnectionId,
                    tenantId
                );
                throw;
            }
        }

        /// <summary>
        /// Notify clients that evolution was triggered.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="batchId">Data batch identifier.</param>
        /// <param name="entityType">Entity type being analyzed.</param>
        public async Task NotifyEvolutionTriggered(string tenantId, string batchId, string entityType)
        {
            try
            {
                _logger.LogDebug(
                    "Notifying evolution triggered for tenant {TenantId}, batch {BatchId}",
                    tenantId,
                    batchId
                );

                await Clients.Group(tenantId).SendAsync("EvolutionTriggered", new
                {
                    tenantId,
                    batchId,
                    entityType,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error notifying evolution triggered for tenant {TenantId}",
                    tenantId
                );
            }
        }

        /// <summary>
        /// Notify clients that a change was detected.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="changeDetails">Change details object.</param>
        public async Task NotifyChangeDetected(string tenantId, object changeDetails)
        {
            try
            {
                _logger.LogDebug(
                    "Notifying change detected for tenant {TenantId}",
                    tenantId
                );

                await Clients.Group(tenantId).SendAsync("ChangeDetected", new
                {
                    tenantId,
                    change = changeDetails,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error notifying change detected for tenant {TenantId}",
                    tenantId
                );
            }
        }

        /// <summary>
        /// Notify clients that ontology successfully evolved to new version.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="newVersion">New ontology version.</param>
        /// <param name="changeCount">Number of changes applied.</param>
        public async Task NotifyOntologyEvolved(string tenantId, string newVersion, int changeCount)
        {
            try
            {
                _logger.LogInformation(
                    "Notifying ontology evolved for tenant {TenantId} to version {NewVersion}",
                    tenantId,
                    newVersion
                );

                await Clients.Group(tenantId).SendAsync("OntologyEvolved", new
                {
                    tenantId,
                    newVersion,
                    changeCount,
                    timestamp = DateTime.UtcNow,
                    message = $"Ontology evolved to version {newVersion} with {changeCount} changes"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error notifying ontology evolved for tenant {TenantId}",
                    tenantId
                );
            }
        }

        /// <summary>
        /// Notify clients that evolution failed.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="errorMessage">Error message.</param>
        /// <param name="batchId">Data batch identifier.</param>
        public async Task NotifyEvolutionFailed(string tenantId, string errorMessage, string batchId)
        {
            try
            {
                _logger.LogWarning(
                    "Notifying evolution failed for tenant {TenantId}, batch {BatchId}: {Error}",
                    tenantId,
                    batchId,
                    errorMessage
                );

                await Clients.Group(tenantId).SendAsync("EvolutionFailed", new
                {
                    tenantId,
                    batchId,
                    error = errorMessage,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error notifying evolution failed for tenant {TenantId}",
                    tenantId
                );
            }
        }

        /// <summary>
        /// Notify clients that a change requires manual review.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="reviewItem">Review queue item details.</param>
        public async Task NotifyReviewRequired(string tenantId, object reviewItem)
        {
            try
            {
                _logger.LogInformation(
                    "Notifying review required for tenant {TenantId}",
                    tenantId
                );

                await Clients.Group(tenantId).SendAsync("ReviewRequired", new
                {
                    tenantId,
                    review = reviewItem,
                    timestamp = DateTime.UtcNow,
                    message = "A new ontology change requires your review"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error notifying review required for tenant {TenantId}",
                    tenantId
                );
            }
        }

        /// <summary>
        /// Notify clients that a review was approved and change was applied.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="reviewId">Review queue item ID.</param>
        /// <param name="newVersion">New ontology version.</param>
        public async Task NotifyReviewApproved(string tenantId, string reviewId, string newVersion)
        {
            try
            {
                _logger.LogInformation(
                    "Notifying review approved for tenant {TenantId}, review {ReviewId}",
                    tenantId,
                    reviewId
                );

                await Clients.Group(tenantId).SendAsync("ReviewApproved", new
                {
                    tenantId,
                    reviewId,
                    newVersion,
                    timestamp = DateTime.UtcNow,
                    message = $"Review approved. Ontology updated to version {newVersion}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error notifying review approved for tenant {TenantId}",
                    tenantId
                );
            }
        }

        /// <summary>
        /// Notify clients that a review was rejected.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="reviewId">Review queue item ID.</param>
        /// <param name="reason">Rejection reason.</param>
        public async Task NotifyReviewRejected(string tenantId, string reviewId, string reason)
        {
            try
            {
                _logger.LogInformation(
                    "Notifying review rejected for tenant {TenantId}, review {ReviewId}",
                    tenantId,
                    reviewId
                );

                await Clients.Group(tenantId).SendAsync("ReviewRejected", new
                {
                    tenantId,
                    reviewId,
                    reason,
                    timestamp = DateTime.UtcNow,
                    message = "Review rejected. Ontology unchanged."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error notifying review rejected for tenant {TenantId}",
                    tenantId
                );
            }
        }

        /// <summary>
        /// Called when a client connects to the hub.
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation(
                "Client {ConnectionId} connected to OntologyHub",
                Context.ConnectionId
            );
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Called when a client disconnects from the hub.
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (exception != null)
            {
                _logger.LogWarning(
                    exception,
                    "Client {ConnectionId} disconnected with error",
                    Context.ConnectionId
                );
            }
            else
            {
                _logger.LogInformation(
                    "Client {ConnectionId} disconnected",
                    Context.ConnectionId
                );
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
