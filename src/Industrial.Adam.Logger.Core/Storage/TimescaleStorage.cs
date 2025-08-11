using System.Collections.Concurrent;
using System.Data;
using System.Text;
using System.Threading.Channels;
using Industrial.Adam.Logger.Core.Configuration;
using Industrial.Adam.Logger.Core.Models;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;

namespace Industrial.Adam.Logger.Core.Storage;

/// <summary>
/// Stores device readings in TimescaleDB (PostgreSQL with TimescaleDB extension)
/// </summary>
public sealed class TimescaleStorage : ITimescaleStorage
{
    private readonly ILogger<TimescaleStorage> _logger;
    private readonly TimescaleSettings _settings;
    private readonly string _connectionString;
    private readonly Channel<DeviceReading> _writeChannel;
    private readonly ChannelWriter<DeviceReading> _writer;
    private readonly CancellationTokenSource _backgroundCts = new();
    private readonly Task _backgroundWriteTask;
    private volatile bool _disposed;

    // Health monitoring fields
    private volatile bool _isBackgroundTaskHealthy = true;
    private DateTimeOffset? _lastSuccessfulWrite;
    private string? _lastError;
    private readonly object _healthLock = new();

    // SQL statements
    private static readonly string _createTableSql = """
        CREATE TABLE IF NOT EXISTS {0} (
            timestamp TIMESTAMPTZ NOT NULL,
            device_id TEXT NOT NULL,
            channel INTEGER NOT NULL,
            raw_value BIGINT NOT NULL,
            processed_value DOUBLE PRECISION,
            rate DOUBLE PRECISION,
            quality TEXT,
            unit TEXT DEFAULT 'counts',
            PRIMARY KEY (timestamp, device_id, channel)
        );
        
        -- Create hypertable if it doesn't exist (TimescaleDB extension)
        SELECT CASE 
            WHEN NOT EXISTS (
                SELECT 1 FROM _timescaledb_catalog.hypertable 
                WHERE table_name = '{0}' AND schema_name = 'public'
            ) THEN create_hypertable('public.{0}', 'timestamp', chunk_time_interval => INTERVAL '1 hour')
            ELSE NULL 
        END;
        """;

    private readonly string _insertSql;

    /// <summary>
    /// Initialize TimescaleDB storage with high-performance Channel-based processing
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="settings">TimescaleDB connection settings</param>
    public TimescaleStorage(ILogger<TimescaleStorage> logger, TimescaleSettings settings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        _connectionString = _settings.GetConnectionString();

        // Prepare SQL statements
        _insertSql = $"""
            INSERT INTO {_settings.TableName} 
            (timestamp, device_id, channel, raw_value, processed_value, rate, quality, unit)
            VALUES ($1, $2, $3, $4, $5, $6, $7, $8)
            ON CONFLICT (timestamp, device_id, channel) DO UPDATE SET
                raw_value = EXCLUDED.raw_value,
                processed_value = EXCLUDED.processed_value,
                rate = EXCLUDED.rate,
                quality = EXCLUDED.quality,
                unit = EXCLUDED.unit
            """;

        // Setup Channel for high-throughput async processing
        var channelOptions = new BoundedChannelOptions(_settings.BatchSize * 10)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        };

        _writeChannel = Channel.CreateBounded<DeviceReading>(channelOptions);
        _writer = _writeChannel.Writer;

        // Initialize database schema
        InitializeDatabaseAsync().GetAwaiter().GetResult();

        // Start background writer task
        _backgroundWriteTask = Task.Run(ProcessWritesAsync, _backgroundCts.Token);

