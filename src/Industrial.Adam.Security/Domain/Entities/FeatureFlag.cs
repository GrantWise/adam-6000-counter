namespace Industrial.Adam.Security.Domain.Entities;

/// <summary>
/// Feature flag for controlling system features
/// </summary>
public class FeatureFlag
{
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Conditions { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}