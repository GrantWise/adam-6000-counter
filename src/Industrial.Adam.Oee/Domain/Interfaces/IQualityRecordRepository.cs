using Industrial.Adam.Oee.Domain.ValueObjects;

namespace Industrial.Adam.Oee.Domain.Interfaces;

/// <summary>
/// Repository interface for QualityRecord value object persistence
/// Provides data access operations for simplified quality tracking
/// </summary>
public interface IQualityRecordRepository
{
    /// <summary>
    /// Get quality records for a specific work order
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of quality records for the work order</returns>
    public Task<IEnumerable<QualityRecord>> GetByWorkOrderIdAsync(string workOrderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get quality records for a work order within a date range
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="startDate">Start date (inclusive)</param>
    /// <param name="endDate">End date (inclusive)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of quality records in the date range</returns>
    public Task<IEnumerable<QualityRecord>> GetByWorkOrderIdAndDateRangeAsync(
        string workOrderId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get quality records by scrap reason code
    /// </summary>
    /// <param name="scrapReasonCode">Scrap reason code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of quality records with the specified scrap reason</returns>
    public Task<IEnumerable<QualityRecord>> GetByScrapReasonCodeAsync(string scrapReasonCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get quality records with issues (scrap count > 0)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of quality records with quality issues</returns>
    public Task<IEnumerable<QualityRecord>> GetRecordsWithIssuesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get quality records within a date range
    /// </summary>
    /// <param name="startDate">Start date (inclusive)</param>
    /// <param name="endDate">End date (inclusive)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of quality records in the date range</returns>
    public Task<IEnumerable<QualityRecord>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new quality record
    /// </summary>
    /// <param name="qualityRecord">Quality record to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Database identifier of the created record</returns>
    public Task<int> AddAsync(QualityRecord qualityRecord, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add multiple quality records in a batch
    /// </summary>
    /// <param name="qualityRecords">Quality records to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of records added</returns>
    public Task<int> AddBatchAsync(IEnumerable<QualityRecord> qualityRecords, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete quality records for a work order
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of records deleted</returns>
    public Task<int> DeleteByWorkOrderIdAsync(string workOrderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete quality records older than specified days
    /// </summary>
    /// <param name="days">Number of days to keep (records older will be deleted)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of records deleted</returns>
    public Task<int> DeleteOldRecordsAsync(int days, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get quality statistics for a work order
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Quality statistics or null if no records found</returns>
    public Task<QualityStatistics?> GetQualityStatisticsAsync(string workOrderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get quality statistics for multiple work orders
    /// </summary>
    /// <param name="workOrderIds">Work order identifiers</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Quality statistics by work order ID</returns>
    public Task<Dictionary<string, QualityStatistics>> GetQualityStatisticsBatchAsync(
        IEnumerable<string> workOrderIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get quality trend data for time-based analysis
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="intervalHours">Interval in hours for data aggregation (default: 1)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Quality trend data points</returns>
    public Task<IEnumerable<QualityTrendData>> GetQualityTrendDataAsync(
        DateTime startDate,
        DateTime endDate,
        int intervalHours = 1,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Quality statistics for a work order
/// </summary>
/// <param name="WorkOrderId">Work order identifier</param>
/// <param name="TotalRecords">Total number of quality records</param>
/// <param name="TotalGoodCount">Total good pieces</param>
/// <param name="TotalScrapCount">Total scrap pieces</param>
/// <param name="TotalPieces">Total pieces (good + scrap)</param>
/// <param name="YieldPercentage">Overall yield percentage</param>
/// <param name="ScrapRatePercentage">Overall scrap rate percentage</param>
/// <param name="RecordsWithIssues">Number of records with quality issues</param>
/// <param name="FirstRecordedAt">Timestamp of first quality record</param>
/// <param name="LastRecordedAt">Timestamp of last quality record</param>
public record QualityStatistics(
    string WorkOrderId,
    int TotalRecords,
    int TotalGoodCount,
    int TotalScrapCount,
    int TotalPieces,
    decimal YieldPercentage,
    decimal ScrapRatePercentage,
    int RecordsWithIssues,
    DateTime FirstRecordedAt,
    DateTime LastRecordedAt
);

/// <summary>
/// Quality trend data for time-based analysis
/// </summary>
/// <param name="TimeInterval">Time interval start</param>
/// <param name="GoodCount">Good pieces in this interval</param>
/// <param name="ScrapCount">Scrap pieces in this interval</param>
/// <param name="TotalPieces">Total pieces in this interval</param>
/// <param name="YieldPercentage">Yield percentage for this interval</param>
/// <param name="RecordCount">Number of quality records in this interval</param>
public record QualityTrendData(
    DateTime TimeInterval,
    int GoodCount,
    int ScrapCount,
    int TotalPieces,
    decimal YieldPercentage,
    int RecordCount
);
