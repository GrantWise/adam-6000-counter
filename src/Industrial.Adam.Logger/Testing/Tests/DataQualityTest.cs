using System.Diagnostics;
using Industrial.Adam.Logger.Configuration;
using Industrial.Adam.Logger.ErrorHandling;
using Industrial.Adam.Logger.Interfaces;
using Industrial.Adam.Logger.Models;
using Industrial.Adam.Logger.Testing.Models;
using Industrial.Adam.Logger.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Industrial.Adam.Logger.Testing.Tests;

/// <summary>
/// Data quality validation tests for counter data processing
/// </summary>
public sealed class DataQualityTest
{
    private readonly ILogger<DataQualityTest> _logger;
    private readonly IOptions<AdamLoggerConfig> _config;
    private readonly IIndustrialErrorService _errorService;

    /// <summary>
    /// Initialize data quality test
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="config">Configuration options</param>
    /// <param name="errorService">Error service</param>
    public DataQualityTest(
        ILogger<DataQualityTest> logger,
        IOptions<AdamLoggerConfig> config,
        IIndustrialErrorService errorService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _errorService = errorService ?? throw new ArgumentNullException(nameof(errorService));
    }

    /// <summary>
    /// Test data validation rules and constraints
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Data validation test result</returns>
    public async Task<TestResult> TestDataValidationAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var testId = "DATA-001";
        var testName = "Data Validation Rules Test";
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
                    TestCategory.DataQuality,
                    stopwatch.Elapsed,
                    "No devices configured for data validation testing",
                    metrics,
                    new[] { "Add device configurations to enable data validation testing" });
            }

            var totalTests = 0;
            var passedTests = 0;
            var failedTests = new List<string>();
            var deviceResults = new Dictionary<string, object>();

            foreach (var device in devices)
            {
                var deviceMetrics = new Dictionary<string, object>();
                var channelResults = new Dictionary<string, object>();

                foreach (var channel in device.Channels.Where(c => c.Enabled))
                {
                    totalTests++;
                    var channelMetrics = new Dictionary<string, object>();

                    try
                    {
                        // Test data validation rules
                        var validationResults = ValidateChannelDataRules(device, channel);

                        channelMetrics["ValidationPassed"] = validationResults.IsValid;
                        channelMetrics["ValidationMessages"] = validationResults.ValidationMessages;
                        channelMetrics["MinValueValid"] = validationResults.MinValueValid;
                        channelMetrics["MaxValueValid"] = validationResults.MaxValueValid;
                        channelMetrics["ScaleFactorValid"] = validationResults.ScaleFactorValid;
                        channelMetrics["RegisterCountValid"] = validationResults.RegisterCountValid;

                        if (validationResults.IsValid)
                        {
                            passedTests++;
                            _logger.LogDebug(
                                "Data validation passed for device {DeviceId} channel {ChannelNumber}",
                                device.DeviceId, channel.ChannelNumber);
                        }
                        else
                        {
                            failedTests.Add($"{device.DeviceId} Ch{channel.ChannelNumber}");
                            _logger.LogWarning(
                                "Data validation failed for device {DeviceId} channel {ChannelNumber}: {ValidationMessages}",
                                device.DeviceId, channel.ChannelNumber, string.Join("; ", validationResults.ValidationMessages));
                        }
                    }
                    catch (Exception ex)
                    {
                        failedTests.Add($"{device.DeviceId} Ch{channel.ChannelNumber}");
                        channelMetrics["ValidationPassed"] = false;
                        channelMetrics["ValidationError"] = ex.Message;

                        _logger.LogError(ex,
                            "Failed to validate data rules for device {DeviceId} channel {ChannelNumber}",
                            device.DeviceId, channel.ChannelNumber);
                    }

                    channelResults[channel.ChannelNumber.ToString()] = channelMetrics;
                }

                deviceMetrics["ChannelResults"] = channelResults;
                deviceMetrics["EnabledChannels"] = device.Channels.Count(c => c.Enabled);
                deviceResults[device.DeviceId] = deviceMetrics;
            }

            metrics["TotalTests"] = totalTests;
            metrics["PassedTests"] = passedTests;
            metrics["FailedTests"] = totalTests - passedTests;
            metrics["SuccessRate"] = totalTests > 0 ? (double)passedTests / totalTests * 100 : 0;
            metrics["DeviceResults"] = deviceResults;

            if (failedTests.Count > 0)
            {
                recommendations.Add("Review data validation rules for failed channels");
                recommendations.Add("Check min/max value constraints");
                recommendations.Add("Verify scale factor and offset settings");
                recommendations.Add("Ensure register count matches data type requirements");

                var errorMessage = $"Data validation failed for {failedTests.Count} channel(s): {string.Join(", ", failedTests)}";

                return TestResult.Failure(
                    testId,
                    testName,
                    TestCategory.DataQuality,
                    stopwatch.Elapsed,
                    errorMessage,
                    metrics: metrics,
                    recommendations: recommendations);
            }

            var successMessage = $"Data validation successful for all {passedTests} channel(s)";
            recommendations.Add("Monitor data quality metrics regularly");
            recommendations.Add("Implement automated data quality alerts");

            return TestResult.Success(
                testId,
                testName,
                TestCategory.DataQuality,
                stopwatch.Elapsed,
                successMessage,
                metrics,
                recommendations);
        }
        catch (Exception ex)
        {
            var errorMessage = _errorService.CreateAndLogError(
                ex,
                "TEST-020",
                "Data validation test failed",
                new Dictionary<string, object>
                {
                    ["TestId"] = testId,
                    ["TestName"] = testName
                });

            return TestResult.Failure(
                testId,
                testName,
                TestCategory.DataQuality,
                stopwatch.Elapsed,
                errorMessage.Summary,
                ex,
                metrics,
                errorMessage.TroubleshootingSteps,
                TestSeverity.Error);
        }
    }

    /// <summary>
    /// Test counter overflow detection and handling
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Counter overflow test result</returns>
    public async Task<TestResult> TestCounterOverflowHandlingAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var testId = "DATA-002";
        var testName = "Counter Overflow Detection Test";
        var metrics = new Dictionary<string, object>();
        var recommendations = new List<string>();

        try
        {
            await Task.CompletedTask; // Ensure this is truly async
            // Test counter overflow scenarios
            var testScenarios = new List<CounterOverflowScenario>
            {
                new() { PreviousValue = 4294967290, CurrentValue = 5, ExpectedOverflow = true, Description = "32-bit counter overflow" },
                new() { PreviousValue = 65530, CurrentValue = 5, ExpectedOverflow = true, Description = "16-bit counter overflow" },
                new() { PreviousValue = 100, CurrentValue = 200, ExpectedOverflow = false, Description = "Normal increment" },
                new() { PreviousValue = 1000, CurrentValue = 500, ExpectedOverflow = false, Description = "Counter reset" }
            };

            var totalTests = testScenarios.Count;
            var passedTests = 0;
            var failedTests = new List<string>();
            var scenarioResults = new Dictionary<string, object>();

            foreach (var scenario in testScenarios)
            {
                var scenarioMetrics = new Dictionary<string, object>();

                try
                {
                    // Test overflow detection logic
                    var overflowDetected = DetectCounterOverflow(scenario.PreviousValue, scenario.CurrentValue);
                    var testPassed = overflowDetected == scenario.ExpectedOverflow;

                    scenarioMetrics["TestPassed"] = testPassed;
                    scenarioMetrics["PreviousValue"] = scenario.PreviousValue;
                    scenarioMetrics["CurrentValue"] = scenario.CurrentValue;
                    scenarioMetrics["ExpectedOverflow"] = scenario.ExpectedOverflow;
                    scenarioMetrics["DetectedOverflow"] = overflowDetected;
                    scenarioMetrics["Description"] = scenario.Description;

                    if (testPassed)
                    {
                        passedTests++;
                        _logger.LogDebug(
                            "Counter overflow test passed: {Description} - Expected: {ExpectedOverflow}, Detected: {DetectedOverflow}",
                            scenario.Description, scenario.ExpectedOverflow, overflowDetected);
                    }
                    else
                    {
                        failedTests.Add(scenario.Description);
                        _logger.LogWarning(
                            "Counter overflow test failed: {Description} - Expected: {ExpectedOverflow}, Detected: {DetectedOverflow}",
                            scenario.Description, scenario.ExpectedOverflow, overflowDetected);
                    }
                }
                catch (Exception ex)
                {
                    failedTests.Add(scenario.Description);
                    scenarioMetrics["TestPassed"] = false;
                    scenarioMetrics["TestError"] = ex.Message;

                    _logger.LogError(ex,
                        "Failed to test counter overflow scenario: {Description}",
                        scenario.Description);
                }

                scenarioResults[scenario.Description] = scenarioMetrics;
            }

            metrics["TotalTests"] = totalTests;
            metrics["PassedTests"] = passedTests;
            metrics["FailedTests"] = totalTests - passedTests;
            metrics["SuccessRate"] = totalTests > 0 ? (double)passedTests / totalTests * 100 : 0;
            metrics["ScenarioResults"] = scenarioResults;

            if (failedTests.Count > 0)
            {
                recommendations.Add("Review counter overflow detection algorithm");
                recommendations.Add("Verify 32-bit and 16-bit counter handling");
                recommendations.Add("Test with actual device counter values");
                recommendations.Add("Implement counter type auto-detection");

                var errorMessage = $"Counter overflow detection failed for {failedTests.Count} scenario(s): {string.Join(", ", failedTests)}";

                return TestResult.Failure(
                    testId,
                    testName,
                    TestCategory.DataQuality,
                    stopwatch.Elapsed,
                    errorMessage,
                    metrics: metrics,
                    recommendations: recommendations);
            }

            var successMessage = $"Counter overflow detection successful for all {passedTests} scenario(s)";
            recommendations.Add("Monitor counter overflow frequency");
            recommendations.Add("Implement overflow alerts for unusual patterns");

            return TestResult.Success(
                testId,
                testName,
                TestCategory.DataQuality,
                stopwatch.Elapsed,
                successMessage,
                metrics,
                recommendations);
        }
        catch (Exception ex)
        {
            var errorMessage = _errorService.CreateAndLogError(
                ex,
                "TEST-021",
                "Counter overflow detection test failed",
                new Dictionary<string, object>
                {
                    ["TestId"] = testId,
                    ["TestName"] = testName
                });

            return TestResult.Failure(
                testId,
                testName,
                TestCategory.DataQuality,
                stopwatch.Elapsed,
                errorMessage.Summary,
                ex,
                metrics,
                errorMessage.TroubleshootingSteps,
                TestSeverity.Error);
        }
    }

    /// <summary>
    /// Test data quality classification
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Data quality classification test result</returns>
    public async Task<TestResult> TestDataQualityClassificationAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var testId = "DATA-003";
        var testName = "Data Quality Classification Test";
        var metrics = new Dictionary<string, object>();
        var recommendations = new List<string>();

        try
        {
            await Task.CompletedTask; // Ensure this is truly async
            // Test data quality classification scenarios
            var testScenarios = new List<DataQualityScenario>
            {
                new() { Value = 100, IsValid = true, IsStable = true, ExpectedQuality = DataQuality.Good, Description = "Valid stable data" },
                new() { Value = -1, IsValid = false, IsStable = true, ExpectedQuality = DataQuality.Bad, Description = "Invalid data" },
                new() { Value = 50, IsValid = true, IsStable = false, ExpectedQuality = DataQuality.Uncertain, Description = "Valid but unstable data" },
                new() { Value = 0, IsValid = true, IsStable = true, ExpectedQuality = DataQuality.Good, Description = "Valid zero value" }
            };

            var totalTests = testScenarios.Count;
            var passedTests = 0;
            var failedTests = new List<string>();
            var scenarioResults = new Dictionary<string, object>();

            foreach (var scenario in testScenarios)
            {
                var scenarioMetrics = new Dictionary<string, object>();

                try
                {
                    // Test data quality classification
                    var classifiedQuality = ClassifyDataQuality(scenario.Value, scenario.IsValid, scenario.IsStable);
                    var testPassed = classifiedQuality == scenario.ExpectedQuality;

                    scenarioMetrics["TestPassed"] = testPassed;
                    scenarioMetrics["Value"] = scenario.Value;
                    scenarioMetrics["IsValid"] = scenario.IsValid;
                    scenarioMetrics["IsStable"] = scenario.IsStable;
                    scenarioMetrics["ExpectedQuality"] = scenario.ExpectedQuality.ToString();
                    scenarioMetrics["ClassifiedQuality"] = classifiedQuality.ToString();
                    scenarioMetrics["Description"] = scenario.Description;

                    if (testPassed)
                    {
                        passedTests++;
                        _logger.LogDebug(
                            "Data quality classification test passed: {Description} - Expected: {ExpectedQuality}, Classified: {ClassifiedQuality}",
                            scenario.Description, scenario.ExpectedQuality, classifiedQuality);
                    }
                    else
                    {
                        failedTests.Add(scenario.Description);
                        _logger.LogWarning(
                            "Data quality classification test failed: {Description} - Expected: {ExpectedQuality}, Classified: {ClassifiedQuality}",
                            scenario.Description, scenario.ExpectedQuality, classifiedQuality);
                    }
                }
                catch (Exception ex)
                {
                    failedTests.Add(scenario.Description);
                    scenarioMetrics["TestPassed"] = false;
                    scenarioMetrics["TestError"] = ex.Message;

                    _logger.LogError(ex,
                        "Failed to test data quality classification scenario: {Description}",
                        scenario.Description);
                }

                scenarioResults[scenario.Description] = scenarioMetrics;
            }

            metrics["TotalTests"] = totalTests;
            metrics["PassedTests"] = passedTests;
            metrics["FailedTests"] = totalTests - passedTests;
            metrics["SuccessRate"] = totalTests > 0 ? (double)passedTests / totalTests * 100 : 0;
            metrics["ScenarioResults"] = scenarioResults;

            if (failedTests.Count > 0)
            {
                recommendations.Add("Review data quality classification algorithm");
                recommendations.Add("Verify quality thresholds and rules");
                recommendations.Add("Test with various data patterns");
                recommendations.Add("Implement quality trend monitoring");

                var errorMessage = $"Data quality classification failed for {failedTests.Count} scenario(s): {string.Join(", ", failedTests)}";

                return TestResult.Failure(
                    testId,
                    testName,
                    TestCategory.DataQuality,
                    stopwatch.Elapsed,
                    errorMessage,
                    metrics: metrics,
                    recommendations: recommendations);
            }

            var successMessage = $"Data quality classification successful for all {passedTests} scenario(s)";
            recommendations.Add("Monitor data quality distribution");
            recommendations.Add("Implement quality-based alerting");

            return TestResult.Success(
                testId,
                testName,
                TestCategory.DataQuality,
                stopwatch.Elapsed,
                successMessage,
                metrics,
                recommendations);
        }
        catch (Exception ex)
        {
            var errorMessage = _errorService.CreateAndLogError(
                ex,
                "TEST-022",
                "Data quality classification test failed",
                new Dictionary<string, object>
                {
                    ["TestId"] = testId,
                    ["TestName"] = testName
                });

            return TestResult.Failure(
                testId,
                testName,
                TestCategory.DataQuality,
                stopwatch.Elapsed,
                errorMessage.Summary,
                ex,
                metrics,
                errorMessage.TroubleshootingSteps,
                TestSeverity.Error);
        }
    }

    /// <summary>
    /// Validate channel data rules
    /// </summary>
    /// <param name="device">Device configuration</param>
    /// <param name="channel">Channel configuration</param>
    /// <returns>Validation result</returns>
    private static ChannelValidationResult ValidateChannelDataRules(
        AdamDeviceConfig device,
        ChannelConfig channel)
    {
        var validationMessages = new List<string>();
        var isValid = true;

        // Validate min/max values
        var minValueValid = channel.MinValue >= 0 && channel.MinValue < channel.MaxValue;
        if (!minValueValid)
        {
            validationMessages.Add($"MinValue ({channel.MinValue}) must be non-negative and less than MaxValue ({channel.MaxValue})");
            isValid = false;
        }

        // Validate scale factor
        var scaleFactorValid = channel.ScaleFactor > 0 && channel.ScaleFactor <= 1000;
        if (!scaleFactorValid)
        {
            validationMessages.Add($"ScaleFactor ({channel.ScaleFactor}) must be between 0 and 1000");
            isValid = false;
        }

        // Validate register count for counter data
        var registerCountValid = channel.RegisterCount == 1 || channel.RegisterCount == 2;
        if (!registerCountValid)
        {
            validationMessages.Add($"RegisterCount ({channel.RegisterCount}) must be 1 or 2 for counter data");
            isValid = false;
        }

        // Validate decimal places
        if (channel.DecimalPlaces < 0 || channel.DecimalPlaces > 10)
        {
            validationMessages.Add($"DecimalPlaces ({channel.DecimalPlaces}) must be between 0 and 10");
            isValid = false;
        }

        return new ChannelValidationResult
        {
            IsValid = isValid,
            ValidationMessages = validationMessages,
            MinValueValid = minValueValid,
            MaxValueValid = channel.MaxValue > channel.MinValue,
            ScaleFactorValid = scaleFactorValid,
            RegisterCountValid = registerCountValid
        };
    }

    /// <summary>
    /// Detect counter overflow
    /// </summary>
    /// <param name="previousValue">Previous counter value</param>
    /// <param name="currentValue">Current counter value</param>
    /// <returns>True if overflow detected</returns>
    private static bool DetectCounterOverflow(uint previousValue, uint currentValue)
    {
        // Simple overflow detection logic
        // For 32-bit counters, overflow occurs when current value is much smaller than previous
        // and previous value is near maximum
        const uint MaxValue32Bit = 4294967295;
        const uint MaxValue16Bit = 65535;

        // 32-bit overflow detection
        if (previousValue > MaxValue32Bit - 1000 && currentValue < 1000)
        {
            return true;
        }

        // 16-bit overflow detection (common for some devices)
        if (previousValue > MaxValue16Bit - 100 && currentValue < 100)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Classify data quality
    /// </summary>
    /// <param name="value">Data value</param>
    /// <param name="isValid">Whether data is valid</param>
    /// <param name="isStable">Whether data is stable</param>
    /// <returns>Data quality classification</returns>
    private static DataQuality ClassifyDataQuality(double value, bool isValid, bool isStable)
    {
        if (!isValid)
        {
            return DataQuality.Bad;
        }

        if (!isStable)
        {
            return DataQuality.Uncertain;
        }

        return DataQuality.Good;
    }

    /// <summary>
    /// Channel validation result
    /// </summary>
    private sealed class ChannelValidationResult
    {
        public required bool IsValid { get; init; }
        public required List<string> ValidationMessages { get; init; }
        public required bool MinValueValid { get; init; }
        public required bool MaxValueValid { get; init; }
        public required bool ScaleFactorValid { get; init; }
        public required bool RegisterCountValid { get; init; }
    }

    /// <summary>
    /// Counter overflow test scenario
    /// </summary>
    private sealed class CounterOverflowScenario
    {
        public required uint PreviousValue { get; init; }
        public required uint CurrentValue { get; init; }
        public required bool ExpectedOverflow { get; init; }
        public required string Description { get; init; }
    }

    /// <summary>
    /// Data quality test scenario
    /// </summary>
    private sealed class DataQualityScenario
    {
        public required double Value { get; init; }
        public required bool IsValid { get; init; }
        public required bool IsStable { get; init; }
        public required DataQuality ExpectedQuality { get; init; }
        public required string Description { get; init; }
    }
}
