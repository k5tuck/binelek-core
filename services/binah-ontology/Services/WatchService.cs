using Binah.Ontology.Models.Watch;
using Binah.Ontology.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Binah.Ontology.Services;

/// <summary>
/// Implementation of watch management service
/// </summary>
public class WatchService : IWatchService
{
    private readonly IWatchRepository _watchRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<WatchService> _logger;

    public WatchService(
        IWatchRepository watchRepository,
        IEventPublisher eventPublisher,
        ILogger<WatchService> logger)
    {
        _watchRepository = watchRepository ?? throw new ArgumentNullException(nameof(watchRepository));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<WatchResponse> CreateWatchAsync(CreateWatchRequest request, string tenantId, string? createdBy)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Watch name cannot be empty", nameof(request));
        }

        var watch = new Watch
        {
            Id = $"watch-{Guid.NewGuid().ToString("N")[..12]}",
            Name = request.Name,
            Description = request.Description,
            EntityTypes = request.EntityTypes != null
                ? JsonSerializer.Serialize(request.EntityTypes)
                : null,
            Condition = JsonSerializer.Serialize(request.Condition),
            NotificationConfig = JsonSerializer.Serialize(request.NotificationConfig),
            CheckIntervalMinutes = request.CheckIntervalMinutes,
            Severity = request.Severity,
            TenantId = tenantId,
            CreatedBy = createdBy,
            Status = WatchStatus.Active
        };

        var created = await _watchRepository.CreateAsync(watch);
        _logger.LogInformation("Created watch {WatchId} for tenant {TenantId}", created.Id, tenantId);

