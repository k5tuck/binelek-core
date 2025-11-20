using System;
using System.Threading.Tasks;
using Binah.Ontology.Data;
using Binah.Ontology.Models.Tenant;
using Binah.Ontology.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Binah.Ontology.Repositories.Implementations
{
    /// <summary>
    /// Entity Framework repository for tenant data access
    /// </summary>
    public class TenantRepository : ITenantRepository
    {
        private readonly OntologyDbContext _context;
        private readonly ILogger<TenantRepository> _logger;

        public TenantRepository(
            OntologyDbContext context,
            ILogger<TenantRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<Tenant?> GetByIdAsync(string tenantId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));

            try
            {
                return await _context.Tenants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve tenant {TenantId}", tenantId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Tenant> CreateAsync(Tenant tenant)
        {
            if (tenant == null)
                throw new ArgumentNullException(nameof(tenant));

            try
            {
                tenant.CreatedAt = DateTime.UtcNow;
                await _context.Tenants.AddAsync(tenant);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created tenant {TenantId}", tenant.Id);
                return tenant;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create tenant {TenantId}", tenant.Id);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Tenant> UpdateAsync(Tenant tenant)
        {
            if (tenant == null)
                throw new ArgumentNullException(nameof(tenant));

            try
            {
                tenant.UpdatedAt = DateTime.UtcNow;
                _context.Tenants.Update(tenant);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated tenant {TenantId}", tenant.Id);
                return tenant;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update tenant {TenantId}", tenant.Id);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(string tenantId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));

            try
            {
                var tenant = await _context.Tenants.FindAsync(tenantId);
                if (tenant == null)
                {
                    _logger.LogWarning("Tenant {TenantId} not found for deletion", tenantId);
                    return false;
                }

                _context.Tenants.Remove(tenant);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted tenant {TenantId}", tenantId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete tenant {TenantId}", tenantId);
                throw;
            }
        }
    }
}
