using Industrial.Adam.EquipmentScheduling.Application.DTOs;
using MediatR;

namespace Industrial.Adam.EquipmentScheduling.Application.Commands;

/// <summary>
/// Command to generate schedules for a resource
/// </summary>
public sealed record GenerateSchedulesCommand(
    long ResourceId,
    DateTime StartDate,
    DateTime EndDate) : IRequest<IEnumerable<EquipmentScheduleDto>>;

/// <summary>
/// Command to regenerate schedules for a resource
/// </summary>
public sealed record RegenerateSchedulesCommand(
    long ResourceId,
    DateTime StartDate,
    DateTime EndDate) : IRequest<IEnumerable<EquipmentScheduleDto>>;

/// <summary>
/// Command to create an exception schedule
/// </summary>
public sealed record CreateExceptionScheduleCommand(
    long ResourceId,
    DateTime Date,
    DateTime StartTime,
    DateTime EndTime,
    string Reason) : IRequest<EquipmentScheduleDto>;

/// <summary>
/// Command to update an equipment schedule
/// </summary>
public sealed record UpdateEquipmentScheduleCommand(
    long Id,
    decimal PlannedHours,
    DateTime? PlannedStartTime = null,
    DateTime? PlannedEndTime = null,
    string? ShiftCode = null,
    string? Notes = null) : IRequest<EquipmentScheduleDto>;

/// <summary>
/// Command to cancel an equipment schedule
/// </summary>
public sealed record CancelEquipmentScheduleCommand(
    long ScheduleId,
    string? Reason = null) : IRequest<Unit>;

/// <summary>
/// Command to complete an equipment schedule
/// </summary>
public sealed record CompleteEquipmentScheduleCommand(
    long ScheduleId) : IRequest<Unit>;
