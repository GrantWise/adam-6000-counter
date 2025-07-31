using System.Diagnostics;
using System.Reflection;
using System.Runtime;
using Industrial.Adam.Logger.Configuration;
using Industrial.Adam.Logger.ErrorHandling;
using Industrial.Adam.Logger.Health.Models;
using Industrial.Adam.Logger.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Industrial.Adam.Logger.Health.Checks;

/// <summary>
/// Health check for core application components and system resources
/// </summary>
public sealed class ApplicationHealthCheck
{
    private readonly ILogger<ApplicationHealthCheck> _logger;
    private readonly IOptions<AdamLoggerConfig> _config;
    private readonly IIndustrialErrorService _errorService;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly DateTime _startTime;

    /// <summary>
    /// Initialize application health check
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="config">Configuration options</param>
    /// <param name="errorService">Error service</param>
    /// <param name="applicationLifetime">Application lifetime</param>
    public ApplicationHealthCheck(
        ILogger<ApplicationHealthCheck> logger,
        IOptions<AdamLoggerConfig> config,
        IIndustrialErrorService errorService,
        IHostApplicationLifetime applicationLifetime)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _errorService = errorService ?? throw new ArgumentNullException(nameof(errorService));
        _applicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
        _startTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Check application health and system resources
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Application health status</returns>
    public async Task<ComponentHealth> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var metrics = new Dictionary<string, object>();
        var warnings = new List<string>();
        var recommendations = new List<string>();

