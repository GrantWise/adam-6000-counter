using FluentAssertions;
using Industrial.Adam.Oee.Domain.Entities;
using Industrial.Adam.Oee.Domain.Interfaces;
using Industrial.Adam.Oee.Domain.Services;
using Industrial.Adam.Oee.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Industrial.Adam.Oee.Tests.Domain.Services;

/// <summary>
/// Unit tests for the OeeCalculationService domain service
/// </summary>
public sealed class OeeCalculationServiceTests
{
    private readonly Mock<ICounterDataRepository> _counterDataRepositoryMock;
    private readonly Mock<IWorkOrderRepository> _workOrderRepositoryMock;
    private readonly Mock<IEquipmentAvailabilityService> _equipmentAvailabilityServiceMock;
    private readonly Mock<ILogger<OeeCalculationService>> _loggerMock;
    private readonly OeeCalculationService _sut;

    public OeeCalculationServiceTests()
    {
        _counterDataRepositoryMock = new Mock<ICounterDataRepository>();
        _workOrderRepositoryMock = new Mock<IWorkOrderRepository>();
        _equipmentAvailabilityServiceMock = new Mock<IEquipmentAvailabilityService>();
        _loggerMock = new Mock<ILogger<OeeCalculationService>>();
        _sut = new OeeCalculationService(
            _counterDataRepositoryMock.Object,
            _workOrderRepositoryMock.Object,
            _equipmentAvailabilityServiceMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public void Constructor_WithNullCounterDataRepository_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var action = () => new OeeCalculationService(
            null!,
            _workOrderRepositoryMock.Object,
            _equipmentAvailabilityServiceMock.Object,
            _loggerMock.Object
        );

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("counterDataRepository");
    }

    [Fact]
    public void Constructor_WithNullWorkOrderRepository_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var action = () => new OeeCalculationService(
            _counterDataRepositoryMock.Object,
            null!,
            _equipmentAvailabilityServiceMock.Object,
            _loggerMock.Object
        );

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("workOrderRepository");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var action = () => new OeeCalculationService(
            _counterDataRepositoryMock.Object,
            _workOrderRepositoryMock.Object,
            _equipmentAvailabilityServiceMock.Object,
            null!
        );

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task CalculateCurrentOeeAsync_WithNoActiveWorkOrder_ShouldReturnDefaultCalculation()
    {
        // Arrange
        var deviceId = "LINE-001";
        _workOrderRepositoryMock
            .Setup(x => x.GetActiveByDeviceAsync(deviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkOrder?)null);

        // Act
        var result = await _sut.CalculateCurrentOeeAsync(deviceId);

        // Assert
        result.Should().NotBeNull();
        result.ResourceReference.Should().Be(deviceId);
        result.OeePercentage.Should().Be(0);
    }

    [Fact]
    public async Task GetCalculationConfigurationAsync_ShouldReturnDefaultConfiguration()
    {
        // Arrange
        var deviceId = "LINE-001";

        // Act
        var result = await _sut.GetCalculationConfigurationAsync(deviceId);

        // Assert
        result.Should().NotBeNull();
        result.DeviceId.Should().Be(deviceId);
        result.ProductionChannel.Should().Be(0);
        result.RejectChannel.Should().Be(1);
        result.DefaultTargetRate.Should().Be(60m);
    }

    [Fact]
    public async Task UpdateCalculationConfigurationAsync_ShouldReturnTrue()
    {
        // Arrange
        var deviceId = "LINE-001";
        var configuration = new OeeCalculationConfiguration(deviceId);

        // Act
        var result = await _sut.UpdateCalculationConfigurationAsync(deviceId, configuration);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateDataSufficiencyAsync_WithInsufficientData_ShouldReturnInvalidResult()
    {
        // Arrange
        var deviceId = "LINE-001";
        var startTime = DateTime.UtcNow.AddHours(-1);
        var endTime = DateTime.UtcNow;
        var counterData = new List<Industrial.Adam.Oee.Domain.Interfaces.CounterReading>
        {
            new("LINE-001", 0, DateTime.UtcNow.AddMinutes(-30), 10, 100),
            new("LINE-001", 0, DateTime.UtcNow.AddMinutes(-20), 10, 200),
            new("LINE-001", 0, DateTime.UtcNow.AddMinutes(-10), 10, 300)
        }; // Only 3 data points, less than minimum 10

        _counterDataRepositoryMock
            .Setup(x => x.GetDataForPeriodAsync(deviceId, startTime, endTime, It.IsAny<CancellationToken>()))
            .ReturnsAsync(counterData);

        _counterDataRepositoryMock
            .Setup(x => x.HasProductionActivityAsync(deviceId, 0, startTime, endTime, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.ValidateDataSufficiencyAsync(deviceId, startTime, endTime, 10);

        // Assert
        result.IsValid.Should().BeFalse();
        result.DataPoints.Should().Be(3);
        result.MinimumRequired.Should().Be(10);
        result.Issues.Should().Contain(i => i.Contains("Insufficient data points"));
    }

    [Fact]
    public async Task ValidateDataSufficiencyAsync_WithShortTimePeriod_ShouldReturnInvalidResult()
    {
        // Arrange
        var deviceId = "LINE-001";
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddMinutes(2); // Very short period

        _counterDataRepositoryMock
            .Setup(x => x.GetDataForPeriodAsync(deviceId, startTime, endTime, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Industrial.Adam.Oee.Domain.Interfaces.CounterReading>());

        // Act
        var result = await _sut.ValidateDataSufficiencyAsync(deviceId, startTime, endTime);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Issues.Should().Contain(i => i.Contains("Time period too short"));
    }

    [Fact]
    public async Task ValidateDataSufficiencyAsync_WithNoProductionActivity_ShouldReturnInvalidResult()
    {
        // Arrange
        var deviceId = "LINE-001";
        var startTime = DateTime.UtcNow.AddHours(-1);
        var endTime = DateTime.UtcNow;
        var counterData = CreateSampleCounterData(deviceId, 15); // Enough data points

        _counterDataRepositoryMock
            .Setup(x => x.GetDataForPeriodAsync(deviceId, startTime, endTime, It.IsAny<CancellationToken>()))
            .ReturnsAsync(counterData);

        _counterDataRepositoryMock
            .Setup(x => x.HasProductionActivityAsync(deviceId, 0, startTime, endTime, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.ValidateDataSufficiencyAsync(deviceId, startTime, endTime);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Issues.Should().Contain(i => i.Contains("No production activity"));
    }

    [Fact]
    public async Task ValidateDataSufficiencyAsync_WithValidData_ShouldReturnValidResult()
    {
        // Arrange
        var deviceId = "LINE-001";
        var startTime = DateTime.UtcNow.AddHours(-1);
        var endTime = DateTime.UtcNow;
        var counterData = CreateSampleCounterData(deviceId, 15); // Enough data points

        _counterDataRepositoryMock
            .Setup(x => x.GetDataForPeriodAsync(deviceId, startTime, endTime, It.IsAny<CancellationToken>()))
            .ReturnsAsync(counterData);

        _counterDataRepositoryMock
            .Setup(x => x.HasProductionActivityAsync(deviceId, 0, startTime, endTime, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.ValidateDataSufficiencyAsync(deviceId, startTime, endTime);

        // Assert
        result.IsValid.Should().BeTrue();
        result.DataPoints.Should().Be(15);
        result.Issues.Should().BeEmpty();
    }

    [Fact]
    public async Task CalculateAvailabilityAsync_WithDowntimeRecords_ShouldUseProvidedRecords()
    {
        // Arrange
        var deviceId = "LINE-001";
        var startTime = DateTime.UtcNow.AddHours(-8);
        var endTime = DateTime.UtcNow;
        var plannedMinutes = 480m; // 8 hours
        var downtimeRecords = new[]
        {
            new Industrial.Adam.Oee.Domain.ValueObjects.DowntimeRecord(30, Industrial.Adam.Oee.Domain.ValueObjects.DowntimeCategory.Planned),
            new Industrial.Adam.Oee.Domain.ValueObjects.DowntimeRecord(45, Industrial.Adam.Oee.Domain.ValueObjects.DowntimeCategory.Unplanned)
        };

        // Act
        var result = await ((IOeeCalculationService)_sut).CalculateAvailabilityAsync(deviceId, startTime, endTime, downtimeRecords);

        // Assert
        result.PlannedTimeMinutes.Should().BeApproximately(plannedMinutes, 0.01m);
        result.ActualRunTimeMinutes.Should().BeApproximately(405m, 0.001m); // 480 - 30 - 45
        result.DowntimeMinutes.Should().Be(75);
        result.Percentage.Should().BeApproximately(84.375m, 0.01m);
    }

    [Fact]
    public async Task CalculateAvailabilityAsync_WithoutDowntimeRecords_ShouldUseCounterData()
    {
        // Arrange
        var deviceId = "LINE-001";
        var startTime = DateTime.UtcNow.AddHours(-8);
        var endTime = DateTime.UtcNow;
        var aggregates = new Industrial.Adam.Oee.Domain.Interfaces.CounterAggregates(
            deviceId, 0, startTime, endTime, 1000, 10, 15, 5, 420, 50);

        _counterDataRepositoryMock
            .Setup(x => x.GetAggregatedDataAsync(deviceId, 0, startTime, endTime, It.IsAny<CancellationToken>()))
            .ReturnsAsync(aggregates);

        // Act
        var result = await ((IOeeCalculationService)_sut).CalculateAvailabilityAsync(deviceId, startTime, endTime);

        // Assert
        result.PlannedTimeMinutes.Should().BeApproximately(480m, 0.01m); // 8 hours
        result.ActualRunTimeMinutes.Should().Be(420);
        result.Percentage.Should().BeApproximately(87.5m, 0.001m);
    }

    [Fact]
    public async Task CalculatePerformanceAsync_WithCounterData_ShouldCalculateCorrectly()
    {
        // Arrange
        var deviceId = "LINE-001";
        var startTime = DateTime.UtcNow.AddHours(-1);
        var endTime = DateTime.UtcNow;
        var targetRate = 10m; // 10 pieces per minute
        var aggregates = new Industrial.Adam.Oee.Domain.Interfaces.CounterAggregates(
            deviceId, 0, startTime, endTime, 450, 7.5m, 10, 5, 60, 20);

        _counterDataRepositoryMock
            .Setup(x => x.GetAggregatedDataAsync(deviceId, 0, startTime, endTime, It.IsAny<CancellationToken>()))
            .ReturnsAsync(aggregates);

        // Act
        var result = await _sut.CalculatePerformanceAsync(deviceId, startTime, endTime, targetRate);

        // Assert
        result.TotalPiecesProduced.Should().Be(450);
        result.TheoreticalMaxProduction.Should().Be(600); // 10 * 60
        result.Percentage.Should().Be(75);
        result.ActualRatePerMinute.Should().Be(450); // 7.5 * 60
    }

    [Fact]
    public async Task CalculateQualityAsync_WithCounterData_ShouldCalculateCorrectly()
    {
        // Arrange
        var deviceId = "LINE-001";
        var startTime = DateTime.UtcNow.AddHours(-1);
        var endTime = DateTime.UtcNow;
        var productionAggregates = new Industrial.Adam.Oee.Domain.Interfaces.CounterAggregates(
            deviceId, 0, startTime, endTime, 500, 8.33m, 10, 5, 60, 20);
        var rejectAggregates = new Industrial.Adam.Oee.Domain.Interfaces.CounterAggregates(
            deviceId, 1, startTime, endTime, 50, 0.83m, 2, 0, 60, 20);

        _counterDataRepositoryMock
            .Setup(x => x.GetAggregatedDataAsync(deviceId, 0, startTime, endTime, It.IsAny<CancellationToken>()))
            .ReturnsAsync(productionAggregates);

        _counterDataRepositoryMock
            .Setup(x => x.GetAggregatedDataAsync(deviceId, 1, startTime, endTime, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rejectAggregates);

        // Act
        var result = await _sut.CalculateQualityAsync(deviceId, startTime, endTime);

        // Assert
        result.GoodPieces.Should().Be(500);
        result.DefectivePieces.Should().Be(50);
        result.TotalPiecesProduced.Should().Be(550);
        result.Percentage.Should().BeApproximately(90.91m, 0.01m);
    }

    [Fact]
    public async Task DetectCurrentStoppageAsync_WithNoStoppage_ShouldReturnNull()
    {
        // Arrange
        var deviceId = "LINE-001";
        _counterDataRepositoryMock
            .Setup(x => x.GetDowntimePeriodsAsync(
                deviceId, 0, It.IsAny<DateTime>(), It.IsAny<DateTime>(), 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Industrial.Adam.Oee.Domain.Interfaces.DowntimePeriod>());

        // Act
        var result = await _sut.DetectCurrentStoppageAsync(deviceId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DetectCurrentStoppageAsync_WithOngoingStoppage_ShouldReturnStoppageInfo()
    {
        // Arrange
        var deviceId = "LINE-001";
        var stoppageStart = DateTime.UtcNow.AddMinutes(-30);
        var downtimePeriods = new[]
        {
            new Industrial.Adam.Oee.Domain.Interfaces.DowntimePeriod(stoppageStart, null, 30, true)
        };

        _counterDataRepositoryMock
            .Setup(x => x.GetDowntimePeriodsAsync(
                deviceId, 0, It.IsAny<DateTime>(), It.IsAny<DateTime>(), 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(downtimePeriods);

        // Act
        var result = await _sut.DetectCurrentStoppageAsync(deviceId);

        // Assert
        result.Should().NotBeNull();
        result!.StartTime.Should().Be(stoppageStart);
        result.DurationMinutes.Should().Be(30);
        result.IsActive.Should().BeTrue();
        result.EstimatedImpact.Should().NotBeNull();
    }

    private static List<Industrial.Adam.Oee.Domain.Interfaces.CounterReading> CreateSampleCounterData(string deviceId, int count)
    {
        var data = new List<Industrial.Adam.Oee.Domain.Interfaces.CounterReading>();
        var baseTime = DateTime.UtcNow.AddHours(-1);

        for (int i = 0; i < count; i++)
        {
            data.Add(new Industrial.Adam.Oee.Domain.Interfaces.CounterReading(
                deviceId,
                0,
                baseTime.AddMinutes(i * 4), // Every 4 minutes
                10, // 10 pieces per second rate
                (i + 1) * 100 // Cumulative count
            ));
        }

        return data;
    }
}
