using Binah.Ontology.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Binah.Ontology.Repositories;

/// <summary>
/// Repository interface for canvas data access operations
/// </summary>
public interface ICanvasRepository
{
    /// <summary>
    /// Creates a new canvas
    /// </summary>
    /// <param name="canvas">Canvas to create</param>
    /// <returns>Created canvas with generated ID</returns>
    Task<Canvas> CreateAsync(Canvas canvas);

    /// <summary>
    /// Retrieves a canvas by ID
    /// </summary>
    /// <param name="id">Canvas ID</param>
    /// <param name="tenantId">Tenant ID for isolation</param>
    /// <returns>Canvas if found, null otherwise</returns>
    Task<Canvas?> GetByIdAsync(Guid id, Guid tenantId);

    /// <summary>
    /// Retrieves all canvases for a tenant with pagination
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="skip">Number of records to skip</param>
    /// <param name="limit">Maximum number of records to return</param>
    /// <returns>List of canvases</returns>
    Task<List<Canvas>> GetByTenantAsync(Guid tenantId, int skip = 0, int limit = 100);

    /// <summary>
    /// Retrieves canvases created by a specific user
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="userId">User ID</param>
    /// <param name="skip">Number of records to skip</param>
    /// <param name="limit">Maximum number of records to return</param>
    /// <returns>List of canvases</returns>
    Task<List<Canvas>> GetByUserAsync(Guid tenantId, Guid userId, int skip = 0, int limit = 100);

    /// <summary>
    /// Retrieves canvases shared with a specific user
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="userId">User ID</param>
    /// <param name="skip">Number of records to skip</param>
    /// <param name="limit">Maximum number of records to return</param>
    /// <returns>List of shared canvases</returns>
    Task<List<Canvas>> GetSharedWithUserAsync(Guid tenantId, Guid userId, int skip = 0, int limit = 100);

    /// <summary>
    /// Updates an existing canvas
    /// </summary>
    /// <param name="canvas">Canvas with updated values</param>
    /// <returns>Updated canvas</returns>
    Task<Canvas> UpdateAsync(Canvas canvas);

    /// <summary>
    /// Deletes a canvas
    /// </summary>
    /// <param name="id">Canvas ID</param>
    /// <param name="tenantId">Tenant ID for isolation</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteAsync(Guid id, Guid tenantId);

    /// <summary>
    /// Checks if a canvas exists
    /// </summary>
    /// <param name="id">Canvas ID</param>
    /// <param name="tenantId">Tenant ID for isolation</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> ExistsAsync(Guid id, Guid tenantId);
}