        try
        {
            await Task.CompletedTask; // Ensure this is truly async

            // Check application lifecycle state
            var isShuttingDown = _applicationLifetime.ApplicationStopping.IsCancellationRequested;
            if (isShuttingDown)
            {
                return ComponentHealth.Degraded(
                    "Application",
                    stopwatch.Elapsed,
                    75,
                    "Application is shutting down",
                    new[] { "Application shutdown in progress" },
                    new[] { "Allow graceful shutdown to complete" },
                    metrics,
                    DateTime.UtcNow - _startTime);
            }

            // Collect system metrics
            var process = Process.GetCurrentProcess();
            var uptime = DateTime.UtcNow - _startTime;

            // Memory metrics
            var workingSet = process.WorkingSet64;
            var privateMemory = process.PrivateMemorySize64;
            var gcMemory = GC.GetTotalMemory(false);
            var gen0Collections = GC.CollectionCount(0);
            var gen1Collections = GC.CollectionCount(1);
            var gen2Collections = GC.CollectionCount(2);

            // CPU metrics
            var totalProcessorTime = process.TotalProcessorTime;
            var threadCount = process.Threads.Count;
            var handleCount = process.HandleCount;

            // Assembly and version information
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version?.ToString() ?? "Unknown";
            var location = assembly.Location;

            // Performance counters
            var performanceCounters = GetPerformanceMetrics();

            // Populate metrics
            metrics["Uptime"] = uptime.TotalSeconds;
            metrics["WorkingSetMB"] = workingSet / (1024.0 * 1024.0);
            metrics["PrivateMemoryMB"] = privateMemory / (1024.0 * 1024.0);
            metrics["GcMemoryMB"] = gcMemory / (1024.0 * 1024.0);
            metrics["Gen0Collections"] = gen0Collections;
            metrics["Gen1Collections"] = gen1Collections;
            metrics["Gen2Collections"] = gen2Collections;
            metrics["TotalProcessorTimeSeconds"] = totalProcessorTime.TotalSeconds;
            metrics["ThreadCount"] = threadCount;
            metrics["HandleCount"] = handleCount;
            metrics["ApplicationVersion"] = version;
            metrics["AssemblyLocation"] = location;
            metrics["ProcessId"] = process.Id;
            metrics["StartTime"] = _startTime;

            // Add performance metrics
            foreach (var metric in performanceCounters)
            {
                metrics[metric.Key] = metric.Value;
            }

            // Analyze metrics for warnings
            AnalyzeResourceUsage(metrics, warnings, recommendations);

            // Check configuration health
            CheckConfigurationHealth(metrics, warnings, recommendations);

            // Determine overall health status
            var healthScore = CalculateHealthScore(metrics, warnings);
            var status = DetermineHealthStatus(healthScore, warnings);

            var statusMessage = status switch
            {
                HealthStatus.Healthy => $"Application running normally (uptime: {uptime:dd\\.hh\\:mm\\:ss})",
                HealthStatus.Degraded => $"Application operational with {warnings.Count} warnings",
                HealthStatus.Unhealthy => "Application has performance or resource issues",
                HealthStatus.Critical => "Application in critical state",
                _ => "Unknown application state"
            };

            _logger.LogDebug(
                "Application health check completed in {Duration}ms. Status: {Status}, Score: {HealthScore}",
                stopwatch.ElapsedMilliseconds, status, healthScore);

            return status switch
            {
                HealthStatus.Healthy => ComponentHealth.Healthy(
                    "Application",
                    stopwatch.Elapsed,
                    statusMessage,
                    metrics,
                    uptime),
                HealthStatus.Degraded => ComponentHealth.Degraded(
                    "Application",
                    stopwatch.Elapsed,
                    healthScore,
                    statusMessage,
                    warnings,
                    recommendations,
                    metrics,
                    uptime),
                _ => ComponentHealth.Unhealthy(
                    "Application",
                    stopwatch.Elapsed,
                    statusMessage,
                    healthScore,
                    recommendations,
                    metrics,
                    uptime)
            };
        }
        catch (Exception ex)
        {
            var errorMessage = _errorService.CreateAndLogError(
                ex,
                "HEALTH-001",
                "Application health check failed",
                new Dictionary<string, object>
                {
                    ["CheckDuration"] = stopwatch.ElapsedMilliseconds,
                    ["ComponentName"] = "Application"
                });

            return ComponentHealth.Critical(
                "Application",
                stopwatch.Elapsed,
                errorMessage.Summary,
                errorMessage.TroubleshootingSteps,
                metrics);
        }
    }

    /// <summary>
    /// Get performance metrics from the system
    /// </summary>
    /// <returns>Performance metrics dictionary</returns>
    private static Dictionary<string, object> GetPerformanceMetrics()
    {
        var metrics = new Dictionary<string, object>();

        try
        {
            // .NET runtime metrics
            metrics["IsServerGC"] = GCSettings.IsServerGC;
            metrics["ProcessorCount"] = Environment.ProcessorCount;
            metrics["OSVersion"] = Environment.OSVersion.ToString();
            metrics["MachineName"] = Environment.MachineName;
            metrics["RuntimeVersion"] = Environment.Version.ToString();
            metrics["Is64BitProcess"] = Environment.Is64BitProcess;
            metrics["Is64BitOperatingSystem"] = Environment.Is64BitOperatingSystem;

            // Time zone and culture
            metrics["TimeZone"] = TimeZoneInfo.Local.DisplayName;
            metrics["CurrentCulture"] = System.Globalization.CultureInfo.CurrentCulture.Name;

            // System memory (if available)
            if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
            {
                try
                {
                    var totalMemory = GC.GetTotalMemory(false);
                    metrics["TotalAllocatedMemory"] = totalMemory;
                }
                catch
                {
                    // Ignore if not available
                }
            }
        }
        catch (Exception ex)
        {
            metrics["PerformanceMetricsError"] = ex.Message;
        }

        return metrics;
    }

    /// <summary>
    /// Analyze resource usage and generate warnings/recommendations
    /// </summary>
    /// <param name="metrics">Current metrics</param>
    /// <param name="warnings">Warnings list to populate</param>
    /// <param name="recommendations">Recommendations list to populate</param>
    private static void AnalyzeResourceUsage(
        Dictionary<string, object> metrics,
        List<string> warnings,
        List<string> recommendations)
    {
        // Memory analysis
        if (metrics.TryGetValue("WorkingSetMB", out var workingSetObj) && workingSetObj is double workingSetMB)
        {
            if (workingSetMB > 1000) // 1GB
            {
                warnings.Add($"High memory usage: {workingSetMB:F1} MB working set");
                recommendations.Add("Monitor memory usage and consider optimization");
            }
        }

        // Thread count analysis
        if (metrics.TryGetValue("ThreadCount", out var threadCountObj) && threadCountObj is int threadCount)
        {
            if (threadCount > 100)
            {
                warnings.Add($"High thread count: {threadCount} threads");
                recommendations.Add("Review thread usage and consider thread pool optimization");
            }
        }

        // Handle count analysis (Windows-specific)
        if (metrics.TryGetValue("HandleCount", out var handleCountObj) && handleCountObj is int handleCount)
        {
            if (handleCount > 10000)
            {
                warnings.Add($"High handle count: {handleCount} handles");
                recommendations.Add("Review resource disposal and handle management");
            }
        }

        // GC analysis
        if (metrics.TryGetValue("Gen2Collections", out var gen2Obj) && gen2Obj is int gen2Collections)
        {
            var uptime = metrics.TryGetValue("Uptime", out var uptimeObj) && uptimeObj is double uptimeSeconds
                ? uptimeSeconds
                : 1.0;

            var gen2Rate = gen2Collections / (uptime / 3600.0); // Collections per hour
            if (gen2Rate > 10) // More than 10 Gen2 collections per hour
            {
                warnings.Add($"High Gen2 GC rate: {gen2Rate:F1} collections/hour");
                recommendations.Add("Review memory allocation patterns and object lifetime management");
            }
        }
    }

    /// <summary>
    /// Check configuration health
    /// </summary>
    /// <param name="metrics">Metrics dictionary</param>
    /// <param name="warnings">Warnings list</param>
    /// <param name="recommendations">Recommendations list</param>
    private void CheckConfigurationHealth(
        Dictionary<string, object> metrics,
        List<string> warnings,
        List<string> recommendations)
    {
        var config = _config.Value;

        // Check critical configuration values
        if (config.PollIntervalMs < 100)
        {
            warnings.Add($"Very low poll interval: {config.PollIntervalMs}ms may cause performance issues");
            recommendations.Add("Consider increasing poll interval to reduce CPU usage");
        }

        if (config.MaxConcurrentDevices > Environment.ProcessorCount * 4)
        {
            warnings.Add($"High concurrent device count: {config.MaxConcurrentDevices} may exceed system capacity");
            recommendations.Add("Consider reducing concurrent device limit based on system resources");
        }

        if (config.Devices.Count == 0)
        {
            warnings.Add("No devices configured for monitoring");
            recommendations.Add("Add device configurations to enable data collection");
        }

        // Add configuration metrics
        metrics["ConfiguredDevices"] = config.Devices.Count;
        metrics["PollIntervalMs"] = config.PollIntervalMs;
        metrics["MaxConcurrentDevices"] = config.MaxConcurrentDevices;
        metrics["HealthCheckIntervalMs"] = config.HealthCheckIntervalMs;
        metrics["DemoMode"] = config.DemoMode;
    }

    /// <summary>
    /// Calculate overall health score based on metrics and warnings
    /// </summary>
    /// <param name="metrics">Current metrics</param>
    /// <param name="warnings">Current warnings</param>
    /// <returns>Health score (0-100)</returns>
    private static int CalculateHealthScore(Dictionary<string, object> metrics, List<string> warnings)
    {
        var baseScore = 100;

        // Penalize for warnings
        var warningPenalty = warnings.Count * 10;
        baseScore -= warningPenalty;

        // Memory-based penalties
        if (metrics.TryGetValue("WorkingSetMB", out var workingSetObj) && workingSetObj is double workingSetMB)
        {
            if (workingSetMB > 2000)
                baseScore -= 20; // 2GB
            else if (workingSetMB > 1000)
                baseScore -= 10; // 1GB
        }

        // Thread count penalties
        if (metrics.TryGetValue("ThreadCount", out var threadCountObj) && threadCountObj is int threadCount)
        {
            if (threadCount > 200)
                baseScore -= 20;
            else if (threadCount > 100)
                baseScore -= 10;
        }

        return Math.Max(0, Math.Min(100, baseScore));
    }

    /// <summary>
    /// Determine health status based on score and warnings
    /// </summary>
    /// <param name="healthScore">Current health score</param>
    /// <param name="warnings">Current warnings</param>
    /// <returns>Health status</returns>
    private static HealthStatus DetermineHealthStatus(int healthScore, List<string> warnings)
    {
        if (healthScore >= 90 && warnings.Count == 0)
            return HealthStatus.Healthy;

        if (healthScore >= 70 && warnings.Count <= 2)
            return HealthStatus.Degraded;

        if (healthScore >= 30)
            return HealthStatus.Unhealthy;

        return HealthStatus.Critical;
    }
}
