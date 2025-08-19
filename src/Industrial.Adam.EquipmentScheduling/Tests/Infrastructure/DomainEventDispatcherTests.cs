using Industrial.Adam.EquipmentScheduling.Domain.Events;
using Industrial.Adam.EquipmentScheduling.Domain.Interfaces;
using Industrial.Adam.EquipmentScheduling.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Industrial.Adam.EquipmentScheduling.Tests.Infrastructure;

/// <summary>
/// Tests for the domain event dispatcher implementation
/// </summary>
public class DomainEventDispatcherTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly Mock<ILogger<DomainEventDispatcher>> _loggerMock;
    private readonly Mock<IDomainEventHandler<EquipmentScheduleCreatedEvent>> _handlerMock;
    private readonly IDomainEventDispatcher _dispatcher;

    public DomainEventDispatcherTests()
    {
        _loggerMock = new Mock<ILogger<DomainEventDispatcher>>();
        _handlerMock = new Mock<IDomainEventHandler<EquipmentScheduleCreatedEvent>>();

        var services = new ServiceCollection();
        services.AddSingleton(_loggerMock.Object);
        services.AddSingleton(_handlerMock.Object);
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        _serviceProvider = services.BuildServiceProvider();
        _dispatcher = _serviceProvider.GetRequiredService<IDomainEventDispatcher>();
    }

    [Fact]
    public async Task DispatchAsync_WithValidEvent_CallsRegisteredHandler()
    {
        // Arrange
        var domainEvent = new EquipmentScheduleCreatedEvent(
            ScheduleId: 1,
            ResourceId: 100,
            ScheduleDate: DateTime.Today,
            PlannedHours: 8.0m,
            IsException: false);

        _handlerMock
            .Setup(h => h.HandleAsync(It.IsAny<EquipmentScheduleCreatedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _dispatcher.DispatchAsync(domainEvent);

        // Assert
        _handlerMock.Verify(
            h => h.HandleAsync(It.Is<EquipmentScheduleCreatedEvent>(e => e.ScheduleId == 1), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_WithNullEvent_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _dispatcher.DispatchAsync(null!));
    }

    [Fact]
    public async Task DispatchManyAsync_WithMultipleEvents_CallsHandlerForEach()
    {
        // Arrange
        var events = new List<IDomainEvent>
        {
            new EquipmentScheduleCreatedEvent(1, 100, DateTime.Today, 8.0m, false),
            new EquipmentScheduleCreatedEvent(2, 100, DateTime.Today.AddDays(1), 8.0m, false)
        };

        _handlerMock
            .Setup(h => h.HandleAsync(It.IsAny<EquipmentScheduleCreatedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _dispatcher.DispatchManyAsync(events);

        // Assert
        _handlerMock.Verify(
            h => h.HandleAsync(It.IsAny<EquipmentScheduleCreatedEvent>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task DispatchManyAsync_WithEmptyCollection_DoesNotCallHandlers()
    {
        // Arrange
        var events = new List<IDomainEvent>();

        // Act
        await _dispatcher.DispatchManyAsync(events);

        // Assert
        _handlerMock.Verify(
            h => h.HandleAsync(It.IsAny<EquipmentScheduleCreatedEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DispatchManyAsync_WithNullCollection_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _dispatcher.DispatchManyAsync(null!));
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
