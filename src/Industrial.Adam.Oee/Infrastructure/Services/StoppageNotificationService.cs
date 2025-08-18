using Industrial.Adam.Oee.Application.Events;
using Industrial.Adam.Oee.Domain.Events;
using Industrial.Adam.Oee.Infrastructure.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Oee.Infrastructure.Services;

/// <summary>
/// Service for sending real-time stoppage notifications via SignalR
/// Implements notification strategies based on urgency and user roles
/// </summary>
public sealed class StoppageNotificationService : IStoppageNotificationService
{
    private readonly IHubContext<StoppageNotificationHub, IStoppageNotificationClient> _hubContext;
    private readonly ILogger<StoppageNotificationService> _logger;

    private const string LineGroupPrefix = "Line_";
    private const string OperatorGroupPrefix = "Operators_";
    private const string SupervisorGroup = "Supervisors";
    private const string MaintenanceGroup = "Maintenance";

    /// <summary>
    /// Constructor for stoppage notification service
    /// </summary>
    public StoppageNotificationService(
        IHubContext<StoppageNotificationHub, IStoppageNotificationClient> hubContext,
        ILogger<StoppageNotificationService> logger)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Notify operators for a specific equipment line
    /// </summary>
    public async Task NotifyLineOperatorsAsync(
        string lineId,
        StoppageNotificationData notification,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lineId))
            throw new ArgumentException("Line ID cannot be null or empty", nameof(lineId));

        if (notification == null)
            throw new ArgumentNullException(nameof(notification));

        try
        {
            var groupName = GetLineGroupName(lineId);

            _logger.LogDebug("Sending stoppage notification to line {LineId} operators", lineId);

            // Send basic stoppage notification
            await _hubContext.Clients.Group(groupName).StoppageDetected(notification);

            // Send high-priority alert if urgent
            if (notification.Urgency >= NotificationUrgency.High)
            {
                var alert = new HighPriorityAlert(
                    notification.StoppageId,
                    notification.LineId,
                    "Stoppage Detection",
                    notification.Summary,
                    notification.Urgency,
                    notification.DetectedAt
                );

                await _hubContext.Clients.Group(groupName).HighPriorityAlert(alert);
            }

            _logger.LogInformation("Sent stoppage notification to line {LineId} operators: {Summary}",
                lineId, notification.Summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify line {LineId} operators", lineId);
            throw;
        }
    }

    /// <summary>
    /// Notify supervisors about significant stoppages
    /// </summary>
    public async Task NotifySupervisorsAsync(
        string lineId,
        StoppageNotificationData notification,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lineId))
            throw new ArgumentException("Line ID cannot be null or empty", nameof(lineId));

        if (notification == null)
            throw new ArgumentNullException(nameof(notification));

        try
        {
            _logger.LogDebug("Sending stoppage notification to supervisors for line {LineId}", lineId);

            // Send notification to supervisor group
            await _hubContext.Clients.Group(SupervisorGroup).StoppageDetected(notification);

            // Send high-priority alert for significant stoppages
            if (notification.Urgency >= NotificationUrgency.Medium)
            {
                var alert = new HighPriorityAlert(
                    notification.StoppageId,
                    notification.LineId,
                    "Supervisor Alert",
                    $"Line {lineId}: {notification.Summary}",
                    notification.Urgency,
                    notification.DetectedAt
                );

                await _hubContext.Clients.Group(SupervisorGroup).HighPriorityAlert(alert);
            }

            _logger.LogInformation("Sent stoppage notification to supervisors for line {LineId}: {Summary}",
                lineId, notification.Summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify supervisors for line {LineId}", lineId);
            throw;
        }
    }

    /// <summary>
    /// Notify maintenance team about critical stoppages
    /// </summary>
    public async Task NotifyMaintenanceTeamAsync(
        string lineId,
        StoppageNotificationData notification,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lineId))
            throw new ArgumentException("Line ID cannot be null or empty", nameof(lineId));

        if (notification == null)
            throw new ArgumentNullException(nameof(notification));

        try
        {
            _logger.LogDebug("Sending critical stoppage notification to maintenance team for line {LineId}", lineId);

            // Send notification to maintenance group
            await _hubContext.Clients.Group(MaintenanceGroup).StoppageDetected(notification);

            // Send critical alert for long stoppages
            var alert = new CriticalAlert(
                notification.StoppageId,
                notification.LineId,
                "Critical Stoppage",
                $"CRITICAL: Line {lineId} stopped for {notification.DurationMinutes:F1} minutes. {notification.Summary}",
                notification.DurationMinutes,
                notification.Urgency == NotificationUrgency.Critical,
                notification.DetectedAt
            );

            await _hubContext.Clients.Group(MaintenanceGroup).CriticalAlert(alert);

            _logger.LogWarning("Sent critical stoppage alert to maintenance team for line {LineId}: {Summary}",
                lineId, notification.Summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify maintenance team for line {LineId}", lineId);
            throw;
        }
    }

    /// <summary>
    /// Send broadcast notification to all connected clients
    /// </summary>
    public async Task NotifyAllClientsAsync(
        StoppageNotificationData notification,
        CancellationToken cancellationToken = default)
    {
        if (notification == null)
            throw new ArgumentNullException(nameof(notification));

        try
        {
            _logger.LogDebug("Broadcasting stoppage notification to all clients");

            await _hubContext.Clients.All.StoppageDetected(notification);

            _logger.LogInformation("Broadcasted stoppage notification: {Summary}", notification.Summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast stoppage notification");
            throw;
        }
    }

    /// <summary>
    /// Send notification to specific user groups
    /// </summary>
    public async Task NotifyUserGroupAsync(
        string groupName,
        StoppageNotificationData notification,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(groupName))
            throw new ArgumentException("Group name cannot be null or empty", nameof(groupName));

        if (notification == null)
            throw new ArgumentNullException(nameof(notification));

        try
        {
            _logger.LogDebug("Sending stoppage notification to group {GroupName}", groupName);

            await _hubContext.Clients.Group(groupName).StoppageDetected(notification);

            _logger.LogInformation("Sent stoppage notification to group {GroupName}: {Summary}",
                groupName, notification.Summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify group {GroupName}", groupName);
            throw;
        }
    }

    /// <summary>
    /// Send stoppage classification notification
    /// </summary>
    public async Task NotifyStoppageClassifiedAsync(
        string lineId,
        StoppageClassificationNotification notification,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lineId))
            throw new ArgumentException("Line ID cannot be null or empty", nameof(lineId));

        if (notification == null)
            throw new ArgumentNullException(nameof(notification));

        try
        {
            var groupName = GetLineGroupName(lineId);

            _logger.LogDebug("Sending stoppage classification notification for line {LineId}", lineId);

            // Notify line operators
            await _hubContext.Clients.Group(groupName).StoppageClassified(notification);

            // Notify supervisors
            await _hubContext.Clients.Group(SupervisorGroup).StoppageClassified(notification);

            _logger.LogInformation("Sent stoppage classification notification for line {LineId}: Stoppage {StoppageId} classified as {CategoryCode}-{Subcode}",
                lineId, notification.StoppageId, notification.CategoryCode, notification.Subcode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify stoppage classification for line {LineId}", lineId);
            throw;
        }
    }

    /// <summary>
    /// Send stoppage ended notification
    /// </summary>
    public async Task NotifyStoppageEndedAsync(
        string lineId,
        StoppageEndedNotification notification,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lineId))
            throw new ArgumentException("Line ID cannot be null or empty", nameof(lineId));

        if (notification == null)
            throw new ArgumentNullException(nameof(notification));

        try
        {
            var groupName = GetLineGroupName(lineId);

            _logger.LogDebug("Sending stoppage ended notification for line {LineId}", lineId);

            // Notify line operators
            await _hubContext.Clients.Group(groupName).StoppageEnded(notification);

            // Notify supervisors for significant stoppages
            if (notification.DurationMinutes >= 15)
            {
                await _hubContext.Clients.Group(SupervisorGroup).StoppageEnded(notification);
            }

            _logger.LogInformation("Sent stoppage ended notification for line {LineId}: Stoppage {StoppageId} ended after {DurationMinutes:F1} minutes",
                lineId, notification.StoppageId, notification.DurationMinutes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify stoppage ended for line {LineId}", lineId);
            throw;
        }
    }

    /// <summary>
    /// Send general status update
    /// </summary>
    public async Task SendStatusUpdateAsync(
        string lineId,
        string updateType,
        string message,
        object? data = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lineId))
            throw new ArgumentException("Line ID cannot be null or empty", nameof(lineId));

        if (string.IsNullOrWhiteSpace(updateType))
            throw new ArgumentException("Update type cannot be null or empty", nameof(updateType));

        try
        {
            var groupName = GetLineGroupName(lineId);
            var update = new StatusUpdate(updateType, message, data, DateTime.UtcNow);

            await _hubContext.Clients.Group(groupName).StatusUpdate(update);

            _logger.LogDebug("Sent status update to line {LineId}: {UpdateType} - {Message}",
                lineId, updateType, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send status update for line {LineId}", lineId);
            throw;
        }
    }

    /// <summary>
    /// Get line group name for SignalR groups
    /// </summary>
    private static string GetLineGroupName(string lineId) => $"{LineGroupPrefix}{lineId}";

    /// <summary>
    /// Get operator group name for SignalR groups
    /// </summary>
    private static string GetOperatorGroupName(string operatorId) => $"{OperatorGroupPrefix}{operatorId}";
}
