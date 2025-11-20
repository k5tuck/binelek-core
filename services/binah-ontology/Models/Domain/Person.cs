using Binah.Core.Exceptions;
using Binah.Ontology.Models.Base;
using Binah.Ontology.Models.SupportModels;
namespace Binah.Ontology.Models.Domain;

/// <summary>
/// Represents an individual person
/// Label: Person
/// </summary>
public class Person : Entity
{
    public Person()
    {
        Type = "Person";
    }

    /// <summary>Canonical person identifier (UUID)</summary>
    public string Uid
    {
        get => Id;
        set => Id = value;
    }

    /// <summary>First name</summary>
    public string? FirstName
    {
        get => GetPropertyValue<string>("first_name");
        set => SetPropertyValue("first_name", value);
    }

    /// <summary>Last name</summary>
    public string? LastName
    {
        get => GetPropertyValue<string>("last_name");
        set => SetPropertyValue("last_name", value);
    }

    /// <summary>Full name</summary>
    public string FullName
    {
        get => GetPropertyValue<string>("full_name") ?? $"{FirstName} {LastName}".Trim();
        set => SetPropertyValue("full_name", value);
    }

    /// <summary>Roles: owner, principal, officer, registered_agent</summary>
    public List<string>? Roles
    {
        get => GetPropertyValue<List<string>>("roles");
        set => SetPropertyValue("roles", value);
    }

    /// <summary>Hashed identifiers (for PII protection - prefer hashed IDs over SSN)</summary>
    public Dictionary<string, string>? Identifiers
    {
        get => GetPropertyValue<Dictionary<string, string>>("identifiers");
        set => SetPropertyValue("identifiers", value);
    }

    /// <summary>Contact information (emails, phones)</summary>
    public ContactInfo? Contacts
    {
        get => GetPropertyValue<ContactInfo>("contacts");
        set => SetPropertyValue("contacts", value);
    }

    /// <summary>Professional credentials (licenses, broker_id)</summary>
    public Credentials? CredentialsInfo
    {
        get => GetPropertyValue<Credentials>("credentials");
        set => SetPropertyValue("credentials", value);
    }

    /// <summary>Source records for data provenance</summary>
    public List<SourceRecord>? SourceRecords
    {
        get => GetPropertyValue<List<SourceRecord>>("source_records");
        set => SetPropertyValue("source_records", value);
    }
}
