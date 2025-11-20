using System.Collections.Generic;

namespace Binah.Ontology.Models.DataNetwork;

/// <summary>
/// Network overview containing aggregated metrics
/// </summary>
public class NetworkOverview
{
    public long TotalEntities { get; set; }
    public long TotalRelationships { get; set; }
    public int KAnonymity { get; set; }
    public DateTime LastUpdated { get; set; }
    public decimal DataFreshness { get; set; }
    public decimal NetworkCoverage { get; set; }
    public decimal RelationshipDensity { get; set; }
    public int TotalEntityTypes { get; set; }
    public int TotalClusters { get; set; }
}

/// <summary>
/// Statistics for a specific entity type
/// </summary>
public class EntityStatistics
{
    public string EntityType { get; set; } = string.Empty;
    public long Count { get; set; }
    public decimal Growth { get; set; }
    public decimal AvgConnections { get; set; }
    public decimal Percentage { get; set; }
}

/// <summary>
/// AI-generated insight derived from cross-tenant data
/// </summary>
public class CrossTenantInsight
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Impact { get; set; } = string.Empty; // high, medium, low
    public decimal Confidence { get; set; }
    public string Category { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Data quality score for a specific dimension
/// </summary>
public class DataQualityScore
{
    public string Dimension { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public string Trend { get; set; } = string.Empty; // up, down, stable
    public List<string> Recommendations { get; set; } = new();
    public string Status { get; set; } = string.Empty; // excellent, good, fair, poor
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Overall data quality response
/// </summary>
public class DataQualityResponse
{
    public decimal OverallScore { get; set; }
    public List<DataQualityScore> Scores { get; set; } = new();
    public List<string> GlobalRecommendations { get; set; } = new();
}

/// <summary>
/// Trend data with predictions
/// </summary>
public class TrendData
{
    public string Period { get; set; } = string.Empty;
    public List<EntityCountTrend> EntityCounts { get; set; } = new();
    public List<Prediction> Predictions { get; set; } = new();
    public Dictionary<string, decimal> Metrics { get; set; } = new();
}

/// <summary>
/// Entity count trend over time
/// </summary>
public class EntityCountTrend
{
    public DateTime Date { get; set; }
    public long Count { get; set; }
    public string EntityType { get; set; } = string.Empty;
}

/// <summary>
/// Prediction for future trends
/// </summary>
public class Prediction
{
    public string Label { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public string TimeFrame { get; set; } = string.Empty;
}

/// <summary>
/// Graph data for network visualization
/// </summary>
public class GraphData
{
    public List<GraphNode> Nodes { get; set; } = new();
    public List<GraphEdge> Edges { get; set; } = new();
    public Dictionary<string, int> NodeCounts { get; set; } = new();
    public Dictionary<string, int> EdgeCounts { get; set; } = new();
}

/// <summary>
/// Node in the network graph
/// </summary>
public class GraphNode
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int Size { get; set; }
    public string Color { get; set; } = string.Empty;
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// Edge in the network graph
/// </summary>
public class GraphEdge
{
    public string Id { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int Weight { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// Recent activity in the network
/// </summary>
public class NetworkActivity
{
    public string Id { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Type { get; set; } = string.Empty;
}
