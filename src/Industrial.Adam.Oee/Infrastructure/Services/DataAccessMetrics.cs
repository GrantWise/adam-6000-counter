using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Oee.Infrastructure.Services;

/// <summary>
/// Data access metrics and telemetry for OEE infrastructure
/// Provides comprehensive monitoring of database operations and performance
/// </summary>
public sealed class DataAccessMetrics : IDisposable
{
    private readonly Meter _meter;
    private readonly Counter<long> _queryCounter;
    private readonly Histogram<double> _queryDuration;
    private readonly Counter<long> _errorCounter;
    private readonly Gauge<int> _activeConnections;
    private readonly ILogger<DataAccessMetrics> _logger;

    /// <summary>
    /// Constructor for data access metrics
    /// </summary>
    /// <param name="logger">Logger instance</param>
    public DataAccessMetrics(ILogger<DataAccessMetrics> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _meter = new Meter("Industrial.Adam.Oee.Infrastructure.DataAccess", "1.0.0");

        _queryCounter = _meter.CreateCounter<long>(
            "oee_database_queries_total",
            "count",
            "Total number of database queries executed");

        _queryDuration = _meter.CreateHistogram<double>(
            "oee_database_query_duration_ms",
            "milliseconds",
            "Duration of database queries in milliseconds");

        _errorCounter = _meter.CreateCounter<long>(
            "oee_database_errors_total",
            "count",
            "Total number of database errors");

        _activeConnections = _meter.CreateGauge<int>(
            "oee_database_connections_active",
            "count",
            "Number of active database connections");
    }

    /// <summary>
    /// Record a database query execution
    /// </summary>
    /// <param name="operation">Type of operation (SELECT, INSERT, UPDATE, DELETE)</param>
    /// <param name="table">Target table</param>
    /// <param name="durationMs">Query duration in milliseconds</param>
    /// <param name="success">Whether the query succeeded</param>
    /// <param name="rowCount">Number of rows affected/returned</param>
    public void RecordQuery(string operation, string table, double durationMs, bool success, int rowCount = 0)
    {
        var tags = new TagList
        {
            { "operation", operation },
            { "table", table },
            { "success", success.ToString().ToLowerInvariant() }
        };

        _queryCounter.Add(1, tags);
        _queryDuration.Record(durationMs, tags);

        if (rowCount > 0)
        {
            tags.Add("row_count_bucket", GetRowCountBucket(rowCount));
        }

        // Log performance warnings
        if (durationMs > 100)
        {
            _logger.LogWarning("Slow query detected: {Operation} on {Table} took {Duration}ms",
                operation, table, durationMs);
        }

        _logger.LogDebug("Database query: {Operation} {Table} - {Duration}ms, {RowCount} rows, Success: {Success}",
            operation, table, durationMs, rowCount, success);
    }

    /// <summary>
    /// Record a database error
    /// </summary>
    /// <param name="operation">Type of operation</param>
    /// <param name="table">Target table</param>
    /// <param name="errorType">Type of error</param>
    /// <param name="exception">Exception details</param>
    public void RecordError(string operation, string table, string errorType, Exception exception)
    {
        var tags = new TagList
        {
            { "operation", operation },
            { "table", table },
            { "error_type", errorType }
        };

        _errorCounter.Add(1, tags);

        _logger.LogError(exception, "Database error: {Operation} on {Table} - {ErrorType}",
            operation, table, errorType);
    }

    /// <summary>
    /// Record active connection count
    /// </summary>
    /// <param name="count">Number of active connections</param>
    public void RecordActiveConnections(int count)
    {
        _activeConnections.Record(count);

        if (count > 50) // Warning threshold
        {
            _logger.LogWarning("High number of active database connections: {Count}", count);
        }
    }

    /// <summary>
    /// Create a scoped query tracker for automatic timing and metrics
    /// </summary>
    /// <param name="operation">Database operation type</param>
    /// <param name="table">Target table name</param>
    /// <returns>Disposable query tracker</returns>
    public ScopedQueryTracker TrackQuery(string operation, string table)
    {
        return new ScopedQueryTracker(this, operation, table);
    }

    /// <summary>
    /// Get row count bucket for metrics grouping
    /// </summary>
    /// <param name="rowCount">Actual row count</param>
    /// <returns>Bucket label</returns>
    private static string GetRowCountBucket(int rowCount)
    {
        return rowCount switch
        {
            <= 10 => "small",
            <= 100 => "medium",
            <= 1000 => "large",
            _ => "very_large"
        };
    }

    /// <summary>
    /// Dispose metrics resources
    /// </summary>
    public void Dispose()
    {
        _meter?.Dispose();
    }
}

/// <summary>
/// Scoped query tracker for automatic timing and metrics collection
/// </summary>
public sealed class ScopedQueryTracker : IDisposable
{
    private readonly DataAccessMetrics _metrics;
    private readonly string _operation;
    private readonly string _table;
    private readonly Stopwatch _stopwatch;
    private bool _disposed;
    private bool _success;
    private int _rowCount;

    /// <summary>
    /// Constructor for scoped query tracker
    /// </summary>
    /// <param name="metrics">Metrics instance</param>
    /// <param name="operation">Database operation</param>
    /// <param name="table">Target table</param>
    internal ScopedQueryTracker(DataAccessMetrics metrics, string operation, string table)
    {
        _metrics = metrics;
        _operation = operation;
        _table = table;
        _stopwatch = Stopwatch.StartNew();
        _success = false;
        _rowCount = 0;
    }

    /// <summary>
    /// Mark the query as successful with optional row count
    /// </summary>
    /// <param name="rowCount">Number of rows affected/returned</param>
    public void MarkSuccess(int rowCount = 0)
    {
        _success = true;
        _rowCount = rowCount;
    }

    /// <summary>
    /// Mark the query as failed
    /// </summary>
    public void MarkFailure()
    {
        _success = false;
    }

    /// <summary>
    /// Dispose and record metrics
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _stopwatch.Stop();
        _metrics.RecordQuery(_operation, _table, _stopwatch.Elapsed.TotalMilliseconds, _success, _rowCount);
        _disposed = true;
    }
}

/// <summary>
/// Data access exception types for categorization
/// </summary>
public static class DataAccessErrorTypes
{
    public const string Connection = "connection";
    public const string Timeout = "timeout";
    public const string Constraint = "constraint";
    public const string NotFound = "not_found";
    public const string Serialization = "serialization";
    public const string Permission = "permission";
    public const string Unknown = "unknown";

    /// <summary>
    /// Categorize exception type
    /// </summary>
    /// <param name="exception">Exception to categorize</param>
    /// <returns>Error type category</returns>
    public static string CategorizeException(Exception exception)
    {
        return exception switch
        {
            TimeoutException => Timeout,
            InvalidOperationException when exception.Message.Contains("connection") => Connection,
            InvalidOperationException when exception.Message.Contains("not found") => NotFound,
            ArgumentException => Constraint,
            UnauthorizedAccessException => Permission,
            _ => Unknown
        };
    }
}
