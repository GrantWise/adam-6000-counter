using Industrial.Adam.EquipmentScheduling.Domain.Enums;

namespace Industrial.Adam.EquipmentScheduling.Application.DTOs;

/// <summary>
/// Data transfer object for equipment schedule information
/// </summary>
public sealed record EquipmentScheduleDto
{
    public long Id { get; init; }
    public long ResourceId { get; init; }
    public DateTime ScheduleDate { get; init; }
    public string? ShiftCode { get; init; }
    public DateTime? PlannedStartTime { get; init; }
    public DateTime? PlannedEndTime { get; init; }
    public decimal PlannedHours { get; init; }
    public ScheduleStatus ScheduleStatus { get; init; }
    public int? PatternId { get; init; }
    public bool IsException { get; init; }
    public DateTime GeneratedAt { get; init; }
    public string? Notes { get; init; }
    public ResourceDto? Resource { get; init; }
    public OperatingPatternDto? OperatingPattern { get; init; }
}

/// <summary>
/// Data transfer object for creating an equipment schedule
/// </summary>
public sealed record CreateEquipmentScheduleDto
{
    public long ResourceId { get; init; }
    public DateTime ScheduleDate { get; init; }
    public decimal PlannedHours { get; init; }
    public int? PatternId { get; init; }
    public string? ShiftCode { get; init; }
    public DateTime? PlannedStartTime { get; init; }
    public DateTime? PlannedEndTime { get; init; }
    public bool IsException { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// Data transfer object for updating an equipment schedule
/// </summary>
public sealed record UpdateEquipmentScheduleDto
{
    public decimal PlannedHours { get; init; }
    public DateTime? PlannedStartTime { get; init; }
    public DateTime? PlannedEndTime { get; init; }
    public string? ShiftCode { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// Data transfer object for schedule availability summary
/// </summary>
public sealed record ScheduleAvailabilityDto
{
    public long ResourceId { get; init; }
    public required string ResourceName { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public decimal TotalPlannedHours { get; init; }
    public decimal TotalAvailableHours { get; init; }
    public decimal AvailabilityPercentage { get; init; }
    public int ScheduledDays { get; init; }
    public int TotalDays { get; init; }
    public List<EquipmentScheduleDto> Schedules { get; init; } = [];
}

/// <summary>
/// Data transfer object for daily schedule summary
/// </summary>
public sealed record DailyScheduleSummaryDto
{
    public DateTime Date { get; init; }
    public long ResourceId { get; init; }
    public required string ResourceName { get; init; }
    public decimal TotalPlannedHours { get; init; }
    public int ScheduleCount { get; init; }
    public bool HasExceptions { get; init; }
    public List<EquipmentScheduleDto> Schedules { get; init; } = [];
}
