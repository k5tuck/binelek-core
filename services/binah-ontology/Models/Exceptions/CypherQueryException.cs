namespace Binah.Ontology.Models.Exceptions;

/// <summary>
/// Exception thrown when Cypher query execution fails
/// </summary>
public class CypherQueryException : OntologyException
{
    public string Query { get; set; }

    public CypherQueryException(string query, string message)
        : base($"Cypher query failed: {message}", "CYPHER_QUERY_FAILED")
    {
        Query = query;
    }

    public CypherQueryException(string query, string message, System.Exception innerException)
        : base($"Cypher query failed: {message}", innerException)
    {
        Query = query;
        ErrorCode = "CYPHER_QUERY_FAILED";
    }
}