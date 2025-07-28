using Industrial.Adam.Logger.Core.Devices;
using Industrial.Adam.Logger.Core.Models;
using Industrial.Adam.Logger.Core.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

namespace Industrial.Adam.Logger.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DataController : ControllerBase
{
    private readonly ModbusDevicePool _devicePool;
    private readonly AdamLoggerService _loggerService;
    private readonly ILogger<DataController> _logger;
    private static readonly ConcurrentDictionary<string, DeviceReading> _latestReadings = new();
    
    public DataController(
        ModbusDevicePool devicePool,
        AdamLoggerService loggerService,
        ILogger<DataController> logger)
    {
        _devicePool = devicePool;
        _loggerService = loggerService;
        _logger = logger;
        
        // Subscribe to device readings
        _devicePool.ReadingReceived += OnReadingReceived;
    }
    
    private void OnReadingReceived(DeviceReading reading)
    {
        _latestReadings.AddOrUpdate(
            $"{reading.DeviceId}:{reading.ChannelNumber}",
            reading,
            (key, existing) => reading);
    }
    
    /// <summary>
    /// Get latest readings for all devices
    /// </summary>
    [HttpGet("latest")]
    public ActionResult<IEnumerable<DeviceReading>> GetLatestReadings()
    {
        return Ok(_latestReadings.Values.OrderBy(r => r.DeviceId).ThenBy(r => r.ChannelNumber));
    }
    
    /// <summary>
    /// Get latest readings for a specific device
    /// </summary>
    [HttpGet("latest/{deviceId}")]
    public ActionResult<IEnumerable<DeviceReading>> GetDeviceLatestReadings(string deviceId)
    {
        var readings = _latestReadings.Values
            .Where(r => r.DeviceId == deviceId)
            .OrderBy(r => r.ChannelNumber);
        
        if (!readings.Any())
        {
            return NotFound($"No readings found for device {deviceId}");
        }
        
        return Ok(readings);
    }
    
    /// <summary>
    /// Get latest reading for a specific channel
    /// </summary>
    [HttpGet("latest/{deviceId}/{channelNumber}")]
    public ActionResult<DeviceReading> GetChannelLatestReading(string deviceId, int channelNumber)
    {
        var key = $"{deviceId}:{channelNumber}";
        if (_latestReadings.TryGetValue(key, out var reading))
        {
            return Ok(reading);
        }
        
        return NotFound($"No reading found for device {deviceId} channel {channelNumber}");
    }
    
    /// <summary>
    /// Clear cached readings
    /// </summary>
    [HttpDelete("cache")]
    public ActionResult ClearCache()
    {
        _latestReadings.Clear();
        _logger.LogInformation("Reading cache cleared");
        return NoContent();
    }
    
    /// <summary>
    /// Get summary statistics
    /// </summary>
    [HttpGet("stats")]
    public ActionResult<object> GetStatistics()
    {
        var now = DateTimeOffset.UtcNow;
        var stats = _latestReadings.Values
            .GroupBy(r => r.DeviceId)
            .Select(g => new
            {
                DeviceId = g.Key,
                ChannelCount = g.Count(),
                OldestReading = g.Min(r => r.Timestamp),
                NewestReading = g.Max(r => r.Timestamp),
                AverageRate = g.Average(r => r.Rate ?? 0)
            });
        
        return Ok(new
        {
            TotalDevices = stats.Count(),
            TotalChannels = _latestReadings.Count,
            Statistics = stats
        });
    }
}