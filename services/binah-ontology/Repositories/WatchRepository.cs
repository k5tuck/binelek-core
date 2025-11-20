using Binah.Ontology.Data;
using Binah.Ontology.Models.Watch;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Binah.Ontology.Repositories;

/// <summary>
/// PostgreSQL repository implementation for watch data access
/// </summary>
public class WatchRepository : IWatchRepository
{
    private readonly OntologyDbContext _context;
    private readonly ILogger<WatchRepository> _logger;

    public WatchRepository(
        OntologyDbContext context,
        ILogger<WatchRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Watch> CreateAsync(Watch watch)
    {
        _context.Watches.Add(watch);
        await _context.SaveChangesAsync();
        return watch;
    }

    public async Task<Watch?> GetByIdAsync(string watchId, string tenantId)
    {
        return await _context.Watches
            .FirstOrDefaultAsync(w => w.Id == watchId && w.TenantId == tenantId && !w.IsDeleted);
    }

    public async Task<List<Watch>> GetByTenantAsync(string tenantId, int skip, int limit, WatchStatus? status = null)
    {
        var query = _context.Watches
            .Where(w => w.TenantId == tenantId && !w.IsDeleted);

        if (status.HasValue)
        {
            query = query.Where(w => w.Status == status.Value);
        }

        return await query
            .OrderByDescending(w => w.CreatedAt)
            .Skip(skip)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<Watch> UpdateAsync(Watch watch)
    {
        _context.Watches.Update(watch);
        await _context.SaveChangesAsync();
        return watch;
    }

    public async Task<List<WatchEntity>> GetWatchEntitiesAsync(string watchId, string tenantId, int skip, int limit)
    {
        return await _context.WatchEntities
            .Where(e => e.WatchId == watchId && e.TenantId == tenantId)
            .OrderByDescending(e => e.AddedAt)
            .Skip(skip)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<WatchEntity> AddWatchEntityAsync(WatchEntity watchEntity)
    {
        _context.WatchEntities.Add(watchEntity);
        await _context.SaveChangesAsync();
        return watchEntity;
    }

    public async Task<bool> RemoveWatchEntityAsync(string watchId, string entityId, string tenantId)
    {
        var entity = await _context.WatchEntities
            .FirstOrDefaultAsync(e => e.WatchId == watchId && e.EntityId == entityId && e.TenantId == tenantId);

        if (entity == null) return false;

        _context.WatchEntities.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<WatchTrigger>> GetWatchTriggersAsync(string watchId, string tenantId, int skip, int limit)
    {
        return await _context.WatchTriggers
            .Where(t => t.WatchId == watchId && t.TenantId == tenantId)
            .OrderByDescending(t => t.TriggeredAt)
            .Skip(skip)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<WatchTrigger> CreateTriggerAsync(WatchTrigger trigger)
    {
        _context.WatchTriggers.Add(trigger);
        await _context.SaveChangesAsync();
        return trigger;
    }
}
