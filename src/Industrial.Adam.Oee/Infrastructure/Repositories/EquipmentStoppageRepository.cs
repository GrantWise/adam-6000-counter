using System.Data;
using Dapper;
using Industrial.Adam.Oee.Domain.Entities;
using Industrial.Adam.Oee.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Oee.Infrastructure.Repositories;

/// <summary>
/// TimescaleDB repository for equipment stoppage persistence
/// Provides full CRUD operations for the equipment_stoppages table
/// Handles complex aggregate reconstruction with audit trail and classification data
/// </summary>
public sealed class EquipmentStoppageRepository : IEquipmentStoppageRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<EquipmentStoppageRepository> _logger;

    /// <summary>
    /// Constructor for equipment stoppage repository
    /// </summary>
    /// <param name="connectionFactory">Database connection factory</param>
    /// <param name="logger">Logger instance</param>
    public EquipmentStoppageRepository(
        IDbConnectionFactory connectionFactory,
        ILogger<EquipmentStoppageRepository> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get an equipment stoppage by its identifier
    /// </summary>
    /// <param name="id">Stoppage identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stoppage or null if not found</returns>
    public async Task<EquipmentStoppage?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("GetEquipmentStoppageById");
        activity?.SetTag("stoppageId", id);

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = @"
                SELECT 
                    id,
                    line_id,
                    work_order_id,
                    start_time,
                    end_time,
                    duration_minutes,
                    is_classified,
                    category_code,
                    subcode,
                    operator_comments,
                    classified_by,
                    classified_at,
                    auto_detected,
                    minimum_threshold_minutes,
                    created_at,
                    updated_at
                FROM equipment_stoppages 
                WHERE id = @id";

            _logger.LogDebug("Retrieving equipment stoppage with ID {StoppageId}", id);

            var stoppageData = await connection.QuerySingleOrDefaultAsync<EquipmentStoppageRowData>(sql, new { id });

            if (stoppageData == null)
            {
                _logger.LogDebug("Equipment stoppage with ID {StoppageId} not found", id);
                return null;
            }

            var stoppage = ReconstructStoppageFromRow(stoppageData);

            _logger.LogDebug("Retrieved equipment stoppage {StoppageId} for line {LineId}", id, stoppage.LineId);

            return stoppage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve equipment stoppage with ID {StoppageId}", id);
            throw;
        }
    }

    /// <summary>
    /// Get active stoppage for a specific equipment line
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Active stoppage or null if none active</returns>
    public async Task<EquipmentStoppage?> GetActiveByLineAsync(string lineId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lineId))
            throw new ArgumentException("Line ID cannot be null or empty", nameof(lineId));

        using var activity = ActivitySource.StartActivity("GetActiveEquipmentStoppageByLine");
        activity?.SetTag("lineId", lineId);

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = @"
                SELECT 
                    id,
                    line_id,
                    work_order_id,
                    start_time,
                    end_time,
                    duration_minutes,
                    is_classified,
                    category_code,
                    subcode,
                    operator_comments,
                    classified_by,
                    classified_at,
                    auto_detected,
                    minimum_threshold_minutes,
                    created_at,
                    updated_at
                FROM equipment_stoppages 
                WHERE line_id = @lineId 
                  AND end_time IS NULL
                ORDER BY start_time DESC
                LIMIT 1";

            _logger.LogDebug("Retrieving active equipment stoppage for line {LineId}", lineId);

            var stoppageData = await connection.QuerySingleOrDefaultAsync<EquipmentStoppageRowData>(sql, new { lineId });

            if (stoppageData == null)
            {
                _logger.LogDebug("No active equipment stoppage found for line {LineId}", lineId);
                return null;
            }

            var stoppage = ReconstructStoppageFromRow(stoppageData);

            _logger.LogDebug("Retrieved active equipment stoppage {StoppageId} for line {LineId}", stoppage.Id, lineId);

            return stoppage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve active equipment stoppage for line {LineId}", lineId);
            throw;
        }
    }

    /// <summary>
    /// Get stoppages for a specific equipment line within a time range
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="startTime">Start of time range</param>
    /// <param name="endTime">End of time range</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of stoppages for the line and time range</returns>
    public async Task<IEnumerable<EquipmentStoppage>> GetByLineAndTimeRangeAsync(
        string lineId,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lineId))
            throw new ArgumentException("Line ID cannot be null or empty", nameof(lineId));

        if (endTime <= startTime)
            throw new ArgumentException("End time must be after start time", nameof(endTime));

        using var activity = ActivitySource.StartActivity("GetEquipmentStoppagesByLineAndTimeRange");
        activity?.SetTag("lineId", lineId);
        activity?.SetTag("startTime", startTime.ToString("O"));
        activity?.SetTag("endTime", endTime.ToString("O"));

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = @"
                SELECT 
                    id,
                    line_id,
                    work_order_id,
                    start_time,
                    end_time,
                    duration_minutes,
                    is_classified,
                    category_code,
                    subcode,
                    operator_comments,
                    classified_by,
                    classified_at,
                    auto_detected,
                    minimum_threshold_minutes,
                    created_at,
                    updated_at
                FROM equipment_stoppages 
                WHERE line_id = @lineId 
                  AND start_time >= @startTime 
                  AND start_time <= @endTime
                ORDER BY start_time ASC";

            _logger.LogDebug("Retrieving equipment stoppages for line {LineId} between {StartTime} and {EndTime}",
                lineId, startTime, endTime);

            var stoppageData = await connection.QueryAsync<EquipmentStoppageRowData>(sql, new { lineId, startTime, endTime });
            var stoppageList = stoppageData.ToList();

            var stoppages = stoppageList.Select(ReconstructStoppageFromRow).ToList();

            _logger.LogDebug("Retrieved {StoppageCount} equipment stoppages for line {LineId} in time range",
                stoppages.Count, lineId);

            return stoppages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve equipment stoppages for line {LineId} in time range {StartTime} to {EndTime}",
                lineId, startTime, endTime);
            throw;
        }
    }

    /// <summary>
    /// Get stoppages for a specific work order
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of stoppages for the work order</returns>
    public async Task<IEnumerable<EquipmentStoppage>> GetByWorkOrderAsync(string workOrderId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workOrderId))
            throw new ArgumentException("Work order ID cannot be null or empty", nameof(workOrderId));

        using var activity = ActivitySource.StartActivity("GetEquipmentStoppagesByWorkOrder");
        activity?.SetTag("workOrderId", workOrderId);

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = @"
                SELECT 
                    id,
                    line_id,
                    work_order_id,
                    start_time,
                    end_time,
                    duration_minutes,
                    is_classified,
                    category_code,
                    subcode,
                    operator_comments,
                    classified_by,
                    classified_at,
                    auto_detected,
                    minimum_threshold_minutes,
                    created_at,
                    updated_at
                FROM equipment_stoppages 
                WHERE work_order_id = @workOrderId
                ORDER BY start_time ASC";

            _logger.LogDebug("Retrieving equipment stoppages for work order {WorkOrderId}", workOrderId);

            var stoppageData = await connection.QueryAsync<EquipmentStoppageRowData>(sql, new { workOrderId });
            var stoppageList = stoppageData.ToList();

            var stoppages = stoppageList.Select(ReconstructStoppageFromRow).ToList();

            _logger.LogDebug("Retrieved {StoppageCount} equipment stoppages for work order {WorkOrderId}",
                stoppages.Count, workOrderId);

            return stoppages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve equipment stoppages for work order {WorkOrderId}", workOrderId);
            throw;
        }
    }

    /// <summary>
    /// Get unclassified stoppages that require attention
    /// </summary>
    /// <param name="lineId">Optional line filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of unclassified stoppages requiring classification</returns>
    public async Task<IEnumerable<EquipmentStoppage>> GetUnclassifiedStoppagesAsync(string? lineId = null, CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("GetUnclassifiedEquipmentStoppages");
        activity?.SetTag("lineId", lineId ?? "all");

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            var sql = @"
                SELECT 
                    id,
                    line_id,
                    work_order_id,
                    start_time,
                    end_time,
                    duration_minutes,
                    is_classified,
                    category_code,
                    subcode,
                    operator_comments,
                    classified_by,
                    classified_at,
                    auto_detected,
                    minimum_threshold_minutes,
                    created_at,
                    updated_at
                FROM equipment_stoppages 
                WHERE is_classified = false";

            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(lineId))
            {
                sql += " AND line_id = @lineId";
                parameters.Add("lineId", lineId);
            }

            sql += " ORDER BY start_time ASC";

            _logger.LogDebug("Retrieving unclassified equipment stoppages" + (lineId != null ? $" for line {lineId}" : ""));

            var stoppageData = await connection.QueryAsync<EquipmentStoppageRowData>(sql, parameters);
            var stoppageList = stoppageData.ToList();

            var stoppages = stoppageList.Select(ReconstructStoppageFromRow).ToList();

            _logger.LogDebug("Retrieved {StoppageCount} unclassified equipment stoppages", stoppages.Count);

            return stoppages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve unclassified equipment stoppages");
            throw;
        }
    }

    /// <summary>
    /// Get stoppages requiring classification (over threshold and unclassified)
    /// </summary>
    /// <param name="lineId">Optional line filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of stoppages requiring classification</returns>
    public async Task<IEnumerable<EquipmentStoppage>> GetStoppagesRequiringClassificationAsync(string? lineId = null, CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("GetStoppagesRequiringClassification");
        activity?.SetTag("lineId", lineId ?? "all");

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            var sql = @"
                SELECT 
                    id,
                    line_id,
                    work_order_id,
                    start_time,
                    end_time,
                    duration_minutes,
                    is_classified,
                    category_code,
                    subcode,
                    operator_comments,
                    classified_by,
                    classified_at,
                    auto_detected,
                    minimum_threshold_minutes,
                    created_at,
                    updated_at
                FROM equipment_stoppages 
                WHERE is_classified = false 
                  AND (
                    (end_time IS NOT NULL AND duration_minutes >= minimum_threshold_minutes)
                    OR 
                    (end_time IS NULL AND EXTRACT(EPOCH FROM (NOW() - start_time))/60 >= minimum_threshold_minutes)
                  )";

            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(lineId))
            {
                sql += " AND line_id = @lineId";
                parameters.Add("lineId", lineId);
            }

            sql += " ORDER BY start_time ASC";

            _logger.LogDebug("Retrieving stoppages requiring classification" + (lineId != null ? $" for line {lineId}" : ""));

            var stoppageData = await connection.QueryAsync<EquipmentStoppageRowData>(sql, parameters);
            var stoppageList = stoppageData.ToList();

            var stoppages = stoppageList.Select(ReconstructStoppageFromRow).ToList();

            _logger.LogDebug("Retrieved {StoppageCount} stoppages requiring classification", stoppages.Count);

            return stoppages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve stoppages requiring classification");
            throw;
        }
    }

    /// <summary>
    /// Get stoppages by classification
    /// </summary>
    /// <param name="categoryCode">Category code filter</param>
    /// <param name="subcode">Optional subcode filter</param>
    /// <param name="startTime">Optional start time filter</param>
    /// <param name="endTime">Optional end time filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of stoppages with the specified classification</returns>
    public async Task<IEnumerable<EquipmentStoppage>> GetByClassificationAsync(
        string categoryCode,
        string? subcode = null,
        DateTime? startTime = null,
        DateTime? endTime = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(categoryCode))
            throw new ArgumentException("Category code cannot be null or empty", nameof(categoryCode));

        using var activity = ActivitySource.StartActivity("GetEquipmentStoppagesByClassification");
        activity?.SetTag("categoryCode", categoryCode);
        activity?.SetTag("subcode", subcode ?? "all");

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            var sql = @"
                SELECT 
                    id,
                    line_id,
                    work_order_id,
                    start_time,
                    end_time,
                    duration_minutes,
                    is_classified,
                    category_code,
                    subcode,
                    operator_comments,
                    classified_by,
                    classified_at,
                    auto_detected,
                    minimum_threshold_minutes,
                    created_at,
                    updated_at
                FROM equipment_stoppages 
                WHERE category_code = @categoryCode";

            var parameters = new DynamicParameters();
            parameters.Add("categoryCode", categoryCode);

            if (!string.IsNullOrWhiteSpace(subcode))
            {
                sql += " AND subcode = @subcode";
                parameters.Add("subcode", subcode);
            }

            if (startTime.HasValue)
            {
                sql += " AND start_time >= @startTime";
                parameters.Add("startTime", startTime.Value);
            }

            if (endTime.HasValue)
            {
                sql += " AND start_time <= @endTime";
                parameters.Add("endTime", endTime.Value);
            }

            sql += " ORDER BY start_time ASC";

            _logger.LogDebug("Retrieving equipment stoppages for classification {CategoryCode}-{Subcode}",
                categoryCode, subcode ?? "all");

            var stoppageData = await connection.QueryAsync<EquipmentStoppageRowData>(sql, parameters);
            var stoppageList = stoppageData.ToList();

            var stoppages = stoppageList.Select(ReconstructStoppageFromRow).ToList();

            _logger.LogDebug("Retrieved {StoppageCount} equipment stoppages for classification {CategoryCode}-{Subcode}",
                stoppages.Count, categoryCode, subcode ?? "all");

            return stoppages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve equipment stoppages for classification {CategoryCode}-{Subcode}",
                categoryCode, subcode ?? "all");
            throw;
        }
    }

    /// <summary>
    /// Get short stops (below threshold) for analysis
    /// </summary>
    /// <param name="lineId">Optional line filter</param>
    /// <param name="startTime">Start time filter</param>
    /// <param name="endTime">End time filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of short stops</returns>
    public async Task<IEnumerable<EquipmentStoppage>> GetShortStopsAsync(
        string? lineId = null,
        DateTime? startTime = null,
        DateTime? endTime = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("GetShortStops");
        activity?.SetTag("lineId", lineId ?? "all");

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            var sql = @"
                SELECT 
                    id,
                    line_id,
                    work_order_id,
                    start_time,
                    end_time,
                    duration_minutes,
                    is_classified,
                    category_code,
                    subcode,
                    operator_comments,
                    classified_by,
                    classified_at,
                    auto_detected,
                    minimum_threshold_minutes,
                    created_at,
                    updated_at
                FROM equipment_stoppages 
                WHERE end_time IS NOT NULL 
                  AND duration_minutes < minimum_threshold_minutes";

            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(lineId))
            {
                sql += " AND line_id = @lineId";
                parameters.Add("lineId", lineId);
            }

            if (startTime.HasValue)
            {
                sql += " AND start_time >= @startTime";
                parameters.Add("startTime", startTime.Value);
            }

            if (endTime.HasValue)
            {
                sql += " AND start_time <= @endTime";
                parameters.Add("endTime", endTime.Value);
            }

            sql += " ORDER BY start_time ASC";

            _logger.LogDebug("Retrieving short stops" + (lineId != null ? $" for line {lineId}" : ""));

            var stoppageData = await connection.QueryAsync<EquipmentStoppageRowData>(sql, parameters);
            var stoppageList = stoppageData.ToList();

            var stoppages = stoppageList.Select(ReconstructStoppageFromRow).ToList();

            _logger.LogDebug("Retrieved {StoppageCount} short stops", stoppages.Count);

            return stoppages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve short stops");
            throw;
        }
    }

    /// <summary>
    /// Get long stops (over threshold) for analysis
    /// </summary>
    /// <param name="lineId">Optional line filter</param>
    /// <param name="startTime">Start time filter</param>
    /// <param name="endTime">End time filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of long stops</returns>
    public async Task<IEnumerable<EquipmentStoppage>> GetLongStopsAsync(
        string? lineId = null,
        DateTime? startTime = null,
        DateTime? endTime = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("GetLongStops");
        activity?.SetTag("lineId", lineId ?? "all");

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            var sql = @"
                SELECT 
                    id,
                    line_id,
                    work_order_id,
                    start_time,
                    end_time,
                    duration_minutes,
                    is_classified,
                    category_code,
                    subcode,
                    operator_comments,
                    classified_by,
                    classified_at,
                    auto_detected,
                    minimum_threshold_minutes,
                    created_at,
                    updated_at
                FROM equipment_stoppages 
                WHERE end_time IS NOT NULL 
                  AND duration_minutes >= minimum_threshold_minutes";

            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(lineId))
            {
                sql += " AND line_id = @lineId";
                parameters.Add("lineId", lineId);
            }

            if (startTime.HasValue)
            {
                sql += " AND start_time >= @startTime";
                parameters.Add("startTime", startTime.Value);
            }

            if (endTime.HasValue)
            {
                sql += " AND start_time <= @endTime";
                parameters.Add("endTime", endTime.Value);
            }

            sql += " ORDER BY start_time ASC";

            _logger.LogDebug("Retrieving long stops" + (lineId != null ? $" for line {lineId}" : ""));

            var stoppageData = await connection.QueryAsync<EquipmentStoppageRowData>(sql, parameters);
            var stoppageList = stoppageData.ToList();

            var stoppages = stoppageList.Select(ReconstructStoppageFromRow).ToList();

            _logger.LogDebug("Retrieved {StoppageCount} long stops", stoppages.Count);

            return stoppages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve long stops");
            throw;
        }
    }

    /// <summary>
    /// Create a new equipment stoppage
    /// </summary>
    /// <param name="stoppage">Stoppage to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created stoppage identifier</returns>
    public async Task<int> CreateAsync(EquipmentStoppage stoppage, CancellationToken cancellationToken = default)
    {
        if (stoppage == null)
            throw new ArgumentNullException(nameof(stoppage));

        using var activity = ActivitySource.StartActivity("CreateEquipmentStoppage");
        activity?.SetTag("lineId", stoppage.LineId);

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = @"
                INSERT INTO equipment_stoppages (
                    line_id,
                    work_order_id,
                    start_time,
                    end_time,
                    duration_minutes,
                    is_classified,
                    category_code,
                    subcode,
                    operator_comments,
                    classified_by,
                    classified_at,
                    auto_detected,
                    minimum_threshold_minutes,
                    created_at,
                    updated_at
                ) VALUES (
                    @LineId,
                    @WorkOrderId,
                    @StartTime,
                    @EndTime,
                    @DurationMinutes,
                    @IsClassified,
                    @CategoryCode,
                    @Subcode,
                    @OperatorComments,
                    @ClassifiedBy,
                    @ClassifiedAt,
                    @AutoDetected,
                    @MinimumThresholdMinutes,
                    @CreatedAt,
                    @UpdatedAt
                ) RETURNING id";

            var parameters = new
            {
                LineId = stoppage.LineId,
                WorkOrderId = stoppage.WorkOrderId,
                StartTime = stoppage.StartTime,
                EndTime = stoppage.EndTime,
                DurationMinutes = stoppage.DurationMinutes,
                IsClassified = stoppage.IsClassified,
                CategoryCode = stoppage.CategoryCode,
                Subcode = stoppage.Subcode,
                OperatorComments = stoppage.OperatorComments,
                ClassifiedBy = stoppage.ClassifiedBy,
                ClassifiedAt = stoppage.ClassifiedAt,
                AutoDetected = stoppage.AutoDetected,
                MinimumThresholdMinutes = stoppage.MinimumThresholdMinutes,
                CreatedAt = stoppage.CreatedAt,
                UpdatedAt = stoppage.UpdatedAt
            };

            _logger.LogDebug("Creating equipment stoppage for line {LineId} at {StartTime}",
                stoppage.LineId, stoppage.StartTime);

            var id = await connection.QuerySingleAsync<int>(sql, parameters);

            _logger.LogInformation("Created equipment stoppage {StoppageId} for line {LineId}", id, stoppage.LineId);

            return id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create equipment stoppage for line {LineId}", stoppage.LineId);
            throw;
        }
    }

    /// <summary>
    /// Update an existing equipment stoppage
    /// </summary>
    /// <param name="stoppage">Stoppage to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if updated successfully, false if not found</returns>
    public async Task<bool> UpdateAsync(EquipmentStoppage stoppage, CancellationToken cancellationToken = default)
    {
        if (stoppage == null)
            throw new ArgumentNullException(nameof(stoppage));

        using var activity = ActivitySource.StartActivity("UpdateEquipmentStoppage");
        activity?.SetTag("stoppageId", stoppage.Id);
        activity?.SetTag("lineId", stoppage.LineId);

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = @"
                UPDATE equipment_stoppages SET
                    work_order_id = @WorkOrderId,
                    end_time = @EndTime,
                    duration_minutes = @DurationMinutes,
                    is_classified = @IsClassified,
                    category_code = @CategoryCode,
                    subcode = @Subcode,
                    operator_comments = @OperatorComments,
                    classified_by = @ClassifiedBy,
                    classified_at = @ClassifiedAt,
                    updated_at = @UpdatedAt
                WHERE id = @Id";

            var parameters = new
            {
                Id = stoppage.Id,
                WorkOrderId = stoppage.WorkOrderId,
                EndTime = stoppage.EndTime,
                DurationMinutes = stoppage.DurationMinutes,
                IsClassified = stoppage.IsClassified,
                CategoryCode = stoppage.CategoryCode,
                Subcode = stoppage.Subcode,
                OperatorComments = stoppage.OperatorComments,
                ClassifiedBy = stoppage.ClassifiedBy,
                ClassifiedAt = stoppage.ClassifiedAt,
                UpdatedAt = DateTime.UtcNow
            };

            _logger.LogDebug("Updating equipment stoppage {StoppageId}", stoppage.Id);

            var rowsAffected = await connection.ExecuteAsync(sql, parameters);
            var wasUpdated = rowsAffected > 0;

            if (wasUpdated)
            {
                _logger.LogInformation("Updated equipment stoppage {StoppageId}", stoppage.Id);
            }
            else
            {
                _logger.LogWarning("Equipment stoppage {StoppageId} not found for update", stoppage.Id);
            }

            return wasUpdated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update equipment stoppage {StoppageId}", stoppage.Id);
            throw;
        }
    }

    /// <summary>
    /// Delete an equipment stoppage
    /// </summary>
    /// <param name="id">Stoppage identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully, false if not found</returns>
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("DeleteEquipmentStoppage");
        activity?.SetTag("stoppageId", id);

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = "DELETE FROM equipment_stoppages WHERE id = @id";

            _logger.LogDebug("Deleting equipment stoppage {StoppageId}", id);

            var rowsAffected = await connection.ExecuteAsync(sql, new { id });
            var wasDeleted = rowsAffected > 0;

            if (wasDeleted)
            {
                _logger.LogInformation("Deleted equipment stoppage {StoppageId}", id);
            }
            else
            {
                _logger.LogWarning("Equipment stoppage {StoppageId} not found for deletion", id);
            }

            return wasDeleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete equipment stoppage {StoppageId}", id);
            throw;
        }
    }

    /// <summary>
    /// Check if a stoppage exists
    /// </summary>
    /// <param name="id">Stoppage identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if stoppage exists, false otherwise</returns>
    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = "SELECT EXISTS(SELECT 1 FROM equipment_stoppages WHERE id = @id)";

            var exists = await connection.QuerySingleAsync<bool>(sql, new { id });

            _logger.LogDebug("Equipment stoppage {StoppageId} exists: {Exists}", id, exists);

            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if equipment stoppage {StoppageId} exists", id);
            throw;
        }
    }

    /// <summary>
    /// Get stoppage statistics for a time period
    /// </summary>
    /// <param name="startTime">Start of time period</param>
    /// <param name="endTime">End of time period</param>
    /// <param name="lineId">Optional line filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stoppage statistics</returns>
    public async Task<StoppageStatistics> GetStatisticsAsync(
        DateTime startTime,
        DateTime endTime,
        string? lineId = null,
        CancellationToken cancellationToken = default)
    {
        if (endTime <= startTime)
            throw new ArgumentException("End time must be after start time", nameof(endTime));

        using var activity = ActivitySource.StartActivity("GetStoppageStatistics");
        activity?.SetTag("lineId", lineId ?? "all");

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            var sql = @"
                SELECT 
                    COUNT(*) as TotalStoppages,
                    COUNT(CASE WHEN is_classified = true THEN 1 END) as ClassifiedStoppages,
                    COUNT(CASE WHEN is_classified = false THEN 1 END) as UnclassifiedStoppages,
                    COUNT(CASE WHEN duration_minutes < minimum_threshold_minutes THEN 1 END) as ShortStops,
                    COUNT(CASE WHEN duration_minutes >= minimum_threshold_minutes THEN 1 END) as LongStops,
                    COALESCE(SUM(duration_minutes), 0) as TotalDowntimeMinutes,
                    COALESCE(AVG(duration_minutes), 0) as AverageStoppageDuration,
                    CASE 
                        WHEN COUNT(*) > 0 THEN 
                            ROUND((COUNT(CASE WHEN is_classified = true THEN 1 END) * 100.0) / COUNT(*), 2)
                        ELSE 0 
                    END as ClassificationRate,
                    COUNT(CASE WHEN auto_detected = true THEN 1 END) as AutoDetectedStoppages,
                    COUNT(CASE WHEN auto_detected = false THEN 1 END) as ManualStoppages
                FROM equipment_stoppages 
                WHERE start_time >= @startTime 
                  AND start_time <= @endTime 
                  AND end_time IS NOT NULL";

            var parameters = new DynamicParameters();
            parameters.Add("startTime", startTime);
            parameters.Add("endTime", endTime);

            if (!string.IsNullOrWhiteSpace(lineId))
            {
                sql += " AND line_id = @lineId";
                parameters.Add("lineId", lineId);
            }

            _logger.LogDebug("Calculating stoppage statistics for period {StartTime} to {EndTime}" +
                (lineId != null ? $" for line {lineId}" : ""), startTime, endTime);

            var result = await connection.QuerySingleAsync(sql, parameters);

            var statistics = new StoppageStatistics(
                TotalStoppages: result.TotalStoppages,
                ClassifiedStoppages: result.ClassifiedStoppages,
                UnclassifiedStoppages: result.UnclassifiedStoppages,
                ShortStops: result.ShortStops,
                LongStops: result.LongStops,
                TotalDowntimeMinutes: result.TotalDowntimeMinutes,
                AverageStoppageDuration: result.AverageStoppageDuration,
                ClassificationRate: result.ClassificationRate,
                AutoDetectedStoppages: result.AutoDetectedStoppages,
                ManualStoppages: result.ManualStoppages
            );

            _logger.LogDebug("Calculated stoppage statistics: {TotalStoppages} total, {ClassifiedStoppages} classified",
                statistics.TotalStoppages, statistics.ClassifiedStoppages);

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate stoppage statistics");
            throw;
        }
    }

    /// <summary>
    /// Search stoppages by criteria
    /// </summary>
    /// <param name="criteria">Search criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of matching stoppages</returns>
    public async Task<IEnumerable<EquipmentStoppage>> SearchAsync(StoppageSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        if (criteria == null)
            throw new ArgumentNullException(nameof(criteria));

        using var activity = ActivitySource.StartActivity("SearchEquipmentStoppages");

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            var sql = @"
                SELECT 
                    id,
                    line_id,
                    work_order_id,
                    start_time,
                    end_time,
                    duration_minutes,
                    is_classified,
                    category_code,
                    subcode,
                    operator_comments,
                    classified_by,
                    classified_at,
                    auto_detected,
                    minimum_threshold_minutes,
                    created_at,
                    updated_at
                FROM equipment_stoppages 
                WHERE 1=1";

            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(criteria.LineId))
            {
                sql += " AND line_id = @LineId";
                parameters.Add("LineId", criteria.LineId);
            }

            if (!string.IsNullOrWhiteSpace(criteria.WorkOrderId))
            {
                sql += " AND work_order_id = @WorkOrderId";
                parameters.Add("WorkOrderId", criteria.WorkOrderId);
            }

            if (criteria.StartTime.HasValue)
            {
                sql += " AND start_time >= @StartTime";
                parameters.Add("StartTime", criteria.StartTime.Value);
            }

            if (criteria.EndTime.HasValue)
            {
                sql += " AND start_time <= @EndTime";
                parameters.Add("EndTime", criteria.EndTime.Value);
            }

            if (criteria.IsClassified.HasValue)
            {
                sql += " AND is_classified = @IsClassified";
                parameters.Add("IsClassified", criteria.IsClassified.Value);
            }

            if (!string.IsNullOrWhiteSpace(criteria.CategoryCode))
            {
                sql += " AND category_code = @CategoryCode";
                parameters.Add("CategoryCode", criteria.CategoryCode);
            }

            if (!string.IsNullOrWhiteSpace(criteria.Subcode))
            {
                sql += " AND subcode = @Subcode";
                parameters.Add("Subcode", criteria.Subcode);
            }

            if (criteria.AutoDetected.HasValue)
            {
                sql += " AND auto_detected = @AutoDetected";
                parameters.Add("AutoDetected", criteria.AutoDetected.Value);
            }

            if (criteria.MinDurationMinutes.HasValue)
            {
                sql += " AND duration_minutes >= @MinDurationMinutes";
                parameters.Add("MinDurationMinutes", criteria.MinDurationMinutes.Value);
            }

            if (criteria.MaxDurationMinutes.HasValue)
            {
                sql += " AND duration_minutes <= @MaxDurationMinutes";
                parameters.Add("MaxDurationMinutes", criteria.MaxDurationMinutes.Value);
            }

            if (criteria.RequiresClassification.HasValue && criteria.RequiresClassification.Value)
            {
                sql += @" AND is_classified = false 
                         AND (
                           (end_time IS NOT NULL AND duration_minutes >= minimum_threshold_minutes)
                           OR 
                           (end_time IS NULL AND EXTRACT(EPOCH FROM (NOW() - start_time))/60 >= minimum_threshold_minutes)
                         )";
            }

            sql += " ORDER BY start_time ASC";

            _logger.LogDebug("Searching equipment stoppages with criteria");

            var stoppageData = await connection.QueryAsync<EquipmentStoppageRowData>(sql, parameters);
            var stoppageList = stoppageData.ToList();

            var stoppages = stoppageList.Select(ReconstructStoppageFromRow).ToList();

            _logger.LogDebug("Found {StoppageCount} equipment stoppages matching criteria", stoppages.Count);

            return stoppages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search equipment stoppages");
            throw;
        }
    }

    /// <summary>
    /// Get classification trends for analysis
    /// </summary>
    /// <param name="startTime">Start time</param>
    /// <param name="endTime">End time</param>
    /// <param name="lineId">Optional line filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of classification trends</returns>
    public async Task<IEnumerable<ClassificationTrend>> GetClassificationTrendsAsync(
        DateTime startTime,
        DateTime endTime,
        string? lineId = null,
        CancellationToken cancellationToken = default)
    {
        if (endTime <= startTime)
            throw new ArgumentException("End time must be after start time", nameof(endTime));

        using var activity = ActivitySource.StartActivity("GetClassificationTrends");
        activity?.SetTag("lineId", lineId ?? "all");

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            var sql = @"
                SELECT 
                    DATE_TRUNC('day', start_time) as Period,
                    category_code as CategoryCode,
                    src.category_name as CategoryName,
                    subcode as Subcode,
                    srs.subcode_name as SubcodeName,
                    COUNT(*) as OccurrenceCount,
                    COALESCE(SUM(duration_minutes), 0) as TotalDurationMinutes,
                    COALESCE(AVG(duration_minutes), 0) as AverageDurationMinutes
                FROM equipment_stoppages es
                LEFT JOIN stoppage_reason_categories src ON es.category_code = src.category_code
                LEFT JOIN stoppage_reason_subcodes srs ON es.category_code = srs.category_code AND es.subcode = srs.subcode
                WHERE es.is_classified = true
                  AND es.start_time >= @startTime 
                  AND es.start_time <= @endTime";

            var parameters = new DynamicParameters();
            parameters.Add("startTime", startTime);
            parameters.Add("endTime", endTime);

            if (!string.IsNullOrWhiteSpace(lineId))
            {
                sql += " AND es.line_id = @lineId";
                parameters.Add("lineId", lineId);
            }

            sql += @" GROUP BY DATE_TRUNC('day', start_time), category_code, src.category_name, subcode, srs.subcode_name
                     ORDER BY Period, category_code, subcode";

            _logger.LogDebug("Retrieving classification trends for period {StartTime} to {EndTime}" +
                (lineId != null ? $" for line {lineId}" : ""), startTime, endTime);

            var trendsData = await connection.QueryAsync(sql, parameters);

            var trends = trendsData.Select(row => new ClassificationTrend(
                Period: row.Period,
                CategoryCode: row.CategoryCode ?? "UNKNOWN",
                CategoryName: row.CategoryName ?? "Unknown Category",
                Subcode: row.Subcode ?? "UNKNOWN",
                SubcodeName: row.SubcodeName ?? "Unknown Subcode",
                OccurrenceCount: row.OccurrenceCount,
                TotalDurationMinutes: row.TotalDurationMinutes,
                AverageDurationMinutes: row.AverageDurationMinutes
            )).ToList();

            _logger.LogDebug("Retrieved {TrendCount} classification trend records", trends.Count);

            return trends;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve classification trends");
            throw;
        }
    }

    /// <summary>
    /// Get top stoppage reasons for a time period
    /// </summary>
    /// <param name="startTime">Start time</param>
    /// <param name="endTime">End time</param>
    /// <param name="lineId">Optional line filter</param>
    /// <param name="topCount">Number of top reasons to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of top stoppage reasons</returns>
    public async Task<IEnumerable<StoppageReasonSummary>> GetTopStoppageReasonsAsync(
        DateTime startTime,
        DateTime endTime,
        string? lineId = null,
        int topCount = 10,
        CancellationToken cancellationToken = default)
    {
        if (endTime <= startTime)
            throw new ArgumentException("End time must be after start time", nameof(endTime));

        if (topCount <= 0)
            throw new ArgumentException("Top count must be positive", nameof(topCount));

        using var activity = ActivitySource.StartActivity("GetTopStoppageReasons");
        activity?.SetTag("lineId", lineId ?? "all");
        activity?.SetTag("topCount", topCount);

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            var sql = @"
                WITH total_downtime AS (
                    SELECT COALESCE(SUM(duration_minutes), 0) as total
                    FROM equipment_stoppages 
                    WHERE is_classified = true
                      AND start_time >= @startTime 
                      AND start_time <= @endTime";

            if (!string.IsNullOrWhiteSpace(lineId))
            {
                sql += " AND line_id = @lineId";
            }

            sql += @"
                )
                SELECT 
                    es.category_code as CategoryCode,
                    src.category_name as CategoryName,
                    es.subcode as Subcode,
                    srs.subcode_name as SubcodeName,
                    CONCAT(es.category_code, '-', es.subcode) as FullReasonCode,
                    COUNT(*) as OccurrenceCount,
                    COALESCE(SUM(es.duration_minutes), 0) as TotalDurationMinutes,
                    COALESCE(AVG(es.duration_minutes), 0) as AverageDurationMinutes,
                    CASE 
                        WHEN td.total > 0 THEN 
                            ROUND((COALESCE(SUM(es.duration_minutes), 0) * 100.0) / td.total, 2)
                        ELSE 0 
                    END as PercentageOfTotalDowntime
                FROM equipment_stoppages es
                LEFT JOIN stoppage_reason_categories src ON es.category_code = src.category_code
                LEFT JOIN stoppage_reason_subcodes srs ON es.category_code = srs.category_code AND es.subcode = srs.subcode
                CROSS JOIN total_downtime td
                WHERE es.is_classified = true
                  AND es.start_time >= @startTime 
                  AND es.start_time <= @endTime";

            var parameters = new DynamicParameters();
            parameters.Add("startTime", startTime);
            parameters.Add("endTime", endTime);
            parameters.Add("topCount", topCount);

            if (!string.IsNullOrWhiteSpace(lineId))
            {
                sql += " AND es.line_id = @lineId";
                parameters.Add("lineId", lineId);
            }

            sql += @" GROUP BY es.category_code, src.category_name, es.subcode, srs.subcode_name, td.total
                     ORDER BY TotalDurationMinutes DESC
                     LIMIT @topCount";

            _logger.LogDebug("Retrieving top {TopCount} stoppage reasons for period {StartTime} to {EndTime}" +
                (lineId != null ? $" for line {lineId}" : ""), topCount, startTime, endTime);

            var reasonsData = await connection.QueryAsync(sql, parameters);

            var reasons = reasonsData.Select(row => new StoppageReasonSummary(
                CategoryCode: row.CategoryCode ?? "UNKNOWN",
                CategoryName: row.CategoryName ?? "Unknown Category",
                Subcode: row.Subcode ?? "UNKNOWN",
                SubcodeName: row.SubcodeName ?? "Unknown Subcode",
                FullReasonCode: row.FullReasonCode ?? "UNKNOWN-UNKNOWN",
                OccurrenceCount: row.OccurrenceCount,
                TotalDurationMinutes: row.TotalDurationMinutes,
                AverageDurationMinutes: row.AverageDurationMinutes,
                PercentageOfTotalDowntime: row.PercentageOfTotalDowntime
            )).ToList();

            _logger.LogDebug("Retrieved {ReasonCount} top stoppage reasons", reasons.Count);

            return reasons;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve top stoppage reasons");
            throw;
        }
    }

    /// <summary>
    /// Bulk update stoppages for work order association
    /// </summary>
    /// <param name="lineId">Line identifier</param>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="startTime">Start time for association</param>
    /// <param name="endTime">End time for association</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of stoppages updated</returns>
    public async Task<int> BulkAssociateWithWorkOrderAsync(
        string lineId,
        string workOrderId,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lineId))
            throw new ArgumentException("Line ID cannot be null or empty", nameof(lineId));

        if (string.IsNullOrWhiteSpace(workOrderId))
            throw new ArgumentException("Work order ID cannot be null or empty", nameof(workOrderId));

        if (endTime <= startTime)
            throw new ArgumentException("End time must be after start time", nameof(endTime));

        using var activity = ActivitySource.StartActivity("BulkAssociateStoppagesWithWorkOrder");
        activity?.SetTag("lineId", lineId);
        activity?.SetTag("workOrderId", workOrderId);

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = @"
                UPDATE equipment_stoppages 
                SET work_order_id = @workOrderId,
                    updated_at = NOW()
                WHERE line_id = @lineId 
                  AND start_time >= @startTime 
                  AND start_time <= @endTime";

            var parameters = new
            {
                lineId,
                workOrderId,
                startTime,
                endTime
            };

            _logger.LogDebug("Bulk associating stoppages with work order {WorkOrderId} for line {LineId} between {StartTime} and {EndTime}",
                workOrderId, lineId, startTime, endTime);

            var rowsAffected = await connection.ExecuteAsync(sql, parameters);

            _logger.LogInformation("Associated {StoppageCount} stoppages with work order {WorkOrderId} for line {LineId}",
                rowsAffected, workOrderId, lineId);

            return rowsAffected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bulk associate stoppages with work order {WorkOrderId} for line {LineId}",
                workOrderId, lineId);
            throw;
        }
    }

    /// <summary>
    /// Reconstruct EquipmentStoppage aggregate from database row
    /// </summary>
    /// <param name="row">Database row data</param>
    /// <returns>Reconstructed EquipmentStoppage</returns>
    private static EquipmentStoppage ReconstructStoppageFromRow(EquipmentStoppageRowData row)
    {
        return new EquipmentStoppage(
            id: row.Id,
            lineId: row.LineId,
            workOrderId: row.WorkOrderId,
            startTime: row.StartTime,
            endTime: row.EndTime,
            durationMinutes: row.DurationMinutes,
            isClassified: row.IsClassified,
            categoryCode: row.CategoryCode,
            subcode: row.Subcode,
            operatorComments: row.OperatorComments,
            classifiedBy: row.ClassifiedBy,
            classifiedAt: row.ClassifiedAt,
            autoDetected: row.AutoDetected,
            minimumThresholdMinutes: row.MinimumThresholdMinutes,
            createdAt: row.CreatedAt,
            updatedAt: row.UpdatedAt
        );
    }

    private static readonly System.Diagnostics.ActivitySource ActivitySource = new("Industrial.Adam.Oee.Infrastructure");

    /// <summary>
    /// Data structure for mapping equipment stoppage database rows
    /// </summary>
    private sealed record EquipmentStoppageRowData(
        int Id,
        string LineId,
        string? WorkOrderId,
        DateTime StartTime,
        DateTime? EndTime,
        decimal? DurationMinutes,
        bool IsClassified,
        string? CategoryCode,
        string? Subcode,
        string? OperatorComments,
        string? ClassifiedBy,
        DateTime? ClassifiedAt,
        bool AutoDetected,
        int MinimumThresholdMinutes,
        DateTime CreatedAt,
        DateTime UpdatedAt
    );
}
