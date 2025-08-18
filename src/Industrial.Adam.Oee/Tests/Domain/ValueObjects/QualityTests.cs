using FluentAssertions;
using Industrial.Adam.Oee.Domain.ValueObjects;
using Xunit;

namespace Industrial.Adam.Oee.Tests.Domain.ValueObjects;

/// <summary>
/// Unit tests for the Quality value object
/// </summary>
public sealed class QualityTests
{
    [Fact]
    public void Constructor_WithValidInputs_ShouldCreateQuality()
    {
        // Arrange
        var goodPieces = 450m;
        var defectivePieces = 50m;

        // Act
        var quality = new Quality(goodPieces, defectivePieces);

        // Assert
        quality.GoodPieces.Should().Be(goodPieces);
        quality.DefectivePieces.Should().Be(defectivePieces);
        quality.TotalPiecesProduced.Should().Be(500m);
    }

    [Fact]
    public void Constructor_WithExplicitTotal_ShouldUseProvidedTotal()
    {
        // Arrange
        var goodPieces = 450m;
        var defectivePieces = 50m;
        var explicitTotal = 600m; // Higher than good + defective (includes rework)

        // Act
        var quality = new Quality(goodPieces, defectivePieces, explicitTotal);

        // Assert
        quality.TotalPiecesProduced.Should().Be(explicitTotal);
    }

