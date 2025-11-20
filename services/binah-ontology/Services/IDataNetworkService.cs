using Binah.Ontology.Models.DataNetwork;

namespace Binah.Ontology.Services;

/// <summary>
/// Service interface for Data Network analytics operations
/// Connects to the PII-scrubbed Neo4j instance (port 7688)
/// </summary>
public interface IDataNetworkService
{
    /// <summary>
    /// Get aggregated network overview metrics
    /// </summary>
    Task<NetworkOverview> GetNetworkOverviewAsync();

    /// <summary>
    /// Get entity statistics by type
    /// </summary>
    Task<List<EntityStatistics>> GetEntityStatisticsAsync();

    /// <summary>
    /// Get AI-generated cross-tenant insights
    /// </summary>
    Task<List<CrossTenantInsight>> GetCrossTenantInsightsAsync(int limit = 10);

    /// <summary>
    /// Get data quality scores
    /// </summary>
    Task<DataQualityResponse> GetDataQualityAsync();

    /// <summary>
    /// Get trend analysis with predictions
    /// </summary>
    Task<TrendData> GetTrendAnalysisAsync(string period);

    /// <summary>
    /// Get graph data for network visualization
    /// </summary>
    Task<GraphData> GetNetworkGraphAsync(int nodeLimit = 100);

    /// <summary>
    /// Get recent network activity
    /// </summary>
    Task<List<NetworkActivity>> GetRecentActivityAsync(int limit = 10);
}
