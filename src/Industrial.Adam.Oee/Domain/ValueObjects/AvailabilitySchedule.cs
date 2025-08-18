namespace Industrial.Adam.Oee.Domain.ValueObjects;

/// <summary>
/// Value object representing a weekly availability schedule for equipment
/// </summary>
public sealed class AvailabilitySchedule : ValueObject
{
    /// <summary>
    /// Equipment line identifier
    /// </summary>
    public string LineId { get; }

    /// <summary>
    /// Start date of the week (typically Monday)
    /// </summary>
    public DateTime WeekStart { get; }

    /// <summary>
    /// End date of the week (typically Sunday)
    /// </summary>
    public DateTime WeekEnd { get; }

    /// <summary>
    /// Daily planned hours for each day of the week
    /// </summary>
    public IReadOnlyDictionary<DateTime, PlannedHours> DailyPlannedHours { get; }

    /// <summary>
    /// Total planned hours for the week
    /// </summary>
    public decimal TotalWeeklyHours { get; }

    /// <summary>
    /// Average confidence level across the week
    /// </summary>
    public decimal AverageConfidence { get; }

    /// <summary>
    /// Number of exception days (non-standard schedules)
    /// </summary>
    public int ExceptionDaysCount { get; }

    /// <summary>
    /// Creates a new AvailabilitySchedule instance
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="weekStart">Start of the week</param>
    /// <param name="dailyPlannedHours">Daily planned hours dictionary</param>
    public AvailabilitySchedule(string lineId, DateTime weekStart, IReadOnlyDictionary<DateTime, PlannedHours> dailyPlannedHours)
    {
        if (string.IsNullOrWhiteSpace(lineId))
            throw new ArgumentException("Line ID cannot be null or empty", nameof(lineId));

        if (dailyPlannedHours == null || !dailyPlannedHours.Any())
            throw new ArgumentException("Daily planned hours cannot be null or empty", nameof(dailyPlannedHours));

        LineId = lineId;
        WeekStart = weekStart.Date;
        WeekEnd = weekStart.AddDays(6).Date;
        DailyPlannedHours = dailyPlannedHours;

        // Calculate aggregated values
        TotalWeeklyHours = dailyPlannedHours.Values.Sum(p => p.TotalHours);
        AverageConfidence = dailyPlannedHours.Values.Any()
            ? dailyPlannedHours.Values.Average(p => p.Confidence)
            : 0m;
        ExceptionDaysCount = dailyPlannedHours.Values.Count(p => p.IsException);
    }

    /// <summary>
    /// Gets planned hours for a specific date within the week
    /// </summary>
    /// <param name="date">Date to get planned hours for</param>
    /// <returns>Planned hours for the date, or null if date is outside the week</returns>
    public PlannedHours? GetPlannedHoursForDate(DateTime date)
    {
        var dateKey = date.Date;
        return DailyPlannedHours.TryGetValue(dateKey, out var plannedHours) ? plannedHours : null;
    }

    /// <summary>
    /// Determines if equipment is planned to be operating at a specific timestamp
    /// </summary>
    /// <param name="timestamp">Timestamp to check</param>
    /// <returns>True if equipment is planned to be operating</returns>
    public bool IsPlannedOperatingAt(DateTime timestamp)
    {
        var plannedHours = GetPlannedHoursForDate(timestamp);
        return plannedHours?.IsOperatingAt(timestamp) ?? false;
    }

    /// <summary>
    /// Gets availability percentage for the week (0.0 to 1.0)
    /// Based on 24-hour days (168 hours per week)
    /// </summary>
    public decimal GetWeeklyAvailabilityPercentage()
    {
        const decimal totalPossibleHours = 168m; // 7 days * 24 hours
        return totalPossibleHours > 0 ? TotalWeeklyHours / totalPossibleHours : 0m;
    }

    /// <summary>
    /// Gets days with scheduled operations
    /// </summary>
    /// <returns>Dates with non-zero planned hours</returns>
    public IEnumerable<DateTime> GetScheduledDays()
    {
        return DailyPlannedHours
            .Where(kvp => kvp.Value.TotalHours > 0)
            .Select(kvp => kvp.Key)
            .OrderBy(d => d);
    }

    /// <summary>
    /// Gets the pattern consistency score (0.0 to 1.0)
    /// Higher scores indicate more consistent weekly patterns
    /// </summary>
    public decimal GetPatternConsistencyScore()
    {
        if (ExceptionDaysCount == 0)
            return 1.0m;

        var totalDays = DailyPlannedHours.Count;
        return totalDays > 0 ? 1.0m - ((decimal)ExceptionDaysCount / totalDays) : 0m;
    }

    /// <summary>
    /// Creates an empty availability schedule (no operations planned)
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="weekStart">Start of the week</param>
    /// <returns>Empty availability schedule</returns>
    public static AvailabilitySchedule CreateEmpty(string lineId, DateTime weekStart)
    {
        var dailyHours = new Dictionary<DateTime, PlannedHours>();

        for (int i = 0; i < 7; i++)
        {
            var date = weekStart.AddDays(i);
            dailyHours[date] = PlannedHours.NoScheduledHours(date);
        }

        return new AvailabilitySchedule(lineId, weekStart, dailyHours.AsReadOnly());
    }

    /// <summary>
    /// Creates a full-week availability schedule (24/7 operations)
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="weekStart">Start of the week</param>
    /// <returns>Full-week availability schedule</returns>
    public static AvailabilitySchedule CreateFullWeek(string lineId, DateTime weekStart)
    {
        var dailyHours = new Dictionary<DateTime, PlannedHours>();

        for (int i = 0; i < 7; i++)
        {
            var date = weekStart.AddDays(i);
            dailyHours[date] = PlannedHours.FullDayOperation(date);
        }

        return new AvailabilitySchedule(lineId, weekStart, dailyHours.AsReadOnly());
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return LineId;
        yield return WeekStart;
        yield return WeekEnd;
        yield return TotalWeeklyHours;
        yield return AverageConfidence;
        yield return ExceptionDaysCount;

        foreach (var kvp in DailyPlannedHours.OrderBy(x => x.Key))
        {
            yield return kvp.Key;
            yield return kvp.Value;
        }
    }

    public override string ToString() =>
        $"AvailabilitySchedule: {LineId}, Week {WeekStart:yyyy-MM-dd}, {TotalWeeklyHours:F1}h total";
}
