using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Industrial.Adam.Logger.ErrorHandling;
using Industrial.Adam.Logger.Health.Checks;
using Industrial.Adam.Logger.Health.Models;
using Industrial.Adam.Logger.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Industrial.Adam.Logger.Tests.Health.Checks;

public class SystemResourceHealthCheckTests
{
    private readonly Mock<ILogger<SystemResourceHealthCheck>> _mockLogger;
    private readonly Mock<IIndustrialErrorService> _mockErrorService;
    private readonly SystemResourceHealthCheck _healthCheck;

    public SystemResourceHealthCheckTests()
    {
        _mockLogger = new Mock<ILogger<SystemResourceHealthCheck>>();
        _mockErrorService = new Mock<IIndustrialErrorService>();
        _healthCheck = new SystemResourceHealthCheck(_mockLogger.Object, _mockErrorService.Object);
    }

    [Fact]
    public void Constructor_WhenLoggerIsNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new SystemResourceHealthCheck(null!, _mockErrorService.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WhenErrorServiceIsNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new SystemResourceHealthCheck(_mockLogger.Object, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("errorService");
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnValidComponentHealth()
    {
        // Act
        var result = await _healthCheck.CheckHealthAsync(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("SystemResources");
        result.Status.Should().BeOneOf(HealthStatus.Healthy, HealthStatus.Degraded, HealthStatus.Unhealthy);
        result.HealthScore.Should().BeInRange(0, 100);
        result.CheckDuration.Should().BeGreaterThan(TimeSpan.Zero);
        result.LastChecked.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        result.Metrics.Should().NotBeNull().And.NotBeEmpty();
        result.Warnings.Should().NotBeNull();
        result.Recommendations.Should().NotBeNull();
        result.Dependencies.Should().NotBeNull();
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldCollectCpuMetrics()
    {
        // Act
        var result = await _healthCheck.CheckHealthAsync(CancellationToken.None);

        // Assert
        result.Metrics.Should().ContainKey("ProcessorCount");
        result.Metrics["ProcessorCount"].Should().BeOfType<int>();
        ((int)result.Metrics["ProcessorCount"]).Should().BeGreaterThan(0);
        
        result.Metrics.Should().ContainKeys(
            "ProcessTotalProcessorTimeMs",
            "ProcessUserProcessorTimeMs", 
            "ProcessPrivilegedProcessorTimeMs"
        );
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldCollectMemoryMetrics()
    {
        // Act
        var result = await _healthCheck.CheckHealthAsync(CancellationToken.None);

        // Assert
        result.Metrics.Should().ContainKeys(
            "ProcessWorkingSetMB",
            "ProcessPrivateMemoryMB",
            "ProcessVirtualMemoryMB",
            "ProcessPagedMemoryMB",
            "ProcessPagedSystemMemoryMB",
            "ProcessNonPagedSystemMemoryMB",
            "GcTotalMemoryMB",
            "GcTotalMemoryAfterCollectionMB"
        );

        var workingSetMB = (double)result.Metrics["ProcessWorkingSetMB"];
        workingSetMB.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldCollectDiskMetrics()
    {
        // Act
        var result = await _healthCheck.CheckHealthAsync(CancellationToken.None);

        // Assert
        result.Metrics.Should().ContainKeys(
            "DiskTotalSpaceGB",
            "DiskFreeSpaceGB",
            "DiskUsedSpaceGB",
            "DiskUsedPercent",
            "DiskDriveFormat",
            "DiskDriveType",
            "TotalDriveCount",
            "ReadyDriveCount"
        );

        var totalDrives = (int)result.Metrics["TotalDriveCount"];
        totalDrives.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldCollectProcessMetrics()
    {
        // Act
        var result = await _healthCheck.CheckHealthAsync(CancellationToken.None);

        // Assert
        result.Metrics.Should().ContainKeys(
            "ProcessId",
            "ProcessName",
            "ProcessThreadCount",
            "ProcessHandleCount",
            "ProcessStartTime",
            "ProcessUptimeSeconds",
            "ProcessBasePriority",
            "ProcessPriorityClass",
            "ProcessSessionId",
            "ProcessMachineName"
        );

        var processId = (int)result.Metrics["ProcessId"];
        processId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldCollectRuntimeMetrics()
    {
        // Act
        var result = await _healthCheck.CheckHealthAsync(CancellationToken.None);

        // Assert
        result.Metrics.Should().ContainKeys(
            "GcGen0Collections",
            "GcGen1Collections",
            "GcGen2Collections",
            "GcIsServerGC",
            "GcLatencyMode",
            "RuntimeVersion",
            "FrameworkDescription",
            "OSDescription",
            "OSArchitecture",
            "ProcessArchitecture",
            "Is64BitProcess",
            "Is64BitOperatingSystem",
            "SystemPageSize",
            "TickCount"
        );
    }

    [Fact]
    public async Task CheckHealthAsync_WhenHighMemoryUsage_ShouldGenerateWarning()
    {
        // Act
        var result = await _healthCheck.CheckHealthAsync(CancellationToken.None);

        // Assert - Check if warning logic works when conditions are met
        if (result.Metrics.TryGetValue("ProcessWorkingSetMB", out var workingSet) && 
            workingSet is double mb && mb > 1024)
        {
            result.Warnings.Should().Contain(w => w.Contains("High memory usage"));
            result.Recommendations.Should().Contain(r => r.Contains("Monitor memory usage"));
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
            result.HealthScore.Should().BeGreaterThanOrEqualTo(85);
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
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("System resource health check completed")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Dispose_ShouldCompleteSuccessfully()
    {
        // Arrange
        var healthCheck = new SystemResourceHealthCheck(_mockLogger.Object, _mockErrorService.Object);

        // Act & Assert
        var act = () => healthCheck.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_WhenCalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var healthCheck = new SystemResourceHealthCheck(_mockLogger.Object, _mockErrorService.Object);

        // Act & Assert
        var act = () =>
        {
            healthCheck.Dispose();
            healthCheck.Dispose();
            healthCheck.Dispose();
        };
        act.Should().NotThrow();
    }

    [Fact]
    public async Task CheckHealthAsync_WhenCancellationRequested_ShouldStillComplete()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(1));

        // Act
        var result = await _healthCheck.CheckHealthAsync(cts.Token);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("SystemResources");
        // The health check should complete even with cancellation
    }

    [Fact]
    public async Task CheckHealthAsync_WindowsSpecificMetrics_ShouldBeHandledGracefully()
    {
        // Act
        var result = await _healthCheck.CheckHealthAsync(CancellationToken.None);

        // Assert
        if (OperatingSystem.IsWindows())
        {
            // On Windows, performance counters might be available
            var hasWindowsMetrics = result.Metrics.Keys.Any(k => 
                k.Contains("SystemCpuUsagePercent") || 
                k.Contains("SystemAvailableMemoryMB") ||
                k.Contains("CpuCounterError") ||
                k.Contains("MemoryCounterError"));
            
            // Should have attempted to collect Windows-specific metrics
            hasWindowsMetrics.Should().BeTrue();
        }
        else
        {
            // On non-Windows, these metrics should not crash the health check
            result.Should().NotBeNull();
        }
    }
}