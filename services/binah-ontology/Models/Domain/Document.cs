
using Binah.Ontology.Models.Base;
namespace Binah.Ontology.Models.Domain;

/// <summary>
/// Represents a legal or administrative document
/// Label: Document
/// </summary>
public class Document : Entity
{
    public Document()
    {
        Type = "Document";
    }

    /// <summary>Canonical document identifier (UUID)</summary>
    public string Uid
    {
        get => Id;
        set => Id = value;
    }

    /// <summary>Document type: permit, deed, loan, appraisal</summary>
    public string? DocumentTypeValue
    {
        get => GetPropertyValue<string>("document_type");
        set => SetPropertyValue("document_type", value);
    }

    /// <summary>Official document identifier</summary>
    public string? DocumentId
    {
        get => GetPropertyValue<string>("document_id");
        set => SetPropertyValue("document_id", value);
    }

    /// <summary>Object store reference to file</summary>
    public string? FileUrl
    {
        get => GetPropertyValue<string>("file_url");
        set => SetPropertyValue("file_url", value);
    }

    /// <summary>Link to Qdrant text embedding</summary>
    public string? TextEmbeddingId
    {
        get => GetPropertyValue<string>("text_embedding_id");
        set => SetPropertyValue("text_embedding_id", value);
    }

    /// <summary>Extracted fields from document parsing (permit_id, fee, issue_date, etc.)</summary>
    public Dictionary<string, object>? ExtractedFields
    {
        get => GetPropertyValue<Dictionary<string, object>>("extracted_fields");
        set => SetPropertyValue("extracted_fields", value);
    }

    /// <summary>Document status</summary>
    public string? Status
    {
        get => GetPropertyValue<string>("status");
        set => SetPropertyValue("status", value);
    }

    /// <summary>Entity ID to whom document was issued</summary>
    public string? IssuedTo
    {
        get => GetPropertyValue<string>("issued_to");
        set => SetPropertyValue("issued_to", value);
    }

    /// <summary>Property ID for which document was issued</summary>
    public string? IssuedFor
    {
        get => GetPropertyValue<string>("issued_for");
        set => SetPropertyValue("issued_for", value);
    }

    /// <summary>Date for which document was issued</summary>
    public DateTime? IssuedDate
    {
        get
        {
            var value = GetPropertyValue<string>("issued_date");
            return string.IsNullOrEmpty(value) ? null : DateTime.Parse(value);
        }
        set => SetPropertyValue("issued_date", value?.ToString("O"));
    }

    /// <summary>Date for which document will expire</summary>
    public DateTime? ExpiryDate
    {
        get
        {
            var value = GetPropertyValue<string>("expiry_date");
            return string.IsNullOrEmpty(value) ? null : DateTime.Parse(value);
        }
        set => SetPropertyValue("expiry_date", value?.ToString("O"));
    }
}