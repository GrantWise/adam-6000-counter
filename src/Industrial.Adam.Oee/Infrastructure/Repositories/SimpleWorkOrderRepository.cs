using System.Data;
using Dapper;
using Industrial.Adam.Oee.Domain.Entities;
using Industrial.Adam.Oee.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Oee.Infrastructure.Repositories;

/// <summary>
/// Simplified work order repository following Logger module patterns
/// Focuses on essential operations with minimal complexity
/// </summary>
public sealed class SimpleWorkOrderRepository : IWorkOrderRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<SimpleWorkOrderRepository> _logger;

    /// <summary>
    /// Constructor for simple work order repository
    /// </summary>
    public SimpleWorkOrderRepository(
        IDbConnectionFactory connectionFactory,
        ILogger<SimpleWorkOrderRepository> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get a work order by its identifier
    /// </summary>
    public async Task<WorkOrder?> GetByIdAsync(string workOrderId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workOrderId))
            throw new ArgumentException("Work order ID cannot be null or empty", nameof(workOrderId));

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = @"
                SELECT 
                    work_order_id, work_order_description, product_id, product_description,
                    planned_quantity, unit_of_measure, scheduled_start_time, scheduled_end_time,
                    resource_reference, status, actual_quantity_good, actual_quantity_scrap,
                    actual_start_time, actual_end_time, created_at, updated_at
                FROM work_orders 
                WHERE work_order_id = @workOrderId";

            var result = await connection.QuerySingleOrDefaultAsync(sql, new { workOrderId });

            if (result == null)
            {
                _logger.LogWarning("Work order {WorkOrderId} not found", workOrderId);
                return null;
            }

            return MapToWorkOrder(result);
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
    public async Task<WorkOrder?> GetActiveByDeviceAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("Device ID cannot be null or empty", nameof(deviceId));

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = @"
                SELECT 
                    work_order_id, work_order_description, product_id, product_description,
                    planned_quantity, unit_of_measure, scheduled_start_time, scheduled_end_time,
                    resource_reference, status, actual_quantity_good, actual_quantity_scrap,
                    actual_start_time, actual_end_time, created_at, updated_at
                FROM work_orders 
                WHERE resource_reference = @deviceId 
                  AND status IN ('Active', 'Paused')
                ORDER BY created_at DESC
                LIMIT 1";

            var result = await connection.QuerySingleOrDefaultAsync(sql, new { deviceId });

            if (result == null)
            {
                _logger.LogDebug("No active work order found for device {DeviceId}", deviceId);
                return null;
            }

            return MapToWorkOrder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve active work order for device {DeviceId}", deviceId);
            throw;
        }
    }

    /// <summary>
    /// Create a new work order
    /// </summary>
    public async Task<string> CreateAsync(WorkOrder workOrder, CancellationToken cancellationToken = default)
    {
        if (workOrder == null)
            throw new ArgumentNullException(nameof(workOrder));

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = @"
                INSERT INTO work_orders (
                    work_order_id, work_order_description, product_id, product_description,
                    planned_quantity, unit_of_measure, scheduled_start_time, scheduled_end_time,
                    resource_reference, status, actual_quantity_good, actual_quantity_scrap,
                    actual_start_time, actual_end_time, created_at, updated_at
                ) VALUES (
                    @Id, @WorkOrderDescription, @ProductId, @ProductDescription,
                    @PlannedQuantity, @UnitOfMeasure, @ScheduledStartTime, @ScheduledEndTime,
                    @ResourceReference, @Status, @ActualQuantityGood, @ActualQuantityScrap,
                    @ActualStartTime, @ActualEndTime, @CreatedAt, @UpdatedAt
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

            var rowsAffected = await connection.ExecuteAsync(sql, parameters);

            if (rowsAffected != 1)
                throw new InvalidOperationException($"Expected 1 row to be affected, but {rowsAffected} were affected");

            _logger.LogInformation("Created work order {WorkOrderId} for device {DeviceId}",
                workOrder.Id, workOrder.ResourceReference);

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
    public async Task<bool> UpdateAsync(WorkOrder workOrder, CancellationToken cancellationToken = default)
    {
        if (workOrder == null)
            throw new ArgumentNullException(nameof(workOrder));

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
                UpdatedAt = DateTime.UtcNow
            };

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
    /// Check if a work order exists
    /// </summary>
    public async Task<bool> ExistsAsync(string workOrderId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workOrderId))
            throw new ArgumentException("Work order ID cannot be null or empty", nameof(workOrderId));

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = "SELECT EXISTS(SELECT 1 FROM work_orders WHERE work_order_id = @workOrderId)";
            return await connection.QuerySingleAsync<bool>(sql, new { workOrderId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if work order {WorkOrderId} exists", workOrderId);
            throw;
        }
    }

    /// <summary>
    /// Maps database row data to WorkOrder domain entity
    /// Simple mapping without complex reflection
    /// </summary>
    private static WorkOrder MapToWorkOrder(dynamic data)
    {
        if (!Enum.TryParse<WorkOrderStatus>(data.status, out WorkOrderStatus status))
        {
            throw new InvalidOperationException($"Invalid work order status: {data.status}");
        }

        return new WorkOrder(
            data.work_order_id,
            data.work_order_description,
            data.product_id,
            data.product_description,
            data.planned_quantity,
            data.scheduled_start_time,
            data.scheduled_end_time,
            data.resource_reference,
            data.unit_of_measure,
            data.actual_quantity_good,
            data.actual_quantity_scrap,
            data.actual_start_time,
            data.actual_end_time,
            status
        );
    }

    // Simplified interface - only implement essential methods for MVP
    // Complex analytics and search methods removed to match Logger simplicity
    public Task<WorkOrder?> GetActiveByLineAsync(string lineId, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Use GetActiveByDeviceAsync instead");

    public Task<IEnumerable<WorkOrder>> GetByStatusAsync(WorkOrderStatus status, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Not needed for MVP");

    public Task<IEnumerable<WorkOrder>> GetByDeviceAndTimeRangeAsync(string deviceId, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Not needed for MVP");

    public Task<IEnumerable<WorkOrder>> GetWorkOrdersRequiringAttentionAsync(decimal qualityThreshold = 95m, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Not needed for MVP");

    public Task<bool> DeleteAsync(string workOrderId, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Soft delete only for audit trail");

    public Task<WorkOrderStatistics> GetStatisticsAsync(DateTime startTime, DateTime endTime, string? deviceId = null, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Not needed for MVP");

    public Task<IEnumerable<WorkOrder>> SearchAsync(WorkOrderSearchCriteria criteria, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Not needed for MVP");

    public Task<IEnumerable<WorkOrder>> GetScheduledWorkOrdersAsync(DateTime startDate, DateTime endDate, string? deviceId = null, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Not needed for MVP");

    public Task<IEnumerable<WorkOrderCompletionRecord>> GetCompletionHistoryAsync(string deviceId, int days = 30, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Not needed for MVP");
}
