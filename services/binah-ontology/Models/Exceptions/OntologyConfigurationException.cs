namespace Binah.Ontology.Models.Exceptions;

/// <summary>
/// Exception thrown when ontology configuration is invalid or missing
/// </summary>
public class OntologyConfigurationException : OntologyException
{
    public OntologyConfigurationException(string message)
        : base(message, "ONTOLOGY_CONFIGURATION_ERROR")
    {
    }

    public OntologyConfigurationException(string message, System.Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = "ONTOLOGY_CONFIGURATION_ERROR";
    }
}
