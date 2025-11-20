using Binah.Ontology.Data;
using Binah.Ontology.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Binah.Ontology.Repositories;

/// <summary>
/// PostgreSQL repository implementation for canvas data access
/// </summary>
public class CanvasRepository : ICanvasRepository
{
    private readonly OntologyDbContext _dbContext;
    private readonly ILogger<CanvasRepository> _logger;

    public CanvasRepository(OntologyDbContext dbContext, ILogger<CanvasRepository> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<Canvas> CreateAsync(Canvas canvas)
    {
        if (canvas == null)
            throw new ArgumentNullException(nameof(canvas));

        canvas.CreatedAt = DateTime.UtcNow;
        canvas.UpdatedAt = DateTime.UtcNow;

        _dbContext.Canvases.Add(canvas);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Created canvas {CanvasId} for tenant {TenantId}", canvas.Id, canvas.TenantId);

        return canvas;
    }

    /// <inheritdoc/>
    public async Task<Canvas?> GetByIdAsync(Guid id, Guid tenantId)
    {
        return await _dbContext.Canvases
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId);
    }

    /// <inheritdoc/>
    public async Task<List<Canvas>> GetByTenantAsync(Guid tenantId, int skip = 0, int limit = 100)
    {
        return await _dbContext.Canvases
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId)
            .OrderByDescending(c => c.UpdatedAt)
            .Skip(skip)
            .Take(limit)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<List<Canvas>> GetByUserAsync(Guid tenantId, Guid userId, int skip = 0, int limit = 100)
    {
        return await _dbContext.Canvases
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.CreatedBy == userId)
            .OrderByDescending(c => c.UpdatedAt)
            .Skip(skip)
            .Take(limit)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<List<Canvas>> GetSharedWithUserAsync(Guid tenantId, Guid userId, int skip = 0, int limit = 100)
    {
        return await _dbContext.Canvases
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.IsShared && c.SharedWith.Contains(userId))
            .OrderByDescending(c => c.UpdatedAt)
            .Skip(skip)
            .Take(limit)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<Canvas> UpdateAsync(Canvas canvas)
    {
        if (canvas == null)
            throw new ArgumentNullException(nameof(canvas));

        var existing = await _dbContext.Canvases
            .FirstOrDefaultAsync(c => c.Id == canvas.Id && c.TenantId == canvas.TenantId);

        if (existing == null)
        {
            throw new InvalidOperationException($"Canvas with ID '{canvas.Id}' not found for tenant '{canvas.TenantId}'");
        }

        // Update properties
        existing.Name = canvas.Name;
        existing.Description = canvas.Description;
        existing.Entities = canvas.Entities;
        existing.Connections = canvas.Connections;
        existing.Viewport = canvas.Viewport;
        existing.IsShared = canvas.IsShared;
        existing.SharedWith = canvas.SharedWith;
        existing.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Updated canvas {CanvasId} for tenant {TenantId}", canvas.Id, canvas.TenantId);

        return existing;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(Guid id, Guid tenantId)
    {
        var canvas = await _dbContext.Canvases
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId);

        if (canvas == null)
        {
            return false;
        }

        _dbContext.Canvases.Remove(canvas);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Deleted canvas {CanvasId} for tenant {TenantId}", id, tenantId);

        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(Guid id, Guid tenantId)
    {
        return await _dbContext.Canvases
            .AnyAsync(c => c.Id == id && c.TenantId == tenantId);
    }
}
