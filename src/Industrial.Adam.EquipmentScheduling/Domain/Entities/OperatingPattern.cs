using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Industrial.Adam.EquipmentScheduling.Domain.Enums;
using Industrial.Adam.EquipmentScheduling.Domain.Events;
using Industrial.Adam.EquipmentScheduling.Domain.Interfaces;
using Industrial.Adam.EquipmentScheduling.Domain.ValueObjects;

namespace Industrial.Adam.EquipmentScheduling.Domain.Entities;

/// <summary>
/// Represents an operating pattern that defines when equipment operates
/// </summary>
public sealed class OperatingPattern : Entity<int>, IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Gets the pattern name
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the pattern type
    /// </summary>
    public PatternType Type { get; private set; }

    /// <summary>
    /// Gets the number of days in the pattern cycle
    /// </summary>
    [Range(1, 365)]
    public int CycleDays { get; private set; }

    /// <summary>
    /// Gets the total weekly hours for this pattern
    /// </summary>
    [Range(0, 168)] // Maximum 168 hours in a week
    public decimal WeeklyHours { get; private set; }

    /// <summary>
    /// Gets the JSON configuration for the pattern
    /// </summary>
    public JsonDocument Configuration { get; private set; } = JsonDocument.Parse("{}");

    /// <summary>
    /// Gets whether this pattern is visible to users
    /// </summary>
    public bool IsVisible { get; private set; } = true;

    /// <summary>
    /// Gets optional description of the pattern
    /// </summary>
    [StringLength(500)]
    public string? Description { get; private set; }

    /// <summary>
    /// Navigation property for pattern assignments
    /// </summary>
    public ICollection<PatternAssignment> Assignments { get; private set; } = [];

    /// <summary>
    /// Gets the domain events for this aggregate root
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    // Required by EF Core
    private OperatingPattern() : base() { }

    /// <summary>
    /// Creates a new operating pattern
    /// </summary>
    /// <param name="name">The pattern name</param>
    /// <param name="type">The pattern type</param>
    /// <param name="cycleDays">The number of days in the cycle</param>
    /// <param name="weeklyHours">The weekly hours</param>
    /// <param name="configuration">The pattern configuration as JSON</param>
    /// <param name="description">Optional description</param>
    public OperatingPattern(
        string name,
        PatternType type,
        int cycleDays,
        decimal weeklyHours,
        JsonDocument configuration,
        string? description = null) : base()
    {
        ValidatePatternCreation(name, type, cycleDays, weeklyHours, configuration);

        Name = name.Trim();
        Type = type;
        CycleDays = cycleDays;
        WeeklyHours = weeklyHours;
        Configuration = configuration;
        Description = description?.Trim();

        AddDomainEvent(new OperatingPatternCreatedEvent(Id, Name, Type, WeeklyHours));
    }

    /// <summary>
    /// Updates the operating pattern
    /// </summary>
    /// <param name="name">The new name</param>
    /// <param name="cycleDays">The new cycle days</param>
    /// <param name="weeklyHours">The new weekly hours</param>
    /// <param name="configuration">The new configuration</param>
    /// <param name="description">Optional description</param>
    public void UpdatePattern(
        string name,
        int cycleDays,
        decimal weeklyHours,
        JsonDocument configuration,
        string? description = null)
    {
        ValidatePatternUpdate(name, cycleDays, weeklyHours, configuration);

        var oldName = Name;
        var oldWeeklyHours = WeeklyHours;

        Name = name.Trim();
        CycleDays = cycleDays;
        WeeklyHours = weeklyHours;
        Configuration = configuration;
        Description = description?.Trim();

        MarkAsUpdated();

        AddDomainEvent(new OperatingPatternUpdatedEvent(Id, Name, WeeklyHours, oldName != Name || oldWeeklyHours != WeeklyHours));
    }

    /// <summary>
    /// Hides the pattern from user selection
    /// </summary>
    public void Hide()
    {
        if (!IsVisible)
            return;

        IsVisible = false;
        MarkAsUpdated();
        AddDomainEvent(new OperatingPatternVisibilityChangedEvent(Id, Name, false));
    }

    /// <summary>
    /// Makes the pattern visible for user selection
    /// </summary>
    public void Show()
    {
        if (IsVisible)
            return;

        IsVisible = true;
        MarkAsUpdated();
        AddDomainEvent(new OperatingPatternVisibilityChangedEvent(Id, Name, true));
    }

    /// <summary>
    /// Calculates the daily hours for a given day of the week
    /// </summary>
    /// <param name="dayOfWeek">The day of the week</param>
    /// <returns>The planned hours for that day</returns>
    public decimal GetDailyHours(DayOfWeek dayOfWeek)
    {
        // This is a simplified calculation - in reality, this would parse the Configuration JSON
        // to determine the actual hours for each day based on the pattern type
        return Type switch
        {
            PatternType.Continuous => 24.0m,
            PatternType.TwoShift => dayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? 0.0m : 16.0m,
            PatternType.DayOnly => dayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? 0.0m : 8.0m,
            PatternType.Extended => dayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? 0.0m : 12.0m,
            PatternType.Custom => CalculateCustomDailyHours(dayOfWeek),
            _ => 0.0m
        };
    }

    /// <summary>
    /// Gets the shift information for a specific time
    /// </summary>
    /// <param name="dateTime">The date and time to check</param>
    /// <returns>The shift code and planned hours, or null if no shift is active</returns>
    public (string ShiftCode, decimal PlannedHours)? GetShiftInfo(DateTime dateTime)
    {
        var dayOfWeek = dateTime.DayOfWeek;
        var timeOfDay = dateTime.TimeOfDay;

        return Type switch
        {
            PatternType.Continuous => ("24HR", 24.0m),
            PatternType.TwoShift => GetTwoShiftInfo(timeOfDay, dayOfWeek),
            PatternType.DayOnly => GetDayShiftInfo(timeOfDay, dayOfWeek),
            PatternType.Extended => GetExtendedShiftInfo(timeOfDay, dayOfWeek),
            PatternType.Custom => GetCustomShiftInfo(dateTime),
            _ => null
        };
    }

    /// <summary>
    /// Adds a domain event to be published
    /// </summary>
    /// <param name="domainEvent">The domain event to add</param>
    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clears all domain events
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    private decimal CalculateCustomDailyHours(DayOfWeek dayOfWeek)
    {
        // Parse the Configuration JSON to determine custom hours
        // This is a placeholder implementation
        try
        {
            if (Configuration.RootElement.TryGetProperty("dailyHours", out var dailyHoursElement))
            {
                var dayName = dayOfWeek.ToString().ToLowerInvariant();
                if (dailyHoursElement.TryGetProperty(dayName, out var hoursElement))
                {
                    return hoursElement.GetDecimal();
                }
            }
        }
        catch
        {
            // Log error and return default
        }

        return WeeklyHours / 7; // Simple fallback
    }

    private (string ShiftCode, decimal PlannedHours)? GetTwoShiftInfo(TimeSpan timeOfDay, DayOfWeek dayOfWeek)
    {
        if (dayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return null;

        return timeOfDay.Hours switch
        {
            >= 6 and < 14 => ("DAY", 8.0m),
            >= 14 and < 22 => ("EVE", 8.0m),
            _ => null
        };
    }

    private (string ShiftCode, decimal PlannedHours)? GetDayShiftInfo(TimeSpan timeOfDay, DayOfWeek dayOfWeek)
    {
        if (dayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return null;

        return timeOfDay.Hours >= 8 && timeOfDay.Hours < 16 ? ("DAY", 8.0m) : null;
    }

    private (string ShiftCode, decimal PlannedHours)? GetExtendedShiftInfo(TimeSpan timeOfDay, DayOfWeek dayOfWeek)
    {
        if (dayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return null;

        return timeOfDay.Hours >= 6 && timeOfDay.Hours < 18 ? ("EXT", 12.0m) : null;
    }

    private (string ShiftCode, decimal PlannedHours)? GetCustomShiftInfo(DateTime dateTime)
    {
        // Parse Configuration JSON for custom shift definitions
        // This is a placeholder implementation
        return ("CUSTOM", WeeklyHours / 7);
    }

    private static void ValidatePatternCreation(string name, PatternType type, int cycleDays, decimal weeklyHours, JsonDocument configuration)
    {
        ValidatePatternUpdate(name, cycleDays, weeklyHours, configuration);

        if (!Enum.IsDefined(typeof(PatternType), type))
            throw new ArgumentException("Invalid pattern type", nameof(type));
    }

    private static void ValidatePatternUpdate(string name, int cycleDays, decimal weeklyHours, JsonDocument configuration)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Pattern name cannot be empty", nameof(name));

        if (name.Length > 100)
            throw new ArgumentException("Pattern name cannot exceed 100 characters", nameof(name));

        if (cycleDays < 1 || cycleDays > 365)
            throw new ArgumentException("Cycle days must be between 1 and 365", nameof(cycleDays));

        if (weeklyHours < 0 || weeklyHours > 168)
            throw new ArgumentException("Weekly hours must be between 0 and 168", nameof(weeklyHours));

        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));
    }
}
