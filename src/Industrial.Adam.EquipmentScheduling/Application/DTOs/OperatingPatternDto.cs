using System.Text.Json;
using Industrial.Adam.EquipmentScheduling.Domain.Enums;

namespace Industrial.Adam.EquipmentScheduling.Application.DTOs;

/// <summary>
/// Data transfer object for operating pattern information
/// </summary>
public sealed record OperatingPatternDto
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public PatternType Type { get; init; }
    public int CycleDays { get; init; }
    public decimal WeeklyHours { get; init; }
    public JsonDocument Configuration { get; init; } = JsonDocument.Parse("{}");
    public bool IsVisible { get; init; }
    public string? Description { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// Data transfer object for creating an operating pattern
/// </summary>
public sealed record CreateOperatingPatternDto
{
    public required string Name { get; init; }
    public PatternType Type { get; init; }
    public int CycleDays { get; init; }
    public decimal WeeklyHours { get; init; }
    public JsonDocument Configuration { get; init; } = JsonDocument.Parse("{}");
    public string? Description { get; init; }
}

/// <summary>
/// Data transfer object for updating an operating pattern
/// </summary>
public sealed record UpdateOperatingPatternDto
{
    public required string Name { get; init; }
    public int CycleDays { get; init; }
    public decimal WeeklyHours { get; init; }
    public JsonDocument Configuration { get; init; } = JsonDocument.Parse("{}");
    public string? Description { get; init; }
}

/// <summary>
/// Data transfer object for pattern availability information
/// </summary>
public sealed record PatternAvailabilityDto
{
    public int PatternId { get; init; }
    public required string PatternName { get; init; }
    public PatternType Type { get; init; }
    public decimal WeeklyHours { get; init; }
    public Dictionary<DayOfWeek, decimal> DailyHours { get; init; } = new();
    public List<ShiftInfoDto> Shifts { get; init; } = [];
}

/// <summary>
/// Data transfer object for shift information
/// </summary>
public sealed record ShiftInfoDto
{
    public required string ShiftCode { get; init; }
    public TimeSpan StartTime { get; init; }
    public TimeSpan EndTime { get; init; }
    public decimal PlannedHours { get; init; }
    public List<DayOfWeek> OperatingDays { get; init; } = [];
}
