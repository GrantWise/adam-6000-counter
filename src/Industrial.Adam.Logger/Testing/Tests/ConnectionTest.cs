using System.Diagnostics;
using System.Net.NetworkInformation;
using Industrial.Adam.Logger.Configuration;
using Industrial.Adam.Logger.ErrorHandling;
using Industrial.Adam.Logger.Interfaces;
using Industrial.Adam.Logger.Testing.Models;
using Industrial.Adam.Logger.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Industrial.Adam.Logger.Testing.Tests;

/// <summary>
/// Connection validation tests for ADAM devices
/// </summary>
public sealed class ConnectionTest
{
    private readonly ILogger<ConnectionTest> _logger;
    private readonly IOptions<AdamLoggerConfig> _config;
    private readonly IIndustrialErrorService _errorService;

    /// <summary>
    /// Initialize connection test
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="config">Configuration options</param>
    /// <param name="errorService">Error service</param>
    public ConnectionTest(
        ILogger<ConnectionTest> logger,
        IOptions<AdamLoggerConfig> config,
        IIndustrialErrorService errorService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _errorService = errorService ?? throw new ArgumentNullException(nameof(errorService));
    }

    /// <summary>
    /// Test network connectivity to all configured devices
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Connection test result</returns>
    public async Task<TestResult> TestNetworkConnectivityAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var testId = "CONN-001";
        var testName = "Network Connectivity Test";
        var metrics = new Dictionary<string, object>();
        var recommendations = new List<string>();

