using Industrial.Adam.EquipmentScheduling.Infrastructure.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.EquipmentScheduling.Infrastructure.Health;

/// <summary>
/// Health check for Equipment Scheduling database context
/// </summary>
public class EquipmentSchedulingDbContextHealthCheck : IHealthCheck
{
    private readonly EquipmentSchedulingDbContext _context;
    private readonly ILogger<EquipmentSchedulingDbContextHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the health check
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="logger">The logger</param>
    public EquipmentSchedulingDbContextHealthCheck(
        EquipmentSchedulingDbContext context,
        ILogger<EquipmentSchedulingDbContextHealthCheck> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Performs the health check
    /// </summary>
    /// <param name="context">The health check context</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The health check result</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // For InMemory database, just ensure the context can be accessed
            await _context.Database.EnsureCreatedAsync(cancellationToken);

            _logger.LogDebug("Equipment Scheduling database health check passed");
            return HealthCheckResult.Healthy("Equipment Scheduling database is accessible");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Equipment Scheduling database health check failed");
            return HealthCheckResult.Unhealthy(
                "Equipment Scheduling database is not accessible",
                ex);
        }
    }
}
