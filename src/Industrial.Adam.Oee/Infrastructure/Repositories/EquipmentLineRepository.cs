using System.Data;
using Dapper;
using Industrial.Adam.Oee.Domain.Entities;
using Industrial.Adam.Oee.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Oee.Infrastructure.Repositories;

/// <summary>
/// TimescaleDB repository for equipment line persistence
/// Provides full CRUD operations for the equipment_lines table
/// Handles ADAM device mapping validation and constraints
/// </summary>
public sealed class EquipmentLineRepository : IEquipmentLineRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<EquipmentLineRepository> _logger;

    /// <summary>
    /// Constructor for equipment line repository
    /// </summary>
    /// <param name="connectionFactory">Database connection factory</param>
    /// <param name="logger">Logger instance</param>
    public EquipmentLineRepository(
        IDbConnectionFactory connectionFactory,
        ILogger<EquipmentLineRepository> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get an equipment line by its database identifier
    /// </summary>
    /// <param name="id">Database identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Equipment line or null if not found</returns>
    public async Task<EquipmentLine?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ArgumentException("ID must be greater than zero", nameof(id));

        using var activity = ActivitySource.StartActivity("GetEquipmentLineById");
        activity?.SetTag("id", id);

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = @"
                SELECT 
                    id,
                    line_id,
                    line_name,
                    adam_device_id,
                    adam_channel,
                    is_active,
                    created_at,
                    updated_at
                FROM equipment_lines 
                WHERE id = @id";

            _logger.LogDebug("Retrieving equipment line with ID {Id}", id);

            var equipmentLineData = await connection.QuerySingleOrDefaultAsync<EquipmentLineData>(sql, new { id });

            if (equipmentLineData == null)
            {
                _logger.LogWarning("Equipment line with ID {Id} not found", id);
                return null;
            }

            var equipmentLine = MapToEquipmentLine(equipmentLineData);

            _logger.LogDebug("Retrieved equipment line {LineId} ({LineName})", 
                equipmentLine.LineId, equipmentLine.LineName);

            return equipmentLine;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve equipment line with ID {Id}", id);
            throw;
        }
    }

    /// <summary>
    /// Get an equipment line by its business line identifier
    /// </summary>
    /// <param name="lineId">Business line identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Equipment line or null if not found</returns>
    public async Task<EquipmentLine?> GetByLineIdAsync(string lineId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lineId))
            throw new ArgumentException("Line ID cannot be null or empty", nameof(lineId));

        using var activity = ActivitySource.StartActivity("GetEquipmentLineByLineId");
        activity?.SetTag("lineId", lineId);

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = @"
                SELECT 
                    id,
                    line_id,
                    line_name,
                    adam_device_id,
                    adam_channel,
                    is_active,
                    created_at,
                    updated_at
                FROM equipment_lines 
                WHERE line_id = @lineId";

            _logger.LogDebug("Retrieving equipment line {LineId}", lineId);

            var equipmentLineData = await connection.QuerySingleOrDefaultAsync<EquipmentLineData>(sql, new { lineId });

            if (equipmentLineData == null)
            {
                _logger.LogDebug("Equipment line {LineId} not found", lineId);
                return null;
            }

            var equipmentLine = MapToEquipmentLine(equipmentLineData);

            _logger.LogDebug("Retrieved equipment line {LineId} ({LineName})", 
                equipmentLine.LineId, equipmentLine.LineName);

            return equipmentLine;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve equipment line {LineId}", lineId);
            throw;
        }
    }

    /// <summary>
    /// Get equipment line by ADAM device mapping
    /// </summary>
    /// <param name="adamDeviceId">ADAM device identifier</param>
    /// <param name="adamChannel">ADAM channel number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Equipment line or null if not found</returns>
    public async Task<EquipmentLine?> GetByAdamDeviceAsync(string adamDeviceId, int adamChannel, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(adamDeviceId))
            throw new ArgumentException("ADAM device ID cannot be null or empty", nameof(adamDeviceId));

        if (adamChannel < 0 || adamChannel > 15)
            throw new ArgumentException("ADAM channel must be between 0 and 15", nameof(adamChannel));

        using var activity = ActivitySource.StartActivity("GetEquipmentLineByAdamDevice");
        activity?.SetTag("adamDeviceId", adamDeviceId);
        activity?.SetTag("adamChannel", adamChannel);

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = @"
                SELECT 
                    id,
                    line_id,
                    line_name,
                    adam_device_id,
                    adam_channel,
                    is_active,
                    created_at,
                    updated_at
                FROM equipment_lines 
                WHERE adam_device_id = @adamDeviceId 
                  AND adam_channel = @adamChannel";

            _logger.LogDebug("Retrieving equipment line for ADAM device {AdamDeviceId}:{AdamChannel}", 
                adamDeviceId, adamChannel);

            var equipmentLineData = await connection.QuerySingleOrDefaultAsync<EquipmentLineData>(sql, 
                new { adamDeviceId, adamChannel });

            if (equipmentLineData == null)
            {
                _logger.LogDebug("No equipment line found for ADAM device {AdamDeviceId}:{AdamChannel}", 
                    adamDeviceId, adamChannel);
                return null;
            }

            var equipmentLine = MapToEquipmentLine(equipmentLineData);

            _logger.LogDebug("Retrieved equipment line {LineId} for ADAM device {AdamDeviceId}:{AdamChannel}", 
                equipmentLine.LineId, adamDeviceId, adamChannel);

            return equipmentLine;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve equipment line for ADAM device {AdamDeviceId}:{AdamChannel}", 
                adamDeviceId, adamChannel);
            throw;
        }
    }

    /// <summary>
    /// Get all active equipment lines
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of active equipment lines</returns>
    public async Task<IEnumerable<EquipmentLine>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("GetActiveEquipmentLines");

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = @"
                SELECT 
                    id,
                    line_id,
                    line_name,
                    adam_device_id,
                    adam_channel,
                    is_active,
                    created_at,
                    updated_at
                FROM equipment_lines 
                WHERE is_active = true
                ORDER BY line_id";

            _logger.LogDebug("Retrieving active equipment lines");

            var equipmentLineDataList = await connection.QueryAsync<EquipmentLineData>(sql);
            var equipmentLines = equipmentLineDataList.Select(MapToEquipmentLine).ToList();

            _logger.LogInformation("Retrieved {Count} active equipment lines", equipmentLines.Count);

            return equipmentLines;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve active equipment lines");
            throw;
        }
    }

    /// <summary>
    /// Get all active equipment lines (alias for GetActiveAsync)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of active equipment lines</returns>
    public async Task<IEnumerable<EquipmentLine>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await GetActiveAsync(cancellationToken);
    }

    /// <summary>
    /// Get all equipment lines (active and inactive)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of all equipment lines</returns>
    public async Task<IEnumerable<EquipmentLine>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("GetAllEquipmentLines");

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = @"
                SELECT 
                    id,
                    line_id,
                    line_name,
                    adam_device_id,
                    adam_channel,
                    is_active,
                    created_at,
                    updated_at
                FROM equipment_lines 
                ORDER BY line_id";

            _logger.LogDebug("Retrieving all equipment lines");

            var equipmentLineDataList = await connection.QueryAsync<EquipmentLineData>(sql);
            var equipmentLines = equipmentLineDataList.Select(MapToEquipmentLine).ToList();

            _logger.LogInformation("Retrieved {Count} equipment lines", equipmentLines.Count);

            return equipmentLines;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve all equipment lines");
            throw;
        }
    }

    /// <summary>
    /// Get equipment lines by ADAM device
    /// </summary>
    /// <param name="adamDeviceId">ADAM device identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of equipment lines for the device</returns>
    public async Task<IEnumerable<EquipmentLine>> GetByAdamDeviceIdAsync(string adamDeviceId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(adamDeviceId))
            throw new ArgumentException("ADAM device ID cannot be null or empty", nameof(adamDeviceId));

        using var activity = ActivitySource.StartActivity("GetEquipmentLinesByAdamDeviceId");
        activity?.SetTag("adamDeviceId", adamDeviceId);

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = @"
                SELECT 
                    id,
                    line_id,
                    line_name,
                    adam_device_id,
                    adam_channel,
                    is_active,
                    created_at,
                    updated_at
                FROM equipment_lines 
                WHERE adam_device_id = @adamDeviceId
                ORDER BY adam_channel";

            _logger.LogDebug("Retrieving equipment lines for ADAM device {AdamDeviceId}", adamDeviceId);

            var equipmentLineDataList = await connection.QueryAsync<EquipmentLineData>(sql, new { adamDeviceId });
            var equipmentLines = equipmentLineDataList.Select(MapToEquipmentLine).ToList();

            _logger.LogInformation("Retrieved {Count} equipment lines for ADAM device {AdamDeviceId}", 
                equipmentLines.Count, adamDeviceId);

            return equipmentLines;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve equipment lines for ADAM device {AdamDeviceId}", adamDeviceId);
            throw;
        }
    }

    /// <summary>
    /// Create a new equipment line
    /// </summary>
    /// <param name="equipmentLine">Equipment line to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created equipment line identifier</returns>
    public async Task<int> CreateAsync(EquipmentLine equipmentLine, CancellationToken cancellationToken = default)
    {
        if (equipmentLine == null)
            throw new ArgumentNullException(nameof(equipmentLine));

        using var activity = ActivitySource.StartActivity("CreateEquipmentLine");
        activity?.SetTag("lineId", equipmentLine.LineId);

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = @"
                INSERT INTO equipment_lines (
                    line_id,
                    line_name,
                    adam_device_id,
                    adam_channel,
                    is_active,
                    created_at,
                    updated_at
                ) VALUES (
                    @LineId,
                    @LineName,
                    @AdamDeviceId,
                    @AdamChannel,
                    @IsActive,
                    @CreatedAt,
                    @UpdatedAt
                ) RETURNING id";

            var parameters = new
            {
                equipmentLine.LineId,
                equipmentLine.LineName,
                equipmentLine.AdamDeviceId,
                equipmentLine.AdamChannel,
                equipmentLine.IsActive,
                equipmentLine.CreatedAt,
                equipmentLine.UpdatedAt
            };

            _logger.LogDebug("Creating equipment line {LineId} ({LineName})", 
                equipmentLine.LineId, equipmentLine.LineName);

            var newId = await connection.QuerySingleAsync<int>(sql, parameters);

            _logger.LogInformation("Created equipment line {LineId} ({LineName}) with ID {Id} for ADAM device {AdamDeviceId}:{AdamChannel}",
                equipmentLine.LineId, equipmentLine.LineName, newId, 
                equipmentLine.AdamDeviceId, equipmentLine.AdamChannel);

            return newId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create equipment line {LineId}", equipmentLine.LineId);
            throw;
        }
    }

    /// <summary>
    /// Update an existing equipment line
    /// </summary>
    /// <param name="equipmentLine">Equipment line to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if updated successfully, false if not found</returns>
    public async Task<bool> UpdateAsync(EquipmentLine equipmentLine, CancellationToken cancellationToken = default)
    {
        if (equipmentLine == null)
            throw new ArgumentNullException(nameof(equipmentLine));

        using var activity = ActivitySource.StartActivity("UpdateEquipmentLine");
        activity?.SetTag("lineId", equipmentLine.LineId);
        activity?.SetTag("id", equipmentLine.Id);

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = @"
                UPDATE equipment_lines SET
                    line_id = @LineId,
                    line_name = @LineName,
                    adam_device_id = @AdamDeviceId,
                    adam_channel = @AdamChannel,
                    is_active = @IsActive,
                    updated_at = @UpdatedAt
                WHERE id = @Id";

            var parameters = new
            {
                equipmentLine.Id,
                equipmentLine.LineId,
                equipmentLine.LineName,
                equipmentLine.AdamDeviceId,
                equipmentLine.AdamChannel,
                equipmentLine.IsActive,
                UpdatedAt = DateTime.UtcNow
            };

            _logger.LogDebug("Updating equipment line {LineId} (ID: {Id})", equipmentLine.LineId, equipmentLine.Id);

            var rowsAffected = await connection.ExecuteAsync(sql, parameters);
            var wasUpdated = rowsAffected > 0;

            if (wasUpdated)
            {
                _logger.LogInformation("Updated equipment line {LineId} (ID: {Id})", 
                    equipmentLine.LineId, equipmentLine.Id);
            }
            else
            {
                _logger.LogWarning("Equipment line with ID {Id} not found for update", equipmentLine.Id);
            }

            return wasUpdated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update equipment line {LineId} (ID: {Id})", 
                equipmentLine.LineId, equipmentLine.Id);
            throw;
        }
    }

    /// <summary>
    /// Delete an equipment line
    /// </summary>
    /// <param name="id">Equipment line identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully, false if not found</returns>
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ArgumentException("ID must be greater than zero", nameof(id));

        using var activity = ActivitySource.StartActivity("DeleteEquipmentLine");
        activity?.SetTag("id", id);

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = "DELETE FROM equipment_lines WHERE id = @id";

            _logger.LogDebug("Deleting equipment line with ID {Id}", id);

            var rowsAffected = await connection.ExecuteAsync(sql, new { id });
            var wasDeleted = rowsAffected > 0;

            if (wasDeleted)
            {
                _logger.LogInformation("Deleted equipment line with ID {Id}", id);
            }
            else
            {
                _logger.LogWarning("Equipment line with ID {Id} not found for deletion", id);
            }

            return wasDeleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete equipment line with ID {Id}", id);
            throw;
        }
    }

    /// <summary>
    /// Check if an equipment line exists by line ID
    /// </summary>
    /// <param name="lineId">Business line identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if equipment line exists, false otherwise</returns>
    public async Task<bool> ExistsByLineIdAsync(string lineId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lineId))
            throw new ArgumentException("Line ID cannot be null or empty", nameof(lineId));

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = "SELECT EXISTS(SELECT 1 FROM equipment_lines WHERE line_id = @lineId)";

            var exists = await connection.QuerySingleAsync<bool>(sql, new { lineId });

            _logger.LogDebug("Equipment line {LineId} exists: {Exists}", lineId, exists);

            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if equipment line {LineId} exists", lineId);
            throw;
        }
    }

    /// <summary>
    /// Check if ADAM device mapping is available
    /// </summary>
    /// <param name="adamDeviceId">ADAM device identifier</param>
    /// <param name="adamChannel">ADAM channel number</param>
    /// <param name="excludeLineId">Line ID to exclude from check (for updates)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if mapping is available, false if already used</returns>
    public async Task<bool> IsAdamDeviceMappingAvailableAsync(string adamDeviceId, int adamChannel, 
        string? excludeLineId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(adamDeviceId))
            throw new ArgumentException("ADAM device ID cannot be null or empty", nameof(adamDeviceId));

        if (adamChannel < 0 || adamChannel > 15)
            throw new ArgumentException("ADAM channel must be between 0 and 15", nameof(adamChannel));

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            var sql = @"
                SELECT EXISTS(
                    SELECT 1 FROM equipment_lines 
                    WHERE adam_device_id = @adamDeviceId 
                      AND adam_channel = @adamChannel";

            var parameters = new Dictionary<string, object>
            {
                ["adamDeviceId"] = adamDeviceId,
                ["adamChannel"] = adamChannel
            };

            if (!string.IsNullOrWhiteSpace(excludeLineId))
            {
                sql += " AND line_id != @excludeLineId";
                parameters["excludeLineId"] = excludeLineId;
            }

            sql += ")";

            var mappingExists = await connection.QuerySingleAsync<bool>(sql, parameters);
            var isAvailable = !mappingExists;

            _logger.LogDebug("ADAM device mapping {AdamDeviceId}:{AdamChannel} available: {IsAvailable}", 
                adamDeviceId, adamChannel, isAvailable);

            return isAvailable;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check ADAM device mapping availability for {AdamDeviceId}:{AdamChannel}", 
                adamDeviceId, adamChannel);
            throw;
        }
    }

    /// <summary>
    /// Get ADAM device mappings for reporting
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of ADAM device mappings</returns>
    public async Task<IEnumerable<AdamDeviceMapping>> GetAdamDeviceMappingsAsync(CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("GetAdamDeviceMappings");

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = @"
                SELECT 
                    line_id as LineId,
                    line_name as LineName,
                    adam_device_id as AdamDeviceId,
                    adam_channel as AdamChannel,
                    is_active as IsActive,
                    created_at as CreatedAt,
                    updated_at as UpdatedAt
                FROM equipment_lines 
                ORDER BY adam_device_id, adam_channel";

            _logger.LogDebug("Retrieving ADAM device mappings");

            var mappings = await connection.QueryAsync<AdamDeviceMapping>(sql);
            var mappingsList = mappings.ToList();

            _logger.LogInformation("Retrieved {Count} ADAM device mappings", mappingsList.Count);

            return mappingsList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve ADAM device mappings");
            throw;
        }
    }

    /// <summary>
    /// Search equipment lines by criteria
    /// </summary>
    /// <param name="criteria">Search criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of matching equipment lines</returns>
    public async Task<IEnumerable<EquipmentLine>> SearchAsync(EquipmentLineSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        if (criteria == null)
            throw new ArgumentNullException(nameof(criteria));

        using var activity = ActivitySource.StartActivity("SearchEquipmentLines");

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            var whereConditions = new List<string>();
            var parameters = new Dictionary<string, object>();

            if (!string.IsNullOrWhiteSpace(criteria.LineId))
            {
                whereConditions.Add("line_id ILIKE @lineId");
                parameters["lineId"] = $"%{criteria.LineId}%";
            }

            if (!string.IsNullOrWhiteSpace(criteria.LineName))
            {
                whereConditions.Add("line_name ILIKE @lineName");
                parameters["lineName"] = $"%{criteria.LineName}%";
            }

            if (!string.IsNullOrWhiteSpace(criteria.AdamDeviceId))
            {
                whereConditions.Add("adam_device_id = @adamDeviceId");
                parameters["adamDeviceId"] = criteria.AdamDeviceId;
            }

            if (criteria.AdamChannel.HasValue)
            {
                whereConditions.Add("adam_channel = @adamChannel");
                parameters["adamChannel"] = criteria.AdamChannel.Value;
            }

            if (criteria.IsActive.HasValue)
            {
                whereConditions.Add("is_active = @isActive");
                parameters["isActive"] = criteria.IsActive.Value;
            }

            var whereClause = whereConditions.Count > 0 ? "WHERE " + string.Join(" AND ", whereConditions) : "";

            var sql = $@"
                SELECT 
                    id,
                    line_id,
                    line_name,
                    adam_device_id,
                    adam_channel,
                    is_active,
                    created_at,
                    updated_at
                FROM equipment_lines 
                {whereClause}
                ORDER BY line_id";

            _logger.LogDebug("Searching equipment lines with {ConditionCount} criteria", whereConditions.Count);

            var equipmentLineDataList = await connection.QueryAsync<EquipmentLineData>(sql, parameters);
            var equipmentLines = equipmentLineDataList.Select(MapToEquipmentLine).ToList();

            _logger.LogInformation("Found {Count} equipment lines matching search criteria", equipmentLines.Count);

            return equipmentLines;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search equipment lines");
            throw;
        }
    }

    /// <summary>
    /// Maps database row data to EquipmentLine domain entity
    /// </summary>
    /// <param name="data">Database row data</param>
    /// <returns>EquipmentLine domain entity</returns>
    private static EquipmentLine MapToEquipmentLine(EquipmentLineData data)
    {
        return new EquipmentLine(
            data.Id,
            data.LineId,
            data.LineName,
            data.AdamDeviceId,
            data.AdamChannel,
            data.IsActive,
            data.CreatedAt,
            data.UpdatedAt
        );
    }

    private static readonly System.Diagnostics.ActivitySource ActivitySource = new("Industrial.Adam.Oee.Infrastructure");

    /// <summary>
    /// Data structure for mapping database rows to domain objects
    /// </summary>
    private sealed record EquipmentLineData(
        int Id,
        string LineId,
        string LineName,
        string AdamDeviceId,
        int AdamChannel,
        bool IsActive,
        DateTime CreatedAt,
        DateTime UpdatedAt
    );
}