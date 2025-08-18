using Industrial.Adam.Oee.Domain.Interfaces;

namespace Industrial.Adam.Oee.Domain.Entities;

/// <summary>
/// Equipment Line Aggregate Root
/// 
/// Represents a physical production line with its associated ADAM device mapping.
/// Provides the bridge between ADAM counter data and business equipment identification.
/// Enforces 1:1 relationship between ADAM device/channel and equipment line.
/// </summary>
public sealed class EquipmentLine : Entity<int>, IAggregateRoot
{
    /// <summary>
    /// Business-friendly line identifier
    /// </summary>
    public string LineId { get; private set; }

    /// <summary>
    /// Human-readable line name
    /// </summary>
    public string LineName { get; private set; }

    /// <summary>
    /// ADAM device identifier providing counter data
    /// </summary>
    public string AdamDeviceId { get; private set; }

    /// <summary>
    /// ADAM channel number (0-15) for production counting
    /// </summary>
    public int AdamChannel { get; private set; }

    /// <summary>
    /// Whether this equipment line is active and available for production
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// When this equipment line was created
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Default constructor for serialization
    /// </summary>
    private EquipmentLine() : base()
    {
        LineId = string.Empty;
        LineName = string.Empty;
        AdamDeviceId = string.Empty;
        IsActive = true;
    }

    /// <summary>
    /// Creates a new equipment line
    /// </summary>
    /// <param name="lineId">Business-friendly line identifier</param>
    /// <param name="lineName">Human-readable line name</param>
    /// <param name="adamDeviceId">ADAM device identifier</param>
    /// <param name="adamChannel">ADAM channel number (0-15)</param>
    /// <param name="isActive">Whether line is active (default: true)</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public EquipmentLine(
        string lineId,
        string lineName,
        string adamDeviceId,
        int adamChannel,
        bool isActive = true) : base()
    {
        ValidateConstructorParameters(lineId, lineName, adamDeviceId, adamChannel);

        LineId = lineId;
        LineName = lineName;
        AdamDeviceId = adamDeviceId;
        AdamChannel = adamChannel;
        IsActive = isActive;

        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Create equipment line with specific ID (for repository loading)
    /// </summary>
    /// <param name="id">Database identifier</param>
    /// <param name="lineId">Business-friendly line identifier</param>
    /// <param name="lineName">Human-readable line name</param>
    /// <param name="adamDeviceId">ADAM device identifier</param>
    /// <param name="adamChannel">ADAM channel number</param>
    /// <param name="isActive">Whether line is active</param>
    /// <param name="createdAt">Creation timestamp</param>
    /// <param name="updatedAt">Last update timestamp</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public EquipmentLine(
        int id,
        string lineId,
        string lineName,
        string adamDeviceId,
        int adamChannel,
        bool isActive,
        DateTime createdAt,
        DateTime updatedAt) : base(id)
    {
        ValidateConstructorParameters(lineId, lineName, adamDeviceId, adamChannel);

        LineId = lineId;
        LineName = lineName;
        AdamDeviceId = adamDeviceId;
        AdamChannel = adamChannel;
        IsActive = isActive;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    /// <summary>
    /// Update line name
    /// </summary>
    /// <param name="lineName">New line name</param>
    /// <exception cref="ArgumentException">Thrown when line name is invalid</exception>
    public void UpdateLineName(string lineName)
    {
        if (string.IsNullOrWhiteSpace(lineName))
            throw new ArgumentException("Line name cannot be empty", nameof(lineName));

        LineName = lineName;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update ADAM device mapping
    /// </summary>
    /// <param name="adamDeviceId">New ADAM device identifier</param>
    /// <param name="adamChannel">New ADAM channel number</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public void UpdateAdamMapping(string adamDeviceId, int adamChannel)
    {
        ValidateAdamParameters(adamDeviceId, adamChannel);

        AdamDeviceId = adamDeviceId;
        AdamChannel = adamChannel;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activate the equipment line
    /// </summary>
    public void Activate()
    {
        if (IsActive)
            return;

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivate the equipment line
    /// </summary>
    public void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Check if this equipment line matches the specified ADAM device and channel
    /// </summary>
    /// <param name="adamDeviceId">ADAM device identifier</param>
    /// <param name="adamChannel">ADAM channel number</param>
    /// <returns>True if matches, false otherwise</returns>
    public bool MatchesAdamDevice(string adamDeviceId, int adamChannel)
    {
        return IsActive &&
               string.Equals(AdamDeviceId, adamDeviceId, StringComparison.OrdinalIgnoreCase) &&
               AdamChannel == adamChannel;
    }

    /// <summary>
    /// Get equipment line summary for reporting
    /// </summary>
    /// <returns>Equipment line summary</returns>
    public EquipmentLineSummary ToSummary()
    {
        return new EquipmentLineSummary(
            Id,
            LineId,
            LineName,
            AdamDeviceId,
            AdamChannel,
            IsActive,
            CreatedAt,
            UpdatedAt
        );
    }

    /// <summary>
    /// Validate constructor parameters
    /// </summary>
    private static void ValidateConstructorParameters(
        string lineId,
        string lineName,
        string adamDeviceId,
        int adamChannel)
    {
        if (string.IsNullOrWhiteSpace(lineId))
            throw new ArgumentException("Line ID is required", nameof(lineId));

        if (string.IsNullOrWhiteSpace(lineName))
            throw new ArgumentException("Line name is required", nameof(lineName));

        ValidateAdamParameters(adamDeviceId, adamChannel);
    }

    /// <summary>
    /// Validate ADAM device parameters
    /// </summary>
    private static void ValidateAdamParameters(string adamDeviceId, int adamChannel)
    {
        if (string.IsNullOrWhiteSpace(adamDeviceId))
            throw new ArgumentException("ADAM device ID is required", nameof(adamDeviceId));

        if (adamChannel < 0 || adamChannel > 15)
            throw new ArgumentException("ADAM channel must be between 0 and 15", nameof(adamChannel));
    }

    /// <summary>
    /// String representation of the equipment line
    /// </summary>
    /// <returns>Formatted string representation</returns>
    public override string ToString()
    {
        return $"Equipment Line {LineId}: {LineName} (ADAM {AdamDeviceId}:{AdamChannel})";
    }
}

/// <summary>
/// Equipment line creation data
/// </summary>
/// <param name="LineId">Business-friendly line identifier</param>
/// <param name="LineName">Human-readable line name</param>
/// <param name="AdamDeviceId">ADAM device identifier</param>
/// <param name="AdamChannel">ADAM channel number</param>
/// <param name="IsActive">Whether line is active</param>
public record EquipmentLineCreationData(
    string LineId,
    string LineName,
    string AdamDeviceId,
    int AdamChannel,
    bool IsActive = true
);

/// <summary>
/// Equipment line summary for reporting
/// </summary>
/// <param name="Id">Database identifier</param>
/// <param name="LineId">Business line identifier</param>
/// <param name="LineName">Line name</param>
/// <param name="AdamDeviceId">ADAM device identifier</param>
/// <param name="AdamChannel">ADAM channel number</param>
/// <param name="IsActive">Whether line is active</param>
/// <param name="CreatedAt">Creation timestamp</param>
/// <param name="UpdatedAt">Last update timestamp</param>
public record EquipmentLineSummary(
    int Id,
    string LineId,
    string LineName,
    string AdamDeviceId,
    int AdamChannel,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

