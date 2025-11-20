using Binah.Ontology.Models.Action;
using Binah.Ontology.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Binah.Ontology.Services;

/// <summary>
/// Implementation of action management service
/// </summary>
public class ActionService : IActionService
{
    private readonly IActionRepository _actionRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<ActionService> _logger;

    public ActionService(
        IActionRepository actionRepository,
        IEventPublisher eventPublisher,
        ILogger<ActionService> logger)
    {
        _actionRepository = actionRepository ?? throw new ArgumentNullException(nameof(actionRepository));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ActionResponse> CreateActionAsync(CreateActionRequest request, string tenantId, string? createdBy)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Action name cannot be empty", nameof(request));
        }

        var action = new Models.Action.Action
        {
            Id = $"action-{Guid.NewGuid().ToString("N")[..12]}",
            Name = request.Name,
            Description = request.Description,
            TriggerType = request.TriggerType,
            Schedule = request.Schedule,
            EventTopic = request.EventTopic,
            ConditionExpression = request.ConditionExpression,
            Configuration = request.Configuration != null
                ? JsonSerializer.Serialize(request.Configuration)
                : "{}",
            TargetEntityTypes = request.TargetEntityTypes != null
                ? JsonSerializer.Serialize(request.TargetEntityTypes)
                : null,
            TenantId = tenantId,
            CreatedBy = createdBy,
            Status = ActionStatus.Active
        };

        var created = await _actionRepository.CreateAsync(action);
        _logger.LogInformation("Created action {ActionId} for tenant {TenantId}", created.Id, tenantId);

