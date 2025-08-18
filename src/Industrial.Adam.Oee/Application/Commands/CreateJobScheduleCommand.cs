using Industrial.Adam.Oee.Domain.Entities;
using MediatR;

namespace Industrial.Adam.Oee.Application.Commands;

/// <summary>
/// Command to create a new job schedule
/// </summary>
/// <param name="WorkOrderId">Work order identifier</param>
/// <param name="EquipmentLineId">Equipment line identifier</param>
/// <param name="ScheduledStartTime">Scheduled start time</param>
/// <param name="ScheduledEndTime">Scheduled end time</param>
/// <param name="Priority">Job priority (1-10)</param>
/// <param name="SetupTimeMinutes">Setup time in minutes</param>
/// <param name="TeardownTimeMinutes">Teardown time in minutes</param>
/// <param name="EstimatedProductionTimeMinutes">Estimated production time</param>
/// <param name="ScheduledBy">Who is scheduling the job</param>
/// <param name="QueueSequence">Queue sequence (optional)</param>
/// <param name="Dependencies">Job dependencies (optional)</param>
/// <param name="ResourceRequirements">Resource requirements (optional)</param>
/// <param name="Constraints">Scheduling constraints (optional)</param>
public record CreateJobScheduleCommand(
    string WorkOrderId,
    string EquipmentLineId,
    DateTime ScheduledStartTime,
    DateTime ScheduledEndTime,
    int Priority,
    decimal SetupTimeMinutes,
    decimal TeardownTimeMinutes,
    decimal EstimatedProductionTimeMinutes,
    string ScheduledBy,
    int QueueSequence = 0,
    List<JobDependencyRequest>? Dependencies = null,
    List<ResourceRequirementRequest>? ResourceRequirements = null,
    List<SchedulingConstraintRequest>? Constraints = null
) : IRequest<CreateJobScheduleResult>;

/// <summary>
/// Job dependency request
/// </summary>
/// <param name="DependentJobId">Job that must satisfy dependency</param>
/// <param name="DependencyType">Type of dependency</param>
/// <param name="Description">Dependency description</param>
public record JobDependencyRequest(
    string DependentJobId,
    JobDependencyType DependencyType,
    string Description
);

/// <summary>
/// Resource requirement request
/// </summary>
/// <param name="ResourceType">Type of resource</param>
/// <param name="ResourceId">Resource identifier</param>
/// <param name="QuantityRequired">Quantity required</param>
/// <param name="IsOptional">Whether resource is optional</param>
public record ResourceRequirementRequest(
    string ResourceType,
    string ResourceId,
    decimal QuantityRequired,
    bool IsOptional
);

/// <summary>
/// Scheduling constraint request
/// </summary>
/// <param name="ConstraintType">Type of constraint</param>
/// <param name="Description">Constraint description</param>
/// <param name="Value">Constraint value</param>
public record SchedulingConstraintRequest(
    SchedulingConstraintType ConstraintType,
    string Description,
    string? Value
);

/// <summary>
/// Result of creating a job schedule
/// </summary>
/// <param name="IsSuccess">Whether creation was successful</param>
/// <param name="ScheduleId">Created schedule identifier</param>
/// <param name="DetectedConflicts">Any conflicts detected during scheduling</param>
/// <param name="Warnings">Warnings about the schedule</param>
/// <param name="ErrorMessage">Error message if creation failed</param>
public record CreateJobScheduleResult(
    bool IsSuccess,
    string? ScheduleId,
    List<string> DetectedConflicts,
    List<string> Warnings,
    string? ErrorMessage
);
