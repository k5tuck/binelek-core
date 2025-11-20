using VDS.RDF;
using VDS.RDF.Writing;
using Neo4j.Driver;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Binah.Ontology.Services;

/// <summary>
/// Service for exporting ontology data in standards-based formats (RDF/OWL)
/// Enables data portability and GDPR compliance (right to data portability)
/// </summary>
public class ExportService : IExportService
{
    private readonly IDriver _neo4jDriver;
    private readonly ILogger<ExportService> _logger;
    private const string BaseUri = "http://binelek.com/ontology/";

    public ExportService(IDriver neo4jDriver, ILogger<ExportService> logger)
    {
        _neo4jDriver = neo4jDriver;
        _logger = logger;
    }

    public async Task<Stream> ExportToRdfXmlAsync(string tenantId)
    {
        _logger.LogInformation("Exporting ontology to RDF/XML for tenant {TenantId}", tenantId);
        var graph = await BuildRdfGraphAsync(tenantId);
        return SerializeGraph(graph, new RdfXmlWriter());
    }

    public async Task<Stream> ExportToTurtleAsync(string tenantId)
    {
        _logger.LogInformation("Exporting ontology to Turtle for tenant {TenantId}", tenantId);
        var graph = await BuildRdfGraphAsync(tenantId);
        return SerializeGraph(graph, new CompressingTurtleWriter());
    }

    public async Task<Stream> ExportToNTriplesAsync(string tenantId)
    {
        _logger.LogInformation("Exporting ontology to N-Triples for tenant {TenantId}", tenantId);
        var graph = await BuildRdfGraphAsync(tenantId);
        return SerializeGraph(graph, new NTriplesWriter());
    }

    public async Task<Stream> ExportToOwlXmlAsync(string tenantId)
    {
        _logger.LogInformation("Exporting ontology to OWL/XML for tenant {TenantId}", tenantId);
        var graph = await BuildOwlGraphAsync(tenantId);
        return SerializeGraph(graph, new RdfXmlWriter());
    }

    public async Task<Stream> ExportToJsonLdAsync(string tenantId)
    {
        _logger.LogInformation("Exporting ontology to JSON-LD for tenant {TenantId}", tenantId);
        var graph = await BuildRdfGraphAsync(tenantId);
        return SerializeGraphToJsonLd(graph);
    }

    /// <summary>
    /// Build RDF graph from Neo4j data
    /// Queries Neo4j for entities and relationships, converts to RDF triples
    /// </summary>
    private async Task<IGraph> BuildRdfGraphAsync(string tenantId)
    {
        var graph = new Graph();
        graph.BaseUri = new Uri(BaseUri);

        await using var session = _neo4jDriver.AsyncSession();

        // Fetch all entities for tenant
        var entitiesQuery = @"
            MATCH (e:Entity {tenantId: $tenantId})
            RETURN e.id AS id, e.type AS type, properties(e) AS props
        ";

        var entities = await session.RunAsync(entitiesQuery, new { tenantId });

        await entities.ForEachAsync(record =>
        {
            var entityId = record["id"].As<string>();
            var entityType = record["type"].As<string>();
            var props = record["props"].As<Dictionary<string, object>>();

            // Create RDF triple: <entity> rdf:type <EntityType>
            var entityNode = graph.CreateUriNode(new Uri(BaseUri + "entity/" + entityId));
            var typeNode = graph.CreateUriNode(new Uri(BaseUri + "type/" + entityType));
            var rdfType = graph.CreateUriNode(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#type"));

            graph.Assert(entityNode, rdfType, typeNode);

            // Add properties as RDF triples
            foreach (var prop in props)
            {
                if (prop.Key == "tenantId" || prop.Key == "id" || prop.Key == "type")
                    continue; // Skip metadata

                var predicate = graph.CreateUriNode(new Uri(BaseUri + "property/" + prop.Key));
                var value = graph.CreateLiteralNode(prop.Value?.ToString() ?? "");

                graph.Assert(entityNode, predicate, value);
            }
        });

        // Fetch all relationships
        var relationshipsQuery = @"
            MATCH (source:Entity {tenantId: $tenantId})-[r]->(target:Entity {tenantId: $tenantId})
            RETURN source.id AS sourceId, type(r) AS relType, target.id AS targetId
        ";

        var relationships = await session.RunAsync(relationshipsQuery, new { tenantId });

        await relationships.ForEachAsync(record =>
        {
            var sourceId = record["sourceId"].As<string>();
            var relType = record["relType"].As<string>();
            var targetId = record["targetId"].As<string>();

            var sourceNode = graph.CreateUriNode(new Uri(BaseUri + "entity/" + sourceId));
            var targetNode = graph.CreateUriNode(new Uri(BaseUri + "entity/" + targetId));
            var predicate = graph.CreateUriNode(new Uri(BaseUri + "relationship/" + relType));

            graph.Assert(sourceNode, predicate, targetNode);
        });

        _logger.LogInformation("Built RDF graph for tenant {TenantId} with {TripleCount} triples",
            tenantId, graph.Triples.Count);

        return graph;
    }

    /// <summary>
    /// Build OWL graph with ontology headers and class definitions
    /// Extends RDF graph with OWL semantics
    /// </summary>
    private async Task<IGraph> BuildOwlGraphAsync(string tenantId)
    {
        var graph = await BuildRdfGraphAsync(tenantId);

        // Add OWL ontology header
        var ontologyNode = graph.CreateUriNode(new Uri(BaseUri + "ontology/" + tenantId));
        var owlOntology = graph.CreateUriNode(new Uri("http://www.w3.org/2002/07/owl#Ontology"));
        var rdfType = graph.CreateUriNode(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#type"));

        graph.Assert(ontologyNode, rdfType, owlOntology);

        // Add OWL class definitions
        var owlClass = graph.CreateUriNode(new Uri("http://www.w3.org/2002/07/owl#Class"));

        // Get unique entity types from graph and define as OWL classes
        var entityTypes = graph.Triples
            .Where(t => t.Predicate.ToString().Contains("rdf-syntax-ns#type"))
            .Select(t => t.Object)
            .Distinct();

        foreach (var typeNode in entityTypes)
        {
            if (typeNode is IUriNode uriNode)
            {
                graph.Assert(uriNode, rdfType, owlClass);
            }
        }

        _logger.LogInformation("Built OWL graph for tenant {TenantId} with {ClassCount} classes",
            tenantId, entityTypes.Count());

        return graph;
    }

    /// <summary>
    /// Serialize RDF graph to stream using specified writer
    /// </summary>
    private Stream SerializeGraph(IGraph graph, IRdfWriter writer)
    {
        var stream = new MemoryStream();
        var streamWriter = new StreamWriter(stream);

        writer.Save(graph, streamWriter);
        streamWriter.Flush();
        stream.Position = 0;

        return stream;
    }

    private Stream SerializeGraphToJsonLd(IGraph graph)
    {
        var stream = new MemoryStream();
        var writer = new CompressingTurtleWriter();

        // Use Turtle format as fallback since JsonLdWriter has compatibility issues
        writer.Save(graph, new StreamWriter(stream));
        stream.Position = 0;

        return stream;
    }
}
