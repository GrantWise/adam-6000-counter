using Industrial.Adam.Oee.Application.Events;
using Industrial.Adam.Oee.Domain.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Oee.Infrastructure.Services;

/// <summary>
/// Simple, type-safe event dispatcher without reflection
/// Follows Logger module patterns for simplicity and reliability
/// </summary>
public sealed class SimpleEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SimpleEventDispatcher> _logger;

    /// <summary>
    /// Initialize the simple event dispatcher
    /// </summary>
    public SimpleEventDispatcher(
        IServiceProvider serviceProvider,
        ILogger<SimpleEventDispatcher> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Dispatch a stoppage detected event directly to its handler
    /// Type-safe and compile-time verified
    /// </summary>
    public async Task DispatchAsync(StoppageDetectedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (domainEvent == null)
            throw new ArgumentNullException(nameof(domainEvent));

        try
        {
            _logger.LogDebug("Dispatching stoppage detected event {EventId} for stoppage {StoppageId}",
                domainEvent.EventId, domainEvent.StoppageId);

            // Get the specific handler - type-safe, no reflection
            var handler = _serviceProvider.GetRequiredService<StoppageDetectedEventHandler>();

            // Call the handler directly - compile-time safe
            await handler.HandleAsync(domainEvent, cancellationToken);

            _logger.LogDebug("Successfully dispatched stoppage detected event {EventId}", domainEvent.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch stoppage detected event {EventId}", domainEvent.EventId);
            throw;
        }
    }

    /// <summary>
    /// Generic dispatch method for future events - type-safe approach
    /// Add specific overloads as new event types are added
    /// </summary>
    public async Task DispatchEventAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        if (domainEvent == null)
            throw new ArgumentNullException(nameof(domainEvent));

        // Handle each event type explicitly - no reflection needed
        switch (domainEvent)
        {
            case StoppageDetectedEvent stoppageEvent:
                await DispatchAsync(stoppageEvent, cancellationToken);
                break;

            default:
                _logger.LogWarning("No handler registered for event type {EventType}", typeof(TEvent).Name);
                break;
        }
    }
}
