using Industrial.Adam.Oee.Domain.Interfaces;
using Industrial.Adam.Oee.Domain.ValueObjects;

namespace Industrial.Adam.Oee.Domain.Entities;

/// <summary>
/// OEE Calculation Aggregate Root
/// 
/// Represents a complete OEE (Overall Equipment Effectiveness) calculation
/// following the canonical model structure.
/// 
/// OEE = Availability × Performance × Quality
/// 
/// This is the key metric for measuring manufacturing productivity, 
/// identifying losses, and improving equipment efficiency.
/// </summary>
public sealed class OeeCalculation : Entity<string>, IAggregateRoot
{
    /// <summary>
    /// Resource reference (device/machine identifier)
    /// </summary>
    public string ResourceReference { get; private set; }

    /// <summary>
    /// Start of the calculation period
    /// </summary>
    public DateTime CalculationPeriodStart { get; private set; }

    /// <summary>
    /// End of the calculation period
    /// </summary>
    public DateTime CalculationPeriodEnd { get; private set; }

    /// <summary>
    /// Availability component of OEE
    /// </summary>
    public Availability Availability { get; private set; }

    /// <summary>
    /// Performance component of OEE
    /// </summary>
    public Performance Performance { get; private set; }

    /// <summary>
    /// Quality component of OEE
    /// </summary>
    public Quality Quality { get; private set; }

    /// <summary>
    /// When this calculation was created
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Default constructor for serialization
    /// </summary>
    private OeeCalculation() : base()
    {
        ResourceReference = string.Empty;
        Availability = new Availability(0, 0);
        Performance = new Performance(0, 0, 1);
        Quality = new Quality(0, 0);
    }

