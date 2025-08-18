using Industrial.Adam.Oee.Domain.ValueObjects;

namespace Industrial.Adam.Oee.Domain.ValueObjects;

/// <summary>
/// Performance Value Object
/// 
/// Represents the Performance (Efficiency) factor in OEE calculation.
/// Performance = (Total Pieces Produced / Theoretical Max Production) × 100
/// or
/// Performance = (Actual Production Rate / Target Production Rate) × 100
/// 
/// This measures speed losses - when equipment runs slower than its theoretical maximum speed.
/// </summary>
public sealed class Performance : ValueObject
{
    /// <summary>
    /// Total pieces produced during the time period
    /// </summary>
    public decimal TotalPiecesProduced { get; private set; }

    /// <summary>
    /// Theoretical maximum production based on target rate and run time
    /// </summary>
    public decimal TheoreticalMaxProduction { get; private set; }

    /// <summary>
    /// Actual production rate per minute
    /// </summary>
    public decimal ActualRatePerMinute { get; private set; }

    /// <summary>
    /// Target production rate per minute
    /// </summary>
    public decimal TargetRatePerMinute { get; private set; }

    /// <summary>
    /// Default constructor for serialization
    /// </summary>
    private Performance() { }

    /// <summary>
    /// Creates a new Performance value object
    /// </summary>
    /// <param name="totalPiecesProduced">Total pieces produced</param>
    /// <param name="runTimeMinutes">Runtime in minutes</param>
    /// <param name="targetRatePerMinute">Target production rate per minute</param>
    /// <param name="actualRatePerMinute">Optional actual rate (calculated if not provided)</param>
    /// <exception cref="ArgumentException">Thrown when values are invalid</exception>
    public Performance(
        decimal totalPiecesProduced,
        decimal runTimeMinutes,
        decimal targetRatePerMinute,
        decimal? actualRatePerMinute = null)
    {
        if (totalPiecesProduced < 0)
            throw new ArgumentException("Total pieces produced cannot be negative", nameof(totalPiecesProduced));

        if (runTimeMinutes < 0)
            throw new ArgumentException("Run time cannot be negative", nameof(runTimeMinutes));

        if (targetRatePerMinute <= 0)
            throw new ArgumentException("Target rate must be positive", nameof(targetRatePerMinute));

        TotalPiecesProduced = totalPiecesProduced;
        TargetRatePerMinute = targetRatePerMinute;
        TheoreticalMaxProduction = targetRatePerMinute * runTimeMinutes;
        ActualRatePerMinute = actualRatePerMinute ??
            (runTimeMinutes > 0 ? totalPiecesProduced / runTimeMinutes : 0);
    }

    /// <summary>
    /// Calculate performance percentage (0-100)
    /// Can exceed 100% if running faster than target, but capped for OEE calculation
    /// </summary>
    public decimal Percentage
    {
        get
        {
            if (TheoreticalMaxProduction == 0)
                return 0;

            var rawPercentage = (TotalPiecesProduced / TheoreticalMaxProduction) * 100;
            // Cap at 100% for OEE calculation (can't have OEE > 100%)
            return Math.Min(rawPercentage, 100);
        }
    }

    /// <summary>
    /// Get raw performance percentage (can exceed 100%)
    /// </summary>
    public decimal RawPercentage
    {
        get
        {
            if (TheoreticalMaxProduction == 0)
                return 0;

            return (TotalPiecesProduced / TheoreticalMaxProduction) * 100;
        }
    }

    /// <summary>
    /// Get performance as a decimal (0-1) for OEE calculation
    /// </summary>
    public decimal Decimal => Percentage / 100;

    /// <summary>
    /// Calculate speed loss in pieces
    /// </summary>
    /// <returns>Number of pieces lost due to speed reduction</returns>
    public decimal GetSpeedLoss() => Math.Max(0, TheoreticalMaxProduction - TotalPiecesProduced);

    /// <summary>
    /// Calculate speed loss in percentage points
    /// </summary>
    /// <returns>Speed loss as percentage</returns>
    public decimal GetSpeedLossPercentage() => Math.Max(0, 100 - Percentage);

