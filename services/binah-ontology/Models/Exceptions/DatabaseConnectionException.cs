namespace Binah.Ontology.Models.Exceptions;

/// <summary>
/// Exception thrown when Neo4j connection fails
/// </summary>
public class DatabaseConnectionException : OntologyException
{
    public DatabaseConnectionException(string message)
        : base($"Database connection failed: {message}", "DATABASE_CONNECTION_FAILED") { }

    public DatabaseConnectionException(string message, Exception innerException)
        : base($"Database connection failed: {message}", innerException)
    {
        ErrorCode = "DATABASE_CONNECTION_FAILED";
    }
}