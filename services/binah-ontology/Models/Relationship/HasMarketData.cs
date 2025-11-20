namespace Binah.Ontology.Models.Relationship;

/// <summary>
/// HAS_MARKET_DATA relationship: Entity has associated MarketData
/// Direction: Entity -> MarketData
/// </summary>
public class HasMarketDataRelationship : Relationship
{
    public HasMarketDataRelationship()
    {
        Type = "HAS_MARKET_DATA";
    }

    /// <summary>Relevance score of this market data to the entity</summary>
    public double? RelevanceScore
    {
        get => GetPropertyValue<double?>("relevance_score");
        set => SetPropertyValue("relevance_score", value);
    }

    /// <summary>Type of market data relationship: direct, comparable, trend</summary>
    public string? RelationType
    {
        get => GetPropertyValue<string>("relation_type");
        set => SetPropertyValue("relation_type", value);
    }
}