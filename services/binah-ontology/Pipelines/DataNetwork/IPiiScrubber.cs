using Binah.Ontology.Models.Base;

namespace Binah.Ontology.Pipelines.DataNetwork;

/// <summary>
/// Interface for PII scrubbing operations
/// Domain-agnostic scrubbing of personally identifiable information
/// </summary>
public interface IPiiScrubber
{
    /// <summary>
    /// Scrub PII from an entity based on the specified scrubbing level
    /// </summary>
    /// <param name="entity">The entity to scrub</param>
    /// <param name="entityType">The type of entity (e.g., "Client", "Account")</param>
    /// <param name="level">The scrubbing level to apply</param>
    /// <returns>A new entity with PII scrubbed according to the level</returns>
    Entity ScrubEntity(Entity entity, string entityType, ScrubbingLevel level);
}

