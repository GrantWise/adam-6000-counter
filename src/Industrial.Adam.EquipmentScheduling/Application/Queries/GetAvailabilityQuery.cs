using Industrial.Adam.EquipmentScheduling.Application.DTOs;
using MediatR;

namespace Industrial.Adam.EquipmentScheduling.Application.Queries;

/// <summary>
/// Query to get equipment availability for a resource and date range
/// </summary>
public sealed record GetEquipmentAvailabilityQuery(
    long ResourceId,
    DateTime StartDate,
    DateTime EndDate) : IRequest<ScheduleAvailabilityDto>;

/// <summary>
/// Query to get equipment schedules for a resource and date range
/// </summary>
public sealed record GetEquipmentSchedulesQuery(
    long ResourceId,
    DateTime StartDate,
    DateTime EndDate) : IRequest<IEnumerable<EquipmentScheduleDto>>;

/// <summary>
/// Query to get daily schedule summary
/// </summary>
public sealed record GetDailyScheduleSummaryQuery(
    long ResourceId,
    DateTime Date) : IRequest<DailyScheduleSummaryDto?>;

/// <summary>
/// Query to get current active schedules
/// </summary>
public sealed record GetCurrentActiveSchedulesQuery(
    long? ResourceId = null) : IRequest<IEnumerable<EquipmentScheduleDto>>;

/// <summary>
/// Query to get schedule conflicts
/// </summary>
public sealed record GetScheduleConflictsQuery(
    long ResourceId,
    DateTime StartDate,
    DateTime EndDate) : IRequest<IEnumerable<string>>;

/// <summary>
/// Query to get missing schedules that need generation
/// </summary>
public sealed record GetMissingSchedulesQuery(
    DateTime StartDate,
    DateTime EndDate,
    long? ResourceId = null) : IRequest<IEnumerable<(long ResourceId, DateTime Date)>>;
