using Industrial.Adam.Logger.Core.Configuration;
using Industrial.Adam.Logger.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace Industrial.Adam.Logger.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DeviceController : ControllerBase
{
    private readonly AdamLoggerService _loggerService;
    private readonly ILogger<DeviceController> _logger;
    
    public DeviceController(
        AdamLoggerService loggerService,
        ILogger<DeviceController> logger)
    {
        _loggerService = loggerService;
        _logger = logger;
    }
    
    /// <summary>
    /// Get all device status
    /// </summary>
    [HttpGet("status")]
    public ActionResult<ServiceStatus> GetStatus()
    {
        return Ok(_loggerService.GetStatus());
    }
    
    /// <summary>
    /// Get a specific device health
    /// </summary>
    [HttpGet("{deviceId}/health")]
    public ActionResult<DeviceHealth> GetDeviceHealth(string deviceId)
    {
        var status = _loggerService.GetStatus();
        if (status.DeviceHealth.TryGetValue(deviceId, out var health))
        {
            return Ok(health);
        }
        
        return NotFound($"Device {deviceId} not found");
    }
    
    /// <summary>
    /// Add a new device
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> AddDevice([FromBody] DeviceConfig config)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        try
        {
            var result = await _loggerService.AddDeviceAsync(config);
            if (result)
            {
                _logger.LogInformation("Device {DeviceId} added successfully", config.DeviceId);
                return CreatedAtAction(
                    nameof(GetDeviceHealth), 
                    new { deviceId = config.DeviceId }, 
                    config);
            }
            
            return Conflict($"Device {config.DeviceId} already exists");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add device {DeviceId}", config.DeviceId);
            return StatusCode(500, "Failed to add device");
        }
    }
    
    /// <summary>
    /// Remove a device
    /// </summary>
    [HttpDelete("{deviceId}")]
    public async Task<ActionResult> RemoveDevice(string deviceId)
    {
        try
        {
            var result = await _loggerService.RemoveDeviceAsync(deviceId);
            if (result)
            {
                _logger.LogInformation("Device {DeviceId} removed successfully", deviceId);
                return NoContent();
            }
            
            return NotFound($"Device {deviceId} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove device {DeviceId}", deviceId);
            return StatusCode(500, "Failed to remove device");
        }
    }
    
    /// <summary>
    /// Restart a device connection
    /// </summary>
    [HttpPost("{deviceId}/restart")]
    public async Task<ActionResult> RestartDevice(string deviceId)
    {
        try
        {
            var result = await _loggerService.RestartDeviceAsync(deviceId);
            if (result)
            {
                _logger.LogInformation("Device {DeviceId} restarted successfully", deviceId);
                return Ok(new { message = $"Device {deviceId} restarted successfully" });
            }
            
            return NotFound($"Device {deviceId} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restart device {DeviceId}", deviceId);
            return StatusCode(500, "Failed to restart device");
        }
    }
}