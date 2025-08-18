using FluentAssertions;
using Industrial.Adam.EquipmentScheduling.Domain.Entities;
using Xunit;

namespace Industrial.Adam.EquipmentScheduling.Tests.Domain;

public sealed class PatternAssignmentTests
{
    [Fact]
    public void PatternAssignment_Creation_Should_Set_Properties_Correctly()
    {
        // Arrange
        const long resourceId = 123L;
        const int patternId = 456;
        var effectiveDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);
        const bool isOverride = true;
        const string assignedBy = "Test User";
        const string notes = "Test assignment";

        // Act
        var assignment = new PatternAssignment(
            resourceId,
            patternId,
            effectiveDate,
            endDate,
            isOverride,
            assignedBy,
            notes);

        // Assert
        assignment.ResourceId.Should().Be(resourceId);
        assignment.PatternId.Should().Be(patternId);
        assignment.EffectiveDate.Should().Be(effectiveDate.Date);
        assignment.EndDate.Should().Be(endDate.Date);
        assignment.IsOverride.Should().Be(isOverride);
        assignment.AssignedBy.Should().Be(assignedBy);
        assignment.Notes.Should().Be(notes);
        assignment.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public void PatternAssignment_Creation_With_Invalid_ResourceId_Should_Throw_Exception()
    {
        // Arrange
        const long invalidResourceId = 0;
        const int patternId = 456;
        var effectiveDate = DateTime.Today;

        // Act
        Action act = () => new PatternAssignment(invalidResourceId, patternId, effectiveDate);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Resource ID must be positive*");
    }

    [Fact]
    public void PatternAssignment_Creation_With_Invalid_PatternId_Should_Throw_Exception()
    {
        // Arrange
        const long resourceId = 123L;
        const int invalidPatternId = 0;
        var effectiveDate = DateTime.Today;

        // Act
        Action act = () => new PatternAssignment(resourceId, invalidPatternId, effectiveDate);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Pattern ID must be positive*");
    }

    [Fact]
    public void PatternAssignment_Creation_With_EndDate_Before_EffectiveDate_Should_Throw_Exception()
    {
        // Arrange
        const long resourceId = 123L;
        const int patternId = 456;
        var effectiveDate = new DateTime(2024, 6, 1);
        var invalidEndDate = new DateTime(2024, 1, 1);

        // Act
        Action act = () => new PatternAssignment(resourceId, patternId, effectiveDate, invalidEndDate);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*End date cannot be before effective date*");
    }

    [Fact]
    public void PatternAssignment_UpdateEndDate_Should_Modify_EndDate()
    {
        // Arrange
        var assignment = new PatternAssignment(123L, 456, DateTime.Today);
        var newEndDate = DateTime.Today.AddDays(30);

        // Act
        assignment.UpdateEndDate(newEndDate);

        // Assert
        assignment.EndDate.Should().Be(newEndDate.Date);
        assignment.DomainEvents.Should().HaveCount(2); // Creation + update
    }

    [Fact]
    public void PatternAssignment_UpdateEndDate_With_Date_Before_Effective_Should_Throw_Exception()
    {
        // Arrange
        var effectiveDate = new DateTime(2024, 6, 1);
        var assignment = new PatternAssignment(123L, 456, effectiveDate);
        var invalidEndDate = new DateTime(2024, 1, 1);

        // Act
        Action act = () => assignment.UpdateEndDate(invalidEndDate);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*End date cannot be before effective date*");
    }

    [Fact]
    public void PatternAssignment_UpdateNotes_Should_Modify_Notes()
    {
        // Arrange
        var assignment = new PatternAssignment(123L, 456, DateTime.Today);
        const string newNotes = "Updated notes";
        const string updatedBy = "Another User";

        // Act
        assignment.UpdateNotes(newNotes, updatedBy);

        // Assert
        assignment.Notes.Should().Be(newNotes);
        assignment.AssignedBy.Should().Be(updatedBy);
    }