        try
        {
            var devices = _config.Value.Devices;
            if (devices.Count == 0)
            {
                return TestResult.Warning(
                    testId,
                    testName,
                    TestCategory.Connection,
                    stopwatch.Elapsed,
                    "No devices configured for testing",
                    metrics,
                    new[] { "Add device configurations to enable connectivity testing" });
            }

            var totalTests = devices.Count;
            var successfulTests = 0;
            var failedDevices = new List<string>();

            metrics["TotalDevices"] = totalTests;
            metrics["TestedDevices"] = new Dictionary<string, object>();

            foreach (var device in devices)
            {
                var deviceMetrics = new Dictionary<string, object>();

                try
                {
                    // Test ping connectivity
                    using var ping = new Ping();
                    var pingResult = await ping.SendPingAsync(device.IpAddress, 5000);

                    deviceMetrics["PingSuccess"] = pingResult.Status == IPStatus.Success;
                    deviceMetrics["PingRoundTripTime"] = pingResult.RoundtripTime;
                    deviceMetrics["PingStatus"] = pingResult.Status.ToString();

                    if (pingResult.Status == IPStatus.Success)
                    {
                        successfulTests++;
                        _logger.LogInformation(
                            "Device {DeviceId} at {IpAddress} responded to ping in {RoundTripTime}ms",
                            device.DeviceId, device.IpAddress, pingResult.RoundtripTime);
                    }
                    else
                    {
                        failedDevices.Add($"{device.DeviceId} ({device.IpAddress})");
                        _logger.LogWarning(
                            "Device {DeviceId} at {IpAddress} failed ping test: {Status}",
                            device.DeviceId, device.IpAddress, pingResult.Status);
                    }
                }
                catch (Exception ex)
                {
                    failedDevices.Add($"{device.DeviceId} ({device.IpAddress})");
                    deviceMetrics["PingSuccess"] = false;
                    deviceMetrics["PingError"] = ex.Message;

                    _logger.LogError(ex,
                        "Failed to ping device {DeviceId} at {IpAddress}",
                        device.DeviceId, device.IpAddress);
                }

                ((Dictionary<string, object>)metrics["TestedDevices"])[device.DeviceId] = deviceMetrics;
            }

            metrics["SuccessfulTests"] = successfulTests;
            metrics["FailedTests"] = totalTests - successfulTests;
            metrics["SuccessRate"] = (double)successfulTests / totalTests * 100;

            if (failedDevices.Count > 0)
            {
                recommendations.Add("Verify network connectivity for failed devices");
                recommendations.Add("Check firewall settings and network routing");
                recommendations.Add("Ensure devices are powered on and connected to network");

                var errorMessage = $"Network connectivity failed for {failedDevices.Count} device(s): {string.Join(", ", failedDevices)}";

                return TestResult.Failure(
                    testId,
                    testName,
                    TestCategory.Connection,
                    stopwatch.Elapsed,
                    errorMessage,
                    metrics: metrics,
                    recommendations: recommendations);
            }

            var successMessage = $"Network connectivity successful for all {successfulTests} device(s)";
            recommendations.Add("Monitor network latency regularly");
            recommendations.Add("Configure network monitoring alerts");

            return TestResult.Success(
                testId,
                testName,
                TestCategory.Connection,
                stopwatch.Elapsed,
                successMessage,
                metrics,
                recommendations);
        }
        catch (Exception ex)
        {
            var errorMessage = _errorService.CreateAndLogError(
                ex,
                "TEST-001",
                "Network connectivity test failed",
                new Dictionary<string, object>
                {
                    ["TestId"] = testId,
                    ["TestName"] = testName
                });

            return TestResult.Failure(
                testId,
                testName,
                TestCategory.Connection,
                stopwatch.Elapsed,
                errorMessage.Summary,
                ex,
                metrics,
                errorMessage.TroubleshootingSteps,
                TestSeverity.Error);
        }
    }

    /// <summary>
    /// Test Modbus TCP connectivity to all configured devices
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Modbus connection test result</returns>
    public async Task<TestResult> TestModbusConnectivityAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var testId = "CONN-002";
        var testName = "Modbus TCP Connection Test";
        var metrics = new Dictionary<string, object>();
        var recommendations = new List<string>();

        try
        {
            var devices = _config.Value.Devices;
            if (devices.Count == 0)
            {
                return TestResult.Warning(
                    testId,
                    testName,
                    TestCategory.Connection,
                    stopwatch.Elapsed,
                    "No devices configured for testing",
                    metrics,
                    new[] { "Add device configurations to enable Modbus testing" });
            }

            var totalTests = devices.Count;
            var successfulTests = 0;
            var failedDevices = new List<string>();

            metrics["TotalDevices"] = totalTests;
            metrics["TestedDevices"] = new Dictionary<string, object>();

            foreach (var device in devices)
            {
                var deviceMetrics = new Dictionary<string, object>();

                try
                {
                    // Test TCP socket connectivity to Modbus port
                    using var tcpClient = new System.Net.Sockets.TcpClient();
                    var connectTask = tcpClient.ConnectAsync(device.IpAddress, device.Port);
                    var timeoutTask = Task.Delay(device.TimeoutMs, cancellationToken);

                    var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                    if (completedTask == connectTask && tcpClient.Connected)
                    {
                        successfulTests++;
                        deviceMetrics["ModbusConnectSuccess"] = true;
                        deviceMetrics["ConnectionTime"] = stopwatch.ElapsedMilliseconds;

                        _logger.LogInformation(
                            "Device {DeviceId} Modbus TCP connection successful on {IpAddress}:{Port}",
                            device.DeviceId, device.IpAddress, device.Port);
                    }
                    else
                    {
                        failedDevices.Add($"{device.DeviceId} ({device.IpAddress}:{device.Port})");
                        deviceMetrics["ModbusConnectSuccess"] = false;
                        deviceMetrics["ConnectionError"] = completedTask == timeoutTask ? "Timeout" : "Connection failed";

                        _logger.LogWarning(
                            "Device {DeviceId} Modbus TCP connection failed on {IpAddress}:{Port}",
                            device.DeviceId, device.IpAddress, device.Port);
                    }
                }
                catch (Exception ex)
                {
                    failedDevices.Add($"{device.DeviceId} ({device.IpAddress}:{device.Port})");
                    deviceMetrics["ModbusConnectSuccess"] = false;
                    deviceMetrics["ConnectionError"] = ex.Message;

                    _logger.LogError(ex,
                        "Failed to connect to device {DeviceId} via Modbus TCP at {IpAddress}:{Port}",
                        device.DeviceId, device.IpAddress, device.Port);
                }

                ((Dictionary<string, object>)metrics["TestedDevices"])[device.DeviceId] = deviceMetrics;
            }

            metrics["SuccessfulTests"] = successfulTests;
            metrics["FailedTests"] = totalTests - successfulTests;
            metrics["SuccessRate"] = (double)successfulTests / totalTests * 100;

            if (failedDevices.Count > 0)
            {
                recommendations.Add("Verify Modbus TCP service is running on failed devices");
                recommendations.Add("Check device port configuration and firewall rules");
                recommendations.Add("Ensure devices are not in exclusive connection mode");
                recommendations.Add("Verify device network settings and IP addresses");

                var errorMessage = $"Modbus TCP connectivity failed for {failedDevices.Count} device(s): {string.Join(", ", failedDevices)}";

                return TestResult.Failure(
                    testId,
                    testName,
                    TestCategory.Connection,
                    stopwatch.Elapsed,
                    errorMessage,
                    metrics: metrics,
                    recommendations: recommendations);
            }

            var successMessage = $"Modbus TCP connectivity successful for all {successfulTests} device(s)";
            recommendations.Add("Monitor Modbus connection stability");
            recommendations.Add("Implement connection retry logic");

            return TestResult.Success(
                testId,
                testName,
                TestCategory.Connection,
                stopwatch.Elapsed,
                successMessage,
                metrics,
                recommendations);
        }
        catch (Exception ex)
        {
            var errorMessage = _errorService.CreateAndLogError(
                ex,
                "TEST-002",
                "Modbus TCP connectivity test failed",
                new Dictionary<string, object>
                {
                    ["TestId"] = testId,
                    ["TestName"] = testName
                });

            return TestResult.Failure(
                testId,
                testName,
                TestCategory.Connection,
                stopwatch.Elapsed,
                errorMessage.Summary,
                ex,
                metrics,
                errorMessage.TroubleshootingSteps,
                TestSeverity.Error);
        }
    }

    /// <summary>
    /// Test device response time and latency
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Latency test result</returns>
    public async Task<TestResult> TestDeviceLatencyAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var testId = "CONN-003";
        var testName = "Device Response Latency Test";
        var metrics = new Dictionary<string, object>();
        var recommendations = new List<string>();

        try
        {
            var devices = _config.Value.Devices;
            if (devices.Count == 0)
            {
                return TestResult.Warning(
                    testId,
                    testName,
                    TestCategory.Connection,
                    stopwatch.Elapsed,
                    "No devices configured for testing",
                    metrics,
                    new[] { "Add device configurations to enable latency testing" });
            }

            var totalLatency = 0.0;
            var deviceCount = 0;
            var highLatencyDevices = new List<string>();
            const double MaxAcceptableLatency = 1000.0; // 1 second

            metrics["TestedDevices"] = new Dictionary<string, object>();

            foreach (var device in devices)
            {
                var deviceMetrics = new Dictionary<string, object>();

                try
                {
                    // Test multiple ping samples for accurate latency measurement
                    using var ping = new Ping();
                    var latencies = new List<long>();

                    for (int i = 0; i < 5; i++)
                    {
                        var pingResult = await ping.SendPingAsync(device.IpAddress, 3000);
                        if (pingResult.Status == IPStatus.Success)
                        {
                            latencies.Add(pingResult.RoundtripTime);
                        }
                    }

                    if (latencies.Count > 0)
                    {
                        var avgLatency = latencies.Average();
                        var minLatency = latencies.Min();
                        var maxLatency = latencies.Max();

                        deviceMetrics["AverageLatency"] = avgLatency;
                        deviceMetrics["MinLatency"] = minLatency;
                        deviceMetrics["MaxLatency"] = maxLatency;
                        deviceMetrics["LatencyStandardDeviation"] = CalculateStandardDeviation(latencies);

                        totalLatency += avgLatency;
                        deviceCount++;

                        if (avgLatency > MaxAcceptableLatency)
                        {
                            highLatencyDevices.Add($"{device.DeviceId} ({avgLatency:F1}ms)");
                        }

                        _logger.LogInformation(
                            "Device {DeviceId} average latency: {AverageLatency:F1}ms (min: {MinLatency}ms, max: {MaxLatency}ms)",
                            device.DeviceId, avgLatency, minLatency, maxLatency);
                    }
                    else
                    {
                        deviceMetrics["AverageLatency"] = -1;
                        deviceMetrics["LatencyTestFailed"] = true;

                        _logger.LogWarning(
                            "Device {DeviceId} failed all latency tests",
                            device.DeviceId);
                    }
                }
                catch (Exception ex)
                {
                    deviceMetrics["AverageLatency"] = -1;
                    deviceMetrics["LatencyTestError"] = ex.Message;

                    _logger.LogError(ex,
                        "Failed to test latency for device {DeviceId}",
                        device.DeviceId);
                }

                ((Dictionary<string, object>)metrics["TestedDevices"])[device.DeviceId] = deviceMetrics;
            }

            var overallAverageLatency = deviceCount > 0 ? totalLatency / deviceCount : 0;
            metrics["OverallAverageLatency"] = overallAverageLatency;
            metrics["TestedDeviceCount"] = deviceCount;
            metrics["HighLatencyDeviceCount"] = highLatencyDevices.Count;

            if (highLatencyDevices.Count > 0)
            {
                recommendations.Add("Investigate network congestion for high-latency devices");
                recommendations.Add("Consider network optimization or device relocation");
                recommendations.Add("Monitor latency trends over time");

                var warningMessage = $"High latency detected for {highLatencyDevices.Count} device(s): {string.Join(", ", highLatencyDevices)}";

                return TestResult.Warning(
                    testId,
                    testName,
                    TestCategory.Connection,
                    stopwatch.Elapsed,
                    warningMessage,
                    metrics,
                    recommendations);
            }

            var successMessage = $"Device latency acceptable for all {deviceCount} device(s). Average: {overallAverageLatency:F1}ms";
            recommendations.Add("Monitor latency regularly for performance trends");
            recommendations.Add("Set up automated latency alerts");

            return TestResult.Success(
                testId,
                testName,
                TestCategory.Connection,
                stopwatch.Elapsed,
                successMessage,
                metrics,
                recommendations);
        }
        catch (Exception ex)
        {
            var errorMessage = _errorService.CreateAndLogError(
                ex,
                "TEST-003",
                "Device latency test failed",
                new Dictionary<string, object>
                {
                    ["TestId"] = testId,
                    ["TestName"] = testName
                });

            return TestResult.Failure(
                testId,
                testName,
                TestCategory.Connection,
                stopwatch.Elapsed,
                errorMessage.Summary,
                ex,
                metrics,
                errorMessage.TroubleshootingSteps,
                TestSeverity.Error);
        }
    }

    /// <summary>
    /// Calculate standard deviation for latency measurements
    /// </summary>
    /// <param name="values">Latency values</param>
    /// <returns>Standard deviation</returns>
    private static double CalculateStandardDeviation(List<long> values)
    {
        if (values.Count <= 1)
            return 0;

        var average = values.Average();
        var sumOfSquares = values.Select(val => (val - average) * (val - average)).Sum();
        return Math.Sqrt(sumOfSquares / (values.Count - 1));
    }
}
