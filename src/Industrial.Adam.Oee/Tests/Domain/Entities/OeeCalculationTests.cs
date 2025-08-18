using FluentAssertions;
using Industrial.Adam.Oee.Domain.Entities;
using Industrial.Adam.Oee.Domain.ValueObjects;
using Xunit;

namespace Industrial.Adam.Oee.Tests.Domain.Entities;

/// <summary>
/// Unit tests for the OeeCalculation aggregate root
/// </summary>
public sealed class OeeCalculationTests
{
    private readonly Availability _sampleAvailability;
    private readonly Performance _samplePerformance;
    private readonly Quality _sampleQuality;
    private readonly DateTime _startTime;
    private readonly DateTime _endTime;

    public OeeCalculationTests()
    {
        _sampleAvailability = new Availability(480, 420); // 87.5% availability
        _samplePerformance = new Performance(450, 60, 10); // 75% performance
        _sampleQuality = new Quality(450, 50); // 90% quality
        _startTime = new DateTime(2024, 1, 15, 8, 0, 0, DateTimeKind.Utc);
        _endTime = new DateTime(2024, 1, 15, 16, 0, 0, DateTimeKind.Utc);
    }

    [Fact]
    public void Constructor_WithValidInputs_ShouldCreateOeeCalculation()
    {
        // Arrange
        var oeeId = "OEE-TEST-001";
        var resourceReference = "LINE-001";

        // Act
        var oeeCalculation = new OeeCalculation(
            oeeId,
            resourceReference,
            _startTime,
            _endTime,
            _sampleAvailability,
            _samplePerformance,
            _sampleQuality
        );

        // Assert
        oeeCalculation.Id.Should().Be(oeeId);
        oeeCalculation.ResourceReference.Should().Be(resourceReference);
        oeeCalculation.CalculationPeriodStart.Should().Be(_startTime);
        oeeCalculation.CalculationPeriodEnd.Should().Be(_endTime);
        oeeCalculation.Availability.Should().Be(_sampleAvailability);
        oeeCalculation.Performance.Should().Be(_samplePerformance);
        oeeCalculation.Quality.Should().Be(_sampleQuality);
        oeeCalculation.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Constructor_WithNullOeeId_ShouldGenerateId()
    {
        // Arrange & Act
        var oeeCalculation = new OeeCalculation(
            null,
            "LINE-001",
            _startTime,
            _endTime,
            _sampleAvailability,
            _samplePerformance,
            _sampleQuality
        );

        // Assert
        oeeCalculation.Id.Should().NotBeNullOrEmpty();
        oeeCalculation.Id.Should().StartWith("OEE-LINE-001-");
    }

    [Fact]
    public void Constructor_WithEmptyResourceReference_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        var action = () => new OeeCalculation(
            "OEE-001",
            "",
            _startTime,
            _endTime,
            _sampleAvailability,
            _samplePerformance,
            _sampleQuality
        );

        action.Should().Throw<ArgumentException>()
            .WithMessage("*Resource reference cannot be null or empty*");
    }

    [Fact]
    public void Constructor_WithEndTimeBeforeStartTime_ShouldThrowArgumentException()
    {
        // Arrange
        var invalidEndTime = _startTime.AddHours(-1);

        // Act & Assert
        var action = () => new OeeCalculation(
            "OEE-001",
            "LINE-001",
            _startTime,
            invalidEndTime,
            _sampleAvailability,
            _samplePerformance,
            _sampleQuality
        );

        action.Should().Throw<ArgumentException>()
            .WithMessage("*Calculation period end must be after start*");
    }

