namespace Industrial.Adam.Oee.Domain.ValueObjects;

/// <summary>
/// Represents a counter reading from the time series data
/// Immutable value object for counter data from Industrial.Adam.Logger
/// </summary>
public class CounterReadingValue : ValueObject
{
    /// <summary>
    /// Device identifier
    /// </summary>
    public string DeviceId { get; init; } = string.Empty;

    /// <summary>
    /// Counter channel number
    /// </summary>
    public int Channel { get; init; }

    /// <summary>
    /// Timestamp of the reading
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Processed counter value
    /// </summary>
    public decimal ProcessedValue { get; init; }

    /// <summary>
    /// Rate calculation (counts per minute)
    /// </summary>
    public decimal Rate { get; init; }

    /// <summary>
    /// Quality indicator
    /// </summary>
    public decimal Quality { get; init; }

    /// <summary>
    /// Private constructor for ORM
    /// </summary>
    private CounterReadingValue() { }

    /// <summary>
    /// Create a counter reading
    /// </summary>
    public CounterReadingValue(string deviceId, int channel, DateTime timestamp,
        decimal processedValue, decimal rate, decimal quality)
    {
        DeviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
        Channel = channel;
        Timestamp = timestamp;
        ProcessedValue = processedValue;
        Rate = rate;
        Quality = quality;
    }

    /// <summary>
    /// Get equality components for value object comparison
    /// </summary>
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DeviceId;
        yield return Channel;
        yield return Timestamp;
        yield return ProcessedValue;
        yield return Rate;
        yield return Quality;
    }
}
