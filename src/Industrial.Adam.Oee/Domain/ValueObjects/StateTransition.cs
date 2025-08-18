using Industrial.Adam.Oee.Domain.Entities;

namespace Industrial.Adam.Oee.Domain.ValueObjects;

/// <summary>
/// State Transition Pattern following canonical manufacturing model patterns
/// 
/// Enforces valid state transitions for manufacturing entities based on business rules.
/// Prevents invalid state changes and ensures audit trail compliance.
/// </summary>
/// <typeparam name="TState">State enumeration type</typeparam>
public sealed class StateTransition<TState> where TState : Enum
{
    private readonly Dictionary<TState, HashSet<TState>> _allowedTransitions;
    private readonly Dictionary<(TState from, TState to), string> _transitionReasons;

    /// <summary>
    /// Creates a new state transition validator
    /// </summary>
    public StateTransition()
    {
        _allowedTransitions = new Dictionary<TState, HashSet<TState>>();
        _transitionReasons = new Dictionary<(TState from, TState to), string>();
    }

    /// <summary>
    /// Add allowed transition from one state to another
    /// </summary>
    /// <param name="fromState">Source state</param>
    /// <param name="toState">Target state</param>
    /// <param name="reason">Business reason for transition</param>
    /// <returns>State transition instance for fluent configuration</returns>
    public StateTransition<TState> Allow(TState fromState, TState toState, string reason = "")
    {
        if (!_allowedTransitions.ContainsKey(fromState))
        {
            _allowedTransitions[fromState] = new HashSet<TState>();
        }

        _allowedTransitions[fromState].Add(toState);

        if (!string.IsNullOrEmpty(reason))
        {
            _transitionReasons[(fromState, toState)] = reason;
        }

        return this;
    }

    /// <summary>
    /// Check if transition from one state to another is allowed
    /// </summary>
    /// <param name="fromState">Current state</param>
    /// <param name="toState">Desired state</param>
    /// <returns>True if transition is allowed</returns>
    public bool IsTransitionAllowed(TState fromState, TState toState)
    {
        // Allow transition to same state (no-op)
        if (fromState.Equals(toState))
            return true;

        return _allowedTransitions.ContainsKey(fromState) &&
               _allowedTransitions[fromState].Contains(toState);
    }

    /// <summary>
    /// Validate state transition and throw exception if not allowed
    /// </summary>
    /// <param name="fromState">Current state</param>
    /// <param name="toState">Desired state</param>
    /// <param name="entityType">Type of entity for error message</param>
    /// <exception cref="InvalidOperationException">Thrown when transition is not allowed</exception>
    public void ValidateTransition(TState fromState, TState toState, string entityType = "Entity")
    {
        if (!IsTransitionAllowed(fromState, toState))
        {
            var availableTransitions = GetAvailableTransitions(fromState);
            var availableStates = availableTransitions.Any()
                ? string.Join(", ", availableTransitions)
                : "none";

            throw new InvalidOperationException(
                $"{entityType} cannot transition from {fromState} to {toState}. " +
                $"Available transitions: {availableStates}");
        }
    }

    /// <summary>
    /// Get available transitions from a given state
    /// </summary>
    /// <param name="fromState">Current state</param>
    /// <returns>List of allowed target states</returns>
    public IEnumerable<TState> GetAvailableTransitions(TState fromState)
    {
        return _allowedTransitions.ContainsKey(fromState)
            ? _allowedTransitions[fromState]
            : Enumerable.Empty<TState>();
    }

    /// <summary>
    /// Get transition reason if available
    /// </summary>
    /// <param name="fromState">Source state</param>
    /// <param name="toState">Target state</param>
    /// <returns>Transition reason or empty string</returns>
    public string GetTransitionReason(TState fromState, TState toState)
    {
        return _transitionReasons.TryGetValue((fromState, toState), out var reason) ? reason : "";
    }
}

