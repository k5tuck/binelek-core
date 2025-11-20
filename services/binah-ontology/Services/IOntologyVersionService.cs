using Binah.Ontology.Models.Ontology;

namespace Binah.Ontology.Services;

public interface IOntologyVersionService
{
    Task<OntologyVersion?> GetActiveAsync(Guid tenantId);
    Task<List<OntologyVersion>> GetVersionsAsync(Guid tenantId);
    Task<OntologyVersion> CreateAsync(OntologyVersion ontology);
    Task PublishAsync(Guid tenantId, string version);
    Task<bool> ValidateAsync(OntologyVersion ontology);
}
