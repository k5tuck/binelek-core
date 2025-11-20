using Binah.Core.Exceptions;
using Binah.Ontology.Models.Base;
using Binah.Ontology.Models.SupportModels;
namespace Binah.Ontology.Models.Domain;

/// <summary>
/// Represents a real property (land, building, or both)
/// Label: Property
/// </summary>
public class Property : Entity
{
    public Property()
    {
        Type = "Property";
    }

    /// <summary>Canonical property identifier (UUID)</summary>
    public string Uid
    {
        get => Id;
        set => Id = value;
    }

    /// <summary>County parcel identifier</summary>
    public string? ParcelId
    {
        get => GetPropertyValue<string>("parcel_id");
        set => SetPropertyValue("parcel_id", value);
    }

    /// <summary>Property address</summary>
    public Address? Address
    {
        get => GetPropertyValue<Address>("address");
        set => SetPropertyValue("address", value);
    }

    /// <summary>Geographic coordinates (centroid latitude, longitude)</summary>
    public GeoPoint? Coordinates
    {
        get => GetPropertyValue<GeoPoint>("coordinates");
        set => SetPropertyValue("coordinates", value);
    }

    /// <summary>Property boundary polygon (link to object store or WKT string)</summary>
    public string? Boundary
    {
        get => GetPropertyValue<string>("boundary");
        set => SetPropertyValue("boundary", value);
    }

    /// <summary>Property use type: residential, commercial, mixed, land, industrial</summary>
    public string? UseType
    {
        get => GetPropertyValue<string>("use_type");
        set => SetPropertyValue("use_type", value);
    }

    /// <summary>Building square footage</summary>
    public double? Sqft
    {
        get => GetPropertyValue<double?>("sqft");
        set => SetPropertyValue("sqft", value);
    }

    /// <summary>Lot square footage</summary>
    public double? LotSqft
    {
        get => GetPropertyValue<double?>("lot_sqft");
        set => SetPropertyValue("lot_sqft", value);
    }

    /// <summary>Assessed value for tax purposes</summary>
    public decimal? AssessedValue
    {
        get => GetPropertyValue<decimal?>("assessed_value");
        set => SetPropertyValue("assessed_value", value);
    }

    /// <summary>Last known market value</summary>
    public decimal? MarketValue
    {
        get => GetPropertyValue<decimal?>("market_value");
        set => SetPropertyValue("market_value", value);
    }

    /// <summary>Last sale transaction details</summary>
    public SaleTransaction? LastSale
    {
        get => GetPropertyValue<SaleTransaction>("last_sale");
        set => SetPropertyValue("last_sale", value);
    }

    /// <summary>Zoning code</summary>
    public string? ZoningCode
    {
        get => GetPropertyValue<string>("zoning_code");
        set => SetPropertyValue("zoning_code", value);
    }

    /// <summary>Available utilities (e.g., water, sewer, gas, electric)</summary>
    public List<string>? Utilities
    {
        get => GetPropertyValue<List<string>>("utilities");
        set => SetPropertyValue("utilities", value);
    }

    /// <summary>Property status: active, offmarket, pending, distressed</summary>
    public string? Status
    {
        get => GetPropertyValue<string>("status");
        set => SetPropertyValue("status", value);
    }

    /// <summary>Source records for data provenance</summary>
    public List<SourceRecord>? SourceRecords
    {
        get => GetPropertyValue<List<SourceRecord>>("source_records");
        set => SetPropertyValue("source_records", value);
    }
}
