using System;
using System.Collections.Generic;
using Binah.Ontology.Pipelines.DataNetwork;

namespace Binah.Ontology.Models.Tenant
{
    /// <summary>
    /// Tenant entity for multi-tenant isolation
    /// Stores tenant-specific settings including data network consent
    /// </summary>
    public class Tenant
    {
        /// <summary>Unique tenant identifier</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Tenant display name</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Whether tenant is active</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>When tenant was created</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>When tenant was last updated</summary>
        public DateTime? UpdatedAt { get; set; }

        // === Data Network Consent Fields ===

        /// <summary>Whether tenant has consented to contribute data to the data network</summary>
        public bool DataNetworkConsent { get; set; } = false;

        /// <summary>When tenant granted data network consent</summary>
        public DateTime? DataNetworkConsentDate { get; set; }

        /// <summary>Version of data network consent agreement accepted</summary>
        public string DataNetworkConsentVersion { get; set; } = "1.0";

        /// <summary>Level of PII scrubbing to apply when contributing to data network</summary>
        public ScrubbingLevel PiiScrubbingLevel { get; set; } = ScrubbingLevel.Strict;

        /// <summary>
        /// List of entity types tenant consents to share
        /// Empty list means all entity types are included
        /// </summary>
        public List<string> DataNetworkCategories { get; set; } = new();
    }
}
