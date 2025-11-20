using Binah.API.Models;
using Binah.API.Repositories;
using Microsoft.Extensions.Caching.Memory;

namespace Binah.API.Services;

public class WhiteLabelConfigService : IWhiteLabelConfigService
{
    private readonly IWhiteLabelConfigRepository _repository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<WhiteLabelConfigService> _logger;

    public WhiteLabelConfigService(
        IWhiteLabelConfigRepository repository,
        IMemoryCache cache,
        ILogger<WhiteLabelConfigService> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<WhiteLabelConfig?> GetByLicenseeIdAsync(Guid licenseeId)
    {
        var cacheKey = $"whitelabel:licensee:{licenseeId}";

        if (_cache.TryGetValue(cacheKey, out WhiteLabelConfig? cached))
        {
            return cached;
        }

        var config = await _repository.GetByLicenseeIdAsync(licenseeId);

        if (config != null)
        {
            _cache.Set(cacheKey, config, TimeSpan.FromMinutes(5));
        }

        return config;
    }

    public async Task<WhiteLabelConfig?> GetByCustomDomainAsync(string customDomain)
    {
        var cacheKey = $"whitelabel:domain:{customDomain}";

        if (_cache.TryGetValue(cacheKey, out WhiteLabelConfig? cached))
        {
            return cached;
        }

        var config = await _repository.GetByCustomDomainAsync(customDomain);

        if (config != null)
        {
            _cache.Set(cacheKey, config, TimeSpan.FromMinutes(5));
        }

        return config;
    }

    public async Task<WhiteLabelConfig> CreateOrUpdateAsync(WhiteLabelConfig config)
    {
        config.UpdatedAt = DateTime.UtcNow;
        var result = await _repository.CreateOrUpdateAsync(config);

        // Invalidate cache
        _cache.Remove($"whitelabel:licensee:{config.LicenseeId}");
        if (!string.IsNullOrEmpty(config.CustomDomain))
        {
            _cache.Remove($"whitelabel:domain:{config.CustomDomain}");
        }

        _logger.LogInformation("Updated white-label config for licensee {LicenseeId}", config.LicenseeId);

        return result;
    }

    public async Task<bool> DeleteAsync(Guid licenseeId)
    {
        var result = await _repository.DeleteAsync(licenseeId);

        // Invalidate cache
        _cache.Remove($"whitelabel:licensee:{licenseeId}");

        return result;
    }
}