        return MapToResponse(created);
    }

    public async Task<ActionResponse?> GetActionByIdAsync(string actionId, string tenantId)
    {
        var action = await _actionRepository.GetByIdAsync(actionId, tenantId);
        return action != null ? MapToResponse(action) : null;
    }

    public async Task<List<ActionResponse>> GetActionsAsync(string tenantId, int skip, int limit, ActionStatus? status = null)
    {
        var actions = await _actionRepository.GetByTenantAsync(tenantId, skip, limit, status);
        return actions.Select(MapToResponse).ToList();
    }

    public async Task<ActionResponse?> UpdateActionAsync(string actionId, UpdateActionRequest request, string tenantId, string? updatedBy)
    {
        var action = await _actionRepository.GetByIdAsync(actionId, tenantId);
        if (action == null) return null;

        if (!string.IsNullOrWhiteSpace(request.Name))
            action.Name = request.Name;

        if (request.Description != null)
            action.Description = request.Description;

        if (request.TriggerType.HasValue)
            action.TriggerType = request.TriggerType.Value;

        if (request.Schedule != null)
            action.Schedule = request.Schedule;

        if (request.EventTopic != null)
            action.EventTopic = request.EventTopic;

        if (request.ConditionExpression != null)
            action.ConditionExpression = request.ConditionExpression;

        if (request.Configuration != null)
            action.Configuration = JsonSerializer.Serialize(request.Configuration);

        if (request.TargetEntityTypes != null)
            action.TargetEntityTypes = JsonSerializer.Serialize(request.TargetEntityTypes);

        action.UpdatedAt = DateTime.UtcNow;
        action.UpdatedBy = updatedBy;

        var updated = await _actionRepository.UpdateAsync(action);
        _logger.LogInformation("Updated action {ActionId}", actionId);

        return MapToResponse(updated);
    }

    public async Task<bool> DeleteActionAsync(string actionId, string tenantId, string? deletedBy)
    {
        var action = await _actionRepository.GetByIdAsync(actionId, tenantId);
        if (action == null) return false;

        action.IsDeleted = true;
        action.DeletedAt = DateTime.UtcNow;
        action.DeletedBy = deletedBy;
        action.Status = ActionStatus.Disabled;

        await _actionRepository.UpdateAsync(action);
        _logger.LogInformation("Deleted action {ActionId}", actionId);

        return true;
    }

    public async Task<ActionRunResponse?> RunActionAsync(string actionId, string tenantId, string? triggeredBy, Dictionary<string, object>? inputData = null)
    {
        var action = await _actionRepository.GetByIdAsync(actionId, tenantId);
        if (action == null) return null;

        if (action.Status != ActionStatus.Active)
        {
            throw new InvalidOperationException($"Action is {action.Status} and cannot be run");
        }

        var run = new ActionRun
        {
            Id = $"run-{Guid.NewGuid().ToString("N")[..12]}",
            ActionId = actionId,
            TenantId = tenantId,
            TriggerType = ActionTriggerType.Manual,
            TriggeredBy = triggeredBy,
            InputData = inputData != null ? JsonSerializer.Serialize(inputData) : null,
            Status = ActionRunStatus.Running,
            StartedAt = DateTime.UtcNow
        };

        var createdRun = await _actionRepository.CreateRunAsync(run);

        try
        {
            // Execute action logic here
            // For now, we'll mark it as completed
            createdRun.Status = ActionRunStatus.Completed;
            createdRun.CompletedAt = DateTime.UtcNow;
            createdRun.DurationMs = (long)(createdRun.CompletedAt.Value - createdRun.StartedAt).TotalMilliseconds;
            createdRun.OutputData = JsonSerializer.Serialize(new { message = "Action executed successfully" });

            await _actionRepository.UpdateRunAsync(createdRun);

            // Update action statistics
            action.LastRunAt = DateTime.UtcNow;
            action.RunCount++;
            action.SuccessCount++;
            await _actionRepository.UpdateAsync(action);

            _logger.LogInformation("Successfully ran action {ActionId}, run {RunId}", actionId, run.Id);
        }
        catch (Exception ex)
        {
            createdRun.Status = ActionRunStatus.Failed;
            createdRun.CompletedAt = DateTime.UtcNow;
            createdRun.DurationMs = (long)(createdRun.CompletedAt.Value - createdRun.StartedAt).TotalMilliseconds;
            createdRun.ErrorMessage = ex.Message;
            createdRun.StackTrace = ex.StackTrace;

            await _actionRepository.UpdateRunAsync(createdRun);

            action.LastRunAt = DateTime.UtcNow;
            action.RunCount++;
            action.FailureCount++;
            await _actionRepository.UpdateAsync(action);

            _logger.LogError(ex, "Failed to run action {ActionId}", actionId);
        }

        return MapRunToResponse(createdRun);
    }

    public async Task<ActionResponse?> PauseActionAsync(string actionId, string tenantId, string? updatedBy)
    {
        var action = await _actionRepository.GetByIdAsync(actionId, tenantId);
        if (action == null) return null;

        action.Status = ActionStatus.Paused;
        action.UpdatedAt = DateTime.UtcNow;
        action.UpdatedBy = updatedBy;

        var updated = await _actionRepository.UpdateAsync(action);
        _logger.LogInformation("Paused action {ActionId}", actionId);

        return MapToResponse(updated);
    }

    public async Task<ActionResponse?> ResumeActionAsync(string actionId, string tenantId, string? updatedBy)
    {
        var action = await _actionRepository.GetByIdAsync(actionId, tenantId);
        if (action == null) return null;

        action.Status = ActionStatus.Active;
        action.UpdatedAt = DateTime.UtcNow;
        action.UpdatedBy = updatedBy;

        var updated = await _actionRepository.UpdateAsync(action);
        _logger.LogInformation("Resumed action {ActionId}", actionId);

        return MapToResponse(updated);
    }

    public async Task<List<ActionRunResponse>> GetActionRunsAsync(string actionId, string tenantId, int skip, int limit)
    {
        var runs = await _actionRepository.GetRunsAsync(actionId, tenantId, skip, limit);
        return runs.Select(MapRunToResponse).ToList();
    }

    private static ActionResponse MapToResponse(Models.Action.Action action)
    {
        return new ActionResponse
        {
            Id = action.Id,
            Name = action.Name,
            Description = action.Description,
            TriggerType = action.TriggerType,
            Schedule = action.Schedule,
            EventTopic = action.EventTopic,
            ConditionExpression = action.ConditionExpression,
            Configuration = !string.IsNullOrEmpty(action.Configuration)
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(action.Configuration)
                : null,
            TargetEntityTypes = !string.IsNullOrEmpty(action.TargetEntityTypes)
                ? JsonSerializer.Deserialize<List<string>>(action.TargetEntityTypes)
                : null,
            Status = action.Status,
            TenantId = action.TenantId,
            CreatedAt = action.CreatedAt,
            CreatedBy = action.CreatedBy,
            UpdatedAt = action.UpdatedAt,
            UpdatedBy = action.UpdatedBy,
            LastRunAt = action.LastRunAt,
            NextRunAt = action.NextRunAt,
            RunCount = action.RunCount,
            SuccessCount = action.SuccessCount,
            FailureCount = action.FailureCount
        };
    }

    private static ActionRunResponse MapRunToResponse(ActionRun run)
    {
        return new ActionRunResponse
        {
            Id = run.Id,
            ActionId = run.ActionId,
            Status = run.Status,
            TriggerType = run.TriggerType,
            TriggeredBy = run.TriggeredBy,
            InputData = !string.IsNullOrEmpty(run.InputData)
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(run.InputData)
                : null,
            OutputData = !string.IsNullOrEmpty(run.OutputData)
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(run.OutputData)
                : null,
            ErrorMessage = run.ErrorMessage,
            StartedAt = run.StartedAt,
            CompletedAt = run.CompletedAt,
            DurationMs = run.DurationMs,
            EntitiesAffected = run.EntitiesAffected,
            CorrelationId = run.CorrelationId
        };
    }
}
