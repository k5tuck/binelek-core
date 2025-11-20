using System.Threading.Tasks;
using Binah.Ontology.Models.Base;

namespace Binah.Ontology.Pipelines.DataNetwork;

/// <summary>
/// Domain-agnostic interface for data network pipeline
/// Processes entities for contribution to the data network
/// </summary>
public interface IDataNetworkPipeline
{
    /// <summary>
    /// Process an entity for data network contribution
    /// Validates consent, scrubs PII, and stores in data network
    /// </summary>
    /// <param name="entity">The entity to process</param>
    /// <returns>True if entity was successfully contributed, false otherwise</returns>
    Task<bool> ProcessEntityAsync(Entity entity);
}

