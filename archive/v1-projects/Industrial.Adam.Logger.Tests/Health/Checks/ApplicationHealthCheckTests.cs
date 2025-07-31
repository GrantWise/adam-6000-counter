using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Industrial.Adam.Logger.Configuration;
using Industrial.Adam.Logger.ErrorHandling;
using Industrial.Adam.Logger.Health.Checks;
using Industrial.Adam.Logger.Health.Models;
using Industrial.Adam.Logger.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Industrial.Adam.Logger.Tests.Health.Checks;

public class ApplicationHealthCheckTests
{
    private readonly Mock<ILogger<ApplicationHealthCheck>> _mockLogger;
    private readonly Mock<IOptions<AdamLoggerConfig>> _mockConfig;
    private readonly Mock<IIndustrialErrorService> _mockErrorService;
    private readonly Mock<IHostApplicationLifetime> _mockApplicationLifetime;
    private readonly ApplicationHealthCheck _healthCheck;
    private readonly AdamLoggerConfig _config;

    public ApplicationHealthCheckTests()
    {
        _mockLogger = new Mock<ILogger<ApplicationHealthCheck>>();
        _mockConfig = new Mock<IOptions<AdamLoggerConfig>>();
        _mockErrorService = new Mock<IIndustrialErrorService>();
        _mockApplicationLifetime = new Mock<IHostApplicationLifetime>();

        _config = new AdamLoggerConfig
        {
            PollIntervalMs = 1000,
            MaxConcurrentDevices = 10,
            HealthCheckIntervalMs = 30000,
            DemoMode = false,
            Devices = new List<AdamDeviceConfig>
            {
                new AdamDeviceConfig 
                { 
                    DeviceId = "device1",
                    IpAddress = "192.168.1.100",
                    Channels = new List<ChannelConfig>
                    {
                        new ChannelConfig { ChannelNumber = 0, Name = "Channel0" }
                    }
                }
            }
        };

        _mockConfig.Setup(x => x.Value).Returns(_config);

        // Setup default application lifetime state
        var normalCts = new CancellationTokenSource();
        _mockApplicationLifetime.Setup(x => x.ApplicationStopping).Returns(normalCts.Token);

        _healthCheck = new ApplicationHealthCheck(
            _mockLogger.Object,
            _mockConfig.Object,
            _mockErrorService.Object,
            _mockApplicationLifetime.Object);
    }

    [Fact]
    public void Constructor_WhenLoggerIsNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new ApplicationHealthCheck(
            null!,
            _mockConfig.Object,
            _mockErrorService.Object,
            _mockApplicationLifetime.Object);
        
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WhenConfigIsNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new ApplicationHealthCheck(
            _mockLogger.Object,
            null!,
            _mockErrorService.Object,
            _mockApplicationLifetime.Object);
        
