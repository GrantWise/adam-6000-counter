using Industrial.Adam.Oee.Domain.Enums;
using Industrial.Adam.Oee.Domain.Exceptions;
using Industrial.Adam.Oee.Domain.Interfaces;
using Industrial.Adam.Oee.Domain.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Industrial.Adam.Oee.Tests.Domain.Services;

/// <summary>
/// Tests for availability calculation service
/// </summary>
public sealed class AvailabilityCalculationServiceTests
{
    private readonly Mock<ICounterDataRepository> _mockCounterDataRepository;
    private readonly Mock<ILogger<AvailabilityCalculationService>> _mockLogger;
    private readonly AvailabilityCalculationService _service;

    public AvailabilityCalculationServiceTests()
    {
        _mockCounterDataRepository = new Mock<ICounterDataRepository>();
        _mockLogger = new Mock<ILogger<AvailabilityCalculationService>>();
        _service = new AvailabilityCalculationService(_mockCounterDataRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task CalculateAsync_WithValidParameters_ReturnsAvailability()
    {
        // Arrange
        var deviceId = "TEST-001";
        var startTime = DateTime.UtcNow.AddHours(-1);
        var endTime = DateTime.UtcNow;

        var mockAggregates = new Industrial.Adam.Oee.Domain.Interfaces.CounterAggregates(
            "TEST-001", 0, startTime, endTime, 100, 2.0m, 3.0m, 1.0m, 45, 10
        );

        _mockCounterDataRepository
            .Setup(x => x.GetAggregatedDataAsync(deviceId, 0, startTime, endTime, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockAggregates);

        // Act
        var result = await _service.CalculateAsync(deviceId, startTime, endTime);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(60m, result.PlannedTimeMinutes); // 1 hour
        Assert.Equal(45m, result.ActualRunTimeMinutes);
        Assert.Equal(75m, result.Percentage); // 45/60 * 100
    }

    [Fact]
    public async Task CalculateAsync_WithNullDeviceId_ThrowsArgumentException()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddHours(-1);
        var endTime = DateTime.UtcNow;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CalculateAsync(null!, startTime, endTime));
    }

    [Fact]
    public async Task CalculateAsync_WithInvalidTimeRange_ThrowsArgumentException()
    {
        // Arrange
        var deviceId = "TEST-001";
        var startTime = DateTime.UtcNow;
        var endTime = DateTime.UtcNow.AddHours(-1); // End before start

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CalculateAsync(deviceId, startTime, endTime));
    }

    [Fact]
    public async Task CalculateAsync_WithRepositoryException_ThrowsOeeCalculationException()
    {
        // Arrange
        var deviceId = "TEST-001";
        var startTime = DateTime.UtcNow.AddHours(-1);
        var endTime = DateTime.UtcNow;

        _mockCounterDataRepository
            .Setup(x => x.GetAggregatedDataAsync(deviceId, 0, startTime, endTime, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<OeeCalculationException>(() =>
            _service.CalculateAsync(deviceId, startTime, endTime));

        Assert.Equal(OeeErrorCode.AvailabilityCalculationFailed, exception.ErrorCode);
        Assert.Equal(deviceId, exception.DeviceId);
        Assert.Contains("Database error", exception.InnerException?.Message);
    }

    [Fact]
    public async Task DetectDowntimeAsync_WithValidParameters_ReturnsDowntimePeriods()
    {
        // Arrange
        var deviceId = "TEST-001";
        var startTime = DateTime.UtcNow.AddHours(-2);
        var endTime = DateTime.UtcNow;
        var minimumStoppageMinutes = 5;

        var mockDowntimePeriods = new List<Industrial.Adam.Oee.Domain.Interfaces.DowntimePeriod>
        {
            new(startTime.AddMinutes(30), startTime.AddMinutes(40), 10, false),
            new(startTime.AddMinutes(60), null, 60, true)
        };

        _mockCounterDataRepository
            .Setup(x => x.GetDowntimePeriodsAsync(deviceId, 0, startTime, endTime, minimumStoppageMinutes, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockDowntimePeriods);

        // Act
        var result = await _service.DetectDowntimeAsync(deviceId, startTime, endTime, minimumStoppageMinutes);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Contains(result, p => p.DurationMinutes == 10 && !p.IsOngoing);
        Assert.Contains(result, p => p.DurationMinutes == 60 && p.IsOngoing);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task DetectDowntimeAsync_WithInvalidDeviceId_ThrowsArgumentException(string? deviceId)
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddHours(-1);
        var endTime = DateTime.UtcNow;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.DetectDowntimeAsync(deviceId!, startTime, endTime));
    }

    [Fact]
    public async Task CalculateActualRuntimeAsync_WithValidParameters_ReturnsRuntime()
    {
        // Arrange
        var deviceId = "TEST-001";
        var startTime = DateTime.UtcNow.AddHours(-1);
        var endTime = DateTime.UtcNow;

        var mockAggregates = new Industrial.Adam.Oee.Domain.Interfaces.CounterAggregates(
            deviceId, 0, startTime, endTime, 120, 2.18m, 3.0m, 1.0m, 55, 10
        );

        _mockCounterDataRepository
            .Setup(x => x.GetAggregatedDataAsync(deviceId, 0, startTime, endTime, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockAggregates);

        // Act
        var result = await _service.CalculateActualRuntimeAsync(deviceId, startTime, endTime);

        // Assert
        Assert.Equal(55m, result);
    }

    [Fact]
    public async Task CalculateActualRuntimeAsync_WithNoData_ReturnsZero()
    {
        // Arrange
        var deviceId = "TEST-001";
        var startTime = DateTime.UtcNow.AddHours(-1);
        var endTime = DateTime.UtcNow;

        _mockCounterDataRepository
            .Setup(x => x.GetAggregatedDataAsync(deviceId, 0, startTime, endTime, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Industrial.Adam.Oee.Domain.Interfaces.CounterAggregates?)null);

        // Act
        var result = await _service.CalculateActualRuntimeAsync(deviceId, startTime, endTime);

        // Assert
        Assert.Equal(0m, result);
    }
}

