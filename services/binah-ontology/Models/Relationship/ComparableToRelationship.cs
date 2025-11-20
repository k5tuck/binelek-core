using Binah.Ontology.Models.Exceptions;

namespace Binah.Ontology.Models.Relationship;

/// <summary>
/// COMPARABLE_TO relationship: Property is comparable to another Property (for market analysis)
/// Direction: Property -> Property
/// </summary>
public class ComparableToRelationship : Relationship
{
    public ComparableToRelationship()
    {
        Type = "COMPARABLE_TO";
    }

    /// <summary>Similarity score (0.0 - 1.0)</summary>
    public double? SimilarityScore
    {
        get => GetPropertyValue<double?>("similarity_score");
        set
        {
            if (value.HasValue && (value < 0 || value > 1))
                throw new RelationshipCreationException("COMPARABLE_TO", "Similarity score must be between 0 and 1");
            SetPropertyValue("similarity_score", value);
        }
    }

    /// <summary>Distance between properties (meters)</summary>
    public double? Distance
    {
        get => GetPropertyValue<double?>("distance");
        set => SetPropertyValue("distance", value);
    }

    /// <summary>Comparison factors: size, location, age, condition, etc.</summary>
    public List<string>? ComparisonFactors
    {
        get => GetPropertyValue<List<string>>("comparison_factors");
        set => SetPropertyValue("comparison_factors", value);
    }

    /// <summary>Price difference percentage</summary>
    public double? PriceDifferencePercent
    {
        get => GetPropertyValue<double?>("price_difference_percent");
        set => SetPropertyValue("price_difference_percent", value);
    }
}