    /// <summary>
    /// Creates a new OEE calculation
    /// </summary>
    /// <param name="oeeId">Optional OEE calculation ID (generated if not provided)</param>
    /// <param name="resourceReference">Resource/device identifier</param>
    /// <param name="calculationPeriodStart">Start of calculation period</param>
    /// <param name="calculationPeriodEnd">End of calculation period</param>
    /// <param name="availability">Availability value object</param>
    /// <param name="performance">Performance value object</param>
    /// <param name="quality">Quality value object</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public OeeCalculation(
        string? oeeId,
        string resourceReference,
        DateTime calculationPeriodStart,
        DateTime calculationPeriodEnd,
        Availability availability,
        Performance performance,
        Quality quality) : base(oeeId ?? GenerateOeeId(resourceReference))
    {
        if (string.IsNullOrWhiteSpace(resourceReference))
            throw new ArgumentException("Resource reference cannot be null or empty", nameof(resourceReference));

        if (calculationPeriodEnd <= calculationPeriodStart)
            throw new ArgumentException("Calculation period end must be after start", nameof(calculationPeriodEnd));

        ResourceReference = resourceReference;
        CalculationPeriodStart = calculationPeriodStart;
        CalculationPeriodEnd = calculationPeriodEnd;
        Availability = availability ?? throw new ArgumentNullException(nameof(availability));
        Performance = performance ?? throw new ArgumentNullException(nameof(performance));
        Quality = quality ?? throw new ArgumentNullException(nameof(quality));
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Get availability percentage (0-100)
    /// </summary>
    public decimal AvailabilityPercentage => Availability.Percentage;

    /// <summary>
    /// Get performance percentage (0-100)
    /// </summary>
    public decimal PerformancePercentage => Performance.Percentage;

    /// <summary>
    /// Get quality percentage (0-100)
    /// </summary>
    public decimal QualityPercentage => Quality.Percentage;

    /// <summary>
    /// Calculate overall OEE percentage (0-100)
    /// OEE = Availability × Performance × Quality
    /// </summary>
    public decimal OeePercentage =>
        Availability.Decimal * Performance.Decimal * Quality.Decimal * 100;

    /// <summary>
    /// Get OEE as a decimal (0-1)
    /// </summary>
    public decimal OeeDecimal => OeePercentage / 100;

    /// <summary>
    /// Calculate the period duration in hours
    /// </summary>
    public decimal PeriodHours
    {
        get
        {
            var periodMs = (CalculationPeriodEnd - CalculationPeriodStart).TotalMilliseconds;
            return (decimal)(periodMs / (1000 * 60 * 60));
        }
    }

    /// <summary>
    /// Determine if OEE requires attention based on thresholds
    /// </summary>
    /// <param name="thresholds">Optional custom thresholds</param>
    /// <returns>True if any metric is below threshold</returns>
    public bool RequiresAttention(OeeThresholds? thresholds = null)
    {
        var limits = thresholds ?? OeeThresholds.Default;

        return OeePercentage < limits.OeeThreshold ||
               AvailabilityPercentage < limits.AvailabilityThreshold ||
               PerformancePercentage < limits.PerformanceThreshold ||
               QualityPercentage < limits.QualityThreshold;
    }

    /// <summary>
    /// Identify which factor is the worst (constraining factor)
    /// </summary>
    /// <returns>The worst performing factor</returns>
    public OeeFactor GetWorstFactor()
    {
        var factors = new[]
        {
            (Factor: OeeFactor.Availability, Value: AvailabilityPercentage),
            (Factor: OeeFactor.Performance, Value: PerformancePercentage),
            (Factor: OeeFactor.Quality, Value: QualityPercentage)
        };

        return factors.OrderBy(f => f.Value).First().Factor;
    }

    /// <summary>
    /// Get improvement potential for each factor
    /// </summary>
    /// <param name="worldClassTargets">Optional world-class targets</param>
    /// <returns>Improvement potential breakdown</returns>
    public OeeImprovementPotential GetImprovementPotential(OeeTargets? worldClassTargets = null)
    {
        var targets = worldClassTargets ?? OeeTargets.WorldClass;

        return new OeeImprovementPotential(
            Math.Max(0, targets.AvailabilityTarget - AvailabilityPercentage),
            Math.Max(0, targets.PerformanceTarget - PerformancePercentage),
            Math.Max(0, targets.QualityTarget - QualityPercentage),
            Math.Max(0, (targets.AvailabilityTarget * targets.PerformanceTarget * targets.QualityTarget / 10000) - OeePercentage)
        );
    }

    /// <summary>
    /// Calculate what OEE would be if one factor improved
    /// </summary>
    /// <param name="factor">Factor to improve</param>
    /// <param name="newPercentage">New percentage for the factor</param>
    /// <returns>Simulated OEE percentage</returns>
    public decimal SimulateImprovement(OeeFactor factor, decimal newPercentage)
    {
        var (availability, performance, quality) = factor switch
        {
            OeeFactor.Availability => (newPercentage, PerformancePercentage, QualityPercentage),
            OeeFactor.Performance => (AvailabilityPercentage, newPercentage, QualityPercentage),
            OeeFactor.Quality => (AvailabilityPercentage, PerformancePercentage, newPercentage),
            _ => throw new ArgumentException($"Unknown OEE factor: {factor}", nameof(factor))
        };

        return (availability * performance * quality) / 10000;
    }

    /// <summary>
    /// Get OEE classification based on industry standards
    /// </summary>
    /// <returns>Classification description</returns>
    public string GetClassification()
    {
        var oee = OeePercentage;

        return oee switch
        {
            >= 85m => "World Class - Excellent performance",
            >= 65m => "Good - Acceptable performance",
            >= 40m => "Fair - Improvement needed",
            _ => "Poor - Significant improvement required"
        };
    }

    /// <summary>
    /// Get detailed breakdown of all components
    /// </summary>
    /// <returns>Complete OEE breakdown</returns>
    public OeeBreakdown GetBreakdown()
    {
        return new OeeBreakdown(
            OeePercentage,
            Availability.GetBreakdown(),
            Performance.GetBreakdown(),
            Quality.GetBreakdown(),
            GetWorstFactor().ToString(),
            GetClassification(),
            PeriodHours
        );
    }

    /// <summary>
    /// Create a summary object for reporting
    /// </summary>
    /// <returns>OEE summary for reports</returns>
    public OeeSummary ToSummary()
    {
        return new OeeSummary(
            Id,
            ResourceReference,
            CalculationPeriodStart,
            CalculationPeriodEnd,
            OeePercentage,
            AvailabilityPercentage,
            PerformancePercentage,
            QualityPercentage,
            GetWorstFactor().ToString(),
            RequiresAttention()
        );
    }

    /// <summary>
    /// Generate a unique OEE ID
    /// </summary>
    /// <param name="resourceReference">Resource identifier</param>
    /// <returns>Generated OEE ID</returns>
    private static string GenerateOeeId(string resourceReference)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return $"OEE-{resourceReference}-{timestamp}";
    }

    /// <summary>
    /// String representation of the OEE calculation
    /// </summary>
    /// <returns>Formatted string representation</returns>
    public override string ToString()
    {
        return $"OEE: {OeePercentage:F1}% (A:{AvailabilityPercentage:F1}% × P:{PerformancePercentage:F1}% × Q:{QualityPercentage:F1}%)";
    }
}

