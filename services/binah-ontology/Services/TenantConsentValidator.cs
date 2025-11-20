using System;
using System.Threading.Tasks;
using Binah.Ontology.Pipelines.DataNetwork;
using Binah.Ontology.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Binah.Ontology.Services
{
    /// <summary>
    /// Validates tenant consent for data network contribution
    /// Checks tenant settings to determine if they've opted in and what scrubbing level to use
    /// </summary>
    public class TenantConsentValidator : ITenantConsentValidator
    {
        private readonly ITenantRepository _tenantRepository;
        private readonly ILogger<TenantConsentValidator> _logger;

        public TenantConsentValidator(
            ITenantRepository tenantRepository,
            ILogger<TenantConsentValidator> logger)
        {
            _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<ConsentValidationResult> ValidateConsentAsync(string? tenantId, string entityType)
        {
            // No tenant ID means no consent
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                _logger.LogDebug("No tenant ID provided for consent validation");
                return new ConsentValidationResult
                {
                    HasConsent = false,
                    ScrubbingLevel = ScrubbingLevel.Strict,
                    ConsentVersion = "1.0",
                    IncludesEntityType = false
                };
            }

            try
            {
                // Look up tenant from database
                var tenant = await _tenantRepository.GetByIdAsync(tenantId);
                if (tenant == null)
                {
                    _logger.LogWarning("Tenant {TenantId} not found for consent validation", tenantId);
                    return new ConsentValidationResult
                    {
                        HasConsent = false,
                        ScrubbingLevel = ScrubbingLevel.Strict,
                        ConsentVersion = "1.0",
                        IncludesEntityType = false
                    };
                }

                // Check if tenant has not consented
                if (!tenant.DataNetworkConsent)
                {
                    _logger.LogDebug("Tenant {TenantId} has not consented to data network", tenantId);
                    return new ConsentValidationResult
                    {
                        HasConsent = false,
                        ScrubbingLevel = ScrubbingLevel.Strict,
                        ConsentVersion = tenant.DataNetworkConsentVersion,
                        IncludesEntityType = false
                    };
                }

                // Check if entity type is included in consent
                // Empty categories list means all entity types are included
                var categories = tenant.DataNetworkCategories ?? new();
                var includesEntity = categories.Count == 0 || categories.Contains(entityType);

                if (!includesEntity)
                {
                    _logger.LogDebug(
                        "Tenant {TenantId} has not consented to share {EntityType} (categories: {Categories})",
                        tenantId,
                        entityType,
                        string.Join(", ", categories));
                }

                return new ConsentValidationResult
                {
                    HasConsent = true,
                    ScrubbingLevel = tenant.PiiScrubbingLevel,
                    ConsentVersion = tenant.DataNetworkConsentVersion,
                    IncludesEntityType = includesEntity
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to validate consent for tenant {TenantId} and entity type {EntityType}",
                    tenantId,
                    entityType);

                // On error, return no consent for safety
                return new ConsentValidationResult
                {
                    HasConsent = false,
                    ScrubbingLevel = ScrubbingLevel.Strict,
                    ConsentVersion = "1.0",
                    IncludesEntityType = false
                };
            }
        }
    }
}
