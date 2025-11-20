using Binah.Contracts.Common;
using Binah.Ontology.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Binah.Ontology.Controllers;

/// <summary>
/// Controller for exporting ontology data in standards-based formats (RDF/OWL)
/// Enables data portability and GDPR compliance
/// </summary>
[ApiController]
[Route("api/export")]
[Authorize]
public class ExportController : ControllerBase
{
    private readonly IExportService _exportService;
    private readonly ILogger<ExportController> _logger;

    public ExportController(IExportService exportService, ILogger<ExportController> logger)
    {
        _exportService = exportService;
        _logger = logger;
    }

    /// <summary>
    /// Export ontology to RDF/XML format
    /// </summary>
    /// <returns>RDF/XML file download</returns>
    [HttpGet("rdf-xml")]
    [Produces("application/rdf+xml")]
    public async Task<IActionResult> ExportRdfXml()
    {
        var tenantId = User.FindFirst("tenant_id")?.Value ?? "core";
        _logger.LogInformation("Exporting RDF/XML for tenant {TenantId}", tenantId);

        var stream = await _exportService.ExportToRdfXmlAsync(tenantId);
        return File(stream, "application/rdf+xml", $"ontology-{tenantId}.rdf");
    }

    /// <summary>
    /// Export ontology to Turtle format
    /// </summary>
    /// <returns>Turtle file download</returns>
    [HttpGet("turtle")]
    [Produces("text/turtle")]
    public async Task<IActionResult> ExportTurtle()
    {
        var tenantId = User.FindFirst("tenant_id")?.Value ?? "core";
        _logger.LogInformation("Exporting Turtle for tenant {TenantId}", tenantId);

        var stream = await _exportService.ExportToTurtleAsync(tenantId);
        return File(stream, "text/turtle", $"ontology-{tenantId}.ttl");
    }

    /// <summary>
    /// Export ontology to N-Triples format
    /// </summary>
    /// <returns>N-Triples file download</returns>
    [HttpGet("ntriples")]
    [Produces("application/n-triples")]
    public async Task<IActionResult> ExportNTriples()
    {
        var tenantId = User.FindFirst("tenant_id")?.Value ?? "core";
        _logger.LogInformation("Exporting N-Triples for tenant {TenantId}", tenantId);

        var stream = await _exportService.ExportToNTriplesAsync(tenantId);
        return File(stream, "application/n-triples", $"ontology-{tenantId}.nt");
    }

    /// <summary>
    /// Export ontology to OWL/XML format
    /// </summary>
    /// <returns>OWL/XML file download</returns>
    [HttpGet("owl-xml")]
    [Produces("application/rdf+xml")]
    public async Task<IActionResult> ExportOwlXml()
    {
        var tenantId = User.FindFirst("tenant_id")?.Value ?? "core";
        _logger.LogInformation("Exporting OWL/XML for tenant {TenantId}", tenantId);

        var stream = await _exportService.ExportToOwlXmlAsync(tenantId);
        return File(stream, "application/rdf+xml", $"ontology-{tenantId}.owl");
    }

    /// <summary>
    /// Export ontology to JSON-LD format
    /// </summary>
    /// <returns>JSON-LD file download</returns>
    [HttpGet("json-ld")]
    [Produces("application/ld+json")]
    public async Task<IActionResult> ExportJsonLd()
    {
        var tenantId = User.FindFirst("tenant_id")?.Value ?? "core";
        _logger.LogInformation("Exporting JSON-LD for tenant {TenantId}", tenantId);

        var stream = await _exportService.ExportToJsonLdAsync(tenantId);
        return File(stream, "application/ld+json", $"ontology-{tenantId}.jsonld");
    }

    /// <summary>
    /// Get list of supported export formats
    /// </summary>
    /// <returns>List of available export formats with endpoints and media types</returns>
    [HttpGet("formats")]
    [Produces("application/json")]
    public IActionResult GetSupportedFormats()
    {
        return Ok(ApiResponse<object>.Ok(new
        {
            formats = new[]
            {
                new { name = "RDF/XML", endpoint = "/api/export/rdf-xml", mediaType = "application/rdf+xml", extension = ".rdf" },
                new { name = "Turtle", endpoint = "/api/export/turtle", mediaType = "text/turtle", extension = ".ttl" },
                new { name = "N-Triples", endpoint = "/api/export/ntriples", mediaType = "application/n-triples", extension = ".nt" },
                new { name = "OWL/XML", endpoint = "/api/export/owl-xml", mediaType = "application/rdf+xml", extension = ".owl" },
                new { name = "JSON-LD", endpoint = "/api/export/json-ld", mediaType = "application/ld+json", extension = ".jsonld" }
            }
        }));
    }
}
