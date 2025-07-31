using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Industrial.Adam.Logger.ErrorHandling;
using Industrial.Adam.Logger.Interfaces;
using Industrial.Adam.Logger.Testing.Models;
using Industrial.Adam.Logger.Testing.Tests;
using Industrial.Adam.Logger.Utilities;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Logger.Testing;

/// <summary>
/// Production test runner for ADAM logger system validation
/// </summary>
public sealed class TestRunner : ITestRunner
{
    private readonly ILogger<TestRunner> _logger;
    private readonly IIndustrialErrorService _errorService;
    private readonly ConnectionTest _connectionTest;
    private readonly ConfigurationTest _configurationTest;
    private readonly DataQualityTest _dataQualityTest;
    private readonly PerformanceBenchmarkTest _performanceTest;

    /// <summary>
    /// Initialize test runner
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="errorService">Error service</param>
    /// <param name="connectionTest">Connection test</param>
    /// <param name="configurationTest">Configuration test</param>
    /// <param name="dataQualityTest">Data quality test</param>
    /// <param name="performanceTest">Performance test</param>
    public TestRunner(
        ILogger<TestRunner> logger,
        IIndustrialErrorService errorService,
        ConnectionTest connectionTest,
        ConfigurationTest configurationTest,
        DataQualityTest dataQualityTest,
        PerformanceBenchmarkTest performanceTest)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _errorService = errorService ?? throw new ArgumentNullException(nameof(errorService));
        _connectionTest = connectionTest ?? throw new ArgumentNullException(nameof(connectionTest));
        _configurationTest = configurationTest ?? throw new ArgumentNullException(nameof(configurationTest));
        _dataQualityTest = dataQualityTest ?? throw new ArgumentNullException(nameof(dataQualityTest));
        _performanceTest = performanceTest ?? throw new ArgumentNullException(nameof(performanceTest));
    }

    /// <summary>
    /// Execute all available tests
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of test results</returns>
    public async Task<OperationResult<IReadOnlyList<TestResult>>> RunAllTestsAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting comprehensive test suite execution");

            var testResults = new List<TestResult>();

            // Run all test categories
            var categories = Enum.GetValues<TestCategory>();
            foreach (var category in categories)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var categoryResult = await RunTestsAsync(category, cancellationToken);
                if (categoryResult.IsSuccess)
                {
                    testResults.AddRange(categoryResult.Value);
                }
                else
                {
                    _logger.LogError("Failed to run tests for category {Category}: {Error}",
                        category, categoryResult.ErrorMessage);
                }
            }

            _logger.LogInformation("Completed test suite execution in {Duration}ms. Results: {Passed} passed, {Failed} failed, {Warnings} warnings",
                stopwatch.ElapsedMilliseconds,
                testResults.Count(r => r.Status == TestStatus.Passed),
                testResults.Count(r => r.Status == TestStatus.Failed),
                testResults.Count(r => r.Status == TestStatus.Warning));

            return OperationResult<IReadOnlyList<TestResult>>.Success(
                testResults.AsReadOnly(),
                stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            var errorMessage = _errorService.CreateAndLogError(
                ex,
                "TEST-100",
                "Failed to execute comprehensive test suite",
                new Dictionary<string, object>
                {
                    ["TestDuration"] = stopwatch.ElapsedMilliseconds,
                    ["TestCount"] = "Unknown"
                });

            return OperationResult<IReadOnlyList<TestResult>>.Failure(
                errorMessage.OriginalException ?? ex,
                stopwatch.Elapsed,
                new Dictionary<string, object>());
        }
    }

    /// <summary>
    /// Execute tests by category
    /// </summary>
    /// <param name="category">Test category to run</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of test results</returns>
    public async Task<OperationResult<IReadOnlyList<TestResult>>> RunTestsAsync(TestCategory category, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Running tests for category: {Category}", category);

            var testResults = new List<TestResult>();

            switch (category)
            {
                case TestCategory.Connection:
                    testResults.AddRange(await RunConnectionTestsAsync(cancellationToken));
                    break;

                case TestCategory.Configuration:
                    testResults.AddRange(await RunConfigurationTestsAsync(cancellationToken));
                    break;

                case TestCategory.DataQuality:
                    testResults.AddRange(await RunDataQualityTestsAsync(cancellationToken));
                    break;

                case TestCategory.Performance:
                    testResults.AddRange(await RunPerformanceTestsAsync(cancellationToken));
                    break;

                case TestCategory.Health:
                    // Health tests would be implemented here
                    _logger.LogWarning("Health tests not yet implemented");
                    break;

                case TestCategory.Discovery:
                    // Discovery tests would be implemented here
                    _logger.LogWarning("Discovery tests not yet implemented");
                    break;

                case TestCategory.Integration:
                    // Integration tests would be implemented here
                    _logger.LogWarning("Integration tests not yet implemented");
                    break;

                default:
                    _logger.LogWarning("Unknown test category: {Category}", category);
                    break;
            }

            _logger.LogInformation("Completed {Category} tests in {Duration}ms. Results: {Passed} passed, {Failed} failed, {Warnings} warnings",
                category,
                stopwatch.ElapsedMilliseconds,
                testResults.Count(r => r.Status == TestStatus.Passed),
                testResults.Count(r => r.Status == TestStatus.Failed),
                testResults.Count(r => r.Status == TestStatus.Warning));

            return OperationResult<IReadOnlyList<TestResult>>.Success(
                testResults.AsReadOnly(),
                stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            var errorMessage = _errorService.CreateAndLogError(
                ex,
                "TEST-101",
                $"Failed to execute {category} tests",
                new Dictionary<string, object>
                {
                    ["Category"] = category.ToString(),
                    ["TestDuration"] = stopwatch.ElapsedMilliseconds
                });

            return OperationResult<IReadOnlyList<TestResult>>.Failure(
                errorMessage.OriginalException ?? ex,
                stopwatch.Elapsed,
                new Dictionary<string, object>());
        }
    }

    /// <summary>
    /// Execute a specific test by ID
    /// </summary>
    /// <param name="testId">Test identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test result</returns>
    public async Task<OperationResult<TestResult>> RunTestAsync(string testId, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Running individual test: {TestId}", testId);

            TestResult result = testId switch
            {
                "CONN-001" => await _connectionTest.TestNetworkConnectivityAsync(cancellationToken),
                "CONN-002" => await _connectionTest.TestModbusConnectivityAsync(cancellationToken),
                "CONN-003" => await _connectionTest.TestDeviceLatencyAsync(cancellationToken),
                "CONF-001" => await _configurationTest.ValidateConfigurationAsync(cancellationToken),
                "CONF-002" => await _configurationTest.ValidateInfluxDbConfigurationAsync(cancellationToken),
                "CONF-003" => await _configurationTest.ValidateDeviceConfigurationsAsync(cancellationToken),
                "DATA-001" => await _dataQualityTest.TestDataValidationAsync(cancellationToken),
                "DATA-002" => await _dataQualityTest.TestCounterOverflowHandlingAsync(cancellationToken),
                "DATA-003" => await _dataQualityTest.TestDataQualityClassificationAsync(cancellationToken),
                "PERF-001" => await _performanceTest.TestResourceUsageAsync(cancellationToken),
                "PERF-002" => await _performanceTest.TestDataThroughputAsync(cancellationToken),
                "PERF-003" => await _performanceTest.TestMemoryUsageAsync(cancellationToken),
                _ => TestResult.Failure(
                    testId,
                    "Unknown Test",
                    TestCategory.Integration,
                    stopwatch.Elapsed,
                    $"Test ID '{testId}' not found",
                    recommendations: new[] { "Verify test ID is correct", "Check available tests list" })
            };

            _logger.LogInformation("Completed test {TestId} in {Duration}ms with status: {Status}",
                testId, stopwatch.ElapsedMilliseconds, result.Status);

            return OperationResult<TestResult>.Success(result, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            var errorMessage = _errorService.CreateAndLogError(
                ex,
                "TEST-102",
                $"Failed to execute test {testId}",
                new Dictionary<string, object>
                {
                    ["TestId"] = testId,
                    ["TestDuration"] = stopwatch.ElapsedMilliseconds
                });

            return OperationResult<TestResult>.Failure(
                errorMessage.OriginalException ?? ex,
                stopwatch.Elapsed,
                new Dictionary<string, object>());
        }
    }

    /// <summary>
    /// Get list of available tests
    /// </summary>
    /// <returns>Available test information</returns>
    public OperationResult<IReadOnlyList<TestInfo>> GetAvailableTests()
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var tests = new List<TestInfo>
            {
                // Connection tests
                new()
                {
                    TestId = "CONN-001",
                    TestName = "Network Connectivity Test",
                    Description = "Test network connectivity to all configured devices",
                    Category = TestCategory.Connection,
                    EstimatedDuration = TimeSpan.FromSeconds(10),
                    RequiresDevice = true,
                    Dependencies = Array.Empty<string>()
                },
                new()
                {
                    TestId = "CONN-002",
                    TestName = "Modbus TCP Connection Test",
                    Description = "Test Modbus TCP connectivity to all configured devices",
                    Category = TestCategory.Connection,
                    EstimatedDuration = TimeSpan.FromSeconds(15),
                    RequiresDevice = true,
                    Dependencies = new[] { "CONN-001" }
                },
                new()
                {
                    TestId = "CONN-003",
                    TestName = "Device Response Latency Test",
                    Description = "Test device response time and latency",
                    Category = TestCategory.Connection,
                    EstimatedDuration = TimeSpan.FromSeconds(20),
                    RequiresDevice = true,
                    Dependencies = new[] { "CONN-001" }
                },

                // Configuration tests
                new()
                {
                    TestId = "CONF-001",
                    TestName = "Core Configuration Validation",
                    Description = "Validate core configuration settings",
                    Category = TestCategory.Configuration,
                    EstimatedDuration = TimeSpan.FromSeconds(5),
                    RequiresDevice = false,
                    Dependencies = Array.Empty<string>()
                },
                new()
                {
                    TestId = "CONF-002",
                    TestName = "InfluxDB Configuration Validation",
                    Description = "Validate InfluxDB configuration settings",
                    Category = TestCategory.Configuration,
                    EstimatedDuration = TimeSpan.FromSeconds(3),
                    RequiresDevice = false,
                    Dependencies = Array.Empty<string>()
                },
                new()
                {
                    TestId = "CONF-003",
                    TestName = "Device Configuration Validation",
                    Description = "Validate device configuration settings",
                    Category = TestCategory.Configuration,
                    EstimatedDuration = TimeSpan.FromSeconds(5),
                    RequiresDevice = false,
                    Dependencies = Array.Empty<string>()
                },

                // Data quality tests
                new()
                {
                    TestId = "DATA-001",
                    TestName = "Data Validation Rules Test",
                    Description = "Test data validation rules and constraints",
                    Category = TestCategory.DataQuality,
                    EstimatedDuration = TimeSpan.FromSeconds(8),
                    RequiresDevice = false,
                    Dependencies = new[] { "CONF-003" }
                },
                new()
                {
                    TestId = "DATA-002",
                    TestName = "Counter Overflow Detection Test",
                    Description = "Test counter overflow detection and handling",
                    Category = TestCategory.DataQuality,
                    EstimatedDuration = TimeSpan.FromSeconds(5),
                    RequiresDevice = false,
                    Dependencies = Array.Empty<string>()
                },
                new()
                {
                    TestId = "DATA-003",
                    TestName = "Data Quality Classification Test",
                    Description = "Test data quality classification",
                    Category = TestCategory.DataQuality,
                    EstimatedDuration = TimeSpan.FromSeconds(5),
                    RequiresDevice = false,
                    Dependencies = Array.Empty<string>()
                },

                // Performance tests
                new()
                {
                    TestId = "PERF-001",
                    TestName = "System Resource Usage Test",
                    Description = "Test system resource usage under load",
                    Category = TestCategory.Performance,
                    EstimatedDuration = TimeSpan.FromSeconds(15),
                    RequiresDevice = false,
                    Dependencies = Array.Empty<string>()
                },
                new()
                {
                    TestId = "PERF-002",
                    TestName = "Data Throughput Performance Test",
                    Description = "Test data throughput performance",
                    Category = TestCategory.Performance,
                    EstimatedDuration = TimeSpan.FromSeconds(12),
                    RequiresDevice = false,
                    Dependencies = Array.Empty<string>()
                },
                new()
                {
                    TestId = "PERF-003",
                    TestName = "Memory Usage Pattern Test",
                    Description = "Test memory usage patterns",
                    Category = TestCategory.Performance,
                    EstimatedDuration = TimeSpan.FromSeconds(10),
                    RequiresDevice = false,
                    Dependencies = Array.Empty<string>()
                }
            };

            return OperationResult<IReadOnlyList<TestInfo>>.Success(
                tests.AsReadOnly(),
                stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            var errorMessage = _errorService.CreateAndLogError(
                ex,
                "TEST-103",
                "Failed to retrieve available tests",
                new Dictionary<string, object>
                {
                    ["Operation"] = "GetAvailableTests"
                });

            return OperationResult<IReadOnlyList<TestInfo>>.Failure(
                errorMessage.OriginalException ?? ex,
                stopwatch.Elapsed,
                new Dictionary<string, object>());
        }
    }

    /// <summary>
    /// Validate production environment readiness
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Production readiness validation result</returns>
    public async Task<OperationResult<ProductionReadinessResult>> ValidateProductionReadinessAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting production readiness validation");

            // Run all tests and analyze results
            var allTestsResult = await RunAllTestsAsync(cancellationToken);
            if (!allTestsResult.IsSuccess)
            {
                return OperationResult<ProductionReadinessResult>.Failure(
                    new InvalidOperationException(allTestsResult.ErrorMessage),
                    stopwatch.Elapsed,
                    new Dictionary<string, object>());
            }

            var testResults = allTestsResult.Value;
            var criticalIssues = new List<string>();
            var warnings = new List<string>();
            var recommendations = new List<string>();

            // Analyze test results
            foreach (var test in testResults)
            {
                switch (test.Status)
                {
                    case TestStatus.Failed:
                        if (test.Severity == TestSeverity.Critical || test.Severity == TestSeverity.Error)
                        {
                            criticalIssues.Add($"{test.TestName}: {test.ErrorMessage}");
                        }
                        else
                        {
                            warnings.Add($"{test.TestName}: {test.ErrorMessage}");
                        }
                        break;

                    case TestStatus.Warning:
                        warnings.Add($"{test.TestName}: {test.SuccessMessage}");
                        break;
                }

                recommendations.AddRange(test.Recommendations);
            }

            // Calculate readiness score
            var totalTests = testResults.Count;
            var passedTests = testResults.Count(r => r.Status == TestStatus.Passed);
            var warningTests = testResults.Count(r => r.Status == TestStatus.Warning);
            var failedTests = testResults.Count(r => r.Status == TestStatus.Failed);

            var baseScore = (double)passedTests / totalTests * 100;
            var warningPenalty = warningTests * 5; // 5 points per warning
            var failurePenalty = failedTests * 20; // 20 points per failure
            var readinessScore = Math.Max(0, (int)(baseScore - warningPenalty - failurePenalty));

            // Determine if ready for production
            var isReady = criticalIssues.Count == 0 && readinessScore >= 80;

            var result = new ProductionReadinessResult
            {
                IsReady = isReady,
                ReadinessScore = readinessScore,
                CriticalIssues = criticalIssues.AsReadOnly(),
                Warnings = warnings.AsReadOnly(),
                Recommendations = recommendations.Distinct().ToList().AsReadOnly(),
                TestResults = testResults
            };

            _logger.LogInformation("Production readiness validation completed in {Duration}ms. Ready: {IsReady}, Score: {Score}",
                stopwatch.ElapsedMilliseconds, isReady, readinessScore);

            return OperationResult<ProductionReadinessResult>.Success(result, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            var errorMessage = _errorService.CreateAndLogError(
                ex,
                "TEST-104",
                "Failed to validate production readiness",
                new Dictionary<string, object>
                {
                    ["ValidationDuration"] = stopwatch.ElapsedMilliseconds
                });

            return OperationResult<ProductionReadinessResult>.Failure(
                errorMessage.OriginalException ?? ex,
                stopwatch.Elapsed,
                new Dictionary<string, object>());
        }
    }

    /// <summary>
    /// Generate test report in specified format
    /// </summary>
    /// <param name="testResults">Test results to include in report</param>
    /// <param name="format">Report format</param>
    /// <returns>Generated report content</returns>
    public Task<OperationResult<string>> GenerateTestReportAsync(IReadOnlyList<TestResult> testResults, ReportFormat format = ReportFormat.Console)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var reportContent = format switch
            {
                ReportFormat.Console => GenerateConsoleReport(testResults),
                ReportFormat.Json => GenerateJsonReport(testResults),
                ReportFormat.Markdown => GenerateMarkdownReport(testResults),
                ReportFormat.Html => GenerateHtmlReport(testResults),
                _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported report format")
            };

            return Task.FromResult(OperationResult<string>.Success(reportContent, stopwatch.Elapsed));
        }
        catch (Exception ex)
        {
            var errorMessage = _errorService.CreateAndLogError(
                ex,
                "TEST-105",
                "Failed to generate test report",
                new Dictionary<string, object>
                {
                    ["Format"] = format.ToString(),
                    ["TestCount"] = testResults.Count,
                    ["ReportDuration"] = stopwatch.ElapsedMilliseconds
                });

            return Task.FromResult(OperationResult<string>.Failure(
                ex,
                stopwatch.Elapsed,
                new Dictionary<string, object>()));
        }
    }

    /// <summary>
    /// Run connection tests
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Connection test results</returns>
    private async Task<List<TestResult>> RunConnectionTestsAsync(CancellationToken cancellationToken)
    {
        var results = new List<TestResult>();

        try
        {
            results.Add(await _connectionTest.TestNetworkConnectivityAsync(cancellationToken));
            results.Add(await _connectionTest.TestModbusConnectivityAsync(cancellationToken));
            results.Add(await _connectionTest.TestDeviceLatencyAsync(cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run connection tests");
        }

        return results;
    }

    /// <summary>
    /// Run configuration tests
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Configuration test results</returns>
    private async Task<List<TestResult>> RunConfigurationTestsAsync(CancellationToken cancellationToken)
    {
        var results = new List<TestResult>();

        try
        {
            results.Add(await _configurationTest.ValidateConfigurationAsync(cancellationToken));
            results.Add(await _configurationTest.ValidateInfluxDbConfigurationAsync(cancellationToken));
            results.Add(await _configurationTest.ValidateDeviceConfigurationsAsync(cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run configuration tests");
        }

        return results;
    }

    /// <summary>
    /// Run data quality tests
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Data quality test results</returns>
    private async Task<List<TestResult>> RunDataQualityTestsAsync(CancellationToken cancellationToken)
    {
        var results = new List<TestResult>();

        try
        {
            results.Add(await _dataQualityTest.TestDataValidationAsync(cancellationToken));
            results.Add(await _dataQualityTest.TestCounterOverflowHandlingAsync(cancellationToken));
            results.Add(await _dataQualityTest.TestDataQualityClassificationAsync(cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run data quality tests");
        }

        return results;
    }

    /// <summary>
    /// Run performance tests
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Performance test results</returns>
    private async Task<List<TestResult>> RunPerformanceTestsAsync(CancellationToken cancellationToken)
    {
        var results = new List<TestResult>();

        try
        {
            results.Add(await _performanceTest.TestResourceUsageAsync(cancellationToken));
            results.Add(await _performanceTest.TestDataThroughputAsync(cancellationToken));
            results.Add(await _performanceTest.TestMemoryUsageAsync(cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run performance tests");
        }

        return results;
    }

    /// <summary>
    /// Generate console-friendly report
    /// </summary>
    /// <param name="testResults">Test results</param>
    /// <returns>Console report</returns>
    private static string GenerateConsoleReport(IReadOnlyList<TestResult> testResults)
    {
        var report = new StringBuilder();

        report.AppendLine("=== ADAM Logger Production Test Report ===");
        report.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        report.AppendLine();

        // Summary
        var passed = testResults.Count(r => r.Status == TestStatus.Passed);
        var failed = testResults.Count(r => r.Status == TestStatus.Failed);
        var warnings = testResults.Count(r => r.Status == TestStatus.Warning);
        var total = testResults.Count;

        report.AppendLine("=== TEST SUMMARY ===");
        report.AppendLine($"Total Tests: {total}");
        report.AppendLine($"Passed: {passed} ({passed * 100.0 / total:F1}%)");
        report.AppendLine($"Failed: {failed} ({failed * 100.0 / total:F1}%)");
        report.AppendLine($"Warnings: {warnings} ({warnings * 100.0 / total:F1}%)");
        report.AppendLine();

        // Group by category
        var categories = testResults.GroupBy(r => r.Category);
        foreach (var category in categories)
        {
            report.AppendLine($"=== {category.Key.ToString().ToUpper()} TESTS ===");

            foreach (var test in category)
            {
                var statusIcon = test.Status switch
                {
                    TestStatus.Passed => "✓",
                    TestStatus.Failed => "✗",
                    TestStatus.Warning => "⚠",
                    TestStatus.Skipped => "○",
                    _ => "?"
                };

                report.AppendLine($"{statusIcon} {test.TestName} ({test.Duration.TotalMilliseconds:F0}ms)");

                if (!string.IsNullOrEmpty(test.SuccessMessage))
                {
                    report.AppendLine($"  Success: {test.SuccessMessage}");
                }

                if (!string.IsNullOrEmpty(test.ErrorMessage))
                {
                    report.AppendLine($"  Error: {test.ErrorMessage}");
                }

                if (test.Recommendations.Count > 0)
                {
                    report.AppendLine($"  Recommendations: {string.Join("; ", test.Recommendations)}");
                }

                report.AppendLine();
            }
        }

        return report.ToString();
    }

    /// <summary>
    /// Generate JSON report
    /// </summary>
    /// <param name="testResults">Test results</param>
    /// <returns>JSON report</returns>
    private static string GenerateJsonReport(IReadOnlyList<TestResult> testResults)
    {
        var report = new
        {
            GeneratedAt = DateTime.UtcNow,
            Summary = new
            {
                TotalTests = testResults.Count,
                Passed = testResults.Count(r => r.Status == TestStatus.Passed),
                Failed = testResults.Count(r => r.Status == TestStatus.Failed),
                Warnings = testResults.Count(r => r.Status == TestStatus.Warning),
                Skipped = testResults.Count(r => r.Status == TestStatus.Skipped)
            },
            TestResults = testResults.Select(r => new
            {
                r.TestId,
                r.TestName,
                Category = r.Category.ToString(),
                Status = r.Status.ToString(),
                Severity = r.Severity.ToString(),
                Duration = r.Duration.TotalMilliseconds,
                r.ExecutedAt,
                r.SuccessMessage,
                r.ErrorMessage,
                r.Recommendations,
                r.Metrics,
                r.DeviceId
            })
        };

        return JsonSerializer.Serialize(report, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    /// <summary>
    /// Generate Markdown report
    /// </summary>
    /// <param name="testResults">Test results</param>
    /// <returns>Markdown report</returns>
    private static string GenerateMarkdownReport(IReadOnlyList<TestResult> testResults)
    {
        var report = new StringBuilder();

        report.AppendLine("# ADAM Logger Production Test Report");
        report.AppendLine($"**Generated:** {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        report.AppendLine();

        // Summary
        var passed = testResults.Count(r => r.Status == TestStatus.Passed);
        var failed = testResults.Count(r => r.Status == TestStatus.Failed);
        var warnings = testResults.Count(r => r.Status == TestStatus.Warning);
        var total = testResults.Count;

        report.AppendLine("## Test Summary");
        report.AppendLine($"- **Total Tests:** {total}");
        report.AppendLine($"- **Passed:** {passed} ({passed * 100.0 / total:F1}%)");
        report.AppendLine($"- **Failed:** {failed} ({failed * 100.0 / total:F1}%)");
        report.AppendLine($"- **Warnings:** {warnings} ({warnings * 100.0 / total:F1}%)");
        report.AppendLine();

        // Group by category
        var categories = testResults.GroupBy(r => r.Category);
        foreach (var category in categories)
        {
            report.AppendLine($"## {category.Key} Tests");

            foreach (var test in category)
            {
                var statusIcon = test.Status switch
                {
                    TestStatus.Passed => "✅",
                    TestStatus.Failed => "❌",
                    TestStatus.Warning => "⚠️",
                    TestStatus.Skipped => "⚪",
                    _ => "❓"
                };

                report.AppendLine($"### {statusIcon} {test.TestName}");
                report.AppendLine($"- **Duration:** {test.Duration.TotalMilliseconds:F0}ms");
                report.AppendLine($"- **Status:** {test.Status}");

                if (!string.IsNullOrEmpty(test.SuccessMessage))
                {
                    report.AppendLine($"- **Result:** {test.SuccessMessage}");
                }

                if (!string.IsNullOrEmpty(test.ErrorMessage))
                {
                    report.AppendLine($"- **Error:** {test.ErrorMessage}");
                }

                if (test.Recommendations.Count > 0)
                {
                    report.AppendLine("- **Recommendations:**");
                    foreach (var rec in test.Recommendations)
                    {
                        report.AppendLine($"  - {rec}");
                    }
                }

                report.AppendLine();
            }
        }

        return report.ToString();
    }

    /// <summary>
    /// Generate HTML report
    /// </summary>
    /// <param name="testResults">Test results</param>
    /// <returns>HTML report</returns>
    private static string GenerateHtmlReport(IReadOnlyList<TestResult> testResults)
    {
        var report = new StringBuilder();

        report.AppendLine("<!DOCTYPE html>");
        report.AppendLine("<html>");
        report.AppendLine("<head>");
        report.AppendLine("<title>ADAM Logger Test Report</title>");
        report.AppendLine("<style>");
        report.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
        report.AppendLine(".passed { color: green; }");
        report.AppendLine(".failed { color: red; }");
        report.AppendLine(".warning { color: orange; }");
        report.AppendLine(".skipped { color: gray; }");
        report.AppendLine("table { border-collapse: collapse; width: 100%; }");
        report.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
        report.AppendLine("th { background-color: #f2f2f2; }");
        report.AppendLine("</style>");
        report.AppendLine("</head>");
        report.AppendLine("<body>");

        report.AppendLine("<h1>ADAM Logger Production Test Report</h1>");
        report.AppendLine($"<p><strong>Generated:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>");

        // Summary
        var passed = testResults.Count(r => r.Status == TestStatus.Passed);
        var failed = testResults.Count(r => r.Status == TestStatus.Failed);
        var warnings = testResults.Count(r => r.Status == TestStatus.Warning);
        var total = testResults.Count;

        report.AppendLine("<h2>Test Summary</h2>");
        report.AppendLine("<table>");
        report.AppendLine("<tr><th>Status</th><th>Count</th><th>Percentage</th></tr>");
        report.AppendLine($"<tr><td>Total</td><td>{total}</td><td>100.0%</td></tr>");
        report.AppendLine($"<tr class=\"passed\"><td>Passed</td><td>{passed}</td><td>{passed * 100.0 / total:F1}%</td></tr>");
        report.AppendLine($"<tr class=\"failed\"><td>Failed</td><td>{failed}</td><td>{failed * 100.0 / total:F1}%</td></tr>");
        report.AppendLine($"<tr class=\"warning\"><td>Warnings</td><td>{warnings}</td><td>{warnings * 100.0 / total:F1}%</td></tr>");
        report.AppendLine("</table>");

        // Detailed results
        report.AppendLine("<h2>Detailed Results</h2>");
        report.AppendLine("<table>");
        report.AppendLine("<tr><th>Test</th><th>Category</th><th>Status</th><th>Duration</th><th>Result</th></tr>");

        foreach (var test in testResults)
        {
            var statusClass = test.Status.ToString().ToLower();
            var resultText = test.Status == TestStatus.Passed ? test.SuccessMessage : test.ErrorMessage;

            report.AppendLine($"<tr class=\"{statusClass}\">");
            report.AppendLine($"<td>{test.TestName}</td>");
            report.AppendLine($"<td>{test.Category}</td>");
            report.AppendLine($"<td>{test.Status}</td>");
            report.AppendLine($"<td>{test.Duration.TotalMilliseconds:F0}ms</td>");
            report.AppendLine($"<td>{resultText}</td>");
            report.AppendLine("</tr>");
        }

        report.AppendLine("</table>");
        report.AppendLine("</body>");
        report.AppendLine("</html>");

        return report.ToString();
    }
}
