using System.Data;
using Dapper;
using Industrial.Adam.Oee.Domain.Entities;
using Industrial.Adam.Oee.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Oee.Infrastructure.Repositories;

/// <summary>
/// TimescaleDB repository for work order persistence
/// Provides full CRUD operations for the OEE-specific work_orders table
/// </summary>
public sealed class WorkOrderRepository : IWorkOrderRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<WorkOrderRepository> _logger;

    /// <summary>
    /// Constructor for work order repository
    /// </summary>
    /// <param name="connectionFactory">Database connection factory</param>
    /// <param name="logger">Logger instance</param>
    public WorkOrderRepository(
        IDbConnectionFactory connectionFactory,
        ILogger<WorkOrderRepository> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get a work order by its identifier
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Work order or null if not found</returns>
    public async Task<WorkOrder?> GetByIdAsync(string workOrderId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workOrderId))
            throw new ArgumentException("Work order ID cannot be null or empty", nameof(workOrderId));

        using var activity = ActivitySource.StartActivity("GetWorkOrderById");
        activity?.SetTag("workOrderId", workOrderId);

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = @"
                SELECT 
                    work_order_id as Id,
                    work_order_description as WorkOrderDescription,
                    product_id as ProductId,
                    product_description as ProductDescription,
                    planned_quantity as PlannedQuantity,
                    unit_of_measure as UnitOfMeasure,
                    scheduled_start_time as ScheduledStartTime,
                    scheduled_end_time as ScheduledEndTime,
                    resource_reference as ResourceReference,
                    status as Status,
                    actual_quantity_good as ActualQuantityGood,
                    actual_quantity_scrap as ActualQuantityScrap,
                    actual_start_time as ActualStartTime,
                    actual_end_time as ActualEndTime,
                    created_at as CreatedAt,
                    updated_at as UpdatedAt
                FROM work_orders 
                WHERE work_order_id = @workOrderId";

            _logger.LogDebug("Retrieving work order {WorkOrderId}", workOrderId);

            var workOrderData = await connection.QuerySingleOrDefaultAsync<WorkOrderData>(sql, new { workOrderId });

            if (workOrderData == null)
            {
                _logger.LogWarning("Work order {WorkOrderId} not found", workOrderId);
                return null;
            }

            var workOrder = MapToWorkOrder(workOrderData);

            _logger.LogDebug("Retrieved work order {WorkOrderId} with status {Status}",
                workOrderId, workOrder.Status);

            return workOrder;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve work order {WorkOrderId}", workOrderId);
            throw;
        }
    }

    /// <summary>
    /// Get the active work order for a specific device
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Active work order or null if none active</returns>
    public async Task<WorkOrder?> GetActiveByDeviceAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("Device ID cannot be null or empty", nameof(deviceId));

        using var activity = ActivitySource.StartActivity("GetActiveWorkOrderByDevice");
        activity?.SetTag("deviceId", deviceId);

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = @"
                SELECT 
                    work_order_id as Id,
                    work_order_description as WorkOrderDescription,
                    product_id as ProductId,
                    product_description as ProductDescription,
                    planned_quantity as PlannedQuantity,
                    unit_of_measure as UnitOfMeasure,
                    scheduled_start_time as ScheduledStartTime,
                    scheduled_end_time as ScheduledEndTime,
                    resource_reference as ResourceReference,
                    status as Status,
                    actual_quantity_good as ActualQuantityGood,
                    actual_quantity_scrap as ActualQuantityScrap,
                    actual_start_time as ActualStartTime,
                    actual_end_time as ActualEndTime,
                    created_at as CreatedAt,
                    updated_at as UpdatedAt
                FROM work_orders 
                WHERE resource_reference = @deviceId 
                  AND status IN ('Active', 'Paused')
                ORDER BY created_at DESC
                LIMIT 1";

            _logger.LogDebug("Retrieving active work order for device {DeviceId}", deviceId);

            var workOrderData = await connection.QuerySingleOrDefaultAsync<WorkOrderData>(sql, new { deviceId });

            if (workOrderData == null)
            {
                _logger.LogDebug("No active work order found for device {DeviceId}", deviceId);
                return null;
            }

            var workOrder = MapToWorkOrder(workOrderData);

            _logger.LogInformation("Found active work order {WorkOrderId} for device {DeviceId} with status {Status}",
                workOrder.Id, deviceId, workOrder.Status);

            return workOrder;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve active work order for device {DeviceId}", deviceId);
            throw;
        }
    }

    /// <summary>
    /// Get the active work order for a specific equipment line
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Active work order or null if none active</returns>
    public async Task<WorkOrder?> GetActiveByLineAsync(string lineId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lineId))
            throw new ArgumentException("Line ID cannot be null or empty", nameof(lineId));

        using var activity = ActivitySource.StartActivity("GetActiveWorkOrderByLine");
        activity?.SetTag("lineId", lineId);

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            // Join with equipment_lines to find the active work order for the line
            const string sql = @"
                SELECT 
                    wo.work_order_id as Id,
                    wo.work_order_description as WorkOrderDescription,
                    wo.product_id as ProductId,
                    wo.product_description as ProductDescription,
                    wo.planned_quantity as PlannedQuantity,
                    wo.unit_of_measure as UnitOfMeasure,
                    wo.scheduled_start_time as ScheduledStartTime,
                    wo.scheduled_end_time as ScheduledEndTime,
                    wo.resource_reference as ResourceReference,
                    wo.status as Status,
                    wo.actual_quantity_good as ActualQuantityGood,
                    wo.actual_quantity_scrap as ActualQuantityScrap,
                    wo.actual_start_time as ActualStartTime,
                    wo.actual_end_time as ActualEndTime,
                    wo.created_at as CreatedAt,
                    wo.updated_at as UpdatedAt
                FROM work_orders wo
                INNER JOIN equipment_lines el ON wo.resource_reference = el.adam_device_id
                WHERE el.line_id = @lineId 
                  AND wo.status IN ('Active', 'Paused')
                ORDER BY wo.created_at DESC
                LIMIT 1";

            _logger.LogDebug("Retrieving active work order for line {LineId}", lineId);

            var workOrderData = await connection.QuerySingleOrDefaultAsync<WorkOrderData>(sql, new { lineId });

            if (workOrderData == null)
            {
                _logger.LogDebug("No active work order found for line {LineId}", lineId);
                return null;
            }

            var workOrder = MapToWorkOrder(workOrderData);

            _logger.LogInformation("Found active work order {WorkOrderId} for line {LineId} with status {Status}",
                workOrder.Id, lineId, workOrder.Status);

            return workOrder;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve active work order for line {LineId}", lineId);
            throw;
        }
    }

    /// <summary>
    /// Get work orders by status
    /// </summary>
    /// <param name="status">Work order status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of work orders with the specified status</returns>
    public async Task<IEnumerable<WorkOrder>> GetByStatusAsync(WorkOrderStatus status, CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("GetWorkOrdersByStatus");
        activity?.SetTag("status", status.ToString());

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = @"
                SELECT 
                    work_order_id as Id,
                    work_order_description as WorkOrderDescription,
                    product_id as ProductId,
                    product_description as ProductDescription,
                    planned_quantity as PlannedQuantity,
                    unit_of_measure as UnitOfMeasure,
                    scheduled_start_time as ScheduledStartTime,
                    scheduled_end_time as ScheduledEndTime,
                    resource_reference as ResourceReference,
                    status as Status,
                    actual_quantity_good as ActualQuantityGood,
                    actual_quantity_scrap as ActualQuantityScrap,
                    actual_start_time as ActualStartTime,
                    actual_end_time as ActualEndTime,
                    created_at as CreatedAt,
                    updated_at as UpdatedAt
                FROM work_orders 
                WHERE status = @status
                ORDER BY created_at DESC";

            _logger.LogDebug("Retrieving work orders with status {Status}", status);

            var workOrderDataList = await connection.QueryAsync<WorkOrderData>(sql, new { status = status.ToString() });
            var workOrders = workOrderDataList.Select(MapToWorkOrder).ToList();

            _logger.LogInformation("Retrieved {Count} work orders with status {Status}", workOrders.Count, status);

            return workOrders;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve work orders with status {Status}", status);
            throw;
        }
    }

    /// <summary>
    /// Get work orders for a specific device within a time range
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="startTime">Start of time range</param>
    /// <param name="endTime">End of time range</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of work orders for the device and time range</returns>
    public async Task<IEnumerable<WorkOrder>> GetByDeviceAndTimeRangeAsync(
        string deviceId,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("Device ID cannot be null or empty", nameof(deviceId));

        if (endTime <= startTime)
            throw new ArgumentException("End time must be after start time", nameof(endTime));

        using var activity = ActivitySource.StartActivity("GetWorkOrdersByDeviceAndTimeRange");
        activity?.SetTag("deviceId", deviceId);

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = @"
                SELECT 
                    work_order_id as Id,
                    work_order_description as WorkOrderDescription,
                    product_id as ProductId,
                    product_description as ProductDescription,
                    planned_quantity as PlannedQuantity,
                    unit_of_measure as UnitOfMeasure,
                    scheduled_start_time as ScheduledStartTime,
                    scheduled_end_time as ScheduledEndTime,
                    resource_reference as ResourceReference,
                    status as Status,
                    actual_quantity_good as ActualQuantityGood,
                    actual_quantity_scrap as ActualQuantityScrap,
                    actual_start_time as ActualStartTime,
                    actual_end_time as ActualEndTime,
                    created_at as CreatedAt,
                    updated_at as UpdatedAt
                FROM work_orders 
                WHERE resource_reference = @deviceId
                  AND (
                    (scheduled_start_time >= @startTime AND scheduled_start_time <= @endTime) OR
                    (scheduled_end_time >= @startTime AND scheduled_end_time <= @endTime) OR
                    (scheduled_start_time <= @startTime AND scheduled_end_time >= @endTime)
                  )
                ORDER BY scheduled_start_time ASC";

            var parameters = new { deviceId, startTime, endTime };

            _logger.LogDebug("Retrieving work orders for device {DeviceId} from {StartTime} to {EndTime}",
                deviceId, startTime, endTime);

            var workOrderDataList = await connection.QueryAsync<WorkOrderData>(sql, parameters);
            var workOrders = workOrderDataList.Select(MapToWorkOrder).ToList();

            _logger.LogInformation("Retrieved {Count} work orders for device {DeviceId} in time range",
                workOrders.Count, deviceId);

            return workOrders;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve work orders for device {DeviceId} in time range", deviceId);
            throw;
        }
    }

    /// <summary>
    /// Get work orders that require attention
    /// </summary>
    /// <param name="qualityThreshold">Quality threshold for attention (default 95%)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of work orders requiring attention</returns>
    public async Task<IEnumerable<WorkOrder>> GetWorkOrdersRequiringAttentionAsync(
        decimal qualityThreshold = 95m,
        CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("GetWorkOrdersRequiringAttention");
        activity?.SetTag("qualityThreshold", qualityThreshold);

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            // This query identifies work orders that may need attention based on:
            // 1. Behind schedule (current time vs scheduled progress)
            // 2. Low quality (high scrap rate)
            // 3. No recent activity (for active jobs)
            const string sql = @"
                WITH work_order_metrics AS (
                    SELECT 
                        *,
                        CASE 
                            WHEN planned_quantity > 0 THEN 
                                ((actual_quantity_good + actual_quantity_scrap) / planned_quantity) * 100
                            ELSE 0 
                        END as completion_percent,
                        CASE 
                            WHEN (actual_quantity_good + actual_quantity_scrap) > 0 THEN 
                                (actual_quantity_good / (actual_quantity_good + actual_quantity_scrap)) * 100
                            ELSE 100 
                        END as quality_percent,
                        CASE 
                            WHEN status = 'Active' AND scheduled_end_time > scheduled_start_time THEN
                                ((EXTRACT(EPOCH FROM (NOW() - scheduled_start_time)) / 
                                  EXTRACT(EPOCH FROM (scheduled_end_time - scheduled_start_time))) * 100)
                            ELSE 0
                        END as schedule_progress_percent
                    FROM work_orders
                    WHERE status IN ('Active', 'Paused')
                )
                SELECT 
                    work_order_id as Id,
                    work_order_description as WorkOrderDescription,
                    product_id as ProductId,
                    product_description as ProductDescription,
                    planned_quantity as PlannedQuantity,
                    unit_of_measure as UnitOfMeasure,
                    scheduled_start_time as ScheduledStartTime,
                    scheduled_end_time as ScheduledEndTime,
                    resource_reference as ResourceReference,
                    status as Status,
                    actual_quantity_good as ActualQuantityGood,
                    actual_quantity_scrap as ActualQuantityScrap,
                    actual_start_time as ActualStartTime,
                    actual_end_time as ActualEndTime,
                    created_at as CreatedAt,
                    updated_at as UpdatedAt
                FROM work_order_metrics
                WHERE 
                    quality_percent < @qualityThreshold OR
                    (completion_percent < schedule_progress_percent - 10) OR
                    (status = 'Active' AND updated_at < NOW() - INTERVAL '10 minutes')
                ORDER BY 
                    CASE 
                        WHEN quality_percent < @qualityThreshold THEN 1
                        WHEN completion_percent < schedule_progress_percent - 10 THEN 2
                        ELSE 3
                    END,
                    updated_at ASC";

            _logger.LogDebug("Retrieving work orders requiring attention with quality threshold {QualityThreshold}%",
                qualityThreshold);

            var workOrderDataList = await connection.QueryAsync<WorkOrderData>(sql, new { qualityThreshold });
            var workOrders = workOrderDataList.Select(MapToWorkOrder).ToList();

            _logger.LogInformation("Found {Count} work orders requiring attention", workOrders.Count);

            return workOrders;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve work orders requiring attention");
            throw;
        }
    }

    /// <summary>
    /// Create a new work order
    /// </summary>
    /// <param name="workOrder">Work order to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created work order identifier</returns>
    public async Task<string> CreateAsync(WorkOrder workOrder, CancellationToken cancellationToken = default)
    {
        if (workOrder == null)
            throw new ArgumentNullException(nameof(workOrder));

        using var activity = ActivitySource.StartActivity("CreateWorkOrder");
        activity?.SetTag("workOrderId", workOrder.Id);

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = @"
                INSERT INTO work_orders (
                    work_order_id,
                    work_order_description,
                    product_id,
                    product_description,
                    planned_quantity,
                    unit_of_measure,
                    scheduled_start_time,
                    scheduled_end_time,
                    resource_reference,
                    status,
                    actual_quantity_good,
                    actual_quantity_scrap,
                    actual_start_time,
                    actual_end_time,
                    created_at,
                    updated_at
                ) VALUES (
                    @Id,
                    @WorkOrderDescription,
                    @ProductId,
                    @ProductDescription,
                    @PlannedQuantity,
                    @UnitOfMeasure,
                    @ScheduledStartTime,
                    @ScheduledEndTime,
                    @ResourceReference,
                    @Status,
                    @ActualQuantityGood,
                    @ActualQuantityScrap,
                    @ActualStartTime,
                    @ActualEndTime,
                    @CreatedAt,
                    @UpdatedAt
                )";

            var parameters = new
            {
                workOrder.Id,
                workOrder.WorkOrderDescription,
                workOrder.ProductId,
                workOrder.ProductDescription,
                workOrder.PlannedQuantity,
                workOrder.UnitOfMeasure,
                workOrder.ScheduledStartTime,
                workOrder.ScheduledEndTime,
                workOrder.ResourceReference,
                Status = workOrder.Status.ToString(),
                workOrder.ActualQuantityGood,
                workOrder.ActualQuantityScrap,
                workOrder.ActualStartTime,
                workOrder.ActualEndTime,
                workOrder.CreatedAt,
                workOrder.UpdatedAt
            };

            _logger.LogDebug("Creating work order {WorkOrderId} for device {DeviceId}",
                workOrder.Id, workOrder.ResourceReference);

            var rowsAffected = await connection.ExecuteAsync(sql, parameters);

            if (rowsAffected != 1)
                throw new InvalidOperationException($"Expected 1 row to be affected, but {rowsAffected} were affected");

            _logger.LogInformation("Created work order {WorkOrderId} for product {ProductDescription} on device {DeviceId}",
                workOrder.Id, workOrder.ProductDescription, workOrder.ResourceReference);

            return workOrder.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create work order {WorkOrderId}", workOrder.Id);
            throw;
        }
    }

    /// <summary>
    /// Update an existing work order
    /// </summary>
    /// <param name="workOrder">Work order to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if updated successfully, false if not found</returns>
    public async Task<bool> UpdateAsync(WorkOrder workOrder, CancellationToken cancellationToken = default)
    {
        if (workOrder == null)
            throw new ArgumentNullException(nameof(workOrder));

        using var activity = ActivitySource.StartActivity("UpdateWorkOrder");
        activity?.SetTag("workOrderId", workOrder.Id);

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = @"
                UPDATE work_orders SET
                    work_order_description = @WorkOrderDescription,
                    product_id = @ProductId,
                    product_description = @ProductDescription,
                    planned_quantity = @PlannedQuantity,
                    unit_of_measure = @UnitOfMeasure,
                    scheduled_start_time = @ScheduledStartTime,
                    scheduled_end_time = @ScheduledEndTime,
                    resource_reference = @ResourceReference,
                    status = @Status,
                    actual_quantity_good = @ActualQuantityGood,
                    actual_quantity_scrap = @ActualQuantityScrap,
                    actual_start_time = @ActualStartTime,
                    actual_end_time = @ActualEndTime,
                    updated_at = @UpdatedAt
                WHERE work_order_id = @Id";

            var parameters = new
            {
                workOrder.Id,
                workOrder.WorkOrderDescription,
                workOrder.ProductId,
                workOrder.ProductDescription,
                workOrder.PlannedQuantity,
                workOrder.UnitOfMeasure,
                workOrder.ScheduledStartTime,
                workOrder.ScheduledEndTime,
                workOrder.ResourceReference,
                Status = workOrder.Status.ToString(),
                workOrder.ActualQuantityGood,
                workOrder.ActualQuantityScrap,
                workOrder.ActualStartTime,
                workOrder.ActualEndTime,
                UpdatedAt = DateTime.UtcNow  // Always use current time for updates
            };

            _logger.LogDebug("Updating work order {WorkOrderId}", workOrder.Id);

            var rowsAffected = await connection.ExecuteAsync(sql, parameters);
            var wasUpdated = rowsAffected > 0;

            if (wasUpdated)
            {
                _logger.LogInformation("Updated work order {WorkOrderId} with status {Status}",
                    workOrder.Id, workOrder.Status);
            }
            else
            {
                _logger.LogWarning("Work order {WorkOrderId} not found for update", workOrder.Id);
            }

            return wasUpdated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update work order {WorkOrderId}", workOrder.Id);
            throw;
        }
    }

    /// <summary>
    /// Delete a work order
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully, false if not found</returns>
    public async Task<bool> DeleteAsync(string workOrderId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workOrderId))
            throw new ArgumentException("Work order ID cannot be null or empty", nameof(workOrderId));

        using var activity = ActivitySource.StartActivity("DeleteWorkOrder");
        activity?.SetTag("workOrderId", workOrderId);

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = "DELETE FROM work_orders WHERE work_order_id = @workOrderId";

            _logger.LogDebug("Deleting work order {WorkOrderId}", workOrderId);

            var rowsAffected = await connection.ExecuteAsync(sql, new { workOrderId });
            var wasDeleted = rowsAffected > 0;

            if (wasDeleted)
            {
                _logger.LogInformation("Deleted work order {WorkOrderId}", workOrderId);
            }
            else
            {
                _logger.LogWarning("Work order {WorkOrderId} not found for deletion", workOrderId);
            }

            return wasDeleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete work order {WorkOrderId}", workOrderId);
            throw;
        }
    }

    /// <summary>
    /// Check if a work order exists
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if work order exists, false otherwise</returns>
    public async Task<bool> ExistsAsync(string workOrderId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workOrderId))
            throw new ArgumentException("Work order ID cannot be null or empty", nameof(workOrderId));

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = "SELECT EXISTS(SELECT 1 FROM work_orders WHERE work_order_id = @workOrderId)";

            var exists = await connection.QuerySingleAsync<bool>(sql, new { workOrderId });

            _logger.LogDebug("Work order {WorkOrderId} exists: {Exists}", workOrderId, exists);

            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if work order {WorkOrderId} exists", workOrderId);
            throw;
        }
    }

    // Additional interface methods would be implemented here following the same patterns...
    // For brevity, I'm including placeholders for the remaining methods

    public Task<WorkOrderStatistics> GetStatisticsAsync(DateTime startTime, DateTime endTime, string? deviceId = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("GetStatisticsAsync will be implemented based on specific requirements");
    }

    public Task<IEnumerable<WorkOrder>> SearchAsync(WorkOrderSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("SearchAsync will be implemented based on specific requirements");
    }

    public Task<IEnumerable<WorkOrder>> GetScheduledWorkOrdersAsync(DateTime startDate, DateTime endDate, string? deviceId = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("GetScheduledWorkOrdersAsync will be implemented based on specific requirements");
    }

    public Task<IEnumerable<WorkOrderCompletionRecord>> GetCompletionHistoryAsync(string deviceId, int days = 30, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("GetCompletionHistoryAsync will be implemented based on specific requirements");
    }

    /// <summary>
    /// Maps database row data to WorkOrder domain entity
    /// </summary>
    /// <param name="data">Database row data</param>
    /// <returns>WorkOrder domain entity</returns>
    private static WorkOrder MapToWorkOrder(WorkOrderData data)
    {
        // Parse the status enum
        if (!Enum.TryParse<WorkOrderStatus>(data.Status, out var status))
        {
            throw new InvalidOperationException($"Invalid work order status: {data.Status}");
        }

        return new WorkOrder(
            data.Id,
            data.WorkOrderDescription,
            data.ProductId,
            data.ProductDescription,
            data.PlannedQuantity,
            data.ScheduledStartTime,
            data.ScheduledEndTime,
            data.ResourceReference,
            data.UnitOfMeasure,
            data.ActualQuantityGood,
            data.ActualQuantityScrap,
            data.ActualStartTime,
            data.ActualEndTime,
            status
        );
    }

    private static readonly System.Diagnostics.ActivitySource ActivitySource = new("Industrial.Adam.Oee.Infrastructure");

    /// <summary>
    /// Data structure for mapping database rows to domain objects
    /// </summary>
    private sealed record WorkOrderData(
        string Id,
        string WorkOrderDescription,
        string ProductId,
        string ProductDescription,
        decimal PlannedQuantity,
        string UnitOfMeasure,
        DateTime ScheduledStartTime,
        DateTime ScheduledEndTime,
        string ResourceReference,
        string Status,
        decimal ActualQuantityGood,
        decimal ActualQuantityScrap,
        DateTime? ActualStartTime,
        DateTime? ActualEndTime,
        DateTime CreatedAt,
        DateTime UpdatedAt
    );
}
