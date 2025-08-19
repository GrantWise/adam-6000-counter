using Industrial.Adam.Oee.Domain.Enums;

namespace Industrial.Adam.Oee.Domain.Interfaces;

/// <summary>
/// Domain service for OEE error handling and reporting
/// </summary>
public interface IIndustrialOeeErrorService
{
    /// <summary>
    /// Log an OEE-related error
    /// </summary>
    /// <param name="errorCode">OEE error code</param>
    /// <param name="message">Error message</param>
    /// <param name="deviceId">Device identifier (optional)</param>
    /// <param name="workOrderId">Work order identifier (optional)</param>
    /// <param name="exception">Exception (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Error tracking identifier</returns>
    public Task<string> LogErrorAsync(
        OeeErrorCode errorCode,
        string message,
        string? deviceId = null,
        string? workOrderId = null,
        Exception? exception = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get error statistics for a device
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="startTime">Start time for statistics</param>
    /// <param name="endTime">End time for statistics</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Error statistics</returns>
    public Task<OeeErrorStatistics> GetErrorStatisticsAsync(
        string deviceId,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get recent errors for a device
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="count">Number of recent errors to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Recent errors</returns>
    public Task<IEnumerable<OeeError>> GetRecentErrorsAsync(
        string deviceId,
        int count = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear resolved errors for a device
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="errorCodes">Specific error codes to clear (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of errors cleared</returns>
    public Task<int> ClearResolvedErrorsAsync(
        string deviceId,
        IEnumerable<OeeErrorCode>? errorCodes = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create an error report for a time period
    /// </summary>
    /// <param name="startTime">Report start time</param>
    /// <param name="endTime">Report end time</param>
    /// <param name="deviceIds">Device identifiers to include (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Error report</returns>
    public Task<OeeErrorReport> CreateErrorReportAsync(
        DateTime startTime,
        DateTime endTime,
        IEnumerable<string>? deviceIds = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// OEE error information
/// </summary>
/// <param name="Id">Error identifier</param>
/// <param name="ErrorCode">OEE error code</param>
/// <param name="Message">Error message</param>
/// <param name="DeviceId">Device identifier</param>
/// <param name="WorkOrderId">Work order identifier</param>
/// <param name="Timestamp">When the error occurred</param>
/// <param name="Severity">Error severity level</param>
/// <param name="IsResolved">Whether the error has been resolved</param>
/// <param name="ResolvedAt">When the error was resolved</param>
public record OeeError(
    string Id,
    OeeErrorCode ErrorCode,
    string Message,
    string? DeviceId,
    string? WorkOrderId,
    DateTime Timestamp,
    string Severity,
    bool IsResolved,
    DateTime? ResolvedAt
);

/// <summary>
/// OEE error statistics
/// </summary>
/// <param name="DeviceId">Device identifier</param>
/// <param name="TotalErrors">Total number of errors</param>
/// <param name="ResolvedErrors">Number of resolved errors</param>
/// <param name="ActiveErrors">Number of active errors</param>
/// <param name="ErrorsByCode">Errors grouped by error code</param>
/// <param name="MostFrequentError">Most frequent error code</param>
/// <param name="AverageResolutionTime">Average resolution time in minutes</param>
public record OeeErrorStatistics(
    string DeviceId,
    int TotalErrors,
    int ResolvedErrors,
    int ActiveErrors,
    IEnumerable<ErrorCodeCount> ErrorsByCode,
    OeeErrorCode? MostFrequentError,
    decimal AverageResolutionTime
);

/// <summary>
/// Error count by error code
/// </summary>
/// <param name="ErrorCode">OEE error code</param>
/// <param name="Count">Number of occurrences</param>
public record ErrorCodeCount(
    OeeErrorCode ErrorCode,
    int Count
);

/// <summary>
/// OEE error report
/// </summary>
/// <param name="StartTime">Report start time</param>
/// <param name="EndTime">Report end time</param>
/// <param name="TotalErrors">Total errors in period</param>
/// <param name="DeviceStatistics">Statistics by device</param>
/// <param name="TopErrors">Most frequent errors</param>
/// <param name="GeneratedAt">When report was generated</param>
public record OeeErrorReport(
    DateTime StartTime,
    DateTime EndTime,
    int TotalErrors,
    IEnumerable<OeeErrorStatistics> DeviceStatistics,
    IEnumerable<ErrorCodeCount> TopErrors,
    DateTime GeneratedAt
);
