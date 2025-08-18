namespace Industrial.Adam.EquipmentScheduling.Domain.Enums;

/// <summary>
/// Standard operating pattern types for equipment scheduling
/// </summary>
public enum PatternType
{
    /// <summary>
    /// 24/7 continuous operation pattern
    /// </summary>
    Continuous = 1,

    /// <summary>
    /// Two shift operation (typically 16 hours/day)
    /// </summary>
    TwoShift = 2,

    /// <summary>
    /// Day shift only operation (typically 8 hours/day)
    /// </summary>
    DayOnly = 3,

    /// <summary>
    /// Extended hours operation (typically 12 hours/day)
    /// </summary>
    Extended = 4,

    /// <summary>
    /// Custom pattern defined by specific configuration
    /// </summary>
    Custom = 5
}
