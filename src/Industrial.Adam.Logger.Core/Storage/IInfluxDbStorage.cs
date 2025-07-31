using Industrial.Adam.Logger.Core.Models;

namespace Industrial.Adam.Logger.Core.Storage;

/// <summary>
/// Interface for storing device readings in InfluxDB
/// </summary>
public interface IInfluxDbStorage : IDisposable
{
    /// <summary>
    /// Write a single reading to InfluxDB
    /// </summary>
    public Task WriteReadingAsync(DeviceReading reading, CancellationToken cancellationToken = default);

    /// <summary>
    /// Write multiple readings in a batch
    /// </summary>
    public Task WriteBatchAsync(IEnumerable<DeviceReading> readings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Test connection to InfluxDB
    /// </summary>
    public Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Flush any pending writes
    /// </summary>
    public Task FlushAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the current health status of the storage background task
    /// </summary>
    public StorageHealthStatus GetHealthStatus();
}

/// <summary>
/// Health status of the storage subsystem
/// </summary>
public class StorageHealthStatus
{
    /// <summary>
    /// Whether the background write task is healthy
    /// </summary>
    public bool IsBackgroundTaskHealthy { get; init; }

    /// <summary>
    /// Timestamp of the last successful write batch
    /// </summary>
    public DateTimeOffset? LastSuccessfulWrite { get; init; }

    /// <summary>
    /// Last error message if any
    /// </summary>
    public string? LastError { get; init; }

    /// <summary>
    /// Number of pending writes in the queue
    /// </summary>
    public int PendingWrites { get; init; }
}