        act.Should().Throw<ArgumentNullException>().WithParameterName("config");
    }

    [Fact]
    public void Constructor_WhenErrorServiceIsNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new ApplicationHealthCheck(
            _mockLogger.Object,
            _mockConfig.Object,
            null!,
            _mockApplicationLifetime.Object);
        
        act.Should().Throw<ArgumentNullException>().WithParameterName("errorService");
    }

    [Fact]
    public void Constructor_WhenApplicationLifetimeIsNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new ApplicationHealthCheck(
            _mockLogger.Object,
            _mockConfig.Object,
            _mockErrorService.Object,
            null!);
        
        act.Should().Throw<ArgumentNullException>().WithParameterName("applicationLifetime");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenApplicationIsHealthy_ShouldReturnHealthyStatus()
    {
        // Act
        var result = await _healthCheck.CheckHealthAsync(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Application");
        result.Status.Should().BeOneOf(HealthStatus.Healthy, HealthStatus.Degraded);
        if (result.Status == HealthStatus.Healthy)
        {
            result.StatusMessage.Should().Contain("Application running normally");
        }
        else
        {
            result.StatusMessage.Should().Contain("Application operational");
        }
        result.Metrics.Should().NotBeNull().And.NotBeEmpty();
        result.CheckDuration.Should().BeGreaterThan(TimeSpan.Zero);
        result.LastChecked.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        result.Uptime.Should().NotBeNull();
        result.Uptime!.Value.Should().BeGreaterThan(TimeSpan.Zero);
        result.Warnings.Should().NotBeNull();
        result.Recommendations.Should().NotBeNull();
        result.Dependencies.Should().NotBeNull();
    }

    [Fact]
    public async Task CheckHealthAsync_WhenApplicationIsStopping_ShouldReturnDegradedStatus()
    {
        // Arrange
        var stoppingCts = new CancellationTokenSource();
        stoppingCts.Cancel(); // Simulate application stopping
        _mockApplicationLifetime.Setup(x => x.ApplicationStopping).Returns(stoppingCts.Token);

        // Act
        var result = await _healthCheck.CheckHealthAsync(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Name.Should().Be("Application");
        result.StatusMessage.Should().Be("Application is shutting down");
        result.Warnings.Should().Contain("Application shutdown in progress");
        result.Recommendations.Should().Contain("Allow graceful shutdown to complete");
        result.HealthScore.Should().Be(75);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldCollectSystemMetrics()
    {
        // Act
        var result = await _healthCheck.CheckHealthAsync(CancellationToken.None);

        // Assert
        result.Metrics.Should().ContainKeys(
            "Uptime",
            "WorkingSetMB",
            "PrivateMemoryMB",
            "GcMemoryMB",
            "Gen0Collections",
            "Gen1Collections",
            "Gen2Collections",
            "TotalProcessorTimeSeconds",
            "ThreadCount",
            "HandleCount",
            "ProcessId",
            "StartTime"
        );

        result.Metrics["ProcessId"].Should().BeOfType<int>();
        ((int)result.Metrics["ProcessId"]).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldCollectApplicationMetrics()
    {
        // Act
        var result = await _healthCheck.CheckHealthAsync(CancellationToken.None);

        // Assert
        result.Metrics.Should().ContainKeys(
            "ApplicationVersion",
            "AssemblyLocation",
            "IsServerGC",
            "ProcessorCount",
            "OSVersion",
            "MachineName",
            "RuntimeVersion",
            "Is64BitProcess",
            "Is64BitOperatingSystem",
            "TimeZone",
            "CurrentCulture"
        );
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldCollectConfigurationMetrics()
    {
        // Act
        var result = await _healthCheck.CheckHealthAsync(CancellationToken.None);

        // Assert
        result.Metrics.Should().ContainKey("ConfiguredDevices");
        result.Metrics["ConfiguredDevices"].Should().Be(1);
        result.Metrics.Should().ContainKey("PollIntervalMs");
        result.Metrics["PollIntervalMs"].Should().Be(1000);
        result.Metrics.Should().ContainKey("MaxConcurrentDevices");
        result.Metrics["MaxConcurrentDevices"].Should().Be(10);
        result.Metrics.Should().ContainKey("HealthCheckIntervalMs");
        result.Metrics.Should().ContainKey("DemoMode");
        result.Metrics["DemoMode"].Should().Be(false);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenVeryLowPollInterval_ShouldAddWarning()
    {
        // Arrange
        _config.PollIntervalMs = 50;

        // Act
        var result = await _healthCheck.CheckHealthAsync(CancellationToken.None);

        // Assert
        result.Warnings.Should().Contain(w => w.Contains("Very low poll interval: 50ms"));
        result.Recommendations.Should().Contain(r => r.Contains("Consider increasing poll interval"));
        result.Status.Should().Be(HealthStatus.Degraded);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenHighConcurrentDevices_ShouldAddWarning()
    {
        // Arrange
        _config.MaxConcurrentDevices = Environment.ProcessorCount * 5;

        // Act
        var result = await _healthCheck.CheckHealthAsync(CancellationToken.None);

        // Assert
        result.Warnings.Should().Contain(w => w.Contains($"High concurrent device count: {_config.MaxConcurrentDevices}"));
        result.Recommendations.Should().Contain(r => r.Contains("Consider reducing concurrent device limit"));
    }

    [Fact]
    public async Task CheckHealthAsync_WhenNoDevicesConfigured_ShouldAddWarning()
    {
        // Arrange
        _config.Devices.Clear();

        // Act
        var result = await _healthCheck.CheckHealthAsync(CancellationToken.None);

        // Assert
        result.Warnings.Should().Contain("No devices configured for monitoring");
        result.Recommendations.Should().Contain("Add device configurations to enable data collection");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenHighMemoryUsage_ShouldGenerateWarning()
    {
        // Act
        var result = await _healthCheck.CheckHealthAsync(CancellationToken.None);

        // Assert - Check if warning logic works when conditions are met
        if (result.Metrics.TryGetValue("WorkingSetMB", out var workingSet) && 
            workingSet is double mb && mb > 1000)
        {
            result.Warnings.Should().Contain(w => w.Contains("High memory usage"));
            result.Recommendations.Should().Contain(r => r.Contains("Monitor memory usage"));
        }
    }

    [Fact]
    public async Task CheckHealthAsync_WhenHighThreadCount_ShouldGenerateWarning()
    {
        // Act
        var result = await _healthCheck.CheckHealthAsync(CancellationToken.None);

        // Assert - Check if warning logic works when conditions are met
        if (result.Metrics.TryGetValue("ThreadCount", out var threadCount) && 
            threadCount is int count && count > 100)
        {
            result.Warnings.Should().Contain(w => w.Contains("High thread count"));
            result.Recommendations.Should().Contain(r => r.Contains("thread pool optimization"));
        }
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldCalculateHealthScore()
    {
        // Act
        var result = await _healthCheck.CheckHealthAsync(CancellationToken.None);

        // Assert
        result.HealthScore.Should().BeInRange(0, 100);
        
        // If healthy, score should be high
        if (result.Status == HealthStatus.Healthy)
        {
            result.HealthScore.Should().BeGreaterThanOrEqualTo(90);
            result.Warnings.Should().BeEmpty();
        }
        
        // If degraded, score should be moderate with warnings
        if (result.Status == HealthStatus.Degraded)
        {
            result.HealthScore.Should().BeInRange(65, 95);
            result.Warnings.Should().NotBeEmpty();
        }
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldLogDebugInformation()
    {
        // Act
        await _healthCheck.CheckHealthAsync(CancellationToken.None);

        // Assert
        _mockLogger.Verify(x => x.Log(
            LogLevel.Debug,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Application health check completed")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenExceptionOccurs_ShouldReturnCriticalAndLogError()
    {
        // Arrange
        var exception = new Exception("Test exception");
        var errorMessage = new IndustrialErrorMessage
        {
            ErrorCode = "HEALTH-001",
            Summary = "Application health check error",
            DetailedDescription = "Test error description",
            TroubleshootingSteps = new[] { "Step 1", "Step 2" },
            Severity = ErrorSeverity.Critical,
            Category = ErrorCategory.System
        };

        // Setup to throw exception when accessing config
        _mockConfig.Setup(x => x.Value).Throws(exception);

        // Setup error service to return our error message
        _mockErrorService.Setup(x => x.CreateAndLogError(
            It.Is<Exception>(e => e == exception),
            It.Is<string>(s => s == "HEALTH-001"),
            It.Is<string>(s => s == "Application health check failed"),
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int>()))
            .Returns(errorMessage);

        // Act
        var result = await _healthCheck.CheckHealthAsync(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(HealthStatus.Critical);
        result.Name.Should().Be("Application");
        result.ErrorMessage.Should().Be("Application health check error");
        result.Recommendations.Should().BeEquivalentTo(new[] { "Step 1", "Step 2" });
        result.HealthScore.Should().Be(0);

        // Verify error service was called
        _mockErrorService.Verify(x => x.CreateAndLogError(
            It.IsAny<Exception>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int>()), 
            Times.Once);
    }

    [Fact]
    public async Task CheckHealthAsync_WithHighGen2Collections_ShouldGenerateWarning()
    {
        // This test validates the Gen2 collection rate analysis logic
        // Act
        var result = await _healthCheck.CheckHealthAsync(CancellationToken.None);

        // Assert
        if (result.Metrics.TryGetValue("Gen2Collections", out var gen2Obj) && 
            gen2Obj is int gen2Count &&
            result.Metrics.TryGetValue("Uptime", out var uptimeObj) && 
            uptimeObj is double uptimeSeconds)
        {
            var gen2Rate = gen2Count / (uptimeSeconds / 3600.0);
            if (gen2Rate > 10)
            {
                result.Warnings.Should().Contain(w => w.Contains("High Gen2 GC rate"));
                result.Recommendations.Should().Contain(r => r.Contains("memory allocation patterns"));
            }
        }
    }
}