namespace Industrial.Adam.Oee.Domain.ValueObjects;

/// <summary>
/// Value object representing an active equipment schedule
/// </summary>
public sealed class ActiveSchedule : ValueObject
{
    /// <summary>
    /// Unique identifier for the schedule
    /// </summary>
    public long ScheduleId { get; }

    /// <summary>
    /// Equipment line identifier
    /// </summary>
    public string LineId { get; }

    /// <summary>
    /// Shift code or identifier
    /// </summary>
    public string? ShiftCode { get; }

    /// <summary>
    /// Planned start time for this schedule
    /// </summary>
    public DateTime PlannedStartTime { get; }

    /// <summary>
    /// Planned end time for this schedule
    /// </summary>
    public DateTime PlannedEndTime { get; }

    /// <summary>
    /// Duration of the schedule in hours
    /// </summary>
    public decimal PlannedHours { get; }

    /// <summary>
    /// Current status of the schedule
    /// </summary>
    public ScheduleStatus Status { get; }

    /// <summary>
    /// Indicates if this is an exception schedule
    /// </summary>
    public bool IsException { get; }

    /// <summary>
    /// Time when this schedule was generated
    /// </summary>
    public DateTime GeneratedAt { get; }

    /// <summary>
    /// Optional notes about the schedule
    /// </summary>
    public string? Notes { get; }

    /// <summary>
    /// Creates a new ActiveSchedule instance
    /// </summary>
    /// <param name="scheduleId">Schedule identifier</param>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="plannedStartTime">Planned start time</param>
    /// <param name="plannedEndTime">Planned end time</param>
    /// <param name="status">Schedule status</param>
    /// <param name="shiftCode">Shift code</param>
    /// <param name="isException">Whether this is an exception schedule</param>
    /// <param name="generatedAt">Time when schedule was generated</param>
    /// <param name="notes">Optional notes</param>
    public ActiveSchedule(long scheduleId, string lineId, DateTime plannedStartTime, DateTime plannedEndTime,
        ScheduleStatus status, string? shiftCode = null, bool isException = false, DateTime? generatedAt = null, string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(lineId))
            throw new ArgumentException("Line ID cannot be null or empty", nameof(lineId));

        if (plannedStartTime >= plannedEndTime)
            throw new ArgumentException("Planned start time must be before planned end time");

        ScheduleId = scheduleId;
        LineId = lineId;
        ShiftCode = shiftCode;
        PlannedStartTime = plannedStartTime;
        PlannedEndTime = plannedEndTime;
        PlannedHours = (decimal)(plannedEndTime - plannedStartTime).TotalHours;
        Status = status;
        IsException = isException;
        GeneratedAt = generatedAt ?? DateTime.UtcNow;
        Notes = notes;
    }

    /// <summary>
    /// Determines if this schedule is currently active
    /// </summary>
    /// <param name="timestamp">Time to check against (defaults to current time)</param>
    /// <returns>True if the schedule is active at the specified time</returns>
    public bool IsActiveAt(DateTime? timestamp = null)
    {
        var checkTime = timestamp ?? DateTime.UtcNow;
        return checkTime >= PlannedStartTime && checkTime <= PlannedEndTime && Status == ScheduleStatus.Active;
    }

    /// <summary>
    /// Gets remaining time in the schedule
    /// </summary>
    /// <param name="timestamp">Current time (defaults to current time)</param>
    /// <returns>Remaining time, or TimeSpan.Zero if schedule is complete</returns>
    public TimeSpan GetRemainingTime(DateTime? timestamp = null)
    {
        var checkTime = timestamp ?? DateTime.UtcNow;

        if (checkTime >= PlannedEndTime)
            return TimeSpan.Zero;
        if (checkTime <= PlannedStartTime)
            return PlannedEndTime - PlannedStartTime;

        return PlannedEndTime - checkTime;
    }

    /// <summary>
    /// Gets elapsed time in the schedule
    /// </summary>
    /// <param name="timestamp">Current time (defaults to current time)</param>
    /// <returns>Elapsed time, or TimeSpan.Zero if schedule hasn't started</returns>
    public TimeSpan GetElapsedTime(DateTime? timestamp = null)
    {
        var checkTime = timestamp ?? DateTime.UtcNow;

        if (checkTime <= PlannedStartTime)
            return TimeSpan.Zero;
        if (checkTime >= PlannedEndTime)
            return PlannedEndTime - PlannedStartTime;

        return checkTime - PlannedStartTime;
    }

    /// <summary>
    /// Gets progress percentage of the schedule (0.0 to 1.0)
    /// </summary>
    /// <param name="timestamp">Current time (defaults to current time)</param>
    /// <returns>Progress percentage</returns>
    public decimal GetProgressPercentage(DateTime? timestamp = null)
    {
        var elapsed = GetElapsedTime(timestamp);
        var total = PlannedEndTime - PlannedStartTime;

        return total.TotalSeconds > 0 ? (decimal)(elapsed.TotalSeconds / total.TotalSeconds) : 0m;
    }

    /// <summary>
    /// Determines if the schedule overlaps with a time period
    /// </summary>
    /// <param name="startTime">Period start time</param>
    /// <param name="endTime">Period end time</param>
    /// <returns>True if there is overlap</returns>
    public bool OverlapsWith(DateTime startTime, DateTime endTime)
    {
        return PlannedStartTime < endTime && PlannedEndTime > startTime;
    }

    /// <summary>
    /// Gets the overlap duration with a time period
    /// </summary>
    /// <param name="startTime">Period start time</param>
    /// <param name="endTime">Period end time</param>
    /// <returns>Overlap duration</returns>
    public TimeSpan GetOverlapWith(DateTime startTime, DateTime endTime)
    {
        if (!OverlapsWith(startTime, endTime))
            return TimeSpan.Zero;

        var overlapStart = PlannedStartTime > startTime ? PlannedStartTime : startTime;
        var overlapEnd = PlannedEndTime < endTime ? PlannedEndTime : endTime;

        return overlapEnd - overlapStart;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return ScheduleId;
        yield return LineId;
        yield return PlannedStartTime;
        yield return PlannedEndTime;
        yield return Status;
        yield return IsException;

        if (ShiftCode != null)
            yield return ShiftCode;
        if (Notes != null)
            yield return Notes;
    }

    public override string ToString() =>
        $"ActiveSchedule: {ScheduleId} for {LineId}, {PlannedStartTime:yyyy-MM-dd HH:mm} - {PlannedEndTime:HH:mm} ({Status})";
}

/// <summary>
/// Enumeration of schedule statuses
/// </summary>
public enum ScheduleStatus
{
    /// <summary>
    /// Schedule is planned but not yet active
    /// </summary>
    Planned = 0,

    /// <summary>
    /// Schedule is currently active
    /// </summary>
    Active = 1,

    /// <summary>
    /// Schedule has been completed
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Schedule has been cancelled
    /// </summary>
    Cancelled = 3,

    /// <summary>
    /// Schedule is on hold
    /// </summary>
    OnHold = 4
}
