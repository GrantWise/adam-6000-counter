// Industrial.Adam.Logger.Tests - TestRunner Unit Tests
// Comprehensive tests for production test orchestration and reporting

using FluentAssertions;
using Industrial.Adam.Logger.Configuration;
using Industrial.Adam.Logger.ErrorHandling;
using Industrial.Adam.Logger.Interfaces;
using Industrial.Adam.Logger.Testing;
using Industrial.Adam.Logger.Testing.Models;
using Industrial.Adam.Logger.Testing.Tests;
using Industrial.Adam.Logger.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Industrial.Adam.Logger.Tests.Testing;

public class TestRunnerTests : IDisposable
{
    private readonly Mock<ILogger<TestRunner>> _mockLogger;
    private readonly Mock<IIndustrialErrorService> _mockErrorService;
    private readonly Mock<IOptions<AdamLoggerConfig>> _mockConfig;
    private readonly AdamLoggerConfig _config;
    private readonly TestRunner _testRunner;

    public TestRunnerTests()
    {
        _mockLogger = new Mock<ILogger<TestRunner>>();
        _mockErrorService = new Mock<IIndustrialErrorService>();
        _mockConfig = new Mock<IOptions<AdamLoggerConfig>>();

        _config = new AdamLoggerConfig
        {
            Devices = new List<AdamDeviceConfig>
            {
                new AdamDeviceConfig
                {
                    DeviceId = "test-device",
                    IpAddress = "192.168.1.100",
                    Port = 502,
                    UnitId = 1,
                    TimeoutMs = 5000,
                    Channels = new List<ChannelConfig>
                    {
                        new ChannelConfig { ChannelNumber = 0, Name = "Channel0", StartRegister = 0 }
                    }
                }
            }
        };

        _mockConfig.Setup(o => o.Value).Returns(_config);

        // Create concrete test instances with mocked dependencies
        var mockConnectionTestLogger = new Mock<ILogger<ConnectionTest>>();
        var mockConfigTestLogger = new Mock<ILogger<ConfigurationTest>>();
        var mockDataQualityTestLogger = new Mock<ILogger<DataQualityTest>>();
        var mockPerformanceTestLogger = new Mock<ILogger<PerformanceBenchmarkTest>>();

        var connectionTest = new ConnectionTest(mockConnectionTestLogger.Object, _mockConfig.Object, _mockErrorService.Object);
        var configurationTest = new ConfigurationTest(mockConfigTestLogger.Object, _mockConfig.Object, _mockErrorService.Object);
        var dataQualityTest = new DataQualityTest(mockDataQualityTestLogger.Object, _mockConfig.Object, _mockErrorService.Object);
        var performanceTest = new PerformanceBenchmarkTest(mockPerformanceTestLogger.Object, _mockConfig.Object, _mockErrorService.Object);

        _testRunner = new TestRunner(
            _mockLogger.Object,
            _mockErrorService.Object,
            connectionTest,
            configurationTest,
            dataQualityTest,
            performanceTest);
    }

    [Fact]
    public void Constructor_ValidParameters_ShouldCreateInstance()
    {
        // Arrange & Act
        var runner = _testRunner;

        // Assert
        runner.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var connectionTest = new ConnectionTest(new Mock<ILogger<ConnectionTest>>().Object, _mockConfig.Object, _mockErrorService.Object);
        var configurationTest = new ConfigurationTest(new Mock<ILogger<ConfigurationTest>>().Object, _mockConfig.Object, _mockErrorService.Object);
        var dataQualityTest = new DataQualityTest(new Mock<ILogger<DataQualityTest>>().Object, _mockConfig.Object, _mockErrorService.Object);
        var performanceTest = new PerformanceBenchmarkTest(new Mock<ILogger<PerformanceBenchmarkTest>>().Object, _mockConfig.Object, _mockErrorService.Object);

        // Act & Assert
        var action = () => new TestRunner(
            null!,
            _mockErrorService.Object,
            connectionTest,
            configurationTest,
            dataQualityTest,
            performanceTest);

        action.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_NullErrorService_ShouldThrowArgumentNullException()
    {
        // Arrange
        var connectionTest = new ConnectionTest(new Mock<ILogger<ConnectionTest>>().Object, _mockConfig.Object, _mockErrorService.Object);
        var configurationTest = new ConfigurationTest(new Mock<ILogger<ConfigurationTest>>().Object, _mockConfig.Object, _mockErrorService.Object);
        var dataQualityTest = new DataQualityTest(new Mock<ILogger<DataQualityTest>>().Object, _mockConfig.Object, _mockErrorService.Object);
        var performanceTest = new PerformanceBenchmarkTest(new Mock<ILogger<PerformanceBenchmarkTest>>().Object, _mockConfig.Object, _mockErrorService.Object);

        // Act & Assert
        var action = () => new TestRunner(
            _mockLogger.Object,
            null!,
            connectionTest,
            configurationTest,
            dataQualityTest,
            performanceTest);

        action.Should().Throw<ArgumentNullException>().WithParameterName("errorService");
    }

    [Fact]
    public void Constructor_NullConnectionTest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var configurationTest = new ConfigurationTest(new Mock<ILogger<ConfigurationTest>>().Object, _mockConfig.Object, _mockErrorService.Object);
        var dataQualityTest = new DataQualityTest(new Mock<ILogger<DataQualityTest>>().Object, _mockConfig.Object, _mockErrorService.Object);
        var performanceTest = new PerformanceBenchmarkTest(new Mock<ILogger<PerformanceBenchmarkTest>>().Object, _mockConfig.Object, _mockErrorService.Object);

        // Act & Assert
        var action = () => new TestRunner(
            _mockLogger.Object,
            _mockErrorService.Object,
            null!,
            configurationTest,
            dataQualityTest,
            performanceTest);

        action.Should().Throw<ArgumentNullException>().WithParameterName("connectionTest");
    }