/// <summary>
/// OEE factor enumeration
/// </summary>
public enum OeeFactor
{
    /// <summary>
    /// Availability factor
    /// </summary>
    Availability,

    /// <summary>
    /// Performance factor
    /// </summary>
    Performance,

    /// <summary>
    /// Quality factor
    /// </summary>
    Quality
}

/// <summary>
/// OEE thresholds for attention alerts
/// </summary>
/// <param name="OeeThreshold">Overall OEE threshold</param>
/// <param name="AvailabilityThreshold">Availability threshold</param>
/// <param name="PerformanceThreshold">Performance threshold</param>
/// <param name="QualityThreshold">Quality threshold</param>
public record OeeThresholds(
    decimal OeeThreshold,
    decimal AvailabilityThreshold,
    decimal PerformanceThreshold,
    decimal QualityThreshold
)
{
    /// <summary>
    /// Default thresholds for attention alerts
    /// </summary>
    public static readonly OeeThresholds Default = new(60m, 80m, 75m, 95m);
}

/// <summary>
/// World-class OEE targets
/// </summary>
/// <param name="AvailabilityTarget">Target availability percentage</param>
/// <param name="PerformanceTarget">Target performance percentage</param>
/// <param name="QualityTarget">Target quality percentage</param>
public record OeeTargets(
    decimal AvailabilityTarget,
    decimal PerformanceTarget,
    decimal QualityTarget
)
{
    /// <summary>
    /// World-class targets
    /// </summary>
    public static readonly OeeTargets WorldClass = new(90m, 95m, 99m);
}

/// <summary>
/// OEE improvement potential
/// </summary>
/// <param name="Availability">Availability improvement potential</param>
/// <param name="Performance">Performance improvement potential</param>
/// <param name="Quality">Quality improvement potential</param>
/// <param name="Overall">Overall OEE improvement potential</param>
public record OeeImprovementPotential(
    decimal Availability,
    decimal Performance,
    decimal Quality,
    decimal Overall
);

/// <summary>
/// Complete OEE breakdown
/// </summary>
/// <param name="OeePercentage">Overall OEE percentage</param>
/// <param name="Availability">Availability breakdown</param>
/// <param name="Performance">Performance breakdown</param>
/// <param name="Quality">Quality breakdown</param>
/// <param name="WorstFactor">Worst performing factor</param>
/// <param name="Classification">OEE classification</param>
/// <param name="PeriodHours">Period duration in hours</param>
public record OeeBreakdown(
    decimal OeePercentage,
    AvailabilityBreakdown Availability,
    PerformanceBreakdown Performance,
    QualityBreakdown Quality,
    string WorstFactor,
    string Classification,
    decimal PeriodHours
);

/// <summary>
/// OEE summary for reporting
/// </summary>
/// <param name="OeeId">OEE calculation ID</param>
/// <param name="ResourceReference">Resource identifier</param>
/// <param name="PeriodStart">Calculation period start</param>
/// <param name="PeriodEnd">Calculation period end</param>
/// <param name="OeePercentage">Overall OEE percentage</param>
/// <param name="AvailabilityPercentage">Availability percentage</param>
/// <param name="PerformancePercentage">Performance percentage</param>
/// <param name="QualityPercentage">Quality percentage</param>
/// <param name="WorstFactor">Worst performing factor</param>
/// <param name="RequiresAttention">Whether this calculation requires attention</param>
public record OeeSummary(
    string OeeId,
    string ResourceReference,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    decimal OeePercentage,
    decimal AvailabilityPercentage,
    decimal PerformancePercentage,
    decimal QualityPercentage,
    string WorstFactor,
    bool RequiresAttention
);
