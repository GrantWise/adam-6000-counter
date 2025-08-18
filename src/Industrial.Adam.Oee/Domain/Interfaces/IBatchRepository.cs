using Industrial.Adam.Oee.Domain.Entities;

namespace Industrial.Adam.Oee.Domain.Interfaces;

/// <summary>
/// Repository interface for Batch aggregate
/// </summary>
public interface IBatchRepository
{
    /// <summary>
    /// Get batch by identifier
    /// </summary>
    /// <param name="batchId">Batch identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Batch if found, null otherwise</returns>
    public Task<Batch?> GetByIdAsync(string batchId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get batch by batch number
    /// </summary>
    /// <param name="batchNumber">Batch number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Batch if found, null otherwise</returns>
    public Task<Batch?> GetByBatchNumberAsync(string batchNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get batches for work order
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of batches</returns>
    public Task<IEnumerable<Batch>> GetByWorkOrderIdAsync(string workOrderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get batches for equipment line
    /// </summary>
    /// <param name="equipmentLineId">Equipment line identifier</param>
    /// <param name="startDate">Start date filter</param>
    /// <param name="endDate">End date filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of batches</returns>
    public Task<IEnumerable<Batch>> GetByEquipmentLineAsync(
        string equipmentLineId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get batches for shift
    /// </summary>
    /// <param name="shiftId">Shift identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of batches</returns>
    public Task<IEnumerable<Batch>> GetByShiftIdAsync(string shiftId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get batches with quality issues
    /// </summary>
    /// <param name="qualityThreshold">Quality threshold percentage</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of batches with quality issues</returns>
    public Task<IEnumerable<Batch>> GetBatchesWithQualityIssuesAsync(decimal qualityThreshold = 95m, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active batches
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of active batches</returns>
    public Task<IEnumerable<Batch>> GetActiveBatchesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get batches by status
    /// </summary>
    /// <param name="status">Batch status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of batches</returns>
    public Task<IEnumerable<Batch>> GetByStatusAsync(BatchStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get child batches for parent batch
    /// </summary>
    /// <param name="parentBatchId">Parent batch identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of child batches</returns>
    public Task<IEnumerable<Batch>> GetChildBatchesAsync(string parentBatchId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Search batches with filters
    /// </summary>
    /// <param name="filter">Batch search filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of batches</returns>
    public Task<IEnumerable<Batch>> SearchAsync(BatchSearchFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add new batch
    /// </summary>
    /// <param name="batch">Batch to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    public Task AddAsync(Batch batch, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update existing batch
    /// </summary>
    /// <param name="batch">Batch to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    public Task UpdateAsync(Batch batch, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete batch
    /// </summary>
    /// <param name="batchId">Batch identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    public Task<bool> DeleteAsync(string batchId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get batch statistics for date range
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="equipmentLineId">Equipment line filter (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Batch statistics</returns>
    public Task<BatchStatistics> GetStatisticsAsync(
        DateTime startDate,
        DateTime endDate,
        string? equipmentLineId = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Batch search filter
/// </summary>
/// <param name="WorkOrderId">Work order identifier filter</param>
/// <param name="ProductId">Product identifier filter</param>
/// <param name="EquipmentLineId">Equipment line identifier filter</param>
/// <param name="OperatorId">Operator identifier filter</param>
/// <param name="ShiftId">Shift identifier filter</param>
/// <param name="Status">Status filter</param>
/// <param name="StartDateFrom">Start date from filter</param>
/// <param name="StartDateTo">Start date to filter</param>
/// <param name="CompletionDateFrom">Completion date from filter</param>
/// <param name="CompletionDateTo">Completion date to filter</param>
/// <param name="MinQualityScore">Minimum quality score filter</param>
/// <param name="MaxQualityScore">Maximum quality score filter</param>
/// <param name="HasQualityIssues">Has quality issues filter</param>
/// <param name="ParentBatchId">Parent batch identifier filter</param>
public record BatchSearchFilter(
    string? WorkOrderId = null,
    string? ProductId = null,
    string? EquipmentLineId = null,
    string? OperatorId = null,
    string? ShiftId = null,
    BatchStatus? Status = null,
    DateTime? StartDateFrom = null,
    DateTime? StartDateTo = null,
    DateTime? CompletionDateFrom = null,
    DateTime? CompletionDateTo = null,
    decimal? MinQualityScore = null,
    decimal? MaxQualityScore = null,
    bool? HasQualityIssues = null,
    string? ParentBatchId = null
);

/// <summary>
/// Batch statistics
/// </summary>
/// <param name="TotalBatches">Total number of batches</param>
/// <param name="CompletedBatches">Number of completed batches</param>
/// <param name="ActiveBatches">Number of active batches</param>
/// <param name="OnHoldBatches">Number of batches on hold</param>
/// <param name="CancelledBatches">Number of cancelled batches</param>
/// <param name="AverageQualityScore">Average quality score</param>
/// <param name="AverageYieldPercentage">Average yield percentage</param>
/// <param name="AverageCompletionPercentage">Average completion percentage</param>
/// <param name="TotalUnitsProduced">Total units produced</param>
/// <param name="TotalUnitsDefective">Total defective units</param>
/// <param name="AverageProductionRate">Average production rate</param>
/// <param name="BatchesWithQualityIssues">Number of batches with quality issues</param>
public record BatchStatistics(
    int TotalBatches,
    int CompletedBatches,
    int ActiveBatches,
    int OnHoldBatches,
    int CancelledBatches,
    decimal AverageQualityScore,
    decimal AverageYieldPercentage,
    decimal AverageCompletionPercentage,
    decimal TotalUnitsProduced,
    decimal TotalUnitsDefective,
    decimal AverageProductionRate,
    int BatchesWithQualityIssues
);