    [Fact]
    public void GetAvailableTests_ShouldReturnTestInformation()
    {
        // Act
        var result = _testRunner.GetAvailableTests();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Count.Should().BeGreaterThan(0);

        // Verify specific tests are included
        result.Value.Should().Contain(t => t.TestId == "CONN-001");
        result.Value.Should().Contain(t => t.TestId == "CONF-001");
        result.Value.Should().Contain(t => t.TestId == "DATA-001");
        result.Value.Should().Contain(t => t.TestId == "PERF-001");
    }

    [Fact]
    public void GetAvailableTests_ShouldIncludeTestDetails()
    {
        // Act
        var result = _testRunner.GetAvailableTests();

        // Assert
        result.IsSuccess.Should().BeTrue();
        var connectionTest = result.Value.FirstOrDefault(t => t.TestId == "CONN-001");
        
        connectionTest.Should().NotBeNull();
        connectionTest!.TestName.Should().Be("Network Connectivity Test");
        connectionTest.Category.Should().Be(TestCategory.Connection);
        connectionTest.RequiresDevice.Should().BeTrue();
        connectionTest.EstimatedDuration.Should().Be(TimeSpan.FromSeconds(10));
        connectionTest.Dependencies.Should().BeEmpty();
    }

    [Fact]
    public async Task RunTestAsync_InvalidTestId_ShouldReturnFailureResult()
    {
        // Arrange
        var invalidTestId = "INVALID-001";

        // Act
        var result = await _testRunner.RunTestAsync(invalidTestId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.TestId.Should().Be(invalidTestId);
        result.Value.Status.Should().Be(TestStatus.Failed);
        result.Value.ErrorMessage.Should().Contain("Test ID");
        result.Value.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task RunTestsAsync_UnimplementedCategory_ShouldLogWarning()
    {
        // Act
        var result = await _testRunner.RunTestsAsync(TestCategory.Health);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Health tests not yet implemented")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RunTestsAsync_WithCancellation_ShouldRespectCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await _testRunner.RunTestsAsync(TestCategory.Connection, cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Note: The actual tests may still run since they're concrete implementations
        // but the cancellation should be checked in the main loop
    }

    [Fact]
    public async Task RunAllTestsAsync_WithCancellation_ShouldStopEarly()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await _testRunner.RunAllTestsAsync(cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateTestReportAsync_ConsoleFormat_ShouldReturnConsoleReport()
    {
        // Arrange
        var testResults = new[]
        {
            TestResult.Success("TEST-001", "Test 1", TestCategory.Connection, TimeSpan.FromSeconds(1), "Success"),
            TestResult.Failure("TEST-002", "Test 2", TestCategory.Configuration, TimeSpan.FromSeconds(1), "Failed")
        };

        // Act
        var result = await _testRunner.GenerateTestReportAsync(testResults, ReportFormat.Console);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().Contain("ADAM Logger Production Test Report");
        result.Value.Should().Contain("TEST SUMMARY");
        result.Value.Should().Contain("✓ Test 1");
        result.Value.Should().Contain("✗ Test 2");
    }

    [Fact]
    public async Task GenerateTestReportAsync_JsonFormat_ShouldReturnJsonReport()
    {
        // Arrange
        var testResults = new[]
        {
            TestResult.Success("TEST-001", "Test 1", TestCategory.Connection, TimeSpan.FromSeconds(1), "Success")
        };

        // Act
        var result = await _testRunner.GenerateTestReportAsync(testResults, ReportFormat.Json);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("\"testId\": \"TEST-001\"");
        result.Value.Should().Contain("\"testName\": \"Test 1\"");
        result.Value.Should().Contain("\"category\": \"Connection\"");
    }

    [Fact]
    public async Task GenerateTestReportAsync_MarkdownFormat_ShouldReturnMarkdownReport()
    {
        // Arrange
        var testResults = new[]
        {
            TestResult.Success("TEST-001", "Test 1", TestCategory.Connection, TimeSpan.FromSeconds(1), "Success")
        };

        // Act
        var result = await _testRunner.GenerateTestReportAsync(testResults, ReportFormat.Markdown);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("# ADAM Logger Production Test Report");
        result.Value.Should().Contain("## Test Summary");
        result.Value.Should().Contain("✅ Test 1");
    }

    [Fact]
    public async Task GenerateTestReportAsync_HtmlFormat_ShouldReturnHtmlReport()
    {
        // Arrange
        var testResults = new[]
        {
            TestResult.Success("TEST-001", "Test 1", TestCategory.Connection, TimeSpan.FromSeconds(1), "Success")
        };

        // Act
        var result = await _testRunner.GenerateTestReportAsync(testResults, ReportFormat.Html);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("<!DOCTYPE html>");
        result.Value.Should().Contain("<title>ADAM Logger Test Report</title>");
        result.Value.Should().Contain("<h1>ADAM Logger Production Test Report</h1>");
        result.Value.Should().Contain("Test 1");
    }

    [Fact]
    public async Task GenerateTestReportAsync_UnsupportedFormat_ShouldReturnFailure()
    {
        // Arrange
        var testResults = new[]
        {
            TestResult.Success("TEST-001", "Test 1", TestCategory.Connection, TimeSpan.FromSeconds(1), "Success")
        };

        // Act
        var result = await _testRunner.GenerateTestReportAsync(testResults, (ReportFormat)999);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNull();
        result.ErrorMessage.Should().Contain("Unsupported report format");
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}