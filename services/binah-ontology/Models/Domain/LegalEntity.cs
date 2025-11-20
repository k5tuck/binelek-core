using Binah.Core.Exceptions;
using Binah.Ontology.Models.Base;
using Binah.Ontology.Models.SupportModels;
namespace Binah.Ontology.Models.Domain;

/// <summary>
/// Represents a legal entity (company, LLC, trust, REIT, bank)
/// Label: Entity
/// </summary>
public class LegalEntity : Entity
{
    public LegalEntity()
    {
        Type = "Entity";
    }

    /// <summary>Canonical entity identifier (UUID)</summary>
    public string Uid
    {
        get => Id;
        set => Id = value;
    }

    /// <summary>Primary entity name</summary>
    public string Name
    {
        get => GetPropertyValue<string>("name") ?? string.Empty;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new EntityCreationException("Entity", "Name is required and cannot be empty");
            SetPropertyValue("name", value);
        }
    }

    /// <summary>Alternative names or DBAs</summary>
    public List<string>? Aliases
    {
        get => GetPropertyValue<List<string>>("aliases");
        set => SetPropertyValue("aliases", value);
    }

    /// <summary>Employer Identification Number or Tax ID</summary>
    public string? EinTaxId
    {
        get => GetPropertyValue<string>("ein_tax_id");
        set => SetPropertyValue("ein_tax_id", value);
    }

    /// <summary>Jurisdiction of registration (state/country)</summary>
    public string? Jurisdiction
    {
        get => GetPropertyValue<string>("jurisdiction");
        set => SetPropertyValue("jurisdiction", value);
    }

    /// <summary>Entity type: llc, corp, trust, individual, reit, bank</summary>
    public string? EntityTypeValue
    {
        get => GetPropertyValue<string>("entity_type");
        set => SetPropertyValue("entity_type", value);
    }

    /// <summary>Registered business address</summary>
    public Address? RegisteredAddress
    {
        get => GetPropertyValue<Address>("registered_address");
        set => SetPropertyValue("registered_address", value);
    }

    /// <summary>Total portfolio value</summary>
    public decimal? PortfolioValue
    {
        get => GetPropertyValue<decimal?>("portfolio_value");
        set => SetPropertyValue("portfolio_value", value);
    }

    /// <summary>Number of properties owned</summary>
    public int? NumPropertiesOwned
    {
        get => GetPropertyValue<int?>("num_properties_owned");
        set => SetPropertyValue("num_properties_owned", value);
    }

    /// <summary>Derived control score metric</summary>
    public double? ControlScore
    {
        get => GetPropertyValue<double?>("control_score");
        set => SetPropertyValue("control_score", value);
    }

    /// <summary>Source records for data provenance</summary>
    public List<SourceRecord>? SourceRecords
    {
        get => GetPropertyValue<List<SourceRecord>>("source_records");
        set => SetPropertyValue("source_records", value);
    }
}