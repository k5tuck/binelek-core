using Binah.Core.Exceptions;
using Binah.Ontology.Models.Base;
using Binah.Ontology.Models.SupportModels;
namespace Binah.Ontology.Models.Domain;

/// <summary>
/// Represents market data and trends
/// Label: MarketData
/// </summary>
public class MarketData : Entity
{
    public MarketData()
    {
        Type = "MarketData";
    }

    /// <summary>Canonical market data identifier (UUID)</summary>
    public string Uid
    {
        get => Id;
        set => Id = value;
    }

    /// <summary>Market data type: sale, listing, rental, comparable, trend</summary>
    public string DataType
    {
        get => GetPropertyValue<string>("data_type") ?? "sale";
        set => SetPropertyValue("data_type", value);
    }

    /// <summary>Geographic scope: property, zone, city, county, state, national</summary>
    public string GeographicScope
    {
        get => GetPropertyValue<string>("geographic_scope") ?? "property";
        set => SetPropertyValue("geographic_scope", value);
    }

    /// <summary>Geographic identifier (property ID, zone ID, city name, etc.)</summary>
    public string? GeographicId
    {
        get => GetPropertyValue<string>("geographic_id");
        set => SetPropertyValue("geographic_id", value);
    }

    /// <summary>Time period start date</summary>
    public DateTime PeriodStart
    {
        get => GetPropertyValue<DateTime>("period_start", DateTime.UtcNow.AddMonths(-1));
        set => SetPropertyValue("period_start", value);
    }

    /// <summary>Time period end date</summary>
    public DateTime PeriodEnd
    {
        get => GetPropertyValue<DateTime>("period_end", DateTime.UtcNow);
        set => SetPropertyValue("period_end", value);
    }

    /// <summary>Median sale price</summary>
    public decimal? MedianPrice
    {
        get => GetPropertyValue<decimal?>("median_price");
        set => SetPropertyValue("median_price", value);
    }

    /// <summary>Average sale price</summary>
    public decimal? AveragePrice
    {
        get => GetPropertyValue<decimal?>("average_price");
        set => SetPropertyValue("average_price", value);
    }

    /// <summary>Price per square foot</summary>
    public decimal? PricePerSqft
    {
        get => GetPropertyValue<decimal?>("price_per_sqft");
        set => SetPropertyValue("price_per_sqft", value);
    }

    /// <summary>Number of sales in period</summary>
    public int? SalesCount
    {
        get => GetPropertyValue<int?>("sales_count");
        set => SetPropertyValue("sales_count", value);
    }

    /// <summary>Number of active listings</summary>
    public int? ListingsCount
    {
        get => GetPropertyValue<int?>("listings_count");
        set => SetPropertyValue("listings_count", value);
    }

    /// <summary>Average days on market</summary>
    public int? DaysOnMarket
    {
        get => GetPropertyValue<int?>("days_on_market");
        set => SetPropertyValue("days_on_market", value);
    }

    /// <summary>Inventory level (months of supply)</summary>
    public double? MonthsOfSupply
    {
        get => GetPropertyValue<double?>("months_of_supply");
        set => SetPropertyValue("months_of_supply", value);
    }

    /// <summary>Price change from previous period (percentage)</summary>
    public double? PriceChangePercent
    {
        get => GetPropertyValue<double?>("price_change_percent");
        set => SetPropertyValue("price_change_percent", value);
    }

    /// <summary>Year-over-year price change (percentage)</summary>
    public double? YearOverYearChange
    {
        get => GetPropertyValue<double?>("year_over_year_change");
        set => SetPropertyValue("year_over_year_change", value);
    }

    /// <summary>Absorption rate (sales per month)</summary>
    public double? AbsorptionRate
    {
        get => GetPropertyValue<double?>("absorption_rate");
        set => SetPropertyValue("absorption_rate", value);
    }

    /// <summary>List to sale price ratio</summary>
    public double? ListToSaleRatio
    {
        get => GetPropertyValue<double?>("list_to_sale_ratio");
        set => SetPropertyValue("list_to_sale_ratio", value);
    }

    /// <summary>Market temperature: hot, warm, balanced, cool, cold</summary>
    public string? MarketTemperature
    {
        get => GetPropertyValue<string>("market_temperature");
        set => SetPropertyValue("market_temperature", value);
    }

    /// <summary>Property type filter: residential, commercial, mixed, land, industrial</summary>
    public string? PropertyTypeFilter
    {
        get => GetPropertyValue<string>("property_type_filter");
        set => SetPropertyValue("property_type_filter", value);
    }

    /// <summary>Additional statistics and metrics</summary>
    public Dictionary<string, object>? Statistics
    {
        get => GetPropertyValue<Dictionary<string, object>>("statistics");
        set => SetPropertyValue("statistics", value);
    }

    /// <summary>Data source and confidence score</summary>
    public double? ConfidenceScore
    {
        get => GetPropertyValue<double?>("confidence_score");
        set => SetPropertyValue("confidence_score", value);
    }

    /// <summary>Source records for data provenance</summary>
    public List<SourceRecord>? SourceRecords
    {
        get => GetPropertyValue<List<SourceRecord>>("source_records");
        set => SetPropertyValue("source_records", value);
    }
}