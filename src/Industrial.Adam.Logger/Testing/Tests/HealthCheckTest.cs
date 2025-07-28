using System.Diagnostics;
using Industrial.Adam.Logger.Configuration;
using Industrial.Adam.Logger.ErrorHandling;
using Industrial.Adam.Logger.Interfaces;
using Industrial.Adam.Logger.Testing.Models;
using Industrial.Adam.Logger.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Industrial.Adam.Logger.Testing.Tests;

/// <summary>
/// Health check tests for ADAM logger system
/// </summary>
public sealed class HealthCheckTest
{
    private readonly ILogger<HealthCheckTest> _logger;
    private readonly IOptions<AdamLoggerConfig> _config;
    private readonly IIndustrialErrorService _errorService;

    /// <summary>
    /// Initialize health check test
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="config">Configuration options</param>
    /// <param name="errorService">Error service</param>
    public HealthCheckTest(
        ILogger<HealthCheckTest> logger,
        IOptions<AdamLoggerConfig> config,
        IIndustrialErrorService errorService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _errorService = errorService ?? throw new ArgumentNullException(nameof(errorService));
    }

    /// <summary>
    /// Test system health monitoring
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health monitoring test result</returns>
    public async Task<TestResult> TestSystemHealthMonitoringAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var testId = "HLTH-001";
        var testName = "System Health Monitoring Test";
        var metrics = new Dictionary<string, object>();
        var recommendations = new List<string>();

        try
        {
            // Test system health metrics collection
            var healthMetrics = await CollectSystemHealthMetricsAsync();

            metrics["SystemHealth"] = healthMetrics;
            metrics["CpuUsage"] = healthMetrics.CpuUsagePercent;
            metrics["MemoryUsage"] = healthMetrics.MemoryUsageMb;
            metrics["DiskUsage"] = healthMetrics.DiskUsagePercent;

            // Check health thresholds
            var issues = new List<string>();

            if (healthMetrics.CpuUsagePercent > 90)
            {
                issues.Add($"High CPU usage: {healthMetrics.CpuUsagePercent:F1}%");
                recommendations.Add("Monitor CPU-intensive processes");
            }

            if (healthMetrics.MemoryUsageMb > 1000)
            {
                issues.Add($"High memory usage: {healthMetrics.MemoryUsageMb:F1}MB");
                recommendations.Add("Monitor memory allocation patterns");
            }

            if (healthMetrics.DiskUsagePercent > 80)
            {
                issues.Add($"High disk usage: {healthMetrics.DiskUsagePercent:F1}%");
                recommendations.Add("Monitor disk space and implement cleanup");
            }

            if (issues.Count > 0)
            {
                recommendations.Add("Set up health monitoring alerts");

                var errorMessage = $"System health issues detected: {string.Join("; ", issues)}";

                return TestResult.Failure(
                    testId,
                    testName,
                    TestCategory.Health,
                    stopwatch.Elapsed,
                    errorMessage,
                    metrics: metrics,
                    recommendations: recommendations);
            }

            var successMessage = $"System health good - CPU: {healthMetrics.CpuUsagePercent:F1}%, Memory: {healthMetrics.MemoryUsageMb:F1}MB, Disk: {healthMetrics.DiskUsagePercent:F1}%";
            recommendations.Add("Continue monitoring system health");
            recommendations.Add("Implement automated health alerts");

            return TestResult.Success(
                testId,
                testName,
                TestCategory.Health,
                stopwatch.Elapsed,
                successMessage,
                metrics,
                recommendations);
        }
        catch (Exception ex)
        {
            var errorMessage = _errorService.CreateAndLogError(
                ex,
                "TEST-040",
                "System health monitoring test failed",
                new Dictionary<string, object>
                {
                    ["TestId"] = testId,
                    ["TestName"] = testName
                });

            return TestResult.Failure(
                testId,
                testName,
                TestCategory.Health,
                stopwatch.Elapsed,
                errorMessage.Summary,
                ex,
                metrics,
                errorMessage.TroubleshootingSteps,
                TestSeverity.Error);
        }
    }

    /// <summary>
    /// Collect system health metrics
    /// </summary>
    /// <returns>Health metrics</returns>
    private static async Task<SystemHealthMetrics> CollectSystemHealthMetricsAsync()
    {
        var process = Process.GetCurrentProcess();

        // Get CPU usage (approximate)
        var startTime = DateTime.UtcNow;
        var startCpuUsage = process.TotalProcessorTime;
        await Task.Delay(1000);
        var endTime = DateTime.UtcNow;
        var endCpuUsage = process.TotalProcessorTime;

        var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
        var totalMsPassed = (endTime - startTime).TotalMilliseconds;
        var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

        // Get memory usage
        var memoryUsage = process.WorkingSet64 / (1024.0 * 1024.0);

        // Get disk usage (approximate)
        var drives = DriveInfo.GetDrives().Where(d => d.IsReady);
        var totalDiskSpace = drives.Sum(d => d.TotalSize);
        var availableDiskSpace = drives.Sum(d => d.AvailableFreeSpace);
        var diskUsagePercent = ((double)(totalDiskSpace - availableDiskSpace) / totalDiskSpace) * 100;

        return new SystemHealthMetrics
        {
            CpuUsagePercent = cpuUsageTotal * 100,
            MemoryUsageMb = memoryUsage,
            DiskUsagePercent = diskUsagePercent,
            ProcessCount = Process.GetProcesses().Length,
            ThreadCount = process.Threads.Count,
            HandleCount = process.HandleCount,
            UptimeSeconds = (DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime()).TotalSeconds
        };
    }

    /// <summary>
    /// System health metrics
    /// </summary>
    private sealed class SystemHealthMetrics
    {
        public required double CpuUsagePercent { get; init; }
        public required double MemoryUsageMb { get; init; }
        public required double DiskUsagePercent { get; init; }
        public required int ProcessCount { get; init; }
        public required int ThreadCount { get; init; }
        public required int HandleCount { get; init; }
        public required double UptimeSeconds { get; init; }
    }
}
