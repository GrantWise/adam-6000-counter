using System.Diagnostics;
using Industrial.Adam.EquipmentScheduling.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.EquipmentScheduling.Infrastructure.Services;

/// <summary>
/// Domain event dispatcher implementation using dependency injection container
/// for automatic event handler resolution and execution
/// </summary>
public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DomainEventDispatcher> _logger;
    private static readonly ActivitySource ActivitySource = new("Industrial.Adam.EquipmentScheduling.Infrastructure");

    /// <summary>
    /// Initializes a new instance of the DomainEventDispatcher
    /// </summary>
    /// <param name="serviceProvider">Service provider for resolving event handlers</param>
    /// <param name="logger">Logger instance</param>
    public DomainEventDispatcher(
        IServiceProvider serviceProvider,
        ILogger<DomainEventDispatcher> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Dispatches a single domain event to all registered handlers
    /// </summary>
    /// <param name="domainEvent">The domain event to dispatch</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (domainEvent == null)
            throw new ArgumentNullException(nameof(domainEvent));

        using var activity = ActivitySource.StartActivity("DispatchDomainEvent");
        activity?.SetTag("event.type", domainEvent.GetType().Name);
        activity?.SetTag("event.id", domainEvent.Id.ToString());

        try
        {
            _logger.LogDebug("Dispatching domain event {EventType} with ID {EventId}",
                domainEvent.GetType().Name, domainEvent.Id);

            await DispatchToHandlersAsync(domainEvent, cancellationToken);

            _logger.LogDebug("Successfully dispatched domain event {EventType} with ID {EventId}",
                domainEvent.GetType().Name, domainEvent.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch domain event {EventType} with ID {EventId}",
                domainEvent.GetType().Name, domainEvent.Id);
            throw;
        }
    }

    /// <summary>
    /// Dispatches multiple domain events to all registered handlers
    /// </summary>
    /// <param name="domainEvents">The collection of domain events to dispatch</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public async Task DispatchManyAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        if (domainEvents == null)
            throw new ArgumentNullException(nameof(domainEvents));

        var events = domainEvents.ToList();
        if (events.Count == 0)
            return;

        using var activity = ActivitySource.StartActivity("DispatchManyDomainEvents");
        activity?.SetTag("event.count", events.Count);

        try
        {
            _logger.LogDebug("Dispatching {EventCount} domain events", events.Count);

            // Process events sequentially to maintain order and handle dependencies
            foreach (var domainEvent in events)
            {
                await DispatchToHandlersAsync(domainEvent, cancellationToken);
            }

            _logger.LogInformation("Successfully dispatched {EventCount} domain events", events.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch domain events batch of {EventCount} events", events.Count);
            throw;
        }
    }

    /// <summary>
    /// Dispatches a domain event to all registered handlers using reflection and DI container
    /// </summary>
    /// <param name="domainEvent">The domain event to dispatch</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    private async Task DispatchToHandlersAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var eventType = domainEvent.GetType();
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);

        // Resolve all handlers for this event type from the DI container
        var handlers = _serviceProvider.GetServices(handlerType);
        var handlersList = handlers.ToList();

        if (handlersList.Count == 0)
        {
            _logger.LogDebug("No handlers registered for domain event {EventType}", eventType.Name);
            return;
        }

        _logger.LogDebug("Found {HandlerCount} handlers for domain event {EventType}",
            handlersList.Count, eventType.Name);

        var handleTasks = new List<Task>();

        foreach (var handler in handlersList)
        {
            try
            {
                // Use reflection to call HandleAsync method on the handler
                var handleMethod = handler.GetType().GetMethod("HandleAsync");
                if (handleMethod != null)
                {
                    var result = handleMethod.Invoke(handler, new object[] { domainEvent, cancellationToken });
                    if (result is Task task)
                    {
                        handleTasks.Add(task);
                    }
                }
                else
                {
                    _logger.LogWarning("Handler {HandlerType} does not implement HandleAsync method",
                        handler.GetType().Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invoking handler {HandlerType} for event {EventType}",
                    handler.GetType().Name, eventType.Name);

                // Continue processing other handlers even if one fails
                // This provides resilience and prevents one failing handler from stopping the entire pipeline
            }
        }

        // Wait for all handlers to complete
        if (handleTasks.Count > 0)
        {
            try
            {
                await Task.WhenAll(handleTasks);
                _logger.LogDebug("All {HandlerCount} handlers completed for event {EventType}",
                    handlersList.Count, eventType.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "One or more handlers failed for event {EventType}", eventType.Name);
                throw;
            }
        }
    }
}