        return MapToResponse(created);
    }

    public async Task<WatchResponse?> GetWatchByIdAsync(string watchId, string tenantId)
    {
        var watch = await _watchRepository.GetByIdAsync(watchId, tenantId);
        return watch != null ? MapToResponse(watch) : null;
    }

    public async Task<List<WatchResponse>> GetWatchesAsync(string tenantId, int skip, int limit, WatchStatus? status = null)
    {
        var watches = await _watchRepository.GetByTenantAsync(tenantId, skip, limit, status);
        return watches.Select(MapToResponse).ToList();
    }

    public async Task<WatchResponse?> UpdateWatchAsync(string watchId, UpdateWatchRequest request, string tenantId, string? updatedBy)
    {
        var watch = await _watchRepository.GetByIdAsync(watchId, tenantId);
        if (watch == null) return null;

        if (!string.IsNullOrWhiteSpace(request.Name))
            watch.Name = request.Name;

        if (request.Description != null)
            watch.Description = request.Description;

        if (request.EntityTypes != null)
            watch.EntityTypes = JsonSerializer.Serialize(request.EntityTypes);

        if (request.Condition != null)
            watch.Condition = JsonSerializer.Serialize(request.Condition);

        if (request.NotificationConfig != null)
            watch.NotificationConfig = JsonSerializer.Serialize(request.NotificationConfig);

        if (request.CheckIntervalMinutes.HasValue)
            watch.CheckIntervalMinutes = request.CheckIntervalMinutes.Value;

        if (request.Severity.HasValue)
            watch.Severity = request.Severity.Value;

        watch.UpdatedAt = DateTime.UtcNow;
        watch.UpdatedBy = updatedBy;

        var updated = await _watchRepository.UpdateAsync(watch);
        _logger.LogInformation("Updated watch {WatchId}", watchId);

        return MapToResponse(updated);
    }

    public async Task<bool> DeleteWatchAsync(string watchId, string tenantId, string? deletedBy)
    {
        var watch = await _watchRepository.GetByIdAsync(watchId, tenantId);
        if (watch == null) return false;

        watch.IsDeleted = true;
        watch.DeletedAt = DateTime.UtcNow;
        watch.DeletedBy = deletedBy;
        watch.Status = WatchStatus.Disabled;

        await _watchRepository.UpdateAsync(watch);
        _logger.LogInformation("Deleted watch {WatchId}", watchId);

        return true;
    }

    public async Task<WatchResponse?> PauseWatchAsync(string watchId, string tenantId, string? updatedBy)
    {
        var watch = await _watchRepository.GetByIdAsync(watchId, tenantId);
        if (watch == null) return null;

        watch.Status = WatchStatus.Paused;
        watch.UpdatedAt = DateTime.UtcNow;
        watch.UpdatedBy = updatedBy;

        var updated = await _watchRepository.UpdateAsync(watch);
        _logger.LogInformation("Paused watch {WatchId}", watchId);

        return MapToResponse(updated);
    }

    public async Task<WatchResponse?> ResumeWatchAsync(string watchId, string tenantId, string? updatedBy)
    {
        var watch = await _watchRepository.GetByIdAsync(watchId, tenantId);
        if (watch == null) return null;

        watch.Status = WatchStatus.Active;
        watch.UpdatedAt = DateTime.UtcNow;
        watch.UpdatedBy = updatedBy;

        var updated = await _watchRepository.UpdateAsync(watch);
        _logger.LogInformation("Resumed watch {WatchId}", watchId);

        return MapToResponse(updated);
    }

    public async Task<List<WatchEntityResponse>> GetWatchEntitiesAsync(string watchId, string tenantId, int skip, int limit)
    {
        var entities = await _watchRepository.GetWatchEntitiesAsync(watchId, tenantId, skip, limit);
        return entities.Select(MapEntityToResponse).ToList();
    }

    public async Task<WatchEntityResponse?> AddWatchEntityAsync(string watchId, AddWatchEntityRequest request, string tenantId, string? addedBy)
    {
        var watch = await _watchRepository.GetByIdAsync(watchId, tenantId);
        if (watch == null) return null;

        var watchEntity = new WatchEntity
        {
            Id = $"we-{Guid.NewGuid().ToString("N")[..12]}",
            WatchId = watchId,
            EntityId = request.EntityId,
            EntityType = request.EntityType,
            TenantId = tenantId,
            AddedBy = addedBy,
            AddedAt = DateTime.UtcNow
        };

        var created = await _watchRepository.AddWatchEntityAsync(watchEntity);

        // Update watched entity count
        watch.WatchedEntityCount++;
        await _watchRepository.UpdateAsync(watch);

        _logger.LogInformation("Added entity {EntityId} to watch {WatchId}", request.EntityId, watchId);

        return MapEntityToResponse(created);
    }

    public async Task<bool> RemoveWatchEntityAsync(string watchId, string entityId, string tenantId)
    {
        var watch = await _watchRepository.GetByIdAsync(watchId, tenantId);
        if (watch == null) return false;

        var result = await _watchRepository.RemoveWatchEntityAsync(watchId, entityId, tenantId);

        if (result)
        {
            watch.WatchedEntityCount = Math.Max(0, watch.WatchedEntityCount - 1);
            await _watchRepository.UpdateAsync(watch);
            _logger.LogInformation("Removed entity {EntityId} from watch {WatchId}", entityId, watchId);
        }

        return result;
    }

    public async Task<List<WatchTriggerResponse>> GetWatchTriggersAsync(string watchId, string tenantId, int skip, int limit)
    {
        var triggers = await _watchRepository.GetWatchTriggersAsync(watchId, tenantId, skip, limit);
        return triggers.Select(MapTriggerToResponse).ToList();
    }

    private static WatchResponse MapToResponse(Watch watch)
    {
        return new WatchResponse
        {
            Id = watch.Id,
            Name = watch.Name,
            Description = watch.Description,
            EntityTypes = !string.IsNullOrEmpty(watch.EntityTypes)
                ? JsonSerializer.Deserialize<List<string>>(watch.EntityTypes)
                : null,
            Condition = !string.IsNullOrEmpty(watch.Condition)
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(watch.Condition)
                : null,
            NotificationConfig = !string.IsNullOrEmpty(watch.NotificationConfig)
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(watch.NotificationConfig)
                : null,
            CheckIntervalMinutes = watch.CheckIntervalMinutes,
            Status = watch.Status,
            Severity = watch.Severity,
            TenantId = watch.TenantId,
            CreatedAt = watch.CreatedAt,
            CreatedBy = watch.CreatedBy,
            UpdatedAt = watch.UpdatedAt,
            UpdatedBy = watch.UpdatedBy,
            LastCheckedAt = watch.LastCheckedAt,
            LastTriggeredAt = watch.LastTriggeredAt,
            TriggerCount = watch.TriggerCount,
            WatchedEntityCount = watch.WatchedEntityCount
        };
    }

    private static WatchEntityResponse MapEntityToResponse(WatchEntity entity)
    {
        return new WatchEntityResponse
        {
            Id = entity.Id,
            WatchId = entity.WatchId,
            EntityId = entity.EntityId,
            EntityType = entity.EntityType,
            AddedAt = entity.AddedAt,
            AddedBy = entity.AddedBy,
            LastTriggeredAt = entity.LastTriggeredAt,
            TriggerCount = entity.TriggerCount
        };
    }

    private static WatchTriggerResponse MapTriggerToResponse(WatchTrigger trigger)
    {
        return new WatchTriggerResponse
        {
            Id = trigger.Id,
            WatchId = trigger.WatchId,
            EntityId = trigger.EntityId,
            EntityType = trigger.EntityType,
            ConditionMet = trigger.ConditionMet,
            PreviousValue = !string.IsNullOrEmpty(trigger.PreviousValue)
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(trigger.PreviousValue)
                : null,
            CurrentValue = !string.IsNullOrEmpty(trigger.CurrentValue)
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(trigger.CurrentValue)
                : null,
            Severity = trigger.Severity,
            TriggeredAt = trigger.TriggeredAt,
            NotificationDelivered = trigger.NotificationDelivered,
            ErrorMessage = trigger.ErrorMessage,
            Acknowledged = trigger.Acknowledged,
            AcknowledgedAt = trigger.AcknowledgedAt,
            AcknowledgedBy = trigger.AcknowledgedBy
        };
    }
}
