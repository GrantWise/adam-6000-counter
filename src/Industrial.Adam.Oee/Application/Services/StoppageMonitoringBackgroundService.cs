using Industrial.Adam.Oee.Application.Events;
using Industrial.Adam.Oee.Domain.Events;
using Industrial.Adam.Oee.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Industrial.Adam.Oee.Application.Services;

/// <summary>
/// Background service for continuous monitoring of equipment lines for stoppages
/// Runs on a configurable interval to detect production stoppages automatically
/// </summary>
public sealed class StoppageMonitoringBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StoppageMonitoringBackgroundService> _logger;
    private readonly StoppageMonitoringOptions _options;
    private readonly SemaphoreSlim _monitoringSemaphore;

    /// <summary>
    /// Constructor for stoppage monitoring background service
    /// </summary>
    public StoppageMonitoringBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<StoppageMonitoringBackgroundService> logger,
        IOptions<StoppageMonitoringOptions> options)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _monitoringSemaphore = new SemaphoreSlim(1, 1);
    }

    /// <summary>
    /// Execute the background monitoring service
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Stoppage monitoring background service starting with {IntervalSeconds}s interval",
            _options.MonitoringIntervalSeconds);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var startTime = DateTime.UtcNow;
                    await PerformMonitoringCycleAsync(stoppingToken);
                    var duration = DateTime.UtcNow - startTime;

                    _logger.LogDebug("Monitoring cycle completed in {DurationMs}ms", duration.TotalMilliseconds);

                    // Wait for the configured interval
                    var delay = TimeSpan.FromSeconds(_options.MonitoringIntervalSeconds);
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    _logger.LogInformation("Stoppage monitoring service cancellation requested");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in monitoring cycle, continuing with next cycle");

                    // Wait a shorter interval on error to avoid rapid retries
                    var errorDelay = TimeSpan.FromSeconds(Math.Min(_options.MonitoringIntervalSeconds, 30));
                    await Task.Delay(errorDelay, stoppingToken);
                }
            }
        }
        finally
        {
            _logger.LogInformation("Stoppage monitoring background service stopped");
        }
    }

    /// <summary>
    /// Perform a single monitoring cycle
    /// </summary>
    private async Task PerformMonitoringCycleAsync(CancellationToken cancellationToken)
    {
        if (!_options.IsEnabled)
        {
            _logger.LogTrace("Stoppage monitoring is disabled, skipping cycle");
            return;
        }

        // Use semaphore to prevent overlapping monitoring cycles
        if (!await _monitoringSemaphore.WaitAsync(100, cancellationToken))
        {
            _logger.LogWarning("Previous monitoring cycle still running, skipping this cycle");
            return;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var detectionService = scope.ServiceProvider.GetRequiredService<IStoppageDetectionService>();
            var eventHandler = scope.ServiceProvider.GetRequiredService<IEventHandler<StoppageDetectedEvent>>();

            _logger.LogTrace("Starting stoppage detection cycle");

            // Monitor all active lines for stoppages
            var detectedEvents = await detectionService.MonitorAllLinesAsync(cancellationToken);

            // Process each detected stoppage
            var processedCount = 0;
            foreach (var stoppageEvent in detectedEvents)
            {
                try
                {
                    await eventHandler.HandleAsync(stoppageEvent, cancellationToken);
                    processedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process stoppage event {EventId} for line {LineId}",
                        stoppageEvent.EventId, stoppageEvent.LineId);
                    // Continue processing other events
                }
            }

            if (processedCount > 0)
            {
                _logger.LogInformation("Processed {ProcessedCount} stoppage events in monitoring cycle", processedCount);
            }
            else
            {
                _logger.LogTrace("No stoppages detected in monitoring cycle");
            }

            // Update monitoring statistics
            await UpdateMonitoringStatisticsAsync(detectedEvents.Count(), processedCount, scope.ServiceProvider, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete monitoring cycle");
            throw;
        }
        finally
        {
            _monitoringSemaphore.Release();
        }
    }

    /// <summary>
    /// Update monitoring statistics for health checks and diagnostics
    /// </summary>
    private async Task UpdateMonitoringStatisticsAsync(
        int detectedCount,
        int processedCount,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        try
        {
            var statisticsService = serviceProvider.GetService<IMonitoringStatisticsService>();
            if (statisticsService != null)
            {
                var statistics = new MonitoringCycleStatistics(
                    DateTime.UtcNow,
                    detectedCount,
                    processedCount,
                    TimeSpan.FromSeconds(_options.MonitoringIntervalSeconds)
                );

                await statisticsService.RecordCycleStatisticsAsync(statistics, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update monitoring statistics");
            // Don't rethrow - statistics failures shouldn't affect monitoring
        }
    }

    /// <summary>
    /// Clean up resources
    /// </summary>
    public override void Dispose()
    {
        _monitoringSemaphore?.Dispose();
        base.Dispose();
    }
}

/// <summary>
/// Configuration options for stoppage monitoring
/// </summary>
public sealed class StoppageMonitoringOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "StoppageMonitoring";

    /// <summary>
    /// Whether stoppage monitoring is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// How often to check for stoppages (in seconds)
    /// </summary>
    public int MonitoringIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum number of lines to monitor per cycle
    /// </summary>
    public int MaxLinesPerCycle { get; set; } = 50;

    /// <summary>
    /// Timeout for each monitoring cycle (in seconds)
    /// </summary>
    public int CycleTimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Whether to enable detailed logging for monitoring cycles
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// Maximum number of concurrent line monitoring operations
    /// </summary>
    public int MaxConcurrency { get; set; } = 10;

    /// <summary>
    /// Whether to monitor lines without active work orders
    /// </summary>
    public bool MonitorIdleLines { get; set; } = true;

    /// <summary>
    /// Hours of recent activity required to consider a line active for monitoring
    /// </summary>
    public int RecentActivityHours { get; set; } = 4;

    /// <summary>
    /// Validate configuration options
    /// </summary>
    public void Validate()
    {
        if (MonitoringIntervalSeconds < 10)
            throw new ArgumentException("Monitoring interval must be at least 10 seconds");

        if (MonitoringIntervalSeconds > 600)
            throw new ArgumentException("Monitoring interval cannot exceed 10 minutes");

        if (MaxLinesPerCycle < 1)
            throw new ArgumentException("Max lines per cycle must be at least 1");

        if (CycleTimeoutSeconds < 30)
            throw new ArgumentException("Cycle timeout must be at least 30 seconds");

        if (MaxConcurrency < 1)
            throw new ArgumentException("Max concurrency must be at least 1");

        if (RecentActivityHours < 1)
            throw new ArgumentException("Recent activity hours must be at least 1");
    }
}

