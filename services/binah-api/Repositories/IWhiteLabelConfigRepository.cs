using Binah.API.Models;

namespace Binah.API.Repositories;

public interface IWhiteLabelConfigRepository
{
    Task<WhiteLabelConfig?> GetByLicenseeIdAsync(Guid licenseeId);
    Task<WhiteLabelConfig?> GetByCustomDomainAsync(string customDomain);
    Task<WhiteLabelConfig> CreateOrUpdateAsync(WhiteLabelConfig config);
    Task<bool> DeleteAsync(Guid licenseeId);
}
