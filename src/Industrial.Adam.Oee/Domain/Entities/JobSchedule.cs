using Industrial.Adam.Oee.Domain.Interfaces;
using Industrial.Adam.Oee.Domain.ValueObjects;

namespace Industrial.Adam.Oee.Domain.Entities;

/// <summary>
/// Job Schedule Aggregate Root
/// 
/// Represents advanced job scheduling with priority management, dependency tracking,
/// queue handling, and conflict resolution. Provides optimization algorithms
/// and scheduling intelligence for production planning.
/// </summary>
public sealed class JobSchedule : Entity<string>, IAggregateRoot
{
    /// <summary>
    /// Work order reference being scheduled
    /// (immutable)
    /// </summary>
    public CanonicalReference WorkOrderReference { get; private set; }

    /// <summary>
    /// Equipment line reference where job will run
    /// (immutable)
    /// </summary>
    public CanonicalReference EquipmentLineReference { get; private set; }

    /// <summary>
    /// Scheduled start time
    /// (immutable initially, can be updated)
    /// </summary>
    public DateTime ScheduledStartTime { get; private set; }

    /// <summary>
    /// Scheduled end time
    /// (immutable initially, can be updated)
    /// </summary>
    public DateTime ScheduledEndTime { get; private set; }

    /// <summary>
    /// Job priority (1 = highest, 10 = lowest)
    /// (can be updated)
    /// </summary>
    public int Priority { get; private set; }

    /// <summary>
    /// Current scheduling status
    /// </summary>
    public JobScheduleStatus Status { get; private set; }

    /// <summary>
    /// Setup time required before job can start (in minutes)
    /// (immutable)
    /// </summary>
    public decimal SetupTimeMinutes { get; private set; }

    /// <summary>
    /// Teardown time required after job completion (in minutes)
    /// (immutable)
    /// </summary>
    public decimal TeardownTimeMinutes { get; private set; }

    /// <summary>
    /// Estimated production time (in minutes)
    /// (immutable)
    /// </summary>
    public decimal EstimatedProductionTimeMinutes { get; private set; }

    /// <summary>
    /// Job dependencies that must complete before this job can start
    /// </summary>
    private readonly List<JobDependency> _dependencies = new();

    /// <summary>
    /// Resource requirements for this job
    /// </summary>
    private readonly List<ResourceRequirement> _resourceRequirements = new();

    /// <summary>
    /// Scheduling constraints and rules
    /// </summary>
    private readonly List<SchedulingConstraint> _constraints = new();

    /// <summary>
    /// Scheduling conflicts detected for this job
    /// </summary>
    private readonly List<SchedulingConflict> _conflicts = new();

    /// <summary>
    /// Optimization hints for scheduler algorithms
    /// </summary>
    private JobOptimizationHints? _optimizationHints;

    /// <summary>
    /// Read-only access to dependencies
    /// </summary>
    public IReadOnlyList<JobDependency> Dependencies => _dependencies.AsReadOnly();

    /// <summary>
    /// Read-only access to resource requirements
    /// </summary>
    public IReadOnlyList<ResourceRequirement> ResourceRequirements => _resourceRequirements.AsReadOnly();

    /// <summary>
    /// Read-only access to constraints
    /// </summary>
    public IReadOnlyList<SchedulingConstraint> Constraints => _constraints.AsReadOnly();

    /// <summary>
    /// Read-only access to conflicts
    /// </summary>
    public IReadOnlyList<SchedulingConflict> Conflicts => _conflicts.AsReadOnly();

    /// <summary>
    /// Optimization hints for scheduling algorithms
    /// </summary>
    public JobOptimizationHints? OptimizationHints => _optimizationHints;

    /// <summary>
    /// Sequence number within equipment line queue
    /// </summary>
    public int QueueSequence { get; private set; }

    /// <summary>
    /// When this job was scheduled
    /// (immutable)
    /// </summary>
    public DateTime ScheduledAt { get; private set; }

    /// <summary>
    /// Who scheduled this job
    /// (immutable)
    /// </summary>
    public string ScheduledBy { get; private set; }

    /// <summary>
    /// When this schedule becomes effective
    /// (immutable)
    /// </summary>
    public DateTime EffectiveFromDate { get; private set; }

    /// <summary>
    /// When this schedule becomes ineffective (null = permanent)
    /// </summary>
    public DateTime? EffectiveToDate { get; private set; }