        _logger.LogInformation(
            "TimescaleDB storage initialized for {Host}:{Port}/{Database}, table={TableName} with Channel-based processing",
            _settings.Host, _settings.Port, _settings.Database, _settings.TableName);
    }

    /// <summary>
    /// Write a single reading to TimescaleDB
    /// </summary>
    public async Task WriteReadingAsync(DeviceReading reading, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(TimescaleStorage));

        // Use Channel for high-performance async writes
        await _writer.WriteAsync(reading, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Write multiple readings in a batch
    /// </summary>
    public async Task WriteBatchAsync(IEnumerable<DeviceReading> readings, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(TimescaleStorage));

        // Use Channel for all writes to maintain consistency and performance
        foreach (var reading in readings)
        {
            await _writer.WriteAsync(reading, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Test connection to TimescaleDB
    /// </summary>
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            // Test basic query
            using var command = new NpgsqlCommand("SELECT 1", connection);
            var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

            var isHealthy = result?.ToString() == "1";

            if (isHealthy)
            {
                _logger.LogInformation("TimescaleDB connection test successful");
            }
            else
            {
                _logger.LogWarning("TimescaleDB connection test failed - unexpected result");
            }

            return isHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TimescaleDB connection test failed");
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

    /// <summary>
    /// Get the current health status of the storage subsystem
    /// </summary>
    public StorageHealthStatus GetHealthStatus()
    {
        lock (_healthLock)
        {
            return new StorageHealthStatus
            {
                IsBackgroundTaskHealthy = _isBackgroundTaskHealthy,
                LastSuccessfulWrite = _lastSuccessfulWrite,
                LastError = _lastError,
                PendingWrites = _writeChannel.Reader.CanCount ? _writeChannel.Reader.Count : 0
            };
        }
    }

    /// <summary>
    /// Initialize database schema and hypertable
    /// </summary>
    private async Task InitializeDatabaseAsync()
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync().ConfigureAwait(false);

            var sql = string.Format(_createTableSql, _settings.TableName);
            using var command = new NpgsqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync().ConfigureAwait(false);

            _logger.LogInformation("TimescaleDB schema initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize TimescaleDB schema");
            throw;
        }
    }

    /// <summary>
    /// Background task that processes writes from the Channel in batches
    /// </summary>
    private async Task ProcessWritesAsync()
    {
        var reader = _writeChannel.Reader;
        var batchList = new List<DeviceReading>(_settings.BatchSize);

        try
        {
            await foreach (var reading in reader.ReadAllAsync(_backgroundCts.Token))
            {
                batchList.Add(reading);

                // Write batch when full or when no more items are immediately available
                if (batchList.Count >= _settings.BatchSize || !reader.TryRead(out var nextReading))
                {
                    if (batchList.Count > 0)
                    {
                        await WriteBatchToTimescaleAsync(batchList, _backgroundCts.Token).ConfigureAwait(false);
                        batchList.Clear();
                    }
                }
                else
                {
                    // Add the next reading if we read one
                    batchList.Add(nextReading);
                }

                // Periodic flush based on time interval
                if (batchList.Count > 0)
                {
                    await Task.Delay(_settings.FlushIntervalMs, _backgroundCts.Token);
                    if (batchList.Count > 0)
                    {
                        await WriteBatchToTimescaleAsync(batchList, _backgroundCts.Token).ConfigureAwait(false);
                        batchList.Clear();
                    }
                }
            }

            // Final flush of any remaining readings
            if (batchList.Count > 0)
            {
                await WriteBatchToTimescaleAsync(batchList, CancellationToken.None).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in background write processing");

            // Update health status
            lock (_healthLock)
            {
                _isBackgroundTaskHealthy = false;
                _lastError = ex.Message;
            }
        }
    }

    /// <summary>
    /// Write a batch of readings to TimescaleDB with optimized bulk insert
    /// </summary>
    private async Task WriteBatchToTimescaleAsync(List<DeviceReading> readings, CancellationToken cancellationToken)
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            // Use COPY for optimal performance with large batches
            if (readings.Count > 10)
            {
                await WriteBatchUsingCopyAsync(connection, readings, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await WriteBatchUsingParametersAsync(connection, readings, cancellationToken).ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            // Update health status on successful write
            lock (_healthLock)
            {
                _isBackgroundTaskHealthy = true;
                _lastSuccessfulWrite = DateTimeOffset.UtcNow;
                _lastError = null;
            }

            _logger.LogDebug("Wrote batch of {Count} readings to TimescaleDB", readings.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write batch of {Count} readings to TimescaleDB", readings.Count);

            // Update health status on write failure
            lock (_healthLock)
            {
                _isBackgroundTaskHealthy = false;
                _lastError = ex.Message;
            }

            throw;
        }
    }

    /// <summary>
    /// Write batch using PostgreSQL COPY for maximum performance
    /// </summary>
    private async Task WriteBatchUsingCopyAsync(NpgsqlConnection connection, List<DeviceReading> readings, CancellationToken cancellationToken)
    {
        var copyCommand = $"COPY {_settings.TableName} (timestamp, device_id, channel, raw_value, processed_value, rate, quality, unit) FROM STDIN (FORMAT BINARY)";

        using var writer = await connection.BeginBinaryImportAsync(copyCommand, cancellationToken).ConfigureAwait(false);

        foreach (var reading in readings)
        {
            await writer.StartRowAsync(cancellationToken).ConfigureAwait(false);
            await writer.WriteAsync(reading.Timestamp.UtcDateTime, NpgsqlDbType.TimestampTz, cancellationToken).ConfigureAwait(false);
            await writer.WriteAsync(reading.DeviceId, NpgsqlDbType.Text, cancellationToken).ConfigureAwait(false);
            await writer.WriteAsync(reading.Channel, NpgsqlDbType.Integer, cancellationToken).ConfigureAwait(false);
            await writer.WriteAsync(reading.RawValue, NpgsqlDbType.Bigint, cancellationToken).ConfigureAwait(false);
            await writer.WriteAsync(reading.ProcessedValue, NpgsqlDbType.Double, cancellationToken).ConfigureAwait(false);
            await writer.WriteAsync(reading.Rate.HasValue ? (object)reading.Rate.Value : DBNull.Value, NpgsqlDbType.Double, cancellationToken).ConfigureAwait(false);
            await writer.WriteAsync(reading.Quality.ToString(), NpgsqlDbType.Text, cancellationToken).ConfigureAwait(false);
            await writer.WriteAsync(reading.Unit, NpgsqlDbType.Text, cancellationToken).ConfigureAwait(false);
        }

        await writer.CompleteAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Write batch using parameterized queries for smaller batches
    /// </summary>
    private async Task WriteBatchUsingParametersAsync(NpgsqlConnection connection, List<DeviceReading> readings, CancellationToken cancellationToken)
    {
        using var command = new NpgsqlCommand(_insertSql, connection);

        // Add parameters
        command.Parameters.Add("", NpgsqlDbType.TimestampTz);
        command.Parameters.Add("", NpgsqlDbType.Text);
        command.Parameters.Add("", NpgsqlDbType.Integer);
        command.Parameters.Add("", NpgsqlDbType.Bigint);
        command.Parameters.Add("", NpgsqlDbType.Double);
        command.Parameters.Add("", NpgsqlDbType.Double);
        command.Parameters.Add("", NpgsqlDbType.Text);
        command.Parameters.Add("", NpgsqlDbType.Text);

        foreach (var reading in readings)
        {
            command.Parameters[0].Value = reading.Timestamp.UtcDateTime;
            command.Parameters[1].Value = reading.DeviceId;
            command.Parameters[2].Value = reading.Channel;
            command.Parameters[3].Value = reading.RawValue;
            command.Parameters[4].Value = reading.ProcessedValue;
            command.Parameters[5].Value = reading.Rate.HasValue ? reading.Rate.Value : DBNull.Value;
            command.Parameters[6].Value = reading.Quality.ToString();
            command.Parameters[7].Value = reading.Unit;

            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Dispose resources
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Complete the channel to stop accepting new writes
        _writer.Complete();

        // Cancel background processing
        _backgroundCts.Cancel();

        try
        {
            // Wait for background task to complete
            _backgroundWriteTask.Wait(TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error waiting for background write task to complete during disposal");
        }

        // Dispose resources
        _backgroundCts.Dispose();
        _backgroundWriteTask.Dispose();

        _logger.LogInformation("TimescaleDB storage disposed");
    }
}
