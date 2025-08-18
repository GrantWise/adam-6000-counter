using Industrial.Adam.Oee.Domain.ValueObjects;

namespace Industrial.Adam.Oee.Domain.ValueObjects;

/// <summary>
/// Availability Value Object
/// 
/// Represents the Availability factor in OEE calculation.
/// Availability = (Actual Run Time / Planned Production Time) × 100
/// 
/// This measures the percentage of scheduled time that the equipment is available 
/// to operate, accounting for both planned and unplanned downtime.
/// </summary>
public sealed class Availability : ValueObject
{
    /// <summary>
    /// Planned production time in minutes
    /// </summary>
    public decimal PlannedTimeMinutes { get; private set; }

    /// <summary>
    /// Actual run time in minutes
    /// </summary>
    public decimal ActualRunTimeMinutes { get; private set; }

    /// <summary>
    /// Downtime in minutes
    /// </summary>
    public decimal DowntimeMinutes { get; private set; }

    /// <summary>
    /// Default constructor for serialization
    /// </summary>
    private Availability() { }

    /// <summary>
    /// Creates a new Availability value object
    /// </summary>
    /// <param name="plannedTimeMinutes">Planned production time in minutes</param>
    /// <param name="actualRunTimeMinutes">Actual run time in minutes</param>
    /// <param name="downtimeMinutes">Optional downtime in minutes (calculated if not provided)</param>
    /// <exception cref="ArgumentException">Thrown when values are invalid</exception>
    public Availability(
        decimal plannedTimeMinutes,
        decimal actualRunTimeMinutes,
        decimal? downtimeMinutes = null)
    {
        if (plannedTimeMinutes < 0)
            throw new ArgumentException("Planned production time cannot be negative", nameof(plannedTimeMinutes));

        if (actualRunTimeMinutes < 0)
            throw new ArgumentException("Actual run time cannot be negative", nameof(actualRunTimeMinutes));

        if (actualRunTimeMinutes > plannedTimeMinutes)
            throw new ArgumentException("Actual run time cannot exceed planned production time", nameof(actualRunTimeMinutes));

        PlannedTimeMinutes = plannedTimeMinutes;
        ActualRunTimeMinutes = actualRunTimeMinutes;
        DowntimeMinutes = downtimeMinutes ?? (plannedTimeMinutes - actualRunTimeMinutes);
    }

    /// <summary>
    /// Calculate availability percentage (0-100)
    /// </summary>
    public decimal Percentage
    {
        get
        {
            if (PlannedTimeMinutes == 0)
                return 0;

            return (ActualRunTimeMinutes / PlannedTimeMinutes) * 100;
        }
    }

    /// <summary>
    /// Get availability as a decimal (0-1) for OEE calculation
    /// </summary>
    public decimal Decimal => Percentage / 100;

    /// <summary>
    /// Check if availability meets the target percentage
    /// </summary>
    /// <param name="targetPercentage">Target percentage to compare against</param>
    /// <returns>True if availability meets or exceeds target</returns>
    public bool MeetsTarget(decimal targetPercentage) => Percentage >= targetPercentage;

    /// <summary>
    /// Calculate the production impact of downtime in minutes
    /// </summary>
    /// <returns>Downtime impact in minutes</returns>
    public decimal GetDowntimeImpact() => DowntimeMinutes;

    /// <summary>
    /// Get breakdown of time components
    /// </summary>
    /// <returns>Availability breakdown details</returns>
    public AvailabilityBreakdown GetBreakdown()
    {
        return new AvailabilityBreakdown(
            PlannedTimeMinutes,
            ActualRunTimeMinutes,
            DowntimeMinutes,
            Percentage
        );
    }

    /// <summary>
    /// Identify if this is the constraining factor for OEE
    /// </summary>
    /// <param name="performancePercentage">Performance percentage to compare</param>
    /// <param name="qualityPercentage">Quality percentage to compare</param>
    /// <returns>True if availability is the lowest factor</returns>
    public bool IsConstrainingFactor(decimal performancePercentage, decimal qualityPercentage)
    {
        return Percentage < performancePercentage && Percentage < qualityPercentage;
    }

    /// <summary>
    /// Create from downtime records
    /// </summary>
    /// <param name="plannedMinutes">Planned production time in minutes</param>
    /// <param name="downtimeRecords">Collection of downtime records</param>
    /// <returns>New Availability instance</returns>
    public static Availability FromDowntimeRecords(
        decimal plannedMinutes,
        IEnumerable<DowntimeRecord> downtimeRecords)
    {
        var totalDowntime = downtimeRecords.Sum(record => record.DurationMinutes);
        var actualRunTime = plannedMinutes - totalDowntime;
        return new Availability(plannedMinutes, actualRunTime, totalDowntime);
    }

    /// <summary>
    /// Get equality components for value object comparison
    /// </summary>
    /// <returns>Components used for equality comparison</returns>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return PlannedTimeMinutes;
        yield return ActualRunTimeMinutes;
        yield return DowntimeMinutes;
    }

    /// <summary>
    /// String representation of the availability
    /// </summary>
    /// <returns>Formatted string representation</returns>
    public override string ToString()
    {
        return $"Availability: {Percentage:F1}% ({ActualRunTimeMinutes}/{PlannedTimeMinutes} min)";
    }
}

/// <summary>
/// Breakdown of availability components
/// </summary>
/// <param name="PlannedTimeMinutes">Planned production time in minutes</param>
/// <param name="ActualRunTimeMinutes">Actual run time in minutes</param>
/// <param name="DowntimeMinutes">Downtime in minutes</param>
/// <param name="UtilizationRate">Utilization rate percentage</param>
public record AvailabilityBreakdown(
    decimal PlannedTimeMinutes,
    decimal ActualRunTimeMinutes,
    decimal DowntimeMinutes,
    decimal UtilizationRate
);

/// <summary>
/// Represents a downtime record
/// </summary>
/// <param name="DurationMinutes">Duration of downtime in minutes</param>
/// <param name="Category">Category of downtime (planned or unplanned)</param>
public record DowntimeRecord(
    decimal DurationMinutes,
    DowntimeCategory Category
);

/// <summary>
/// Categories of downtime
/// </summary>
public enum DowntimeCategory
{
    /// <summary>
    /// Planned downtime (maintenance, changeovers, breaks)
    /// </summary>
    Planned,

    /// <summary>
    /// Unplanned downtime (breakdowns, quality issues)
    /// </summary>
    Unplanned
}
