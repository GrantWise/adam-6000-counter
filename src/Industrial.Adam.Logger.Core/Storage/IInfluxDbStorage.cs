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
    Task WriteReadingAsync(DeviceReading reading, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Write multiple readings in a batch
    /// </summary>
    Task WriteBatchAsync(IEnumerable<DeviceReading> readings, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Test connection to InfluxDB
    /// </summary>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Flush any pending writes
    /// </summary>
    Task FlushAsync(CancellationToken cancellationToken = default);
}