using System.Threading.Tasks;
using Binah.Ontology.Models.Tenant;

namespace Binah.Ontology.Repositories.Interfaces
{
    /// <summary>
    /// Repository for tenant data access
    /// </summary>
    public interface ITenantRepository
    {
        /// <summary>
        /// Get tenant by ID
        /// </summary>
        /// <param name="tenantId">Tenant identifier</param>
        /// <returns>Tenant entity or null if not found</returns>
        Task<Tenant?> GetByIdAsync(string tenantId);

        /// <summary>
        /// Create a new tenant
        /// </summary>
        /// <param name="tenant">Tenant to create</param>
        /// <returns>Created tenant</returns>
        Task<Tenant> CreateAsync(Tenant tenant);

        /// <summary>
        /// Update an existing tenant
        /// </summary>
        /// <param name="tenant">Tenant to update</param>
        /// <returns>Updated tenant</returns>
        Task<Tenant> UpdateAsync(Tenant tenant);

        /// <summary>
        /// Delete a tenant
        /// </summary>
        /// <param name="tenantId">Tenant ID to delete</param>
        /// <returns>True if deleted, false if not found</returns>
        Task<bool> DeleteAsync(string tenantId);
    }
}
