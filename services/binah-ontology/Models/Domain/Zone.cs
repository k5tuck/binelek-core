using Binah.Ontology.Models.SupportModels;
using Binah.Ontology.Models.Base;
using Binah.Core.Exceptions;

namespace Binah.Ontology.Models.Domain;

/// <summary>
/// Represents a geographic zone (census tract, zoning district, block, etc.)
/// Label: Zone
/// </summary>
public class Zone : Entity
{
    public Zone()
    {
        Type = "Zone";
    }

    /// <summary>Canonical zone identifier (UUID)</summary>
    public string Uid
    {
        get => Id;
        set => Id = value;
    }

    /// <summary>Zone name (e.g., "Block A", "Census Tract 123.01")</summary>
    public string Name
    {
        get => GetPropertyValue<string>("name") ?? string.Empty;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new EntityCreationException("Zone", "Name is required and cannot be empty");
            SetPropertyValue("name", value);
        }
    }

    /// <summary>Zone boundary polygon (WKT or reference)</summary>
    public string? Boundary
    {
        get => GetPropertyValue<string>("boundary");
        set => SetPropertyValue("boundary", value);
    }

    /// <summary>Zoning type classification</summary>
    public string? ZoningType
    {
        get => GetPropertyValue<string>("zoning_type");
        set => SetPropertyValue("zoning_type", value);
    }

    /// <summary>Link to demographics data warehouse record</summary>
    public string? Demographics
    {
        get => GetPropertyValue<string>("demographics");
        set => SetPropertyValue("demographics", value);
    }

    /// <summary>Population/economic growth rate</summary>
    public double? GrowthRate
    {
        get => GetPropertyValue<double?>("growth_rate");
        set => SetPropertyValue("growth_rate", value);
    }

    /// <summary>Risk assessment scores (flood, seismic, environmental)</summary>
    public RiskScores? RiskScoresValue
    {
        get => GetPropertyValue<RiskScores>("risk_scores");
        set => SetPropertyValue("risk_scores", value);
    }
}