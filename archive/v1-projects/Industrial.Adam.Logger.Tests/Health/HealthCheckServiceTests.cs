// Industrial.Adam.Logger.Tests - HealthCheckService Unit Tests
// Tests for the comprehensive health check service implementation

using FluentAssertions;
using Industrial.Adam.Logger.Configuration;
using Industrial.Adam.Logger.ErrorHandling;
using Industrial.Adam.Logger.Health;
using Industrial.Adam.Logger.Health.Checks;
using Industrial.Adam.Logger.Health.Models;
using Industrial.Adam.Logger.Interfaces;
using Industrial.Adam.Logger.Models;
using Industrial.Adam.Logger.Tests.TestHelpers;
using Industrial.Adam.Logger.Utilities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Industrial.Adam.Logger.Tests.Health;

/// <summary>
/// Unit tests for HealthCheckService focusing on orchestration logic and event handling
/// </summary>
public class HealthCheckServiceTests : IDisposable
{
    private readonly Mock<ILogger<HealthCheckService>> _mockLogger;
    private readonly Mock<IOptions<AdamLoggerConfig>> _mockConfig;
    private readonly Mock<IIndustrialErrorService> _mockErrorService;
    private readonly Mock<IHostApplicationLifetime> _mockAppLifetime;
    private readonly Mock<IInfluxDbWriter> _mockInfluxDbWriter;
    private ApplicationHealthCheck _applicationHealthCheck;
    private InfluxDbHealthCheck _influxDbHealthCheck;
    private SystemResourceHealthCheck _systemResourceHealthCheck;
    private readonly AdamLoggerConfig _testConfig;
    private readonly List<HealthCheckService> _servicesToDispose;

    public HealthCheckServiceTests()
    {
        _mockLogger = new Mock<ILogger<HealthCheckService>>();
        _mockConfig = new Mock<IOptions<AdamLoggerConfig>>();
        _mockErrorService = new Mock<IIndustrialErrorService>();
        _mockAppLifetime = new Mock<IHostApplicationLifetime>();
        _mockInfluxDbWriter = new Mock<IInfluxDbWriter>();
        _testConfig = TestConfigurationBuilder.ValidLoggerConfig();
        _servicesToDispose = new List<HealthCheckService>();

        SetupMocks();
    }

    private void SetupMocks()
    {
        _mockConfig.Setup(x => x.Value).Returns(_testConfig);
        
        // Create real health check instances with mocked dependencies
        _applicationHealthCheck = new ApplicationHealthCheck(
            new NullLogger<ApplicationHealthCheck>(),
            _mockConfig.Object,
            _mockErrorService.Object,
            _mockAppLifetime.Object);
            
        _influxDbHealthCheck = new InfluxDbHealthCheck(
            new NullLogger<InfluxDbHealthCheck>(),
            _mockInfluxDbWriter.Object,
            _mockErrorService.Object);
            
        _systemResourceHealthCheck = new SystemResourceHealthCheck(
            new NullLogger<SystemResourceHealthCheck>(),
            _mockConfig.Object,
            _mockErrorService.Object);
        _mockConfig.Setup(x => x.Value).Returns(_testConfig);

        // Setup mocks for health check dependencies
        _mockInfluxDbWriter.Setup(x => x.IsHealthyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        _mockAppLifetime.SetupGet(x => x.ApplicationStarted).Returns(new CancellationToken());
        _mockAppLifetime.SetupGet(x => x.ApplicationStopping).Returns(new CancellationToken());
        _mockAppLifetime.SetupGet(x => x.ApplicationStopped).Returns(new CancellationToken());

        // Setup error service mock
        _mockErrorService.Setup(x => x.CreateAndLogError(
            It.IsAny<Exception>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int>()))
            .Returns(new IndustrialErrorMessage
            {
                ErrorCode = "TEST-ERROR",
                Summary = "Test error message",
                DetailedDescription = "Test detailed error message",
                TroubleshootingSteps = new[] { "Test troubleshooting step" },
                Context = new Dictionary<string, object>(),
                Severity = ErrorSeverity.Critical,
                Category = ErrorCategory.System
            });
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ValidParameters_ShouldCreateInstance()
    {
        // Act
        var service = CreateService();

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Action action = () => new HealthCheckService(
            null!,
            _mockConfig.Object,
            _mockErrorService.Object,
            _applicationHealthCheck,
            _influxDbHealthCheck,
            _systemResourceHealthCheck);

        action.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_NullConfig_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Action action = () => new HealthCheckService(
            _mockLogger.Object,
            null!,
            _mockErrorService.Object,
            _applicationHealthCheck,
            _influxDbHealthCheck,
            _systemResourceHealthCheck);

        action.Should().Throw<ArgumentNullException>().WithParameterName("config");
    }

    [Fact]
    public void Constructor_NullErrorService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Action action = () => new HealthCheckService(
            _mockLogger.Object,
            _mockConfig.Object,
            null!,
            _applicationHealthCheck,
            _influxDbHealthCheck,
            _systemResourceHealthCheck);

        action.Should().Throw<ArgumentNullException>().WithParameterName("errorService");
    }

    [Fact]
    public void Constructor_NullApplicationHealthCheck_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Action action = () => new HealthCheckService(
            _mockLogger.Object,
            _mockConfig.Object,
            _mockErrorService.Object,
            null!,
            _influxDbHealthCheck,
            _systemResourceHealthCheck);

        action.Should().Throw<ArgumentNullException>().WithParameterName("applicationHealthCheck");
    }

