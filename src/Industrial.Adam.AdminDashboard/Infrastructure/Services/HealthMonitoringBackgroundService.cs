using Industrial.Adam.AdminDashboard.Application.DTOs;
using Industrial.Adam.AdminDashboard.Domain.Interfaces;
using Industrial.Adam.AdminDashboard.Infrastructure.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.AdminDashboard.Infrastructure.Services;

/// <summary>
/// Background service for continuous health monitoring and real-time updates
/// </summary>
public class HealthMonitoringBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HealthMonitoringBackgroundService> _logger;
    private readonly TimeSpan _healthCheckInterval = TimeSpan.FromSeconds(30);
    private readonly TimeSpan _metricsInterval = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Constructor for HealthMonitoringBackgroundService
    /// </summary>
    /// <param name="scopeFactory">Service scope factory</param>
    /// <param name="logger">Logger instance</param>
    public HealthMonitoringBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<HealthMonitoringBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Execute the background service
    /// </summary>
    /// <param name="stoppingToken">Cancellation token</param>
    /// <returns>Task</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Health monitoring background service started");

        // Start both health check and metrics collection tasks
        var healthTask = PeriodicHealthChecks(stoppingToken);
        var metricsTask = PeriodicMetricsCollection(stoppingToken);

        try
        {
            await Task.WhenAll(healthTask, metricsTask);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Health monitoring background service stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health monitoring background service encountered an error");
        }
    }

    private async Task PeriodicHealthChecks(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var healthService = scope.ServiceProvider.GetRequiredService<ISystemHealthService>();
                var healthUpdateService = scope.ServiceProvider.GetRequiredService<IHealthUpdateService>();

                // Check all services health
                var healthResults = await healthService.CheckAllServicesAsync(stoppingToken);

                // Convert to DTOs
                var healthDtos = healthResults.Select(result => new ServiceHealthDto
                {
                    ServiceName = result.ServiceName,
                    Status = result.Status,
                    ResponseTimeMs = result.ResponseTimeMs,
                    ErrorCount = result.ErrorCount,
                    Timestamp = result.Timestamp,
                    ErrorMessage = result.ErrorMessage,
                    AdditionalData = result.AdditionalData
                }).ToList();

                // Send real-time updates
                await healthUpdateService.SendHealthUpdateAsync(healthDtos, stoppingToken);

                // TODO: Store metrics in database for historical analysis
                // This will be implemented when we add the SystemHealthMetricRepository integration

                _logger.LogDebug("Completed health check cycle for {ServiceCount} services", healthDtos.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during health check cycle");
            }

            await Task.Delay(_healthCheckInterval, stoppingToken);
        }
    }

    private async Task PeriodicMetricsCollection(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var healthService = scope.ServiceProvider.GetRequiredService<ISystemHealthService>();
                var healthUpdateService = scope.ServiceProvider.GetRequiredService<IHealthUpdateService>();

                // Collect system metrics
                var metrics = await healthService.GetSystemMetricsAsync(stoppingToken);

                // Convert to DTO
                var metricsDto = new SystemMetricsDto
                {
                    CpuUsagePercent = metrics.CpuUsagePercent,
                    MemoryUsageBytes = metrics.MemoryUsageBytes,
                    TotalMemoryBytes = metrics.TotalMemoryBytes,
                    DiskUsageBytes = metrics.DiskUsageBytes,
                    TotalDiskBytes = metrics.TotalDiskBytes,
                    UptimeSeconds = metrics.UptimeSeconds,
                    Timestamp = metrics.Timestamp,
                    AdditionalMetrics = metrics.AdditionalMetrics
                };

                // Send real-time updates
                await healthUpdateService.SendSystemMetricsUpdateAsync(metricsDto, stoppingToken);

                _logger.LogDebug("Completed metrics collection - CPU: {CpuUsage}%, Memory: {MemoryUsage}%",
                    metricsDto.CpuUsagePercent,
                    metricsDto.TotalMemoryBytes > 0 ? (double)metricsDto.MemoryUsageBytes / metricsDto.TotalMemoryBytes * 100 : 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during metrics collection");
            }

            await Task.Delay(_metricsInterval, stoppingToken);
        }
    }

    /// <summary>
    /// Cleanup when service stops
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Health monitoring background service is stopping");
        await base.StopAsync(cancellationToken);
    }
}