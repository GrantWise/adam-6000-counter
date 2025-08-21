using Industrial.Adam.EquipmentScheduling.Domain.Events;
using Industrial.Adam.EquipmentScheduling.Domain.Interfaces;
// using Industrial.Adam.EquipmentScheduling.Infrastructure.Services; // DISABLED: Services removed
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Industrial.Adam.EquipmentScheduling.Tests.Infrastructure;

/// <summary>
/// Tests for the domain event dispatcher implementation
/// TEMPORARILY DISABLED: DomainEventDispatcher removed pending reflection-free implementation
/// </summary>
[Trait("Category", "Disabled")]
public class DomainEventDispatcherTests : IDisposable
{
    // private readonly ServiceProvider _serviceProvider;
    // private readonly Mock<ILogger<DomainEventDispatcher>> _loggerMock;
    // private readonly Mock<IDomainEventHandler<EquipmentScheduleCreatedEvent>> _handlerMock;
    // private readonly IDomainEventDispatcher _dispatcher;

    public DomainEventDispatcherTests()
    {
        // DISABLED: DomainEventDispatcher removed pending reflection-free implementation
        /*
        _loggerMock = new Mock<ILogger<DomainEventDispatcher>>();
        _handlerMock = new Mock<IDomainEventHandler<EquipmentScheduleCreatedEvent>>();

        var services = new ServiceCollection();
        services.AddSingleton(_loggerMock.Object);
        services.AddSingleton(_handlerMock.Object);
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        _serviceProvider = services.BuildServiceProvider();
        _dispatcher = _serviceProvider.GetRequiredService<IDomainEventDispatcher>();
        */
    }

    [Fact(Skip = "DomainEventDispatcher removed pending reflection-free implementation")]
    public async Task DispatchAsync_WithValidEvent_CallsRegisteredHandler()
    {
        await Task.CompletedTask; // Placeholder to avoid async warning
        /*
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
        */
    }

    [Fact(Skip = "DomainEventDispatcher removed pending reflection-free implementation")]
    public async Task DispatchAsync_WithNullEvent_ThrowsArgumentNullException()
    {
        await Task.CompletedTask; // Placeholder to avoid async warning
        // await Assert.ThrowsAsync<ArgumentNullException>(() => _dispatcher.DispatchAsync(null!));
    }

    [Fact(Skip = "DomainEventDispatcher removed pending reflection-free implementation")]
    public async Task DispatchManyAsync_WithMultipleEvents_CallsHandlerForEach()
    {
        await Task.CompletedTask; // Placeholder to avoid async warning
        // Implementation commented out - DomainEventDispatcher removed
    }

    [Fact(Skip = "DomainEventDispatcher removed pending reflection-free implementation")]
    public async Task DispatchManyAsync_WithEmptyCollection_DoesNotCallHandlers()
    {
        await Task.CompletedTask; // Placeholder to avoid async warning
        // Implementation commented out - DomainEventDispatcher removed
    }

    [Fact(Skip = "DomainEventDispatcher removed pending reflection-free implementation")]
    public async Task DispatchManyAsync_WithNullCollection_ThrowsArgumentNullException()
    {
        await Task.CompletedTask; // Placeholder to avoid async warning
        // Implementation commented out - DomainEventDispatcher removed
    }

    public void Dispose()
    {
        // _serviceProvider?.Dispose(); // DISABLED: DomainEventDispatcher removed
    }
}
