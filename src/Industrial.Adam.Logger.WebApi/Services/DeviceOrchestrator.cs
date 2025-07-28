using Industrial.Adam.Logger.Configuration;
using Industrial.Adam.Logger.Infrastructure;
using Industrial.Adam.Logger.Interfaces;
using Industrial.Adam.Logger.Models;
using Industrial.Adam.Logger.Utilities;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Industrial.Adam.Logger.WebApi.Services;

/// <summary>
/// Orchestrates device management operations
/// </summary>
public class DeviceOrchestrator : IDeviceOrchestrator
{
    private readonly IAdamLoggerService _loggerService;
    private readonly IOptionsMonitor<AdamLoggerConfig> _configOptions;
    private readonly ILogger<DeviceOrchestrator> _logger;
    private readonly IServiceProvider _serviceProvider;

    public DeviceOrchestrator(
        IAdamLoggerService loggerService,
        IOptionsMonitor<AdamLoggerConfig> configOptions,
        ILogger<DeviceOrchestrator> logger,
        IServiceProvider serviceProvider)
    {
        _loggerService = loggerService;
        _configOptions = configOptions;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task<OperationResult<IReadOnlyList<DeviceWithStatus>>> GetAllDevicesAsync()
    {
        try
        {
            var config = _configOptions.CurrentValue;
            var healthStatuses = await _loggerService.GetAllDeviceHealthAsync();
            var healthMap = healthStatuses.ToDictionary(h => h.DeviceId);

            var devicesWithStatus = config.Devices.Select(device => new DeviceWithStatus
            {
                Config = device,
                Health = healthMap.GetValueOrDefault(device.DeviceId)
            }).ToList();

            return OperationResult<IReadOnlyList<DeviceWithStatus>>.Success(devicesWithStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all devices");
            return OperationResult<IReadOnlyList<DeviceWithStatus>>.Failure($"Failed to get devices: {ex.Message}");
        }
    }

    public async Task<OperationResult<DeviceWithStatus>> GetDeviceByIdAsync(string deviceId)
    {
        try
        {
            var config = _configOptions.CurrentValue;
            var device = config.Devices.FirstOrDefault(d => d.DeviceId == deviceId);
            
            if (device == null)
            {
                return OperationResult<DeviceWithStatus>.Failure($"Device '{deviceId}' not found");
            }

            var health = await _loggerService.GetDeviceHealthAsync(deviceId);

            return OperationResult<DeviceWithStatus>.Success(new DeviceWithStatus
            {
                Config = device,
                Health = health
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get device {DeviceId}", deviceId);
            return OperationResult<DeviceWithStatus>.Failure($"Failed to get device: {ex.Message}");
        }
    }

    public async Task<OperationResult<DeviceWithStatus>> CreateDeviceAsync(AdamDeviceConfig config)
    {
        try
        {
            // Validate the device configuration
            var validationResult = ValidateDeviceConfig(config);
            if (!validationResult.IsValid)
            {
                return OperationResult<DeviceWithStatus>.Failure(
                    string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)));
            }

            // Check if device already exists
            if (_configOptions.CurrentValue.Devices.Any(d => d.DeviceId == config.DeviceId))
            {
                return OperationResult<DeviceWithStatus>.Failure($"Device '{config.DeviceId}' already exists");
            }

            // Add device to the logger service
            await _loggerService.AddDeviceAsync(config);

            // Get the device status
            var health = await _loggerService.GetDeviceHealthAsync(config.DeviceId);

            return OperationResult<DeviceWithStatus>.Success(new DeviceWithStatus
            {
                Config = config,
                Health = health
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create device {DeviceId}", config.DeviceId);
            return OperationResult<DeviceWithStatus>.Failure($"Failed to create device: {ex.Message}");
        }
    }

    public async Task<OperationResult<DeviceWithStatus>> UpdateDeviceAsync(string deviceId, AdamDeviceConfig config)
    {
        try
        {
            // Ensure the device ID matches
            if (config.DeviceId != deviceId)
            {
                return OperationResult<DeviceWithStatus>.Failure("Device ID in URL does not match device ID in body");
            }

            // Validate the device configuration
            var validationResult = ValidateDeviceConfig(config);
            if (!validationResult.IsValid)
            {
                return OperationResult<DeviceWithStatus>.Failure(
                    string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)));
            }

            // Update device in the logger service
            await _loggerService.UpdateDeviceConfigAsync(config);

            // Get the updated device status
            var health = await _loggerService.GetDeviceHealthAsync(config.DeviceId);

            return OperationResult<DeviceWithStatus>.Success(new DeviceWithStatus
            {
                Config = config,
                Health = health
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update device {DeviceId}", deviceId);
            return OperationResult<DeviceWithStatus>.Failure($"Failed to update device: {ex.Message}");
        }
    }

    public async Task<OperationResult> DeleteDeviceAsync(string deviceId)
    {
        try
        {
            await _loggerService.RemoveDeviceAsync(deviceId);
            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete device {DeviceId}", deviceId);
            return OperationResult.Failure($"Failed to delete device: {ex.Message}");
        }
    }

    public async Task<OperationResult<ConnectionTestResult>> TestDeviceConnectionAsync(string deviceId)
    {
        try
        {
            var device = _configOptions.CurrentValue.Devices.FirstOrDefault(d => d.DeviceId == deviceId);
            if (device == null)
            {
                return OperationResult<ConnectionTestResult>.Failure($"Device '{deviceId}' not found");
            }

            var result = new ConnectionTestResult
            {
                Success = false,
                Steps = new List<TestStep>()
            };

            var stopwatch = new Stopwatch();

            // Step 1: Test TCP connection
            var tcpStep = await TestTcpConnectionAsync(device);
            result.Steps.Add(tcpStep);
            if (!tcpStep.Success)
            {
                result.ErrorMessage = "TCP connection failed";
                return OperationResult<ConnectionTestResult>.Success(result);
            }

            // Step 2: Test Modbus connection
            var modbusStep = await TestModbusConnectionAsync(device);
            result.Steps.Add(modbusStep);
            if (!modbusStep.Success)
            {
                result.ErrorMessage = "Modbus connection failed";
                return OperationResult<ConnectionTestResult>.Success(result);
            }

            // Step 3: Test register read
            var registerStep = await TestRegisterReadAsync(device);
            result.Steps.Add(registerStep);
            if (!registerStep.Success)
            {
                result.ErrorMessage = "Register read failed";
                return OperationResult<ConnectionTestResult>.Success(result);
            }

            // All tests passed
            result.Success = true;
            result.ResponseTimeMs = result.Steps.Sum(s => s.DurationMs);

            return OperationResult<ConnectionTestResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test device connection {DeviceId}", deviceId);
            return OperationResult<ConnectionTestResult>.Failure($"Failed to test connection: {ex.Message}");
        }
    }

    public async Task<OperationResult> SetDeviceEnabledAsync(string deviceId, bool enabled)
    {
        try
        {
            var device = _configOptions.CurrentValue.Devices.FirstOrDefault(d => d.DeviceId == deviceId);
            if (device == null)
            {
                return OperationResult.Failure($"Device '{deviceId}' not found");
            }

            // Note: Need to implement enabled/disabled state in device config
            // For now, we'll just update the device configuration
            await _loggerService.UpdateDeviceConfigAsync(device);

            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set device {DeviceId} enabled state to {Enabled}", deviceId, enabled);
            return OperationResult.Failure($"Failed to update device state: {ex.Message}");
        }
    }

    public OperationResult<AdamLoggerConfig> GetConfiguration()
    {
        try
        {
            var config = _configOptions.CurrentValue;
            return OperationResult<AdamLoggerConfig>.Success(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get configuration");
            return OperationResult<AdamLoggerConfig>.Failure($"Failed to get configuration: {ex.Message}");
        }
    }

    public Task<OperationResult> UpdateConfigurationAsync(AdamLoggerConfig config)
    {
        try
        {
            var validationResult = ValidateConfiguration(config);
            if (!validationResult.Value.IsValid)
            {
                return Task.FromResult(OperationResult.Failure(
                    string.Join("; ", validationResult.Value.Errors.Select(e => e.ErrorMessage))));
            }

            // Update configuration would typically involve writing to a configuration store
            // For now, we'll just validate and return success
            _logger.LogInformation("Configuration validated successfully");
            
            return Task.FromResult(OperationResult.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update configuration");
            return Task.FromResult(OperationResult.Failure($"Failed to update configuration: {ex.Message}"));
        }
    }

    public OperationResult<ValidationResult> ValidateConfiguration(AdamLoggerConfig config)
    {
        try
        {
            var errors = new List<ValidationError>();

            // Validate using data annotations
            var validationContext = new ValidationContext(config);
            var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            
            if (!Validator.TryValidateObject(config, validationContext, validationResults, true))
            {
                errors.AddRange(validationResults.Select(vr => new ValidationError
                {
                    PropertyName = vr.MemberNames.FirstOrDefault() ?? "Unknown",
                    ErrorMessage = vr.ErrorMessage ?? "Validation failed"
                }));
            }

            // Additional custom validation
            if (config.Devices.Count == 0)
            {
                errors.Add(new ValidationError
                {
                    PropertyName = "Devices",
                    ErrorMessage = "At least one device must be configured",
                    Severity = ValidationSeverity.Warning
                });
            }

            // Check for duplicate device IDs
            var duplicateIds = config.Devices
                .GroupBy(d => d.DeviceId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (var duplicateId in duplicateIds)
            {
                errors.Add(new ValidationError
                {
                    PropertyName = "Devices",
                    ErrorMessage = $"Duplicate device ID: {duplicateId}"
                });
            }

            var result = new ValidationResult
            {
                IsValid = !errors.Any(e => e.Severity == ValidationSeverity.Error),
                Errors = errors
            };

            return OperationResult<ValidationResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate configuration");
            return OperationResult<ValidationResult>.Failure($"Failed to validate configuration: {ex.Message}");
        }
    }

    private ValidationResult ValidateDeviceConfig(AdamDeviceConfig config)
    {
        var errors = new List<ValidationError>();

        // Validate using data annotations
        var validationContext = new ValidationContext(config);
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        
        if (!Validator.TryValidateObject(config, validationContext, validationResults, true))
        {
            errors.AddRange(validationResults.Select(vr => new ValidationError
            {
                PropertyName = vr.MemberNames.FirstOrDefault() ?? "Unknown",
                ErrorMessage = vr.ErrorMessage ?? "Validation failed"
            }));
        }

        // Additional custom validation
        if (config.Channels.Count == 0)
        {
            errors.Add(new ValidationError
            {
                PropertyName = "Channels",
                ErrorMessage = "At least one channel should be configured",
                Severity = ValidationSeverity.Warning
            });
        }

        return new ValidationResult
        {
            IsValid = !errors.Any(e => e.Severity == ValidationSeverity.Error),
            Errors = errors
        };
    }

    private async Task<TestStep> TestTcpConnectionAsync(AdamDeviceConfig device)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var tcpClient = new System.Net.Sockets.TcpClient();
            await tcpClient.ConnectAsync(device.IpAddress, device.Port);
            stopwatch.Stop();

            return new TestStep
            {
                Name = "TCP Connection",
                Success = true,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Details = $"Connected to {device.IpAddress}:{device.Port}"
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new TestStep
            {
                Name = "TCP Connection",
                Success = false,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Details = ex.Message
            };
        }
    }

    private async Task<TestStep> TestModbusConnectionAsync(AdamDeviceConfig device)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            // For now, we'll just simulate a Modbus test
            // In a real implementation, you would use the Modbus device manager
            await Task.Delay(50); // Simulate network call
            
            stopwatch.Stop();

            return new TestStep
            {
                Name = "Modbus Protocol",
                Success = true,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Details = "Modbus communication test (simulated)"
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new TestStep
            {
                Name = "Modbus Protocol",
                Success = false,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Details = ex.Message
            };
        }
    }

    private async Task<TestStep> TestRegisterReadAsync(AdamDeviceConfig device)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            if (!device.Channels.Any())
            {
                return new TestStep
                {
                    Name = "Register Read",
                    Success = true,
                    DurationMs = 0,
                    Details = "No channels configured to test"
                };
            }

            var firstChannel = device.Channels.First();
            
            // Simulate register read test
            await Task.Delay(30);
            
            stopwatch.Stop();

            return new TestStep
            {
                Name = "Register Read",
                Success = true,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Details = $"Register read test for {firstChannel.RegisterCount} registers from address {firstChannel.StartRegister} (simulated)"
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new TestStep
            {
                Name = "Register Read",
                Success = false,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Details = ex.Message
            };
        }
    }
}