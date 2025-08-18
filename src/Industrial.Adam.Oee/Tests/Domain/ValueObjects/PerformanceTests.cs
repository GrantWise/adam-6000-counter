using FluentAssertions;
using Industrial.Adam.Oee.Domain.ValueObjects;
using Xunit;

namespace Industrial.Adam.Oee.Tests.Domain.ValueObjects;

/// <summary>
/// Unit tests for the Performance value object
/// </summary>
public sealed class PerformanceTests
{
    [Fact]
    public void Constructor_WithValidInputs_ShouldCreatePerformance()
    {
        // Arrange
        var totalPieces = 450m;
        var runTime = 60m;
        var targetRate = 10m;

        // Act
        var performance = new Performance(totalPieces, runTime, targetRate);

        // Assert
        performance.TotalPiecesProduced.Should().Be(totalPieces);
        performance.TargetRatePerMinute.Should().Be(targetRate);
        performance.TheoreticalMaxProduction.Should().Be(600m); // 10 * 60
        performance.ActualRatePerMinute.Should().Be(7.5m); // 450 / 60
    }

    [Fact]
    public void Constructor_WithExplicitActualRate_ShouldUseProvidedRate()
    {
        // Arrange
        var totalPieces = 450m;
        var runTime = 60m;
        var targetRate = 10m;
        var actualRate = 8m;

        // Act
        var performance = new Performance(totalPieces, runTime, targetRate, actualRate);

        // Assert
        performance.ActualRatePerMinute.Should().Be(actualRate);
    }

