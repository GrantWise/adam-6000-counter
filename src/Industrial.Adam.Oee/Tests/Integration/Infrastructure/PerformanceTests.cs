using System.Data;
using System.Diagnostics;
using Dapper;
using Industrial.Adam.Oee.Domain.Interfaces;
using Industrial.Adam.Oee.Infrastructure;
using Industrial.Adam.Oee.Infrastructure.Repositories;
using Industrial.Adam.Oee.Infrastructure.Services;
using Industrial.Adam.Oee.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using Xunit;
using Xunit.Abstractions;

namespace Industrial.Adam.Oee.Tests.Integration.Infrastructure;

/// <summary>
/// Performance tests for OEE infrastructure components
/// Validates sub-100ms query performance requirements
/// Uses centralized container management for proper port allocation
/// </summary>
public sealed class PerformanceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly ITestOutputHelper _output;
    private IDbConnectionFactory _connectionFactory = null!;
    private ICounterDataRepository _counterDataRepository = null!;
    private IWorkOrderRepository _workOrderRepository = null!;
    private IServiceProvider _serviceProvider = null!;
    private const string TestClassName = nameof(PerformanceTests);

    private const int PerformanceThresholdMs = 100;
    private const int LargeDataSetSize = 10000;

    public PerformanceTests(ITestOutputHelper output)
    {
        _output = output;
        _postgresContainer = TestContainerManager.CreateContainer(TestClassName);
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        services.AddSingleton<DataAccessMetrics>();

        _serviceProvider = services.BuildServiceProvider();
        _connectionFactory = TestContainerManager.CreateConnectionFactory(_postgresContainer, _serviceProvider);

        var logger = _serviceProvider.GetRequiredService<ILogger<SimpleCounterDataRepository>>();
        var workOrderLogger = _serviceProvider.GetRequiredService<ILogger<WorkOrderRepository>>();

        _counterDataRepository = new SimpleCounterDataRepository(_connectionFactory, logger);
        _workOrderRepository = new WorkOrderRepository(_connectionFactory, workOrderLogger);

        await TestContainerManager.SetupOeeDatabaseAsync(_connectionFactory);
        await SetupPerformanceIndexesAsync();
        await SeedLargeDataSetAsync();
    }

    public async Task DisposeAsync()
    {
        if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();
        await TestContainerManager.DisposeContainerAsync(TestClassName);
    }

    [Fact]
    public async Task CounterDataRepository_GetDataForPeriod_MeetsPerformanceThreshold()
    {
        // Arrange
        var deviceId = "PERF_DEVICE_001";
        var startTime = DateTime.UtcNow.AddHours(-1);
        var endTime = DateTime.UtcNow;

        // Act & Measure
        var stopwatch = Stopwatch.StartNew();
        var result = await _counterDataRepository.GetDataForPeriodAsync(deviceId, startTime, endTime);
        stopwatch.Stop();

        var readings = result.ToList();

        // Assert
        _output.WriteLine($"GetDataForPeriod: {stopwatch.ElapsedMilliseconds}ms for {readings.Count} readings");
        Assert.True(stopwatch.ElapsedMilliseconds < PerformanceThresholdMs,
            $"Query took {stopwatch.ElapsedMilliseconds}ms, exceeding threshold of {PerformanceThresholdMs}ms");
        Assert.NotEmpty(readings);
    }

    [Fact]
    public async Task CounterDataRepository_GetLatestReading_MeetsPerformanceThreshold()
    {
        // Arrange
        var deviceId = "PERF_DEVICE_002";
        var channel = 0;

        // Act & Measure
        var stopwatch = Stopwatch.StartNew();
        var result = await _counterDataRepository.GetLatestReadingAsync(deviceId, channel);
        stopwatch.Stop();

        // Assert
        _output.WriteLine($"GetLatestReading: {stopwatch.ElapsedMilliseconds}ms");
        Assert.True(stopwatch.ElapsedMilliseconds < PerformanceThresholdMs,
            $"Query took {stopwatch.ElapsedMilliseconds}ms, exceeding threshold of {PerformanceThresholdMs}ms");
        Assert.NotNull(result);
    }

    [Fact]
    public async Task CounterDataRepository_GetAggregatedData_MeetsPerformanceThreshold()
    {
        // Arrange
        var deviceId = "PERF_DEVICE_003";
        var channel = 0;
        var startTime = DateTime.UtcNow.AddHours(-2);
        var endTime = DateTime.UtcNow;

        // Act & Measure
        var stopwatch = Stopwatch.StartNew();
        var result = await _counterDataRepository.GetAggregatedDataAsync(deviceId, channel, startTime, endTime);
        stopwatch.Stop();

        // Assert
        _output.WriteLine($"GetAggregatedData: {stopwatch.ElapsedMilliseconds}ms");
        Assert.True(stopwatch.ElapsedMilliseconds < PerformanceThresholdMs,
            $"Query took {stopwatch.ElapsedMilliseconds}ms, exceeding threshold of {PerformanceThresholdMs}ms");
        Assert.NotNull(result);
    }

    [Fact]
    public async Task CounterDataRepository_GetCurrentRate_MeetsPerformanceThreshold()
    {
        // Arrange
        var deviceId = "PERF_DEVICE_004";
        var channel = 0;
        var lookbackMinutes = 10;

        // Act & Measure
        var stopwatch = Stopwatch.StartNew();
        var result = await _counterDataRepository.GetCurrentRateAsync(deviceId, channel, lookbackMinutes);
        stopwatch.Stop();

        // Assert
        _output.WriteLine($"GetCurrentRate: {stopwatch.ElapsedMilliseconds}ms, Rate: {result:F2}");
        Assert.True(stopwatch.ElapsedMilliseconds < PerformanceThresholdMs,
            $"Query took {stopwatch.ElapsedMilliseconds}ms, exceeding threshold of {PerformanceThresholdMs}ms");
        Assert.True(result >= 0);
    }

    [Fact]
    public async Task WorkOrderRepository_GetActiveByDevice_MeetsPerformanceThreshold()
    {
        // Arrange
        var deviceId = "PERF_DEVICE_005";

        // Act & Measure
        var stopwatch = Stopwatch.StartNew();
        var result = await _workOrderRepository.GetActiveByDeviceAsync(deviceId);
        stopwatch.Stop();

        // Assert
        _output.WriteLine($"GetActiveByDevice: {stopwatch.ElapsedMilliseconds}ms");
        Assert.True(stopwatch.ElapsedMilliseconds < PerformanceThresholdMs,
            $"Query took {stopwatch.ElapsedMilliseconds}ms, exceeding threshold of {PerformanceThresholdMs}ms");
    }

    [Fact]
    public async Task WorkOrderRepository_GetById_MeetsPerformanceThreshold()
    {
        // Arrange
        var workOrderId = "PERF_WO_001";

        // Act & Measure
        var stopwatch = Stopwatch.StartNew();
        var result = await _workOrderRepository.GetByIdAsync(workOrderId);
        stopwatch.Stop();

        // Assert
        _output.WriteLine($"GetById: {stopwatch.ElapsedMilliseconds}ms");
        Assert.True(stopwatch.ElapsedMilliseconds < PerformanceThresholdMs,
            $"Query took {stopwatch.ElapsedMilliseconds}ms, exceeding threshold of {PerformanceThresholdMs}ms");
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ConcurrentQueries_MaintainPerformanceUnderLoad()
    {
        // Arrange
        var deviceIds = Enumerable.Range(1, 10).Select(i => $"PERF_DEVICE_{i:D3}").ToList();
        var tasks = new List<Task<long>>();

        // Act - Execute concurrent queries
        foreach (var deviceId in deviceIds)
        {
            tasks.Add(Task.Run(async () =>
            {
                var stopwatch = Stopwatch.StartNew();
                var startTime = DateTime.UtcNow.AddMinutes(-30);
                var endTime = DateTime.UtcNow;

                var result = await _counterDataRepository.GetDataForPeriodAsync(deviceId, startTime, endTime);
                var readings = result.ToList();

                stopwatch.Stop();
                return stopwatch.ElapsedMilliseconds;
            }));
        }

        var durations = await Task.WhenAll(tasks);

        // Assert
        var maxDuration = durations.Max();
        var avgDuration = durations.Average();

        _output.WriteLine($"Concurrent queries - Max: {maxDuration}ms, Avg: {avgDuration:F1}ms");

        Assert.True(maxDuration < PerformanceThresholdMs * 2, // Allow 2x threshold under load
            $"Slowest concurrent query took {maxDuration}ms, exceeding load threshold");

        Assert.True(avgDuration < PerformanceThresholdMs,
            $"Average query time {avgDuration:F1}ms exceeds threshold of {PerformanceThresholdMs}ms");
    }

    [Fact]
    public async Task IndexEffectiveness_VerifyOptimalQueryPlans()
    {
        // This test verifies that our indexes are being used effectively
        using var connection = await _connectionFactory.CreateConnectionAsync();

        // Test 1: Device + timestamp query should use index
        var plan1 = await connection.QueryAsync<string>(@"
            EXPLAIN (FORMAT JSON)
            SELECT * FROM counter_data 
            WHERE device_id = 'PERF_DEVICE_001' 
              AND timestamp >= NOW() - INTERVAL '1 hour'
              AND channel IN (0, 1)
            ORDER BY timestamp DESC
            LIMIT 100");

        var plan1Json = plan1.First();
        _output.WriteLine($"Query Plan 1: {plan1Json}");

        // Test 2: Latest reading query should use index
        var plan2 = await connection.QueryAsync<string>(@"
            EXPLAIN (FORMAT JSON)
            SELECT * FROM counter_data 
            WHERE device_id = 'PERF_DEVICE_001' 
              AND channel = 0
            ORDER BY timestamp DESC 
            LIMIT 1");

        var plan2Json = plan2.First();
        _output.WriteLine($"Query Plan 2: {plan2Json}");

        // Test 3: Work order active query should use index
        var plan3 = await connection.QueryAsync<string>(@"
            EXPLAIN (FORMAT JSON)
            SELECT * FROM work_orders 
            WHERE resource_reference = 'PERF_DEVICE_001' 
              AND status IN ('Active', 'Paused')
            ORDER BY created_at DESC
            LIMIT 1");

        var plan3Json = plan3.First();
        _output.WriteLine($"Query Plan 3: {plan3Json}");

        // Basic assertion - plans should not contain sequential scans on large tables
        Assert.DoesNotContain("Seq Scan", plan1Json);
        Assert.DoesNotContain("Seq Scan", plan2Json);
        Assert.DoesNotContain("Seq Scan", plan3Json);
    }

    [Fact]
    public async Task MemoryUsage_StaysWithinReasonableLimits()
    {
        // Measure memory usage during large query operations
        var initialMemory = GC.GetTotalMemory(true);

        // Execute multiple large queries
        for (int i = 0; i < 5; i++)
        {
            var deviceId = $"PERF_DEVICE_{i:D3}";
            var startTime = DateTime.UtcNow.AddHours(-4);
            var endTime = DateTime.UtcNow;

            var result = await _counterDataRepository.GetDataForPeriodAsync(deviceId, startTime, endTime);
            var readings = result.ToList(); // Force enumeration

            _output.WriteLine($"Query {i + 1}: Retrieved {readings.Count} readings");
        }

        var finalMemory = GC.GetTotalMemory(true);
        var memoryIncrease = finalMemory - initialMemory;
        var memoryIncreaseMB = memoryIncrease / (1024 * 1024);

        _output.WriteLine($"Memory increase: {memoryIncreaseMB:F1} MB");

        // Assert memory increase is reasonable (less than 50MB for test operations)
        Assert.True(memoryIncreaseMB < 50,
            $"Memory increase of {memoryIncreaseMB:F1} MB is too high for test operations");
    }

    /// <summary>
    /// Add additional performance indexes beyond the base schema
    /// </summary>
    private async Task SetupPerformanceIndexesAsync()
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();

        // Create additional performance indexes for testing
        await connection.ExecuteAsync(@"
            CREATE INDEX IF NOT EXISTS idx_counter_data_device_channel_timestamp 
            ON counter_data(device_id, channel, timestamp DESC)
            WHERE channel IN (0, 1);

            CREATE INDEX IF NOT EXISTS idx_counter_data_latest_by_device_channel 
            ON counter_data(device_id, channel, timestamp DESC, rate)
            WHERE channel IN (0, 1) AND rate IS NOT NULL;");

        // Analyze tables for optimal query planning
        await connection.ExecuteAsync("ANALYZE counter_data; ANALYZE work_orders;");
    }

    /// <summary>
    /// Seed large dataset for performance testing
    /// </summary>
    private async Task SeedLargeDataSetAsync()
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();

        _output.WriteLine($"Seeding {LargeDataSetSize} counter data records...");

        var random = new Random();
        var devices = Enumerable.Range(1, 10).Select(i => $"PERF_DEVICE_{i:D3}").ToList();
        var startTime = DateTime.UtcNow.AddHours(-24); // 24 hours of data

        var batchSize = 1000;
        var batches = LargeDataSetSize / batchSize;

        for (int batch = 0; batch < batches; batch++)
        {
            var records = new List<object>();

            for (int i = 0; i < batchSize; i++)
            {
                var deviceId = devices[random.Next(devices.Count)];
                var channel = random.Next(2); // 0 or 1
                var timestamp = startTime.AddMinutes(random.Next(0, 24 * 60)); // Random time in 24h
                var rate = random.Next(0, 100) / 100.0m; // Random rate 0-1
                var processedValue = 1000m + (decimal)(random.NextDouble() * 9000); // 1000-10000

                records.Add(new
                {
                    timestamp,
                    device_id = deviceId,
                    channel,
                    rate,
                    processed_value = processedValue,
                    quality = "Good"
                });
            }

            await connection.ExecuteAsync(@"
                INSERT INTO counter_data (timestamp, device_id, channel, rate, processed_value, quality)
                VALUES (@timestamp, @device_id, @channel, @rate, @processed_value, @quality)",
                records);

            if (batch % 10 == 0)
            {
                _output.WriteLine($"Seeded batch {batch + 1}/{batches}");
            }
        }

        // Seed some work orders
        foreach (var deviceId in devices)
        {
            await connection.ExecuteAsync(@"
                INSERT INTO work_orders (
                    work_order_id, work_order_description, product_id, product_description,
                    planned_quantity, scheduled_start_time, scheduled_end_time, resource_reference,
                    status, actual_quantity_good, actual_quantity_scrap
                ) VALUES (
                    @workOrderId, @description, @productId, @productDescription,
                    @plannedQuantity, @scheduledStart, @scheduledEnd, @resourceReference,
                    @status, @actualGood, @actualScrap
                )",
                new
                {
                    workOrderId = $"PERF_WO_{deviceId.Substring(12)}",
                    description = $"Performance test work order for {deviceId}",
                    productId = "PERF_PRODUCT",
                    productDescription = "Performance Test Product",
                    plannedQuantity = 1000m,
                    scheduledStart = DateTime.UtcNow.AddHours(-8),
                    scheduledEnd = DateTime.UtcNow.AddHours(8),
                    resourceReference = deviceId,
                    status = "Active",
                    actualGood = random.Next(0, 500),
                    actualScrap = random.Next(0, 50)
                });
        }

        // Update table statistics
        await connection.ExecuteAsync("ANALYZE counter_data; ANALYZE work_orders;");

        _output.WriteLine("Data seeding completed");
    }
}