    [Fact]
    public void Constructor_NullInfluxDbHealthCheck_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Action action = () => new HealthCheckService(
            _mockLogger.Object,
            _mockConfig.Object,
            _mockErrorService.Object,
            _applicationHealthCheck,
            null!,
            _systemResourceHealthCheck);

        action.Should().Throw<ArgumentNullException>().WithParameterName("influxDbHealthCheck");
    }

    [Fact]
    public void Constructor_NullSystemResourceHealthCheck_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Action action = () => new HealthCheckService(
            _mockLogger.Object,
            _mockConfig.Object,
            _mockErrorService.Object,
            _applicationHealthCheck,
            _influxDbHealthCheck,
            null!);

        action.Should().Throw<ArgumentNullException>().WithParameterName("systemResourceHealthCheck");
    }

    #endregion

    #region CheckHealthAsync Tests

    [Fact]
    public async Task CheckHealthAsync_AllComponentsHealthy_ShouldReturnHealthyResponse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.CheckHealthAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Status.Should().Be(HealthStatus.Healthy);
        result.Value.HealthScore.Should().Be(100);
        result.Value.Components.Should().HaveCount(3);
        result.Value.Components.Should().ContainKey("Application");
        result.Value.Components.Should().ContainKey("InfluxDB");
        result.Value.Components.Should().ContainKey("SystemResources");
    }

    [Fact]
    public async Task CheckHealthAsync_SomeComponentsDegraded_ShouldReturnDegradedResponse()
    {
        // Arrange
        var service = CreateService();

        // Setup one component as degraded
        _mockInfluxDbHealthCheck.Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ComponentHealth.Degraded(
                "InfluxDB",
                TimeSpan.FromMilliseconds(5),
                70,
                "Connection latency is high",
                new[] { "High connection latency detected" },
                new[] { "Check network connectivity" }));

        // Act
        var result = await service.CheckHealthAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(HealthStatus.Degraded);
        result.Value.HealthScore.Should().BeLessThan(100);
        result.Value.Warnings.Should().Contain("High connection latency detected");
        result.Value.Recommendations.Should().Contain("Check network connectivity");
    }

    [Fact]
    public async Task CheckHealthAsync_ApplicationUnhealthy_ShouldReturnCriticalResponse()
    {
        // Arrange
        var service = CreateService();

        // Setup application as unhealthy (should make overall status critical)
        _mockApplicationHealthCheck.Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ComponentHealth.Unhealthy(
                "Application",
                TimeSpan.FromMilliseconds(5),
                "Application startup failed",
                0,
                new[] { "Restart application service" }));

        // Act
        var result = await service.CheckHealthAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(HealthStatus.Critical);
        result.Value.HealthScore.Should().BeLessThan(100);
    }

    [Fact]
    public async Task CheckHealthAsync_WithCancellation_ShouldRespectCancellation()
    {
        // Arrange
        var service = CreateService();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await service.Invoking(s => s.CheckHealthAsync(cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task CheckHealthAsync_ExceptionInHealthCheck_ShouldReturnFailureResult()
    {
        // Arrange
        var service = CreateService();
        var expectedException = new InvalidOperationException("Health check failed");

        _mockApplicationHealthCheck.Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act
        var result = await service.CheckHealthAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();

        // Verify error service was called
        _mockErrorService.Verify(x => x.CreateAndLogError(
            It.IsAny<Exception>(),
            "HEALTH-100",
            "Comprehensive health check failed",
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldIncludeVersionAndEnvironmentInfo()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.CheckHealthAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Version.Should().NotBeNull();
        result.Value.Version.ApplicationVersion.Should().NotBeNullOrEmpty();
        result.Value.Version.RuntimeVersion.Should().NotBeNullOrEmpty();
        result.Value.Environment.Should().NotBeNull();
        result.Value.Environment.MachineName.Should().NotBeNullOrEmpty();
        result.Value.Environment.OperatingSystem.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldIncludeSystemMetrics()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.CheckHealthAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Metrics.Should().NotBeEmpty();
        result.Value.Metrics.Should().ContainKey("ComponentCount");
        result.Value.Metrics.Should().ContainKey("HealthyComponentCount");
        result.Value.Metrics.Should().ContainKey("ConfiguredDevices");
        result.Value.Metrics.Should().ContainKey("DemoMode");
    }

    #endregion

    #region CheckComponentHealthAsync Tests

    [Fact]
    public async Task CheckComponentHealthAsync_ApplicationComponent_ShouldReturnApplicationHealth()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.CheckComponentHealthAsync("Application");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Application");
        result.Value.Status.Should().Be(HealthStatus.Healthy);

        // Verify application health check was called
        _mockApplicationHealthCheck.Verify(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckComponentHealthAsync_InfluxDbComponent_ShouldReturnInfluxDbHealth()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.CheckComponentHealthAsync("InfluxDB");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("InfluxDB");
        result.Value.Status.Should().Be(HealthStatus.Healthy);

        // Verify InfluxDB health check was called
        _mockInfluxDbHealthCheck.Verify(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckComponentHealthAsync_SystemResourcesComponent_ShouldReturnSystemResourcesHealth()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.CheckComponentHealthAsync("SystemResources");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("SystemResources");
        result.Value.Status.Should().Be(HealthStatus.Healthy);

        // Verify system resources health check was called
        _mockSystemResourceHealthCheck.Verify(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckComponentHealthAsync_UnknownComponent_ShouldReturnUnhealthyResult()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.CheckComponentHealthAsync("UnknownComponent");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("UnknownComponent");
        result.Value.Status.Should().Be(HealthStatus.Unhealthy);
        result.Value.ErrorMessage.Should().Contain("Unknown component");
        result.Value.Recommendations.Should().Contain("Verify component name is correct");
    }

    [Fact]
    public async Task CheckComponentHealthAsync_CustomComponent_ShouldReturnCustomHealth()
    {
        // Arrange
        var service = CreateService();
        var customHealth = ComponentHealth.Healthy("CustomComponent", TimeSpan.FromMilliseconds(5));

        // Register custom health check
        var registerResult = service.RegisterComponentHealthCheck("CustomComponent",
            _ => Task.FromResult(customHealth));
        registerResult.IsSuccess.Should().BeTrue();

        // Act
        var result = await service.CheckComponentHealthAsync("CustomComponent");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("CustomComponent");
        result.Value.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckComponentHealthAsync_ExceptionInHealthCheck_ShouldReturnFailureResult()
    {
        // Arrange
        var service = CreateService();
        var expectedException = new InvalidOperationException("Component health check failed");

        _mockApplicationHealthCheck.Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act
        var result = await service.CheckComponentHealthAsync("Application");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();

        // Verify error service was called
        _mockErrorService.Verify(x => x.CreateAndLogError(
            It.IsAny<Exception>(),
            "HEALTH-101",
            "Component health check failed for Application",
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int>()),
            Times.Once);
    }

    #endregion

    #region GetQuickHealthStatusAsync Tests

    [Fact]
    public async Task GetQuickHealthStatusAsync_HealthyApplication_ShouldReturnHealthy()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetQuickHealthStatusAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(HealthStatus.Healthy);

        // Verify only application health check was called (quick check)
        _mockApplicationHealthCheck.Verify(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetQuickHealthStatusAsync_UnhealthyApplication_ShouldReturnUnhealthy()
    {
        // Arrange
        var service = CreateService();

        _mockApplicationHealthCheck.Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ComponentHealth.Unhealthy(
                "Application",
                TimeSpan.FromMilliseconds(5),
                "Application is not responding"));

        // Act
        var result = await service.GetQuickHealthStatusAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task GetQuickHealthStatusAsync_ExceptionInHealthCheck_ShouldReturnFailureResult()
    {
        // Arrange
        var service = CreateService();
        var expectedException = new InvalidOperationException("Quick health check failed");

        _mockApplicationHealthCheck.Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act
        var result = await service.GetQuickHealthStatusAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();

        // Verify error service was called
        _mockErrorService.Verify(x => x.CreateAndLogError(
            It.IsAny<Exception>(),
            "HEALTH-102",
            "Quick health status check failed",
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int>()),
            Times.Once);
    }

    #endregion

    #region GetHealthMetricsAsync Tests

    [Fact]
    public async Task GetHealthMetricsAsync_SuccessfulHealthCheck_ShouldReturnMetrics()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetHealthMetricsAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        result.Value.Should().ContainKey("system_health_status");
        result.Value.Should().ContainKey("system_health_score");
        result.Value.Should().ContainKey("system_uptime_seconds");
        result.Value.Should().ContainKey("component_application_health_status");
        result.Value.Should().ContainKey("component_influxdb_health_status");
        result.Value.Should().ContainKey("component_systemresources_health_status");
    }

    [Fact]
    public async Task GetHealthMetricsAsync_FailedHealthCheck_ShouldReturnFailureResult()
    {
        // Arrange
        var service = CreateService();
        var expectedException = new InvalidOperationException("Health check failed");

        _mockApplicationHealthCheck.Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act
        var result = await service.GetHealthMetricsAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
    }

    #endregion

    #region GetAvailableComponents Tests

    [Fact]
    public void GetAvailableComponents_ShouldReturnBuiltInComponents()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.GetAvailableComponents();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("Application");
        result.Value.Should().Contain("InfluxDB");
        result.Value.Should().Contain("SystemResources");
        result.Value.Should().HaveCount(3);
    }

    [Fact]
    public void GetAvailableComponents_WithCustomComponents_ShouldReturnAllComponents()
    {
        // Arrange
        var service = CreateService();
        var customHealth = ComponentHealth.Healthy("CustomComponent", TimeSpan.FromMilliseconds(5));

        // Register custom health check
        var registerResult = service.RegisterComponentHealthCheck("CustomComponent",
            _ => Task.FromResult(customHealth));
        registerResult.IsSuccess.Should().BeTrue();

        // Act
        var result = service.GetAvailableComponents();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("Application");
        result.Value.Should().Contain("InfluxDB");
        result.Value.Should().Contain("SystemResources");
        result.Value.Should().Contain("CustomComponent");
        result.Value.Should().HaveCount(4);
    }

    #endregion

    #region RegisterComponentHealthCheck Tests

    [Fact]
    public void RegisterComponentHealthCheck_ValidComponent_ShouldReturnSuccess()
    {
        // Arrange
        var service = CreateService();
        var customHealth = ComponentHealth.Healthy("CustomComponent", TimeSpan.FromMilliseconds(5));

        // Act
        var result = service.RegisterComponentHealthCheck("CustomComponent",
            _ => Task.FromResult(customHealth));

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();

        // Verify component was registered
        var componentsResult = service.GetAvailableComponents();
        componentsResult.Value.Should().Contain("CustomComponent");
    }

    [Fact]
    public void RegisterComponentHealthCheck_NullComponentName_ShouldReturnFailure()
    {
        // Arrange
        var service = CreateService();
        var customHealth = ComponentHealth.Healthy("CustomComponent", TimeSpan.FromMilliseconds(5));

        // Act
        var result = service.RegisterComponentHealthCheck(null!,
            _ => Task.FromResult(customHealth));

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().BeOfType<ArgumentException>();
    }

    [Fact]
    public void RegisterComponentHealthCheck_EmptyComponentName_ShouldReturnFailure()
    {
        // Arrange
        var service = CreateService();
        var customHealth = ComponentHealth.Healthy("CustomComponent", TimeSpan.FromMilliseconds(5));

        // Act
        var result = service.RegisterComponentHealthCheck("",
            _ => Task.FromResult(customHealth));

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().BeOfType<ArgumentException>();
    }

    [Fact]
    public void RegisterComponentHealthCheck_NullHealthCheck_ShouldReturnFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.RegisterComponentHealthCheck("CustomComponent", null!);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().BeOfType<ArgumentNullException>();
    }

    [Fact]
    public void RegisterComponentHealthCheck_WithDependencies_ShouldReturnSuccess()
    {
        // Arrange
        var service = CreateService();
        var customHealth = ComponentHealth.Healthy("CustomComponent", TimeSpan.FromMilliseconds(5));
        var dependencies = new[] { "Application", "InfluxDB" };

        // Act
        var result = service.RegisterComponentHealthCheck("CustomComponent",
            _ => Task.FromResult(customHealth), dependencies);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();

        // Verify component was registered
        var componentsResult = service.GetAvailableComponents();
        componentsResult.Value.Should().Contain("CustomComponent");
    }

    #endregion

    #region UnregisterComponentHealthCheck Tests

    [Fact]
    public void UnregisterComponentHealthCheck_ExistingComponent_ShouldReturnSuccess()
    {
        // Arrange
        var service = CreateService();
        var customHealth = ComponentHealth.Healthy("CustomComponent", TimeSpan.FromMilliseconds(5));

        // Register component first
        var registerResult = service.RegisterComponentHealthCheck("CustomComponent",
            _ => Task.FromResult(customHealth));
        registerResult.IsSuccess.Should().BeTrue();

        // Act
        var result = service.UnregisterComponentHealthCheck("CustomComponent");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();

        // Verify component was unregistered
        var componentsResult = service.GetAvailableComponents();
        componentsResult.Value.Should().NotContain("CustomComponent");
    }

    [Fact]
    public void UnregisterComponentHealthCheck_NonExistentComponent_ShouldReturnSuccess()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.UnregisterComponentHealthCheck("NonExistentComponent");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();

        // Verify warning was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Attempted to unregister non-existent")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Event Tests

    [Fact]
    public async Task CheckHealthAsync_ComponentHealthChanges_ShouldTriggerEvent()
    {
        // Arrange
        var service = CreateService();
        ComponentHealthChangedEventArgs? eventArgs = null;

        service.ComponentHealthChanged += (sender, args) => eventArgs = args;

        // First check to establish baseline
        await service.CheckHealthAsync();

        // Change application health status
        _mockApplicationHealthCheck.Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ComponentHealth.Unhealthy(
                "Application",
                TimeSpan.FromMilliseconds(5),
                "Application is down"));

        // Act
        await service.CheckHealthAsync();

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.ComponentName.Should().Be("Application");
        eventArgs.PreviousStatus.Should().Be(HealthStatus.Healthy);
        eventArgs.CurrentStatus.Should().Be(HealthStatus.Unhealthy);
        eventArgs.ComponentHealth.Should().NotBeNull();
        eventArgs.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CheckHealthAsync_SystemHealthChanges_ShouldTriggerEvent()
    {
        // Arrange
        var service = CreateService();
        SystemHealthChangedEventArgs? eventArgs = null;

        service.SystemHealthChanged += (sender, args) => eventArgs = args;

        // First check to establish baseline
        await service.CheckHealthAsync();

        // Change application health status to trigger system health change
        _mockApplicationHealthCheck.Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ComponentHealth.Unhealthy(
                "Application",
                TimeSpan.FromMilliseconds(5),
                "Application is down"));

        // Act
        await service.CheckHealthAsync();

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.PreviousStatus.Should().Be(HealthStatus.Healthy);
        eventArgs.CurrentStatus.Should().Be(HealthStatus.Critical);
        eventArgs.PreviousHealthScore.Should().Be(100);
        eventArgs.CurrentHealthScore.Should().BeLessThan(100);
        eventArgs.HealthResponse.Should().NotBeNull();
        eventArgs.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_ShouldCleanupResources()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.Dispose();

        // Assert
        // Verify system resource health check was disposed
        _mockSystemResourceHealthCheck.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_MultipleDispose_ShouldNotThrow()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        service.Dispose();
        Action secondDispose = () => service.Dispose();
        secondDispose.Should().NotThrow();
    }

    #endregion

    #region Helper Methods

    private HealthCheckService CreateService()
    {
        var service = new HealthCheckService(
            _mockLogger.Object,
            _mockConfig.Object,
            _mockErrorService.Object,
            _applicationHealthCheck,
            _influxDbHealthCheck,
            _systemResourceHealthCheck);

        _servicesToDispose.Add(service);
        return service;
    }

    #endregion

    public void Dispose()
    {
        foreach (var service in _servicesToDispose)
        {
            try
            {
                service.Dispose();
            }
            catch
            {
                // Ignore disposal errors in tests
            }
        }
        _servicesToDispose.Clear();
    }
}
