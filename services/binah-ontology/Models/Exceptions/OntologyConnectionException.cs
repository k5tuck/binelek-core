namespace Binah.Ontology.Models.Exceptions;

/// <summary>
/// Exception thrown when connection to ontology services fails
/// </summary>
public class OntologyConnectionException : OntologyException
{
    public OntologyConnectionException(string message)
        : base(message, "ONTOLOGY_CONNECTION_ERROR")
    {
    }

    public OntologyConnectionException(string message, System.Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = "ONTOLOGY_CONNECTION_ERROR";
    }
}
