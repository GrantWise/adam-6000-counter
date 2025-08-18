using Industrial.Adam.Oee.Domain.Entities;

namespace Industrial.Adam.Oee.Domain.Interfaces;

/// <summary>
/// Repository interface for equipment line persistence
/// </summary>
public interface IEquipmentLineRepository
{
    /// <summary>
    /// Get an equipment line by its database identifier
    /// </summary>
    /// <param name="id">Database identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Equipment line or null if not found</returns>
    public Task<EquipmentLine?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get an equipment line by its business line identifier
    /// </summary>
    /// <param name="lineId">Business line identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Equipment line or null if not found</returns>
    public Task<EquipmentLine?> GetByLineIdAsync(string lineId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get equipment line by ADAM device mapping
    /// </summary>
    /// <param name="adamDeviceId">ADAM device identifier</param>
    /// <param name="adamChannel">ADAM channel number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Equipment line or null if not found</returns>
    public Task<EquipmentLine?> GetByAdamDeviceAsync(string adamDeviceId, int adamChannel, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active equipment lines
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of active equipment lines</returns>
    public Task<IEnumerable<EquipmentLine>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active equipment lines (alias for GetActiveAsync to match detection service usage)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of active equipment lines</returns>
    public Task<IEnumerable<EquipmentLine>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all equipment lines (active and inactive)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of all equipment lines</returns>
    public Task<IEnumerable<EquipmentLine>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get equipment lines by ADAM device
    /// </summary>
    /// <param name="adamDeviceId">ADAM device identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of equipment lines for the device</returns>
    public Task<IEnumerable<EquipmentLine>> GetByAdamDeviceIdAsync(string adamDeviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new equipment line
    /// </summary>
    /// <param name="equipmentLine">Equipment line to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created equipment line identifier</returns>
    public Task<int> CreateAsync(EquipmentLine equipmentLine, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing equipment line
    /// </summary>
    /// <param name="equipmentLine">Equipment line to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if updated successfully, false if not found</returns>
    public Task<bool> UpdateAsync(EquipmentLine equipmentLine, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete an equipment line
    /// </summary>
    /// <param name="id">Equipment line identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully, false if not found</returns>
    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if an equipment line exists by line ID
    /// </summary>
    /// <param name="lineId">Business line identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if equipment line exists, false otherwise</returns>
    public Task<bool> ExistsByLineIdAsync(string lineId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if ADAM device mapping is available
    /// </summary>
    /// <param name="adamDeviceId">ADAM device identifier</param>
    /// <param name="adamChannel">ADAM channel number</param>
    /// <param name="excludeLineId">Line ID to exclude from check (for updates)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if mapping is available, false if already used</returns>
    public Task<bool> IsAdamDeviceMappingAvailableAsync(string adamDeviceId, int adamChannel, string? excludeLineId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get ADAM device mappings for reporting
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of ADAM device mappings</returns>
    public Task<IEnumerable<AdamDeviceMapping>> GetAdamDeviceMappingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Search equipment lines by criteria
    /// </summary>
    /// <param name="criteria">Search criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of matching equipment lines</returns>
    public Task<IEnumerable<EquipmentLine>> SearchAsync(EquipmentLineSearchCriteria criteria, CancellationToken cancellationToken = default);
}

/// <summary>
/// Search criteria for equipment lines
/// </summary>
/// <param name="LineId">Optional line ID filter</param>
/// <param name="LineName">Optional line name filter (partial match)</param>
/// <param name="AdamDeviceId">Optional ADAM device ID filter</param>
/// <param name="AdamChannel">Optional ADAM channel filter</param>
/// <param name="IsActive">Optional active status filter</param>
public record EquipmentLineSearchCriteria(
    string? LineId = null,
    string? LineName = null,
    string? AdamDeviceId = null,
    int? AdamChannel = null,
    bool? IsActive = null
);

/// <summary>
/// ADAM device mapping for reporting
/// </summary>
/// <param name="LineId">Equipment line identifier</param>
/// <param name="LineName">Equipment line name</param>
/// <param name="AdamDeviceId">ADAM device identifier</param>
/// <param name="AdamChannel">ADAM channel number</param>
/// <param name="IsActive">Whether the mapping is active</param>
/// <param name="CreatedAt">When the mapping was created</param>
/// <param name="UpdatedAt">When the mapping was last updated</param>
public record AdamDeviceMapping(
    string LineId,
    string LineName,
    string AdamDeviceId,
    int AdamChannel,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