    [Fact]
    public void PatternAssignment_UpdateNotes_With_Long_Text_Should_Throw_Exception()
    {
        // Arrange
        var assignment = new PatternAssignment(123L, 456, DateTime.Today);
        var longNotes = new string('x', 501); // Exceeds 500 character limit

        // Act
        Action act = () => assignment.UpdateNotes(longNotes);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Notes cannot exceed 500 characters*");
    }

    [Fact]
    public void PatternAssignment_Terminate_Should_Set_EndDate_To_Today()
    {
        // Arrange
        var assignment = new PatternAssignment(123L, 456, DateTime.Today.AddDays(-10));
        const string terminatedBy = "System";

        // Act
        assignment.Terminate(terminatedBy);

        // Assert
        assignment.EndDate.Should().Be(DateTime.UtcNow.Date);
        assignment.AssignedBy.Should().Be(terminatedBy);
        assignment.DomainEvents.Should().HaveCount(2); // Creation + termination
    }

    [Fact]
    public void PatternAssignment_IsActiveOn_Should_Return_True_For_Date_In_Range()
    {
        // Arrange
        var effectiveDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);
        var assignment = new PatternAssignment(123L, 456, effectiveDate, endDate);
        var testDate = new DateTime(2024, 6, 15);

        // Act
        var isActive = assignment.IsActiveOn(testDate);

        // Assert
        isActive.Should().BeTrue();
    }

    [Fact]
    public void PatternAssignment_IsActiveOn_Should_Return_False_For_Date_Before_Effective()
    {
        // Arrange
        var effectiveDate = new DateTime(2024, 6, 1);
        var assignment = new PatternAssignment(123L, 456, effectiveDate);
        var testDate = new DateTime(2024, 1, 1);

        // Act
        var isActive = assignment.IsActiveOn(testDate);

        // Assert
        isActive.Should().BeFalse();
    }

    [Fact]
    public void PatternAssignment_IsActiveOn_Should_Return_False_For_Date_After_End()
    {
        // Arrange
        var effectiveDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 6, 30);
        var assignment = new PatternAssignment(123L, 456, effectiveDate, endDate);
        var testDate = new DateTime(2024, 12, 1);

        // Act
        var isActive = assignment.IsActiveOn(testDate);

        // Assert
        isActive.Should().BeFalse();
    }

    [Fact]
    public void PatternAssignment_OverlapsWith_Should_Return_True_For_Overlapping_Assignments()
    {
        // Arrange
        const long resourceId = 123L;
        var assignment1 = new PatternAssignment(resourceId, 456, new DateTime(2024, 1, 1), new DateTime(2024, 6, 30));
        var assignment2 = new PatternAssignment(resourceId, 789, new DateTime(2024, 6, 1), new DateTime(2024, 12, 31));

        // Act
        var overlaps = assignment1.OverlapsWith(assignment2);

        // Assert
        overlaps.Should().BeTrue();
    }

    [Fact]
    public void PatternAssignment_OverlapsWith_Should_Return_False_For_Different_Resources()
    {
        // Arrange
        var assignment1 = new PatternAssignment(123L, 456, new DateTime(2024, 1, 1), new DateTime(2024, 6, 30));
        var assignment2 = new PatternAssignment(999L, 789, new DateTime(2024, 6, 1), new DateTime(2024, 12, 31));

        // Act
        var overlaps = assignment1.OverlapsWith(assignment2);

        // Assert
        overlaps.Should().BeFalse();
    }

    [Fact]
    public void PatternAssignment_GetDurationDays_Should_Return_Correct_Duration()
    {
        // Arrange
        var effectiveDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 10);
        var assignment = new PatternAssignment(123L, 456, effectiveDate, endDate);

        // Act
        var duration = assignment.GetDurationDays();

        // Assert
        duration.Should().Be(10); // 1st to 10th inclusive = 10 days
    }

    [Fact]
    public void PatternAssignment_GetDurationDays_Should_Return_Null_For_Indefinite_Assignment()
    {
        // Arrange
        var assignment = new PatternAssignment(123L, 456, DateTime.Today);

        // Act
        var duration = assignment.GetDurationDays();

        // Assert
        duration.Should().BeNull();
    }
}
