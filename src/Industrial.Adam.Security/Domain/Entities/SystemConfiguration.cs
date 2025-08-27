namespace Industrial.Adam.Security.Domain.Entities;

/// <summary>
/// System configuration setting
/// </summary>
public class SystemConfiguration
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int UpdatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}