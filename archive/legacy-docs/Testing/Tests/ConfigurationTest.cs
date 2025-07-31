using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Industrial.Adam.Logger.Configuration;
using Industrial.Adam.Logger.ErrorHandling;
using Industrial.Adam.Logger.Interfaces;
using Industrial.Adam.Logger.Testing.Models;
using Industrial.Adam.Logger.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Industrial.Adam.Logger.Testing.Tests;

/// <summary>
/// Configuration validation tests for ADAM logger system
/// </summary>
public sealed class ConfigurationTest
{
    private readonly ILogger<ConfigurationTest> _logger;
    private readonly IOptions<AdamLoggerConfig> _config;
    private readonly IIndustrialErrorService _errorService;

    /// <summary>
    /// Initialize configuration test
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="config">Configuration options</param>
    /// <param name="errorService">Error service</param>
    public ConfigurationTest(
        ILogger<ConfigurationTest> logger,
        IOptions<AdamLoggerConfig> config,
        IIndustrialErrorService errorService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _errorService = errorService ?? throw new ArgumentNullException(nameof(errorService));
    }

    /// <summary>
    /// Validate core configuration settings
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Configuration validation result</returns>
    public async Task<TestResult> ValidateConfigurationAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var testId = "CONF-001";
        var testName = "Core Configuration Validation";
        var metrics = new Dictionary<string, object>();
        var recommendations = new List<string>();

