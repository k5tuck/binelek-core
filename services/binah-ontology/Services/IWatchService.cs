using Binah.Ontology.Models.Watch;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Binah.Ontology.Services;

/// <summary>
/// Service interface for managing entity watches
/// </summary>
public interface IWatchService
{
    /// <summary>
    /// Creates a new watch
    /// </summary>
    Task<WatchResponse> CreateWatchAsync(CreateWatchRequest request, string tenantId, string? createdBy);

    /// <summary>
    /// Retrieves a watch by ID
    /// </summary>
    Task<WatchResponse?> GetWatchByIdAsync(string watchId, string tenantId);

    /// <summary>
    /// Retrieves watches for a tenant with optional filtering
    /// </summary>
    Task<List<WatchResponse>> GetWatchesAsync(string tenantId, int skip, int limit, WatchStatus? status = null);

    /// <summary>
    /// Updates an existing watch
    /// </summary>
    Task<WatchResponse?> UpdateWatchAsync(string watchId, UpdateWatchRequest request, string tenantId, string? updatedBy);

    /// <summary>
    /// Deletes a watch (soft delete)
    /// </summary>
    Task<bool> DeleteWatchAsync(string watchId, string tenantId, string? deletedBy);

    /// <summary>
    /// Pauses a watch
    /// </summary>
    Task<WatchResponse?> PauseWatchAsync(string watchId, string tenantId, string? updatedBy);

    /// <summary>
    /// Resumes a paused watch
    /// </summary>
    Task<WatchResponse?> ResumeWatchAsync(string watchId, string tenantId, string? updatedBy);

    /// <summary>
    /// Gets entities being watched
    /// </summary>
    Task<List<WatchEntityResponse>> GetWatchEntitiesAsync(string watchId, string tenantId, int skip, int limit);

    /// <summary>
    /// Adds an entity to the watch
    /// </summary>
    Task<WatchEntityResponse?> AddWatchEntityAsync(string watchId, AddWatchEntityRequest request, string tenantId, string? addedBy);

    /// <summary>
    /// Removes an entity from the watch
    /// </summary>
    Task<bool> RemoveWatchEntityAsync(string watchId, string entityId, string tenantId);

    /// <summary>
    /// Gets trigger history for a watch
    /// </summary>
    Task<List<WatchTriggerResponse>> GetWatchTriggersAsync(string watchId, string tenantId, int skip, int limit);
}
