namespace Industrial.Adam.EquipmentScheduling.Domain.Enums;

/// <summary>
/// Status of generated equipment schedule entries
/// </summary>
public enum ScheduleStatus
{
    /// <summary>
    /// Schedule is active and operational
    /// </summary>
    Active = 1,

    /// <summary>
    /// Schedule is planned but not yet active
    /// </summary>
    Planned = 2,

    /// <summary>
    /// Schedule has been cancelled
    /// </summary>
    Cancelled = 3,

    /// <summary>
    /// Schedule is completed
    /// </summary>
    Completed = 4,

    /// <summary>
    /// Schedule is temporarily suspended
    /// </summary>
    Suspended = 5
}
