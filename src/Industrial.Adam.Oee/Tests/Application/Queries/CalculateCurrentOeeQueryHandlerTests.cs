using Industrial.Adam.Oee.Application.DTOs;
using Industrial.Adam.Oee.Application.Queries;
using Industrial.Adam.Oee.Application.Queries.Handlers;
using Industrial.Adam.Oee.Domain.Entities;
using Industrial.Adam.Oee.Domain.Interfaces;
using Industrial.Adam.Oee.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Industrial.Adam.Oee.Tests.Application.Queries;

/// <summary>
/// Unit tests for CalculateCurrentOeeQueryHandler
/// </summary>
public class CalculateCurrentOeeQueryHandlerTests
{
    private readonly Mock<IOeeCalculationService> _mockOeeCalculationService;
    private readonly Mock<IWorkOrderRepository> _mockWorkOrderRepository;
    private readonly Mock<ILogger<CalculateCurrentOeeQueryHandler>> _mockLogger;
    private readonly CalculateCurrentOeeQueryHandler _handler;

    public CalculateCurrentOeeQueryHandlerTests()
    {
        _mockOeeCalculationService = new Mock<IOeeCalculationService>();
        _mockWorkOrderRepository = new Mock<IWorkOrderRepository>();
        _mockLogger = new Mock<ILogger<CalculateCurrentOeeQueryHandler>>();

        _handler = new CalculateCurrentOeeQueryHandler(
            _mockOeeCalculationService.Object,
            _mockWorkOrderRepository.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WithSpecificTimeRange_CalculatesOeeForPeriod()
    {
        // Arrange
        var deviceId = "DEVICE-001";
        var startTime = DateTime.UtcNow.AddHours(-8);
        var endTime = DateTime.UtcNow;

        var query = new CalculateCurrentOeeQuery(deviceId, startTime, endTime);

        var oeeCalculation = CreateSampleOeeCalculation(deviceId, startTime, endTime);

        _mockOeeCalculationService
            .Setup(x => x.CalculateOeeForPeriodAsync(deviceId, startTime, endTime, It.IsAny<CancellationToken>()))
            .ReturnsAsync(oeeCalculation);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(deviceId, result.ResourceReference);
        Assert.Equal(85.5m, result.OeePercentage);
        Assert.Equal("Availability", result.WorstFactor);

        _mockOeeCalculationService.Verify(
            x => x.CalculateOeeForPeriodAsync(deviceId, startTime, endTime, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithActiveWorkOrder_CalculatesOeeForWorkOrder()
    {
        // Arrange
        var deviceId = "DEVICE-001";
        var query = new CalculateCurrentOeeQuery(deviceId);

        var activeWorkOrder = new WorkOrder(
            "WO-001", "Test Work Order", "PROD-001", "Test Product", 100,
            DateTime.UtcNow.AddHours(-4), DateTime.UtcNow.AddHours(4), deviceId);
        activeWorkOrder.Start();

        var oeeCalculation = CreateSampleOeeCalculation(deviceId,
            activeWorkOrder.ActualStartTime!.Value, DateTime.UtcNow);

        _mockWorkOrderRepository
            .Setup(x => x.GetActiveByDeviceAsync(deviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeWorkOrder);

        _mockOeeCalculationService
            .Setup(x => x.CalculateOeeForWorkOrderAsync(activeWorkOrder, It.IsAny<CancellationToken>()))
            .ReturnsAsync(oeeCalculation);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(deviceId, result.ResourceReference);

        _mockWorkOrderRepository.Verify(
            x => x.GetActiveByDeviceAsync(deviceId, It.IsAny<CancellationToken>()),
            Times.Once);

        _mockOeeCalculationService.Verify(
            x => x.CalculateOeeForWorkOrderAsync(activeWorkOrder, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NoActiveWorkOrder_CalculatesCurrentOee()
    {
        // Arrange
        var deviceId = "DEVICE-001";
        var query = new CalculateCurrentOeeQuery(deviceId);

        var oeeCalculation = CreateSampleOeeCalculation(deviceId,
            DateTime.UtcNow.AddHours(-1), DateTime.UtcNow);

        _mockWorkOrderRepository
            .Setup(x => x.GetActiveByDeviceAsync(deviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkOrder?)null);

        _mockOeeCalculationService
            .Setup(x => x.CalculateCurrentOeeAsync(deviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(oeeCalculation);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(deviceId, result.ResourceReference);

        _mockWorkOrderRepository.Verify(
            x => x.GetActiveByDeviceAsync(deviceId, It.IsAny<CancellationToken>()),
            Times.Once);

        _mockOeeCalculationService.Verify(
            x => x.CalculateCurrentOeeAsync(deviceId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ServiceThrowsException_PropagatesException()
    {
        // Arrange
        var deviceId = "DEVICE-001";
        var query = new CalculateCurrentOeeQuery(deviceId);

        _mockWorkOrderRepository
            .Setup(x => x.GetActiveByDeviceAsync(deviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkOrder?)null);

        _mockOeeCalculationService
            .Setup(x => x.CalculateCurrentOeeAsync(deviceId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(query, CancellationToken.None));

        Assert.Equal("Test exception", exception.Message);
    }

    /// <summary>
    /// Create a sample OEE calculation for testing
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="startTime">Start time</param>
    /// <param name="endTime">End time</param>
    /// <returns>Sample OEE calculation</returns>
    private static OeeCalculation CreateSampleOeeCalculation(string deviceId, DateTime startTime, DateTime endTime)
    {
        var availability = new Availability(450, 480); // 93.75%
        var performance = new Performance(90, 100, 1); // 90%
        var quality = new Quality(95, 100); // 95%

        return new OeeCalculation(
            null, // Let it generate ID
            deviceId,
            startTime,
            endTime,
            availability,
            performance,
            quality);
    }
}
