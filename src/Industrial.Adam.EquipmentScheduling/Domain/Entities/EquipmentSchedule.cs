using System.ComponentModel.DataAnnotations;
using Industrial.Adam.EquipmentScheduling.Domain.Enums;
using Industrial.Adam.EquipmentScheduling.Domain.Events;
using Industrial.Adam.EquipmentScheduling.Domain.Interfaces;
using Industrial.Adam.EquipmentScheduling.Domain.ValueObjects;

namespace Industrial.Adam.EquipmentScheduling.Domain.Entities;

/// <summary>
/// Represents a generated equipment schedule entry
/// </summary>
public sealed class EquipmentSchedule : Entity<long>, IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Gets the resource identifier this schedule applies to
    /// </summary>
    public long ResourceId { get; private set; }

    /// <summary>
    /// Gets the schedule date
    /// </summary>
    public DateTime ScheduleDate { get; private set; }

    /// <summary>
    /// Gets the shift code for this schedule entry
    /// </summary>
    [StringLength(10)]
    public string? ShiftCode { get; private set; }

    /// <summary>
    /// Gets the planned start time
    /// </summary>
    public DateTime? PlannedStartTime { get; private set; }

    /// <summary>
    /// Gets the planned end time
    /// </summary>
    public DateTime? PlannedEndTime { get; private set; }

    /// <summary>
    /// Gets the planned hours for this schedule entry
    /// </summary>
    [Range(0, 24)]
    public decimal PlannedHours { get; private set; }

    /// <summary>
    /// Gets the schedule status
    /// </summary>
    public ScheduleStatus Status { get; private set; } = ScheduleStatus.Planned;

    /// <summary>
    /// Gets the pattern identifier used to generate this schedule
    /// </summary>
    public int? PatternId { get; private set; }

    /// <summary>
    /// Gets whether this is an exception/override schedule
    /// </summary>
    public bool IsException { get; private set; }

    /// <summary>
    /// Gets when this schedule was generated
    /// </summary>
    public DateTime GeneratedAt { get; private set; }

    /// <summary>
    /// Gets optional notes about this schedule entry
    /// </summary>
    [StringLength(500)]
    public string? Notes { get; private set; }

    /// <summary>
    /// Navigation property to the resource
    /// </summary>
    public Resource Resource { get; private set; } = null!;

    /// <summary>
    /// Navigation property to the operating pattern (if applicable)
    /// </summary>
    public OperatingPattern? OperatingPattern { get; private set; }

    /// <summary>
    /// Gets the domain events for this aggregate root
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    // Required by EF Core
    private EquipmentSchedule() : base() { }

    /// <summary>
    /// Creates a new equipment schedule entry
    /// </summary>
    /// <param name="resourceId">The resource identifier</param>
    /// <param name="scheduleDate">The schedule date</param>
    /// <param name="plannedHours">The planned hours</param>
    /// <param name="patternId">Optional pattern identifier</param>
    /// <param name="shiftCode">Optional shift code</param>
    /// <param name="plannedStartTime">Optional planned start time</param>
    /// <param name="plannedEndTime">Optional planned end time</param>
    /// <param name="isException">Whether this is an exception schedule</param>
    /// <param name="notes">Optional notes</param>
    public EquipmentSchedule(
        long resourceId,
        DateTime scheduleDate,
        decimal plannedHours,
        int? patternId = null,
        string? shiftCode = null,
        DateTime? plannedStartTime = null,
        DateTime? plannedEndTime = null,
        bool isException = false,
        string? notes = null) : base()
    {
        ValidateScheduleCreation(resourceId, scheduleDate, plannedHours, plannedStartTime, plannedEndTime);

        ResourceId = resourceId;
        ScheduleDate = scheduleDate.Date; // Normalize to date only
        PlannedHours = plannedHours;
        PatternId = patternId;
        ShiftCode = shiftCode?.Trim().ToUpperInvariant();
        PlannedStartTime = plannedStartTime;
        PlannedEndTime = plannedEndTime;
        IsException = isException;
        GeneratedAt = DateTime.UtcNow;
        Notes = notes?.Trim();

        AddDomainEvent(new EquipmentScheduleCreatedEvent(Id, ResourceId, ScheduleDate, PlannedHours, IsException));
    }

    /// <summary>
    /// Updates the schedule times and hours
    /// </summary>
    /// <param name="plannedHours">The new planned hours</param>
    /// <param name="plannedStartTime">Optional new start time</param>
    /// <param name="plannedEndTime">Optional new end time</param>
    /// <param name="shiftCode">Optional new shift code</param>
    /// <param name="notes">Optional notes</param>
    public void UpdateSchedule(
        decimal plannedHours,
        DateTime? plannedStartTime = null,
        DateTime? plannedEndTime = null,
        string? shiftCode = null,
        string? notes = null)
    {
        ValidateScheduleUpdate(plannedHours, plannedStartTime, plannedEndTime);

        var oldPlannedHours = PlannedHours;
        var oldStartTime = PlannedStartTime;
        var oldEndTime = PlannedEndTime;

        PlannedHours = plannedHours;
        PlannedStartTime = plannedStartTime;
        PlannedEndTime = plannedEndTime;
        ShiftCode = shiftCode?.Trim().ToUpperInvariant();
        Notes = notes?.Trim();

        MarkAsUpdated();

        if (oldPlannedHours != PlannedHours || oldStartTime != PlannedStartTime || oldEndTime != PlannedEndTime)
        {
            AddDomainEvent(new EquipmentScheduleUpdatedEvent(Id, ResourceId, ScheduleDate, PlannedHours));
        }
    }

    /// <summary>
    /// Updates the schedule status
    /// </summary>
    /// <param name="status">The new status</param>
    /// <param name="notes">Optional notes about the status change</param>
    public void UpdateStatus(ScheduleStatus status, string? notes = null)
    {
        if (!Enum.IsDefined(typeof(ScheduleStatus), status))
            throw new ArgumentException("Invalid schedule status", nameof(status));

        var oldStatus = Status;
        Status = status;

        if (!string.IsNullOrWhiteSpace(notes))
        {
            Notes = notes.Trim();
        }

        MarkAsUpdated();

        if (oldStatus != Status)
        {
            AddDomainEvent(new EquipmentScheduleStatusChangedEvent(Id, ResourceId, ScheduleDate, oldStatus, Status));
        }
    }

    /// <summary>
    /// Marks the schedule as an exception
    /// </summary>
    /// <param name="notes">Optional notes about why this is an exception</param>
    public void MarkAsException(string? notes = null)
    {
        if (IsException)
            return;

        IsException = true;
        Notes = notes?.Trim();

        MarkAsUpdated();
        AddDomainEvent(new EquipmentScheduleExceptionMarkedEvent(Id, ResourceId, ScheduleDate));
    }

    /// <summary>
    /// Removes the exception flag
    /// </summary>
    public void ClearException()
    {
        if (!IsException)
            return;

        IsException = false;
        MarkAsUpdated();
        AddDomainEvent(new EquipmentScheduleExceptionClearedEvent(Id, ResourceId, ScheduleDate));
    }

    /// <summary>
    /// Cancels the schedule
    /// </summary>
    /// <param name="reason">The reason for cancellation</param>
    public void Cancel(string? reason = null)
    {
        if (Status == ScheduleStatus.Cancelled)
            return;

        UpdateStatus(ScheduleStatus.Cancelled, reason);
        AddDomainEvent(new EquipmentScheduleCancelledEvent(Id, ResourceId, ScheduleDate, reason));
    }

    /// <summary>
    /// Completes the schedule
    /// </summary>
    public void Complete()
    {
        if (Status == ScheduleStatus.Completed)
            return;

        UpdateStatus(ScheduleStatus.Completed);
        AddDomainEvent(new EquipmentScheduleCompletedEvent(Id, ResourceId, ScheduleDate, PlannedHours));
    }

    /// <summary>
    /// Gets the actual duration of the scheduled period
    /// </summary>
    /// <returns>The duration, or null if start/end times are not specified</returns>
    public TimeSpan? GetScheduledDuration()
    {
        if (!PlannedStartTime.HasValue || !PlannedEndTime.HasValue)
            return null;

        return PlannedEndTime.Value - PlannedStartTime.Value;
    }

    /// <summary>
    /// Checks if this schedule is active at the specified time
    /// </summary>
    /// <param name="dateTime">The date and time to check</param>
    /// <returns>True if the schedule is active at the specified time</returns>
    public bool IsActiveAt(DateTime dateTime)
    {
        if (dateTime.Date != ScheduleDate.Date)
            return false;

        if (Status != ScheduleStatus.Active && Status != ScheduleStatus.Planned)
            return false;

        if (!PlannedStartTime.HasValue || !PlannedEndTime.HasValue)
            return true; // All day schedule

        return dateTime >= PlannedStartTime.Value && dateTime <= PlannedEndTime.Value;
    }

    /// <summary>
    /// Validates that the schedule doesn't conflict with another schedule
    /// </summary>
    /// <param name="other">The other schedule to check</param>
    /// <returns>True if there's a conflict</returns>
    public bool ConflictsWith(EquipmentSchedule other)
    {
        if (ResourceId != other.ResourceId || ScheduleDate.Date != other.ScheduleDate.Date)
            return false;

        if (Status == ScheduleStatus.Cancelled || other.Status == ScheduleStatus.Cancelled)
            return false;

        // If either schedule doesn't have specific times, they potentially conflict
        if (!PlannedStartTime.HasValue || !PlannedEndTime.HasValue ||
            !other.PlannedStartTime.HasValue || !other.PlannedEndTime.HasValue)
            return true;

        // Check for time overlap
        return PlannedStartTime.Value < other.PlannedEndTime.Value &&
               PlannedEndTime.Value > other.PlannedStartTime.Value;
    }

    /// <summary>
    /// Adds a domain event to be published
    /// </summary>
    /// <param name="domainEvent">The domain event to add</param>
    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clears all domain events
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    private static void ValidateScheduleCreation(long resourceId, DateTime scheduleDate, decimal plannedHours, DateTime? plannedStartTime, DateTime? plannedEndTime)
    {
        ValidateScheduleUpdate(plannedHours, plannedStartTime, plannedEndTime);

        if (resourceId <= 0)
            throw new ArgumentException("Resource ID must be positive", nameof(resourceId));

        if (scheduleDate == default)
            throw new ArgumentException("Schedule date must be specified", nameof(scheduleDate));
    }

    private static void ValidateScheduleUpdate(decimal plannedHours, DateTime? plannedStartTime, DateTime? plannedEndTime)
    {
        if (plannedHours < 0 || plannedHours > 24)
            throw new ArgumentException("Planned hours must be between 0 and 24", nameof(plannedHours));

        if (plannedStartTime.HasValue && plannedEndTime.HasValue && plannedEndTime.Value <= plannedStartTime.Value)
            throw new ArgumentException("Planned end time must be after start time", nameof(plannedEndTime));
    }
}
