using System;
using System.Threading.Tasks;
using Binah.Ontology.Models.Base;
using Microsoft.Extensions.Logging;

namespace Binah.Ontology.Pipelines.DataNetwork;

/// <summary>
/// Domain-agnostic data network pipeline
/// Orchestrates consent validation, PII scrubbing, and data network storage
/// Works with any domain (Finance, Healthcare, Real Estate, etc.)
/// </summary>
public class DataNetworkPipeline : IDataNetworkPipeline
{
    private readonly ITenantConsentValidator _consentValidator;
    private readonly IPiiScrubber _piiScrubber;
    private readonly IDataNetworkStore _dataNetworkStore;
    private readonly ILogger<DataNetworkPipeline> _logger;

    public DataNetworkPipeline(
        ITenantConsentValidator consentValidator,
        IPiiScrubber piiScrubber,
        IDataNetworkStore dataNetworkStore,
        ILogger<DataNetworkPipeline> logger)
    {
        _consentValidator = consentValidator ?? throw new ArgumentNullException(nameof(consentValidator));
        _piiScrubber = piiScrubber ?? throw new ArgumentNullException(nameof(piiScrubber));
        _dataNetworkStore = dataNetworkStore ?? throw new ArgumentNullException(nameof(dataNetworkStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<bool> ProcessEntityAsync(Entity entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        try
        {
            // Step 1: Validate tenant consent
            var consent = await _consentValidator.ValidateConsentAsync(entity.TenantId, entity.Type);
            if (!consent.HasConsent)
            {
                _logger.LogDebug(
                    "Tenant {TenantId} has not consented to data network for {EntityType}",
                    entity.TenantId, entity.Type);
                return false;
            }

            // Step 2: Scrub PII based on consent level
            var scrubbed = _piiScrubber.ScrubEntity(entity, entity.Type, consent.ScrubbingLevel);

            // Step 3: Determine domain from entity type or metadata
            var domain = ExtractDomain(entity);

            // Step 4: Store in data network
            var tenantHash = scrubbed.Metadata?.TryGetValue("original_tenant_id_hash", out var hashObj) == true
                ? hashObj?.ToString() ?? ""
                : "";
            
            var metadata = new DataNetworkMetadata
            {
                Domain = domain,
                EntityType = entity.Type,
                OriginalTenantHash = tenantHash,
                ScrubbingLevel = consent.ScrubbingLevel,
                ConsentVersion = consent.ConsentVersion,
                IngestedAt = DateTime.UtcNow
            };

            await _dataNetworkStore.StoreAsync(scrubbed, metadata);

            _logger.LogInformation(
                "Entity {EntityType} from domain {Domain} contributed to data network (scrubbing: {Level})",
                entity.Type, domain, consent.ScrubbingLevel);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to process entity {EntityType} for data network",
                entity.Type);
            return false;
        }
    }

    /// <summary>
    /// Extract domain name from entity metadata or infer from entity type
    /// </summary>
    private string ExtractDomain(Entity entity)
    {
        // Try to get domain from metadata first
        if (entity.Metadata != null && entity.Metadata.TryGetValue("domain", out var domainObj))
        {
            return domainObj?.ToString() ?? "Unknown";
        }

        // Try to infer from entity type or source
        // This is a simple heuristic - can be enhanced with domain registry
        if (!string.IsNullOrEmpty(entity.Source))
        {
            // Source might contain domain info (e.g., "Finance.Domain", "Healthcare.Domain")
            var parts = entity.Source.Split('.');
            if (parts.Length > 0 && !string.IsNullOrEmpty(parts[0]))
            {
                return parts[0];
            }
        }

        // Default to "Unknown" if we can't determine
        return "Unknown";
    }
}

