using FluentAssertions;
using Industrial.Adam.Oee.Domain.Entities;
using Industrial.Adam.Oee.Domain.Events;
using Industrial.Adam.Oee.Domain.Interfaces;
using Industrial.Adam.Oee.Domain.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Industrial.Adam.Oee.Tests.Domain.Services;

/// <summary>
/// Unit tests for StoppageDetectionService
/// </summary>
public class StoppageDetectionServiceTests
{
    private readonly Mock<ICounterDataRepository> _mockCounterDataRepository;
    private readonly Mock<IEquipmentLineRepository> _mockEquipmentLineRepository;
    private readonly Mock<IEquipmentStoppageRepository> _mockStoppageRepository;
    private readonly Mock<IWorkOrderRepository> _mockWorkOrderRepository;
    private readonly Mock<ILogger<StoppageDetectionService>> _mockLogger;
    private readonly StoppageDetectionService _service;

    public StoppageDetectionServiceTests()
    {
        _mockCounterDataRepository = new Mock<ICounterDataRepository>();
        _mockEquipmentLineRepository = new Mock<IEquipmentLineRepository>();
        _mockStoppageRepository = new Mock<IEquipmentStoppageRepository>();
        _mockWorkOrderRepository = new Mock<IWorkOrderRepository>();
        _mockLogger = new Mock<ILogger<StoppageDetectionService>>();

        _service = new StoppageDetectionService(
            _mockCounterDataRepository.Object,
            _mockEquipmentLineRepository.Object,
            _mockStoppageRepository.Object,
            _mockWorkOrderRepository.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetDetectionThresholdAsync_WithValidLineId_ReturnsDefaultThreshold()
    {
        // Arrange
        const string lineId = "LINE001";
        var equipmentLine = CreateTestEquipmentLine(lineId);

        _mockEquipmentLineRepository
            .Setup(x => x.GetByLineIdAsync(lineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(equipmentLine);

        // Act
        var threshold = await _service.GetDetectionThresholdAsync(lineId);

        // Assert
        threshold.Should().Be(5); // Default threshold
    }

    [Fact]
    public async Task GetDetectionThresholdAsync_WithInvalidLineId_ReturnsDefaultThreshold()
    {
        // Arrange
        const string lineId = "INVALID";

        _mockEquipmentLineRepository
            .Setup(x => x.GetByLineIdAsync(lineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EquipmentLine?)null);

        // Act
        var threshold = await _service.GetDetectionThresholdAsync(lineId);

        // Assert
        threshold.Should().Be(5); // Default threshold even when line not found
    }

    [Fact]
    public async Task IsLineStopped_WithActiveStoppage_ReturnsTrue()
    {
        // Arrange
        const string lineId = "LINE001";
        var lastProductionTime = DateTime.UtcNow.AddMinutes(-10); // 10 minutes ago

        SetupLastProductionTime(lineId, lastProductionTime);

        // Act
        var isStopped = await _service.IsLineStopped(lineId);

        // Assert
        isStopped.Should().BeTrue();
    }

    [Fact]
    public async Task IsLineStopped_WithRecentProduction_ReturnsFalse()
    {
        // Arrange
        const string lineId = "LINE001";
        var lastProductionTime = DateTime.UtcNow.AddMinutes(-2); // 2 minutes ago (below threshold)

        SetupLastProductionTime(lineId, lastProductionTime);

        // Act
        var isStopped = await _service.IsLineStopped(lineId);

        // Assert
        isStopped.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateStoppageCreationAsync_WithExistingActiveStoppage_ReturnsUseExisting()
    {
        // Arrange
        const string lineId = "LINE001";
        var lastProductionTime = DateTime.UtcNow.AddMinutes(-10);
        var existingStoppage = CreateTestStoppage(lineId);

        _mockStoppageRepository
            .Setup(x => x.GetActiveByLineAsync(lineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingStoppage);

        // Act
        var result = await _service.ValidateStoppageCreationAsync(lineId, lastProductionTime);

        // Assert
        result.ShouldCreateStoppage.Should().BeFalse();
        result.ExistingStoppage.Should().Be(existingStoppage);
        result.Reason.Should().Contain("Active stoppage already exists");
    }

    [Fact]
    public async Task ValidateStoppageCreationAsync_WithNoActiveStoppageAndAboveThreshold_ReturnsCreateStoppage()
    {
        // Arrange
        const string lineId = "LINE001";
        var lastProductionTime = DateTime.UtcNow.AddMinutes(-10); // Above 5-minute threshold

        _mockStoppageRepository
            .Setup(x => x.GetActiveByLineAsync(lineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EquipmentStoppage?)null);

        SetupEquipmentLineForThreshold(lineId);

        // Act
        var result = await _service.ValidateStoppageCreationAsync(lineId, lastProductionTime);

        // Assert
        result.ShouldCreateStoppage.Should().BeTrue();
        result.ExistingStoppage.Should().BeNull();
        result.StoppageDuration.Should().NotBeNull();
        result.StoppageDuration!.Value.TotalMinutes.Should().BeGreaterThan(5);
    }

    [Fact]
    public async Task ValidateStoppageCreationAsync_WithNoActiveStoppageBelowThreshold_ReturnsNoAction()
    {
        // Arrange
        const string lineId = "LINE001";
        var lastProductionTime = DateTime.UtcNow.AddMinutes(-2); // Below 5-minute threshold

        _mockStoppageRepository
            .Setup(x => x.GetActiveByLineAsync(lineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EquipmentStoppage?)null);

        SetupEquipmentLineForThreshold(lineId);

        // Act
        var result = await _service.ValidateStoppageCreationAsync(lineId, lastProductionTime);

        // Assert
        result.ShouldCreateStoppage.Should().BeFalse();
        result.ExistingStoppage.Should().BeNull();
        result.Reason.Should().Contain("below threshold");
    }

    [Fact]
    public async Task CreateDetectedStoppageAsync_WithValidData_CreatesStoppageSuccessfully()
    {
        // Arrange
        const string lineId = "LINE001";
        const string workOrderId = "WO123";
        var startTime = DateTime.UtcNow.AddMinutes(-10);
        const int threshold = 5;

        _mockStoppageRepository
            .Setup(x => x.CreateAsync(It.IsAny<EquipmentStoppage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var stoppage = await _service.CreateDetectedStoppageAsync(lineId, startTime, workOrderId, threshold);

        // Assert
        stoppage.Should().NotBeNull();
        stoppage.LineId.Should().Be(lineId);
        stoppage.WorkOrderId.Should().Be(workOrderId);
        stoppage.StartTime.Should().Be(startTime);
        stoppage.AutoDetected.Should().BeTrue();
        stoppage.MinimumThresholdMinutes.Should().Be(threshold);

        _mockStoppageRepository.Verify(
            x => x.CreateAsync(It.IsAny<EquipmentStoppage>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EndActiveStoppageAsync_WithActiveStoppage_EndsStoppageSuccessfully()
    {
        // Arrange
        const string lineId = "LINE001";
        var activeStoppage = CreateTestStoppage(lineId);
        var endTime = DateTime.UtcNow;

        _mockStoppageRepository
            .Setup(x => x.GetActiveByLineAsync(lineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeStoppage);

        _mockStoppageRepository
            .Setup(x => x.UpdateAsync(It.IsAny<EquipmentStoppage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.EndActiveStoppageAsync(lineId, endTime);

        // Assert
        result.Should().NotBeNull();
        result!.EndTime.Should().Be(endTime);
        result.DurationMinutes.Should().BeGreaterThan(0);

        _mockStoppageRepository.Verify(
            x => x.UpdateAsync(It.IsAny<EquipmentStoppage>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EndActiveStoppageAsync_WithNoActiveStoppage_ReturnsNull()
    {
        // Arrange
        const string lineId = "LINE001";

        _mockStoppageRepository
            .Setup(x => x.GetActiveByLineAsync(lineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EquipmentStoppage?)null);

        // Act
        var result = await _service.EndActiveStoppageAsync(lineId);

        // Assert
        result.Should().BeNull();

        _mockStoppageRepository.Verify(
            x => x.UpdateAsync(It.IsAny<EquipmentStoppage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(3, false)] // Below threshold
    [InlineData(5, true)]  // At threshold
    [InlineData(10, true)] // Above threshold
    public async Task ShouldTriggerAlertAsync_WithVariousThresholds_ReturnsExpectedResult(
        int stoppageMinutes,
        bool expectedResult)
    {
        // Arrange
        const string lineId = "LINE001";
        var stoppageDuration = TimeSpan.FromMinutes(stoppageMinutes);

        SetupEquipmentLineForThreshold(lineId);

        // Act
        var shouldTrigger = await _service.ShouldTriggerAlertAsync(lineId, stoppageDuration);

        // Assert
        shouldTrigger.Should().Be(expectedResult);
    }

    [Fact]
    public async Task GetActiveMonitoringLinesAsync_WithActiveLines_ReturnsFilteredLines()
    {
        // Arrange
        var allLines = new[]
        {
            CreateTestEquipmentLine("LINE001"),
            CreateTestEquipmentLine("LINE002"),
            CreateTestEquipmentLine("LINE003")
        };

        _mockEquipmentLineRepository
            .Setup(x => x.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allLines);

        // Setup work orders for some lines
        _mockWorkOrderRepository
            .Setup(x => x.GetActiveByLineAsync("LINE001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestWorkOrder("WO001", "LINE001"));

        _mockWorkOrderRepository
            .Setup(x => x.GetActiveByLineAsync("LINE002", It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkOrder?)null);

        _mockWorkOrderRepository
            .Setup(x => x.GetActiveByLineAsync("LINE003", It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkOrder?)null);

        // Setup recent activity for lines without work orders
        _mockCounterDataRepository
            .Setup(x => x.HasProductionActivityAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var activeLines = await _service.GetActiveMonitoringLinesAsync();

        // Assert
        activeLines.Should().NotBeEmpty();
        activeLines.Should().HaveCount(3); // All lines should be included
    }

    // Helper methods

    private static EquipmentLine CreateTestEquipmentLine(string lineId)
    {
        return new EquipmentLine(
            1,
            lineId,
            $"Line {lineId}",
            "ADAM001",
            1,
            true,
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow);
    }

    private static EquipmentStoppage CreateTestStoppage(string lineId)
    {
        return new EquipmentStoppage(
            lineId,
            DateTime.UtcNow.AddMinutes(-10),
            "WO123",
            autoDetected: true,
            minimumThresholdMinutes: 5);
    }

    private static WorkOrder CreateTestWorkOrder(string workOrderId, string lineId)
    {
        return new WorkOrder(
            workOrderId,
            "Test Work Order",
            "PROD001",
            "Test Product",
            1000,
            DateTime.UtcNow.AddHours(-2),
            DateTime.UtcNow.AddHours(2),
            lineId,
            "pieces",
            0,
            0,
            DateTime.UtcNow,
            null,
            WorkOrderStatus.Active);
    }

    private void SetupLastProductionTime(string lineId, DateTime? lastProductionTime)
    {
        var equipmentLine = CreateTestEquipmentLine(lineId);

        _mockEquipmentLineRepository
            .Setup(x => x.GetByLineIdAsync(lineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(equipmentLine);

        if (lastProductionTime.HasValue)
        {
            var reading = new CounterReading(
                equipmentLine.AdamDeviceId,
                equipmentLine.AdamChannel,
                lastProductionTime.Value,
                10, // rate > 0 indicates production
                100,
                "Good");

            _mockCounterDataRepository
                .Setup(x => x.GetLatestReadingAsync(
                    equipmentLine.AdamDeviceId,
                    equipmentLine.AdamChannel,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(reading);
        }
        else
        {
            _mockCounterDataRepository
                .Setup(x => x.GetLatestReadingAsync(
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((CounterReading?)null);
        }
    }

    private void SetupEquipmentLineForThreshold(string lineId)
    {
        var equipmentLine = CreateTestEquipmentLine(lineId);

        _mockEquipmentLineRepository
            .Setup(x => x.GetByLineIdAsync(lineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(equipmentLine);
    }
}
