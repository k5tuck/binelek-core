using Binah.Auth.Models;
using Binah.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Binah.Auth.Controllers;

/// <summary>
/// Controller for managing user favorites/pinned sidebar items
/// </summary>
[ApiController]
[Route("api/favorites")]
[Authorize]
public class FavoritesController : ControllerBase
{
    private readonly AuthDbContext _context;
    private readonly ILogger<FavoritesController> _logger;

    public FavoritesController(AuthDbContext context, ILogger<FavoritesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get current user's favorites
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<FavoriteDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<FavoriteDto>>>> GetFavorites()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var tenantId = User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized();
        }

        var favorites = await _context.UserFavorites
            .Where(f => f.UserId == userId && f.TenantId == tenantId)
            .OrderBy(f => f.SortOrder)
            .Select(f => new FavoriteDto
            {
                Id = f.Id,
                Route = f.Route,
                Label = f.Label,
                Icon = f.Icon,
                SortOrder = f.SortOrder
            })
            .ToListAsync();

        return Ok(ApiResponse<List<FavoriteDto>>.Ok(favorites));
    }

    /// <summary>
    /// Add a favorite
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<FavoriteDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<FavoriteDto>>> AddFavorite([FromBody] CreateFavoriteRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var tenantId = User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized();
        }

        // Check if already favorited
        var existing = await _context.UserFavorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.TenantId == tenantId && f.Route == request.Route);

        if (existing != null)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Already Favorited",
                Detail = "This item is already in your favorites",
                Status = StatusCodes.Status400BadRequest
            });
        }

        // Get next sort order
        var maxOrder = await _context.UserFavorites
            .Where(f => f.UserId == userId && f.TenantId == tenantId)
            .MaxAsync(f => (int?)f.SortOrder) ?? 0;

        var favorite = new UserFavorite
        {
            UserId = userId,
            TenantId = tenantId,
            Route = request.Route,
            Label = request.Label,
            Icon = request.Icon,
            SortOrder = maxOrder + 1
        };

        _context.UserFavorites.Add(favorite);
        await _context.SaveChangesAsync();

        var dto = new FavoriteDto
        {
            Id = favorite.Id,
            Route = favorite.Route,
            Label = favorite.Label,
            Icon = favorite.Icon,
            SortOrder = favorite.SortOrder
        };

        return CreatedAtAction(nameof(GetFavorites), ApiResponse<FavoriteDto>.Ok(dto));
    }

    /// <summary>
    /// Remove a favorite
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFavorite(string id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var tenantId = User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized();
        }

        var favorite = await _context.UserFavorites
            .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId && f.TenantId == tenantId);

        if (favorite == null)
        {
            return NotFound();
        }

        _context.UserFavorites.Remove(favorite);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Remove a favorite by route
    /// </summary>
    [HttpDelete("route")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFavoriteByRoute([FromQuery] string route)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var tenantId = User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized();
        }

        var favorite = await _context.UserFavorites
            .FirstOrDefaultAsync(f => f.Route == route && f.UserId == userId && f.TenantId == tenantId);

        if (favorite == null)
        {
            return NotFound();
        }

        _context.UserFavorites.Remove(favorite);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Update favorites order
    /// </summary>
    [HttpPut("order")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateOrder([FromBody] UpdateFavoriteOrderRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var tenantId = User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized();
        }

        var favorites = await _context.UserFavorites
            .Where(f => f.UserId == userId && f.TenantId == tenantId)
            .ToListAsync();

        for (int i = 0; i < request.FavoriteIds.Count; i++)
        {
            var favorite = favorites.FirstOrDefault(f => f.Id == request.FavoriteIds[i]);
            if (favorite != null)
            {
                favorite.SortOrder = i;
            }
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }
}
