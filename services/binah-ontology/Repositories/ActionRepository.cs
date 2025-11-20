using Binah.Ontology.Data;
using Binah.Ontology.Models.Action;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Binah.Ontology.Repositories;

/// <summary>
/// PostgreSQL repository implementation for action data access
/// </summary>
public class ActionRepository : IActionRepository
{
    private readonly OntologyDbContext _context;
    private readonly ILogger<ActionRepository> _logger;

    public ActionRepository(
        OntologyDbContext context,
        ILogger<ActionRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Action> CreateAsync(Action action)
    {
        _context.Actions.Add(action);
        await _context.SaveChangesAsync();
        return action;
    }

    public async Task<Action?> GetByIdAsync(string actionId, string tenantId)
    {
        return await _context.Actions
            .FirstOrDefaultAsync(a => a.Id == actionId && a.TenantId == tenantId && !a.IsDeleted);
    }

    public async Task<List<Action>> GetByTenantAsync(string tenantId, int skip, int limit, ActionStatus? status = null)
    {
        var query = _context.Actions
            .Where(a => a.TenantId == tenantId && !a.IsDeleted);

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip(skip)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<Action> UpdateAsync(Action action)
    {
        _context.Actions.Update(action);
        await _context.SaveChangesAsync();
        return action;
    }

    public async Task<ActionRun> CreateRunAsync(ActionRun run)
    {
        _context.ActionRuns.Add(run);
        await _context.SaveChangesAsync();
        return run;
    }

    public async Task<ActionRun> UpdateRunAsync(ActionRun run)
    {
        _context.ActionRuns.Update(run);
        await _context.SaveChangesAsync();
        return run;
    }

    public async Task<List<ActionRun>> GetRunsAsync(string actionId, string tenantId, int skip, int limit)
    {
        return await _context.ActionRuns
            .Where(r => r.ActionId == actionId && r.TenantId == tenantId)
            .OrderByDescending(r => r.StartedAt)
            .Skip(skip)
            .Take(limit)
            .ToListAsync();
    }
}
