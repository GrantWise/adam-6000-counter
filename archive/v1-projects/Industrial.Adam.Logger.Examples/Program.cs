// Industrial.Adam.Logger.Examples - Simple Console Demo
// This demonstrates basic usage of the ADAM Logger library
// For comprehensive examples, see EXAMPLES.md in the csharp folder

using System.Collections;
using Industrial.Adam.Logger.Configuration;
using Industrial.Adam.Logger.ErrorHandling;
using Industrial.Adam.Logger.Extensions;
using Industrial.Adam.Logger.Interfaces;
using Industrial.Adam.Logger.Logging;
using Industrial.Adam.Logger.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Logger.Examples;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("ADAM-6051 InfluxDB Logger - Console Demo");
        Console.WriteLine("========================================");
        Console.WriteLine();

        // Check for test mode
        var isTestMode = args.Contains("--test") || args.Contains("-t");
        if (isTestMode)
        {
            await RunTestModeAsync(args);
            return;
        }

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Add ADAM Logger service with structured logging
                services.AddAdamLoggerWithStructuredLogging(context.Configuration, config =>
                {
                    // Set sensible defaults
                    config.PollIntervalMs = 2000;
                    config.HealthCheckIntervalMs = 10000;
                    config.MaxConcurrentDevices = 1;

                    // Configure InfluxDB with environment-aware URL
                    var isProduction = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") == "Production";
                    config.InfluxDb = new InfluxDbConfig
                    {
                        Url = isProduction ? "http://influxdb:8086" : "http://localhost:8086",
                        Token = "adam-super-secret-token",
                        Organization = "adam_org",
                        Bucket = "adam_counters",
                        Measurement = "counter_data",
                        WriteBatchSize = 50,
                        FlushIntervalMs = 5000
                    };

                    // Add demo device for local development
                    if (!isProduction)
                    {
                        config.DemoMode = true;
                        config.Devices.Add(new AdamDeviceConfig
                        {
                            DeviceId = "DEMO_ADAM_001",
                            IpAddress = "127.0.0.1",
                            Port = 502,
                            UnitId = 1,
                            TimeoutMs = 2000,
                            MaxRetries = 1,
                            Channels = new List<ChannelConfig>
                            {
                                new()
                                {
                                    ChannelNumber = 0,
                                    Name = "DemoCounter",
                                    StartRegister = 0,
                                    RegisterCount = 2,
                                    Enabled = true,
                                    MinValue = 0,
                                    MaxValue = 4294967295,
                                    ScaleFactor = 1.0,
                                    Offset = 0.0,
                                    DecimalPlaces = 0
                                }
                            }
                        });
                    }
                });

                // Apply environment variable overrides (clean approach)
                services.PostConfigure<AdamLoggerConfig>(config =>
                {
                    context.Configuration.GetSection("AdamLogger").Bind(config);

                    // If demo mode is enabled via environment variable, clear any real devices and add demo device
                    if (config.DemoMode && !config.Devices.Any(d => d.DeviceId.StartsWith("DEMO_")))
                    {
                        config.Devices.Clear();
                        config.Devices.Add(new AdamDeviceConfig
                        {
                            DeviceId = "DEMO_ADAM_001",
                            IpAddress = "127.0.0.1",
                            Port = 502,
                            UnitId = 1,
                            TimeoutMs = 2000,
                            MaxRetries = 1,
                            Channels = new List<ChannelConfig>
                            {
                                new()
                                {
                                    ChannelNumber = 0,
                                    Name = "DemoCounter",
                                    StartRegister = 0,
                                    RegisterCount = 2,
                                    Enabled = true,
                                    MinValue = 0,
                                    MaxValue = 4294967295,
                                    ScaleFactor = 1.0,
                                    Offset = 0.0,
                                    DecimalPlaces = 0
                                }
                            }
                        });
                    }
                });
            })
            .UseConsoleLifetime()
            .Build();

        // Get the ADAM Logger service
        var adamLogger = host.Services.GetRequiredService<IAdamLoggerService>();
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        var errorService = host.Services.GetRequiredService<IIndustrialErrorService>();

        Console.WriteLine("Starting ADAM Logger service...");
        Console.WriteLine("Press Ctrl+C to stop");
        Console.WriteLine();

        // Subscribe to data stream with structured logging
        var dataSubscription = adamLogger.DataStream.Subscribe(
            data =>
            {
                Console.WriteLine($"[{data.Timestamp:HH:mm:ss.fff}] {data.DeviceId} Ch{data.Channel}: " +
                                 $"Raw={data.RawValue}, Processed={data.ProcessedValue}, Quality={data.Quality}");
                if (data.Rate.HasValue)
                {
                    Console.WriteLine($"    Rate: {data.Rate.Value:F2} units/min");
                }

                // Log structured data processing event
                logger.LogDataProcessing(data.DeviceId, data.Channel, data.ProcessedValue, data.Quality.ToString());
            },
            error =>
            {
                // Use industrial error service for enhanced error handling
                var errorMessage = errorService.CreateAndLogError(
                    error,
                    "DATA-001",
                    "Data stream processing error",
                    new Dictionary<string, object>
                    {
                        ["StreamType"] = "DataStream",
                        ["SubscriptionActive"] = true
                    });

                Console.WriteLine($"[ERROR] {errorMessage.Summary}");
                Console.WriteLine($"  Troubleshooting: {errorMessage.TroubleshootingSteps.FirstOrDefault()}");
            });

        // Subscribe to health updates with structured logging
        var healthSubscription = adamLogger.HealthStream.Subscribe(
            health =>
            {
                Console.WriteLine($"[HEALTH] {health.DeviceId}: {health.Status} " +
                                 $"(Connected: {health.IsConnected}, " +
                                 $"Reads: {health.TotalReads}, " +
                                 $"Failures: {health.ConsecutiveFailures})");

                // Log structured health event
                logger.LogInformation(
                    "Health update: {DeviceId} {Status} (Connected: {IsConnected}, Reads: {TotalReads}, Failures: {ConsecutiveFailures})",
                    health.DeviceId, health.Status, health.IsConnected, health.TotalReads, health.ConsecutiveFailures);
            },
            error =>
            {
                // Use industrial error service for enhanced error handling
                var errorMessage = errorService.CreateAndLogError(
                    error,
                    "HLTH-001",
                    "Health stream monitoring error",
                    new Dictionary<string, object>
                    {
                        ["StreamType"] = "HealthStream",
                        ["SubscriptionActive"] = true
                    });

                Console.WriteLine($"[ERROR] {errorMessage.Summary}");
                Console.WriteLine($"  Troubleshooting: {errorMessage.TroubleshootingSteps.FirstOrDefault()}");
            });

        try
        {
            // Start the service and run until cancelled
            await host.RunAsync();
        }
        finally
        {
            Console.WriteLine("\nShutting down...");
            dataSubscription.Dispose();
            healthSubscription.Dispose();
        }
    }

    /// <summary>
    /// Run production test mode
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>Test execution task</returns>
    private static async Task RunTestModeAsync(string[] args)
    {
        Console.WriteLine("=== ADAM Logger Production Test Mode ===");
        Console.WriteLine();

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Add ADAM Logger services with structured logging
                services.AddAdamLoggerWithStructuredLogging(context.Configuration, config =>
                {
                    // Set sensible defaults for testing
                    config.PollIntervalMs = 2000;
                    config.HealthCheckIntervalMs = 10000;
                    config.MaxConcurrentDevices = 1;

                    // Configure InfluxDB with environment-aware URL
                    var isProduction = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") == "Production";
                    config.InfluxDb = new InfluxDbConfig
                    {
                        Url = isProduction ? "http://influxdb:8086" : "http://localhost:8086",
                        Token = "adam-super-secret-token",
                        Organization = "adam_org",
                        Bucket = "adam_counters",
                        Measurement = "counter_data",
                        WriteBatchSize = 50,
                        FlushIntervalMs = 5000
                    };

                    // Add demo device for testing
                    config.DemoMode = true;
                    config.Devices.Add(new AdamDeviceConfig
                    {
                        DeviceId = "TEST_ADAM_001",
                        IpAddress = "127.0.0.1",
                        Port = 502,
                        UnitId = 1,
                        TimeoutMs = 2000,
                        MaxRetries = 1,
                        Channels = new List<ChannelConfig>
                        {
                            new()
                            {
                                ChannelNumber = 0,
                                Name = "TestCounter",
                                StartRegister = 0,
                                RegisterCount = 2,
                                Enabled = true,
                                MinValue = 0,
                                MaxValue = 4294967295,
                                ScaleFactor = 1.0,
                                Offset = 0.0,
                                DecimalPlaces = 0
                            }
                        }
                    });
                });

                // Add testing services
                services.AddAdamLoggerTesting();
            })
            .UseConsoleLifetime()
            .Build();

        try
        {
            // Get test runner service
            var testRunner = host.Services.GetRequiredService<ITestRunner>();
            var logger = host.Services.GetRequiredService<ILogger<Program>>();

            // Parse test arguments
            var testCategory = ParseTestCategory(args);
            var testId = ParseTestId(args);
            var reportFormat = ParseReportFormat(args);

            if (!string.IsNullOrEmpty(testId))
            {
                // Run specific test
                Console.WriteLine($"Running test: {testId}");
                var testResult = await testRunner.RunTestAsync(testId);

                if (testResult.IsSuccess)
                {
                    await DisplayTestResultAsync(testRunner, new[] { testResult.Value }, reportFormat);
                }
                else
                {
                    Console.WriteLine($"Failed to run test {testId}: {testResult.ErrorMessage}");
                }
            }
            else if (testCategory.HasValue)
            {
                // Run tests by category
                Console.WriteLine($"Running {testCategory.Value} tests...");
                var testResults = await testRunner.RunTestsAsync(testCategory.Value);

                if (testResults.IsSuccess)
                {
                    await DisplayTestResultAsync(testRunner, testResults.Value, reportFormat);
                }
                else
                {
                    Console.WriteLine($"Failed to run {testCategory.Value} tests: {testResults.ErrorMessage}");
                }
            }
            else
            {
                // Run all tests
                Console.WriteLine("Running comprehensive test suite...");
                var testResults = await testRunner.RunAllTestsAsync();

                if (testResults.IsSuccess)
                {
                    await DisplayTestResultAsync(testRunner, testResults.Value, reportFormat);

                    // Also run production readiness validation
                    Console.WriteLine();
                    Console.WriteLine("=== Production Readiness Assessment ===");
                    var readinessResult = await testRunner.ValidateProductionReadinessAsync();

                    if (readinessResult.IsSuccess)
                    {
                        var readiness = readinessResult.Value;
                        Console.WriteLine($"Production Ready: {(readiness.IsReady ? "✅ YES" : "❌ NO")}");
                        Console.WriteLine($"Readiness Score: {readiness.ReadinessScore}/100");

                        if (readiness.CriticalIssues.Count > 0)
                        {
                            Console.WriteLine();
                            Console.WriteLine("CRITICAL ISSUES:");
                            foreach (var issue in readiness.CriticalIssues)
                            {
                                Console.WriteLine($"  ❌ {issue}");
                            }
                        }

                        if (readiness.Warnings.Count > 0)
                        {
                            Console.WriteLine();
                            Console.WriteLine("WARNINGS:");
                            foreach (var warning in readiness.Warnings)
                            {
                                Console.WriteLine($"  ⚠️  {warning}");
                            }
                        }

                        if (readiness.Recommendations.Count > 0)
                        {
                            Console.WriteLine();
                            Console.WriteLine("RECOMMENDATIONS:");
                            foreach (var recommendation in readiness.Recommendations.Take(5))
                            {
                                Console.WriteLine($"  💡 {recommendation}");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Failed to validate production readiness: {readinessResult.ErrorMessage}");
                    }
                }
                else
                {
                    Console.WriteLine($"Failed to run comprehensive test suite: {testResults.ErrorMessage}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test execution failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// Parse test category from command line arguments
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>Test category if specified</returns>
    private static Industrial.Adam.Logger.Testing.Models.TestCategory? ParseTestCategory(string[] args)
    {
        var categoryIndex = Array.IndexOf(args, "--category");
        if (categoryIndex == -1)
            categoryIndex = Array.IndexOf(args, "-c");

        if (categoryIndex != -1 && categoryIndex + 1 < args.Length)
        {
            var categoryName = args[categoryIndex + 1];
            if (Enum.TryParse<Industrial.Adam.Logger.Testing.Models.TestCategory>(categoryName, true, out var category))
            {
                return category;
            }
        }

        return null;
    }

    /// <summary>
    /// Parse specific test ID from command line arguments
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>Test ID if specified</returns>
    private static string? ParseTestId(string[] args)
    {
        var testIdIndex = Array.IndexOf(args, "--test-id");
        if (testIdIndex == -1)
            testIdIndex = Array.IndexOf(args, "-i");

        if (testIdIndex != -1 && testIdIndex + 1 < args.Length)
        {
            return args[testIdIndex + 1];
        }

        return null;
    }

    /// <summary>
    /// Parse report format from command line arguments
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>Report format</returns>
    private static ReportFormat ParseReportFormat(string[] args)
    {
        var formatIndex = Array.IndexOf(args, "--format");
        if (formatIndex == -1)
            formatIndex = Array.IndexOf(args, "-f");

        if (formatIndex != -1 && formatIndex + 1 < args.Length)
        {
            var formatName = args[formatIndex + 1];
            if (Enum.TryParse<ReportFormat>(formatName, true, out var format))
            {
                return format;
            }
        }

        return ReportFormat.Console;
    }

    /// <summary>
    /// Display test results in specified format
    /// </summary>
    /// <param name="testRunner">Test runner instance</param>
    /// <param name="testResults">Test results to display</param>
    /// <param name="format">Report format</param>
    /// <returns>Display task</returns>
    private static async Task DisplayTestResultAsync(ITestRunner testRunner, IReadOnlyList<Industrial.Adam.Logger.Testing.Models.TestResult> testResults, ReportFormat format)
    {
        var reportResult = await testRunner.GenerateTestReportAsync(testResults, format);

        if (reportResult.IsSuccess)
        {
            Console.WriteLine();
            Console.WriteLine(reportResult.Value);
        }
        else
        {
            Console.WriteLine($"Failed to generate test report: {reportResult.ErrorMessage}");
        }
    }
}
