using Industrial.Adam.Oee.Domain.ValueObjects;

namespace Industrial.Adam.Oee.Domain.ValueObjects;

/// <summary>
/// Quality Value Object
/// 
/// Represents the Quality factor in OEE calculation.
/// Quality = (Good Pieces / Total Pieces Produced) × 100
/// 
/// This measures the percentage of products that meet quality standards without 
/// requiring rework. Also known as First Pass Yield (FPY).
/// </summary>
public sealed class Quality : ValueObject
{
    /// <summary>
    /// Number of good pieces produced
    /// </summary>
    public decimal GoodPieces { get; private set; }

    /// <summary>
    /// Number of defective pieces produced
    /// </summary>
    public decimal DefectivePieces { get; private set; }

    /// <summary>
    /// Total pieces produced (good + defective)
    /// </summary>
    public decimal TotalPiecesProduced { get; private set; }

    /// <summary>
    /// Default constructor for serialization
    /// </summary>
    private Quality() { }

    /// <summary>
    /// Creates a new Quality value object
    /// </summary>
    /// <param name="goodPieces">Number of good pieces</param>
    /// <param name="defectivePieces">Number of defective pieces</param>
    /// <param name="totalPiecesProduced">Optional total pieces (calculated if not provided)</param>
    /// <exception cref="ArgumentException">Thrown when values are invalid</exception>
    public Quality(
        decimal goodPieces,
        decimal defectivePieces,
        decimal? totalPiecesProduced = null)
    {
        if (goodPieces < 0)
            throw new ArgumentException("Good pieces cannot be negative", nameof(goodPieces));

        if (defectivePieces < 0)
            throw new ArgumentException("Defective pieces cannot be negative", nameof(defectivePieces));

        GoodPieces = goodPieces;
        DefectivePieces = defectivePieces;
        TotalPiecesProduced = totalPiecesProduced ?? (goodPieces + defectivePieces);

        if (TotalPiecesProduced < GoodPieces + DefectivePieces)
            throw new ArgumentException("Total pieces cannot be less than good + defective pieces");
    }

    /// <summary>
    /// Calculate quality percentage (0-100)
    /// Also known as First Pass Yield (FPY)
    /// </summary>
    public decimal Percentage
    {
        get
        {
            if (TotalPiecesProduced == 0)
                return 100; // No production = no defects

            return (GoodPieces / TotalPiecesProduced) * 100;
        }
    }

    /// <summary>
    /// Get quality as a decimal (0-1) for OEE calculation
    /// </summary>
    public decimal Decimal => Percentage / 100;

    /// <summary>
    /// Calculate defect rate as percentage
    /// </summary>
    /// <returns>Defect rate percentage</returns>
    public decimal GetDefectRate()
    {
        if (TotalPiecesProduced == 0)
            return 0;

        return (DefectivePieces / TotalPiecesProduced) * 100;
    }

    /// <summary>
    /// Calculate defects per million opportunities (DPMO)
    /// Standard Six Sigma metric
    /// </summary>
    /// <returns>Defects per million opportunities</returns>
    public decimal GetDPMO()
    {
        if (TotalPiecesProduced == 0)
            return 0;

        return (DefectivePieces / TotalPiecesProduced) * 1_000_000;
    }

    /// <summary>
    /// Calculate quality loss in pieces
    /// </summary>
    /// <returns>Number of defective pieces</returns>
    public decimal GetQualityLoss() => DefectivePieces;

    /// <summary>
    /// Check if quality meets the target percentage
    /// </summary>
    /// <param name="targetPercentage">Target percentage to compare against</param>
    /// <returns>True if quality meets or exceeds target</returns>
    public bool MeetsTarget(decimal targetPercentage) => Percentage >= targetPercentage;

    /// <summary>
    /// Determine if quality alert is required based on defect rate
    /// </summary>
    /// <param name="thresholdPercentage">Defect rate threshold (default 5%)</param>
    /// <returns>True if defect rate exceeds threshold</returns>
    public bool RequiresQualityAlert(decimal thresholdPercentage = 5m) => GetDefectRate() > thresholdPercentage;

    /// <summary>
    /// Get quality classification based on industry standards
    /// </summary>
    /// <returns>Quality level description</returns>
    public string GetQualityLevel()
    {
        var defectRate = GetDefectRate();

        return defectRate switch
        {
            0 => "Perfect - Zero defects",
            < 0.1m => "Excellent - World class quality",
            < 1m => "Good - High quality production",
            < 3m => "Acceptable - Standard quality",
            < 5m => "Marginal - Quality improvement needed",
            _ => "Poor - Immediate quality intervention required"
        };
    }

    /// <summary>
    /// Calculate the cost impact of quality issues
    /// </summary>
    /// <param name="costPerDefect">Average cost of a defective piece (scrap + rework)</param>
    /// <returns>Total cost impact of defects</returns>
    public decimal CalculateCostImpact(decimal costPerDefect) => DefectivePieces * costPerDefect;

    /// <summary>
    /// Get breakdown of quality components
    /// </summary>
    /// <returns>Quality breakdown details</returns>
    public QualityBreakdown GetBreakdown()
    {
        return new QualityBreakdown(
            GoodPieces,
            DefectivePieces,
            TotalPiecesProduced,
            Percentage,
            GetDefectRate(),
            GetDPMO()
        );
    }

    /// <summary>
    /// Identify if this is the constraining factor for OEE
    /// </summary>
    /// <param name="availabilityPercentage">Availability percentage to compare</param>
    /// <param name="performancePercentage">Performance percentage to compare</param>
    /// <returns>True if quality is the lowest factor</returns>
    public bool IsConstrainingFactor(decimal availabilityPercentage, decimal performancePercentage)
    {
        return Percentage < availabilityPercentage && Percentage < performancePercentage;
    }

    /// <summary>
    /// Create from counter channel data
    /// </summary>
    /// <param name="channelGood">Good pieces from production channel</param>
    /// <param name="channelRejects">Reject pieces from reject channel</param>
    /// <returns>New Quality instance</returns>
    public static Quality FromCounterChannels(decimal channelGood, decimal channelRejects)
    {
        return new Quality(channelGood, channelRejects);
    }

    /// <summary>
    /// Get equality components for value object comparison
    /// </summary>
    /// <returns>Components used for equality comparison</returns>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return GoodPieces;
        yield return DefectivePieces;
        yield return TotalPiecesProduced;
    }

    /// <summary>
    /// String representation of the quality
    /// </summary>
    /// <returns>Formatted string representation</returns>
    public override string ToString()
    {
        return $"Quality: {Percentage:F1}% ({GoodPieces}/{TotalPiecesProduced} good, {DefectivePieces} defects)";
    }
}

/// <summary>
/// Breakdown of quality components
/// </summary>
/// <param name="Good">Number of good pieces</param>
/// <param name="Defective">Number of defective pieces</param>
/// <param name="Total">Total pieces produced</param>
/// <param name="YieldRate">Yield rate percentage</param>
/// <param name="DefectRate">Defect rate percentage</param>
/// <param name="DPMO">Defects per million opportunities</param>
public record QualityBreakdown(
    decimal Good,
    decimal Defective,
    decimal Total,
    decimal YieldRate,
    decimal DefectRate,
    decimal DPMO
);
