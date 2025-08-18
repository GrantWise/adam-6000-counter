using Microsoft.AspNetCore.Mvc;

namespace Industrial.Adam.Oee.WebApi.Controllers;

/// <summary>
/// Health check controller for monitoring API status
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    /// <summary>
    /// Constructor for health controller
    /// </summary>
    /// <param name="logger">Logger instance</param>
    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Basic health check endpoint
    /// </summary>
    /// <returns>Health status information</returns>
    [HttpGet]
    public IActionResult GetHealth()
    {
        var response = new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Service = "OEE API",
            Version = "1.0.0"
        };

        _logger.LogDebug("Health check requested - Status: {Status}", response.Status);

        return Ok(response);
    }

    /// <summary>
    /// Detailed health check with dependencies
    /// </summary>
    /// <returns>Detailed health status information</returns>
    [HttpGet("detailed")]
    public IActionResult GetDetailedHealth()
    {
        var response = new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Service = "OEE API",
            Version = "1.0.0",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
            Dependencies = new
            {
                Database = "Healthy", // Will be updated when we add actual health checks
                TimescaleDB = "Healthy"
            },
            Uptime = Environment.TickCount64 / 1000 // Seconds since start
        };

        _logger.LogInformation("Detailed health check requested - Status: {Status}", response.Status);

        return Ok(response);
    }
}