    [Fact]
    public void Constructor_WithNullAvailability_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var action = () => new OeeCalculation(
            "OEE-001",
            "LINE-001",
            _startTime,
            _endTime,
            null!,
            _samplePerformance,
            _sampleQuality
        );

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("availability");
    }

    [Fact]
    public void OeePercentage_ShouldCalculateCorrectly()
    {
        // Arrange
        var oeeCalculation = CreateSampleOeeCalculation();

        // Act & Assert
        // OEE = 87.5% * 75% * 90% = 59.0625%
        oeeCalculation.OeePercentage.Should().BeApproximately(59.0625m, 0.01m);
    }

    [Fact]
    public void OeeDecimal_ShouldCalculateCorrectly()
    {
        // Arrange
        var oeeCalculation = CreateSampleOeeCalculation();

        // Act & Assert
        oeeCalculation.OeeDecimal.Should().BeApproximately(0.590625m, 0.000001m);
    }

    [Fact]
    public void AvailabilityPercentage_ShouldReturnCorrectValue()
    {
        // Arrange
        var oeeCalculation = CreateSampleOeeCalculation();

        // Act & Assert
        oeeCalculation.AvailabilityPercentage.Should().Be(87.5m);
    }

    [Fact]
    public void PerformancePercentage_ShouldReturnCorrectValue()
    {
        // Arrange
        var oeeCalculation = CreateSampleOeeCalculation();

        // Act & Assert
        oeeCalculation.PerformancePercentage.Should().Be(75m);
    }

    [Fact]
    public void QualityPercentage_ShouldReturnCorrectValue()
    {
        // Arrange
        var oeeCalculation = CreateSampleOeeCalculation();

        // Act & Assert
        oeeCalculation.QualityPercentage.Should().Be(90m);
    }

    [Fact]
    public void PeriodHours_ShouldCalculateCorrectly()
    {
        // Arrange
        var oeeCalculation = CreateSampleOeeCalculation();

        // Act & Assert
        oeeCalculation.PeriodHours.Should().Be(8m); // 8 hour shift
    }

    [Theory]
    [InlineData(55, 80, 75, 95, true)]    // OEE below threshold
    [InlineData(65, 80, 70, 95, true)]    // Performance below threshold
    [InlineData(65, 75, 75, 95, true)]    // Availability below threshold
    [InlineData(65, 80, 75, 85, true)]    // Quality below threshold
    [InlineData(50, 80, 75, 85, false)]   // All above thresholds
    public void RequiresAttention_WithVariousThresholds_ShouldReturnCorrectResult(
        decimal oeeThreshold, decimal availThreshold, decimal perfThreshold, decimal qualThreshold, bool expectedResult)
    {
        // Arrange
        var oeeCalculation = CreateSampleOeeCalculation();
        var thresholds = new OeeThresholds(oeeThreshold, availThreshold, perfThreshold, qualThreshold);

        // Act & Assert
        oeeCalculation.RequiresAttention(thresholds).Should().Be(expectedResult);
    }

    [Fact]
    public void RequiresAttention_WithDefaultThresholds_ShouldReturnTrue()
    {
        // Arrange
        var oeeCalculation = CreateSampleOeeCalculation();

        // Act & Assert
        // Default thresholds: OEE 60%, Availability 80%, Performance 75%, Quality 95%
        // Our sample: OEE 59.06%, Availability 87.5%, Performance 75%, Quality 90%
        // Should require attention due to OEE below 60% and Quality below 95%
        oeeCalculation.RequiresAttention().Should().BeTrue();
    }

    [Theory]
    [InlineData(87.5, 75, 90, OeeFactor.Performance)]    // Performance is lowest
    [InlineData(75, 87.5, 90, OeeFactor.Availability)]   // Availability is lowest
    [InlineData(87.5, 90, 75, OeeFactor.Quality)]        // Quality is lowest
    public void GetWorstFactor_WithVariousValues_ShouldReturnCorrectFactor(
        decimal availPercent, decimal perfPercent, decimal qualPercent, OeeFactor expectedFactor)
    {
        // Arrange
        var availability = new Availability(100, availPercent);
        var performance = new Performance(perfPercent * 6, 60, 10); // Adjust pieces for percentage
        var quality = new Quality(qualPercent * 10, (100 - qualPercent) * 10);

        var oeeCalculation = new OeeCalculation(
            null, "LINE-001", _startTime, _endTime, availability, performance, quality);

        // Act & Assert
        oeeCalculation.GetWorstFactor().Should().Be(expectedFactor);
    }

    [Fact]
    public void GetImprovementPotential_WithWorldClassTargets_ShouldCalculateCorrectly()
    {
        // Arrange
        var oeeCalculation = CreateSampleOeeCalculation();
        var worldClassTargets = new OeeTargets(90m, 95m, 99m);

        // Act
        var potential = oeeCalculation.GetImprovementPotential(worldClassTargets);

        // Assert
        potential.Availability.Should().Be(2.5m); // 90 - 87.5
        potential.Performance.Should().Be(20m);   // 95 - 75
        potential.Quality.Should().Be(9m);        // 99 - 90
        // Overall = (90 * 95 * 99 / 10000) - 59.0625 = 84.645 - 59.0625 = 25.5825
        potential.Overall.Should().BeApproximately(25.58m, 0.01m);
    }

    [Theory]
    [InlineData(OeeFactor.Availability, 95, 64.13)]  // Improve availability to 95%
    [InlineData(OeeFactor.Performance, 85, 66.94)]   // Improve performance to 85%
    [InlineData(OeeFactor.Quality, 99, 64.97)]       // Improve quality to 99%
    public void SimulateImprovement_WithVariousFactors_ShouldCalculateCorrectly(
        OeeFactor factor, decimal newPercentage, decimal expectedOee)
    {
        // Arrange
        var oeeCalculation = CreateSampleOeeCalculation();

        // Act
        var result = oeeCalculation.SimulateImprovement(factor, newPercentage);

        // Assert
        result.Should().BeApproximately(expectedOee, 0.1m);
    }

    [Theory]
    [InlineData(85, "World Class - Excellent performance")]
    [InlineData(70, "Good - Acceptable performance")]
    [InlineData(50, "Fair - Improvement needed")]
    [InlineData(30, "Poor - Significant improvement required")]
    public void GetClassification_WithVariousOeeValues_ShouldReturnCorrectClassification(
        decimal oeePercent, string expectedClassification)
    {
        // Arrange
        // Create OEE calculation that produces the desired OEE percentage
        var targetDecimal = oeePercent / 100m;
        var performance = new Performance(targetDecimal * 600, 60, 10); // Adjust for target OEE

        var oeeCalculation = new OeeCalculation(
            null, "LINE-001", _startTime, _endTime,
            new Availability(100, 100), // 100% availability
            performance,
            new Quality(100, 0) // 100% quality
        );

        // Act
        var result = oeeCalculation.GetClassification();

        // Assert
        result.Should().Be(expectedClassification);
    }

    [Fact]
    public void GetBreakdown_ShouldReturnCompleteBreakdown()
    {
        // Arrange
        var oeeCalculation = CreateSampleOeeCalculation();

        // Act
        var breakdown = oeeCalculation.GetBreakdown();

        // Assert
        breakdown.OeePercentage.Should().BeApproximately(59.06m, 0.01m);
        breakdown.Availability.Should().NotBeNull();
        breakdown.Performance.Should().NotBeNull();
        breakdown.Quality.Should().NotBeNull();
        breakdown.WorstFactor.Should().Be("Performance");
        breakdown.Classification.Should().Contain("Fair");
        breakdown.PeriodHours.Should().Be(8m);
    }

    [Fact]
    public void ToSummary_ShouldReturnCorrectSummary()
    {
        // Arrange
        var oeeCalculation = CreateSampleOeeCalculation();

        // Act
        var summary = oeeCalculation.ToSummary();

        // Assert
        summary.OeeId.Should().Be(oeeCalculation.Id);
        summary.ResourceReference.Should().Be("LINE-001");
        summary.PeriodStart.Should().Be(_startTime);
        summary.PeriodEnd.Should().Be(_endTime);
        summary.OeePercentage.Should().BeApproximately(59.06m, 0.01m);
        summary.AvailabilityPercentage.Should().Be(87.5m);
        summary.PerformancePercentage.Should().Be(75m);
        summary.QualityPercentage.Should().Be(90m);
        summary.WorstFactor.Should().Be("Performance");
        summary.RequiresAttention.Should().BeTrue();
    }

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var oeeCalculation = CreateSampleOeeCalculation();

        // Act
        var result = oeeCalculation.ToString();

        // Assert
        result.Should().Be("OEE: 59.1% (A:87.5% × P:75.0% × Q:90.0%)");
    }

    [Fact]
    public void Equals_WithSameId_ShouldReturnTrue()
    {
        // Arrange
        var oeeId = "OEE-TEST-001";
        var oeeCalculation1 = new OeeCalculation(oeeId, "LINE-001", _startTime, _endTime,
            _sampleAvailability, _samplePerformance, _sampleQuality);
        var oeeCalculation2 = new OeeCalculation(oeeId, "LINE-002", _startTime.AddDays(1), _endTime.AddDays(1),
            _sampleAvailability, _samplePerformance, _sampleQuality);

        // Act & Assert
        oeeCalculation1.Equals(oeeCalculation2).Should().BeTrue();
        (oeeCalculation1 == oeeCalculation2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentId_ShouldReturnFalse()
    {
        // Arrange
        var oeeCalculation1 = new OeeCalculation("OEE-001", "LINE-001", _startTime, _endTime,
            _sampleAvailability, _samplePerformance, _sampleQuality);
        var oeeCalculation2 = new OeeCalculation("OEE-002", "LINE-001", _startTime, _endTime,
            _sampleAvailability, _samplePerformance, _sampleQuality);

        // Act & Assert
        oeeCalculation1.Equals(oeeCalculation2).Should().BeFalse();
        (oeeCalculation1 != oeeCalculation2).Should().BeTrue();
    }

    [Fact]
    public void OeeThresholds_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var defaults = OeeThresholds.Default;

        // Assert
        defaults.OeeThreshold.Should().Be(60m);
        defaults.AvailabilityThreshold.Should().Be(80m);
        defaults.PerformanceThreshold.Should().Be(75m);
        defaults.QualityThreshold.Should().Be(95m);
    }

    [Fact]
    public void OeeTargets_WorldClassValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var worldClass = OeeTargets.WorldClass;

        // Assert
        worldClass.AvailabilityTarget.Should().Be(90m);
        worldClass.PerformanceTarget.Should().Be(95m);
        worldClass.QualityTarget.Should().Be(99m);
    }

    private OeeCalculation CreateSampleOeeCalculation()
    {
        return new OeeCalculation(
            "OEE-TEST-001",
            "LINE-001",
            _startTime,
            _endTime,
            _sampleAvailability,
            _samplePerformance,
            _sampleQuality
        );
    }
}
