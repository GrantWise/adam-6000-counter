using Industrial.Adam.Logger.Configuration;
using Industrial.Adam.Logger.WebApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace Industrial.Adam.Logger.WebApi.Controllers;

/// <summary>
/// Controller for managing ADAM counter devices
/// </summary>
[ApiController]
[Route("api/devices")]
[Produces("application/json")]
public class DeviceController : ControllerBase
{
    private readonly IDeviceOrchestrator _orchestrator;
    private readonly ILogger<DeviceController> _logger;

    public DeviceController(IDeviceOrchestrator orchestrator, ILogger<DeviceController> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    /// <summary>
    /// Get all configured devices with their current status
    /// </summary>
    /// <returns>List of devices with status information</returns>
    /// <response code="200">Returns the list of devices</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<DeviceWithStatus>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllDevices()
    {
        var result = await _orchestrator.GetAllDevicesAsync();
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        _logger.LogError("Failed to get devices: {Error}", result.ErrorMessage);
        return StatusCode(500, new { error = result.ErrorMessage });
    }

    /// <summary>
    /// Get a specific device by ID
    /// </summary>
    /// <param name="id">Device ID</param>
    /// <returns>Device details with status</returns>
    /// <response code="200">Returns the device details</response>
    /// <response code="404">If the device is not found</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(DeviceWithStatus), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDevice(string id)
    {
        var result = await _orchestrator.GetDeviceByIdAsync(id);
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        if (result.ErrorMessage?.Contains("not found") == true)
        {
            return NotFound(new { error = result.ErrorMessage });
        }

        _logger.LogError("Failed to get device {DeviceId}: {Error}", id, result.ErrorMessage);
        return StatusCode(500, new { error = result.ErrorMessage });
    }

    /// <summary>
    /// Create a new device configuration
    /// </summary>
    /// <param name="config">Device configuration</param>
    /// <returns>Created device with status</returns>
    /// <response code="201">Returns the created device</response>
    /// <response code="400">If the configuration is invalid</response>
    /// <response code="409">If a device with the same ID already exists</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpPost]
    [ProducesResponseType(typeof(DeviceWithStatus), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateDevice([FromBody] AdamDeviceConfig config)
    {
        var result = await _orchestrator.CreateDeviceAsync(config);
        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetDevice), new { id = config.DeviceId }, result.Value);
        }

        if (result.ErrorMessage?.Contains("already exists") == true)
        {
            return Conflict(new { error = result.ErrorMessage });
        }

        if (result.ErrorMessage?.Contains("validation", StringComparison.OrdinalIgnoreCase) == true)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }

        _logger.LogError("Failed to create device {DeviceId}: {Error}", config.DeviceId, result.ErrorMessage);
        return StatusCode(500, new { error = result.ErrorMessage });
    }

    /// <summary>
    /// Update an existing device configuration
    /// </summary>
    /// <param name="id">Device ID</param>
    /// <param name="config">Updated device configuration</param>
    /// <returns>Updated device with status</returns>
    /// <response code="200">Returns the updated device</response>
    /// <response code="400">If the configuration is invalid</response>
    /// <response code="404">If the device is not found</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(DeviceWithStatus), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateDevice(string id, [FromBody] AdamDeviceConfig config)
    {
        var result = await _orchestrator.UpdateDeviceAsync(id, config);
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        if (result.ErrorMessage?.Contains("not found") == true)
        {
            return NotFound(new { error = result.ErrorMessage });
        }

        if (result.ErrorMessage?.Contains("validation", StringComparison.OrdinalIgnoreCase) == true ||
            result.ErrorMessage?.Contains("does not match") == true)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }

        _logger.LogError("Failed to update device {DeviceId}: {Error}", id, result.ErrorMessage);
        return StatusCode(500, new { error = result.ErrorMessage });
    }

    /// <summary>
    /// Delete a device configuration
    /// </summary>
    /// <param name="id">Device ID</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Device deleted successfully</response>
    /// <response code="404">If the device is not found</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteDevice(string id)
    {
        var result = await _orchestrator.DeleteDeviceAsync(id);
        if (result.IsSuccess)
        {
            return NoContent();
        }

        if (result.ErrorMessage?.Contains("not found") == true)
        {
            return NotFound(new { error = result.ErrorMessage });
        }

        _logger.LogError("Failed to delete device {DeviceId}: {Error}", id, result.ErrorMessage);
        return StatusCode(500, new { error = result.ErrorMessage });
    }

    /// <summary>
    /// Test device connection
    /// </summary>
    /// <param name="id">Device ID</param>
    /// <returns>Connection test results</returns>
    /// <response code="200">Returns test results</response>
    /// <response code="404">If the device is not found</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpPost("{id}/test")]
    [ProducesResponseType(typeof(ConnectionTestResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> TestConnection(string id)
    {
        var result = await _orchestrator.TestDeviceConnectionAsync(id);
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        if (result.ErrorMessage?.Contains("not found") == true)
        {
            return NotFound(new { error = result.ErrorMessage });
        }

        _logger.LogError("Failed to test device {DeviceId}: {Error}", id, result.ErrorMessage);
        return StatusCode(500, new { error = result.ErrorMessage });
    }

    /// <summary>
    /// Enable a device
    /// </summary>
    /// <param name="id">Device ID</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Device enabled successfully</response>
    /// <response code="404">If the device is not found</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpPost("{id}/enable")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> EnableDevice(string id)
    {
        var result = await _orchestrator.SetDeviceEnabledAsync(id, true);
        if (result.IsSuccess)
        {
            return NoContent();
        }

        if (result.ErrorMessage?.Contains("not found") == true)
        {
            return NotFound(new { error = result.ErrorMessage });
        }

        _logger.LogError("Failed to enable device {DeviceId}: {Error}", id, result.ErrorMessage);
        return StatusCode(500, new { error = result.ErrorMessage });
    }

    /// <summary>
    /// Disable a device
    /// </summary>
    /// <param name="id">Device ID</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Device disabled successfully</response>
    /// <response code="404">If the device is not found</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpPost("{id}/disable")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DisableDevice(string id)
    {
        var result = await _orchestrator.SetDeviceEnabledAsync(id, false);
        if (result.IsSuccess)
        {
            return NoContent();
        }

        if (result.ErrorMessage?.Contains("not found") == true)
        {
            return NotFound(new { error = result.ErrorMessage });
        }

        _logger.LogError("Failed to disable device {DeviceId}: {Error}", id, result.ErrorMessage);
        return StatusCode(500, new { error = result.ErrorMessage });
    }

    /// <summary>
    /// Get channels for a specific device
    /// </summary>
    /// <param name="id">Device ID</param>
    /// <returns>List of channels</returns>
    /// <response code="200">Returns the channels</response>
    /// <response code="404">If the device is not found</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet("{id}/channels")]
    [ProducesResponseType(typeof(IEnumerable<ChannelConfig>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetChannels(string id)
    {
        var result = await _orchestrator.GetDeviceByIdAsync(id);
        if (result.IsSuccess)
        {
            return Ok(result.Value.Config.Channels);
        }

        if (result.ErrorMessage?.Contains("not found") == true)
        {
            return NotFound(new { error = result.ErrorMessage });
        }

        _logger.LogError("Failed to get channels for device {DeviceId}: {Error}", id, result.ErrorMessage);
        return StatusCode(500, new { error = result.ErrorMessage });
    }

    /// <summary>
    /// Update all channels for a device
    /// </summary>
    /// <param name="id">Device ID</param>
    /// <param name="channels">Updated channel configurations</param>
    /// <returns>Updated device with status</returns>
    /// <response code="200">Returns the updated device</response>
    /// <response code="400">If the channel configuration is invalid</response>
    /// <response code="404">If the device is not found</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpPut("{id}/channels")]
    [ProducesResponseType(typeof(DeviceWithStatus), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateChannels(string id, [FromBody] List<ChannelConfig> channels)
    {
        var deviceResult = await _orchestrator.GetDeviceByIdAsync(id);
        if (!deviceResult.IsSuccess)
        {
            if (deviceResult.ErrorMessage?.Contains("not found") == true)
            {
                return NotFound(new { error = deviceResult.ErrorMessage });
            }
            return StatusCode(500, new { error = deviceResult.ErrorMessage });
        }

        var device = deviceResult.Value.Config;
        device.Channels = channels;

        var updateResult = await _orchestrator.UpdateDeviceAsync(id, device);
        if (updateResult.IsSuccess)
        {
            return Ok(updateResult.Value);
        }

        if (updateResult.ErrorMessage?.Contains("validation", StringComparison.OrdinalIgnoreCase) == true)
        {
            return BadRequest(new { error = updateResult.ErrorMessage });
        }

        _logger.LogError("Failed to update channels for device {DeviceId}: {Error}", id, updateResult.ErrorMessage);
        return StatusCode(500, new { error = updateResult.ErrorMessage });
    }

    /// <summary>
    /// Update a single channel for a device
    /// </summary>
    /// <param name="id">Device ID</param>
    /// <param name="channelId">Channel number</param>
    /// <param name="channel">Updated channel configuration</param>
    /// <returns>Updated device with status</returns>
    /// <response code="200">Returns the updated device</response>
    /// <response code="400">If the channel configuration is invalid</response>
    /// <response code="404">If the device or channel is not found</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpPut("{id}/channels/{channelId}")]
    [ProducesResponseType(typeof(DeviceWithStatus), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateChannel(string id, int channelId, [FromBody] ChannelConfig channel)
    {
        var deviceResult = await _orchestrator.GetDeviceByIdAsync(id);
        if (!deviceResult.IsSuccess)
        {
            if (deviceResult.ErrorMessage?.Contains("not found") == true)
            {
                return NotFound(new { error = deviceResult.ErrorMessage });
            }
            return StatusCode(500, new { error = deviceResult.ErrorMessage });
        }

        var device = deviceResult.Value.Config;
        var existingChannel = device.Channels.FirstOrDefault(c => c.ChannelNumber == channelId);
        if (existingChannel == null)
        {
            return NotFound(new { error = $"Channel {channelId} not found on device {id}" });
        }

        // Ensure channel number matches
        channel.ChannelNumber = channelId;

        // Update the channel
        var index = device.Channels.IndexOf(existingChannel);
        device.Channels[index] = channel;

        var updateResult = await _orchestrator.UpdateDeviceAsync(id, device);
        if (updateResult.IsSuccess)
        {
            return Ok(updateResult.Value);
        }

        if (updateResult.ErrorMessage?.Contains("validation", StringComparison.OrdinalIgnoreCase) == true)
        {
            return BadRequest(new { error = updateResult.ErrorMessage });
        }

        _logger.LogError("Failed to update channel {ChannelId} for device {DeviceId}: {Error}", 
            channelId, id, updateResult.ErrorMessage);
        return StatusCode(500, new { error = updateResult.ErrorMessage });
    }
}