        try
        {
            await Task.CompletedTask; // Ensure this is truly async
            var configuration = _config.Value;
            var validationResults = new List<ValidationResult>();
            var context = new ValidationContext(configuration);

            // Validate configuration using data annotations
            var isValid = Validator.TryValidateObject(configuration, context, validationResults, true);

            metrics["IsValid"] = isValid;
            metrics["ValidationErrors"] = validationResults.Select(r => r.ErrorMessage).ToArray();
            metrics["DeviceCount"] = configuration.Devices.Count;
            metrics["PollInterval"] = configuration.PollIntervalMs;
            metrics["HealthCheckInterval"] = configuration.HealthCheckIntervalMs;
            metrics["MaxConcurrentDevices"] = configuration.MaxConcurrentDevices;
            metrics["DemoMode"] = configuration.DemoMode;

            if (!isValid)
            {
                var errorMessages = validationResults.Select(r => r.ErrorMessage).ToArray();
                recommendations.Add("Review configuration file for required settings");
                recommendations.Add("Verify all device configurations are complete");
                recommendations.Add("Check data annotation constraints");

                var errorMessage = $"Configuration validation failed with {errorMessages.Length} error(s): {string.Join("; ", errorMessages)}";

                return TestResult.Failure(
                    testId,
                    testName,
                    TestCategory.Configuration,
                    stopwatch.Elapsed,
                    errorMessage,
                    metrics: metrics,
                    recommendations: recommendations);
            }

            // Additional validation checks
            var warnings = ValidateConfigurationLogic(configuration, metrics, recommendations);

            if (warnings.Count > 0)
            {
                var warningMessage = $"Configuration valid but {warnings.Count} warning(s) detected: {string.Join("; ", warnings)}";

                return TestResult.Warning(
                    testId,
                    testName,
                    TestCategory.Configuration,
                    stopwatch.Elapsed,
                    warningMessage,
                    metrics,
                    recommendations);
            }

            var successMessage = $"Configuration validation successful for {configuration.Devices.Count} device(s)";
            recommendations.Add("Regularly review configuration for optimization opportunities");
            recommendations.Add("Monitor configuration changes through version control");

            return TestResult.Success(
                testId,
                testName,
                TestCategory.Configuration,
                stopwatch.Elapsed,
                successMessage,
                metrics,
                recommendations);
        }
        catch (Exception ex)
        {
            var errorMessage = _errorService.CreateAndLogError(
                ex,
                "TEST-010",
                "Configuration validation test failed",
                new Dictionary<string, object>
                {
                    ["TestId"] = testId,
                    ["TestName"] = testName
                });

            return TestResult.Failure(
                testId,
                testName,
                TestCategory.Configuration,
                stopwatch.Elapsed,
                errorMessage.Summary,
                ex,
                metrics,
                errorMessage.TroubleshootingSteps,
                TestSeverity.Error);
        }
    }

    /// <summary>
    /// Validate InfluxDB configuration
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>InfluxDB configuration validation result</returns>
    public async Task<TestResult> ValidateInfluxDbConfigurationAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var testId = "CONF-002";
        var testName = "InfluxDB Configuration Validation";
        var metrics = new Dictionary<string, object>();
        var recommendations = new List<string>();

        try
        {
            await Task.CompletedTask; // Ensure this is truly async
            var influxConfig = _config.Value.InfluxDb;
            if (influxConfig == null)
            {
                return TestResult.Failure(
                    testId,
                    testName,
                    TestCategory.Configuration,
                    stopwatch.Elapsed,
                    "InfluxDB configuration is missing",
                    metrics: metrics,
                    recommendations: new[] { "Add InfluxDB configuration section to appsettings.json" });
            }

            var validationResults = new List<ValidationResult>();
            var context = new ValidationContext(influxConfig);
            var isValid = Validator.TryValidateObject(influxConfig, context, validationResults, true);

            metrics["IsValid"] = isValid;
            metrics["ValidationErrors"] = validationResults.Select(r => r.ErrorMessage).ToArray();
            metrics["Url"] = influxConfig.Url;
            metrics["Organization"] = influxConfig.Organization;
            metrics["Bucket"] = influxConfig.Bucket;
            metrics["Measurement"] = influxConfig.Measurement;
            metrics["WriteBatchSize"] = influxConfig.WriteBatchSize;
            metrics["FlushInterval"] = influxConfig.FlushIntervalMs;

            if (!isValid)
            {
                var errorMessages = validationResults.Select(r => r.ErrorMessage).ToArray();
                recommendations.Add("Review InfluxDB configuration settings");
                recommendations.Add("Verify URL format and accessibility");
                recommendations.Add("Check authentication credentials");

                var errorMessage = $"InfluxDB configuration validation failed: {string.Join("; ", errorMessages)}";

                return TestResult.Failure(
                    testId,
                    testName,
                    TestCategory.Configuration,
                    stopwatch.Elapsed,
                    errorMessage,
                    metrics: metrics,
                    recommendations: recommendations);
            }

            // Additional validation checks
            var warnings = new List<string>();

            if (influxConfig.WriteBatchSize < 10 || influxConfig.WriteBatchSize > 1000)
            {
                warnings.Add($"WriteBatchSize ({influxConfig.WriteBatchSize}) should be between 10 and 1000");
                recommendations.Add("Optimize write batch size for performance");
            }

            if (influxConfig.FlushIntervalMs < 1000 || influxConfig.FlushIntervalMs > 30000)
            {
                warnings.Add($"FlushIntervalMs ({influxConfig.FlushIntervalMs}) should be between 1000 and 30000");
                recommendations.Add("Adjust flush interval for data freshness vs performance balance");
            }

            if (!Uri.TryCreate(influxConfig.Url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != "http" && uri.Scheme != "https"))
            {
                warnings.Add($"URL format may be invalid: {influxConfig.Url}");
                recommendations.Add("Verify InfluxDB URL format and protocol");
            }

            metrics["WarningCount"] = warnings.Count;
            metrics["Warnings"] = warnings.ToArray();

            if (warnings.Count > 0)
            {
                var warningMessage = $"InfluxDB configuration valid but {warnings.Count} warning(s): {string.Join("; ", warnings)}";

                return TestResult.Warning(
                    testId,
                    testName,
                    TestCategory.Configuration,
                    stopwatch.Elapsed,
                    warningMessage,
                    metrics,
                    recommendations);
            }

            var successMessage = "InfluxDB configuration validation successful";
            recommendations.Add("Test InfluxDB connectivity after configuration changes");
            recommendations.Add("Monitor InfluxDB performance metrics");

            return TestResult.Success(
                testId,
                testName,
                TestCategory.Configuration,
                stopwatch.Elapsed,
                successMessage,
                metrics,
                recommendations);
        }
        catch (Exception ex)
        {
            var errorMessage = _errorService.CreateAndLogError(
                ex,
                "TEST-011",
                "InfluxDB configuration validation test failed",
                new Dictionary<string, object>
                {
                    ["TestId"] = testId,
                    ["TestName"] = testName
                });

            return TestResult.Failure(
                testId,
                testName,
                TestCategory.Configuration,
                stopwatch.Elapsed,
                errorMessage.Summary,
                ex,
                metrics,
                errorMessage.TroubleshootingSteps,
                TestSeverity.Error);
        }
    }

    /// <summary>
    /// Validate device configurations
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Device configuration validation result</returns>
    public async Task<TestResult> ValidateDeviceConfigurationsAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var testId = "CONF-003";
        var testName = "Device Configuration Validation";
        var metrics = new Dictionary<string, object>();
        var recommendations = new List<string>();

        try
        {
            await Task.CompletedTask; // Ensure this is truly async
            var devices = _config.Value.Devices;
            if (devices.Count == 0)
            {
                return TestResult.Warning(
                    testId,
                    testName,
                    TestCategory.Configuration,
                    stopwatch.Elapsed,
                    "No devices configured",
                    metrics,
                    new[] { "Add device configurations to enable data collection" });
            }

            var validDevices = 0;
            var invalidDevices = new List<string>();
            var warnings = new List<string>();
            var deviceValidationResults = new Dictionary<string, object>();

            foreach (var device in devices)
            {
                var deviceMetrics = new Dictionary<string, object>();
                var deviceWarnings = new List<string>();
                var validationResults = new List<ValidationResult>();
                var context = new ValidationContext(device);
                var isValid = Validator.TryValidateObject(device, context, validationResults, true);

                deviceMetrics["IsValid"] = isValid;
                deviceMetrics["ValidationErrors"] = validationResults.Select(r => r.ErrorMessage).ToArray();
                deviceMetrics["ChannelCount"] = device.Channels.Count;
                deviceMetrics["IpAddress"] = device.IpAddress;
                deviceMetrics["Port"] = device.Port;
                deviceMetrics["UnitId"] = device.UnitId;
                deviceMetrics["TimeoutMs"] = device.TimeoutMs;
                deviceMetrics["MaxRetries"] = device.MaxRetries;

                if (isValid)
                {
                    validDevices++;

                    // Additional device-specific validation
                    ValidateDeviceLogic(device, deviceMetrics, deviceWarnings);

                    if (deviceWarnings.Count > 0)
                    {
                        warnings.AddRange(deviceWarnings);
                    }
                }
                else
                {
                    invalidDevices.Add(device.DeviceId);
                }

                deviceMetrics["WarningCount"] = deviceWarnings.Count;
                deviceMetrics["Warnings"] = deviceWarnings.ToArray();
                deviceValidationResults[device.DeviceId] = deviceMetrics;
            }

            metrics["TotalDevices"] = devices.Count;
            metrics["ValidDevices"] = validDevices;
            metrics["InvalidDevices"] = invalidDevices.Count;
            metrics["WarningCount"] = warnings.Count;
            metrics["DeviceValidationResults"] = deviceValidationResults;

            if (invalidDevices.Count > 0)
            {
                recommendations.Add("Review device configurations for required fields");
                recommendations.Add("Verify IP addresses and port settings");
                recommendations.Add("Check channel configurations");

                var errorMessage = $"Device configuration validation failed for {invalidDevices.Count} device(s): {string.Join(", ", invalidDevices)}";

                return TestResult.Failure(
                    testId,
                    testName,
                    TestCategory.Configuration,
                    stopwatch.Elapsed,
                    errorMessage,
                    metrics: metrics,
                    recommendations: recommendations);
            }

            if (warnings.Count > 0)
            {
                recommendations.Add("Review device configuration warnings");
                recommendations.Add("Optimize settings for better performance");

                var warningMessage = $"Device configurations valid but {warnings.Count} warning(s) detected";

                return TestResult.Warning(
                    testId,
                    testName,
                    TestCategory.Configuration,
                    stopwatch.Elapsed,
                    warningMessage,
                    metrics,
                    recommendations);
            }

            var successMessage = $"Device configuration validation successful for all {validDevices} device(s)";
            recommendations.Add("Regularly audit device configurations");
            recommendations.Add("Implement configuration change tracking");

            return TestResult.Success(
                testId,
                testName,
                TestCategory.Configuration,
                stopwatch.Elapsed,
                successMessage,
                metrics,
                recommendations);
        }
        catch (Exception ex)
        {
            var errorMessage = _errorService.CreateAndLogError(
                ex,
                "TEST-012",
                "Device configuration validation test failed",
                new Dictionary<string, object>
                {
                    ["TestId"] = testId,
                    ["TestName"] = testName
                });

            return TestResult.Failure(
                testId,
                testName,
                TestCategory.Configuration,
                stopwatch.Elapsed,
                errorMessage.Summary,
                ex,
                metrics,
                errorMessage.TroubleshootingSteps,
                TestSeverity.Error);
        }
    }

    /// <summary>
    /// Validate configuration logic beyond data annotations
    /// </summary>
    /// <param name="configuration">Configuration to validate</param>
    /// <param name="metrics">Metrics dictionary</param>
    /// <param name="recommendations">Recommendations list</param>
    /// <returns>List of warning messages</returns>
    private static List<string> ValidateConfigurationLogic(
        AdamLoggerConfig configuration,
        Dictionary<string, object> metrics,
        List<string> recommendations)
    {
        var warnings = new List<string>();

        // Validate poll interval
        if (configuration.PollIntervalMs < 100)
        {
            warnings.Add($"PollIntervalMs ({configuration.PollIntervalMs}) is very low and may cause performance issues");
            recommendations.Add("Consider increasing poll interval to reduce CPU usage");
        }
        else if (configuration.PollIntervalMs > 60000)
        {
            warnings.Add($"PollIntervalMs ({configuration.PollIntervalMs}) is very high and may impact data freshness");
            recommendations.Add("Consider decreasing poll interval for better data timeliness");
        }

        // Validate health check interval
        if (configuration.HealthCheckIntervalMs < configuration.PollIntervalMs)
        {
            warnings.Add("HealthCheckIntervalMs is less than PollIntervalMs");
            recommendations.Add("Health check interval should be greater than poll interval");
        }

        // Validate max concurrent devices
        if (configuration.MaxConcurrentDevices > Environment.ProcessorCount * 2)
        {
            warnings.Add($"MaxConcurrentDevices ({configuration.MaxConcurrentDevices}) exceeds recommended limit");
            recommendations.Add("Consider reducing concurrent device limit to avoid resource contention");
        }

        // Check for duplicate device IDs
        var duplicateDeviceIds = configuration.Devices
            .GroupBy(d => d.DeviceId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateDeviceIds.Count > 0)
        {
            warnings.Add($"Duplicate device IDs detected: {string.Join(", ", duplicateDeviceIds)}");
            recommendations.Add("Ensure all device IDs are unique");
        }

        metrics["PollIntervalValidation"] = configuration.PollIntervalMs >= 100 && configuration.PollIntervalMs <= 60000;
        metrics["HealthCheckIntervalValidation"] = configuration.HealthCheckIntervalMs >= configuration.PollIntervalMs;
        metrics["MaxConcurrentDevicesValidation"] = configuration.MaxConcurrentDevices <= Environment.ProcessorCount * 2;
        metrics["DuplicateDeviceIds"] = duplicateDeviceIds.Count;

        return warnings;
    }

    /// <summary>
    /// Validate device-specific logic
    /// </summary>
    /// <param name="device">Device configuration</param>
    /// <param name="deviceMetrics">Device metrics dictionary</param>
    /// <param name="deviceWarnings">Device warnings list</param>
    private static void ValidateDeviceLogic(
        AdamDeviceConfig device,
        Dictionary<string, object> deviceMetrics,
        List<string> deviceWarnings)
    {
        // Validate IP address format
        if (!System.Net.IPAddress.TryParse(device.IpAddress, out _))
        {
            deviceWarnings.Add($"Invalid IP address format: {device.IpAddress}");
        }

        // Validate port range
        if (device.Port < 1 || device.Port > 65535)
        {
            deviceWarnings.Add($"Port ({device.Port}) is outside valid range (1-65535)");
        }

        // Validate timeout
        if (device.TimeoutMs < 1000 || device.TimeoutMs > 30000)
        {
            deviceWarnings.Add($"TimeoutMs ({device.TimeoutMs}) should be between 1000 and 30000");
        }

        // Validate retry count
        if (device.MaxRetries < 0 || device.MaxRetries > 10)
        {
            deviceWarnings.Add($"MaxRetries ({device.MaxRetries}) should be between 0 and 10");
        }

        // Validate channels
        if (device.Channels.Count == 0)
        {
            deviceWarnings.Add("No channels configured for device");
        }
        else
        {
            var duplicateChannels = device.Channels
                .GroupBy(c => c.ChannelNumber)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateChannels.Count > 0)
            {
                deviceWarnings.Add($"Duplicate channel numbers: {string.Join(", ", duplicateChannels)}");
            }

            foreach (var channel in device.Channels)
            {
                if (channel.RegisterCount < 1 || channel.RegisterCount > 10)
                {
                    deviceWarnings.Add($"Channel {channel.ChannelNumber} RegisterCount ({channel.RegisterCount}) should be between 1 and 10");
                }

                if (channel.ScaleFactor <= 0)
                {
                    deviceWarnings.Add($"Channel {channel.ChannelNumber} ScaleFactor ({channel.ScaleFactor}) should be greater than 0");
                }
            }
        }

        deviceMetrics["IpAddressValid"] = System.Net.IPAddress.TryParse(device.IpAddress, out _);
        deviceMetrics["PortValid"] = device.Port >= 1 && device.Port <= 65535;
        deviceMetrics["TimeoutValid"] = device.TimeoutMs >= 1000 && device.TimeoutMs <= 30000;
        deviceMetrics["MaxRetriesValid"] = device.MaxRetries >= 0 && device.MaxRetries <= 10;
        deviceMetrics["HasChannels"] = device.Channels.Count > 0;
        deviceMetrics["EnabledChannels"] = device.Channels.Count(c => c.Enabled);
    }
}
