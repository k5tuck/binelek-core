using System.IO;
using System.Threading.Tasks;

namespace Binah.Ontology.Services;

/// <summary>
/// Service for exporting ontology data in standards-based formats (RDF/OWL)
/// Enables data portability and prevents vendor lock-in
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Export ontology to RDF/XML format
    /// </summary>
    /// <param name="tenantId">Tenant ID for data isolation</param>
    /// <returns>Stream containing RDF/XML data</returns>
    Task<Stream> ExportToRdfXmlAsync(string tenantId);

    /// <summary>
    /// Export ontology to Turtle format (compact RDF)
    /// </summary>
    /// <param name="tenantId">Tenant ID for data isolation</param>
    /// <returns>Stream containing Turtle data</returns>
    Task<Stream> ExportToTurtleAsync(string tenantId);

    /// <summary>
    /// Export ontology to N-Triples format (line-based RDF)
    /// </summary>
    /// <param name="tenantId">Tenant ID for data isolation</param>
    /// <returns>Stream containing N-Triples data</returns>
    Task<Stream> ExportToNTriplesAsync(string tenantId);

    /// <summary>
    /// Export ontology to OWL/XML format (ontology with class definitions)
    /// </summary>
    /// <param name="tenantId">Tenant ID for data isolation</param>
    /// <returns>Stream containing OWL/XML data</returns>
    Task<Stream> ExportToOwlXmlAsync(string tenantId);

    /// <summary>
    /// Export ontology to JSON-LD format (JSON-based RDF)
    /// </summary>
    /// <param name="tenantId">Tenant ID for data isolation</param>
    /// <returns>Stream containing JSON-LD data</returns>
    Task<Stream> ExportToJsonLdAsync(string tenantId);
}
