using System.Data;
using System.Diagnostics;
using Dapper;
using Industrial.Adam.Oee.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Oee.Infrastructure.Repositories;

/// <summary>
/// Simple TimescaleDB repository for counter data access
/// Provides high-performance queries for OEE calculations
/// </summary>
public sealed class SimpleCounterDataRepository : ICounterDataRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<SimpleCounterDataRepository> _logger;

    /// <summary>
    /// Constructor for counter data repository
    /// </summary>
    /// <param name="connectionFactory">Database connection factory</param>
    /// <param name="logger">Logger instance</param>
    public SimpleCounterDataRepository(
        IDbConnectionFactory connectionFactory,
        ILogger<SimpleCounterDataRepository> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get counter readings for a specific device and time period
    /// </summary>
    public async Task<IEnumerable<CounterReading>> GetDataForPeriodAsync(
        string deviceId,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("Device ID cannot be null or empty", nameof(deviceId));

        if (endTime <= startTime)
            throw new ArgumentException("End time must be after start time", nameof(endTime));

        using var activity = ActivitySource.StartActivity("GetCounterDataForPeriod");
        activity?.SetTag("deviceId", deviceId);

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = @"
                SELECT 
                    device_id as DeviceId,
                    channel,
                    timestamp,
                    rate,
                    processed_value as ProcessedValue,
                    quality
                FROM counter_data 
                WHERE device_id = @deviceId 
                  AND timestamp >= @startTime 
                  AND timestamp <= @endTime
                ORDER BY timestamp ASC";

            _logger.LogDebug("Retrieving counter data for device {DeviceId} from {StartTime} to {EndTime}",
                deviceId, startTime, endTime);

            var counterData = await connection.QueryAsync<CounterDataRow>(sql,
                new { deviceId, startTime, endTime },
                commandTimeout: 30);

            var readings = counterData.Select(MapToCounterReading).ToList();

            _logger.LogInformation("Retrieved {Count} counter readings for device {DeviceId}",
                readings.Count, deviceId);

            return readings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve counter data for device {DeviceId}", deviceId);
            throw;
        }
    }

    /// <summary>
    /// Get the latest counter reading for a specific device and channel
    /// </summary>
    public async Task<CounterReading?> GetLatestReadingAsync(
        string deviceId,
        int channel,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("Device ID cannot be null or empty", nameof(deviceId));

        using var activity = ActivitySource.StartActivity("GetLatestCounterReading");
        activity?.SetTag("deviceId", deviceId);
        activity?.SetTag("channel", channel);

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = @"
                SELECT 
                    device_id as DeviceId,
                    channel,
                    timestamp,
                    rate,
                    processed_value as ProcessedValue,
                    quality
                FROM counter_data 
                WHERE device_id = @deviceId AND channel = @channel
                ORDER BY timestamp DESC
                LIMIT 1";

            _logger.LogDebug("Retrieving latest reading for device {DeviceId} channel {Channel}",
                deviceId, channel);

            var counterData = await connection.QuerySingleOrDefaultAsync<CounterDataRow>(sql,
                new { deviceId, channel });

            if (counterData == null)
            {
                _logger.LogDebug("No readings found for device {DeviceId} channel {Channel}",
                    deviceId, channel);
                return null;
            }

            var reading = MapToCounterReading(counterData);

            _logger.LogDebug("Retrieved latest reading for device {DeviceId} channel {Channel} at {Timestamp}",
                deviceId, channel, reading.Timestamp);

            return reading;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve latest reading for device {DeviceId} channel {Channel}",
                deviceId, channel);
            throw;
        }
    }

    /// <summary>
    /// Get counter readings for multiple channels within a time period
    /// </summary>
    public async Task<IEnumerable<CounterReading>> GetDataForChannelsAsync(
        string deviceId,
        IEnumerable<int> channels,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("Device ID cannot be null or empty", nameof(deviceId));

        if (endTime <= startTime)
            throw new ArgumentException("End time must be after start time", nameof(endTime));

        var channelList = channels.ToList();
        if (!channelList.Any())
            return Enumerable.Empty<CounterReading>();

        using var activity = ActivitySource.StartActivity("GetCounterDataForChannels");
        activity?.SetTag("deviceId", deviceId);
        activity?.SetTag("channelCount", channelList.Count);

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = @"
                SELECT 
                    device_id as DeviceId,
                    channel,
                    timestamp,
                    rate,
                    processed_value as ProcessedValue,
                    quality
                FROM counter_data 
                WHERE device_id = @deviceId 
                  AND channel = ANY(@channels::int[])
                  AND timestamp >= @startTime 
                  AND timestamp <= @endTime
                ORDER BY channel, timestamp ASC";

            _logger.LogDebug("Retrieving counter data for device {DeviceId} channels {Channels} from {StartTime} to {EndTime}",
                deviceId, string.Join(",", channelList), startTime, endTime);

            var counterData = await connection.QueryAsync<CounterDataRow>(sql,
                new { deviceId, channels = channelList.ToArray(), startTime, endTime });

            var readings = counterData.Select(MapToCounterReading).ToList();

            _logger.LogInformation("Retrieved {Count} counter readings for device {DeviceId} across {ChannelCount} channels",
                readings.Count, deviceId, channelList.Count);

            return readings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve counter data for device {DeviceId} channels {Channels}",
                deviceId, string.Join(",", channelList));
            throw;
        }
    }

    /// <summary>
    /// Get aggregated counter data for performance calculations
    /// </summary>
    public async Task<CounterAggregates?> GetAggregatedDataAsync(
        string deviceId,
        int channel,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("Device ID cannot be null or empty", nameof(deviceId));

        if (endTime <= startTime)
            throw new ArgumentException("End time must be after start time", nameof(endTime));

        using var activity = ActivitySource.StartActivity("GetAggregatedCounterData");
        activity?.SetTag("deviceId", deviceId);
        activity?.SetTag("channel", channel);

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = @"
                SELECT 
                    @deviceId as DeviceId,
                    @channel as Channel,
                    @startTime as StartTime,
                    @endTime as EndTime,
                    COALESCE(MAX(processed_value) - MIN(processed_value), 0) as TotalCount,
                    COALESCE(AVG(rate), 0) as AverageRate,
                    COALESCE(MAX(rate), 0) as MaxRate,
                    COALESCE(MIN(rate), 0) as MinRate,
                    COALESCE(SUM(CASE WHEN rate > 0 THEN 1 ELSE 0 END) * 
                        (EXTRACT(EPOCH FROM (@endTime - @startTime)) / 60.0) / 
                        GREATEST(COUNT(*), 1), 0) as RunTimeMinutes,
                    COUNT(*) as DataPoints
                FROM counter_data 
                WHERE device_id = @deviceId 
                  AND channel = @channel
                  AND timestamp >= @startTime 
                  AND timestamp <= @endTime";

            _logger.LogDebug("Calculating aggregated data for device {DeviceId} channel {Channel} from {StartTime} to {EndTime}",
                deviceId, channel, startTime, endTime);

            var aggregates = await connection.QuerySingleOrDefaultAsync<CounterAggregatesData>(sql,
                new { deviceId, channel, startTime, endTime });

            if (aggregates == null || aggregates.DataPoints == 0)
            {
                _logger.LogDebug("No data found for aggregation for device {DeviceId} channel {Channel}",
                    deviceId, channel);
                return null;
            }

            var result = new CounterAggregates(
                aggregates.DeviceId,
                aggregates.Channel,
                aggregates.StartTime,
                aggregates.EndTime,
                aggregates.TotalCount,
                aggregates.AverageRate,
                aggregates.MaxRate,
                aggregates.MinRate,
                aggregates.RunTimeMinutes,
                (int)aggregates.DataPoints
            );

            _logger.LogInformation("Calculated aggregates for device {DeviceId} channel {Channel}: {TotalCount} total, {AverageRate:F2} avg rate",
                deviceId, channel, result.TotalCount, result.AverageRate);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate aggregated data for device {DeviceId} channel {Channel}",
                deviceId, channel);
            throw;
        }
    }

    /// <summary>
    /// Get the current production rate for a device
    /// </summary>
    public async Task<decimal> GetCurrentRateAsync(
        string deviceId,
        int channel,
        int lookbackMinutes = 5,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("Device ID cannot be null or empty", nameof(deviceId));

        if (lookbackMinutes <= 0)
            throw new ArgumentException("Lookback minutes must be positive", nameof(lookbackMinutes));

        using var activity = ActivitySource.StartActivity("GetCurrentProductionRate");
        activity?.SetTag("deviceId", deviceId);
        activity?.SetTag("channel", channel);

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = @"
                SELECT COALESCE(AVG(rate), 0) as CurrentRate
                FROM counter_data 
                WHERE device_id = @deviceId 
                  AND channel = @channel
                  AND timestamp >= NOW() - INTERVAL '1 minute' * @lookbackMinutes";

            _logger.LogDebug("Calculating current rate for device {DeviceId} channel {Channel} over {LookbackMinutes} minutes",
                deviceId, channel, lookbackMinutes);

            var currentRate = await connection.QuerySingleAsync<decimal>(sql,
                new { deviceId, channel, lookbackMinutes });

            _logger.LogDebug("Current rate for device {DeviceId} channel {Channel}: {Rate:F2}",
                deviceId, channel, currentRate);

            return currentRate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate current rate for device {DeviceId} channel {Channel}",
                deviceId, channel);
            throw;
        }
    }

    /// <summary>
    /// Check if a device has been producing within a time period
    /// </summary>
    public async Task<bool> HasProductionActivityAsync(
        string deviceId,
        int channel,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("Device ID cannot be null or empty", nameof(deviceId));

        if (endTime <= startTime)
            throw new ArgumentException("End time must be after start time", nameof(endTime));

        using var activity = ActivitySource.StartActivity("CheckProductionActivity");
        activity?.SetTag("deviceId", deviceId);
        activity?.SetTag("channel", channel);

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = @"
                SELECT EXISTS(
                    SELECT 1 
                    FROM counter_data 
                    WHERE device_id = @deviceId 
                      AND channel = @channel
                      AND timestamp >= @startTime 
                      AND timestamp <= @endTime
                      AND rate > 0
                ) as HasActivity";

            _logger.LogDebug("Checking production activity for device {DeviceId} channel {Channel} from {StartTime} to {EndTime}",
                deviceId, channel, startTime, endTime);

            var hasActivity = await connection.QuerySingleAsync<bool>(sql,
                new { deviceId, channel, startTime, endTime });

            _logger.LogDebug("Production activity for device {DeviceId} channel {Channel}: {HasActivity}",
                deviceId, channel, hasActivity);

            return hasActivity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check production activity for device {DeviceId} channel {Channel}",
                deviceId, channel);
            throw;
        }
    }

    /// <summary>
    /// Get downtime periods where production was stopped
    /// </summary>
    public async Task<IEnumerable<DowntimePeriod>> GetDowntimePeriodsAsync(
        string deviceId,
        int channel,
        DateTime startTime,
        DateTime endTime,
        int minimumStoppageMinutes = 5,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("Device ID cannot be null or empty", nameof(deviceId));

        if (endTime <= startTime)
            throw new ArgumentException("End time must be after start time", nameof(endTime));

        if (minimumStoppageMinutes <= 0)
            throw new ArgumentException("Minimum stoppage minutes must be positive", nameof(minimumStoppageMinutes));

        using var activity = ActivitySource.StartActivity("GetDowntimePeriods");
        activity?.SetTag("deviceId", deviceId);
        activity?.SetTag("channel", channel);

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            // Complex query to identify downtime periods
            const string sql = @"
                WITH production_status AS (
                    SELECT 
                        timestamp,
                        rate > 0 as is_producing,
                        LAG(rate > 0, 1, true) OVER (ORDER BY timestamp) as prev_producing
                    FROM counter_data 
                    WHERE device_id = @deviceId 
                      AND channel = @channel
                      AND timestamp >= @startTime 
                      AND timestamp <= @endTime
                    ORDER BY timestamp
                ),
                downtime_starts AS (
                    SELECT timestamp as start_time
                    FROM production_status
                    WHERE NOT is_producing AND prev_producing
                ),
                downtime_ends AS (
                    SELECT timestamp as end_time
                    FROM production_status
                    WHERE is_producing AND NOT prev_producing
                ),
                downtime_periods AS (
                    SELECT 
                        ds.start_time,
                        de.end_time,
                        EXTRACT(EPOCH FROM (COALESCE(de.end_time, @endTime) - ds.start_time)) / 60.0 as duration_minutes
                    FROM downtime_starts ds
                    LEFT JOIN downtime_ends de ON de.end_time > ds.start_time
                    WHERE EXTRACT(EPOCH FROM (COALESCE(de.end_time, @endTime) - ds.start_time)) / 60.0 >= @minimumStoppageMinutes
                )
                SELECT 
                    start_time as StartTime,
                    end_time as EndTime,
                    duration_minutes as DurationMinutes,
                    (end_time IS NULL) as IsOngoing
                FROM downtime_periods
                ORDER BY start_time";

            _logger.LogDebug("Identifying downtime periods for device {DeviceId} channel {Channel} (min {MinimumMinutes} minutes)",
                deviceId, channel, minimumStoppageMinutes);

            var downtimeData = await connection.QueryAsync<DowntimePeriodData>(sql,
                new { deviceId, channel, startTime, endTime, minimumStoppageMinutes });

            var downtimes = downtimeData.Select(d => new DowntimePeriod(
                d.StartTime,
                d.EndTime,
                d.DurationMinutes,
                d.IsOngoing
            )).ToList();

            _logger.LogInformation("Found {Count} downtime periods for device {DeviceId} channel {Channel}",
                downtimes.Count, deviceId, channel);

            return downtimes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve downtime periods for device {DeviceId} channel {Channel}",
                deviceId, channel);
            throw;
        }
    }

    /// <summary>
    /// Maps database row data to CounterReading domain object
    /// </summary>
    private static CounterReading MapToCounterReading(CounterDataRow data)
    {
        return new CounterReading(
            data.DeviceId,
            data.Channel,
            data.Timestamp,
            data.Rate,
            data.ProcessedValue,
            data.Quality
        );
    }

    private static readonly System.Diagnostics.ActivitySource ActivitySource = new("Industrial.Adam.Oee.Infrastructure");

    /// <summary>
    /// Data structure for mapping database rows to domain objects
    /// </summary>
    private sealed record CounterDataRow(
        string DeviceId,
        int Channel,
        DateTime Timestamp,
        decimal Rate,
        decimal ProcessedValue,
        string? Quality
    );

    /// <summary>
    /// Data structure for aggregated counter statistics
    /// </summary>
    private sealed class CounterAggregatesData
    {
        public string DeviceId { get; set; } = string.Empty;
        public int Channel { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal TotalCount { get; set; }
        public decimal AverageRate { get; set; }
        public decimal MaxRate { get; set; }
        public decimal MinRate { get; set; }
        public decimal RunTimeMinutes { get; set; }
        public long DataPoints { get; set; }
    }

    /// <summary>
    /// Data structure for downtime period mapping
    /// </summary>
    private sealed record DowntimePeriodData(
        DateTime StartTime,
        DateTime? EndTime,
        decimal DurationMinutes,
        bool IsOngoing
    );
}
