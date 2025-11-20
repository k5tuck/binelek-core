using System.Threading.Tasks;

namespace Binah.Ontology.Pipelines.DataNetwork
{
    /// <summary>
    /// Validates tenant consent for data network contribution
    /// Checks if tenant has opted-in and what scrubbing level to use
    /// </summary>
    public interface ITenantConsentValidator
    {
        /// <summary>
        /// Validate if tenant has consented to contribute data for a specific entity type
        /// </summary>
        /// <param name="tenantId">Tenant ID to check consent for</param>
        /// <param name="entityType">Entity type being contributed (e.g., "Client", "Account")</param>
        /// <returns>Consent validation result with scrubbing level</returns>
        Task<ConsentValidationResult> ValidateConsentAsync(string? tenantId, string entityType);
    }

    /// <summary>
    /// Result of tenant consent validation
    /// </summary>
    public record ConsentValidationResult
    {
        /// <summary>Whether tenant has consented to data network contribution</summary>
        public bool HasConsent { get; init; }

        /// <summary>Scrubbing level to apply (from tenant preferences or default)</summary>
        public ScrubbingLevel ScrubbingLevel { get; init; }

        /// <summary>Version of consent agreement tenant accepted</summary>
        public string ConsentVersion { get; init; } = "1.0";

        /// <summary>Whether the specific entity type is included in consent</summary>
        public bool IncludesEntityType { get; init; }
    }
}
