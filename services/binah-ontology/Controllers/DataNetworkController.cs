using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Binah.Ontology.Services;
using Binah.Ontology.Models.DataNetwork;
using Binah.Contracts.Common;

namespace Binah.Ontology.Controllers;

/// <summary>
/// REST API controller for Data Network analytics
/// Provides aggregated, anonymized insights from the PII-scrubbed Neo4j instance
/// </summary>
[ApiController]
[Route("api/data-network")]
[Produces("application/json")]
[Authorize]
public class DataNetworkController : ControllerBase
{
    private readonly IDataNetworkService _dataNetworkService;
    private readonly ILogger<DataNetworkController> _logger;

    public DataNetworkController(
        IDataNetworkService dataNetworkService,
        ILogger<DataNetworkController> logger)
    {
        _dataNetworkService = dataNetworkService ?? throw new ArgumentNullException(nameof(dataNetworkService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get aggregated network overview metrics
    /// </summary>
    /// <returns>Network overview with total entities, relationships, and key metrics</returns>
    /// <response code="200">Overview retrieved successfully</response>
    /// <response code="401">Authentication required</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("overview")]
    [ProducesResponseType(typeof(ApiResponse<NetworkOverview>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<NetworkOverview>>> GetOverview()
    {
        try
        {
            _logger.LogInformation("Retrieving network overview");
            var overview = await _dataNetworkService.GetNetworkOverviewAsync();
            return Ok(ApiResponse<NetworkOverview>.Ok(overview));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving network overview");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while retrieving network overview",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Get entity statistics by type
    /// </summary>
    /// <returns>List of entity statistics with counts, growth rates, and percentages</returns>
    /// <response code="200">Statistics retrieved successfully</response>
    /// <response code="401">Authentication required</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("entities")]
    [ProducesResponseType(typeof(ApiResponse<List<EntityStatistics>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<EntityStatistics>>>> GetEntityStatistics()
    {
        try
        {
            _logger.LogInformation("Retrieving entity statistics");
            var statistics = await _dataNetworkService.GetEntityStatisticsAsync();
            return Ok(ApiResponse<List<EntityStatistics>>.Ok(statistics));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving entity statistics");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while retrieving entity statistics",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Get AI-generated cross-tenant insights
    /// </summary>
    /// <param name="limit">Maximum number of insights to return (default: 10)</param>
    /// <returns>List of anonymized insights derived from aggregated data</returns>
    /// <response code="200">Insights retrieved successfully</response>
    /// <response code="401">Authentication required</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("insights")]
    [ProducesResponseType(typeof(ApiResponse<List<CrossTenantInsight>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<CrossTenantInsight>>>> GetCrossTenantInsights(
        [FromQuery] int limit = 10)
    {
        try
        {
            _logger.LogInformation("Retrieving cross-tenant insights (limit: {Limit})", limit);
            var insights = await _dataNetworkService.GetCrossTenantInsightsAsync(limit);
            return Ok(ApiResponse<List<CrossTenantInsight>>.Ok(insights));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cross-tenant insights");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while retrieving insights",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Get data quality scores across all dimensions
    /// </summary>
    /// <returns>Data quality scores with recommendations</returns>
    /// <response code="200">Quality scores retrieved successfully</response>
    /// <response code="401">Authentication required</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("quality")]
    [ProducesResponseType(typeof(ApiResponse<DataQualityResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<DataQualityResponse>>> GetDataQuality()
    {
        try
        {
            _logger.LogInformation("Retrieving data quality scores");
            var quality = await _dataNetworkService.GetDataQualityAsync();
            return Ok(ApiResponse<DataQualityResponse>.Ok(quality));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving data quality scores");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while retrieving data quality scores",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Get trend analysis with predictions
    /// </summary>
    /// <param name="period">Time period for analysis (7d, 30d, 90d, 1y)</param>
    /// <returns>Trend data with entity counts and predictions</returns>
    /// <response code="200">Trends retrieved successfully</response>
    /// <response code="400">Invalid period parameter</response>
    /// <response code="401">Authentication required</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("trends")]
    [ProducesResponseType(typeof(ApiResponse<TrendData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<TrendData>>> GetTrends(
        [FromQuery] string period = "30d")
    {
        try
        {
            // Validate period
            var validPeriods = new[] { "7d", "30d", "90d", "1y" };
            if (!validPeriods.Contains(period))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Period",
                    Detail = $"Period must be one of: {string.Join(", ", validPeriods)}",
                    Status = StatusCodes.Status400BadRequest,
                    Instance = HttpContext.Request.Path
                });
            }

            _logger.LogInformation("Retrieving trend analysis for period {Period}", period);
            var trends = await _dataNetworkService.GetTrendAnalysisAsync(period);
            return Ok(ApiResponse<TrendData>.Ok(trends));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving trend analysis");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while retrieving trend analysis",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Get graph data for network visualization
    /// </summary>
    /// <param name="nodeLimit">Maximum number of nodes to return (default: 100)</param>
    /// <returns>Graph nodes and edges for visualization</returns>
    /// <response code="200">Graph data retrieved successfully</response>
    /// <response code="401">Authentication required</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("graph")]
    [ProducesResponseType(typeof(ApiResponse<GraphData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<GraphData>>> GetNetworkGraph(
        [FromQuery] int nodeLimit = 100)
    {
        try
        {
            // Limit maximum nodes for performance
            if (nodeLimit > 500)
            {
                nodeLimit = 500;
            }

            _logger.LogInformation("Retrieving network graph (nodeLimit: {NodeLimit})", nodeLimit);
            var graph = await _dataNetworkService.GetNetworkGraphAsync(nodeLimit);
            return Ok(ApiResponse<GraphData>.Ok(graph));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving network graph");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while retrieving network graph",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Get recent network activity
    /// </summary>
    /// <param name="limit">Maximum number of activities to return (default: 10)</param>
    /// <returns>List of recent network activities</returns>
    /// <response code="200">Activities retrieved successfully</response>
    /// <response code="401">Authentication required</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("activity")]
    [ProducesResponseType(typeof(ApiResponse<List<NetworkActivity>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<NetworkActivity>>>> GetRecentActivity(
        [FromQuery] int limit = 10)
    {
        try
        {
            _logger.LogInformation("Retrieving recent network activity (limit: {Limit})", limit);
            var activities = await _dataNetworkService.GetRecentActivityAsync(limit);
            return Ok(ApiResponse<List<NetworkActivity>>.Ok(activities));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent activity");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while retrieving recent activity",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }
}