    /// <summary>
    /// When this schedule was created
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Default constructor for serialization
    /// </summary>
    private JobSchedule() : base()
    {
        WorkOrderReference = CanonicalReference.ToWorkOrder("unknown");
        EquipmentLineReference = CanonicalReference.ToResource("unknown");
        Status = JobScheduleStatus.Planned;
        ScheduledBy = string.Empty;
    }

    /// <summary>
    /// Creates a new job schedule
    /// </summary>
    /// <param name="scheduleId">Unique schedule identifier (immutable)</param>
    /// <param name="workOrderReference">Work order reference being scheduled (immutable)</param>
    /// <param name="equipmentLineReference">Equipment line reference for production (immutable)</param>
    /// <param name="scheduledStartTime">Scheduled start time</param>
    /// <param name="scheduledEndTime">Scheduled end time</param>
    /// <param name="priority">Job priority (1-10)</param>
    /// <param name="setupTimeMinutes">Setup time in minutes (immutable)</param>
    /// <param name="teardownTimeMinutes">Teardown time in minutes (immutable)</param>
    /// <param name="estimatedProductionTimeMinutes">Estimated production time (immutable)</param>
    /// <param name="scheduledBy">Who scheduled the job (immutable)</param>
    /// <param name="queueSequence">Sequence in equipment queue</param>
    /// <param name="effectiveFromDate">When schedule becomes effective (immutable)</param>
    /// <param name="effectiveToDate">When schedule becomes ineffective</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public JobSchedule(
        string scheduleId,
        CanonicalReference workOrderReference,
        CanonicalReference equipmentLineReference,
        DateTime scheduledStartTime,
        DateTime scheduledEndTime,
        int priority,
        decimal setupTimeMinutes,
        decimal teardownTimeMinutes,
        decimal estimatedProductionTimeMinutes,
        string scheduledBy,
        int queueSequence = 0,
        DateTime? effectiveFromDate = null,
        DateTime? effectiveToDate = null) : base(scheduleId)
    {
        ValidateConstructorParameters(scheduleId, workOrderReference, equipmentLineReference, scheduledStartTime,
            scheduledEndTime, priority, setupTimeMinutes, teardownTimeMinutes,
            estimatedProductionTimeMinutes, scheduledBy);

        WorkOrderReference = workOrderReference;
        EquipmentLineReference = equipmentLineReference;
        ScheduledStartTime = scheduledStartTime;
        ScheduledEndTime = scheduledEndTime;
        Priority = priority;
        SetupTimeMinutes = setupTimeMinutes;
        TeardownTimeMinutes = teardownTimeMinutes;
        EstimatedProductionTimeMinutes = estimatedProductionTimeMinutes;
        ScheduledBy = scheduledBy;
        QueueSequence = queueSequence;

        Status = JobScheduleStatus.Planned;
        ScheduledAt = DateTime.UtcNow;
        EffectiveFromDate = effectiveFromDate ?? DateTime.UtcNow;
        EffectiveToDate = effectiveToDate;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Get total scheduled time including setup and teardown
    /// </summary>
    public decimal TotalScheduledTimeMinutes => SetupTimeMinutes + EstimatedProductionTimeMinutes + TeardownTimeMinutes;

    /// <summary>
    /// Check if schedule is currently effective
    /// </summary>
    public bool IsEffective
    {
        get
        {
            var now = DateTime.UtcNow;
            return now >= EffectiveFromDate && (EffectiveToDate == null || now <= EffectiveToDate);
        }
    }

    /// <summary>
    /// Check if job schedule is confirmed
    /// </summary>
    public bool IsConfirmed => Status == JobScheduleStatus.Confirmed;

    /// <summary>
    /// Check if job schedule is active
    /// </summary>
    public bool IsActive => Status == JobScheduleStatus.Active;

    /// <summary>
    /// Check if job schedule has conflicts
    /// </summary>
    public bool HasConflicts => _conflicts.Any(c => !c.IsResolved);

    /// <summary>
    /// Check if all dependencies are satisfied
    /// </summary>
    public bool AreDependenciesSatisfied => _dependencies.All(d => d.IsSatisfied);

    /// <summary>
    /// Update scheduling times
    /// </summary>
    /// <param name="scheduledStartTime">New scheduled start time</param>
    /// <param name="scheduledEndTime">New scheduled end time</param>
    /// <param name="updatedBy">Who made the update</param>
    /// <exception cref="ArgumentException">Thrown when times are invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when schedule cannot be updated</exception>
    public void UpdateScheduledTimes(DateTime scheduledStartTime, DateTime scheduledEndTime, string updatedBy)
    {
        if (scheduledEndTime <= scheduledStartTime)
            throw new ArgumentException("Scheduled end time must be after start time");

        if (Status == JobScheduleStatus.Completed || Status == JobScheduleStatus.Cancelled)
            throw new InvalidOperationException($"Cannot update schedule with status: {Status}");

        ScheduledStartTime = scheduledStartTime;
        ScheduledEndTime = scheduledEndTime;
        UpdatedAt = DateTime.UtcNow;

        // Clear existing conflicts as times have changed
        _conflicts.Clear();
    }

    /// <summary>
    /// Update job priority
    /// </summary>
    /// <param name="priority">New priority (1-10)</param>
    /// <param name="updatedBy">Who made the update</param>
    /// <exception cref="ArgumentException">Thrown when priority is invalid</exception>
    public void UpdatePriority(int priority, string updatedBy)
    {
        if (priority < 1 || priority > 10)
            throw new ArgumentException("Priority must be between 1 and 10", nameof(priority));

        Priority = priority;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update queue sequence
    /// </summary>
    /// <param name="queueSequence">New queue sequence</param>
    /// <exception cref="ArgumentException">Thrown when sequence is invalid</exception>
    public void UpdateQueueSequence(int queueSequence)
    {
        if (queueSequence < 0)
            throw new ArgumentException("Queue sequence cannot be negative", nameof(queueSequence));

        QueueSequence = queueSequence;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Confirm the job schedule
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when schedule cannot be confirmed</exception>
    public void Confirm()
    {
        JobScheduleStateTransitions.ValidateTransition(Status, JobScheduleStatus.Confirmed);

        if (HasConflicts)
            throw new InvalidOperationException("Cannot confirm schedule with unresolved conflicts");

        Status = JobScheduleStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activate the job schedule (job is starting)
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when schedule cannot be activated</exception>
    public void Activate()
    {
        JobScheduleStateTransitions.ValidateTransition(Status, JobScheduleStatus.Active);

        if (!AreDependenciesSatisfied)
            throw new InvalidOperationException("Cannot activate schedule with unsatisfied dependencies");

        Status = JobScheduleStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Complete the job schedule
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when schedule cannot be completed</exception>
    public void Complete()
    {
        JobScheduleStateTransitions.ValidateTransition(Status, JobScheduleStatus.Completed);

        Status = JobScheduleStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Cancel the job schedule
    /// </summary>
    /// <param name="reason">Cancellation reason</param>
    /// <exception cref="InvalidOperationException">Thrown when schedule cannot be cancelled</exception>
    public void Cancel(string reason)
    {
        JobScheduleStateTransitions.ValidateTransition(Status, JobScheduleStatus.Cancelled);

        Status = JobScheduleStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Add job dependency
    /// </summary>
    /// <param name="dependentJobId">Job that must complete first</param>
    /// <param name="dependencyType">Type of dependency</param>
    /// <param name="description">Dependency description</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public void AddDependency(string dependentJobId, JobDependencyType dependencyType, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(dependentJobId))
            throw new ArgumentException("Dependent job ID is required", nameof(dependentJobId));

        // Check if dependency already exists
        if (_dependencies.Any(d => d.DependentJobId == dependentJobId && d.DependencyType == dependencyType))
            throw new InvalidOperationException($"Dependency on job {dependentJobId} already exists");

        var dependency = new JobDependency(
            dependentJobId,
            dependencyType,
            description ?? $"{dependencyType} dependency on job {dependentJobId}",
            false
        );

        _dependencies.Add(dependency);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Mark dependency as satisfied
    /// </summary>
    /// <param name="dependentJobId">Job that completed</param>
    /// <param name="dependencyType">Type of dependency</param>
    /// <exception cref="ArgumentException">Thrown when dependency not found</exception>
    public void SatisfyDependency(string dependentJobId, JobDependencyType dependencyType)
    {
        var dependency = _dependencies.FirstOrDefault(d =>
            d.DependentJobId == dependentJobId && d.DependencyType == dependencyType);

        if (dependency == null)
            throw new ArgumentException($"Dependency on job {dependentJobId} not found");

        var index = _dependencies.IndexOf(dependency);
        _dependencies[index] = dependency with { IsSatisfied = true };
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Add resource requirement
    /// </summary>
    /// <param name="resourceType">Type of resource</param>
    /// <param name="resourceId">Resource identifier</param>
    /// <param name="quantityRequired">Quantity required</param>
    /// <param name="isOptional">Whether resource is optional</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public void AddResourceRequirement(string resourceType, string resourceId, decimal quantityRequired, bool isOptional = false)
    {
        if (string.IsNullOrWhiteSpace(resourceType))
            throw new ArgumentException("Resource type is required", nameof(resourceType));

        if (string.IsNullOrWhiteSpace(resourceId))
            throw new ArgumentException("Resource ID is required", nameof(resourceId));

        if (quantityRequired <= 0)
            throw new ArgumentException("Quantity required must be positive", nameof(quantityRequired));

        var requirement = new ResourceRequirement(
            resourceType,
            resourceId,
            quantityRequired,
            isOptional
        );

        _resourceRequirements.Add(requirement);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Add scheduling constraint
    /// </summary>
    /// <param name="constraintType">Type of constraint</param>
    /// <param name="description">Constraint description</param>
    /// <param name="value">Constraint value</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public void AddConstraint(SchedulingConstraintType constraintType, string description, string? value = null)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Constraint description is required", nameof(description));

        var constraint = new SchedulingConstraint(
            constraintType,
            description,
            value
        );

        _constraints.Add(constraint);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Add scheduling conflict
    /// </summary>
    /// <param name="conflictType">Type of conflict</param>
    /// <param name="description">Conflict description</param>
    /// <param name="conflictingJobId">Conflicting job identifier</param>
    /// <param name="severity">Conflict severity</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public void AddConflict(SchedulingConflictType conflictType, string description, string? conflictingJobId = null, ConflictSeverity severity = ConflictSeverity.Medium)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Conflict description is required", nameof(description));

        var conflict = new SchedulingConflict(
            conflictType,
            description,
            conflictingJobId,
            severity,
            DateTime.UtcNow,
            false
        );

        _conflicts.Add(conflict);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Resolve scheduling conflict
    /// </summary>
    /// <param name="conflictType">Type of conflict to resolve</param>
    /// <param name="conflictingJobId">Conflicting job identifier</param>
    /// <exception cref="ArgumentException">Thrown when conflict not found</exception>
    public void ResolveConflict(SchedulingConflictType conflictType, string? conflictingJobId = null)
    {
        var conflict = _conflicts.FirstOrDefault(c =>
            c.ConflictType == conflictType &&
            c.ConflictingJobId == conflictingJobId &&
            !c.IsResolved);

        if (conflict == null)
            throw new ArgumentException("Conflict not found");

        var index = _conflicts.IndexOf(conflict);
        _conflicts[index] = conflict with { IsResolved = true };
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Set optimization hints
    /// </summary>
    /// <param name="hints">Optimization hints</param>
    public void SetOptimizationHints(JobOptimizationHints hints)
    {
        _optimizationHints = hints;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Calculate scheduling score for optimization algorithms
    /// </summary>
    /// <returns>Scheduling score (higher is better)</returns>
    public decimal CalculateSchedulingScore()
    {
        decimal score = 100;

        // Priority contributes to score (higher priority = higher score)
        score += (11 - Priority) * 10;

        // Penalty for conflicts
        score -= _conflicts.Count(c => !c.IsResolved) * 15;

        // Penalty for unsatisfied dependencies
        score -= _dependencies.Count(d => !d.IsSatisfied) * 10;

        // Bonus for shorter setup times
        score += Math.Max(0, 60 - SetupTimeMinutes);

        return Math.Max(0, score);
    }

    /// <summary>
    /// Check if job can start at specified time
    /// </summary>
    /// <param name="proposedStartTime">Proposed start time</param>
    /// <returns>True if job can start at proposed time</returns>
    public bool CanStartAt(DateTime proposedStartTime)
    {
        // Check if all dependencies are satisfied
        if (!AreDependenciesSatisfied)
            return false;

        // Check constraints
        foreach (var constraint in _constraints)
        {
            if (constraint.ConstraintType == SchedulingConstraintType.NoStartBefore)
            {
                if (DateTime.TryParse(constraint.Value, out var noStartBefore) && proposedStartTime < noStartBefore)
                    return false;
            }
            else if (constraint.ConstraintType == SchedulingConstraintType.NoStartAfter)
            {
                if (DateTime.TryParse(constraint.Value, out var noStartAfter) && proposedStartTime > noStartAfter)
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Get job schedule summary
    /// </summary>
    /// <returns>Job schedule summary</returns>
    public JobScheduleSummary ToSummary()
    {
        return new JobScheduleSummary(
            Id,
            WorkOrderReference,
            EquipmentLineReference,
            Status.ToString(),
            ScheduledStartTime,
            ScheduledEndTime,
            Priority,
            QueueSequence,
            TotalScheduledTimeMinutes,
            _dependencies.Count,
            _dependencies.Count(d => d.IsSatisfied),
            _resourceRequirements.Count,
            _constraints.Count,
            _conflicts.Count,
            _conflicts.Count(c => !c.IsResolved),
            CalculateSchedulingScore(),
            ScheduledBy,
            ScheduledAt
        );
    }

    /// <summary>
    /// Validate constructor parameters
    /// </summary>
    private static void ValidateConstructorParameters(
        string scheduleId,
        CanonicalReference workOrderReference,
        CanonicalReference equipmentLineReference,
        DateTime scheduledStartTime,
        DateTime scheduledEndTime,
        int priority,
        decimal setupTimeMinutes,
        decimal teardownTimeMinutes,
        decimal estimatedProductionTimeMinutes,
        string scheduledBy)
    {
        if (string.IsNullOrWhiteSpace(scheduleId))
            throw new ArgumentException("Schedule ID is required", nameof(scheduleId));

        if (workOrderReference == null)
            throw new ArgumentNullException(nameof(workOrderReference));

        if (!workOrderReference.IsWorkOrder)
            throw new ArgumentException("Reference must be to a work order", nameof(workOrderReference));

        if (equipmentLineReference == null)
            throw new ArgumentNullException(nameof(equipmentLineReference));

        if (!equipmentLineReference.IsResource)
            throw new ArgumentException("Reference must be to a resource", nameof(equipmentLineReference));

        if (scheduledEndTime <= scheduledStartTime)
            throw new ArgumentException("Scheduled end time must be after start time", nameof(scheduledEndTime));

        if (priority < 1 || priority > 10)
            throw new ArgumentException("Priority must be between 1 and 10", nameof(priority));

        if (setupTimeMinutes < 0)
            throw new ArgumentException("Setup time cannot be negative", nameof(setupTimeMinutes));

        if (teardownTimeMinutes < 0)
            throw new ArgumentException("Teardown time cannot be negative", nameof(teardownTimeMinutes));

        if (estimatedProductionTimeMinutes <= 0)
            throw new ArgumentException("Estimated production time must be positive", nameof(estimatedProductionTimeMinutes));

        if (string.IsNullOrWhiteSpace(scheduledBy))
            throw new ArgumentException("Scheduled by is required", nameof(scheduledBy));
    }

    /// <summary>
    /// String representation of the job schedule
    /// </summary>
    /// <returns>Formatted string representation</returns>
    public override string ToString()
    {
        return $"Job Schedule {Id}: WO {WorkOrderReference.Id} on {EquipmentLineReference.Id} ({ScheduledStartTime:MM/dd HH:mm} - {ScheduledEndTime:HH:mm}, Priority {Priority})";
    }
}

/// <summary>
/// Job schedule status enumeration
/// </summary>
public enum JobScheduleStatus
{
    /// <summary>
    /// Schedule is planned but not confirmed
    /// </summary>
    Planned,

    /// <summary>
    /// Schedule is confirmed and ready
    /// </summary>
    Confirmed,

    /// <summary>
    /// Job is currently running
    /// </summary>
    Active,

    /// <summary>
    /// Job has been completed
    /// </summary>
    Completed,

    /// <summary>
    /// Schedule has been cancelled
    /// </summary>
    Cancelled
}

/// <summary>
/// Job dependency type enumeration
/// </summary>
public enum JobDependencyType
{
    /// <summary>
    /// Job must complete before this job can start
    /// </summary>
    FinishToStart,

    /// <summary>
    /// Job must start before this job can start
    /// </summary>
    StartToStart,

    /// <summary>
    /// Job must complete before this job can complete
    /// </summary>
    FinishToFinish,

    /// <summary>
    /// Job must start before this job can complete
    /// </summary>
    StartToFinish,

    /// <summary>
    /// Resource dependency (shared resource)
    /// </summary>
    Resource,

    /// <summary>
    /// Material dependency (material from previous job)
    /// </summary>
    Material
}

/// <summary>
/// Scheduling constraint type enumeration
/// </summary>
public enum SchedulingConstraintType
{
    /// <summary>
    /// Job cannot start before specified time
    /// </summary>
    NoStartBefore,

    /// <summary>
    /// Job cannot start after specified time
    /// </summary>
    NoStartAfter,

    /// <summary>
    /// Job must complete by specified time
    /// </summary>
    MustCompleteBy,

    /// <summary>
    /// Job requires specific operator
    /// </summary>
    RequiredOperator,

    /// <summary>
    /// Job requires specific shift
    /// </summary>
    RequiredShift,

    /// <summary>
    /// Job has maintenance window constraint
    /// </summary>
    MaintenanceWindow,

    /// <summary>
    /// Job has quality checkpoint constraint
    /// </summary>
    QualityCheckpoint
}

/// <summary>
/// Scheduling conflict type enumeration
/// </summary>
public enum SchedulingConflictType
{
    /// <summary>
    /// Time overlap with another job
    /// </summary>
    TimeOverlap,

    /// <summary>
    /// Resource contention
    /// </summary>
    ResourceContention,

    /// <summary>
    /// Dependency violation
    /// </summary>
    DependencyViolation,

    /// <summary>
    /// Constraint violation
    /// </summary>
    ConstraintViolation,

    /// <summary>
    /// Capacity exceeded
    /// </summary>
    CapacityExceeded,

    /// <summary>
    /// Operator unavailable
    /// </summary>
    OperatorUnavailable
}

/// <summary>
/// Conflict severity enumeration
/// </summary>
public enum ConflictSeverity
{
    /// <summary>
    /// Low severity conflict
    /// </summary>
    Low,

    /// <summary>
    /// Medium severity conflict
    /// </summary>
    Medium,

    /// <summary>
    /// High severity conflict
    /// </summary>
    High,

    /// <summary>
    /// Critical conflict that must be resolved
    /// </summary>
    Critical
}

/// <summary>
/// Job dependency record
/// </summary>
/// <param name="DependentJobId">Job that must satisfy dependency</param>
/// <param name="DependencyType">Type of dependency</param>
/// <param name="Description">Dependency description</param>
/// <param name="IsSatisfied">Whether dependency is satisfied</param>
public record JobDependency(
    string DependentJobId,
    JobDependencyType DependencyType,
    string Description,
    bool IsSatisfied
);

/// <summary>
/// Resource requirement record
/// </summary>
/// <param name="ResourceType">Type of resource</param>
/// <param name="ResourceId">Resource identifier</param>
/// <param name="QuantityRequired">Quantity required</param>
/// <param name="IsOptional">Whether resource is optional</param>
public record ResourceRequirement(
    string ResourceType,
    string ResourceId,
    decimal QuantityRequired,
    bool IsOptional
);

/// <summary>
/// Scheduling constraint record
/// </summary>
/// <param name="ConstraintType">Type of constraint</param>
/// <param name="Description">Constraint description</param>
/// <param name="Value">Constraint value</param>
public record SchedulingConstraint(
    SchedulingConstraintType ConstraintType,
    string Description,
    string? Value
);

/// <summary>
/// Scheduling conflict record
/// </summary>
/// <param name="ConflictType">Type of conflict</param>
/// <param name="Description">Conflict description</param>
/// <param name="ConflictingJobId">Conflicting job identifier</param>
/// <param name="Severity">Conflict severity</param>
/// <param name="DetectedAt">When conflict was detected</param>
/// <param name="IsResolved">Whether conflict is resolved</param>
public record SchedulingConflict(
    SchedulingConflictType ConflictType,
    string Description,
    string? ConflictingJobId,
    ConflictSeverity Severity,
    DateTime DetectedAt,
    bool IsResolved
);

/// <summary>
/// Job optimization hints for scheduling algorithms
/// </summary>
/// <param name="PreferredStartTime">Preferred start time</param>
/// <param name="MaxDelayMinutes">Maximum acceptable delay</param>
/// <param name="CanSplit">Whether job can be split</param>
/// <param name="PreferredOperatorIds">Preferred operators</param>
/// <param name="OptimizationGoal">Primary optimization goal</param>
/// <param name="FlexibilityScore">Job flexibility score (0-100)</param>
public record JobOptimizationHints(
    DateTime? PreferredStartTime,
    decimal MaxDelayMinutes,
    bool CanSplit,
    List<string> PreferredOperatorIds,
    OptimizationGoal OptimizationGoal,
    decimal FlexibilityScore
);

/// <summary>
/// Optimization goal enumeration
/// </summary>
public enum OptimizationGoal
{
    /// <summary>
    /// Minimize makespan (total completion time)
    /// </summary>
    MinimizeMakespan,

    /// <summary>
    /// Minimize setup times
    /// </summary>
    MinimizeSetupTime,

    /// <summary>
    /// Maximize throughput
    /// </summary>
    MaximizeThroughput,

    /// <summary>
    /// Minimize tardiness
    /// </summary>
    MinimizeTardiness,

    /// <summary>
    /// Balance workload
    /// </summary>
    BalanceWorkload,

    /// <summary>
    /// Maximize resource utilization
    /// </summary>
    MaximizeUtilization
}

/// <summary>
/// Job schedule summary for reporting
/// </summary>
/// <param name="ScheduleId">Schedule identifier</param>
/// <param name="WorkOrderReference">Work order reference</param>
/// <param name="EquipmentLineReference">Equipment line reference</param>
/// <param name="Status">Current status</param>
/// <param name="ScheduledStartTime">Scheduled start time</param>
/// <param name="ScheduledEndTime">Scheduled end time</param>
/// <param name="Priority">Job priority</param>
/// <param name="QueueSequence">Queue sequence</param>
/// <param name="TotalTimeMinutes">Total scheduled time</param>
/// <param name="DependencyCount">Number of dependencies</param>
/// <param name="SatisfiedDependencyCount">Number of satisfied dependencies</param>
/// <param name="ResourceRequirementCount">Number of resource requirements</param>
/// <param name="ConstraintCount">Number of constraints</param>
/// <param name="ConflictCount">Number of conflicts</param>
/// <param name="UnresolvedConflictCount">Number of unresolved conflicts</param>
/// <param name="SchedulingScore">Scheduling optimization score</param>
/// <param name="ScheduledBy">Who scheduled the job</param>
/// <param name="ScheduledAt">When job was scheduled</param>
public record JobScheduleSummary(
    string ScheduleId,
    CanonicalReference WorkOrderReference,
    CanonicalReference EquipmentLineReference,
    string Status,
    DateTime ScheduledStartTime,
    DateTime ScheduledEndTime,
    int Priority,
    int QueueSequence,
    decimal TotalTimeMinutes,
    int DependencyCount,
    int SatisfiedDependencyCount,
    int ResourceRequirementCount,
    int ConstraintCount,
    int ConflictCount,
    int UnresolvedConflictCount,
    decimal SchedulingScore,
    string ScheduledBy,
    DateTime ScheduledAt
);

/// <summary>
/// Job schedule creation data
/// </summary>
/// <param name="ScheduleId">Unique schedule identifier (immutable)</param>
/// <param name="WorkOrderReference">Work order reference being scheduled (immutable)</param>
/// <param name="EquipmentLineReference">Equipment line reference for production (immutable)</param>
/// <param name="ScheduledStartTime">Scheduled start time</param>
/// <param name="ScheduledEndTime">Scheduled end time</param>
/// <param name="Priority">Job priority (1-10)</param>
/// <param name="SetupTimeMinutes">Setup time in minutes (immutable)</param>
/// <param name="TeardownTimeMinutes">Teardown time in minutes (immutable)</param>
/// <param name="EstimatedProductionTimeMinutes">Estimated production time (immutable)</param>
/// <param name="ScheduledBy">Who scheduled the job (immutable)</param>
/// <param name="QueueSequence">Sequence in equipment queue</param>
/// <param name="EffectiveFromDate">When schedule becomes effective (immutable)</param>
/// <param name="EffectiveToDate">When schedule becomes ineffective</param>
public record JobScheduleCreationData(
    string ScheduleId,
    CanonicalReference WorkOrderReference,
    CanonicalReference EquipmentLineReference,
    DateTime ScheduledStartTime,
    DateTime ScheduledEndTime,
    int Priority,
    decimal SetupTimeMinutes,
    decimal TeardownTimeMinutes,
    decimal EstimatedProductionTimeMinutes,
    string ScheduledBy,
    int QueueSequence = 0,
    DateTime? EffectiveFromDate = null,
    DateTime? EffectiveToDate = null
);
