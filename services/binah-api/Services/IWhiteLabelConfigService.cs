using Binah.API.Models;

namespace Binah.API.Services;

public interface IWhiteLabelConfigService
{
    Task<WhiteLabelConfig?> GetByLicenseeIdAsync(Guid licenseeId);
    Task<WhiteLabelConfig?> GetByCustomDomainAsync(string customDomain);
    Task<WhiteLabelConfig> CreateOrUpdateAsync(WhiteLabelConfig config);
    Task<bool> DeleteAsync(Guid licenseeId);
}
