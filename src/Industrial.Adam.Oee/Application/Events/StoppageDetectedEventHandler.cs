using Industrial.Adam.Oee.Application.Interfaces;
using Industrial.Adam.Oee.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Oee.Application.Events;

/// <summary>
/// Application event handler for stoppage detected events
/// Coordinates notification and workflow actions when stoppages are detected
/// </summary>
public sealed class StoppageDetectedEventHandler : IEventHandler<StoppageDetectedEvent>
{
    private readonly IStoppageNotificationService _notificationService;
    private readonly ILogger<StoppageDetectedEventHandler> _logger;

    /// <summary>
    /// Constructor for stoppage detected event handler
    /// </summary>
    public StoppageDetectedEventHandler(
        IStoppageNotificationService notificationService,
        ILogger<StoppageDetectedEventHandler> logger)
    {
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handle a stoppage detected event
    /// </summary>
    /// <param name="stoppageEvent">The stoppage detected event</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task HandleAsync(StoppageDetectedEvent stoppageEvent, CancellationToken cancellationToken = default)
    {
        if (stoppageEvent == null)
            throw new ArgumentNullException(nameof(stoppageEvent));

        _logger.LogInformation("Processing stoppage detected event: {Summary}", stoppageEvent.GetSummary());

        try
        {
            // Send real-time notifications to operators
            await NotifyOperatorsAsync(stoppageEvent, cancellationToken);

            // Log the event for audit trail
            await LogStoppageEventAsync(stoppageEvent, cancellationToken);

            // Trigger additional workflows if needed
            await TriggerWorkflowActionsAsync(stoppageEvent, cancellationToken);

            _logger.LogInformation("Successfully processed stoppage detected event {EventId} for stoppage {StoppageId}",
                stoppageEvent.EventId, stoppageEvent.StoppageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process stoppage detected event {EventId} for stoppage {StoppageId}",
                stoppageEvent.EventId, stoppageEvent.StoppageId);
            throw;
        }
    }

    /// <summary>
    /// Send notifications to operators and supervisors
    /// </summary>
    private async Task NotifyOperatorsAsync(StoppageDetectedEvent stoppageEvent, CancellationToken cancellationToken)
    {
        try
        {
            // Create notification data
            var notificationData = stoppageEvent.ToNotificationData();

            // Send to equipment line operators
            await _notificationService.NotifyLineOperatorsAsync(
                stoppageEvent.LineId,
                notificationData,
                cancellationToken);

            // Send to supervisors if this is a significant stoppage
            if (stoppageEvent.IsSignificantStoppage())
            {
                await _notificationService.NotifySupervisorsAsync(
                    stoppageEvent.LineId,
                    notificationData,
                    cancellationToken);
            }

            // Send critical alerts for long stoppages
            if (stoppageEvent.GetUrgencyLevel() == NotificationUrgency.Critical)
            {
                await _notificationService.NotifyMaintenanceTeamAsync(
                    stoppageEvent.LineId,
                    notificationData,
                    cancellationToken);
            }

            _logger.LogDebug("Sent notifications for stoppage {StoppageId} on line {LineId}",
                stoppageEvent.StoppageId, stoppageEvent.LineId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notifications for stoppage {StoppageId}",
                stoppageEvent.StoppageId);
            // Don't rethrow - continue with other processing
        }
    }

    /// <summary>
    /// Log the stoppage event for audit and analysis
    /// </summary>
    private async Task LogStoppageEventAsync(StoppageDetectedEvent stoppageEvent, CancellationToken cancellationToken)
    {
        try
        {
            // Structured logging for analytics
            _logger.LogInformation("Stoppage Detection Event: " +
                "StoppageId={StoppageId}, " +
                "LineId={LineId}, " +
                "WorkOrderId={WorkOrderId}, " +
                "StartTime={StartTime}, " +
                "DetectedAt={DetectedAt}, " +
                "DurationMinutes={DurationMinutes}, " +
                "ThresholdMinutes={ThresholdMinutes}, " +
                "RequiresClassification={RequiresClassification}, " +
                "UrgencyLevel={UrgencyLevel}",
                stoppageEvent.StoppageId,
                stoppageEvent.LineId,
                stoppageEvent.WorkOrderId ?? "None",
                stoppageEvent.StartTime,
                stoppageEvent.DetectedAt,
                stoppageEvent.DurationSinceLastProduction.TotalMinutes,
                stoppageEvent.DetectionThresholdMinutes,
                stoppageEvent.RequiresClassification,
                stoppageEvent.GetUrgencyLevel());

            // In future: Could send to external analytics system
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log stoppage event {EventId}", stoppageEvent.EventId);
            // Don't rethrow - logging failures shouldn't stop processing
        }
    }

    /// <summary>
    /// Trigger additional workflow actions based on stoppage characteristics
    /// </summary>
    private async Task TriggerWorkflowActionsAsync(StoppageDetectedEvent stoppageEvent, CancellationToken cancellationToken)
    {
        try
        {
            // Auto-classify very short stoppages as "changeover" if they occur during job transitions
            if (!stoppageEvent.RequiresClassification &&
                stoppageEvent.DurationSinceLastProduction.TotalMinutes < 2)
            {
                // In future: Could implement auto-classification logic
                _logger.LogDebug("Short stoppage {StoppageId} detected, potential changeover",
                    stoppageEvent.StoppageId);
            }

            // Escalate long stoppages automatically
            if (stoppageEvent.GetUrgencyLevel() >= NotificationUrgency.High)
            {
                // In future: Could trigger maintenance work orders or escalation workflows
                _logger.LogInformation("High-priority stoppage {StoppageId} requires attention",
                    stoppageEvent.StoppageId);
            }

            // Update OEE calculations in real-time
            // In future: Could trigger OEE recalculation service

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to trigger workflow actions for stoppage {StoppageId}",
                stoppageEvent.StoppageId);
            // Don't rethrow - workflow failures shouldn't stop core processing
        }
    }
}

/// <summary>
/// Generic event handler interface
/// </summary>
/// <typeparam name="TEvent">Type of event to handle</typeparam>
public interface IEventHandler<in TEvent> where TEvent : IDomainEvent
{
    /// <summary>
    /// Handle an event
    /// </summary>
    /// <param name="domainEvent">The event to handle</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service interface for stoppage notifications
/// </summary>
public interface IStoppageNotificationService
{
    /// <summary>
    /// Notify operators for a specific equipment line
    /// </summary>
    public Task NotifyLineOperatorsAsync(string lineId, StoppageNotificationData notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Notify supervisors about significant stoppages
    /// </summary>
    public Task NotifySupervisorsAsync(string lineId, StoppageNotificationData notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Notify maintenance team about critical stoppages
    /// </summary>
    public Task NotifyMaintenanceTeamAsync(string lineId, StoppageNotificationData notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send broadcast notification to all connected clients
    /// </summary>
    public Task NotifyAllClientsAsync(StoppageNotificationData notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send notification to specific user groups
    /// </summary>
    public Task NotifyUserGroupAsync(string groupName, StoppageNotificationData notification, CancellationToken cancellationToken = default);
}