/// <summary>
/// Statistics from a monitoring cycle
/// </summary>
/// <param name="CycleTime">When the cycle occurred</param>
/// <param name="StoppagesDetected">Number of stoppages detected</param>
/// <param name="EventsProcessed">Number of events successfully processed</param>
/// <param name="CycleDuration">How long the cycle took</param>
public record MonitoringCycleStatistics(
    DateTime CycleTime,
    int StoppagesDetected,
    int EventsProcessed,
    TimeSpan CycleDuration
);

/// <summary>
/// Service interface for monitoring statistics
/// </summary>
public interface IMonitoringStatisticsService
{
    /// <summary>
    /// Record statistics from a monitoring cycle
    /// </summary>
    public Task RecordCycleStatisticsAsync(MonitoringCycleStatistics statistics, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get monitoring health status
    /// </summary>
    public Task<MonitoringHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Health status of the monitoring service
/// </summary>
/// <param name="IsHealthy">Whether monitoring is healthy</param>
/// <param name="LastCycleTime">Last successful monitoring cycle</param>
/// <param name="CyclesPerHour">Average cycles per hour</param>
/// <param name="AverageDetectionsPerCycle">Average stoppages detected per cycle</param>
/// <param name="ErrorRate">Percentage of cycles with errors</param>
/// <param name="Status">Health status description</param>
public record MonitoringHealthStatus(
    bool IsHealthy,
    DateTime? LastCycleTime,
    decimal CyclesPerHour,
    decimal AverageDetectionsPerCycle,
    decimal ErrorRate,
    string Status
);
