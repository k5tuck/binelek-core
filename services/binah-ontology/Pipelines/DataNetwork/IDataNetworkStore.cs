using System;
using System.Threading.Tasks;
using Binah.Ontology.Models.Base;

namespace Binah.Ontology.Pipelines.DataNetwork
{
    /// <summary>
    /// Store for data network entities (separate from production)
    /// Stores anonymized/scrubbed entities for cross-tenant analytics
    /// </summary>
    public interface IDataNetworkStore
    {
        /// <summary>
        /// Store a scrubbed entity in the data network
        /// </summary>
        /// <param name="entity">The scrubbed entity to store</param>
        /// <param name="metadata">Metadata about scrubbing and consent</param>
        Task StoreAsync(Entity entity, DataNetworkMetadata metadata);
    }

    /// <summary>
    /// Metadata about data network entity storage
    /// </summary>
    public record DataNetworkMetadata
    {
        /// <summary>Domain the entity belongs to (e.g., "Finance", "Healthcare")</summary>
        public required string Domain { get; init; }

        /// <summary>Entity type (e.g., "Client", "Account")</summary>
        public required string EntityType { get; init; }

        /// <summary>Hashed tenant ID for traceability without PII</summary>
        public required string OriginalTenantHash { get; init; }

        /// <summary>Level of PII scrubbing applied</summary>
        public required ScrubbingLevel ScrubbingLevel { get; init; }

        /// <summary>Version of consent agreement tenant accepted</summary>
        public required string ConsentVersion { get; init; }

        /// <summary>Timestamp when entity was ingested to data network</summary>
        public required DateTime IngestedAt { get; init; }
    }

    /// <summary>
    /// Scrubbing level for data network contribution
    /// </summary>
    public enum ScrubbingLevel
    {
        /// <summary>Remove ALL PII, hash IDs, generalize dates to month-level</summary>
        Strict,

        /// <summary>Tokenize PII (reversible), keep more granular data for analytics</summary>
        Moderate,

        /// <summary>Only remove highly sensitive/encrypted fields (SSN, etc.)</summary>
        Minimal
    }
}
