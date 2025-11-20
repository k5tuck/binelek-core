using System;

namespace Binah.Ontology.Models.Exceptions;

/// <summary>
/// Base exception for all ontology-related errors
/// </summary>
public class OntologyException : System.Exception
{
    public string ErrorCode { get; set; } = string.Empty;
    public object? Context { get; set; }

    public OntologyException(string message) : base(message) { }

    public OntologyException(string message, System.Exception innerException)
        : base(message, innerException) { }

    public OntologyException(string message, string errorCode, object? context = null)
        : base(message)
    {
        ErrorCode = errorCode;
        Context = context;
    }
}