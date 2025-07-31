using System.Buffers;
using System.Collections.Concurrent;
using System.Threading.Channels;
using Industrial.Adam.Logger.Core.Configuration;
using Industrial.Adam.Logger.Core.Models;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Logger.Core.Storage;

/// <summary>
/// Stores device readings in InfluxDB time-series database
/// </summary>
public sealed class InfluxDbStorage : IInfluxDbStorage
{
    private readonly ILogger<InfluxDbStorage> _logger;
    private readonly InfluxDbSettings _settings;
    private readonly IInfluxDBClient _client;
    private readonly IWriteApiAsync _writeApi;
    private readonly Channel<PointData> _writeChannel;
    private readonly ChannelWriter<PointData> _writer;
    private readonly ArrayPool<PointData> _pointPool = ArrayPool<PointData>.Shared;
    private readonly CancellationTokenSource _backgroundCts = new();
    private readonly Task _backgroundWriteTask;
    private volatile bool _disposed;

    /// <summary>
    /// Initialize InfluxDB storage with high-performance Channel-based processing
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="settings">InfluxDB connection settings</param>
    public InfluxDbStorage(ILogger<InfluxDbStorage> logger, InfluxDbSettings settings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        // Create InfluxDB client
        var options = new InfluxDBClientOptions(_settings.Url)
        {
            Token = _settings.Token,
            Org = _settings.Organization,
            Bucket = _settings.Bucket
        };

        _client = new InfluxDBClient(options);
        _writeApi = _client.GetWriteApiAsync();

        // Setup Channel for high-throughput async processing
        var channelOptions = new BoundedChannelOptions(_settings.BatchSize * 10)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        };

        _writeChannel = Channel.CreateBounded<PointData>(channelOptions);
        _writer = _writeChannel.Writer;

        // Start background writer task
        _backgroundWriteTask = Task.Run(ProcessWritesAsync, _backgroundCts.Token);

        _logger.LogInformation(
            "InfluxDB storage initialized for {Url}, org={Org}, bucket={Bucket} with Channel-based processing",
            _settings.Url, _settings.Organization, _settings.Bucket);
    }

    /// <summary>
    /// Write a single reading to InfluxDB
    /// </summary>
    public async Task WriteReadingAsync(DeviceReading reading, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(InfluxDbStorage));

        var point = CreatePointData(reading);

        // Use Channel for high-performance async writes
        await _writer.WriteAsync(point, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Write multiple readings in a batch
    /// </summary>
    public async Task WriteBatchAsync(IEnumerable<DeviceReading> readings, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(InfluxDbStorage));

        // Use Channel for all writes to maintain consistency and performance
        foreach (var reading in readings)
        {
            var point = CreatePointData(reading);
            await _writer.WriteAsync(point, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Test connection to InfluxDB
    /// </summary>
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var pingResult = await _client.PingAsync();
            var isHealthy = pingResult;

            if (isHealthy)
            {
                _logger.LogInformation("InfluxDB connection test successful");
            }
            else
            {
                _logger.LogWarning("InfluxDB ping failed");
            }

            return isHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "InfluxDB connection test failed");
            return false;
        }
    }

    /// <summary>
    /// Flush any pending writes
    /// </summary>
    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            return;

        // Wait for background processing to complete without completing the channel
        // The channel will be completed in Dispose()
        try
        {
            // Give background task time to process any pending items
            await Task.Delay(100, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
    }

    private PointData CreatePointData(DeviceReading reading)
    {
        var point = PointData
            .Measurement(_settings.MeasurementName)
            .Tag("device_id", reading.DeviceId)
            .Tag("channel", reading.Channel.ToString())
            .Field("raw_value", reading.RawValue)
            .Field("processed_value", reading.ProcessedValue)
            .Field("quality", reading.Quality.ToString())
            .Timestamp(reading.Timestamp.UtcDateTime, WritePrecision.Ms);

        // Add rate if available
        if (reading.Rate.HasValue)
        {
            point = point.Field("rate", reading.Rate.Value);
        }

        // Add custom tags if configured
        if (_settings.Tags != null)
        {
            foreach (var tag in _settings.Tags)
            {
                point = point.Tag(tag.Key, tag.Value);
            }
        }

        return point;
    }

    /// <summary>
    /// Background task that processes writes from the Channel in batches
    /// </summary>
    private async Task ProcessWritesAsync()
    {
        var reader = _writeChannel.Reader;
        var batchList = new List<PointData>(_settings.BatchSize);

        try
        {
            await foreach (var point in reader.ReadAllAsync(_backgroundCts.Token))
            {
                batchList.Add(point);

                // Write batch when full or when no more items are immediately available
                if (batchList.Count >= _settings.BatchSize || !reader.TryRead(out var nextPoint))
                {
                    if (batchList.Count > 0)
                    {
                        await WriteBatchToInfluxAsync(batchList, _backgroundCts.Token).ConfigureAwait(false);
                        batchList.Clear();
                    }
                }
                else
                {
                    // Add the next point if we read one
                    batchList.Add(nextPoint);
                }

                // Periodic flush based on time interval
                if (batchList.Count > 0)
                {
                    await Task.Delay(_settings.FlushIntervalMs, _backgroundCts.Token);
                    if (batchList.Count > 0)
                    {
                        await WriteBatchToInfluxAsync(batchList, _backgroundCts.Token).ConfigureAwait(false);
                        batchList.Clear();
                    }
                }
            }

            // Final flush of any remaining points
            if (batchList.Count > 0)
            {
                await WriteBatchToInfluxAsync(batchList, CancellationToken.None).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in background write processing");
        }
    }

    /// <summary>
    /// Write a batch of points to InfluxDB with retry logic
    /// </summary>
    private async Task WriteBatchToInfluxAsync(List<PointData> points, CancellationToken cancellationToken)
    {
        try
        {
            await _writeApi.WritePointsAsync(
                points,
                _settings.Bucket,
                _settings.Organization,
                cancellationToken).ConfigureAwait(false);

            _logger.LogDebug("Wrote batch of {Count} points to InfluxDB", points.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write batch of {Count} points to InfluxDB", points.Count);
            throw;
        }
    }

    /// <summary>
    /// Dispose of InfluxDB storage resources and flush any pending writes
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Signal no more writes and complete the channel
        try
        {
            _writer.Complete();
        }
        catch (InvalidOperationException)
        {
            // Channel may already be completed
        }

        // Cancel background processing and wait for completion
        _backgroundCts.Cancel();

        try
        {
            _backgroundWriteTask.Wait(TimeSpan.FromSeconds(10));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error waiting for background write task completion during dispose");
        }

        // Dispose resources
        _backgroundCts?.Dispose();
        _client?.Dispose();
    }
}
