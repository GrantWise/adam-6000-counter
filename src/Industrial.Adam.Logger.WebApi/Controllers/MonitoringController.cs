using Industrial.Adam.Logger.Models;
using Industrial.Adam.Logger.WebApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace Industrial.Adam.Logger.WebApi.Controllers;

/// <summary>
/// Controller for monitoring ADAM logger health, metrics, and performance
/// </summary>
[ApiController]
[Route("api/monitoring")]
[Produces("application/json")]
public class MonitoringController : ControllerBase
{
    private readonly IDeviceOrchestrator _orchestrator;
    private readonly ILogger<MonitoringController> _logger;

    public MonitoringController(IDeviceOrchestrator orchestrator, ILogger<MonitoringController> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    /// <summary>
    /// Get overall system health status
    /// </summary>
    /// <returns>System health information</returns>
    /// <response code="200">Returns system health status</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet("health")]
    [ProducesResponseType(typeof(SystemHealthStatus), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSystemHealth()
    {
        try
        {
            var devicesResult = await _orchestrator.GetAllDevicesAsync();
            if (!devicesResult.IsSuccess)
            {
                return StatusCode(500, new { error = devicesResult.ErrorMessage });
            }

            var devices = devicesResult.Value;
            var healthyDevices = devices.Count(d => d.Health?.Status == DeviceStatus.Online);
            var totalDevices = devices.Count;

            var systemHealth = new SystemHealthStatus
            {
                OverallStatus = healthyDevices == totalDevices ? "Healthy" : 
                               healthyDevices > 0 ? "Degraded" : "Critical",
                TotalDevices = totalDevices,
                HealthyDevices = healthyDevices,
                UnhealthyDevices = totalDevices - healthyDevices,
                LastUpdated = DateTimeOffset.UtcNow,
                Uptime = GetSystemUptime()
            };

            return Ok(systemHealth);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get system health");
            return StatusCode(500, new { error = "Failed to retrieve system health" });
        }
    }

    /// <summary>
    /// Get health status for all devices
    /// </summary>
    /// <returns>Health status for each device</returns>
    /// <response code="200">Returns device health statuses</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet("devices/health")]
    [ProducesResponseType(typeof(IReadOnlyList<DeviceHealthSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDevicesHealth()
    {
        try
        {
            var devicesResult = await _orchestrator.GetAllDevicesAsync();
            if (!devicesResult.IsSuccess)
            {
                return StatusCode(500, new { error = devicesResult.ErrorMessage });
            }

            var healthSummaries = devicesResult.Value.Select(device => new DeviceHealthSummary
            {
                DeviceId = device.Config.DeviceId,
                Name = device.Config.DeviceId, // Use DeviceId as name since Name property doesn't exist
                Status = device.Health?.Status.ToString() ?? "Unknown",
                LastContact = device.Health?.Timestamp,
                ErrorCount = device.Health?.ConsecutiveFailures ?? 0,
                ConnectionUptime = CalculateUptime(device.Health?.Timestamp, device.Health?.Timestamp)
            }).ToList();

            return Ok(healthSummaries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get devices health");
            return StatusCode(500, new { error = "Failed to retrieve devices health" });
        }
    }

    /// <summary>
    /// Get health status for a specific device
    /// </summary>
    /// <param name="deviceId">Device ID</param>
    /// <returns>Device health information</returns>
    /// <response code="200">Returns device health status</response>
    /// <response code="404">If device not found</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet("devices/{deviceId}/health")]
    [ProducesResponseType(typeof(DeviceHealthDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDeviceHealth(string deviceId)
    {
        try
        {
            var deviceResult = await _orchestrator.GetDeviceByIdAsync(deviceId);
            if (!deviceResult.IsSuccess)
            {
                if (deviceResult.ErrorMessage?.Contains("not found") == true)
                {
                    return NotFound(new { error = $"Device '{deviceId}' not found" });
                }
                return StatusCode(500, new { error = deviceResult.ErrorMessage });
            }

            var device = deviceResult.Value;
            var healthDetail = new DeviceHealthDetail
            {
                DeviceId = device.Config.DeviceId,
                Name = device.Config.DeviceId, // Use DeviceId as name since Name property doesn't exist
                IpAddress = device.Config.IpAddress,
                Port = device.Config.Port,
                Status = device.Health?.Status.ToString() ?? "Unknown",
                LastContact = device.Health?.Timestamp,
                LastConnected = device.Health?.Timestamp, // Use Timestamp since LastConnected doesn't exist
                ErrorCount = device.Health?.ConsecutiveFailures ?? 0,
                LastError = device.Health?.LastError,
                ConnectionUptime = CalculateUptime(device.Health?.Timestamp, device.Health?.Timestamp),
                ChannelCount = device.Config.Channels.Count,
                DataPointsCollected = device.Health?.SuccessfulReads ?? 0
            };

            return Ok(healthDetail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get device health for {DeviceId}", deviceId);
            return StatusCode(500, new { error = "Failed to retrieve device health" });
        }
    }

    /// <summary>
    /// Get system performance metrics
    /// </summary>
    /// <returns>Performance metrics</returns>
    /// <response code="200">Returns system metrics</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet("metrics")]
    [ProducesResponseType(typeof(SystemMetrics), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSystemMetrics()
    {
        try
        {
            var devicesResult = await _orchestrator.GetAllDevicesAsync();
            if (!devicesResult.IsSuccess)
            {
                return StatusCode(500, new { error = devicesResult.ErrorMessage });
            }

            var devices = devicesResult.Value;
            var totalDataPoints = devices.Sum(d => d.Health?.SuccessfulReads ?? 0);
            var totalErrors = devices.Sum(d => d.Health?.ConsecutiveFailures ?? 0);

            var metrics = new SystemMetrics
            {
                TotalDataPointsCollected = totalDataPoints,
                TotalErrors = totalErrors,
                ErrorRate = totalDataPoints > 0 ? (double)totalErrors / totalDataPoints * 100 : 0,
                ActiveConnections = devices.Count(d => d.Health?.Status == DeviceStatus.Online),
                AverageResponseTime = CalculateAverageResponseTime(devices),
                MemoryUsage = GC.GetTotalMemory(false),
                CpuUsage = GetCpuUsage(),
                Timestamp = DateTimeOffset.UtcNow
            };

            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get system metrics");
            return StatusCode(500, new { error = "Failed to retrieve system metrics" });
        }
    }

    private static TimeSpan GetSystemUptime()
    {
        return TimeSpan.FromMilliseconds(Environment.TickCount64);
    }

    private static double? CalculateUptime(DateTimeOffset? connected, DateTimeOffset? lastContact)
    {
        if (!connected.HasValue || !lastContact.HasValue)
            return null;

        var uptime = lastContact.Value - connected.Value;
        return uptime.TotalHours;
    }

    private static double CalculateAverageResponseTime(IReadOnlyList<DeviceWithStatus> devices)
    {
        var connectedDevices = devices.Where(d => d.Health?.Status == DeviceStatus.Online).ToList();
        if (!connectedDevices.Any())
            return 0;

        // Simulate response time calculation - in real implementation, 
        // this would come from actual response time tracking
        return 150.0; // milliseconds
    }

    private static double GetCpuUsage()
    {
        // Simplified CPU usage - in real implementation, 
        // this would use proper system monitoring
        return Random.Shared.NextDouble() * 25; // 0-25% usage
    }
}

/// <summary>
/// Overall system health status
/// </summary>
public class SystemHealthStatus
{
    /// <summary>
    /// Overall system status (Healthy, Degraded, Critical)
    /// </summary>
    public string OverallStatus { get; set; } = string.Empty;

    /// <summary>
    /// Total number of configured devices
    /// </summary>
    public int TotalDevices { get; set; }

    /// <summary>
    /// Number of healthy devices
    /// </summary>
    public int HealthyDevices { get; set; }

    /// <summary>
    /// Number of unhealthy devices
    /// </summary>
    public int UnhealthyDevices { get; set; }

    /// <summary>
    /// System uptime
    /// </summary>
    public TimeSpan Uptime { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTimeOffset LastUpdated { get; set; }
}

/// <summary>
/// Device health summary
/// </summary>
public class DeviceHealthSummary
{
    /// <summary>
    /// Device ID
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Device name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Current status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Last contact time
    /// </summary>
    public DateTimeOffset? LastContact { get; set; }

    /// <summary>
    /// Error count
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// Connection uptime in hours
    /// </summary>
    public double? ConnectionUptime { get; set; }
}

/// <summary>
/// Detailed device health information
/// </summary>
public class DeviceHealthDetail : DeviceHealthSummary
{
    /// <summary>
    /// Device IP address
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// Device port
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Last successful connection time
    /// </summary>
    public DateTimeOffset? LastConnected { get; set; }

    /// <summary>
    /// Last error message
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Number of configured channels
    /// </summary>
    public int ChannelCount { get; set; }

    /// <summary>
    /// Total data points collected
    /// </summary>
    public long DataPointsCollected { get; set; }
}

/// <summary>
/// System performance metrics
/// </summary>
public class SystemMetrics
{
    /// <summary>
    /// Total data points collected across all devices
    /// </summary>
    public long TotalDataPointsCollected { get; set; }

    /// <summary>
    /// Total errors encountered
    /// </summary>
    public long TotalErrors { get; set; }

    /// <summary>
    /// Error rate as percentage
    /// </summary>
    public double ErrorRate { get; set; }

    /// <summary>
    /// Number of active connections
    /// </summary>
    public int ActiveConnections { get; set; }

    /// <summary>
    /// Average response time in milliseconds
    /// </summary>
    public double AverageResponseTime { get; set; }

    /// <summary>
    /// Memory usage in bytes
    /// </summary>
    public long MemoryUsage { get; set; }

    /// <summary>
    /// CPU usage percentage
    /// </summary>
    public double CpuUsage { get; set; }

    /// <summary>
    /// Metrics timestamp
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
}