namespace Industrial.Adam.Oee.Domain.ValueObjects;

/// <summary>
/// Value object representing planned operating hours for a specific date
/// </summary>
public sealed class PlannedHours : ValueObject
{
    /// <summary>
    /// Date for which hours are planned
    /// </summary>
    public DateTime Date { get; }

    /// <summary>
    /// Total planned operating hours for the date
    /// </summary>
    public decimal TotalHours { get; }

    /// <summary>
    /// Scheduled shifts for the date
    /// </summary>
    public IReadOnlyList<ScheduledShift> Shifts { get; }

    /// <summary>
    /// Indicates if this is an exception schedule (not following standard pattern)
    /// </summary>
    public bool IsException { get; }

    /// <summary>
    /// Confidence level of the planned hours (0.0 to 1.0)
    /// Used to indicate reliability of scheduling data
    /// </summary>
    public decimal Confidence { get; }

    /// <summary>
    /// Creates a new PlannedHours instance
    /// </summary>
    /// <param name="date">Date for planned hours</param>
    /// <param name="totalHours">Total planned hours</param>
    /// <param name="shifts">Scheduled shifts</param>
    /// <param name="isException">Whether this is an exception schedule</param>
    /// <param name="confidence">Confidence level (0.0 to 1.0)</param>
    public PlannedHours(DateTime date, decimal totalHours, IEnumerable<ScheduledShift> shifts, bool isException = false, decimal confidence = 1.0m)
    {
        if (totalHours < 0 || totalHours > 24)
            throw new ArgumentOutOfRangeException(nameof(totalHours), "Total hours must be between 0 and 24");

        if (confidence < 0 || confidence > 1)
            throw new ArgumentOutOfRangeException(nameof(confidence), "Confidence must be between 0 and 1");

        Date = date.Date;
        TotalHours = totalHours;
        Shifts = shifts?.ToList().AsReadOnly() ?? new List<ScheduledShift>().AsReadOnly();
        IsException = isException;
        Confidence = confidence;
    }

    /// <summary>
    /// Creates a PlannedHours instance with no scheduled hours
    /// </summary>
    /// <param name="date">Date</param>
    /// <returns>PlannedHours with zero hours</returns>
    public static PlannedHours NoScheduledHours(DateTime date) =>
        new(date, 0, Array.Empty<ScheduledShift>(), confidence: 1.0m);

    /// <summary>
    /// Creates a PlannedHours instance for a full 24-hour operation
    /// </summary>
    /// <param name="date">Date</param>
    /// <returns>PlannedHours with 24 hours</returns>
    public static PlannedHours FullDayOperation(DateTime date) =>
        new(date, 24, new[]
        {
            new ScheduledShift("24H", TimeOnly.MinValue, TimeOnly.MaxValue, 24m)
        }, confidence: 1.0m);

    /// <summary>
    /// Determines if equipment is planned to be operating at a specific time
    /// </summary>
    /// <param name="time">Time to check</param>
    /// <returns>True if operating is planned at the specified time</returns>
    public bool IsOperatingAt(DateTime time)
    {
        if (time.Date != Date)
            return false;

        var timeOfDay = TimeOnly.FromDateTime(time);
        return Shifts.Any(shift => shift.IsActiveAt(timeOfDay));
    }

    /// <summary>
    /// Gets the efficiency factor based on confidence
    /// Used to adjust OEE calculations when scheduling data is uncertain
    /// </summary>
    public decimal GetEfficiencyFactor() => Confidence;

    /// <summary>
    /// Gets the components used for equality comparison
    /// </summary>
    /// <returns>An enumerable of objects representing the value object's components</returns>
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Date;
        yield return TotalHours;
        yield return IsException;
        yield return Confidence;
        foreach (var shift in Shifts)
            yield return shift;
    }

    /// <summary>
    /// Returns a string representation of the planned hours
    /// </summary>
    /// <returns>A formatted string containing planned hours details</returns>
    public override string ToString() =>
        $"PlannedHours: {TotalHours:F1}h on {Date:yyyy-MM-dd} ({Shifts.Count} shifts)";
}

/// <summary>
/// Value object representing a scheduled shift
/// </summary>
public sealed class ScheduledShift : ValueObject
{
    /// <summary>
    /// Shift identifier or name
    /// </summary>
    public string ShiftCode { get; }

    /// <summary>
    /// Planned start time
    /// </summary>
    public TimeOnly StartTime { get; }

    /// <summary>
    /// Planned end time
    /// </summary>
    public TimeOnly EndTime { get; }

    /// <summary>
    /// Duration of the shift in hours
    /// </summary>
    public decimal Hours { get; }

    /// <summary>
    /// Creates a new ScheduledShift instance
    /// </summary>
    /// <param name="shiftCode">Shift identifier</param>
    /// <param name="startTime">Start time</param>
    /// <param name="endTime">End time</param>
    /// <param name="hours">Duration in hours</param>
    public ScheduledShift(string shiftCode, TimeOnly startTime, TimeOnly endTime, decimal hours)
    {
        if (string.IsNullOrWhiteSpace(shiftCode))
            throw new ArgumentException("Shift code cannot be null or empty", nameof(shiftCode));

        if (hours < 0 || hours > 24)
            throw new ArgumentOutOfRangeException(nameof(hours), "Hours must be between 0 and 24");

        ShiftCode = shiftCode;
        StartTime = startTime;
        EndTime = endTime;
        Hours = hours;
    }

    /// <summary>
    /// Determines if the shift is active at a specific time
    /// </summary>
    /// <param name="time">Time to check</param>
    /// <returns>True if shift is active at the specified time</returns>
    public bool IsActiveAt(TimeOnly time)
    {
        // Handle shifts that span midnight
        if (EndTime <= StartTime)
        {
            return time >= StartTime || time <= EndTime;
        }

        return time >= StartTime && time <= EndTime;
    }

    /// <summary>
    /// Gets the components used for equality comparison
    /// </summary>
    /// <returns>An enumerable of objects representing the value object's components</returns>
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return ShiftCode;
        yield return StartTime;
        yield return EndTime;
        yield return Hours;
    }

    /// <summary>
    /// Returns a string representation of the scheduled shift
    /// </summary>
    /// <returns>A formatted string containing shift details</returns>
    public override string ToString() =>
        $"{ShiftCode}: {StartTime:HH:mm}-{EndTime:HH:mm} ({Hours:F1}h)";
}
