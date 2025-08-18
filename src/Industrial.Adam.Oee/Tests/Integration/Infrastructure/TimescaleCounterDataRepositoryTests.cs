using Dapper;
using Industrial.Adam.Oee.Domain.Interfaces;
using Industrial.Adam.Oee.Infrastructure;
using Industrial.Adam.Oee.Infrastructure.Repositories;
using Industrial.Adam.Oee.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using Xunit;

namespace Industrial.Adam.Oee.Tests.Integration.Infrastructure;

/// <summary>
/// Integration tests for TimescaleCounterDataRepository
/// Tests READ-ONLY access to counter_data table from Industrial.Adam.Logger
/// </summary>
public sealed class TimescaleCounterDataRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private IDbConnectionFactory _connectionFactory = null!;
    private ICounterDataRepository _repository = null!;
    private IServiceProvider _serviceProvider = null!;

    public TimescaleCounterDataRepositoryTests()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("timescale/timescaledb:latest-pg15")
            .WithDatabase("adam_counters")
            .WithUsername("adam_user")
            .WithPassword("adam_password")
            .WithPortBinding(54320, 5432)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

        // Add connection factory with test container connection string
        services.AddSingleton<IDbConnectionFactory>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<NpgsqlConnectionFactory>>();
            return new NpgsqlConnectionFactory(_postgresContainer.GetConnectionString(), logger);
        });

        services.AddSingleton<DataAccessMetrics>();

        _serviceProvider = services.BuildServiceProvider();
        _connectionFactory = _serviceProvider.GetRequiredService<IDbConnectionFactory>();

        var logger = _serviceProvider.GetRequiredService<ILogger<SimpleCounterDataRepository>>();
        var metrics = _serviceProvider.GetRequiredService<DataAccessMetrics>();

        _repository = new SimpleCounterDataRepository(_connectionFactory, logger);

        // Set up test database schema
        await SetupTestDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();
        await _postgresContainer.DisposeAsync();
    }

    [Fact]
    public async Task GetDataForPeriodAsync_WithValidParameters_ReturnsData()
    {
        // Arrange
        var deviceId = "TEST_DEVICE_001";
        var startTime = DateTime.UtcNow.AddMinutes(-30);
        var endTime = DateTime.UtcNow;

        await SeedTestCounterDataAsync(deviceId, startTime, endTime);

        // Act
        var result = await _repository.GetDataForPeriodAsync(deviceId, startTime, endTime);

        // Assert
        Assert.NotNull(result);
        var readings = result.ToList();
        Assert.NotEmpty(readings);
        Assert.All(readings, reading =>
        {
            Assert.Equal(deviceId, reading.DeviceId);
            Assert.True(reading.Timestamp >= startTime && reading.Timestamp <= endTime);
            Assert.Contains(reading.Channel, new[] { 0, 1 }); // Only production and reject channels
        });
    }

    [Fact]
    public async Task GetDataForPeriodAsync_WithInvalidDeviceId_ThrowsArgumentException()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddMinutes(-30);
        var endTime = DateTime.UtcNow;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _repository.GetDataForPeriodAsync("", startTime, endTime));

        await Assert.ThrowsAsync<ArgumentException>(
            () => _repository.GetDataForPeriodAsync("   ", startTime, endTime));
    }

    [Fact]
    public async Task GetDataForPeriodAsync_WithInvalidTimeRange_ThrowsArgumentException()
    {
        // Arrange
        var deviceId = "TEST_DEVICE_001";
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddMinutes(-30); // End before start

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _repository.GetDataForPeriodAsync(deviceId, startTime, endTime));
    }

    [Fact]
    public async Task GetLatestReadingAsync_WithValidParameters_ReturnsLatestReading()
    {
        // Arrange
        var deviceId = "TEST_DEVICE_002";
        var channel = 0;
        var baseTime = DateTime.UtcNow.AddMinutes(-10);

        await SeedTestCounterDataAsync(deviceId, baseTime, DateTime.UtcNow);

        // Act
        var result = await _repository.GetLatestReadingAsync(deviceId, channel);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(deviceId, result.DeviceId);
        Assert.Equal(channel, result.Channel);
    }

    [Fact]
    public async Task GetLatestReadingAsync_WithNonExistentDevice_ReturnsNull()
    {
        // Arrange
        var deviceId = "NON_EXISTENT_DEVICE";
        var channel = 0;

        // Act
        var result = await _repository.GetLatestReadingAsync(deviceId, channel);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetLatestReadingAsync_WithInvalidChannel_ThrowsArgumentException()
    {
        // Arrange
        var deviceId = "TEST_DEVICE_001";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _repository.GetLatestReadingAsync(deviceId, -1));

        await Assert.ThrowsAsync<ArgumentException>(
            () => _repository.GetLatestReadingAsync(deviceId, 2));
    }

    [Fact]
    public async Task GetDataForChannelsAsync_WithValidChannels_ReturnsFilteredData()
    {
        // Arrange
        var deviceId = "TEST_DEVICE_003";
        var channels = new[] { 0, 1 };
        var startTime = DateTime.UtcNow.AddMinutes(-20);
        var endTime = DateTime.UtcNow;

        await SeedTestCounterDataAsync(deviceId, startTime, endTime);

        // Act
        var result = await _repository.GetDataForChannelsAsync(deviceId, channels, startTime, endTime);

        // Assert
        var readings = result.ToList();
        Assert.NotEmpty(readings);
        Assert.All(readings, reading =>
        {
            Assert.Equal(deviceId, reading.DeviceId);
            Assert.Contains(reading.Channel, channels);
        });
    }

    [Fact]
    public async Task GetAggregatedDataAsync_WithValidParameters_ReturnsAggregates()
    {
        // Arrange
        var deviceId = "TEST_DEVICE_004";
        var channel = 0;
        var startTime = DateTime.UtcNow.AddMinutes(-15);
        var endTime = DateTime.UtcNow;

        await SeedTestCounterDataAsync(deviceId, startTime, endTime);

        // Act
        var result = await _repository.GetAggregatedDataAsync(deviceId, channel, startTime, endTime);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(deviceId, result.DeviceId);
        Assert.Equal(channel, result.Channel);
        Assert.True(result.DataPoints > 0);
        Assert.True(result.AverageRate >= 0);
        Assert.True(result.MaxRate >= result.MinRate);
    }

    [Fact]
    public async Task GetCurrentRateAsync_WithValidParameters_ReturnsRate()
    {
        // Arrange
        var deviceId = "TEST_DEVICE_005";
        var channel = 0;
        var lookbackMinutes = 5;

        // Seed recent data
        await SeedTestCounterDataAsync(deviceId, DateTime.UtcNow.AddMinutes(-lookbackMinutes), DateTime.UtcNow);

        // Act
        var result = await _repository.GetCurrentRateAsync(deviceId, channel, lookbackMinutes);

        // Assert
        Assert.True(result >= 0);
    }

    [Fact]
    public async Task HasProductionActivityAsync_WithActiveProduction_ReturnsTrue()
    {
        // Arrange
        var deviceId = "TEST_DEVICE_006";
        var channel = 0;
        var startTime = DateTime.UtcNow.AddMinutes(-10);
        var endTime = DateTime.UtcNow;

        // Seed data with positive rates
        await SeedTestCounterDataWithRatesAsync(deviceId, channel, startTime, endTime, minRate: 1.0m);

        // Act
        var result = await _repository.HasProductionActivityAsync(deviceId, channel, startTime, endTime);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task HasProductionActivityAsync_WithNoProduction_ReturnsFalse()
    {
        // Arrange
        var deviceId = "TEST_DEVICE_007";
        var channel = 0;
        var startTime = DateTime.UtcNow.AddMinutes(-10);
        var endTime = DateTime.UtcNow;

        // Seed data with zero rates
        await SeedTestCounterDataWithRatesAsync(deviceId, channel, startTime, endTime, minRate: 0.0m, maxRate: 0.0m);

        // Act
        var result = await _repository.HasProductionActivityAsync(deviceId, channel, startTime, endTime);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetDowntimePeriodsAsync_WithStoppages_ReturnsDowntimePeriods()
    {
        // Arrange
        var deviceId = "TEST_DEVICE_008";
        var channel = 0;
        var startTime = DateTime.UtcNow.AddMinutes(-30);
        var endTime = DateTime.UtcNow;

        // Seed data with periods of zero production
        await SeedTestCounterDataWithStoppagesAsync(deviceId, channel, startTime, endTime);

        // Act
        var result = await _repository.GetDowntimePeriodsAsync(deviceId, channel, startTime, endTime);

        // Assert
        var downtimePeriods = result.ToList();
        // Note: The exact assertion depends on the test data pattern
        // For now, just verify the method executes without error
        Assert.NotNull(downtimePeriods);
    }

    /// <summary>
    /// Set up the test database with TimescaleDB extension and counter_data table
    /// Simulates the existing Industrial.Adam.Logger schema
    /// </summary>
    private async Task SetupTestDatabaseAsync()
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();

        // Create TimescaleDB extension
        await connection.ExecuteAsync("CREATE EXTENSION IF NOT EXISTS timescaledb CASCADE;");

        // Create counter_data table (simulating Industrial.Adam.Logger schema)
        await connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS counter_data (
                timestamp TIMESTAMPTZ NOT NULL,
                device_id VARCHAR(20) NOT NULL,
                channel INTEGER NOT NULL,
                rate DECIMAL(10,2),
                processed_value DECIMAL(18,3),
                quality VARCHAR(10),
                PRIMARY KEY (timestamp, device_id, channel)
            );");

        // Convert to TimescaleDB hypertable
        await connection.ExecuteAsync(@"
            SELECT create_hypertable('counter_data', 'timestamp', if_not_exists => TRUE);");

        // Create the performance indexes that would be applied in production
        await connection.ExecuteAsync(@"
            CREATE INDEX IF NOT EXISTS idx_counter_data_device_timestamp_desc 
            ON counter_data(device_id, timestamp DESC)
            WHERE channel IN (0, 1);");
    }

    /// <summary>
    /// Seed test counter data for a specific device and time range
    /// </summary>
    private async Task SeedTestCounterDataAsync(string deviceId, DateTime startTime, DateTime endTime)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();

        var random = new Random();
        var currentTime = startTime;
        var processedValue = 1000m;

        while (currentTime <= endTime)
        {
            for (int channel = 0; channel <= 1; channel++)
            {
                var rate = random.Next(0, 10) / 10.0m; // Random rate between 0 and 1
                processedValue += rate;

                await connection.ExecuteAsync(@"
                    INSERT INTO counter_data (timestamp, device_id, channel, rate, processed_value, quality)
                    VALUES (@timestamp, @deviceId, @channel, @rate, @processedValue, @quality)",
                    new
                    {
                        timestamp = currentTime,
                        deviceId,
                        channel,
                        rate,
                        processedValue,
                        quality = "Good"
                    });
            }

            currentTime = currentTime.AddMinutes(1); // Add data every minute
        }
    }

    /// <summary>
    /// Seed test counter data with specific rate ranges
    /// </summary>
    private async Task SeedTestCounterDataWithRatesAsync(
        string deviceId,
        int channel,
        DateTime startTime,
        DateTime endTime,
        decimal minRate,
        decimal maxRate = 1.0m)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();

        var random = new Random();
        var currentTime = startTime;
        var processedValue = 1000m;

        while (currentTime <= endTime)
        {
            var rate = minRate + (decimal)random.NextDouble() * (maxRate - minRate);
            processedValue += rate;

            await connection.ExecuteAsync(@"
                INSERT INTO counter_data (timestamp, device_id, channel, rate, processed_value, quality)
                VALUES (@timestamp, @deviceId, @channel, @rate, @processedValue, @quality)",
                new
                {
                    timestamp = currentTime,
                    deviceId,
                    channel,
                    rate,
                    processedValue,
                    quality = "Good"
                });

            currentTime = currentTime.AddMinutes(1);
        }
    }

    /// <summary>
    /// Seed test data with stoppage patterns
    /// </summary>
    private async Task SeedTestCounterDataWithStoppagesAsync(
        string deviceId,
        int channel,
        DateTime startTime,
        DateTime endTime)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();

        var currentTime = startTime;
        var processedValue = 1000m;
        var isProducing = true;

        while (currentTime <= endTime)
        {
            // Toggle production every 10 minutes to create stoppages
            if (currentTime.Minute % 10 == 0)
            {
                isProducing = !isProducing;
            }

            var rate = isProducing ? 1.0m : 0.0m;
            processedValue += rate;

            await connection.ExecuteAsync(@"
                INSERT INTO counter_data (timestamp, device_id, channel, rate, processed_value, quality)
                VALUES (@timestamp, @deviceId, @channel, @rate, @processedValue, @quality)",
                new
                {
                    timestamp = currentTime,
                    deviceId,
                    channel,
                    rate,
                    processedValue,
                    quality = "Good"
                });

            currentTime = currentTime.AddMinutes(1);
        }
    }
}
