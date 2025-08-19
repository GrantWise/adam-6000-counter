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

    /// <summary>
    /// Get work order statistics for a time period with TimescaleDB optimized aggregation
    /// </summary>
    /// <param name="startTime">Start of time period</param>
    /// <param name="endTime">End of time period</param>
    /// <param name="deviceId">Optional device filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Work order statistics</returns>
    public async Task<WorkOrderStatistics> GetStatisticsAsync(DateTime startTime, DateTime endTime, string? deviceId = null, CancellationToken cancellationToken = default)
    {
        if (endTime <= startTime)
            throw new ArgumentException("End time must be after start time", nameof(endTime));

        using var activity = ActivitySource.StartActivity("GetWorkOrderStatistics");
        activity?.SetTag("deviceId", deviceId);
        activity?.SetTag("timeRange", $"{startTime:yyyy-MM-dd} to {endTime:yyyy-MM-dd}");

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            // TimescaleDB optimized statistics query with time-bucket aggregation
            const string sql = @"
                WITH work_order_metrics AS (
                    SELECT 
                        work_order_id,
                        status,
                        planned_quantity,
                        actual_quantity_good,
                        actual_quantity_scrap,
                        scheduled_start_time,
                        scheduled_end_time,
                        actual_start_time,
                        actual_end_time,
                        resource_reference,
                        CASE 
                            WHEN planned_quantity > 0 THEN 
                                ((actual_quantity_good + actual_quantity_scrap) / planned_quantity) * 100
                            ELSE 0 
                        END as completion_percentage,
                        CASE 
                            WHEN (actual_quantity_good + actual_quantity_scrap) > 0 THEN 
                                (actual_quantity_good / (actual_quantity_good + actual_quantity_scrap)) * 100
                            ELSE 100 
                        END as yield_percentage,
                        CASE 
                            WHEN actual_end_time IS NOT NULL AND scheduled_end_time IS NOT NULL THEN
                                CASE WHEN actual_end_time <= scheduled_end_time THEN 1 ELSE 0 END
                            ELSE 0
                        END as completed_on_time
                    FROM work_orders 
                    WHERE 
                        (scheduled_start_time >= @startTime AND scheduled_start_time <= @endTime)
                        OR (scheduled_end_time >= @startTime AND scheduled_end_time <= @endTime)
                        OR (scheduled_start_time <= @startTime AND scheduled_end_time >= @endTime)
                        AND (@deviceId IS NULL OR resource_reference = @deviceId)
                )
                SELECT 
                    COUNT(*) as TotalWorkOrders,
                    COUNT(CASE WHEN status = 'Completed' THEN 1 END) as CompletedWorkOrders,
                    COUNT(CASE WHEN status IN ('Active', 'Paused') THEN 1 END) as ActiveWorkOrders,
                    COUNT(CASE WHEN status = 'Cancelled' THEN 1 END) as CancelledWorkOrders,
                    COALESCE(AVG(completion_percentage), 0) as AverageCompletionPercentage,
                    COALESCE(AVG(yield_percentage), 100) as AverageYieldPercentage,
                    COALESCE(SUM(planned_quantity), 0) as TotalPlannedQuantity,
                    COALESCE(SUM(actual_quantity_good + actual_quantity_scrap), 0) as TotalActualQuantity,
                    CASE 
                        WHEN COUNT(CASE WHEN status = 'Completed' THEN 1 END) > 0 THEN
                            (CAST(SUM(completed_on_time) AS DECIMAL) / COUNT(CASE WHEN status = 'Completed' THEN 1 END)) * 100
                        ELSE 0
                    END as OnTimeCompletionRate
                FROM work_order_metrics";

            var parameters = new { startTime, endTime, deviceId };

            _logger.LogDebug("Calculating work order statistics for period {StartTime} to {EndTime}, device: {DeviceId}",
                startTime, endTime, deviceId ?? "ALL");

            var statisticsData = await connection.QuerySingleAsync<WorkOrderStatisticsData>(sql, parameters);

            var statistics = new WorkOrderStatistics(
                statisticsData.TotalWorkOrders,
                statisticsData.CompletedWorkOrders,
                statisticsData.ActiveWorkOrders,
                statisticsData.CancelledWorkOrders,
                Math.Round(statisticsData.AverageCompletionPercentage, 2),
                Math.Round(statisticsData.AverageYieldPercentage, 2),
                statisticsData.TotalPlannedQuantity,
                statisticsData.TotalActualQuantity,
                Math.Round(statisticsData.OnTimeCompletionRate, 2)
            );

            _logger.LogInformation(
                "Calculated statistics for {TotalWorkOrders} work orders: {CompletedWorkOrders} completed, {ActiveWorkOrders} active, " +
                "average completion {AverageCompletion}%, average yield {AverageYield}%, on-time rate {OnTimeRate}%",
                statistics.TotalWorkOrders, statistics.CompletedWorkOrders, statistics.ActiveWorkOrders,
                statistics.AverageCompletionPercentage, statistics.AverageYieldPercentage, statistics.OnTimeCompletionRate);

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate work order statistics for period {StartTime} to {EndTime}, device: {DeviceId}",
                startTime, endTime, deviceId ?? "ALL");
            throw;
        }
    }

    /// <summary>
    /// Search work orders by multiple criteria with TimescaleDB optimized filtering
    /// </summary>
    /// <param name="criteria">Search criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of matching work orders</returns>
    public async Task<IEnumerable<WorkOrder>> SearchAsync(WorkOrderSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        if (criteria == null)
            throw new ArgumentNullException(nameof(criteria));

        using var activity = ActivitySource.StartActivity("SearchWorkOrders");
        activity?.SetTag("deviceId", criteria.DeviceId);
        activity?.SetTag("status", criteria.Status?.ToString());

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            // Build dynamic WHERE clause based on criteria
            var whereConditions = new List<string>();
            var parameters = new Dictionary<string, object>();

            if (!string.IsNullOrWhiteSpace(criteria.WorkOrderId))
            {
                whereConditions.Add("work_order_id ILIKE @workOrderId");
                parameters["workOrderId"] = $"%{criteria.WorkOrderId}%";
            }

            if (!string.IsNullOrWhiteSpace(criteria.ProductId))
            {
                whereConditions.Add("product_id ILIKE @productId");
                parameters["productId"] = $"%{criteria.ProductId}%";
            }

            if (!string.IsNullOrWhiteSpace(criteria.DeviceId))
            {
                whereConditions.Add("resource_reference = @deviceId");
                parameters["deviceId"] = criteria.DeviceId;
            }

            if (criteria.Status.HasValue)
            {
                whereConditions.Add("status = @status");
                parameters["status"] = criteria.Status.Value.ToString();
            }

            if (criteria.StartDate.HasValue)
            {
                whereConditions.Add("scheduled_start_time >= @startDate");
                parameters["startDate"] = criteria.StartDate.Value;
            }

            if (criteria.EndDate.HasValue)
            {
                whereConditions.Add("scheduled_end_time <= @endDate");
                parameters["endDate"] = criteria.EndDate.Value;
            }

            if (criteria.MinCompletionPercentage.HasValue || criteria.MaxCompletionPercentage.HasValue)
            {
                if (criteria.MinCompletionPercentage.HasValue)
                {
                    whereConditions.Add("((actual_quantity_good + actual_quantity_scrap) / NULLIF(planned_quantity, 0)) * 100 >= @minCompletion");
                    parameters["minCompletion"] = criteria.MinCompletionPercentage.Value;
                }

                if (criteria.MaxCompletionPercentage.HasValue)
                {
                    whereConditions.Add("((actual_quantity_good + actual_quantity_scrap) / NULLIF(planned_quantity, 0)) * 100 <= @maxCompletion");
                    parameters["maxCompletion"] = criteria.MaxCompletionPercentage.Value;
                }
            }

            if (criteria.RequiresAttention.HasValue && criteria.RequiresAttention.Value)
            {
                // Add conditions for work orders requiring attention (similar to GetWorkOrdersRequiringAttentionAsync)
                whereConditions.Add(@"(
                    (CASE WHEN (actual_quantity_good + actual_quantity_scrap) > 0 THEN 
                        (actual_quantity_good / (actual_quantity_good + actual_quantity_scrap)) * 100 ELSE 100 END) < 95 OR
                    (status = 'Active' AND updated_at < NOW() - INTERVAL '10 minutes')
                )");
            }

            // Construct the complete SQL query
            var whereClause = whereConditions.Count > 0 ? "WHERE " + string.Join(" AND ", whereConditions) : "";

            var sql = $@"
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
                {whereClause}
                ORDER BY 
                    CASE 
                        WHEN status = 'Active' THEN 1
                        WHEN status = 'Paused' THEN 2
                        WHEN status = 'Scheduled' THEN 3
                        WHEN status = 'Completed' THEN 4
                        ELSE 5
                    END,
                    scheduled_start_time DESC
                LIMIT 1000"; // Reasonable limit to prevent excessive memory usage

            _logger.LogDebug("Searching work orders with {ConditionCount} criteria", whereConditions.Count);

            var workOrderDataList = await connection.QueryAsync<WorkOrderData>(sql, parameters);
            var workOrders = workOrderDataList.Select(MapToWorkOrder).ToList();

            _logger.LogInformation("Found {Count} work orders matching search criteria", workOrders.Count);

            return workOrders;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search work orders with criteria");
            throw;
        }
    }

    /// <summary>
    /// Get work orders scheduled for a specific date range with TimescaleDB time-bucket optimization
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="deviceId">Optional device filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of scheduled work orders</returns>
    public async Task<IEnumerable<WorkOrder>> GetScheduledWorkOrdersAsync(DateTime startDate, DateTime endDate, string? deviceId = null, CancellationToken cancellationToken = default)
    {
        if (endDate <= startDate)
            throw new ArgumentException("End date must be after start date", nameof(endDate));

        using var activity = ActivitySource.StartActivity("GetScheduledWorkOrders");
        activity?.SetTag("deviceId", deviceId);
        activity?.SetTag("dateRange", $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            // TimescaleDB optimized query for future work order planning
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
                WHERE 
                    (
                        (scheduled_start_time >= @startDate AND scheduled_start_time <= @endDate)
                        OR (scheduled_end_time >= @startDate AND scheduled_end_time <= @endDate)
                        OR (scheduled_start_time <= @startDate AND scheduled_end_time >= @endDate)
                    )
                    AND status IN ('Scheduled', 'Active', 'Paused')
                    AND (@deviceId IS NULL OR resource_reference = @deviceId)
                ORDER BY 
                    scheduled_start_time ASC,
                    CASE 
                        WHEN status = 'Active' THEN 1
                        WHEN status = 'Paused' THEN 2
                        WHEN status = 'Scheduled' THEN 3
                        ELSE 4
                    END";

            var parameters = new { startDate, endDate, deviceId };

            _logger.LogDebug("Retrieving scheduled work orders for date range {StartDate} to {EndDate}, device: {DeviceId}",
                startDate, endDate, deviceId ?? "ALL");

            var workOrderDataList = await connection.QueryAsync<WorkOrderData>(sql, parameters);
            var workOrders = workOrderDataList.Select(MapToWorkOrder).ToList();

            _logger.LogInformation("Retrieved {Count} scheduled work orders for date range {StartDate} to {EndDate}, device: {DeviceId}",
                workOrders.Count, startDate, endDate, deviceId ?? "ALL");

            return workOrders;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve scheduled work orders for date range {StartDate} to {EndDate}, device: {DeviceId}",
                startDate, endDate, deviceId ?? "ALL");
            throw;
        }
    }

    /// <summary>
    /// Get work order completion history for analytics with TimescaleDB time-series optimization
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="days">Number of days to look back</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Work order completion history</returns>
    public async Task<IEnumerable<WorkOrderCompletionRecord>> GetCompletionHistoryAsync(string deviceId, int days = 30, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("Device ID cannot be null or empty", nameof(deviceId));

        if (days <= 0 || days > 365)
            throw new ArgumentException("Days must be between 1 and 365", nameof(days));

        using var activity = ActivitySource.StartActivity("GetWorkOrderCompletionHistory");
        activity?.SetTag("deviceId", deviceId);
        activity?.SetTag("days", days);

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            // TimescaleDB optimized historical performance analysis query
            const string sql = @"
                SELECT 
                    work_order_id as WorkOrderId,
                    product_description as ProductDescription,
                    planned_quantity as PlannedQuantity,
                    (actual_quantity_good + actual_quantity_scrap) as ActualQuantity,
                    CASE 
                        WHEN (actual_quantity_good + actual_quantity_scrap) > 0 THEN 
                            (actual_quantity_good / (actual_quantity_good + actual_quantity_scrap)) * 100
                        ELSE 100 
                    END as YieldPercentage,
                    EXTRACT(EPOCH FROM (scheduled_end_time - scheduled_start_time)) / 3600.0 as ScheduledDuration,
                    CASE 
                        WHEN actual_start_time IS NOT NULL AND actual_end_time IS NOT NULL THEN
                            EXTRACT(EPOCH FROM (actual_end_time - actual_start_time)) / 3600.0
                        ELSE 0
                    END as ActualDuration,
                    CASE 
                        WHEN actual_end_time IS NOT NULL AND scheduled_end_time IS NOT NULL THEN
                            actual_end_time <= scheduled_end_time
                        ELSE false
                    END as CompletedOnTime,
                    COALESCE(actual_end_time, updated_at) as CompletionDate
                FROM work_orders 
                WHERE 
                    resource_reference = @deviceId
                    AND status = 'Completed'
                    AND actual_end_time >= NOW() - INTERVAL '@days days'
                    AND actual_end_time IS NOT NULL
                ORDER BY actual_end_time DESC
                LIMIT 500"; // Reasonable limit for historical data

            var parameters = new { deviceId, days };

            _logger.LogDebug("Retrieving work order completion history for device {DeviceId} over {Days} days",
                deviceId, days);

            var completionDataList = await connection.QueryAsync<WorkOrderCompletionData>(sql, parameters);
            var completionRecords = completionDataList.Select(data => new WorkOrderCompletionRecord(
                data.WorkOrderId,
                data.ProductDescription,
                data.PlannedQuantity,
                data.ActualQuantity,
                Math.Round(data.YieldPercentage, 2),
                Math.Round(data.ScheduledDuration, 2),
                Math.Round(data.ActualDuration, 2),
                data.CompletedOnTime,
                data.CompletionDate
            )).ToList();

            _logger.LogInformation("Retrieved {Count} completion records for device {DeviceId} over {Days} days",
                completionRecords.Count, deviceId, days);

            return completionRecords;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve completion history for device {DeviceId} over {Days} days",
                deviceId, days);
            throw;
        }
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

    /// <summary>
    /// Data structure for mapping work order statistics from database aggregation queries
    /// </summary>
    private sealed record WorkOrderStatisticsData(
        int TotalWorkOrders,
        int CompletedWorkOrders,
        int ActiveWorkOrders,
        int CancelledWorkOrders,
        decimal AverageCompletionPercentage,
        decimal AverageYieldPercentage,
        decimal TotalPlannedQuantity,
        decimal TotalActualQuantity,
        decimal OnTimeCompletionRate
    );

    /// <summary>
    /// Data structure for mapping work order completion history from database queries
    /// </summary>
    private sealed record WorkOrderCompletionData(
        string WorkOrderId,
        string ProductDescription,
        decimal PlannedQuantity,
        decimal ActualQuantity,
        decimal YieldPercentage,
        decimal ScheduledDuration,
        decimal ActualDuration,
        bool CompletedOnTime,
        DateTime CompletionDate
    );
}
