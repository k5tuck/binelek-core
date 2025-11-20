using Binah.Ontology.Models.Action;
using System.Collections.Generic;
using System.Threading.Tasks;
using ActionModel = Binah.Ontology.Models.Action.Action;

namespace Binah.Ontology.Repositories;

/// <summary>
/// Repository interface for action data access
/// </summary>
public interface IActionRepository
{
    /// <summary>
    /// Creates a new action
    /// </summary>
    Task<ActionModel> CreateAsync(ActionModel action);

    /// <summary>
    /// Retrieves an action by ID
    /// </summary>
    Task<ActionModel?> GetByIdAsync(string actionId, string tenantId);

    /// <summary>
    /// Retrieves actions for a tenant
    /// </summary>
    Task<List<ActionModel>> GetByTenantAsync(string tenantId, int skip, int limit, ActionStatus? status = null);

    /// <summary>
    /// Updates an action
    /// </summary>
    Task<ActionModel> UpdateAsync(ActionModel action);

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
