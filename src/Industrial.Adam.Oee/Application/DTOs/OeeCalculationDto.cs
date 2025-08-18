using Industrial.Adam.Oee.Domain.Entities;

namespace Industrial.Adam.Oee.Application.DTOs;

/// <summary>
/// Data transfer object for OEE calculation results
/// </summary>
public class OeeCalculationDto
{
    /// <summary>
    /// OEE calculation identifier
    /// </summary>
    public string OeeId { get; set; } = string.Empty;

    /// <summary>
    /// Resource reference (device/machine identifier)
    /// </summary>
    public string ResourceReference { get; set; } = string.Empty;

    /// <summary>
    /// Start of the calculation period
    /// </summary>
    public DateTime CalculationPeriodStart { get; set; }

    /// <summary>
    /// End of the calculation period
    /// </summary>
    public DateTime CalculationPeriodEnd { get; set; }

    /// <summary>
    /// Availability percentage (0-100)
    /// </summary>
    public decimal AvailabilityPercentage { get; set; }

    /// <summary>
    /// Performance percentage (0-100)
    /// </summary>
    public decimal PerformancePercentage { get; set; }

    /// <summary>
    /// Quality percentage (0-100)
    /// </summary>
    public decimal QualityPercentage { get; set; }

    /// <summary>
    /// Overall OEE percentage (0-100)
    /// </summary>
    public decimal OeePercentage { get; set; }

    /// <summary>
    /// Period duration in hours
    /// </summary>
    public decimal PeriodHours { get; set; }

    /// <summary>
    /// Factor with the worst performance
    /// </summary>
    public string WorstFactor { get; set; } = string.Empty;

    /// <summary>
    /// OEE classification (World Class, Good, Fair, Poor)
    /// </summary>
    public string Classification { get; set; } = string.Empty;

    /// <summary>
    /// Whether this OEE requires attention
    /// </summary>
    public bool RequiresAttention { get; set; }

    /// <summary>
    /// When this calculation was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Create DTO from domain entity
    /// </summary>
    /// <param name="oeeCalculation">OEE calculation domain entity</param>
    /// <returns>OEE calculation DTO</returns>
    public static OeeCalculationDto FromDomain(OeeCalculation oeeCalculation)
    {
        return new OeeCalculationDto
        {
            OeeId = oeeCalculation.Id,
            ResourceReference = oeeCalculation.ResourceReference,
            CalculationPeriodStart = oeeCalculation.CalculationPeriodStart,
            CalculationPeriodEnd = oeeCalculation.CalculationPeriodEnd,
            AvailabilityPercentage = oeeCalculation.AvailabilityPercentage,
            PerformancePercentage = oeeCalculation.PerformancePercentage,
            QualityPercentage = oeeCalculation.QualityPercentage,
            OeePercentage = oeeCalculation.OeePercentage,
            PeriodHours = oeeCalculation.PeriodHours,
            WorstFactor = oeeCalculation.GetWorstFactor().ToString(),
            Classification = oeeCalculation.GetClassification(),
            RequiresAttention = oeeCalculation.RequiresAttention(),
            CreatedAt = oeeCalculation.CreatedAt
        };
    }
}
