using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Binah.Auth.Controllers
{
    /// <summary>
    /// Health check endpoints for Kubernetes liveness and readiness probes
    /// </summary>
    [ApiController]
    [Route("health")]
    [AllowAnonymous]  // Health checks should not require authentication
    public class HealthController : ControllerBase
    {
        private readonly HealthCheckService _healthCheckService;
        private readonly ILogger<HealthController> _logger;

        public HealthController(
            HealthCheckService healthCheckService,
            ILogger<HealthController> logger)
        {
            _healthCheckService = healthCheckService;
            _logger = logger;
        }

        /// <summary>
        /// Overall health status with all dependency checks
        /// Returns 200 OK when healthy, 503 Service Unavailable when unhealthy
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                var report = await _healthCheckService.CheckHealthAsync();

                var response = new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description,
                        duration = e.Value.Duration.TotalMilliseconds,
                        exception = e.Value.Exception?.Message
                    }),
                    totalDuration = report.TotalDuration.TotalMilliseconds
                };

                var statusCode = report.Status == HealthStatus.Healthy ? 200 : 503;

                if (statusCode == 503)
                {
                    _logger.LogWarning("Health check failed: {Status}", report.Status);
                }

                return StatusCode(statusCode, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check endpoint failed");
                return StatusCode(503, new { status = "unhealthy", error = "Health check failed" });
            }
        }

        /// <summary>
        /// Readiness probe - indicates if the service can accept traffic
        /// Used by Kubernetes to determine if pod should receive requests
        /// </summary>
        [HttpGet("ready")]
        public async Task<IActionResult> Ready()
        {
            try
            {
                var report = await _healthCheckService.CheckHealthAsync();

                if (report.Status == HealthStatus.Healthy)
                {
                    return Ok(new { status = "ready", timestamp = DateTime.UtcNow });
                }

                _logger.LogWarning("Readiness check failed: {Status}", report.Status);
                return StatusCode(503, new { status = "not_ready", timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Readiness check failed");
                return StatusCode(503, new { status = "not_ready", error = ex.Message });
            }
        }

        /// <summary>
        /// Liveness probe - indicates if the process is running
        /// Used by Kubernetes to determine if pod should be restarted
        /// Should always return 200 OK unless the process is completely dead
        /// </summary>
        [HttpGet("live")]
        public IActionResult Live()
        {
            return Ok(new
            {
                status = "alive",
                service = "binah-auth",
                timestamp = DateTime.UtcNow
            });
        }
    }
}
