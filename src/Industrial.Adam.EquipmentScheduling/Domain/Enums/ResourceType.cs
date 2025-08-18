namespace Industrial.Adam.EquipmentScheduling.Domain.Enums;

/// <summary>
/// ISA-95 compliant equipment hierarchy resource types
/// </summary>
public enum ResourceType
{
    /// <summary>
    /// Highest level organizational unit (entire company/plant)
    /// </summary>
    Enterprise = 1,

    /// <summary>
    /// Physical location or major facility section
    /// </summary>
    Site = 2,

    /// <summary>
    /// Functional area within a site (production area, storage area)
    /// </summary>
    Area = 3,

    /// <summary>
    /// Group of related equipment performing similar functions
    /// </summary>
    WorkCenter = 4,

    /// <summary>
    /// Individual piece of equipment or machine
    /// </summary>
    WorkUnit = 5
}
