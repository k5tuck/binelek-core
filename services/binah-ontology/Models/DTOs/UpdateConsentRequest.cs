using System.ComponentModel.DataAnnotations;

namespace Binah.Ontology.Models.DTOs;

/// <summary>
/// Request DTO for updating tenant data network consent
/// </summary>
public class UpdateConsentRequest
{
    /// <summary>Whether tenant consents to data network contribution</summary>
    [Required]
    public bool DataNetworkConsent { get; set; }

    /// <summary>
    /// Level of PII scrubbing to apply
    /// Valid values: Strict, Moderate, Minimal
    /// </summary>
    [Required]
    [RegularExpression("^(Strict|Moderate|Minimal)$",
        ErrorMessage = "PiiScrubbingLevel must be Strict, Moderate, or Minimal")]
    public string PiiScrubbingLevel { get; set; } = "Strict";

    /// <summary>
    /// Entity types to share (empty list = all types)
    /// </summary>
    public List<string> DataNetworkCategories { get; set; } = new();
}
