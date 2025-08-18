using FluentAssertions;
using Industrial.Adam.Oee.Domain.ValueObjects;
using Xunit;

namespace Industrial.Adam.Oee.Tests.Domain.ValueObjects;

/// <summary>
/// Unit tests for the Availability value object
/// </summary>
public sealed class AvailabilityTests
{
    [Fact]
    public void Constructor_WithValidInputs_ShouldCreateAvailability()
    {
        // Arrange
        var plannedTime = 480m; // 8 hours
        var actualRunTime = 420m; // 7 hours

        // Act
        var availability = new Availability(plannedTime, actualRunTime);

        // Assert
        availability.PlannedTimeMinutes.Should().Be(plannedTime);
        availability.ActualRunTimeMinutes.Should().Be(actualRunTime);
        availability.DowntimeMinutes.Should().Be(60m);
    }

    [Fact]
    public void Constructor_WithExplicitDowntime_ShouldUseProvidedDowntime()
    {
        // Arrange
        var plannedTime = 480m;
        var actualRunTime = 420m;
        var explicitDowntime = 60m;

        // Act
        var availability = new Availability(plannedTime, actualRunTime, explicitDowntime);

        // Assert
        availability.DowntimeMinutes.Should().Be(explicitDowntime);
    }

    [Fact]
    public void Constructor_WithNegativePlannedTime_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        var action = () => new Availability(-100, 50);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*Planned production time cannot be negative*");
    }

    [Fact]
    public void Constructor_WithNegativeActualRunTime_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        var action = () => new Availability(100, -50);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*Actual run time cannot be negative*");
    }

    [Fact]
    public void Constructor_WithActualRunTimeExceedingPlanned_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        var action = () => new Availability(100, 150);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*Actual run time cannot exceed planned production time*");
    }

    [Theory]
    [InlineData(480, 480, 100)]
    [InlineData(480, 240, 50)]
    [InlineData(480, 0, 0)]
    [InlineData(0, 0, 0)]
    public void Percentage_WithVariousInputs_ShouldCalculateCorrectly(
        decimal plannedTime, decimal actualTime, decimal expectedPercentage)
    {
        // Arrange
        var availability = new Availability(plannedTime, actualTime);

        // Act & Assert
        availability.Percentage.Should().Be(expectedPercentage);
    }

    [Theory]
    [InlineData(480, 480, 1.0)]
    [InlineData(480, 240, 0.5)]
    [InlineData(480, 0, 0.0)]
    public void Decimal_WithVariousInputs_ShouldCalculateCorrectly(
        decimal plannedTime, decimal actualTime, decimal expectedDecimal)
    {
        // Arrange
        var availability = new Availability(plannedTime, actualTime);

        // Act & Assert
        availability.Decimal.Should().Be(expectedDecimal);
    }

    [Theory]
    [InlineData(480, 420, 85, true)]
    [InlineData(480, 420, 90, false)]
    [InlineData(480, 480, 100, true)]
    public void MeetsTarget_WithVariousTargets_ShouldReturnCorrectResult(
        decimal plannedTime, decimal actualTime, decimal target, bool expectedResult)
    {
        // Arrange
        var availability = new Availability(plannedTime, actualTime);

        // Act & Assert
        availability.MeetsTarget(target).Should().Be(expectedResult);
    }

    [Fact]
    public void GetDowntimeImpact_ShouldReturnDowntimeMinutes()
    {
        // Arrange
        var availability = new Availability(480, 420);

        // Act & Assert
        availability.GetDowntimeImpact().Should().Be(60);
    }

    [Fact]
    public void GetBreakdown_ShouldReturnCorrectComponents()
    {
        // Arrange
        var availability = new Availability(480, 420);

        // Act
        var breakdown = availability.GetBreakdown();

        // Assert
        breakdown.PlannedTimeMinutes.Should().Be(480);
        breakdown.ActualRunTimeMinutes.Should().Be(420);
        breakdown.DowntimeMinutes.Should().Be(60);
        breakdown.UtilizationRate.Should().Be(87.5m);
    }

    [Theory]
    [InlineData(80, 85, 90, true)]   // Availability is lowest
    [InlineData(90, 85, 80, false)]  // Quality is lowest
    [InlineData(90, 80, 85, false)]  // Performance is lowest
    [InlineData(80, 80, 80, false)]  // All equal
    public void IsConstrainingFactor_WithVariousFactors_ShouldReturnCorrectResult(
        decimal availabilityPercent, decimal performancePercent, decimal qualityPercent, bool expectedResult)
    {
        // Arrange
        var availability = new Availability(100, availabilityPercent);

        // Act & Assert
        availability.IsConstrainingFactor(performancePercent, qualityPercent).Should().Be(expectedResult);
    }

    [Fact]
    public void FromDowntimeRecords_WithMultipleRecords_ShouldCalculateCorrectly()
    {
        // Arrange
        var plannedMinutes = 480m;
        var downtimeRecords = new[]
        {
            new DowntimeRecord(30, DowntimeCategory.Planned),
            new DowntimeRecord(45, DowntimeCategory.Unplanned)
        };

        // Act
        var availability = Availability.FromDowntimeRecords(plannedMinutes, downtimeRecords);

        // Assert
        availability.PlannedTimeMinutes.Should().Be(480);
        availability.ActualRunTimeMinutes.Should().Be(405); // 480 - 75
        availability.DowntimeMinutes.Should().Be(75);
        availability.Percentage.Should().Be(84.375m);
    }

    [Fact]
    public void Equals_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var availability1 = new Availability(480, 420, 60);
        var availability2 = new Availability(480, 420, 60);

        // Act & Assert
        availability1.Equals(availability2).Should().BeTrue();
        (availability1 == availability2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentValues_ShouldReturnFalse()
    {
        // Arrange
        var availability1 = new Availability(480, 420);
        var availability2 = new Availability(480, 400);

        // Act & Assert
        availability1.Equals(availability2).Should().BeFalse();
        (availability1 != availability2).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_WithSameValues_ShouldReturnSameHash()
    {
        // Arrange
        var availability1 = new Availability(480, 420);
        var availability2 = new Availability(480, 420);

        // Act & Assert
        availability1.GetHashCode().Should().Be(availability2.GetHashCode());
    }

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var availability = new Availability(480, 420);

        // Act
        var result = availability.ToString();

        // Assert
        result.Should().Be("Availability: 87.5% (420/480 min)");
    }

    [Fact]
    public void DowntimeRecord_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var record = new DowntimeRecord(30, DowntimeCategory.Planned);

        // Assert
        record.DurationMinutes.Should().Be(30);
        record.Category.Should().Be(DowntimeCategory.Planned);
    }

    [Fact]
    public void AvailabilityBreakdown_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var breakdown = new AvailabilityBreakdown(480, 420, 60, 87.5m);

        // Assert
        breakdown.PlannedTimeMinutes.Should().Be(480);
        breakdown.ActualRunTimeMinutes.Should().Be(420);
        breakdown.DowntimeMinutes.Should().Be(60);
        breakdown.UtilizationRate.Should().Be(87.5m);
    }
}