    [Fact]
    public void Constructor_WithNegativeTotalPieces_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        var action = () => new Performance(-100, 60, 10);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*Total pieces produced cannot be negative*");
    }

    [Fact]
    public void Constructor_WithNegativeRunTime_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        var action = () => new Performance(100, -60, 10);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*Run time cannot be negative*");
    }

    [Fact]
    public void Constructor_WithZeroTargetRate_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        var action = () => new Performance(100, 60, 0);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*Target rate must be positive*");
    }

    [Fact]
    public void Constructor_WithZeroRunTime_ShouldSetActualRateToZero()
    {
        // Arrange & Act
        var performance = new Performance(100, 0, 10);

        // Assert
        performance.ActualRatePerMinute.Should().Be(0);
    }

    [Theory]
    [InlineData(600, 600, 100)]     // Perfect performance
    [InlineData(450, 600, 75)]      // Reduced performance
    [InlineData(750, 600, 100)]     // Capped at 100% for OEE
    [InlineData(0, 600, 0)]         // No production
    public void Percentage_WithVariousInputs_ShouldCalculateCorrectly(
        decimal actualPieces, decimal theoreticalMax, decimal expectedPercentage)
    {
        // Arrange
        var targetRate = theoreticalMax / 60; // 60 minute runtime
        var performance = new Performance(actualPieces, 60, targetRate);

        // Act & Assert
        performance.Percentage.Should().Be(expectedPercentage);
    }

    [Theory]
    [InlineData(600, 600, 100)]     // Perfect performance
    [InlineData(450, 600, 75)]      // Reduced performance
    [InlineData(750, 600, 125)]     // Over 100% not capped
    [InlineData(0, 600, 0)]         // No production
    public void RawPercentage_WithVariousInputs_ShouldCalculateCorrectly(
        decimal actualPieces, decimal theoreticalMax, decimal expectedPercentage)
    {
        // Arrange
        var targetRate = theoreticalMax / 60;
        var performance = new Performance(actualPieces, 60, targetRate);

        // Act & Assert
        performance.RawPercentage.Should().Be(expectedPercentage);
    }

    [Theory]
    [InlineData(600, 600, 1.0)]     // Perfect performance
    [InlineData(450, 600, 0.75)]    // Reduced performance
    [InlineData(750, 600, 1.0)]     // Capped at 1.0 for OEE
    public void Decimal_WithVariousInputs_ShouldCalculateCorrectly(
        decimal actualPieces, decimal theoreticalMax, decimal expectedDecimal)
    {
        // Arrange
        var targetRate = theoreticalMax / 60;
        var performance = new Performance(actualPieces, 60, targetRate);

        // Act & Assert
        performance.Decimal.Should().Be(expectedDecimal);
    }

    [Theory]
    [InlineData(450, 600, 150)]     // Speed loss calculation
    [InlineData(600, 600, 0)]       // No speed loss
    [InlineData(750, 600, 0)]       // Overproduction, no loss
    public void GetSpeedLoss_WithVariousInputs_ShouldCalculateCorrectly(
        decimal actualPieces, decimal theoreticalMax, decimal expectedLoss)
    {
        // Arrange
        var targetRate = theoreticalMax / 60;
        var performance = new Performance(actualPieces, 60, targetRate);

        // Act & Assert
        performance.GetSpeedLoss().Should().Be(expectedLoss);
    }

    [Theory]
    [InlineData(450, 600, 25)]      // 25% speed loss
    [InlineData(600, 600, 0)]       // No speed loss
    [InlineData(750, 600, 0)]       // Overproduction, no loss
    public void GetSpeedLossPercentage_WithVariousInputs_ShouldCalculateCorrectly(
        decimal actualPieces, decimal theoreticalMax, decimal expectedLossPercentage)
    {
        // Arrange
        var targetRate = theoreticalMax / 60;
        var performance = new Performance(actualPieces, 60, targetRate);

        // Act & Assert
        performance.GetSpeedLossPercentage().Should().Be(expectedLossPercentage);
    }

    [Theory]
    [InlineData(450, 600, 75, true)]    // Meets 75% target
    [InlineData(450, 600, 80, false)]   // Doesn't meet 80% target
    [InlineData(600, 600, 100, true)]   // Meets 100% target
    public void MeetsTarget_WithVariousTargets_ShouldReturnCorrectResult(
        decimal actualPieces, decimal theoreticalMax, decimal target, bool expectedResult)
    {
        // Arrange
        var targetRate = theoreticalMax / 60;
        var performance = new Performance(actualPieces, 60, targetRate);

        // Act & Assert
        performance.MeetsTarget(target).Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(10, 10, "No bottleneck - running at or near target speed")]
    [InlineData(8.5, 10, "No bottleneck - running at or near target speed")]   // 85%
    [InlineData(7.8, 10, "Minor speed losses - possible minor adjustments needed")]  // 78%
    [InlineData(7.2, 10, "Moderate speed losses - equipment may need adjustment or maintenance")]  // 72%
    [InlineData(5.5, 10, "Significant speed losses - investigate mechanical issues or operator training")]  // 55%
    [InlineData(3, 10, "Severe speed losses - critical bottleneck requiring immediate attention")]  // 30%
    public void IdentifyBottleneck_WithVariousRates_ShouldReturnCorrectAssessment(
        decimal actualRate, decimal targetRate, string expectedAssessment)
    {
        // Arrange
        var performance = new Performance(450, 60, targetRate, actualRate);

        // Act
        var result = performance.IdentifyBottleneck();

        // Assert
        result.Should().Be(expectedAssessment);
    }

    [Fact]
    public void GetBreakdown_ShouldReturnCorrectComponents()
    {
        // Arrange
        var performance = new Performance(450, 60, 10, 7.5m);

        // Act
        var breakdown = performance.GetBreakdown();

        // Assert
        breakdown.ActualProduction.Should().Be(450);
        breakdown.TheoreticalMax.Should().Be(600);
        breakdown.SpeedLoss.Should().Be(150);
        breakdown.ActualRate.Should().Be(7.5m);
        breakdown.TargetRate.Should().Be(10);
        breakdown.Efficiency.Should().Be(75);
    }

    [Theory]
    [InlineData(75, 85, 90, true)]   // Performance is lowest
    [InlineData(90, 75, 85, false)]  // Availability is lowest
    [InlineData(90, 85, 75, false)]  // Quality is lowest
    [InlineData(80, 80, 80, false)]  // All equal
    public void IsConstrainingFactor_WithVariousFactors_ShouldReturnCorrectResult(
        decimal performancePercent, decimal availabilityPercent, decimal qualityPercent, bool expectedResult)
    {
        // Arrange
        var targetRate = 10m;
        var totalPieces = (performancePercent / 100) * (targetRate * 60); // Calculate pieces for desired percentage
        var performance = new Performance(totalPieces, 60, targetRate);

        // Act & Assert
        performance.IsConstrainingFactor(availabilityPercent, qualityPercent).Should().Be(expectedResult);
    }

    [Fact]
    public void FromProductionData_ShouldCreateCorrectPerformance()
    {
        // Arrange
        var goodPieces = 450m;
        var runTime = 60m;
        var targetRate = 10m;

        // Act
        var performance = Performance.FromProductionData(goodPieces, runTime, targetRate);

        // Assert
        performance.TotalPiecesProduced.Should().Be(goodPieces);
        performance.ActualRatePerMinute.Should().Be(7.5m);
        performance.Percentage.Should().Be(75);
    }

    [Fact]
    public void Equals_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var performance1 = new Performance(450, 60, 10, 7.5m);
        var performance2 = new Performance(450, 60, 10, 7.5m);

        // Act & Assert
        performance1.Equals(performance2).Should().BeTrue();
        (performance1 == performance2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentValues_ShouldReturnFalse()
    {
        // Arrange
        var performance1 = new Performance(450, 60, 10);
        var performance2 = new Performance(400, 60, 10);

        // Act & Assert
        performance1.Equals(performance2).Should().BeFalse();
        (performance1 != performance2).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_WithSameValues_ShouldReturnSameHash()
    {
        // Arrange
        var performance1 = new Performance(450, 60, 10);
        var performance2 = new Performance(450, 60, 10);

        // Act & Assert
        performance1.GetHashCode().Should().Be(performance2.GetHashCode());
    }

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var performance = new Performance(450, 60, 10);

        // Act
        var result = performance.ToString();

        // Assert
        result.Should().Be("Performance: 75.0% (450/600 pieces, 7.5/10 pcs/min)");
    }

    [Fact]
    public void PerformanceBreakdown_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var breakdown = new PerformanceBreakdown(450, 600, 150, 7.5m, 10m, 75m);

        // Assert
        breakdown.ActualProduction.Should().Be(450);
        breakdown.TheoreticalMax.Should().Be(600);
        breakdown.SpeedLoss.Should().Be(150);
        breakdown.ActualRate.Should().Be(7.5m);
        breakdown.TargetRate.Should().Be(10m);
        breakdown.Efficiency.Should().Be(75m);
    }
}
