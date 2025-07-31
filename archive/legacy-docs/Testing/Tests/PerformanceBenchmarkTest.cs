using System.Diagnostics;
using System.Runtime;
using Industrial.Adam.Logger.Configuration;
using Industrial.Adam.Logger.ErrorHandling;
using Industrial.Adam.Logger.Interfaces;
using Industrial.Adam.Logger.Testing.Models;
using Industrial.Adam.Logger.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Industrial.Adam.Logger.Testing.Tests;

/// <summary>
/// Performance benchmarking tests for ADAM logger system
/// </summary>
public sealed class PerformanceBenchmarkTest
{
    private readonly ILogger<PerformanceBenchmarkTest> _logger;
    private readonly IOptions<AdamLoggerConfig> _config;
    private readonly IIndustrialErrorService _errorService;

    /// <summary>
    /// Initialize performance benchmark test
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="config">Configuration options</param>
    /// <param name="errorService">Error service</param>
    public PerformanceBenchmarkTest(
        ILogger<PerformanceBenchmarkTest> logger,
        IOptions<AdamLoggerConfig> config,
        IIndustrialErrorService errorService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _errorService = errorService ?? throw new ArgumentNullException(nameof(errorService));
    }

    /// <summary>
    /// Test system resource usage under load
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Resource usage test result</returns>
    public async Task<TestResult> TestResourceUsageAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var testId = "PERF-001";
        var testName = "System Resource Usage Test";
        var metrics = new Dictionary<string, object>();
        var recommendations = new List<string>();

        try
        {
            // Collect baseline metrics
            var baselineMetrics = await CollectSystemMetricsAsync();

            // Simulate load
            var loadTasks = new List<Task>();
            const int LoadDurationMs = 5000; // 5 seconds of load

            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                loadTasks.Add(Task.Run(async () =>
                {
                    var loadStopwatch = Stopwatch.StartNew();
                    while (loadStopwatch.ElapsedMilliseconds < LoadDurationMs && !cancellationToken.IsCancellationRequested)
                    {
                        // Simulate data processing load
                        var random = new Random();
                        var data = new byte[1024];
                        random.NextBytes(data);

                        // Simulate some processing
                        Array.Sort(data);
                        await Task.Delay(1, cancellationToken);
                    }
                }, cancellationToken));
            }

            // Wait for load completion
            await Task.WhenAll(loadTasks);

            // Collect metrics under load
            var loadMetrics = await CollectSystemMetricsAsync();

            // Allow system to settle
            await Task.Delay(2000, cancellationToken);

            // Collect post-load metrics
            var postLoadMetrics = await CollectSystemMetricsAsync();

            // Analyze performance
            var cpuIncrease = loadMetrics.CpuUsagePercent - baselineMetrics.CpuUsagePercent;
            var memoryIncrease = loadMetrics.MemoryUsageMb - baselineMetrics.MemoryUsageMb;
            var gcIncrease = loadMetrics.GcCollectionCount - baselineMetrics.GcCollectionCount;

            metrics["BaselineMetrics"] = baselineMetrics.ToDictionary();
            metrics["LoadMetrics"] = loadMetrics.ToDictionary();
            metrics["PostLoadMetrics"] = postLoadMetrics.ToDictionary();
            metrics["CpuIncrease"] = cpuIncrease;
            metrics["MemoryIncrease"] = memoryIncrease;
            metrics["GcIncrease"] = gcIncrease;
            metrics["LoadDurationMs"] = LoadDurationMs;
            metrics["ProcessorCount"] = Environment.ProcessorCount;

            // Determine test result
            var issues = new List<string>();

            if (cpuIncrease > 80)
            {
                issues.Add($"Excessive CPU usage increase: {cpuIncrease:F1}%");
                recommendations.Add("Optimize CPU-intensive operations");
            }

            if (memoryIncrease > 100)
            {
                issues.Add($"High memory usage increase: {memoryIncrease:F1}MB");
                recommendations.Add("Review memory allocation patterns");
            }

            if (gcIncrease > 10)
            {
                issues.Add($"Frequent garbage collection: {gcIncrease} collections");
                recommendations.Add("Optimize object allocation and disposal");
            }

            if (issues.Count > 0)
            {
                recommendations.Add("Monitor system resources in production");
                recommendations.Add("Consider implementing resource limits");

                var errorMessage = $"Performance issues detected: {string.Join("; ", issues)}";

                return TestResult.Failure(
                    testId,
                    testName,
                    TestCategory.Performance,
                    stopwatch.Elapsed,
                    errorMessage,
                    metrics: metrics,
                    recommendations: recommendations);
            }

            var successMessage = $"Resource usage acceptable - CPU: +{cpuIncrease:F1}%, Memory: +{memoryIncrease:F1}MB, GC: +{gcIncrease}";
            recommendations.Add("Continue monitoring resource usage trends");
            recommendations.Add("Set up resource usage alerts");

