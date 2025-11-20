using Microsoft.Extensions.Diagnostics.HealthChecks;
using Neo4j.Driver; 

public class Neo4jHealthCheck : IHealthCheck
{
    private readonly IDriver _driver;

    public Neo4jHealthCheck(IDriver driver)
    {
        _driver = driver;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Open a session and run a simple query to verify connectivity
            await using var session = _driver.AsyncSession();
            await session.RunAsync("MATCH (n) RETURN n LIMIT 1"); 

            return HealthCheckResult.Healthy("Neo4j is healthy.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Neo4j is unhealthy.", ex);
        }
    }
}