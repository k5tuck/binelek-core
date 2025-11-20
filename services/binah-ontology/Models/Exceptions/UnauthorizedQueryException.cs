namespace Binah.Ontology.Models.Exceptions;

/// <summary>
/// Exception thrown when a query contains unauthorized operations
/// </summary>
public class UnauthorizedQueryException : OntologyException
{
    public string Query { get; set; }
    public string Reason { get; set; }

    public UnauthorizedQueryException(string query, string reason)
        : base($"Query contains unauthorized operation: {reason}", "UNAUTHORIZED_QUERY")
    {
        Query = query;
        Reason = reason;
    }
}