    [Fact]
    public void Constructor_WithNegativeGoodPieces_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        var action = () => new Quality(-100, 50);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*Good pieces cannot be negative*");
    }

    [Fact]
    public void Constructor_WithNegativeDefectivePieces_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        var action = () => new Quality(100, -50);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*Defective pieces cannot be negative*");
    }

    [Fact]
    public void Constructor_WithTotalLessThanGoodPlusDefective_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        var action = () => new Quality(100, 50, 100);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*Total pieces cannot be less than good + defective pieces*");
    }

    [Theory]
    [InlineData(450, 50, 90)]       // 90% quality
    [InlineData(500, 0, 100)]       // Perfect quality
    [InlineData(0, 100, 0)]         // No good pieces
    [InlineData(0, 0, 100)]         // No production = no defects
    public void Percentage_WithVariousInputs_ShouldCalculateCorrectly(
        decimal goodPieces, decimal defectivePieces, decimal expectedPercentage)
    {
        // Arrange
        var quality = new Quality(goodPieces, defectivePieces);

        // Act & Assert
        quality.Percentage.Should().Be(expectedPercentage);
    }

    [Theory]
    [InlineData(450, 50, 0.9)]      // 90% quality
    [InlineData(500, 0, 1.0)]       // Perfect quality
    [InlineData(0, 100, 0.0)]       // No good pieces
    public void Decimal_WithVariousInputs_ShouldCalculateCorrectly(
        decimal goodPieces, decimal defectivePieces, decimal expectedDecimal)
    {
        // Arrange
        var quality = new Quality(goodPieces, defectivePieces);

        // Act & Assert
        quality.Decimal.Should().Be(expectedDecimal);
    }

    [Theory]
    [InlineData(450, 50, 10)]       // 10% defect rate
    [InlineData(500, 0, 0)]         // No defects
    [InlineData(0, 100, 100)]       // All defects
    [InlineData(0, 0, 0)]           // No production
    public void GetDefectRate_WithVariousInputs_ShouldCalculateCorrectly(
        decimal goodPieces, decimal defectivePieces, decimal expectedDefectRate)
    {
        // Arrange
        var quality = new Quality(goodPieces, defectivePieces);

        // Act & Assert
        quality.GetDefectRate().Should().Be(expectedDefectRate);
    }

    [Theory]
    [InlineData(450, 50, 100000)]   // 100,000 DPMO (10% defect rate)
    [InlineData(500, 0, 0)]         // Zero DPMO
    [InlineData(0, 100, 1000000)]   // 1,000,000 DPMO (100% defect rate)
    [InlineData(0, 0, 0)]           // No production
    public void GetDPMO_WithVariousInputs_ShouldCalculateCorrectly(
        decimal goodPieces, decimal defectivePieces, decimal expectedDPMO)
    {
        // Arrange
        var quality = new Quality(goodPieces, defectivePieces);

        // Act & Assert
        quality.GetDPMO().Should().Be(expectedDPMO);
    }

    [Fact]
    public void GetQualityLoss_ShouldReturnDefectivePieces()
    {
        // Arrange
        var quality = new Quality(450, 50);

        // Act & Assert
        quality.GetQualityLoss().Should().Be(50);
    }

    [Theory]
    [InlineData(450, 50, 90, true)]     // Meets 90% target
    [InlineData(450, 50, 95, false)]    // Doesn't meet 95% target
    [InlineData(500, 0, 100, true)]     // Meets 100% target
    public void MeetsTarget_WithVariousTargets_ShouldReturnCorrectResult(
        decimal goodPieces, decimal defectivePieces, decimal target, bool expectedResult)
    {
        // Arrange
        var quality = new Quality(goodPieces, defectivePieces);

        // Act & Assert
        quality.MeetsTarget(target).Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(450, 50, 5, true)]      // 10% defect rate > 5% threshold
    [InlineData(450, 50, 15, false)]    // 10% defect rate < 15% threshold
    [InlineData(500, 0, 5, false)]      // No defects
    public void RequiresQualityAlert_WithVariousThresholds_ShouldReturnCorrectResult(
        decimal goodPieces, decimal defectivePieces, decimal threshold, bool expectedResult)
    {
        // Arrange
        var quality = new Quality(goodPieces, defectivePieces);

        // Act & Assert
        quality.RequiresQualityAlert(threshold).Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(500, 0, "Perfect - Zero defects")]
    [InlineData(9999, 1, "Excellent - World class quality")]       // 0.01% defect rate
    [InlineData(995, 5, "Good - High quality production")]         // 0.5% defect rate
    [InlineData(980, 20, "Acceptable - Standard quality")]         // 2% defect rate
    [InlineData(960, 40, "Marginal - Quality improvement needed")] // 4% defect rate
    [InlineData(900, 100, "Poor - Immediate quality intervention required")] // 10% defect rate
    public void GetQualityLevel_WithVariousDefectRates_ShouldReturnCorrectLevel(
        decimal goodPieces, decimal defectivePieces, string expectedLevel)
    {
        // Arrange
        var quality = new Quality(goodPieces, defectivePieces);

        // Act
        var result = quality.GetQualityLevel();

        // Assert
        result.Should().Be(expectedLevel);
    }

    [Fact]
    public void CalculateCostImpact_ShouldCalculateCorrectly()
    {
        // Arrange
        var quality = new Quality(450, 50);
        var costPerDefect = 25m;

        // Act
        var result = quality.CalculateCostImpact(costPerDefect);

        // Assert
        result.Should().Be(1250m); // 50 * 25
    }

    [Fact]
    public void GetBreakdown_ShouldReturnCorrectComponents()
    {
        // Arrange
        var quality = new Quality(450, 50);

        // Act
        var breakdown = quality.GetBreakdown();

        // Assert
        breakdown.Good.Should().Be(450);
        breakdown.Defective.Should().Be(50);
        breakdown.Total.Should().Be(500);
        breakdown.YieldRate.Should().Be(90);
        breakdown.DefectRate.Should().Be(10);
        breakdown.DPMO.Should().Be(100000);
    }

    [Theory]
    [InlineData(80, 85, 90, true)]   // Quality is lowest
    [InlineData(90, 80, 85, false)]  // Availability is lowest
    [InlineData(90, 85, 80, false)]  // Performance is lowest
    [InlineData(80, 80, 80, false)]  // All equal
    public void IsConstrainingFactor_WithVariousFactors_ShouldReturnCorrectResult(
        decimal qualityPercent, decimal availabilityPercent, decimal performancePercent, bool expectedResult)
    {
        // Arrange
        var goodPieces = qualityPercent * 10; // Total of 1000 pieces assumed
        var defectivePieces = (100 - qualityPercent) * 10;
        var quality = new Quality(goodPieces, defectivePieces);

        // Act & Assert
        quality.IsConstrainingFactor(availabilityPercent, performancePercent).Should().Be(expectedResult);
    }

    [Fact]
    public void FromCounterChannels_ShouldCreateCorrectQuality()
    {
        // Arrange
        var channelGood = 450m;
        var channelRejects = 50m;

        // Act
        var quality = Quality.FromCounterChannels(channelGood, channelRejects);

        // Assert
        quality.GoodPieces.Should().Be(channelGood);
        quality.DefectivePieces.Should().Be(channelRejects);
        quality.TotalPiecesProduced.Should().Be(500);
        quality.Percentage.Should().Be(90);
    }

    [Fact]
    public void Equals_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var quality1 = new Quality(450, 50, 500);
        var quality2 = new Quality(450, 50, 500);

        // Act & Assert
        quality1.Equals(quality2).Should().BeTrue();
        (quality1 == quality2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentValues_ShouldReturnFalse()
    {
        // Arrange
        var quality1 = new Quality(450, 50);
        var quality2 = new Quality(400, 50);

        // Act & Assert
        quality1.Equals(quality2).Should().BeFalse();
        (quality1 != quality2).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_WithSameValues_ShouldReturnSameHash()
    {
        // Arrange
        var quality1 = new Quality(450, 50);
        var quality2 = new Quality(450, 50);

        // Act & Assert
        quality1.GetHashCode().Should().Be(quality2.GetHashCode());
    }

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var quality = new Quality(450, 50);

        // Act
        var result = quality.ToString();

        // Assert
        result.Should().Be("Quality: 90.0% (450/500 good, 50 defects)");
    }

    [Fact]
    public void QualityBreakdown_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var breakdown = new QualityBreakdown(450, 50, 500, 90, 10, 100000);

        // Assert
        breakdown.Good.Should().Be(450);
        breakdown.Defective.Should().Be(50);
        breakdown.Total.Should().Be(500);
        breakdown.YieldRate.Should().Be(90);
        breakdown.DefectRate.Should().Be(10);
        breakdown.DPMO.Should().Be(100000);
    }
}