    /// <summary>
    /// Check if performance meets the target percentage
    /// </summary>
    /// <param name="targetPercentage">Target percentage to compare against</param>
    /// <returns>True if performance meets or exceeds target</returns>
    public bool MeetsTarget(decimal targetPercentage) => Percentage >= targetPercentage;

    /// <summary>
    /// Identify potential bottleneck based on performance
    /// </summary>
    /// <param name="excellentThreshold">Threshold for excellent performance (default 85%)</param>
    /// <param name="goodThreshold">Threshold for good performance (default 75%)</param>
    /// <returns>Bottleneck assessment description</returns>
    public string IdentifyBottleneck(decimal excellentThreshold = 85m, decimal goodThreshold = 75m)
    {
        var ratio = ActualRatePerMinute / TargetRatePerMinute;
        var ratioPercentage = ratio * 100;

        return ratioPercentage switch
        {
            >= 85m when ratioPercentage >= excellentThreshold =>
                "No bottleneck - running at or near target speed",
            >= 75m when ratioPercentage >= goodThreshold =>
                "Minor speed losses - possible minor adjustments needed",
            >= 70 =>
                "Moderate speed losses - equipment may need adjustment or maintenance",
            >= 50 =>
                "Significant speed losses - investigate mechanical issues or operator training",
            _ =>
                "Severe speed losses - critical bottleneck requiring immediate attention"
        };
    }

    /// <summary>
    /// Get breakdown of performance components
    /// </summary>
    /// <returns>Performance breakdown details</returns>
    public PerformanceBreakdown GetBreakdown()
    {
        return new PerformanceBreakdown(
            TotalPiecesProduced,
            TheoreticalMaxProduction,
            GetSpeedLoss(),
            ActualRatePerMinute,
            TargetRatePerMinute,
            Percentage
        );
    }

    /// <summary>
    /// Identify if this is the constraining factor for OEE
    /// </summary>
    /// <param name="availabilityPercentage">Availability percentage to compare</param>
    /// <param name="qualityPercentage">Quality percentage to compare</param>
    /// <returns>True if performance is the lowest factor</returns>
    public bool IsConstrainingFactor(decimal availabilityPercentage, decimal qualityPercentage)
    {
        return Percentage < availabilityPercentage && Percentage < qualityPercentage;
    }

    /// <summary>
    /// Create from production counts and time
    /// </summary>
    /// <param name="goodPieces">Number of good pieces produced</param>
    /// <param name="runTimeMinutes">Runtime in minutes</param>
    /// <param name="targetRatePerMinute">Target production rate per minute</param>
    /// <returns>New Performance instance</returns>
    public static Performance FromProductionData(
        decimal goodPieces,
        decimal runTimeMinutes,
        decimal targetRatePerMinute)
    {
        return new Performance(goodPieces, runTimeMinutes, targetRatePerMinute);
    }

    /// <summary>
    /// Get equality components for value object comparison
    /// </summary>
    /// <returns>Components used for equality comparison</returns>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return TotalPiecesProduced;
        yield return TheoreticalMaxProduction;
        yield return TargetRatePerMinute;
        yield return ActualRatePerMinute;
    }

    /// <summary>
    /// String representation of the performance
    /// </summary>
    /// <returns>Formatted string representation</returns>
    public override string ToString()
    {
        return $"Performance: {Percentage:F1}% ({TotalPiecesProduced}/{TheoreticalMaxProduction} pieces, {ActualRatePerMinute:F1}/{TargetRatePerMinute} pcs/min)";
    }
}

/// <summary>
/// Breakdown of performance components
/// </summary>
/// <param name="ActualProduction">Actual pieces produced</param>
/// <param name="TheoreticalMax">Theoretical maximum production</param>
/// <param name="SpeedLoss">Speed loss in pieces</param>
/// <param name="ActualRate">Actual production rate per minute</param>
/// <param name="TargetRate">Target production rate per minute</param>
/// <param name="Efficiency">Performance efficiency percentage</param>
public record PerformanceBreakdown(
    decimal ActualProduction,
    decimal TheoreticalMax,
    decimal SpeedLoss,
    decimal ActualRate,
    decimal TargetRate,
    decimal Efficiency
);
