using Industrial.Adam.Logger.Configuration;
using Industrial.Adam.Logger.Models;
using Industrial.Adam.Logger.Utilities;

namespace Industrial.Adam.Logger.WebApi.Services;

/// <summary>
/// Orchestrates device management operations between the API and the core logger service
/// </summary>
public interface IDeviceOrchestrator
{
    /// <summary>
    /// Get all configured devices with their current status
    /// </summary>
    Task<OperationResult<IReadOnlyList<DeviceWithStatus>>> GetAllDevicesAsync();

    /// <summary>
    /// Get a specific device by ID
    /// </summary>
    Task<OperationResult<DeviceWithStatus>> GetDeviceByIdAsync(string deviceId);

    /// <summary>
    /// Create a new device configuration
    /// </summary>
    Task<OperationResult<DeviceWithStatus>> CreateDeviceAsync(AdamDeviceConfig config);

    /// <summary>
    /// Update an existing device configuration
    /// </summary>
    Task<OperationResult<DeviceWithStatus>> UpdateDeviceAsync(string deviceId, AdamDeviceConfig config);

    /// <summary>
    /// Delete a device configuration
    /// </summary>
    Task<OperationResult> DeleteDeviceAsync(string deviceId);

    /// <summary>
    /// Test device connection
    /// </summary>
    Task<OperationResult<ConnectionTestResult>> TestDeviceConnectionAsync(string deviceId);

    /// <summary>
    /// Enable or disable a device
    /// </summary>
    Task<OperationResult> SetDeviceEnabledAsync(string deviceId, bool enabled);

    /// <summary>
    /// Get current configuration
    /// </summary>
    OperationResult<AdamLoggerConfig> GetConfiguration();

    /// <summary>
    /// Update configuration
    /// </summary>
    Task<OperationResult> UpdateConfigurationAsync(AdamLoggerConfig config);

    /// <summary>
    /// Validate configuration
    /// </summary>
    OperationResult<ValidationResult> ValidateConfiguration(AdamLoggerConfig config);
}

/// <summary>
/// Device information with current status
/// </summary>
public record DeviceWithStatus
{
    /// <summary>
    /// Device configuration
    /// </summary>
    public required AdamDeviceConfig Config { get; init; }

    /// <summary>
    /// Current health status
    /// </summary>
    public AdamDeviceHealth? Health { get; init; }

    /// <summary>
    /// Connection status
    /// </summary>
    public string Status => Health?.Status.ToString() ?? "Unknown";

    /// <summary>
    /// Last successful communication
    /// </summary>
    public DateTimeOffset? LastContact => Health?.Timestamp;
}

/// <summary>
/// Result of a connection test
/// </summary>
public class ConnectionTestResult
{
    /// <summary>
    /// Whether the connection test succeeded
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Response time in milliseconds
    /// </summary>
    public double? ResponseTimeMs { get; set; }

    /// <summary>
    /// Error message if test failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Detailed test steps and results
    /// </summary>
    public List<TestStep> Steps { get; set; } = new();
}

/// <summary>
/// Individual step in a connection test
/// </summary>
public record TestStep
{
    /// <summary>
    /// Step name
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Whether this step succeeded
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Step duration in milliseconds
    /// </summary>
    public double DurationMs { get; init; }

    /// <summary>
    /// Step details or error message
    /// </summary>
    public string? Details { get; init; }
}

/// <summary>
/// Configuration validation result
/// </summary>
public record ValidationResult
{
    /// <summary>
    /// Whether the configuration is valid
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// Validation errors
    /// </summary>
    public List<ValidationError> Errors { get; init; } = new();
}

/// <summary>
/// Individual validation error
/// </summary>
public record ValidationError
{
    /// <summary>
    /// Property that failed validation
    /// </summary>
    public required string PropertyName { get; init; }

    /// <summary>
    /// Error message
    /// </summary>
    public required string ErrorMessage { get; init; }

    /// <summary>
    /// Error severity
    /// </summary>
    public ValidationSeverity Severity { get; init; } = ValidationSeverity.Error;
}

/// <summary>
/// Validation error severity
/// </summary>
public enum ValidationSeverity
{
    /// <summary>
    /// Informational message
    /// </summary>
    Info,

    /// <summary>
    /// Warning that should be addressed
    /// </summary>
    Warning,

    /// <summary>
    /// Error that must be fixed
    /// </summary>
    Error
}