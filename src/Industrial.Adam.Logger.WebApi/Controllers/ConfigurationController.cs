using Industrial.Adam.Logger.Configuration;
using Industrial.Adam.Logger.WebApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Industrial.Adam.Logger.WebApi.Controllers;

/// <summary>
/// Controller for managing global ADAM logger configuration
/// </summary>
[ApiController]
[Route("api/config")]
[Produces("application/json")]
public class ConfigurationController : ControllerBase
{
    private readonly IDeviceOrchestrator _orchestrator;
    private readonly ILogger<ConfigurationController> _logger;

    public ConfigurationController(IDeviceOrchestrator orchestrator, ILogger<ConfigurationController> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    /// <summary>
    /// Get current global configuration
    /// </summary>
    /// <returns>Current configuration</returns>
    /// <response code="200">Returns the configuration</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet]
    [ProducesResponseType(typeof(AdamLoggerConfig), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult GetConfiguration()
    {
        var result = _orchestrator.GetConfiguration();
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        _logger.LogError("Failed to get configuration: {Error}", result.ErrorMessage);
        return StatusCode(500, new { error = result.ErrorMessage });
    }

    /// <summary>
    /// Update global configuration
    /// </summary>
    /// <param name="config">Updated configuration</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Configuration updated successfully</response>
    /// <response code="400">If the configuration is invalid</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateConfiguration([FromBody] AdamLoggerConfig config)
    {
        var result = await _orchestrator.UpdateConfigurationAsync(config);
        if (result.IsSuccess)
        {
            return NoContent();
        }

        if (result.ErrorMessage?.Contains("validation", StringComparison.OrdinalIgnoreCase) == true)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }

        _logger.LogError("Failed to update configuration: {Error}", result.ErrorMessage);
        return StatusCode(500, new { error = result.ErrorMessage });
    }

    /// <summary>
    /// Validate configuration without applying it
    /// </summary>
    /// <param name="config">Configuration to validate</param>
    /// <returns>Validation results</returns>
    /// <response code="200">Returns validation results</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(ValidationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult ValidateConfiguration([FromBody] AdamLoggerConfig config)
    {
        var result = _orchestrator.ValidateConfiguration(config);
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        _logger.LogError("Failed to validate configuration: {Error}", result.ErrorMessage);
        return StatusCode(500, new { error = result.ErrorMessage });
    }

    /// <summary>
    /// Export current configuration as JSON file
    /// </summary>
    /// <returns>Configuration JSON file</returns>
    /// <response code="200">Returns configuration as JSON file</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet("export")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult ExportConfiguration()
    {
        var result = _orchestrator.GetConfiguration();
        if (!result.IsSuccess)
        {
            _logger.LogError("Failed to export configuration: {Error}", result.ErrorMessage);
            return StatusCode(500, new { error = result.ErrorMessage });
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(result.Value, options);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        
        var fileName = $"adam-logger-config-{DateTime.UtcNow:yyyy-MM-dd-HHmmss}.json";
        return File(bytes, "application/json", fileName);
    }

    /// <summary>
    /// Import configuration from JSON file
    /// </summary>
    /// <param name="file">Configuration JSON file</param>
    /// <returns>Validation results</returns>
    /// <response code="200">Configuration imported and validated</response>
    /// <response code="400">If the file or configuration is invalid</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ImportConfiguration(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "No file uploaded" });
        }

        if (!file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "File must be a JSON file" });
        }

        try
        {
            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var config = JsonSerializer.Deserialize<AdamLoggerConfig>(json, options);
            if (config == null)
            {
                return BadRequest(new { error = "Invalid configuration format" });
            }

            var validationResult = _orchestrator.ValidateConfiguration(config);
            if (!validationResult.IsSuccess)
            {
                return StatusCode(500, new { error = validationResult.ErrorMessage });
            }

            var importResult = new ImportResult
            {
                FileName = file.FileName,
                FileSize = file.Length,
                ValidationResult = validationResult.Value,
                DeviceCount = config.Devices.Count,
                TotalChannelCount = config.Devices.Sum(d => d.Channels.Count)
            };

            // If validation passed and user wants to apply, they would call PUT /api/config
            // with the validated configuration

            return Ok(importResult);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse configuration file");
            return BadRequest(new { error = $"Invalid JSON format: {ex.Message}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import configuration");
            return StatusCode(500, new { error = $"Failed to import configuration: {ex.Message}" });
        }
    }
}

/// <summary>
/// Result of configuration import
/// </summary>
public class ImportResult
{
    /// <summary>
    /// Uploaded file name
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Validation results
    /// </summary>
    public ValidationResult ValidationResult { get; set; } = null!;

    /// <summary>
    /// Number of devices in configuration
    /// </summary>
    public int DeviceCount { get; set; }

    /// <summary>
    /// Total number of channels across all devices
    /// </summary>
    public int TotalChannelCount { get; set; }
}