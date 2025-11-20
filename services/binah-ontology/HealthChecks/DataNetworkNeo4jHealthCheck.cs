using System;
using System.Threading;
using System.Threading.Tasks;
using Binah.Ontology.Infrastructure;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Binah.Ontology.HealthChecks;

/// <summary>
/// Health check for Data Network Neo4j connection
/// Verifies connectivity to the separate data network Neo4j instance
/// </summary>
public class DataNetworkNeo4jHealthCheck : IHealthCheck
{
    private readonly IDataNetworkNeo4jDriver? _driver;
    private readonly ILogger<DataNetworkNeo4jHealthCheck> _logger;

    public DataNetworkNeo4jHealthCheck(
        IDataNetworkNeo4jDriver? driver,
        ILogger<DataNetworkNeo4jHealthCheck> logger)
    {
        _driver = driver;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // If driver is null, data network is disabled (optional feature)
            if (_driver == null)
            {
                _logger.LogWarning("Data Network Neo4j driver is not configured. Data network feature is disabled.");
                return HealthCheckResult.Degraded(
                    "Data Network Neo4j is not configured. Data network contribution is disabled.",
                    data: new Dictionary<string, object>
                    {
                        { "configured", false },
                        { "feature", "optional" }
                    });
            }

            // Verify connectivity to data network Neo4j
            await _driver.Driver.VerifyConnectivityAsync();

            return HealthCheckResult.Healthy(
                "Data Network Neo4j connection is healthy",
                data: new Dictionary<string, object>
                {
                    { "configured", true },
                    { "connected", true }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Data Network Neo4j health check failed");

            return HealthCheckResult.Degraded(
                $"Data Network Neo4j connection failed: {ex.Message}",
                ex,
                data: new Dictionary<string, object>
                {
                    { "configured", _driver != null },
                    { "connected", false },
                    { "error", ex.Message }
                });
        }
    }
}
