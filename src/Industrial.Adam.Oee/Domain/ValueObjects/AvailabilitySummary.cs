namespace Industrial.Adam.Oee.Domain.ValueObjects;

/// <summary>
/// Value object representing availability summary for a date range
/// </summary>
public sealed class AvailabilitySummary : ValueObject
{
    /// <summary>
    /// Equipment line identifier
    /// </summary>
    public string LineId { get; }

    /// <summary>
    /// Start date of the summary period
    /// </summary>
    public DateTime StartDate { get; }

    /// <summary>
    /// End date of the summary period
    /// </summary>
    public DateTime EndDate { get; }

    /// <summary>
    /// Total planned operating hours in the period
    /// </summary>
    public decimal TotalPlannedHours { get; }

    /// <summary>
    /// Total possible operating hours in the period (24 hours per day)
    /// </summary>
    public decimal TotalPossibleHours { get; }

    /// <summary>
    /// Planned availability percentage (0.0 to 1.0)
    /// </summary>
    public decimal AvailabilityPercentage { get; }

    /// <summary>
    /// Number of scheduled operating days
    /// </summary>
    public int ScheduledDays { get; }

    /// <summary>
    /// Total number of days in the period
    /// </summary>
    public int TotalDays { get; }

    /// <summary>
    /// Average confidence level across the period
    /// </summary>
    public decimal AverageConfidence { get; }

    /// <summary>
    /// Number of exception days (non-standard schedules)
    /// </summary>
    public int ExceptionDaysCount { get; }

    /// <summary>
    /// Daily availability breakdown
    /// </summary>
    public IReadOnlyList<DailyAvailability> DailyBreakdown { get; }

    /// <summary>
    /// Creates a new AvailabilitySummary instance
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="dailyBreakdown">Daily availability data</param>
    public AvailabilitySummary(string lineId, DateTime startDate, DateTime endDate, IEnumerable<DailyAvailability> dailyBreakdown)
    {
        if (string.IsNullOrWhiteSpace(lineId))
            throw new ArgumentException("Line ID cannot be null or empty", nameof(lineId));

        if (startDate > endDate)
            throw new ArgumentException("Start date must be before or equal to end date");

        LineId = lineId;
        StartDate = startDate.Date;
        EndDate = endDate.Date;
        DailyBreakdown = dailyBreakdown?.ToList().AsReadOnly() ?? new List<DailyAvailability>().AsReadOnly();

        // Calculate aggregated values
        TotalDays = (int)(EndDate - StartDate).TotalDays + 1;
        TotalPossibleHours = TotalDays * 24m;
        TotalPlannedHours = DailyBreakdown.Sum(d => d.PlannedHours);
        AvailabilityPercentage = TotalPossibleHours > 0 ? TotalPlannedHours / TotalPossibleHours : 0m;
        ScheduledDays = DailyBreakdown.Count(d => d.PlannedHours > 0);
        AverageConfidence = DailyBreakdown.Any() ? DailyBreakdown.Average(d => d.Confidence) : 0m;
        ExceptionDaysCount = DailyBreakdown.Count(d => d.IsException);
    }

    /// <summary>
    /// Gets availability summary for a specific day within the period
    /// </summary>
    /// <param name="date">Date to get availability for</param>
    /// <returns>Daily availability for the date, or null if not found</returns>
    public DailyAvailability? GetDailyAvailability(DateTime date)
    {
        var dateKey = date.Date;
        return DailyBreakdown.FirstOrDefault(d => d.Date == dateKey);
    }

    /// <summary>
    /// Determines if equipment is planned to be operating at a specific timestamp
    /// </summary>
    /// <param name="timestamp">Timestamp to check</param>
    /// <returns>True if equipment is planned to be operating</returns>
    public bool IsPlannedOperatingAt(DateTime timestamp)
    {
        if (timestamp.Date < StartDate || timestamp.Date > EndDate)
            return false;

        var dailyAvailability = GetDailyAvailability(timestamp);
        return dailyAvailability?.IsOperatingAt(timestamp) ?? false;
    }

    /// <summary>
    /// Gets the utilization rate (planned hours vs. scheduled days)
    /// </summary>
    /// <returns>Average hours per scheduled day</returns>
    public decimal GetUtilizationRate()
    {
        return ScheduledDays > 0 ? TotalPlannedHours / ScheduledDays : 0m;
    }

