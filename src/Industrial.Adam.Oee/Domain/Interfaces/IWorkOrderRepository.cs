using Industrial.Adam.Oee.Domain.Entities;

namespace Industrial.Adam.Oee.Domain.Interfaces;

/// <summary>
/// Repository interface for work order persistence
/// </summary>
public interface IWorkOrderRepository
{
    /// <summary>
    /// Get a work order by its identifier
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Work order or null if not found</returns>
    public Task<WorkOrder?> GetByIdAsync(string workOrderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the active work order for a specific device
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Active work order or null if none active</returns>
    public Task<WorkOrder?> GetActiveByDeviceAsync(string deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the active work order for a specific equipment line
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Active work order or null if none active</returns>
    public Task<WorkOrder?> GetActiveByLineAsync(string lineId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get work orders by status
    /// </summary>
    /// <param name="status">Work order status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of work orders with the specified status</returns>
    public Task<IEnumerable<WorkOrder>> GetByStatusAsync(WorkOrderStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get work orders for a specific device within a time range
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="startTime">Start of time range</param>
    /// <param name="endTime">End of time range</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of work orders for the device and time range</returns>
    public Task<IEnumerable<WorkOrder>> GetByDeviceAndTimeRangeAsync(
        string deviceId,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get work orders that require attention
    /// </summary>
    /// <param name="qualityThreshold">Quality threshold for attention (default 95%)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of work orders requiring attention</returns>
    public Task<IEnumerable<WorkOrder>> GetWorkOrdersRequiringAttentionAsync(
        decimal qualityThreshold = 95m,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new work order
    /// </summary>
    /// <param name="workOrder">Work order to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created work order identifier</returns>
    public Task<string> CreateAsync(WorkOrder workOrder, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing work order
    /// </summary>
    /// <param name="workOrder">Work order to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if updated successfully, false if not found</returns>
    public Task<bool> UpdateAsync(WorkOrder workOrder, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a work order
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully, false if not found</returns>
    public Task<bool> DeleteAsync(string workOrderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a work order exists
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if work order exists, false otherwise</returns>
    public Task<bool> ExistsAsync(string workOrderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get work order statistics for a time period
    /// </summary>
    /// <param name="startTime">Start of time period</param>
    /// <param name="endTime">End of time period</param>
    /// <param name="deviceId">Optional device filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Work order statistics</returns>
    public Task<WorkOrderStatistics> GetStatisticsAsync(
        DateTime startTime,
        DateTime endTime,
        string? deviceId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search work orders by criteria
    /// </summary>
    /// <param name="criteria">Search criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of matching work orders</returns>
    public Task<IEnumerable<WorkOrder>> SearchAsync(WorkOrderSearchCriteria criteria, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get work orders scheduled for a specific date range
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="deviceId">Optional device filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of scheduled work orders</returns>
    public Task<IEnumerable<WorkOrder>> GetScheduledWorkOrdersAsync(
        DateTime startDate,
        DateTime endDate,
        string? deviceId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get work order completion history for analytics
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="days">Number of days to look back</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Work order completion history</returns>
    public Task<IEnumerable<WorkOrderCompletionRecord>> GetCompletionHistoryAsync(
        string deviceId,
        int days = 30,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Work order statistics for a time period
/// </summary>
/// <param name="TotalWorkOrders">Total number of work orders</param>
/// <param name="CompletedWorkOrders">Number of completed work orders</param>
/// <param name="ActiveWorkOrders">Number of active work orders</param>
/// <param name="CancelledWorkOrders">Number of cancelled work orders</param>
/// <param name="AverageCompletionPercentage">Average completion percentage</param>
/// <param name="AverageYieldPercentage">Average yield percentage</param>
/// <param name="TotalPlannedQuantity">Total planned quantity across all work orders</param>
/// <param name="TotalActualQuantity">Total actual quantity produced</param>
/// <param name="OnTimeCompletionRate">Percentage of work orders completed on time</param>
public record WorkOrderStatistics(
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
/// Search criteria for work orders
/// </summary>
/// <param name="WorkOrderId">Optional work order ID filter</param>
/// <param name="ProductId">Optional product ID filter</param>
/// <param name="DeviceId">Optional device ID filter</param>
/// <param name="Status">Optional status filter</param>
/// <param name="StartDate">Optional start date filter</param>
/// <param name="EndDate">Optional end date filter</param>
/// <param name="RequiresAttention">Optional filter for work orders requiring attention</param>
/// <param name="MinCompletionPercentage">Optional minimum completion percentage</param>
/// <param name="MaxCompletionPercentage">Optional maximum completion percentage</param>
public record WorkOrderSearchCriteria(
    string? WorkOrderId = null,
    string? ProductId = null,
    string? DeviceId = null,
    WorkOrderStatus? Status = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    bool? RequiresAttention = null,
    decimal? MinCompletionPercentage = null,
    decimal? MaxCompletionPercentage = null
);

/// <summary>
/// Work order completion record for analytics
/// </summary>
/// <param name="WorkOrderId">Work order identifier</param>
/// <param name="ProductDescription">Product description</param>
/// <param name="PlannedQuantity">Planned quantity</param>
/// <param name="ActualQuantity">Actual quantity produced</param>
/// <param name="YieldPercentage">Yield percentage</param>
/// <param name="ScheduledDuration">Scheduled duration in hours</param>
/// <param name="ActualDuration">Actual duration in hours</param>
/// <param name="CompletedOnTime">Whether completed on time</param>
/// <param name="CompletionDate">When the work order was completed</param>
public record WorkOrderCompletionRecord(
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
