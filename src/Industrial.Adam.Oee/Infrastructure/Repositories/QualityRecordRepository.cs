using System.Data;
using Dapper;
using Industrial.Adam.Oee.Domain.Interfaces;
using Industrial.Adam.Oee.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Oee.Infrastructure.Repositories;

/// <summary>
/// TimescaleDB repository for quality record persistence
/// Provides full CRUD operations for the quality_records table
/// Handles QualityRecord value object persistence and analytics
/// </summary>
public sealed class QualityRecordRepository : IQualityRecordRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<QualityRecordRepository> _logger;

    /// <summary>
    /// Constructor for quality record repository
    /// </summary>
    /// <param name="connectionFactory">Database connection factory</param>
    /// <param name="logger">Logger instance</param>
    public QualityRecordRepository(
        IDbConnectionFactory connectionFactory,
        ILogger<QualityRecordRepository> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get quality records for a specific work order
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of quality records for the work order</returns>
    public async Task<IEnumerable<QualityRecord>> GetByWorkOrderIdAsync(string workOrderId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workOrderId))
            throw new ArgumentException("Work order ID cannot be null or empty", nameof(workOrderId));

        using var activity = ActivitySource.StartActivity("GetQualityRecordsByWorkOrderId");
        activity?.SetTag("workOrderId", workOrderId);

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = @"
                SELECT 
                    work_order_id,
                    good_count,
                    scrap_count,
                    scrap_reason_code,
                    notes,
                    recorded_at
                FROM quality_records 
                WHERE work_order_id = @workOrderId
                ORDER BY recorded_at ASC";

            _logger.LogDebug("Retrieving quality records for work order {WorkOrderId}", workOrderId);

            var qualityRecordDataList = await connection.QueryAsync<QualityRecordData>(sql, new { workOrderId });
            var qualityRecords = qualityRecordDataList.Select(MapToQualityRecord).ToList();

            _logger.LogDebug("Retrieved {Count} quality records for work order {WorkOrderId}",
                qualityRecords.Count, workOrderId);

            return qualityRecords;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve quality records for work order {WorkOrderId}", workOrderId);
            throw;
        }
    }

    /// <summary>
    /// Get quality records for a work order within a date range
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="startDate">Start date (inclusive)</param>
    /// <param name="endDate">End date (inclusive)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of quality records in the date range</returns>
    public async Task<IEnumerable<QualityRecord>> GetByWorkOrderIdAndDateRangeAsync(
        string workOrderId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workOrderId))
            throw new ArgumentException("Work order ID cannot be null or empty", nameof(workOrderId));

        if (endDate < startDate)
            throw new ArgumentException("End date must be greater than or equal to start date", nameof(endDate));

        using var activity = ActivitySource.StartActivity("GetQualityRecordsByWorkOrderIdAndDateRange");
        activity?.SetTag("workOrderId", workOrderId);

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = @"
                SELECT 
                    work_order_id,
                    good_count,
                    scrap_count,
                    scrap_reason_code,
                    notes,
                    recorded_at
                FROM quality_records 
                WHERE work_order_id = @workOrderId
                  AND recorded_at >= @startDate
                  AND recorded_at <= @endDate
                ORDER BY recorded_at ASC";

            var parameters = new { workOrderId, startDate, endDate };

            _logger.LogDebug("Retrieving quality records for work order {WorkOrderId} from {StartDate} to {EndDate}",
                workOrderId, startDate, endDate);

            var qualityRecordDataList = await connection.QueryAsync<QualityRecordData>(sql, parameters);
            var qualityRecords = qualityRecordDataList.Select(MapToQualityRecord).ToList();

            _logger.LogDebug("Retrieved {Count} quality records for work order {WorkOrderId} in date range",
                qualityRecords.Count, workOrderId);

            return qualityRecords;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve quality records for work order {WorkOrderId} in date range", workOrderId);
            throw;
        }
    }

    /// <summary>
    /// Get quality records by scrap reason code
    /// </summary>
    /// <param name="scrapReasonCode">Scrap reason code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of quality records with the specified scrap reason</returns>
    public async Task<IEnumerable<QualityRecord>> GetByScrapReasonCodeAsync(string scrapReasonCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(scrapReasonCode))
            throw new ArgumentException("Scrap reason code cannot be null or empty", nameof(scrapReasonCode));

        using var activity = ActivitySource.StartActivity("GetQualityRecordsByScrapReasonCode");
        activity?.SetTag("scrapReasonCode", scrapReasonCode);

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = @"
                SELECT 
                    work_order_id,
                    good_count,
                    scrap_count,
                    scrap_reason_code,
                    notes,
                    recorded_at
                FROM quality_records 
                WHERE scrap_reason_code = @scrapReasonCode
                ORDER BY recorded_at DESC
                LIMIT 1000"; // Reasonable limit to prevent excessive memory usage

            _logger.LogDebug("Retrieving quality records for scrap reason code {ScrapReasonCode}", scrapReasonCode);

            var qualityRecordDataList = await connection.QueryAsync<QualityRecordData>(sql, new { scrapReasonCode });
            var qualityRecords = qualityRecordDataList.Select(MapToQualityRecord).ToList();

            _logger.LogInformation("Retrieved {Count} quality records for scrap reason code {ScrapReasonCode}",
                qualityRecords.Count, scrapReasonCode);

            return qualityRecords;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve quality records for scrap reason code {ScrapReasonCode}", scrapReasonCode);
            throw;
        }
    }

    /// <summary>
    /// Get quality records with issues (scrap count > 0)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of quality records with quality issues</returns>
    public async Task<IEnumerable<QualityRecord>> GetRecordsWithIssuesAsync(CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("GetQualityRecordsWithIssues");

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = @"
                SELECT 
                    work_order_id,
                    good_count,
                    scrap_count,
                    scrap_reason_code,
                    notes,
                    recorded_at
                FROM quality_records 
                WHERE scrap_count > 0
                ORDER BY recorded_at DESC
                LIMIT 1000"; // Reasonable limit to prevent excessive memory usage

            _logger.LogDebug("Retrieving quality records with issues");

            var qualityRecordDataList = await connection.QueryAsync<QualityRecordData>(sql);
            var qualityRecords = qualityRecordDataList.Select(MapToQualityRecord).ToList();

            _logger.LogInformation("Retrieved {Count} quality records with issues", qualityRecords.Count);

            return qualityRecords;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve quality records with issues");
            throw;
        }
    }

    /// <summary>
    /// Get quality records within a date range
    /// </summary>
    /// <param name="startDate">Start date (inclusive)</param>
    /// <param name="endDate">End date (inclusive)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of quality records in the date range</returns>
    public async Task<IEnumerable<QualityRecord>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        if (endDate < startDate)
            throw new ArgumentException("End date must be greater than or equal to start date", nameof(endDate));

        using var activity = ActivitySource.StartActivity("GetQualityRecordsByDateRange");

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = @"
                SELECT 
                    work_order_id,
                    good_count,
                    scrap_count,
                    scrap_reason_code,
                    notes,
                    recorded_at
                FROM quality_records 
                WHERE recorded_at >= @startDate
                  AND recorded_at <= @endDate
                ORDER BY recorded_at ASC";

            var parameters = new { startDate, endDate };

            _logger.LogDebug("Retrieving quality records from {StartDate} to {EndDate}", startDate, endDate);

            var qualityRecordDataList = await connection.QueryAsync<QualityRecordData>(sql, parameters);
            var qualityRecords = qualityRecordDataList.Select(MapToQualityRecord).ToList();

            _logger.LogInformation("Retrieved {Count} quality records in date range", qualityRecords.Count);

            return qualityRecords;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve quality records in date range");
            throw;
        }
    }

    /// <summary>
    /// Add a new quality record
    /// </summary>
    /// <param name="qualityRecord">Quality record to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Database identifier of the created record</returns>
    public async Task<int> AddAsync(QualityRecord qualityRecord, CancellationToken cancellationToken = default)
    {
        if (qualityRecord == null)
            throw new ArgumentNullException(nameof(qualityRecord));

        using var activity = ActivitySource.StartActivity("AddQualityRecord");
        activity?.SetTag("workOrderId", qualityRecord.WorkOrderId);

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = @"
                INSERT INTO quality_records (
                    work_order_id,
                    good_count,
                    scrap_count,
                    scrap_reason_code,
                    notes,
                    recorded_at,
                    created_at
                ) VALUES (
                    @WorkOrderId,
                    @GoodCount,
                    @ScrapCount,
                    @ScrapReasonCode,
                    @Notes,
                    @RecordedAt,
                    NOW()
                ) RETURNING id";

            var parameters = new
            {
                qualityRecord.WorkOrderId,
                qualityRecord.GoodCount,
                qualityRecord.ScrapCount,
                qualityRecord.ScrapReasonCode,
                qualityRecord.Notes,
                qualityRecord.RecordedAt
            };

            _logger.LogDebug("Adding quality record for work order {WorkOrderId}: {GoodCount} good, {ScrapCount} scrap",
                qualityRecord.WorkOrderId, qualityRecord.GoodCount, qualityRecord.ScrapCount);

            var newId = await connection.QuerySingleAsync<int>(sql, parameters);

            _logger.LogInformation("Added quality record for work order {WorkOrderId} with ID {Id} (Yield: {Yield}%)",
                qualityRecord.WorkOrderId, newId, qualityRecord.YieldPercentage);

            return newId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add quality record for work order {WorkOrderId}", qualityRecord.WorkOrderId);
            throw;
        }
    }

    /// <summary>
    /// Add multiple quality records in a batch
    /// </summary>
    /// <param name="qualityRecords">Quality records to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of records added</returns>
    public async Task<int> AddBatchAsync(IEnumerable<QualityRecord> qualityRecords, CancellationToken cancellationToken = default)
    {
        if (qualityRecords == null)
            throw new ArgumentNullException(nameof(qualityRecords));

        var qualityRecordsList = qualityRecords.ToList();
        if (!qualityRecordsList.Any())
            return 0;

        using var activity = ActivitySource.StartActivity("AddQualityRecordsBatch");
        activity?.SetTag("recordCount", qualityRecordsList.Count);

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = @"
                INSERT INTO quality_records (
                    work_order_id,
                    good_count,
                    scrap_count,
                    scrap_reason_code,
                    notes,
                    recorded_at,
                    created_at
                ) VALUES (
                    @WorkOrderId,
                    @GoodCount,
                    @ScrapCount,
                    @ScrapReasonCode,
                    @Notes,
                    @RecordedAt,
                    NOW()
                )";

            var parameters = qualityRecordsList.Select(qr => new
            {
                qr.WorkOrderId,
                qr.GoodCount,
                qr.ScrapCount,
                qr.ScrapReasonCode,
                qr.Notes,
                qr.RecordedAt
            });

            _logger.LogDebug("Adding batch of {Count} quality records", qualityRecordsList.Count);

            var rowsAffected = await connection.ExecuteAsync(sql, parameters);

            _logger.LogInformation("Added batch of {Count} quality records", rowsAffected);

            return rowsAffected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add batch of {Count} quality records", qualityRecordsList.Count);
            throw;
        }
    }

    /// <summary>
    /// Delete quality records for a work order
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of records deleted</returns>
    public async Task<int> DeleteByWorkOrderIdAsync(string workOrderId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workOrderId))
            throw new ArgumentException("Work order ID cannot be null or empty", nameof(workOrderId));

        using var activity = ActivitySource.StartActivity("DeleteQualityRecordsByWorkOrderId");
        activity?.SetTag("workOrderId", workOrderId);

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = "DELETE FROM quality_records WHERE work_order_id = @workOrderId";

            _logger.LogDebug("Deleting quality records for work order {WorkOrderId}", workOrderId);

            var rowsAffected = await connection.ExecuteAsync(sql, new { workOrderId });

            _logger.LogInformation("Deleted {Count} quality records for work order {WorkOrderId}",
                rowsAffected, workOrderId);

            return rowsAffected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete quality records for work order {WorkOrderId}", workOrderId);
            throw;
        }
    }

    /// <summary>
    /// Delete quality records older than specified days
    /// </summary>
    /// <param name="days">Number of days to keep (records older will be deleted)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of records deleted</returns>
    public async Task<int> DeleteOldRecordsAsync(int days, CancellationToken cancellationToken = default)
    {
        if (days <= 0)
            throw new ArgumentException("Days must be greater than zero", nameof(days));

        using var activity = ActivitySource.StartActivity("DeleteOldQualityRecords");
        activity?.SetTag("days", days);

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = @"
                DELETE FROM quality_records 
                WHERE recorded_at < NOW() - INTERVAL '@days days'";

            _logger.LogDebug("Deleting quality records older than {Days} days", days);

            var rowsAffected = await connection.ExecuteAsync(sql, new { days });

            _logger.LogInformation("Deleted {Count} quality records older than {Days} days",
                rowsAffected, days);

            return rowsAffected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete quality records older than {Days} days", days);
            throw;
        }
    }

    /// <summary>
    /// Get quality statistics for a work order
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Quality statistics or null if no records found</returns>
    public async Task<QualityStatistics?> GetQualityStatisticsAsync(string workOrderId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workOrderId))
            throw new ArgumentException("Work order ID cannot be null or empty", nameof(workOrderId));

        using var activity = ActivitySource.StartActivity("GetQualityStatistics");
        activity?.SetTag("workOrderId", workOrderId);

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = @"
                SELECT 
                    @workOrderId as WorkOrderId,
                    COUNT(*) as TotalRecords,
                    COALESCE(SUM(good_count), 0) as TotalGoodCount,
                    COALESCE(SUM(scrap_count), 0) as TotalScrapCount,
                    COALESCE(SUM(good_count + scrap_count), 0) as TotalPieces,
                    CASE 
                        WHEN COALESCE(SUM(good_count + scrap_count), 0) = 0 THEN 100.0
                        ELSE (COALESCE(SUM(good_count), 0)::DECIMAL / COALESCE(SUM(good_count + scrap_count), 0)) * 100.0
                    END as YieldPercentage,
                    CASE 
                        WHEN COALESCE(SUM(good_count + scrap_count), 0) = 0 THEN 0.0
                        ELSE (COALESCE(SUM(scrap_count), 0)::DECIMAL / COALESCE(SUM(good_count + scrap_count), 0)) * 100.0
                    END as ScrapRatePercentage,
                    COUNT(CASE WHEN scrap_count > 0 THEN 1 END) as RecordsWithIssues,
                    COALESCE(MIN(recorded_at), NOW()) as FirstRecordedAt,
                    COALESCE(MAX(recorded_at), NOW()) as LastRecordedAt
                FROM quality_records 
                WHERE work_order_id = @workOrderId";

            _logger.LogDebug("Calculating quality statistics for work order {WorkOrderId}", workOrderId);

            var statisticsData = await connection.QuerySingleOrDefaultAsync<QualityStatisticsData>(sql, new { workOrderId });

            if (statisticsData == null || statisticsData.TotalRecords == 0)
            {
                _logger.LogDebug("No quality records found for work order {WorkOrderId}", workOrderId);
                return null;
            }

            var statistics = new QualityStatistics(
                statisticsData.WorkOrderId,
                statisticsData.TotalRecords,
                statisticsData.TotalGoodCount,
                statisticsData.TotalScrapCount,
                statisticsData.TotalPieces,
                Math.Round(statisticsData.YieldPercentage, 2),
                Math.Round(statisticsData.ScrapRatePercentage, 2),
                statisticsData.RecordsWithIssues,
                statisticsData.FirstRecordedAt,
                statisticsData.LastRecordedAt
            );

            _logger.LogInformation("Calculated quality statistics for work order {WorkOrderId}: {TotalPieces} pieces, {Yield}% yield",
                workOrderId, statistics.TotalPieces, statistics.YieldPercentage);

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get quality statistics for work order {WorkOrderId}", workOrderId);
            throw;
        }
    }

    /// <summary>
    /// Get quality statistics for multiple work orders
    /// </summary>
    /// <param name="workOrderIds">Work order identifiers</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Quality statistics by work order ID</returns>
    public async Task<Dictionary<string, QualityStatistics>> GetQualityStatisticsBatchAsync(
        IEnumerable<string> workOrderIds,
        CancellationToken cancellationToken = default)
    {
        if (workOrderIds == null)
            throw new ArgumentNullException(nameof(workOrderIds));

        var workOrderIdsList = workOrderIds.ToList();
        if (!workOrderIdsList.Any())
            return new Dictionary<string, QualityStatistics>();

        using var activity = ActivitySource.StartActivity("GetQualityStatisticsBatch");
        activity?.SetTag("workOrderCount", workOrderIdsList.Count);

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = @"
                SELECT 
                    work_order_id as WorkOrderId,
                    COUNT(*) as TotalRecords,
                    COALESCE(SUM(good_count), 0) as TotalGoodCount,
                    COALESCE(SUM(scrap_count), 0) as TotalScrapCount,
                    COALESCE(SUM(good_count + scrap_count), 0) as TotalPieces,
                    CASE 
                        WHEN COALESCE(SUM(good_count + scrap_count), 0) = 0 THEN 100.0
                        ELSE (COALESCE(SUM(good_count), 0)::DECIMAL / COALESCE(SUM(good_count + scrap_count), 0)) * 100.0
                    END as YieldPercentage,
                    CASE 
                        WHEN COALESCE(SUM(good_count + scrap_count), 0) = 0 THEN 0.0
                        ELSE (COALESCE(SUM(scrap_count), 0)::DECIMAL / COALESCE(SUM(good_count + scrap_count), 0)) * 100.0
                    END as ScrapRatePercentage,
                    COUNT(CASE WHEN scrap_count > 0 THEN 1 END) as RecordsWithIssues,
                    COALESCE(MIN(recorded_at), NOW()) as FirstRecordedAt,
                    COALESCE(MAX(recorded_at), NOW()) as LastRecordedAt
                FROM quality_records 
                WHERE work_order_id = ANY(@workOrderIds)
                GROUP BY work_order_id";

            _logger.LogDebug("Calculating quality statistics for {Count} work orders", workOrderIdsList.Count);

            var statisticsDataList = await connection.QueryAsync<QualityStatisticsData>(sql,
                new { workOrderIds = workOrderIdsList.ToArray() });

            var statisticsByWorkOrder = statisticsDataList.ToDictionary(
                data => data.WorkOrderId,
                data => new QualityStatistics(
                    data.WorkOrderId,
                    data.TotalRecords,
                    data.TotalGoodCount,
                    data.TotalScrapCount,
                    data.TotalPieces,
                    Math.Round(data.YieldPercentage, 2),
                    Math.Round(data.ScrapRatePercentage, 2),
                    data.RecordsWithIssues,
                    data.FirstRecordedAt,
                    data.LastRecordedAt
                )
            );

            _logger.LogInformation("Calculated quality statistics for {Count} work orders with data",
                statisticsByWorkOrder.Count);

            return statisticsByWorkOrder;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get quality statistics for {Count} work orders", workOrderIdsList.Count);
            throw;
        }
    }

    /// <summary>
    /// Get quality trend data for time-based analysis
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="intervalHours">Interval in hours for data aggregation (default: 1)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Quality trend data points</returns>
    public async Task<IEnumerable<QualityTrendData>> GetQualityTrendDataAsync(
        DateTime startDate,
        DateTime endDate,
        int intervalHours = 1,
        CancellationToken cancellationToken = default)
    {
        if (endDate < startDate)
            throw new ArgumentException("End date must be greater than or equal to start date", nameof(endDate));

        if (intervalHours <= 0 || intervalHours > 24)
            throw new ArgumentException("Interval hours must be between 1 and 24", nameof(intervalHours));

        using var activity = ActivitySource.StartActivity("GetQualityTrendData");

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            // TimescaleDB time-bucket aggregation for performance
            const string sql = @"
                SELECT 
                    time_bucket(INTERVAL '@intervalHours hours', recorded_at) as TimeInterval,
                    COALESCE(SUM(good_count), 0) as GoodCount,
                    COALESCE(SUM(scrap_count), 0) as ScrapCount,
                    COALESCE(SUM(good_count + scrap_count), 0) as TotalPieces,
                    CASE 
                        WHEN COALESCE(SUM(good_count + scrap_count), 0) = 0 THEN 100.0
                        ELSE (COALESCE(SUM(good_count), 0)::DECIMAL / COALESCE(SUM(good_count + scrap_count), 0)) * 100.0
                    END as YieldPercentage,
                    COUNT(*) as RecordCount
                FROM quality_records 
                WHERE recorded_at >= @startDate
                  AND recorded_at <= @endDate
                GROUP BY TimeInterval
                ORDER BY TimeInterval ASC";

            var parameters = new { startDate, endDate, intervalHours };

            _logger.LogDebug("Retrieving quality trend data from {StartDate} to {EndDate} with {IntervalHours}h intervals",
                startDate, endDate, intervalHours);

            var trendDataList = await connection.QueryAsync<QualityTrendDataRaw>(sql, parameters);
            var trendData = trendDataList.Select(data => new QualityTrendData(
                data.TimeInterval,
                data.GoodCount,
                data.ScrapCount,
                data.TotalPieces,
                Math.Round(data.YieldPercentage, 2),
                data.RecordCount
            )).ToList();

            _logger.LogInformation("Retrieved {Count} quality trend data points", trendData.Count);

            return trendData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve quality trend data");
            throw;
        }
    }

    /// <summary>
    /// Maps database row data to QualityRecord value object
    /// </summary>
    /// <param name="data">Database row data</param>
    /// <returns>QualityRecord value object</returns>
    private static QualityRecord MapToQualityRecord(QualityRecordData data)
    {
        return new QualityRecord(
            data.WorkOrderId,
            data.GoodCount,
            data.ScrapCount,
            data.ScrapReasonCode,
            data.Notes
        );
    }

    private static readonly System.Diagnostics.ActivitySource ActivitySource = new("Industrial.Adam.Oee.Infrastructure");

    /// <summary>
    /// Data structure for mapping database rows to quality records
    /// </summary>
    private sealed record QualityRecordData(
        string WorkOrderId,
        int GoodCount,
        int ScrapCount,
        string? ScrapReasonCode,
        string? Notes,
        DateTime RecordedAt
    );

    /// <summary>
    /// Data structure for mapping quality statistics from database queries
    /// </summary>
    private sealed record QualityStatisticsData(
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
    /// Raw data structure for quality trend data from database
    /// </summary>
    private sealed record QualityTrendDataRaw(
        DateTime TimeInterval,
        int GoodCount,
        int ScrapCount,
        int TotalPieces,
        decimal YieldPercentage,
        int RecordCount
    );
}
