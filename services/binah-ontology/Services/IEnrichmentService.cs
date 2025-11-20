using Binah.Ontology.Models.Base;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Binah.Ontology.Services;

/// <summary>
/// Service interface for entity enrichment operations
/// </summary>
public interface IEnrichmentService
{
    /// <summary>
    /// Enriches an entity with external data based on enrichment type
    /// </summary>
    /// <param name="entity">The entity to enrich</param>
    /// <param name="enrichmentType">Type of enrichment (geocoding, property_details, credit_score, etc.)</param>
    /// <param name="parameters">Additional parameters for enrichment</param>
    /// <returns>Dictionary of enriched properties</returns>
    Task<Dictionary<string, object>> EnrichAsync(
        Entity entity,
        string enrichmentType,
        Dictionary<string, object>? parameters = null);

    /// <summary>
    /// Geocodes an address and returns latitude/longitude
    /// </summary>
    Task<Dictionary<string, object>> GeocodeAddressAsync(string address);

    /// <summary>
    /// Enriches property details from external sources (MLS, Zillow, etc.)
    /// </summary>
    Task<Dictionary<string, object>> EnrichPropertyDetailsAsync(string propertyId, string address);

    /// <summary>
    /// Gets credit score information for a person
    /// </summary>
    Task<Dictionary<string, object>> EnrichCreditScoreAsync(string personId, string ssn);

    /// <summary>
    /// Enriches company information from external business databases
    /// </summary>
    Task<Dictionary<string, object>> EnrichCompanyDataAsync(string companyName, string? ein = null);
}