/// <summary>
/// Batch status state transitions following canonical patterns
/// </summary>
public static class BatchStateTransitions
{
    private static readonly StateTransition<BatchStatus> _transitions = new StateTransition<BatchStatus>()
        .Allow(BatchStatus.Planned, BatchStatus.InProgress, "Batch production started")
        .Allow(BatchStatus.InProgress, BatchStatus.OnHold, "Batch placed on hold")
        .Allow(BatchStatus.InProgress, BatchStatus.Completed, "Batch production completed")
        .Allow(BatchStatus.InProgress, BatchStatus.Cancelled, "Batch cancelled during production")
        .Allow(BatchStatus.OnHold, BatchStatus.InProgress, "Batch resumed from hold")
        .Allow(BatchStatus.OnHold, BatchStatus.Cancelled, "Batch cancelled while on hold")
        .Allow(BatchStatus.Planned, BatchStatus.Cancelled, "Batch cancelled before start");

    /// <summary>
    /// Validate batch status transition
    /// </summary>
    /// <param name="fromStatus">Current status</param>
    /// <param name="toStatus">Desired status</param>
    public static void ValidateTransition(BatchStatus fromStatus, BatchStatus toStatus)
    {
        _transitions.ValidateTransition(fromStatus, toStatus, "Batch");
    }

    /// <summary>
    /// Check if batch status transition is allowed
    /// </summary>
    /// <param name="fromStatus">Current status</param>
    /// <param name="toStatus">Desired status</param>
    /// <returns>True if transition is allowed</returns>
    public static bool IsTransitionAllowed(BatchStatus fromStatus, BatchStatus toStatus)
    {
        return _transitions.IsTransitionAllowed(fromStatus, toStatus);
    }

    /// <summary>
    /// Get available transitions from current batch status
    /// </summary>
    /// <param name="fromStatus">Current status</param>
    /// <returns>Available target states</returns>
    public static IEnumerable<BatchStatus> GetAvailableTransitions(BatchStatus fromStatus)
    {
        return _transitions.GetAvailableTransitions(fromStatus);
    }
}

/// <summary>
/// Shift status state transitions following canonical patterns
/// </summary>
public static class ShiftStateTransitions
{
    private static readonly StateTransition<ShiftStatus> _transitions = new StateTransition<ShiftStatus>()
        .Allow(ShiftStatus.Planned, ShiftStatus.Active, "Shift started")
        .Allow(ShiftStatus.Planned, ShiftStatus.Cancelled, "Shift cancelled before start")
        .Allow(ShiftStatus.Active, ShiftStatus.Completed, "Shift ended normally")
        .Allow(ShiftStatus.Active, ShiftStatus.Cancelled, "Shift cancelled during operation");

    /// <summary>
    /// Validate shift status transition
    /// </summary>
    /// <param name="fromStatus">Current status</param>
    /// <param name="toStatus">Desired status</param>
    public static void ValidateTransition(ShiftStatus fromStatus, ShiftStatus toStatus)
    {
        _transitions.ValidateTransition(fromStatus, toStatus, "Shift");
    }

    /// <summary>
    /// Check if shift status transition is allowed
    /// </summary>
    /// <param name="fromStatus">Current status</param>
    /// <param name="toStatus">Desired status</param>
    /// <returns>True if transition is allowed</returns>
    public static bool IsTransitionAllowed(ShiftStatus fromStatus, ShiftStatus toStatus)
    {
        return _transitions.IsTransitionAllowed(fromStatus, toStatus);
    }

    /// <summary>
    /// Get available transitions from current shift status
    /// </summary>
    /// <param name="fromStatus">Current status</param>
    /// <returns>Available target states</returns>
    public static IEnumerable<ShiftStatus> GetAvailableTransitions(ShiftStatus fromStatus)
    {
        return _transitions.GetAvailableTransitions(fromStatus);
    }
}

