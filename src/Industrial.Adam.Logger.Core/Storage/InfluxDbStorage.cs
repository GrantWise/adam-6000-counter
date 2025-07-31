using System.Collections.Concurrent;
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
    private readonly ConcurrentQueue<PointData> _pendingWrites = new();
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly Timer _batchTimer;
    private volatile bool _disposed;
    
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
        
        // Setup batch timer if configured
        if (_settings.BatchSize > 1)
        {
            _batchTimer = new Timer(
                BatchTimerCallback,
                null,
                TimeSpan.FromMilliseconds(_settings.FlushIntervalMs),
                TimeSpan.FromMilliseconds(_settings.FlushIntervalMs));
        }
        else
        {
            _batchTimer = new Timer(_ => { }, null, Timeout.Infinite, Timeout.Infinite);
        }
        
        _logger.LogInformation(
            "InfluxDB storage initialized for {Url}, org={Org}, bucket={Bucket}",
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
        
        if (_settings.BatchSize > 1)
        {
            // Add to batch queue
            _pendingWrites.Enqueue(point);
            
            // Check if we should flush
            if (_pendingWrites.Count >= _settings.BatchSize)
            {
                await FlushBatchAsync(cancellationToken);
            }
        }
        else
        {
            // Write immediately
            await WritePointAsync(point, cancellationToken);
        }
    }
    
    /// <summary>
    /// Write multiple readings in a batch
    /// </summary>
    public async Task WriteBatchAsync(IEnumerable<DeviceReading> readings, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(InfluxDbStorage));
        
        var points = readings.Select(CreatePointData).ToList();
        
        if (!points.Any())
            return;
        
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await _writeApi.WritePointsAsync(
                points,
                _settings.Bucket,
                _settings.Organization,
                cancellationToken);
            
            _logger.LogDebug("Wrote batch of {Count} points to InfluxDB", points.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write batch of {Count} points to InfluxDB", points.Count);
            throw;
        }
        finally
        {
            _writeLock.Release();
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
        
        await FlushBatchAsync(cancellationToken);
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
    
    private async Task WritePointAsync(PointData point, CancellationToken cancellationToken)
    {
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await _writeApi.WritePointAsync(
                point,
                _settings.Bucket,
                _settings.Organization,
                cancellationToken);
            
            _logger.LogDebug("Wrote point to InfluxDB");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write point to InfluxDB");
            throw;
        }
        finally
        {
            _writeLock.Release();
        }
    }
    
    private async Task FlushBatchAsync(CancellationToken cancellationToken)
    {
        if (_pendingWrites.IsEmpty)
            return;
        
        var points = new List<PointData>();
        while (_pendingWrites.TryDequeue(out var point))
        {
            points.Add(point);
        }
        
        if (points.Any())
        {
            await _writeLock.WaitAsync(cancellationToken);
            try
            {
                await _writeApi.WritePointsAsync(
                    points,
                    _settings.Bucket,
                    _settings.Organization,
                    cancellationToken);
                
                _logger.LogDebug("Flushed batch of {Count} points to InfluxDB", points.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to flush batch of {Count} points to InfluxDB", points.Count);
                
                // Re-queue failed points
                foreach (var point in points)
                {
                    _pendingWrites.Enqueue(point);
                }
                
                throw;
            }
            finally
            {
                _writeLock.Release();
            }
        }
    }
    
    private async void BatchTimerCallback(object? state)
    {
        try
        {
            await FlushBatchAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in batch timer callback");
        }
    }
    
    public void Dispose()
    {
        if (_disposed)
            return;
        
        _disposed = true;
        
        // Stop the timer
        _batchTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        _batchTimer?.Dispose();
        
        // Flush any remaining data
        try
        {
            FlushAsync().Wait(TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error flushing data during dispose");
        }
        
        // Dispose resources
        _writeLock?.Dispose();
        _client?.Dispose();
    }
}