    /// <summary>
    /// Gets pattern consistency score for the period
    /// </summary>
    /// <returns>Consistency score (0.0 to 1.0)</returns>
    public decimal GetPatternConsistency()
    {
        return TotalDays > 0 ? 1.0m - ((decimal)ExceptionDaysCount / TotalDays) : 1.0m;
    }

    /// <summary>
    /// Creates an empty availability summary
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <returns>Empty availability summary</returns>
    public static AvailabilitySummary CreateEmpty(string lineId, DateTime startDate, DateTime endDate)
    {
        var dailyBreakdown = new List<DailyAvailability>();

        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            dailyBreakdown.Add(new DailyAvailability(date, 0m, Array.Empty<ScheduledShift>()));
        }

        return new AvailabilitySummary(lineId, startDate, endDate, dailyBreakdown);
    }

    /// <summary>
    /// Gets the components used for equality comparison
    /// </summary>
    /// <returns>An enumerable of objects representing the value object's components</returns>
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return LineId;
        yield return StartDate;
        yield return EndDate;
        yield return TotalPlannedHours;
        yield return AvailabilityPercentage;
        yield return AverageConfidence;

        foreach (var daily in DailyBreakdown.OrderBy(d => d.Date))
            yield return daily;
    }

    /// <summary>
    /// Returns a string representation of the availability summary
    /// </summary>
    /// <returns>A formatted string containing summary details</returns>
    public override string ToString() =>
        $"AvailabilitySummary: {LineId}, {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}, {AvailabilityPercentage:P1} availability";
}

/// <summary>
/// Value object representing daily availability information
/// </summary>
public sealed class DailyAvailability : ValueObject
{
    /// <summary>
    /// Date for this availability information
    /// </summary>
    public DateTime Date { get; }

    /// <summary>
    /// Planned operating hours for the date
    /// </summary>
    public decimal PlannedHours { get; }

    /// <summary>
    /// Scheduled shifts for the date
    /// </summary>
    public IReadOnlyList<ScheduledShift> Shifts { get; }

    /// <summary>
    /// Indicates if this is an exception schedule
    /// </summary>
    public bool IsException { get; }

    /// <summary>
    /// Confidence level for this day's schedule
    /// </summary>
    public decimal Confidence { get; }

    /// <summary>
    /// Creates a new DailyAvailability instance
    /// </summary>
    /// <param name="date">Date</param>
    /// <param name="plannedHours">Planned hours</param>
    /// <param name="shifts">Scheduled shifts</param>
    /// <param name="isException">Whether this is an exception schedule</param>
    /// <param name="confidence">Confidence level</param>
    public DailyAvailability(DateTime date, decimal plannedHours, IEnumerable<ScheduledShift> shifts,
        bool isException = false, decimal confidence = 1.0m)
    {
        if (plannedHours < 0 || plannedHours > 24)
            throw new ArgumentOutOfRangeException(nameof(plannedHours), "Planned hours must be between 0 and 24");

        if (confidence < 0 || confidence > 1)
            throw new ArgumentOutOfRangeException(nameof(confidence), "Confidence must be between 0 and 1");

        Date = date.Date;
        PlannedHours = plannedHours;
        Shifts = shifts?.ToList().AsReadOnly() ?? new List<ScheduledShift>().AsReadOnly();
        IsException = isException;
        Confidence = confidence;
    }

    /// <summary>
    /// Determines if equipment is operating at a specific time on this date
    /// </summary>
    /// <param name="timestamp">Timestamp to check</param>
    /// <returns>True if operating at the specified time</returns>
    public bool IsOperatingAt(DateTime timestamp)
    {
        if (timestamp.Date != Date)
            return false;

        var timeOfDay = TimeOnly.FromDateTime(timestamp);
        return Shifts.Any(shift => shift.IsActiveAt(timeOfDay));
    }

    /// <summary>
    /// Gets availability percentage for this day (0.0 to 1.0)
    /// </summary>
    public decimal GetDailyAvailabilityPercentage() => PlannedHours / 24m;

    /// <summary>
    /// Gets the components used for equality comparison
    /// </summary>
    /// <returns>An enumerable of objects representing the value object's components</returns>
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Date;
        yield return PlannedHours;
        yield return IsException;
        yield return Confidence;

        foreach (var shift in Shifts)
            yield return shift;
    }

    /// <summary>
    /// Returns a string representation of the daily availability
    /// </summary>
    /// <returns>A formatted string containing daily availability details</returns>
    public override string ToString() =>
        $"DailyAvailability: {Date:yyyy-MM-dd}, {PlannedHours:F1}h ({Shifts.Count} shifts)";
}
