using Binah.Ontology.Models.Watch;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Binah.Ontology.Repositories;

/// <summary>
/// Repository interface for watch data access
/// </summary>
public interface IWatchRepository
{
    /// <summary>
    /// Creates a new watch
    /// </summary>
    Task<Watch> CreateAsync(Watch watch);

    /// <summary>
    /// Retrieves a watch by ID
    /// </summary>
    Task<Watch?> GetByIdAsync(string watchId, string tenantId);

    /// <summary>
    /// Retrieves watches for a tenant
    /// </summary>
    Task<List<Watch>> GetByTenantAsync(string tenantId, int skip, int limit, WatchStatus? status = null);

    /// <summary>
    /// Updates a watch
    /// </summary>
    Task<Watch> UpdateAsync(Watch watch);

    /// <summary>
    /// Gets entities being watched
    /// </summary>
    Task<List<WatchEntity>> GetWatchEntitiesAsync(string watchId, string tenantId, int skip, int limit);

    /// <summary>
    /// Adds an entity to a watch
    /// </summary>
    Task<WatchEntity> AddWatchEntityAsync(WatchEntity watchEntity);

    /// <summary>
    /// Removes an entity from a watch
    /// </summary>
    Task<bool> RemoveWatchEntityAsync(string watchId, string entityId, string tenantId);

    /// <summary>
    /// Gets triggers for a watch
    /// </summary>
    Task<List<WatchTrigger>> GetWatchTriggersAsync(string watchId, string tenantId, int skip, int limit);

    /// <summary>
    /// Creates a watch trigger
    /// </summary>
    Task<WatchTrigger> CreateTriggerAsync(WatchTrigger trigger);
}
