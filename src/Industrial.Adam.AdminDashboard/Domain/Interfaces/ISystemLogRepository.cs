using Industrial.Adam.AdminDashboard.Domain.Entities;
using LogLevel = Industrial.Adam.AdminDashboard.Domain.Enums.LogLevel;

namespace Industrial.Adam.AdminDashboard.Domain.Interfaces;

/// <summary>
/// Repository interface for system logs
/// </summary>
public interface ISystemLogRepository
{
    /// <summary>
    /// Add a new log entry
    /// </summary>
    /// <param name="log">The log entry to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The added log entry</returns>
    Task<SystemLog> AddAsync(SystemLog log, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add multiple log entries in batch
    /// </summary>
    /// <param name="logs">Collection of log entries to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of logs added</returns>
    Task<int> AddBatchAsync(IEnumerable<SystemLog> logs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get logs within a time range with optional filters
    /// </summary>
    /// <param name="fromTime">Start time range</param>
    /// <param name="toTime">End time range</param>
    /// <param name="serviceName">Optional service name filter</param>
    /// <param name="logLevel">Optional log level filter</param>
    /// <param name="pageNumber">Page number for pagination</param>
    /// <param name="pageSize">Page size for pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of log entries</returns>
    Task<IEnumerable<SystemLog>> GetLogsAsync(
        DateTime fromTime,
        DateTime toTime,
        string? serviceName = null,
        LogLevel? logLevel = null,
        int pageNumber = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search logs using regex pattern
    /// </summary>
    /// <param name="searchPattern">Regex pattern to search for</param>
    /// <param name="fromTime">Start time range</param>
    /// <param name="toTime">End time range</param>
    /// <param name="serviceName">Optional service name filter</param>
    /// <param name="pageNumber">Page number for pagination</param>
    /// <param name="pageSize">Page size for pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of matching log entries</returns>
    Task<IEnumerable<SystemLog>> SearchLogsAsync(
        string searchPattern,
        DateTime fromTime,
        DateTime toTime,
        string? serviceName = null,
        int pageNumber = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get distinct service names from logs
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of service names</returns>
    Task<IEnumerable<string>> GetServiceNamesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Purge logs older than specified date
    /// </summary>
    /// <param name="olderThan">Date threshold for purging</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of logs purged</returns>
    Task<int> PurgeLogsAsync(DateTime olderThan, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get log count within time range with optional filters
    /// </summary>
    /// <param name="fromTime">Start time range</param>
    /// <param name="toTime">End time range</param>
    /// <param name="serviceName">Optional service name filter</param>
    /// <param name="logLevel">Optional log level filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Count of matching logs</returns>
    Task<int> GetLogCountAsync(
        DateTime fromTime,
        DateTime toTime,
        string? serviceName = null,
        LogLevel? logLevel = null,
        CancellationToken cancellationToken = default);
}