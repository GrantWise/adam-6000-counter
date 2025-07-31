using Industrial.Adam.Logger.Core.Configuration;
using Industrial.Adam.Logger.Core.Models;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Logger.Core.Processing;

/// <summary>
/// Processes device readings with counter overflow detection and rate calculations
/// </summary>
public sealed class DataProcessor : IDataProcessor
{
    private readonly ILogger<DataProcessor> _logger;
    private readonly Dictionary<string, ChannelConfig> _channelConfigs;
    
    // Counter limits for overflow detection
    private const long Counter16BitMax = 65535;
    private const long Counter32BitMax = 4294967295;
    
    public DataProcessor(ILogger<DataProcessor> logger, LoggerConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        
        // Build channel config lookup
        _channelConfigs = new Dictionary<string, ChannelConfig>();
        foreach (var device in configuration.Devices)
        {
            foreach (var channel in device.Channels)
            {
                var key = GetChannelKey(device.DeviceId, channel.ChannelNumber);
                _channelConfigs[key] = channel;
            }
        }
    }
    
    /// <summary>
    /// Process a raw device reading with overflow detection and rate calculation
    /// </summary>
    public DeviceReading ProcessReading(DeviceReading reading, DeviceReading? previousReading = null)
    {
        var channelKey = GetChannelKey(reading.DeviceId, reading.Channel);
        if (!_channelConfigs.TryGetValue(channelKey, out var channelConfig))
        {
            _logger.LogWarning(
                "No configuration found for device {DeviceId} channel {Channel}",
                reading.DeviceId, reading.Channel);
            return reading;
        }
        
        // Create a new reading with processed values
        var processed = reading with
        {
            ProcessedValue = reading.RawValue * channelConfig.ScaleFactor,
            Quality = DataQuality.Good
        };
        
        // Calculate rate if we have a previous reading
        if (previousReading != null)
        {
            processed = CalculateRate(processed, previousReading, channelConfig);
        }
        
        // Validate the processed reading
        if (!ValidateProcessedReading(processed, channelConfig))
        {
            // Only set to Bad if not already Degraded
            if (processed.Quality != DataQuality.Degraded)
            {
                processed = processed with { Quality = DataQuality.Bad };
            }
        }
        
        return processed;
    }
    
    /// <summary>
    /// Validate a processed reading against channel limits
    /// </summary>
    public bool ValidateReading(DeviceReading reading)
    {
        var channelKey = GetChannelKey(reading.DeviceId, reading.Channel);
        if (!_channelConfigs.TryGetValue(channelKey, out var channelConfig))
        {
            return false;
        }
        
        return ValidateProcessedReading(reading, channelConfig);
    }
    
    private DeviceReading CalculateRate(
        DeviceReading current,
        DeviceReading previous,
        ChannelConfig channelConfig)
    {
        var timeDiff = (current.Timestamp - previous.Timestamp).TotalSeconds;
        if (timeDiff <= 0)
        {
            _logger.LogWarning(
                "Invalid time difference for rate calculation: {TimeDiff}s",
                timeDiff);
            return current;
        }
        
        // Handle counter overflow
        long valueDiff = (long)current.RawValue - (long)previous.RawValue;
        
        // Detect overflow based on register count
        var maxValue = channelConfig.RegisterCount switch
        {
            1 => Counter16BitMax,
            2 => Counter32BitMax,
            _ => Counter32BitMax
        };
        
        // If the difference is negative and large, assume overflow
        if (valueDiff < 0 && Math.Abs(valueDiff) > (long)(maxValue / 2))
        {
            // Counter wrapped around
            valueDiff = (long)(maxValue + 1) + valueDiff;
            _logger.LogDebug(
                "Counter overflow detected for {DeviceId} channel {Channel}: " +
                "prev={Previous}, curr={Current}, adjusted diff={Diff}",
                current.DeviceId, current.Channel,
                previous.RawValue, current.RawValue, valueDiff);
        }
        
        // Calculate rate (units per second)
        var rate = valueDiff / timeDiff * channelConfig.ScaleFactor;
        
        // Apply rate limits if configured
        if (channelConfig.MaxChangeRate.HasValue && 
            Math.Abs(rate) > channelConfig.MaxChangeRate.Value)
        {
            _logger.LogWarning(
                "Rate {Rate} exceeds max change rate {MaxRate} for {DeviceId} channel {Channel}",
                rate, channelConfig.MaxChangeRate.Value, current.DeviceId, current.Channel);
            
            return current with 
            { 
                Rate = rate,
                Quality = DataQuality.Degraded
            };
        }
        
        return current with { Rate = rate };
    }
    
    private bool ValidateProcessedReading(DeviceReading reading, ChannelConfig channelConfig)
    {
        // Check min/max limits if configured
        if (channelConfig.MinValue.HasValue && reading.ProcessedValue < channelConfig.MinValue.Value)
        {
            _logger.LogWarning(
                "Reading {Value} below minimum {Min} for {DeviceId} channel {Channel}",
                reading.ProcessedValue, channelConfig.MinValue.Value,
                reading.DeviceId, reading.Channel);
            return false;
        }
        
        if (channelConfig.MaxValue.HasValue && reading.ProcessedValue > channelConfig.MaxValue.Value)
        {
            _logger.LogWarning(
                "Reading {Value} above maximum {Max} for {DeviceId} channel {Channel}",
                reading.ProcessedValue, channelConfig.MaxValue.Value,
                reading.DeviceId, reading.Channel);
            return false;
        }
        
        // Check rate limits if available
        if (reading.Rate.HasValue && channelConfig.MaxChangeRate.HasValue)
        {
            if (Math.Abs(reading.Rate.Value) > channelConfig.MaxChangeRate.Value)
            {
                return false;
            }
        }
        
        return true;
    }
    
    private static string GetChannelKey(string deviceId, int channel)
    {
        return $"{deviceId}:{channel}";
    }
}