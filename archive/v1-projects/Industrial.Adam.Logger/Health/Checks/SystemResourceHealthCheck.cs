using System.Diagnostics;
using System.Runtime;
using Industrial.Adam.Logger.ErrorHandling;
using Industrial.Adam.Logger.Health.Models;
using Industrial.Adam.Logger.Interfaces;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Logger.Health.Checks;

/// <summary>
/// Health check for system resources including CPU, memory, disk, and network
/// </summary>
public sealed class SystemResourceHealthCheck
{
    private readonly ILogger<SystemResourceHealthCheck> _logger;
    private readonly IIndustrialErrorService _errorService;
    private readonly PerformanceCounter? _cpuCounter;
    private readonly PerformanceCounter? _memoryCounter;

    /// <summary>
    /// Initialize system resource health check
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="errorService">Error service</param>
    public SystemResourceHealthCheck(
        ILogger<SystemResourceHealthCheck> logger,
        IIndustrialErrorService errorService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _errorService = errorService ?? throw new ArgumentNullException(nameof(errorService));

        // Initialize performance counters (Windows only)
        if (OperatingSystem.IsWindows())
        {
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize performance counters");
            }
        }
    }

    /// <summary>
    /// Check system resource health
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>System resource health status</returns>
    public async Task<ComponentHealth> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var metrics = new Dictionary<string, object>();
        var warnings = new List<string>();
        var recommendations = new List<string>();

        try
        {
            // Allow for async patterns even though most operations are synchronous
            await Task.CompletedTask;

            // Collect CPU metrics
            var cpuMetrics = await CollectCpuMetrics();
            foreach (var metric in cpuMetrics)
            {
                metrics[metric.Key] = metric.Value;
            }

            // Collect memory metrics
            var memoryMetrics = await CollectMemoryMetrics();
            foreach (var metric in memoryMetrics)
            {
                metrics[metric.Key] = metric.Value;
            }

            // Collect disk metrics
            var diskMetrics = await CollectDiskMetrics();
            foreach (var metric in diskMetrics)
            {
                metrics[metric.Key] = metric.Value;
            }

            // Collect process metrics
            var processMetrics = await CollectProcessMetrics();
            foreach (var metric in processMetrics)
            {
                metrics[metric.Key] = metric.Value;
            }

            // Collect .NET runtime metrics
            var runtimeMetrics = await CollectRuntimeMetrics();
            foreach (var metric in runtimeMetrics)
            {
                metrics[metric.Key] = metric.Value;
            }

            // Analyze resource usage and generate warnings
            AnalyzeResourceUsage(metrics, warnings, recommendations);

            // Calculate health score
            var healthScore = CalculateHealthScore(metrics, warnings);
            var status = DetermineHealthStatus(healthScore, warnings);

            var statusMessage = status switch
            {
                HealthStatus.Healthy => "System resources operating normally",
                HealthStatus.Degraded => $"System resources operational with {warnings.Count} warnings",
                HealthStatus.Unhealthy => "System resources under stress",
                HealthStatus.Critical => "Critical system resource issues detected",
                _ => "Unknown system resource state"
            };

            _logger.LogDebug(
                "System resource health check completed in {Duration}ms. Status: {Status}, Score: {HealthScore}",
                stopwatch.ElapsedMilliseconds, status, healthScore);

            return status switch
            {
                HealthStatus.Healthy => ComponentHealth.Healthy(
                    "SystemResources",
                    stopwatch.Elapsed,
                    statusMessage,
                    metrics),
                HealthStatus.Degraded => ComponentHealth.Degraded(
                    "SystemResources",
                    stopwatch.Elapsed,
                    healthScore,
                    statusMessage,
                    warnings,
                    recommendations,
                    metrics),
                _ => ComponentHealth.Unhealthy(
                    "SystemResources",
                    stopwatch.Elapsed,
                    statusMessage,
                    healthScore,
                    recommendations,
                    metrics)
            };
        }
        catch (Exception ex)
        {
            var errorMessage = _errorService.CreateAndLogError(
                ex,
                "HEALTH-003",
                "System resource health check failed",
                new Dictionary<string, object>
                {
                    ["CheckDuration"] = stopwatch.ElapsedMilliseconds,
                    ["ComponentName"] = "SystemResources"
                });

            return ComponentHealth.Critical(
                "SystemResources",
                stopwatch.Elapsed,
                errorMessage.Summary,
                errorMessage.TroubleshootingSteps,
                metrics);
        }
    }

    /// <summary>
    /// Collect CPU-related metrics
    /// </summary>
    /// <returns>CPU metrics</returns>
    private async Task<Dictionary<string, object>> CollectCpuMetrics()
    {
        var metrics = new Dictionary<string, object>();

        try
        {
            await Task.CompletedTask; // Ensure async pattern

            // Basic CPU information
            metrics["ProcessorCount"] = Environment.ProcessorCount;

            // Windows-specific CPU metrics
            if (OperatingSystem.IsWindows() && _cpuCounter != null)
            {
                try
                {
                    // First call to initialize
                    _cpuCounter.NextValue();
                    await Task.Delay(100); // Small delay for accurate reading
                    var cpuUsage = _cpuCounter.NextValue();
                    metrics["SystemCpuUsagePercent"] = Math.Round(cpuUsage, 2);
                }
                catch (Exception ex)
                {
                    metrics["CpuCounterError"] = ex.Message;
                }
            }

            // Process-specific CPU metrics
            var process = Process.GetCurrentProcess();
            metrics["ProcessTotalProcessorTimeMs"] = process.TotalProcessorTime.TotalMilliseconds;
            metrics["ProcessUserProcessorTimeMs"] = process.UserProcessorTime.TotalMilliseconds;
            metrics["ProcessPrivilegedProcessorTimeMs"] = process.PrivilegedProcessorTime.TotalMilliseconds;
        }
        catch (Exception ex)
        {
            metrics["CpuMetricsError"] = ex.Message;
        }

        return metrics;
    }

    /// <summary>
    /// Collect memory-related metrics
    /// </summary>
    /// <returns>Memory metrics</returns>
    private async Task<Dictionary<string, object>> CollectMemoryMetrics()
    {
        var metrics = new Dictionary<string, object>();

        try
        {
            await Task.CompletedTask; // Ensure async pattern

            // System memory information
            if (OperatingSystem.IsWindows() && _memoryCounter != null)
            {
                try
                {
                    var availableMemoryMB = _memoryCounter.NextValue();
                    metrics["SystemAvailableMemoryMB"] = Math.Round(availableMemoryMB, 2);
                }
                catch (Exception ex)
                {
                    metrics["MemoryCounterError"] = ex.Message;
                }
            }

            // Process memory metrics
            var process = Process.GetCurrentProcess();
            metrics["ProcessWorkingSetMB"] = Math.Round(process.WorkingSet64 / (1024.0 * 1024.0), 2);
            metrics["ProcessPrivateMemoryMB"] = Math.Round(process.PrivateMemorySize64 / (1024.0 * 1024.0), 2);
            metrics["ProcessVirtualMemoryMB"] = Math.Round(process.VirtualMemorySize64 / (1024.0 * 1024.0), 2);
            metrics["ProcessPagedMemoryMB"] = Math.Round(process.PagedMemorySize64 / (1024.0 * 1024.0), 2);
            metrics["ProcessPagedSystemMemoryMB"] = Math.Round(process.PagedSystemMemorySize64 / (1024.0 * 1024.0), 2);
            metrics["ProcessNonPagedSystemMemoryMB"] = Math.Round(process.NonpagedSystemMemorySize64 / (1024.0 * 1024.0), 2);

            // .NET GC memory metrics
            metrics["GcTotalMemoryMB"] = Math.Round(GC.GetTotalMemory(false) / (1024.0 * 1024.0), 2);
            metrics["GcTotalMemoryAfterCollectionMB"] = Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2);
        }
        catch (Exception ex)
        {
            metrics["MemoryMetricsError"] = ex.Message;
        }

        return metrics;
    }

    /// <summary>
    /// Collect disk-related metrics
    /// </summary>
    /// <returns>Disk metrics</returns>
    private async Task<Dictionary<string, object>> CollectDiskMetrics()
    {
        var metrics = new Dictionary<string, object>();

        try
        {
            await Task.CompletedTask; // Ensure async pattern

            // Get disk information for current drive
            var currentDirectory = Environment.CurrentDirectory;
            var drive = new DriveInfo(Path.GetPathRoot(currentDirectory) ?? "C:\\");

            if (drive.IsReady)
            {
                var totalSpaceGB = Math.Round(drive.TotalSize / (1024.0 * 1024.0 * 1024.0), 2);
                var freeSpaceGB = Math.Round(drive.TotalFreeSpace / (1024.0 * 1024.0 * 1024.0), 2);
                var usedSpaceGB = totalSpaceGB - freeSpaceGB;
                var usedPercent = Math.Round((usedSpaceGB / totalSpaceGB) * 100, 2);

                metrics["DiskTotalSpaceGB"] = totalSpaceGB;
                metrics["DiskFreeSpaceGB"] = freeSpaceGB;
                metrics["DiskUsedSpaceGB"] = usedSpaceGB;
                metrics["DiskUsedPercent"] = usedPercent;
                metrics["DiskDriveFormat"] = drive.DriveFormat;
                metrics["DiskDriveType"] = drive.DriveType.ToString();
            }

            // Get information for all available drives
            var allDrives = DriveInfo.GetDrives();
            metrics["TotalDriveCount"] = allDrives.Length;
            metrics["ReadyDriveCount"] = allDrives.Count(d => d.IsReady);
        }
        catch (Exception ex)
        {
            metrics["DiskMetricsError"] = ex.Message;
        }

        return metrics;
    }

    /// <summary>
    /// Collect process-related metrics
    /// </summary>
    /// <returns>Process metrics</returns>
    private async Task<Dictionary<string, object>> CollectProcessMetrics()
    {
        var metrics = new Dictionary<string, object>();

        try
        {
            await Task.CompletedTask; // Ensure async pattern

            var process = Process.GetCurrentProcess();

            metrics["ProcessId"] = process.Id;
            metrics["ProcessName"] = process.ProcessName;
            metrics["ProcessThreadCount"] = process.Threads.Count;
            metrics["ProcessHandleCount"] = process.HandleCount;
            metrics["ProcessStartTime"] = process.StartTime;
            metrics["ProcessUptimeSeconds"] = (DateTime.Now - process.StartTime).TotalSeconds;

            // Thread priority information
            metrics["ProcessBasePriority"] = process.BasePriority;
            metrics["ProcessPriorityClass"] = process.PriorityClass.ToString();

            // Session information
            metrics["ProcessSessionId"] = process.SessionId;
            metrics["ProcessMachineName"] = process.MachineName;
        }
        catch (Exception ex)
        {
            metrics["ProcessMetricsError"] = ex.Message;
        }

        return metrics;
    }

    /// <summary>
    /// Collect .NET runtime metrics
    /// </summary>
    /// <returns>Runtime metrics</returns>
    private async Task<Dictionary<string, object>> CollectRuntimeMetrics()
    {
        var metrics = new Dictionary<string, object>();

        try
        {
            await Task.CompletedTask; // Ensure async pattern

            // GC metrics
            metrics["GcGen0Collections"] = GC.CollectionCount(0);
            metrics["GcGen1Collections"] = GC.CollectionCount(1);
            metrics["GcGen2Collections"] = GC.CollectionCount(2);
            metrics["GcIsServerGC"] = GCSettings.IsServerGC;
            metrics["GcLatencyMode"] = GCSettings.LatencyMode.ToString();

            // Runtime environment
            metrics["RuntimeVersion"] = Environment.Version.ToString();
            metrics["FrameworkDescription"] = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
            metrics["OSDescription"] = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
            metrics["OSArchitecture"] = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString();
            metrics["ProcessArchitecture"] = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString();

            // Process information
            metrics["Is64BitProcess"] = Environment.Is64BitProcess;
            metrics["Is64BitOperatingSystem"] = Environment.Is64BitOperatingSystem;
            metrics["SystemPageSize"] = Environment.SystemPageSize;
            metrics["TickCount"] = Environment.TickCount64;
        }
        catch (Exception ex)
        {
            metrics["RuntimeMetricsError"] = ex.Message;
        }

        return metrics;
    }

    /// <summary>
    /// Analyze resource usage and generate warnings/recommendations
    /// </summary>
    /// <param name="metrics">Current metrics</param>
    /// <param name="warnings">Warnings list</param>
    /// <param name="recommendations">Recommendations list</param>
    private static void AnalyzeResourceUsage(
        Dictionary<string, object> metrics,
        List<string> warnings,
        List<string> recommendations)
    {
        // CPU analysis
        if (metrics.TryGetValue("SystemCpuUsagePercent", out var cpuObj) && cpuObj is double cpuUsage)
        {
            if (cpuUsage > 90)
            {
                warnings.Add($"Very high CPU usage: {cpuUsage:F1}%");
                recommendations.Add("Investigate high CPU usage processes and optimize performance");
            }
            else if (cpuUsage > 75)
            {
                warnings.Add($"High CPU usage: {cpuUsage:F1}%");
                recommendations.Add("Monitor CPU usage trends and consider optimization");
            }
        }

        // Memory analysis
        if (metrics.TryGetValue("ProcessWorkingSetMB", out var workingSetObj) && workingSetObj is double workingSetMB)
        {
            if (workingSetMB > 2048) // 2GB
            {
                warnings.Add($"Very high memory usage: {workingSetMB:F1} MB");
                recommendations.Add("Investigate memory usage patterns and consider optimization");
            }
            else if (workingSetMB > 1024) // 1GB
            {
                warnings.Add($"High memory usage: {workingSetMB:F1} MB");
                recommendations.Add("Monitor memory usage trends");
            }
        }

        // Available system memory analysis
        if (metrics.TryGetValue("SystemAvailableMemoryMB", out var availableMemObj) && availableMemObj is double availableMemMB)
        {
            if (availableMemMB < 512) // Less than 512MB available
            {
                warnings.Add($"Low available system memory: {availableMemMB:F1} MB");
                recommendations.Add("Free up system memory or add more RAM");
            }
            else if (availableMemMB < 1024) // Less than 1GB available
            {
                warnings.Add($"Moderate available system memory: {availableMemMB:F1} MB");
                recommendations.Add("Monitor system memory usage");
            }
        }

        // Disk space analysis
        if (metrics.TryGetValue("DiskUsedPercent", out var diskUsedObj) && diskUsedObj is double diskUsedPercent)
        {
            if (diskUsedPercent > 95)
            {
                warnings.Add($"Very low disk space: {diskUsedPercent:F1}% used");
                recommendations.Add("Free up disk space immediately");
            }
            else if (diskUsedPercent > 85)
            {
                warnings.Add($"Low disk space: {diskUsedPercent:F1}% used");
                recommendations.Add("Plan for disk space cleanup or expansion");
            }
        }

        // Thread count analysis
        if (metrics.TryGetValue("ProcessThreadCount", out var threadCountObj) && threadCountObj is int threadCount)
        {
            if (threadCount > 500)
            {
                warnings.Add($"Very high thread count: {threadCount}");
                recommendations.Add("Review thread usage and consider thread pool optimization");
            }
            else if (threadCount > 200)
            {
                warnings.Add($"High thread count: {threadCount}");
                recommendations.Add("Monitor thread usage patterns");
            }
        }

        // Handle count analysis
        if (metrics.TryGetValue("ProcessHandleCount", out var handleCountObj) && handleCountObj is int handleCount)
        {
            if (handleCount > 20000)
            {
                warnings.Add($"Very high handle count: {handleCount}");
                recommendations.Add("Review resource disposal and handle management");
            }
            else if (handleCount > 10000)
            {
                warnings.Add($"High handle count: {handleCount}");
                recommendations.Add("Monitor handle usage patterns");
            }
        }

        // GC analysis
        if (metrics.TryGetValue("GcGen2Collections", out var gen2Obj) && gen2Obj is int gen2Collections &&
            metrics.TryGetValue("ProcessUptimeSeconds", out var uptimeObj) && uptimeObj is double uptimeSeconds)
        {
            var gen2Rate = gen2Collections / (uptimeSeconds / 3600.0); // Collections per hour
            if (gen2Rate > 20)
            {
                warnings.Add($"High Gen2 GC rate: {gen2Rate:F1} collections/hour");
                recommendations.Add("Review memory allocation patterns and object lifetime management");
            }
        }
    }

    /// <summary>
    /// Calculate health score based on metrics and warnings
    /// </summary>
    /// <param name="metrics">Current metrics</param>
    /// <param name="warnings">Current warnings</param>
    /// <returns>Health score (0-100)</returns>
    private static int CalculateHealthScore(Dictionary<string, object> metrics, List<string> warnings)
    {
        var baseScore = 100;

        // Penalize for warnings
        baseScore -= warnings.Count * 10;

        // CPU penalties
        if (metrics.TryGetValue("SystemCpuUsagePercent", out var cpuObj) && cpuObj is double cpuUsage)
        {
            if (cpuUsage > 90)
                baseScore -= 30;
            else if (cpuUsage > 75)
                baseScore -= 15;
            else if (cpuUsage > 60)
                baseScore -= 5;
        }

        // Memory penalties
        if (metrics.TryGetValue("ProcessWorkingSetMB", out var memoryObj) && memoryObj is double memoryMB)
        {
            if (memoryMB > 2048)
                baseScore -= 25;
            else if (memoryMB > 1024)
                baseScore -= 10;
        }

        // Disk space penalties
        if (metrics.TryGetValue("DiskUsedPercent", out var diskObj) && diskObj is double diskPercent)
        {
            if (diskPercent > 95)
                baseScore -= 30;
            else if (diskPercent > 85)
                baseScore -= 15;
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
        if (healthScore >= 85 && warnings.Count == 0)
            return HealthStatus.Healthy;

        if (healthScore >= 65 && warnings.Count <= 3)
            return HealthStatus.Degraded;

        if (healthScore >= 30)
            return HealthStatus.Unhealthy;

        return HealthStatus.Critical;
    }

    /// <summary>
    /// Dispose of performance counters
    /// </summary>
    public void Dispose()
    {
        _cpuCounter?.Dispose();
        _memoryCounter?.Dispose();
    }
}
