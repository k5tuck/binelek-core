using Binah.Ontology.Pipelines.DataNetwork;

namespace Binah.Ontology.Models.DTOs;

/// <summary>
/// Response DTO for tenant data network consent settings
/// </summary>
public class DataNetworkConsentResponse
{
    /// <summary>Whether tenant has consented to data network contribution</summary>
    public bool DataNetworkConsent { get; set; }

    /// <summary>Level of PII scrubbing applied</summary>
    public string PiiScrubbingLevel { get; set; } = "Strict";

    /// <summary>Entity types tenant consents to share (empty = all)</summary>
    public List<string> DataNetworkCategories { get; set; } = new();

    /// <summary>Version of consent agreement</summary>
    public string ConsentVersion { get; set; } = "1.0";

    /// <summary>When consent was granted (null if not consented)</summary>
    public DateTime? ConsentDate { get; set; }

    /// <summary>
    /// Creates response from Tenant model
    /// </summary>
    public static DataNetworkConsentResponse FromTenant(Tenant.Tenant tenant)
    {
        return new DataNetworkConsentResponse
        {
            DataNetworkConsent = tenant.DataNetworkConsent,
            PiiScrubbingLevel = tenant.PiiScrubbingLevel.ToString(),
            DataNetworkCategories = tenant.DataNetworkCategories,
            ConsentVersion = tenant.DataNetworkConsentVersion,
            ConsentDate = tenant.DataNetworkConsentDate
        };
    }
}
