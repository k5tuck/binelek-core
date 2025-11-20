using Binah.Ontology.Models.Action;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Binah.Ontology.Repositories;

/// <summary>
/// Repository interface for action data access
/// </summary>
public interface IActionRepository
{
    /// <summary>
    /// Creates a new action
    /// </summary>
    Task<Action> CreateAsync(Action action);

    /// <summary>
    /// Retrieves an action by ID
    /// </summary>
    Task<Action?> GetByIdAsync(string actionId, string tenantId);

    /// <summary>
    /// Retrieves actions for a tenant
    /// </summary>
    Task<List<Action>> GetByTenantAsync(string tenantId, int skip, int limit, ActionStatus? status = null);

    /// <summary>
    /// Updates an action
    /// </summary>
    Task<Action> UpdateAsync(Action action);

    /// <summary>
    /// Creates an action run
    /// </summary>
    Task<ActionRun> CreateRunAsync(ActionRun run);

    /// <summary>
    /// Updates an action run
    /// </summary>
    Task<ActionRun> UpdateRunAsync(ActionRun run);

    /// <summary>
    /// Gets runs for an action
    /// </summary>
    Task<List<ActionRun>> GetRunsAsync(string actionId, string tenantId, int skip, int limit);
}
