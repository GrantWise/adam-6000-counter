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
}
