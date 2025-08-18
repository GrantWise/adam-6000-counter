using Industrial.Adam.Oee.Application.Commands;
using Industrial.Adam.Oee.Application.Commands.Handlers;
using Industrial.Adam.Oee.Domain.Entities;
using Industrial.Adam.Oee.Domain.Interfaces;
using Industrial.Adam.Oee.Domain.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Industrial.Adam.Oee.Tests.Application.Commands;

/// <summary>
/// Unit tests for StartWorkOrderCommandHandler
/// </summary>
public class StartWorkOrderCommandHandlerTests
{
    private readonly Mock<IWorkOrderRepository> _mockWorkOrderRepository;
    private readonly Mock<ICounterDataRepository> _mockCounterDataRepository;
    private readonly Mock<IJobSequencingService> _mockJobSequencingService;
    private readonly Mock<IEquipmentLineService> _mockEquipmentLineService;
    private readonly Mock<ILogger<StartWorkOrderCommandHandler>> _mockLogger;
    private readonly StartWorkOrderCommandHandler _handler;

    public StartWorkOrderCommandHandlerTests()
    {
        _mockWorkOrderRepository = new Mock<IWorkOrderRepository>();
        _mockCounterDataRepository = new Mock<ICounterDataRepository>();
        _mockJobSequencingService = new Mock<IJobSequencingService>();
        _mockEquipmentLineService = new Mock<IEquipmentLineService>();
        _mockLogger = new Mock<ILogger<StartWorkOrderCommandHandler>>();

        _handler = new StartWorkOrderCommandHandler(
            _mockWorkOrderRepository.Object,
            _mockCounterDataRepository.Object,
            _mockJobSequencingService.Object,
            _mockEquipmentLineService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesAndStartsWorkOrder()
    {
        // Arrange
        var command = new StartWorkOrderCommand
        {
            WorkOrderId = "WO-001",
            WorkOrderDescription = "Test Work Order",
            ProductId = "PROD-001",
            ProductDescription = "Test Product",
            PlannedQuantity = 100,
            ScheduledStartTime = DateTime.UtcNow,
            ScheduledEndTime = DateTime.UtcNow.AddHours(8),
            LineId = "LINE-001",
            UnitOfMeasure = "pieces"
        };

        // Setup validation services
        _mockJobSequencingService
            .Setup(x => x.ValidateJobStartAsync(command.LineId, command.WorkOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(JobSequencingValidationResult.Success());

        var equipmentLine = new EquipmentLine("LINE-001", "Test Line", "DEVICE-001", 0);
        _mockEquipmentLineService
            .Setup(x => x.ValidateWorkOrderEquipmentAsync(command.WorkOrderId, command.LineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EquipmentValidationResult.Success(equipmentLine));

        _mockCounterDataRepository
            .Setup(x => x.GetLatestReadingAsync("DEVICE-001", 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CounterReading("DEVICE-001", 0, DateTime.UtcNow, 0, 50, null));

        _mockCounterDataRepository
            .Setup(x => x.GetLatestReadingAsync("DEVICE-001", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CounterReading("DEVICE-001", 1, DateTime.UtcNow, 0, 5, null));

        _mockWorkOrderRepository
            .Setup(x => x.CreateAsync(It.IsAny<WorkOrder>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(command.WorkOrderId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(command.WorkOrderId, result);

        _mockWorkOrderRepository.Verify(
            x => x.CreateAsync(It.Is<WorkOrder>(wo =>
                wo.Id == command.WorkOrderId &&
                wo.IsActive),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_JobSequencingValidationFails_ThrowsInvalidOperationException()
    {
        // Arrange
        var command = new StartWorkOrderCommand
        {
            WorkOrderId = "WO-001",
            LineId = "LINE-001"
        };

        _mockJobSequencingService
            .Setup(x => x.ValidateJobStartAsync(command.LineId, command.WorkOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(JobSequencingValidationResult.Failure(JobSequencingViolationType.OverlappingJob, "Validation failed"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));

        Assert.Contains("Validation failed", exception.Message);
    }

    [Fact]
    public async Task Handle_EquipmentValidationFails_ThrowsInvalidOperationException()
    {
        // Arrange
        var command = new StartWorkOrderCommand
        {
            WorkOrderId = "WO-001",
            LineId = "LINE-001"
        };

        _mockJobSequencingService
            .Setup(x => x.ValidateJobStartAsync(command.LineId, command.WorkOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(JobSequencingValidationResult.Success());

        _mockEquipmentLineService
            .Setup(x => x.ValidateWorkOrderEquipmentAsync(command.WorkOrderId, command.LineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EquipmentValidationResult.Failure(EquipmentValidationType.LineNotFound, "Equipment validation failed"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));

        Assert.Contains("Equipment validation failed", exception.Message);
    }

    [Fact]
    public async Task Handle_NoCounterData_StartsWithZeroCounts()
    {
        // Arrange
        var command = new StartWorkOrderCommand
        {
            WorkOrderId = "WO-001",
            WorkOrderDescription = "Test Work Order",
            ProductId = "PROD-001",
            ProductDescription = "Test Product",
            PlannedQuantity = 100,
            ScheduledStartTime = DateTime.UtcNow,
            ScheduledEndTime = DateTime.UtcNow.AddHours(8),
            LineId = "LINE-001"
        };

        // Setup validation services
        _mockJobSequencingService
            .Setup(x => x.ValidateJobStartAsync(command.LineId, command.WorkOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(JobSequencingValidationResult.Success());

        var equipmentLine = new EquipmentLine("LINE-001", "Test Line", "DEVICE-001", 0);
        _mockEquipmentLineService
            .Setup(x => x.ValidateWorkOrderEquipmentAsync(command.WorkOrderId, command.LineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EquipmentValidationResult.Success(equipmentLine));

        // No counter data available
        _mockCounterDataRepository
            .Setup(x => x.GetLatestReadingAsync("DEVICE-001", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CounterReading?)null);

        _mockWorkOrderRepository
            .Setup(x => x.CreateAsync(It.IsAny<WorkOrder>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(command.WorkOrderId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(command.WorkOrderId, result);

        _mockWorkOrderRepository.Verify(
            x => x.CreateAsync(It.IsAny<WorkOrder>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
