using Binah.Ontology.Models.Action;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Binah.Ontology.Services;

/// <summary>
/// Service interface for managing workflow actions
/// </summary>
public interface IActionService
{
    /// <summary>
    /// Creates a new action
    /// </summary>
    Task<ActionResponse> CreateActionAsync(CreateActionRequest request, string tenantId, string? createdBy);

    /// <summary>
    /// Retrieves an action by ID
    /// </summary>
    Task<ActionResponse?> GetActionByIdAsync(string actionId, string tenantId);

    /// <summary>
    /// Retrieves actions for a tenant with optional filtering
    /// </summary>
    Task<List<ActionResponse>> GetActionsAsync(string tenantId, int skip, int limit, ActionStatus? status = null);

    /// <summary>
    /// Updates an existing action
    /// </summary>
    Task<ActionResponse?> UpdateActionAsync(string actionId, UpdateActionRequest request, string tenantId, string? updatedBy);

    /// <summary>
    /// Deletes an action (soft delete)
    /// </summary>
    Task<bool> DeleteActionAsync(string actionId, string tenantId, string? deletedBy);

    /// <summary>
    /// Manually runs an action
    /// </summary>
    Task<ActionRunResponse?> RunActionAsync(string actionId, string tenantId, string? triggeredBy, Dictionary<string, object>? inputData = null);

    /// <summary>
    /// Pauses an action
    /// </summary>
    Task<ActionResponse?> PauseActionAsync(string actionId, string tenantId, string? updatedBy);

    /// <summary>
    /// Resumes a paused action
    /// </summary>
    Task<ActionResponse?> ResumeActionAsync(string actionId, string tenantId, string? updatedBy);

    /// <summary>
    /// Gets execution history for an action
    /// </summary>
    Task<List<ActionRunResponse>> GetActionRunsAsync(string actionId, string tenantId, int skip, int limit);
}
