using Industrial.Adam.Oee.Domain.Entities;
using Industrial.Adam.Oee.Domain.ValueObjects;
using Xunit;

namespace Industrial.Adam.Oee.Tests.Domain.ValueObjects;

public class StateTransitionTests
{
    [Fact]
    public void BatchStateTransitions_ValidTransition_ShouldAllow()
    {
        // Arrange
        var fromStatus = BatchStatus.Planned;
        var toStatus = BatchStatus.InProgress;

        // Act
        var isAllowed = BatchStateTransitions.IsTransitionAllowed(fromStatus, toStatus);

        // Assert
        Assert.True(isAllowed);
    }

    [Fact]
    public void BatchStateTransitions_InvalidTransition_ShouldNotAllow()
    {
        // Arrange
        var fromStatus = BatchStatus.Completed;
        var toStatus = BatchStatus.InProgress;

        // Act
        var isAllowed = BatchStateTransitions.IsTransitionAllowed(fromStatus, toStatus);

        // Assert
        Assert.False(isAllowed);
    }

    [Fact]
    public void BatchStateTransitions_ValidateInvalidTransition_ShouldThrowException()
    {
        // Arrange
        var fromStatus = BatchStatus.Completed;
        var toStatus = BatchStatus.InProgress;

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            BatchStateTransitions.ValidateTransition(fromStatus, toStatus));

        Assert.Contains("cannot transition", exception.Message);
        Assert.Contains("Completed", exception.Message);
        Assert.Contains("InProgress", exception.Message);
    }

    [Fact]
    public void BatchStateTransitions_GetAvailableTransitions_FromPlanned_ShouldReturnCorrectStates()
    {
        // Act
        var availableTransitions = BatchStateTransitions.GetAvailableTransitions(BatchStatus.Planned);

        // Assert
        Assert.Contains(BatchStatus.InProgress, availableTransitions);
        Assert.Contains(BatchStatus.Cancelled, availableTransitions);
        Assert.DoesNotContain(BatchStatus.Completed, availableTransitions);
    }

    [Fact]
    public void ShiftStateTransitions_ValidTransition_ShouldAllow()
    {
        // Arrange
        var fromStatus = ShiftStatus.Planned;
        var toStatus = ShiftStatus.Active;

        // Act
        var isAllowed = ShiftStateTransitions.IsTransitionAllowed(fromStatus, toStatus);

        // Assert
        Assert.True(isAllowed);
    }

    [Fact]
    public void ShiftStateTransitions_InvalidTransition_ShouldNotAllow()
    {
        // Arrange
        var fromStatus = ShiftStatus.Completed;
        var toStatus = ShiftStatus.Active;

        // Act
        var isAllowed = ShiftStateTransitions.IsTransitionAllowed(fromStatus, toStatus);

        // Assert
        Assert.False(isAllowed);
    }

    [Fact]
    public void JobScheduleStateTransitions_ValidTransition_ShouldAllow()
    {
        // Arrange
        var fromStatus = JobScheduleStatus.Planned;
        var toStatus = JobScheduleStatus.Confirmed;

        // Act
        var isAllowed = JobScheduleStateTransitions.IsTransitionAllowed(fromStatus, toStatus);

        // Assert
        Assert.True(isAllowed);
    }

    [Fact]
    public void JobScheduleStateTransitions_InvalidTransition_ShouldNotAllow()
    {
        // Arrange
        var fromStatus = JobScheduleStatus.Planned;
        var toStatus = JobScheduleStatus.Active;

        // Act
        var isAllowed = JobScheduleStateTransitions.IsTransitionAllowed(fromStatus, toStatus);

        // Assert
        Assert.False(isAllowed);
    }

    [Fact]
    public void QualityInspectionStateTransitions_ValidTransition_ShouldAllow()
    {
        // Arrange
        var fromStatus = QualityInspectionStatus.Planned;
        var toStatus = QualityInspectionStatus.InProgress;

        // Act
        var isAllowed = QualityInspectionStateTransitions.IsTransitionAllowed(fromStatus, toStatus);

        // Assert
        Assert.True(isAllowed);
    }

    [Fact]
    public void QualityInspectionStateTransitions_InvalidTransition_ShouldNotAllow()
    {
        // Arrange
        var fromStatus = QualityInspectionStatus.Planned;
        var toStatus = QualityInspectionStatus.Approved;

        // Act
        var isAllowed = QualityInspectionStateTransitions.IsTransitionAllowed(fromStatus, toStatus);

        // Assert
        Assert.False(isAllowed);
    }

    [Fact]
    public void StateTransition_SameState_ShouldAlwaysBeAllowed()
    {
        // Arrange
        var status = BatchStatus.InProgress;

        // Act
        var isAllowed = BatchStateTransitions.IsTransitionAllowed(status, status);

        // Assert
        Assert.True(isAllowed);
    }

    [Fact]
    public void StateTransition_GenericImplementation_ShouldWork()
    {
        // Arrange
        var stateTransition = new StateTransition<BatchStatus>()
            .Allow(BatchStatus.Planned, BatchStatus.InProgress, "Start production")
            .Allow(BatchStatus.InProgress, BatchStatus.Completed, "Finish production");

        // Act
        var isAllowed = stateTransition.IsTransitionAllowed(BatchStatus.Planned, BatchStatus.InProgress);
        var isNotAllowed = stateTransition.IsTransitionAllowed(BatchStatus.Planned, BatchStatus.Completed);
        var reason = stateTransition.GetTransitionReason(BatchStatus.Planned, BatchStatus.InProgress);

        // Assert
        Assert.True(isAllowed);
        Assert.False(isNotAllowed);
        Assert.Equal("Start production", reason);
    }

    [Fact]
    public void StateTransition_GetAvailableTransitions_ShouldReturnCorrectStates()
    {
        // Arrange
        var stateTransition = new StateTransition<BatchStatus>()
            .Allow(BatchStatus.Planned, BatchStatus.InProgress)
            .Allow(BatchStatus.Planned, BatchStatus.Cancelled);

        // Act
        var availableTransitions = stateTransition.GetAvailableTransitions(BatchStatus.Planned);

        // Assert
        Assert.Contains(BatchStatus.InProgress, availableTransitions);
        Assert.Contains(BatchStatus.Cancelled, availableTransitions);
        Assert.Equal(2, availableTransitions.Count());
    }
}
