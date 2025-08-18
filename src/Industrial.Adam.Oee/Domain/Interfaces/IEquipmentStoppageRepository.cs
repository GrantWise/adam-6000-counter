using Industrial.Adam.Oee.Domain.Entities;

namespace Industrial.Adam.Oee.Domain.Interfaces;

/// <summary>
/// Repository interface for equipment stoppage persistence
/// </summary>
public interface IEquipmentStoppageRepository
{
    /// <summary>
    /// Get an equipment stoppage by its identifier
    /// </summary>
    /// <param name="id">Stoppage identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stoppage or null if not found</returns>
    public Task<EquipmentStoppage?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active stoppage for a specific equipment line
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Active stoppage or null if none active</returns>
    public Task<EquipmentStoppage?> GetActiveByLineAsync(string lineId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get stoppages for a specific equipment line within a time range
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="startTime">Start of time range</param>
    /// <param name="endTime">End of time range</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of stoppages for the line and time range</returns>
    public Task<IEnumerable<EquipmentStoppage>> GetByLineAndTimeRangeAsync(
        string lineId,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get stoppages for a specific work order
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of stoppages for the work order</returns>
    public Task<IEnumerable<EquipmentStoppage>> GetByWorkOrderAsync(string workOrderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get unclassified stoppages that require attention
    /// </summary>
    /// <param name="lineId">Optional line filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of unclassified stoppages requiring classification</returns>
    public Task<IEnumerable<EquipmentStoppage>> GetUnclassifiedStoppagesAsync(string? lineId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get stoppages requiring classification (over threshold and unclassified)
    /// </summary>
    /// <param name="lineId">Optional line filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of stoppages requiring classification</returns>
    public Task<IEnumerable<EquipmentStoppage>> GetStoppagesRequiringClassificationAsync(string? lineId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get stoppages by classification
    /// </summary>
    /// <param name="categoryCode">Category code filter</param>
    /// <param name="subcode">Optional subcode filter</param>
    /// <param name="startTime">Optional start time filter</param>
    /// <param name="endTime">Optional end time filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of stoppages with the specified classification</returns>
    public Task<IEnumerable<EquipmentStoppage>> GetByClassificationAsync(
        string categoryCode,
        string? subcode = null,
        DateTime? startTime = null,
        DateTime? endTime = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get short stops (below threshold) for analysis
    /// </summary>
    /// <param name="lineId">Optional line filter</param>
    /// <param name="startTime">Start time filter</param>
    /// <param name="endTime">End time filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of short stops</returns>
    public Task<IEnumerable<EquipmentStoppage>> GetShortStopsAsync(
        string? lineId = null,
        DateTime? startTime = null,
        DateTime? endTime = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get long stops (over threshold) for analysis
    /// </summary>
    /// <param name="lineId">Optional line filter</param>
    /// <param name="startTime">Start time filter</param>
    /// <param name="endTime">End time filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of long stops</returns>
    public Task<IEnumerable<EquipmentStoppage>> GetLongStopsAsync(
        string? lineId = null,
        DateTime? startTime = null,
        DateTime? endTime = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new equipment stoppage
    /// </summary>
    /// <param name="stoppage">Stoppage to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created stoppage identifier</returns>
    public Task<int> CreateAsync(EquipmentStoppage stoppage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing equipment stoppage
    /// </summary>
    /// <param name="stoppage">Stoppage to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if updated successfully, false if not found</returns>
    public Task<bool> UpdateAsync(EquipmentStoppage stoppage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete an equipment stoppage
    /// </summary>
    /// <param name="id">Stoppage identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully, false if not found</returns>
    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a stoppage exists
    /// </summary>
    /// <param name="id">Stoppage identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if stoppage exists, false otherwise</returns>
    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get stoppage statistics for a time period
    /// </summary>
    /// <param name="startTime">Start of time period</param>
    /// <param name="endTime">End of time period</param>
    /// <param name="lineId">Optional line filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stoppage statistics</returns>
    public Task<StoppageStatistics> GetStatisticsAsync(
        DateTime startTime,
        DateTime endTime,
        string? lineId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search stoppages by criteria
    /// </summary>
    /// <param name="criteria">Search criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of matching stoppages</returns>
    public Task<IEnumerable<EquipmentStoppage>> SearchAsync(StoppageSearchCriteria criteria, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get classification trends for analysis
    /// </summary>
    /// <param name="startTime">Start time</param>
    /// <param name="endTime">End time</param>
    /// <param name="lineId">Optional line filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of classification trends</returns>
    public Task<IEnumerable<ClassificationTrend>> GetClassificationTrendsAsync(
        DateTime startTime,
        DateTime endTime,
        string? lineId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get top stoppage reasons for a time period
    /// </summary>
    /// <param name="startTime">Start time</param>
    /// <param name="endTime">End time</param>
    /// <param name="lineId">Optional line filter</param>
    /// <param name="topCount">Number of top reasons to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of top stoppage reasons</returns>
    public Task<IEnumerable<StoppageReasonSummary>> GetTopStoppageReasonsAsync(
        DateTime startTime,
        DateTime endTime,
        string? lineId = null,
        int topCount = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk update stoppages for work order association
    /// </summary>
    /// <param name="lineId">Line identifier</param>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="startTime">Start time for association</param>
    /// <param name="endTime">End time for association</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of stoppages updated</returns>
    public Task<int> BulkAssociateWithWorkOrderAsync(
        string lineId,
        string workOrderId,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Stoppage statistics for a time period
/// </summary>
/// <param name="TotalStoppages">Total number of stoppages</param>
/// <param name="ClassifiedStoppages">Number of classified stoppages</param>
/// <param name="UnclassifiedStoppages">Number of unclassified stoppages</param>
/// <param name="ShortStops">Number of short stops</param>
/// <param name="LongStops">Number of long stops</param>
/// <param name="TotalDowntimeMinutes">Total downtime in minutes</param>
/// <param name="AverageStoppageDuration">Average stoppage duration in minutes</param>
/// <param name="ClassificationRate">Percentage of stoppages classified</param>
/// <param name="AutoDetectedStoppages">Number of auto-detected stoppages</param>
/// <param name="ManualStoppages">Number of manually entered stoppages</param>
public record StoppageStatistics(
    int TotalStoppages,
    int ClassifiedStoppages,
    int UnclassifiedStoppages,
    int ShortStops,
    int LongStops,
    decimal TotalDowntimeMinutes,
    decimal AverageStoppageDuration,
    decimal ClassificationRate,
    int AutoDetectedStoppages,
    int ManualStoppages
);

/// <summary>
/// Search criteria for stoppages
/// </summary>
/// <param name="LineId">Optional line ID filter</param>
/// <param name="WorkOrderId">Optional work order ID filter</param>
/// <param name="StartTime">Optional start time filter</param>
/// <param name="EndTime">Optional end time filter</param>
/// <param name="IsClassified">Optional classification status filter</param>
/// <param name="CategoryCode">Optional category code filter</param>
/// <param name="Subcode">Optional subcode filter</param>
/// <param name="AutoDetected">Optional auto-detection filter</param>
/// <param name="MinDurationMinutes">Optional minimum duration filter</param>
/// <param name="MaxDurationMinutes">Optional maximum duration filter</param>
/// <param name="RequiresClassification">Optional requires classification filter</param>
public record StoppageSearchCriteria(
    string? LineId = null,
    string? WorkOrderId = null,
    DateTime? StartTime = null,
    DateTime? EndTime = null,
    bool? IsClassified = null,
    string? CategoryCode = null,
    string? Subcode = null,
    bool? AutoDetected = null,
    decimal? MinDurationMinutes = null,
    decimal? MaxDurationMinutes = null,
    bool? RequiresClassification = null
);

/// <summary>
/// Classification trend for analysis
/// </summary>
/// <param name="Period">Time period</param>
/// <param name="CategoryCode">Category code</param>
/// <param name="CategoryName">Category name</param>
/// <param name="Subcode">Subcode</param>
/// <param name="SubcodeName">Subcode name</param>
/// <param name="OccurrenceCount">Number of occurrences</param>
/// <param name="TotalDurationMinutes">Total duration in minutes</param>
/// <param name="AverageDurationMinutes">Average duration in minutes</param>
public record ClassificationTrend(
    DateTime Period,
    string CategoryCode,
    string CategoryName,
    string Subcode,
    string SubcodeName,
    int OccurrenceCount,
    decimal TotalDurationMinutes,
    decimal AverageDurationMinutes
);

/// <summary>
/// Stoppage reason summary for top reasons analysis
/// </summary>
/// <param name="CategoryCode">Category code</param>
/// <param name="CategoryName">Category name</param>
/// <param name="Subcode">Subcode</param>
/// <param name="SubcodeName">Subcode name</param>
/// <param name="FullReasonCode">Full reason code</param>
/// <param name="OccurrenceCount">Number of occurrences</param>
/// <param name="TotalDurationMinutes">Total duration in minutes</param>
/// <param name="AverageDurationMinutes">Average duration in minutes</param>
/// <param name="PercentageOfTotalDowntime">Percentage of total downtime</param>
public record StoppageReasonSummary(
    string CategoryCode,
    string CategoryName,
    string Subcode,
    string SubcodeName,
    string FullReasonCode,
    int OccurrenceCount,
    decimal TotalDurationMinutes,
    decimal AverageDurationMinutes,
    decimal PercentageOfTotalDowntime
);
