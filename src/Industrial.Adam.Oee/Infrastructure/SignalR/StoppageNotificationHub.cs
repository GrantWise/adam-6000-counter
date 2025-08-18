using Industrial.Adam.Oee.Domain.Events;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Oee.Infrastructure.SignalR;

/// <summary>
/// SignalR hub for real-time stoppage notifications to operators and supervisors
/// Provides typed connections for equipment line monitoring and alerts
/// </summary>
public sealed class StoppageNotificationHub : Hub<IStoppageNotificationClient>
{
    private readonly ILogger<StoppageNotificationHub> _logger;
    private const string LineGroupPrefix = "Line_";
    private const string OperatorGroupPrefix = "Operators_";
    private const string SupervisorGroup = "Supervisors";
    private const string MaintenanceGroup = "Maintenance";

    /// <summary>
    /// Constructor for stoppage notification hub
    /// </summary>
    public StoppageNotificationHub(ILogger<StoppageNotificationHub> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Called when a client connects to the hub
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client {ConnectionId} connected to stoppage notification hub", Context.ConnectionId);

        // Send current connection info to client
        await Clients.Caller.ConnectionEstablished(new ConnectionInfo(
            Context.ConnectionId,
            DateTime.UtcNow,
            "Connected to stoppage notifications"
        ));

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            _logger.LogWarning(exception, "Client {ConnectionId} disconnected with error", Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation("Client {ConnectionId} disconnected", Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribe to notifications for a specific equipment line
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    public async Task SubscribeToLine(string lineId)
    {
        if (string.IsNullOrWhiteSpace(lineId))
        {
            _logger.LogWarning("Client {ConnectionId} attempted to subscribe with empty line ID", Context.ConnectionId);
            await Clients.Caller.SubscriptionError("Line ID cannot be empty");
            return;
        }

        try
        {
            var groupName = GetLineGroupName(lineId);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            _logger.LogInformation("Client {ConnectionId} subscribed to line {LineId}", Context.ConnectionId, lineId);

            await Clients.Caller.SubscriptionConfirmed(new SubscriptionInfo(
                lineId,
                groupName,
                "Line subscription confirmed"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe client {ConnectionId} to line {LineId}", Context.ConnectionId, lineId);
            await Clients.Caller.SubscriptionError($"Failed to subscribe to line {lineId}");
        }
    }

    /// <summary>
    /// Unsubscribe from notifications for a specific equipment line
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    public async Task UnsubscribeFromLine(string lineId)
    {
        if (string.IsNullOrWhiteSpace(lineId))
        {
            _logger.LogWarning("Client {ConnectionId} attempted to unsubscribe with empty line ID", Context.ConnectionId);
            return;
        }

        try
        {
            var groupName = GetLineGroupName(lineId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

            _logger.LogInformation("Client {ConnectionId} unsubscribed from line {LineId}", Context.ConnectionId, lineId);

            await Clients.Caller.SubscriptionRemoved(new SubscriptionInfo(
                lineId,
                groupName,
                "Line subscription removed"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unsubscribe client {ConnectionId} from line {LineId}", Context.ConnectionId, lineId);
        }
    }

    /// <summary>
    /// Subscribe to operator notifications for multiple lines
    /// </summary>
    /// <param name="lineIds">Equipment line identifiers</param>
    /// <param name="operatorId">Operator identifier</param>
    public async Task SubscribeAsOperator(string[] lineIds, string operatorId)
    {
        if (lineIds == null || lineIds.Length == 0)
        {
            await Clients.Caller.SubscriptionError("Must specify at least one line ID");
            return;
        }

        if (string.IsNullOrWhiteSpace(operatorId))
        {
            await Clients.Caller.SubscriptionError("Operator ID is required");
            return;
        }

        try
        {
            // Add to operator group
            var operatorGroupName = GetOperatorGroupName(operatorId);
            await Groups.AddToGroupAsync(Context.ConnectionId, operatorGroupName);

            // Add to each line group
            foreach (var lineId in lineIds.Where(id => !string.IsNullOrWhiteSpace(id)))
            {
                var lineGroupName = GetLineGroupName(lineId);
                await Groups.AddToGroupAsync(Context.ConnectionId, lineGroupName);
            }

            _logger.LogInformation("Operator {OperatorId} (connection {ConnectionId}) subscribed to {LineCount} lines",
                operatorId, Context.ConnectionId, lineIds.Length);

            await Clients.Caller.OperatorSubscriptionConfirmed(new OperatorSubscriptionInfo(
                operatorId,
                lineIds,
                "Operator subscription confirmed"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe operator {OperatorId} (connection {ConnectionId})",
                operatorId, Context.ConnectionId);
            await Clients.Caller.SubscriptionError($"Failed to subscribe as operator");
        }
    }

    /// <summary>
    /// Subscribe to supervisor notifications (all lines)
    /// </summary>
    /// <param name="supervisorId">Supervisor identifier</param>
    public async Task SubscribeAsSupervisor(string supervisorId)
    {
        if (string.IsNullOrWhiteSpace(supervisorId))
        {
            await Clients.Caller.SubscriptionError("Supervisor ID is required");
            return;
        }

        try
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, SupervisorGroup);

            _logger.LogInformation("Supervisor {SupervisorId} (connection {ConnectionId}) subscribed to all notifications",
                supervisorId, Context.ConnectionId);

            await Clients.Caller.SupervisorSubscriptionConfirmed(new SupervisorSubscriptionInfo(
                supervisorId,
                SupervisorGroup,
                "Supervisor subscription confirmed"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe supervisor {SupervisorId} (connection {ConnectionId})",
                supervisorId, Context.ConnectionId);
            await Clients.Caller.SubscriptionError("Failed to subscribe as supervisor");
        }
    }

    /// <summary>
    /// Subscribe to maintenance notifications
    /// </summary>
    /// <param name="maintenanceId">Maintenance team member identifier</param>
    public async Task SubscribeAsMaintenance(string maintenanceId)
    {
        if (string.IsNullOrWhiteSpace(maintenanceId))
        {
            await Clients.Caller.SubscriptionError("Maintenance ID is required");
            return;
        }

        try
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, MaintenanceGroup);

            _logger.LogInformation("Maintenance {MaintenanceId} (connection {ConnectionId}) subscribed to critical notifications",
                maintenanceId, Context.ConnectionId);

            await Clients.Caller.MaintenanceSubscriptionConfirmed(new MaintenanceSubscriptionInfo(
                maintenanceId,
                MaintenanceGroup,
                "Maintenance subscription confirmed"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe maintenance {MaintenanceId} (connection {ConnectionId})",
                maintenanceId, Context.ConnectionId);
            await Clients.Caller.SubscriptionError("Failed to subscribe as maintenance");
        }
    }

    /// <summary>
    /// Get heartbeat status for connection health monitoring
    /// </summary>
    public async Task Heartbeat()
    {
        await Clients.Caller.HeartbeatResponse(new HeartbeatInfo(
            Context.ConnectionId,
            DateTime.UtcNow,
            "Healthy"
        ));
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

/// <summary>
/// Typed client interface for stoppage notifications
/// Defines all methods that can be called on connected clients
/// </summary>
public interface IStoppageNotificationClient
{
    /// <summary>
    /// Notify client of a detected stoppage
    /// </summary>
    public Task StoppageDetected(StoppageNotificationData notification);

    /// <summary>
    /// Notify client of stoppage classification
    /// </summary>
    public Task StoppageClassified(StoppageClassificationNotification notification);

    /// <summary>
    /// Notify client that stoppage has ended
    /// </summary>
    public Task StoppageEnded(StoppageEndedNotification notification);

    /// <summary>
    /// Notify client of high-priority stoppage alert
    /// </summary>
    public Task HighPriorityAlert(HighPriorityAlert alert);

    /// <summary>
    /// Notify client of critical stoppage requiring immediate attention
    /// </summary>
    public Task CriticalAlert(CriticalAlert alert);

    /// <summary>
    /// Confirm connection establishment
    /// </summary>
    public Task ConnectionEstablished(ConnectionInfo info);

    /// <summary>
    /// Confirm subscription to line notifications
    /// </summary>
    public Task SubscriptionConfirmed(SubscriptionInfo info);

    /// <summary>
    /// Confirm subscription removal
    /// </summary>
    public Task SubscriptionRemoved(SubscriptionInfo info);

    /// <summary>
    /// Confirm operator subscription
    /// </summary>
    public Task OperatorSubscriptionConfirmed(OperatorSubscriptionInfo info);

    /// <summary>
    /// Confirm supervisor subscription
    /// </summary>
    public Task SupervisorSubscriptionConfirmed(SupervisorSubscriptionInfo info);

    /// <summary>
    /// Confirm maintenance subscription
    /// </summary>
    public Task MaintenanceSubscriptionConfirmed(MaintenanceSubscriptionInfo info);

    /// <summary>
    /// Notify client of subscription error
    /// </summary>
    public Task SubscriptionError(string error);

    /// <summary>
    /// Respond to heartbeat request
    /// </summary>
    public Task HeartbeatResponse(HeartbeatInfo info);

    /// <summary>
    /// General status update
    /// </summary>
    public Task StatusUpdate(StatusUpdate update);
}

// Supporting data structures for SignalR notifications

/// <summary>
/// Connection information
/// </summary>
/// <param name="ConnectionId">SignalR connection identifier</param>
/// <param name="ConnectedAt">Connection timestamp</param>
/// <param name="Message">Connection message</param>
public record ConnectionInfo(string ConnectionId, DateTime ConnectedAt, string Message);

/// <summary>
/// Subscription information
/// </summary>
/// <param name="LineId">Equipment line identifier</param>
/// <param name="GroupName">SignalR group name</param>
/// <param name="Message">Subscription message</param>
public record SubscriptionInfo(string LineId, string GroupName, string Message);

/// <summary>
/// Operator subscription information
/// </summary>
/// <param name="OperatorId">Operator identifier</param>
/// <param name="LineIds">Subscribed line identifiers</param>
/// <param name="Message">Subscription message</param>
public record OperatorSubscriptionInfo(string OperatorId, string[] LineIds, string Message);

/// <summary>
/// Supervisor subscription information
/// </summary>
/// <param name="SupervisorId">Supervisor identifier</param>
/// <param name="GroupName">SignalR group name</param>
/// <param name="Message">Subscription message</param>
public record SupervisorSubscriptionInfo(string SupervisorId, string GroupName, string Message);

/// <summary>
/// Maintenance subscription information
/// </summary>
/// <param name="MaintenanceId">Maintenance team member identifier</param>
/// <param name="GroupName">SignalR group name</param>
/// <param name="Message">Subscription message</param>
public record MaintenanceSubscriptionInfo(string MaintenanceId, string GroupName, string Message);

/// <summary>
/// Heartbeat information
/// </summary>
/// <param name="ConnectionId">Connection identifier</param>
/// <param name="Timestamp">Heartbeat timestamp</param>
/// <param name="Status">Connection status</param>
public record HeartbeatInfo(string ConnectionId, DateTime Timestamp, string Status);

/// <summary>
/// Stoppage classification notification
/// </summary>
/// <param name="StoppageId">Stoppage identifier</param>
/// <param name="LineId">Equipment line identifier</param>
/// <param name="CategoryCode">Classification category</param>
/// <param name="Subcode">Classification subcode</param>
/// <param name="ClassifiedBy">Who classified the stoppage</param>
/// <param name="ClassifiedAt">When classified</param>
/// <param name="Comments">Operator comments</param>
public record StoppageClassificationNotification(
    int StoppageId,
    string LineId,
    string CategoryCode,
    string Subcode,
    string ClassifiedBy,
    DateTime ClassifiedAt,
    string? Comments
);

/// <summary>
/// Stoppage ended notification
/// </summary>
/// <param name="StoppageId">Stoppage identifier</param>
/// <param name="LineId">Equipment line identifier</param>
/// <param name="EndTime">When stoppage ended</param>
/// <param name="DurationMinutes">Total duration in minutes</param>
/// <param name="WasClassified">Whether stoppage was classified</param>
public record StoppageEndedNotification(
    int StoppageId,
    string LineId,
    DateTime EndTime,
    decimal DurationMinutes,
    bool WasClassified
);

/// <summary>
/// High-priority alert
/// </summary>
/// <param name="StoppageId">Stoppage identifier</param>
/// <param name="LineId">Equipment line identifier</param>
/// <param name="AlertType">Type of alert</param>
/// <param name="Message">Alert message</param>
/// <param name="Urgency">Alert urgency level</param>
/// <param name="AlertTime">When alert was triggered</param>
public record HighPriorityAlert(
    int StoppageId,
    string LineId,
    string AlertType,
    string Message,
    NotificationUrgency Urgency,
    DateTime AlertTime
);

/// <summary>
/// Critical alert requiring immediate attention
/// </summary>
/// <param name="StoppageId">Stoppage identifier</param>
/// <param name="LineId">Equipment line identifier</param>
/// <param name="AlertType">Type of alert</param>
/// <param name="Message">Alert message</param>
/// <param name="DurationMinutes">Stoppage duration</param>
/// <param name="RequiresEscalation">Whether escalation is required</param>
/// <param name="AlertTime">When alert was triggered</param>
public record CriticalAlert(
    int StoppageId,
    string LineId,
    string AlertType,
    string Message,
    double DurationMinutes,
    bool RequiresEscalation,
    DateTime AlertTime
);

/// <summary>
/// General status update
/// </summary>
/// <param name="Type">Type of status update</param>
/// <param name="Message">Status message</param>
/// <param name="Data">Additional data</param>
/// <param name="Timestamp">Update timestamp</param>
public record StatusUpdate(
    string Type,
    string Message,
    object? Data,
    DateTime Timestamp
);
