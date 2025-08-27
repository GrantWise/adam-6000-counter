namespace Industrial.Adam.Security.Domain.ValueObjects;

/// <summary>
/// Security configuration value object with encryption support
/// </summary>
public sealed class SecurityConfiguration
{
    public string Key { get; }
    public string Value { get; }
    public bool IsEncrypted { get; }
    public ConfigurationType Type { get; }
    public DateTime CreatedAt { get; }
    public DateTime? ModifiedAt { get; }
    public string CreatedBy { get; }
    public string? ModifiedBy { get; }
    public Dictionary<string, string> Metadata { get; }

    public SecurityConfiguration(
        string key,
        string value,
        bool isEncrypted,
        ConfigurationType type,
        string createdBy,
        Dictionary<string, string>? metadata = null,
        DateTime? modifiedAt = null,
        string? modifiedBy = null,
        DateTime? createdAt = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Configuration key cannot be null or whitespace.", nameof(key));
        
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Configuration value cannot be null or whitespace.", nameof(value));
        
        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ArgumentException("CreatedBy cannot be null or whitespace.", nameof(createdBy));

        Key = key;
        Value = value;
        IsEncrypted = isEncrypted;
        Type = type;
        CreatedAt = createdAt ?? DateTime.UtcNow;
        CreatedBy = createdBy;
        ModifiedAt = modifiedAt;
        ModifiedBy = modifiedBy;
        Metadata = metadata ?? new Dictionary<string, string>();
    }

    public SecurityConfiguration UpdateValue(string newValue, string modifiedBy, bool isEncrypted = false)
    {
        return new SecurityConfiguration(Key, newValue, isEncrypted, Type, CreatedBy, Metadata, DateTime.UtcNow, modifiedBy, CreatedAt);
    }

    public bool IsSensitive => Type == ConfigurationType.Secret || Type == ConfigurationType.ConnectionString || IsEncrypted;

    public override bool Equals(object? obj)
    {
        return obj is SecurityConfiguration other && Key == other.Key;
    }

    public override int GetHashCode()
    {
        return Key.GetHashCode();
    }
}

public enum ConfigurationType
{
    Setting = 1,
    Secret = 2,
    ConnectionString = 3,
    Certificate = 4,
    Policy = 5
}

/// <summary>
/// IP Address whitelist value object
/// </summary>
public sealed class IpWhitelist
{
    public List<string> AllowedRanges { get; }
    public List<string> BlockedRanges { get; }
    public DateTime CreatedAt { get; }
    public string CreatedBy { get; }

    public IpWhitelist(
        IEnumerable<string> allowedRanges,
        IEnumerable<string>? blockedRanges = null,
        string createdBy = "System")
    {
        AllowedRanges = allowedRanges?.ToList() ?? throw new ArgumentNullException(nameof(allowedRanges));
        BlockedRanges = blockedRanges?.ToList() ?? new List<string>();
        CreatedAt = DateTime.UtcNow;
        CreatedBy = createdBy;

        ValidateRanges();
    }

    private void ValidateRanges()
    {
        foreach (var range in AllowedRanges.Concat(BlockedRanges))
        {
            if (!IsValidIpRange(range))
                throw new ArgumentException($"Invalid IP range format: {range}");
        }
    }

    private static bool IsValidIpRange(string range)
    {
        // Basic validation - could be enhanced with proper CIDR validation
        return !string.IsNullOrWhiteSpace(range) && 
               (range.Contains('/') || System.Net.IPAddress.TryParse(range, out _));
    }

    public bool IsAllowed(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return false;

        // Check if blocked first
        if (BlockedRanges.Any(range => IsIpInRange(ipAddress, range)))
            return false;

        // If no allowed ranges specified, allow all (except blocked)
        if (!AllowedRanges.Any())
            return true;

        // Check if in allowed ranges
        return AllowedRanges.Any(range => IsIpInRange(ipAddress, range));
    }

    private static bool IsIpInRange(string ipAddress, string range)
    {
        // Simplified IP range checking - should be enhanced with proper CIDR logic
        if (range == "*" || range == "0.0.0.0/0")
            return true;

        if (range.Contains('/'))
        {
            // CIDR notation - simplified check
            var parts = range.Split('/');
            if (parts.Length == 2 && ipAddress.StartsWith(parts[0].Split('.')[0]))
                return true;
        }
        else
        {
            // Exact match
            return ipAddress == range;
        }

        return false;
    }
}