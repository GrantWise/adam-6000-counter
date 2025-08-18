using Industrial.Adam.Oee.Domain.Entities;

namespace Industrial.Adam.Oee.Domain.Events;

/// <summary>
/// Domain event raised when a production stoppage is automatically detected
/// </summary>
/// <param name="StoppageId">The ID of the detected stoppage</param>
/// <param name="LineId">Equipment line where stoppage was detected</param>
/// <param name="WorkOrderId">Associated work order (if any)</param>
/// <param name="StartTime">When the stoppage started</param>
/// <param name="DetectedAt">When the stoppage was detected</param>
/// <param name="DetectionThresholdMinutes">Threshold used for detection</param>
/// <param name="LastProductionTime">Last time production activity was detected</param>
/// <param name="RequiresClassification">Whether stoppage requires operator classification</param>
/// <param name="Stoppage">The full stoppage entity</param>
public record StoppageDetectedEvent(
    int StoppageId,
    string LineId,
    string? WorkOrderId,
    DateTime StartTime,
    DateTime DetectedAt,
    int DetectionThresholdMinutes,
    DateTime LastProductionTime,
    bool RequiresClassification,
    EquipmentStoppage Stoppage
) : IDomainEvent
{
    /// <summary>
    /// When this event occurred
    /// </summary>
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Unique identifier for this event
    /// </summary>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Duration since last production activity
    /// </summary>
    public TimeSpan DurationSinceLastProduction => DetectedAt - LastProductionTime;

    /// <summary>
    /// Get event summary for logging
    /// </summary>
    /// <returns>Event summary</returns>
    public string GetSummary()
    {
        var classification = RequiresClassification ? "Requires Classification" : "Below Threshold";
        var workOrder = string.IsNullOrEmpty(WorkOrderId) ? "No Work Order" : $"Work Order: {WorkOrderId}";

        return $"Stoppage detected on Line {LineId} - Duration: {DurationSinceLastProduction.TotalMinutes:F1}min, " +
               $"Status: {classification}, {workOrder}";
    }

    /// <summary>
    /// Check if this is a significant stoppage requiring immediate attention
    /// </summary>
    /// <returns>True if requires immediate attention</returns>
    public bool IsSignificantStoppage()
    {
        return RequiresClassification && DurationSinceLastProduction.TotalMinutes >= DetectionThresholdMinutes;
    }

    /// <summary>
    /// Get notification urgency level
    /// </summary>
    /// <returns>Urgency level for notifications</returns>
    public NotificationUrgency GetUrgencyLevel()
    {
        var durationMinutes = DurationSinceLastProduction.TotalMinutes;

        if (durationMinutes >= 30)
            return NotificationUrgency.Critical;

        if (durationMinutes >= 15)
            return NotificationUrgency.High;

        if (RequiresClassification)
            return NotificationUrgency.Medium;

        return NotificationUrgency.Low;
    }

    /// <summary>
    /// Create notification data for SignalR
    /// </summary>
    /// <returns>Notification data</returns>
    public StoppageNotificationData ToNotificationData()
    {
        return new StoppageNotificationData(
            StoppageId,
            LineId,
            WorkOrderId,
            StartTime,
            DetectedAt,
            DurationSinceLastProduction.TotalMinutes,
            RequiresClassification,
            GetUrgencyLevel(),
            GetSummary()
        );
    }
}

/// <summary>
/// Domain event interface marker
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// When this event occurred
    /// </summary>
    public DateTime OccurredAt { get; }

    /// <summary>
    /// Unique identifier for this event
    /// </summary>
    public Guid EventId { get; }
}

/// <summary>
/// Notification urgency levels
/// </summary>
public enum NotificationUrgency
{
    /// <summary>
    /// Low priority notification
    /// </summary>
    Low = 1,

    /// <summary>
    /// Medium priority notification
    /// </summary>
    Medium = 2,

    /// <summary>
    /// High priority notification
    /// </summary>
    High = 3,

    /// <summary>
    /// Critical priority notification
    /// </summary>
    Critical = 4
}

/// <summary>
/// Stoppage notification data for SignalR
/// </summary>
/// <param name="StoppageId">Stoppage identifier</param>
/// <param name="LineId">Equipment line identifier</param>
/// <param name="WorkOrderId">Work order identifier</param>
/// <param name="StartTime">Stoppage start time</param>
/// <param name="DetectedAt">Detection timestamp</param>
/// <param name="DurationMinutes">Duration in minutes</param>
/// <param name="RequiresClassification">Whether requires classification</param>
/// <param name="Urgency">Notification urgency level</param>
/// <param name="Summary">Human-readable summary</param>
public record StoppageNotificationData(
    int StoppageId,
    string LineId,
    string? WorkOrderId,
    DateTime StartTime,
    DateTime DetectedAt,
    double DurationMinutes,
    bool RequiresClassification,
    NotificationUrgency Urgency,
    string Summary
);