            return TestResult.Success(
                testId,
                testName,
                TestCategory.Performance,
                stopwatch.Elapsed,
                successMessage,
                metrics,
                recommendations);
        }
        catch (Exception ex)
        {
            var errorMessage = _errorService.CreateAndLogError(
                ex,
                "TEST-030",
                "Resource usage test failed",
                new Dictionary<string, object>
                {
                    ["TestId"] = testId,
                    ["TestName"] = testName
                });

            return TestResult.Failure(
                testId,
                testName,
                TestCategory.Performance,
                stopwatch.Elapsed,
                errorMessage.Summary,
                ex,
                metrics,
                errorMessage.TroubleshootingSteps,
                TestSeverity.Error);
        }
    }

    /// <summary>
    /// Test data throughput performance
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Throughput test result</returns>
    public async Task<TestResult> TestDataThroughputAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var testId = "PERF-002";
        var testName = "Data Throughput Performance Test";
        var metrics = new Dictionary<string, object>();
        var recommendations = new List<string>();

        try
        {
            const int TestDurationMs = 10000; // 10 seconds
            const int TargetDataPointsPerSecond = 100;

            var dataPointsProcessed = 0;
            var processingTimes = new List<long>();
            var throughputStopwatch = Stopwatch.StartNew();

            // Simulate data processing
            while (throughputStopwatch.ElapsedMilliseconds < TestDurationMs && !cancellationToken.IsCancellationRequested)
            {
                var dataStopwatch = Stopwatch.StartNew();

                // Simulate data point processing
                await ProcessMockDataPointAsync(cancellationToken);

                dataStopwatch.Stop();
                processingTimes.Add(dataStopwatch.ElapsedMilliseconds);
                dataPointsProcessed++;

                // Maintain target rate
                var targetInterval = 1000 / TargetDataPointsPerSecond;
                var remainingTime = targetInterval - dataStopwatch.ElapsedMilliseconds;
                if (remainingTime > 0)
                {
                    await Task.Delay((int)remainingTime, cancellationToken);
                }
            }

            throughputStopwatch.Stop();

            // Calculate performance metrics
            var actualDurationSeconds = throughputStopwatch.ElapsedMilliseconds / 1000.0;
            var actualThroughput = dataPointsProcessed / actualDurationSeconds;
            var averageProcessingTime = processingTimes.Average();
            var maxProcessingTime = processingTimes.Max();
            var minProcessingTime = processingTimes.Min();
            var throughputEfficiency = (actualThroughput / TargetDataPointsPerSecond) * 100;

            metrics["DataPointsProcessed"] = dataPointsProcessed;
            metrics["ActualDurationSeconds"] = actualDurationSeconds;
            metrics["TargetThroughput"] = TargetDataPointsPerSecond;
            metrics["ActualThroughput"] = actualThroughput;
            metrics["ThroughputEfficiency"] = throughputEfficiency;
            metrics["AverageProcessingTimeMs"] = averageProcessingTime;
            metrics["MaxProcessingTimeMs"] = maxProcessingTime;
            metrics["MinProcessingTimeMs"] = minProcessingTime;
            metrics["ProcessingTimeStandardDeviation"] = CalculateStandardDeviation(processingTimes);

            // Determine test result
            var issues = new List<string>();

            if (throughputEfficiency < 90)
            {
                issues.Add($"Low throughput efficiency: {throughputEfficiency:F1}%");
                recommendations.Add("Optimize data processing algorithms");
            }

            if (averageProcessingTime > 5)
            {
                issues.Add($"High average processing time: {averageProcessingTime:F1}ms");
                recommendations.Add("Review data processing pipeline performance");
            }

            if (maxProcessingTime > 100)
            {
                issues.Add($"High maximum processing time: {maxProcessingTime}ms");
                recommendations.Add("Investigate processing time spikes");
            }

            if (issues.Count > 0)
            {
                recommendations.Add("Monitor data processing performance");
                recommendations.Add("Consider implementing processing queues");

                var errorMessage = $"Throughput performance issues: {string.Join("; ", issues)}";

                return TestResult.Failure(
                    testId,
                    testName,
                    TestCategory.Performance,
                    stopwatch.Elapsed,
                    errorMessage,
                    metrics: metrics,
                    recommendations: recommendations);
            }

            var successMessage = $"Throughput performance acceptable - {actualThroughput:F1} points/sec ({throughputEfficiency:F1}% efficiency)";
            recommendations.Add("Monitor throughput trends over time");
            recommendations.Add("Implement throughput alerting");

            return TestResult.Success(
                testId,
                testName,
                TestCategory.Performance,
                stopwatch.Elapsed,
                successMessage,
                metrics,
                recommendations);
        }
        catch (Exception ex)
        {
            var errorMessage = _errorService.CreateAndLogError(
                ex,
                "TEST-031",
                "Data throughput test failed",
                new Dictionary<string, object>
                {
                    ["TestId"] = testId,
                    ["TestName"] = testName
                });

            return TestResult.Failure(
                testId,
                testName,
                TestCategory.Performance,
                stopwatch.Elapsed,
                errorMessage.Summary,
                ex,
                metrics,
                errorMessage.TroubleshootingSteps,
                TestSeverity.Error);
        }
    }

    /// <summary>
    /// Test memory usage patterns
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Memory usage test result</returns>
    public async Task<TestResult> TestMemoryUsageAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var testId = "PERF-003";
        var testName = "Memory Usage Pattern Test";
        var metrics = new Dictionary<string, object>();
        var recommendations = new List<string>();

        try
        {
            var initialMemory = GC.GetTotalMemory(true);
            var memorySnapshots = new List<MemorySnapshot>();

            // Take initial snapshot
            memorySnapshots.Add(new MemorySnapshot
            {
                TimestampMs = stopwatch.ElapsedMilliseconds,
                MemoryUsage = GC.GetTotalMemory(false),
                Gen0Collections = GC.CollectionCount(0),
                Gen1Collections = GC.CollectionCount(1),
                Gen2Collections = GC.CollectionCount(2)
            });

            // Simulate memory allocation patterns
            var allocations = new List<byte[]>();
            const int AllocationSize = 1024 * 1024; // 1MB
            const int AllocationCount = 50;

            for (int i = 0; i < AllocationCount && !cancellationToken.IsCancellationRequested; i++)
            {
                // Allocate memory
                var data = new byte[AllocationSize];
                allocations.Add(data);

                // Take snapshot every 10 allocations
                if (i % 10 == 0)
                {
                    memorySnapshots.Add(new MemorySnapshot
                    {
                        TimestampMs = stopwatch.ElapsedMilliseconds,
                        MemoryUsage = GC.GetTotalMemory(false),
                        Gen0Collections = GC.CollectionCount(0),
                        Gen1Collections = GC.CollectionCount(1),
                        Gen2Collections = GC.CollectionCount(2)
                    });
                }

                await Task.Delay(100, cancellationToken);
            }

            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Take final snapshot
            memorySnapshots.Add(new MemorySnapshot
            {
                TimestampMs = stopwatch.ElapsedMilliseconds,
                MemoryUsage = GC.GetTotalMemory(false),
                Gen0Collections = GC.CollectionCount(0),
                Gen1Collections = GC.CollectionCount(1),
                Gen2Collections = GC.CollectionCount(2)
            });

            // Analyze memory usage
            var peakMemory = memorySnapshots.Max(s => s.MemoryUsage);
            var finalMemory = memorySnapshots.Last().MemoryUsage;
            var memoryIncrease = finalMemory - initialMemory;
            var totalGen0Collections = memorySnapshots.Last().Gen0Collections - memorySnapshots.First().Gen0Collections;
            var totalGen1Collections = memorySnapshots.Last().Gen1Collections - memorySnapshots.First().Gen1Collections;
            var totalGen2Collections = memorySnapshots.Last().Gen2Collections - memorySnapshots.First().Gen2Collections;

            metrics["InitialMemoryMb"] = initialMemory / (1024.0 * 1024.0);
            metrics["PeakMemoryMb"] = peakMemory / (1024.0 * 1024.0);
            metrics["FinalMemoryMb"] = finalMemory / (1024.0 * 1024.0);
            metrics["MemoryIncreaseMb"] = memoryIncrease / (1024.0 * 1024.0);
            metrics["TotalGen0Collections"] = totalGen0Collections;
            metrics["TotalGen1Collections"] = totalGen1Collections;
            metrics["TotalGen2Collections"] = totalGen2Collections;
            metrics["AllocationsPerformed"] = AllocationCount;
            metrics["AllocationSizeMb"] = AllocationSize / (1024.0 * 1024.0);
            metrics["MemorySnapshots"] = memorySnapshots.Select(s => s.ToDictionary()).ToArray();

            // Determine test result
            var issues = new List<string>();

            if (memoryIncrease > 10 * 1024 * 1024) // 10MB
            {
                issues.Add($"High memory increase: {memoryIncrease / (1024.0 * 1024.0):F1}MB");
                recommendations.Add("Investigate memory leaks");
            }

            if (totalGen2Collections > 5)
            {
                issues.Add($"Excessive Gen2 collections: {totalGen2Collections}");
                recommendations.Add("Optimize object lifecycle management");
            }

            if (totalGen1Collections > 20)
            {
                issues.Add($"High Gen1 collections: {totalGen1Collections}");
                recommendations.Add("Review object allocation patterns");
            }

            if (issues.Count > 0)
            {
                recommendations.Add("Monitor memory usage patterns");
                recommendations.Add("Implement memory usage alerts");

                var errorMessage = $"Memory usage issues detected: {string.Join("; ", issues)}";

                return TestResult.Failure(
                    testId,
                    testName,
                    TestCategory.Performance,
                    stopwatch.Elapsed,
                    errorMessage,
                    metrics: metrics,
                    recommendations: recommendations);
            }

            var successMessage = $"Memory usage acceptable - Peak: {peakMemory / (1024.0 * 1024.0):F1}MB, Final: {finalMemory / (1024.0 * 1024.0):F1}MB";
            recommendations.Add("Continue monitoring memory trends");
            recommendations.Add("Set up memory usage alerts");

            return TestResult.Success(
                testId,
                testName,
                TestCategory.Performance,
                stopwatch.Elapsed,
                successMessage,
                metrics,
                recommendations);
        }
        catch (Exception ex)
        {
            var errorMessage = _errorService.CreateAndLogError(
                ex,
                "TEST-032",
                "Memory usage test failed",
                new Dictionary<string, object>
                {
                    ["TestId"] = testId,
                    ["TestName"] = testName
                });

            return TestResult.Failure(
                testId,
                testName,
                TestCategory.Performance,
                stopwatch.Elapsed,
                errorMessage.Summary,
                ex,
                metrics,
                errorMessage.TroubleshootingSteps,
                TestSeverity.Error);
        }
    }

    /// <summary>
    /// Collect system performance metrics
    /// </summary>
    /// <returns>System metrics</returns>
    private static async Task<SystemMetrics> CollectSystemMetricsAsync()
    {
        var process = Process.GetCurrentProcess();

        // Get CPU usage (approximate)
        var startTime = DateTime.UtcNow;
        var startCpuUsage = process.TotalProcessorTime;
        await Task.Delay(1000); // Wait 1 second
        var endTime = DateTime.UtcNow;
        var endCpuUsage = process.TotalProcessorTime;

        var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
        var totalMsPassed = (endTime - startTime).TotalMilliseconds;
        var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

        return new SystemMetrics
        {
            CpuUsagePercent = cpuUsageTotal * 100,
            MemoryUsageMb = process.WorkingSet64 / (1024.0 * 1024.0),
            GcCollectionCount = GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2),
            ThreadCount = process.Threads.Count,
            HandleCount = process.HandleCount
        };
    }

    /// <summary>
    /// Simulate processing a data point
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Processing task</returns>
    private static async Task ProcessMockDataPointAsync(CancellationToken cancellationToken)
    {
        // Simulate data processing work
        var random = new Random();
        var value = random.NextDouble() * 1000;

        // Simulate validation
        var isValid = value >= 0 && value <= 1000;

        // Simulate transformation
        var processedValue = value * 1.5 + 10;

        // Simulate some async work
        await Task.Delay(1, cancellationToken);

        // Simulate data quality check
        var quality = isValid ? "Good" : "Bad";
    }

    /// <summary>
    /// Calculate standard deviation
    /// </summary>
    /// <param name="values">Values to calculate standard deviation for</param>
    /// <returns>Standard deviation</returns>
    private static double CalculateStandardDeviation(List<long> values)
    {
        if (values.Count <= 1)
            return 0;

        var average = values.Average();
        var sumOfSquares = values.Select(val => (val - average) * (val - average)).Sum();
        return Math.Sqrt(sumOfSquares / (values.Count - 1));
    }

    /// <summary>
    /// System performance metrics
    /// </summary>
    private sealed class SystemMetrics
    {
        public required double CpuUsagePercent { get; init; }
        public required double MemoryUsageMb { get; init; }
        public required int GcCollectionCount { get; init; }
        public required int ThreadCount { get; init; }
        public required int HandleCount { get; init; }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["CpuUsagePercent"] = CpuUsagePercent,
                ["MemoryUsageMb"] = MemoryUsageMb,
                ["GcCollectionCount"] = GcCollectionCount,
                ["ThreadCount"] = ThreadCount,
                ["HandleCount"] = HandleCount
            };
        }
    }

    /// <summary>
    /// Memory usage snapshot
    /// </summary>
    private sealed class MemorySnapshot
    {
        public required long TimestampMs { get; init; }
        public required long MemoryUsage { get; init; }
        public required int Gen0Collections { get; init; }
        public required int Gen1Collections { get; init; }
        public required int Gen2Collections { get; init; }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["TimestampMs"] = TimestampMs,
                ["MemoryUsageMb"] = MemoryUsage / (1024.0 * 1024.0),
                ["Gen0Collections"] = Gen0Collections,
                ["Gen1Collections"] = Gen1Collections,
                ["Gen2Collections"] = Gen2Collections
            };
        }
    }
}