/// <summary>
/// Job schedule status state transitions following canonical patterns
/// </summary>
public static class JobScheduleStateTransitions
{
    private static readonly StateTransition<JobScheduleStatus> _transitions = new StateTransition<JobScheduleStatus>()
        .Allow(JobScheduleStatus.Planned, JobScheduleStatus.Confirmed, "Schedule confirmed")
        .Allow(JobScheduleStatus.Planned, JobScheduleStatus.Cancelled, "Schedule cancelled before confirmation")
        .Allow(JobScheduleStatus.Confirmed, JobScheduleStatus.Active, "Job started")
        .Allow(JobScheduleStatus.Confirmed, JobScheduleStatus.Cancelled, "Schedule cancelled after confirmation")
        .Allow(JobScheduleStatus.Active, JobScheduleStatus.Completed, "Job completed")
        .Allow(JobScheduleStatus.Active, JobScheduleStatus.Cancelled, "Job cancelled during execution");

    /// <summary>
    /// Validate job schedule status transition
    /// </summary>
    /// <param name="fromStatus">Current status</param>
    /// <param name="toStatus">Desired status</param>
    public static void ValidateTransition(JobScheduleStatus fromStatus, JobScheduleStatus toStatus)
    {
        _transitions.ValidateTransition(fromStatus, toStatus, "JobSchedule");
    }

    /// <summary>
    /// Check if job schedule status transition is allowed
    /// </summary>
    /// <param name="fromStatus">Current status</param>
    /// <param name="toStatus">Desired status</param>
    /// <returns>True if transition is allowed</returns>
    public static bool IsTransitionAllowed(JobScheduleStatus fromStatus, JobScheduleStatus toStatus)
    {
        return _transitions.IsTransitionAllowed(fromStatus, toStatus);
    }

    /// <summary>
    /// Get available transitions from current job schedule status
    /// </summary>
    /// <param name="fromStatus">Current status</param>
    /// <returns>Available target states</returns>
    public static IEnumerable<JobScheduleStatus> GetAvailableTransitions(JobScheduleStatus fromStatus)
    {
        return _transitions.GetAvailableTransitions(fromStatus);
    }
}

/// <summary>
/// Quality inspection status state transitions following canonical patterns
/// </summary>
public static class QualityInspectionStateTransitions
{
    private static readonly StateTransition<QualityInspectionStatus> _transitions = new StateTransition<QualityInspectionStatus>()
        .Allow(QualityInspectionStatus.Planned, QualityInspectionStatus.InProgress, "Inspection started")
        .Allow(QualityInspectionStatus.InProgress, QualityInspectionStatus.Complete, "Inspection completed")
        .Allow(QualityInspectionStatus.Complete, QualityInspectionStatus.Approved, "Inspection approved");

    /// <summary>
    /// Validate quality inspection status transition
    /// </summary>
    /// <param name="fromStatus">Current status</param>
    /// <param name="toStatus">Desired status</param>
    public static void ValidateTransition(QualityInspectionStatus fromStatus, QualityInspectionStatus toStatus)
    {
        _transitions.ValidateTransition(fromStatus, toStatus, "QualityInspection");
    }

    /// <summary>
    /// Check if quality inspection status transition is allowed
    /// </summary>
    /// <param name="fromStatus">Current status</param>
    /// <param name="toStatus">Desired status</param>
    /// <returns>True if transition is allowed</returns>
    public static bool IsTransitionAllowed(QualityInspectionStatus fromStatus, QualityInspectionStatus toStatus)
    {
        return _transitions.IsTransitionAllowed(fromStatus, toStatus);
    }

    /// <summary>
    /// Get available transitions from current quality inspection status
    /// </summary>
    /// <param name="fromStatus">Current status</param>
    /// <returns>Available target states</returns>
    public static IEnumerable<QualityInspectionStatus> GetAvailableTransitions(QualityInspectionStatus fromStatus)
    {
        return _transitions.GetAvailableTransitions(fromStatus);
    }
}
