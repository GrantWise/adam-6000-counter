using System.Diagnostics;
using System.Text.Json;
using Industrial.Adam.Oee.Domain.Enums;
using Industrial.Adam.Oee.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Oee.Domain.Services;

/// <summary>
/// Industrial-grade error handling service for OEE operations
/// Provides structured error handling with categorization, logging, and alerting
/// Duplicated pattern from Industrial.Adam.Logger to maintain independence
/// </summary>
public interface IIndustrialOeeErrorService
{
    /// <summary>
    /// Handle an OEE exception with proper categorization and logging
    /// </summary>
    /// <param name="exception">OEE exception to handle</param>
    /// <param name="context">Additional context</param>
    /// <returns>Error tracking ID</returns>
    public Task<string> HandleExceptionAsync(OeeException exception, Dictionary<string, object>? context = null);

    /// <summary>
    /// Handle a general exception by converting to OEE exception
    /// </summary>
    /// <param name="exception">General exception</param>
    /// <param name="operation">Operation that failed</param>
    /// <param name="deviceId">Device ID (optional)</param>
    /// <param name="workOrderId">Work order ID (optional)</param>
    /// <returns>Error tracking ID</returns>
    public Task<string> HandleExceptionAsync(Exception exception, string operation, string? deviceId = null, string? workOrderId = null);

    /// <summary>
    /// Create and handle a calculation exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="calculationType">Type of calculation</param>
    /// <param name="deviceId">Device ID</param>
    /// <param name="startTime">Period start time</param>
    /// <param name="endTime">Period end time</param>
    /// <param name="innerException">Inner exception</param>
    /// <returns>Error tracking ID</returns>
    public Task<string> HandleCalculationErrorAsync(string message, string calculationType, string deviceId, DateTime? startTime = null, DateTime? endTime = null, Exception? innerException = null);

    /// <summary>
    /// Create and handle a work order exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="workOrderId">Work order ID</param>
    /// <param name="operation">Operation attempted</param>
    /// <param name="currentStatus">Current work order status</param>
    /// <param name="innerException">Inner exception</param>
    /// <returns>Error tracking ID</returns>
    public Task<string> HandleWorkOrderErrorAsync(string message, string workOrderId, string operation, string? currentStatus = null, Exception? innerException = null);

    /// <summary>
    /// Create and handle a data access exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="dataSource">Data source name</param>
    /// <param name="deviceId">Device ID</param>
    /// <param name="query">Query or operation</param>
    /// <param name="innerException">Inner exception</param>
    /// <returns>Error tracking ID</returns>
    public Task<string> HandleDataErrorAsync(string message, string dataSource, string? deviceId = null, string? query = null, Exception? innerException = null);

    /// <summary>
    /// Get error statistics for monitoring
    /// </summary>
    /// <param name="since">Get stats since this time</param>
    /// <returns>Error statistics</returns>
    public Task<ErrorStatistics> GetErrorStatisticsAsync(DateTimeOffset? since = null);

    /// <summary>
    /// Check if an error type should trigger an alert
    /// </summary>
    /// <param name="errorCode">Error code</param>
    /// <param name="deviceId">Device ID</param>
    /// <returns>True if alert should be triggered</returns>
    public bool ShouldTriggerAlert(OeeErrorCode errorCode, string? deviceId = null);
}

/// <summary>
/// Implementation of industrial OEE error service
/// </summary>
public sealed class IndustrialOeeErrorService : IIndustrialOeeErrorService
{
    private readonly ILogger<IndustrialOeeErrorService> _logger;
    private readonly ErrorTracker _errorTracker;
    private readonly AlertThresholds _alertThresholds;

    /// <summary>
    /// Initialize the error service
    /// </summary>
    /// <param name="logger">Logger instance</param>
    public IndustrialOeeErrorService(ILogger<IndustrialOeeErrorService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _errorTracker = new ErrorTracker();
        _alertThresholds = new AlertThresholds();

        _logger.LogInformation("Industrial OEE Error Service initialized");
    }

    /// <inheritdoc />
    public async Task<string> HandleExceptionAsync(OeeException exception, Dictionary<string, object>? context = null)
    {
        var errorId = GenerateErrorId();
        var enrichedException = EnrichException(exception, errorId, context);

        // Track the error
        await _errorTracker.TrackErrorAsync(enrichedException);

        // Log with appropriate level based on severity
        LogException(enrichedException, errorId);

        // Check if alert should be triggered
        if (ShouldTriggerAlert(exception.ErrorCode, exception.DeviceId))
        {
            await TriggerAlertAsync(enrichedException, errorId);
        }

        return errorId;
    }

    /// <inheritdoc />
    public async Task<string> HandleExceptionAsync(Exception exception, string operation, string? deviceId = null, string? workOrderId = null)
    {
        var oeeException = ConvertToOeeException(exception, operation, deviceId, workOrderId);
        return await HandleExceptionAsync(oeeException);
    }

    /// <inheritdoc />
    public async Task<string> HandleCalculationErrorAsync(string message, string calculationType, string deviceId, DateTime? startTime = null, DateTime? endTime = null, Exception? innerException = null)
    {
        var exception = new OeeCalculationException(message, calculationType, OeeErrorCode.CalculationFailed, deviceId, startTime, endTime, innerException);
        return await HandleExceptionAsync(exception);
    }

    /// <inheritdoc />
    public async Task<string> HandleWorkOrderErrorAsync(string message, string workOrderId, string operation, string? currentStatus = null, Exception? innerException = null)
    {
        var exception = new WorkOrderException(message, workOrderId, OeeErrorCode.WorkOrderValidationFailed, currentStatus, operation, innerException);
        return await HandleExceptionAsync(exception);
    }

    /// <inheritdoc />
    public async Task<string> HandleDataErrorAsync(string message, string dataSource, string? deviceId = null, string? query = null, Exception? innerException = null)
    {
        var exception = new OeeDataException(message, dataSource, OeeErrorCode.DataNotAvailable, deviceId, query, innerException);
        return await HandleExceptionAsync(exception);
    }

    /// <inheritdoc />
    public async Task<ErrorStatistics> GetErrorStatisticsAsync(DateTimeOffset? since = null)
    {
        return await _errorTracker.GetStatisticsAsync(since ?? DateTimeOffset.UtcNow.AddHours(-24));
    }

    /// <inheritdoc />
    public bool ShouldTriggerAlert(OeeErrorCode errorCode, string? deviceId = null)
    {
        // Check threshold-based alerting
        var recentErrorCount = _errorTracker.GetRecentErrorCount(errorCode, deviceId, TimeSpan.FromMinutes(15));
        var threshold = _alertThresholds.GetThreshold(errorCode);

        if (recentErrorCount >= threshold)
        {
            return true;
        }

        // Always alert on critical errors
        if (errorCode.GetSeverity() == ErrorSeverity.Critical)
        {
            return true;
        }

        return false;
    }

    private static string GenerateErrorId()
    {
        return $"OEE-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}"[..32];
    }

    private static OeeException EnrichException(OeeException exception, string errorId, Dictionary<string, object>? context)
    {
        exception.WithContext("ErrorId", errorId)
                 .WithContext("Environment", Environment.MachineName)
                 .WithContext("ProcessId", Environment.ProcessId)
                 .WithContext("ThreadId", Environment.CurrentManagedThreadId);

        if (context != null)
        {
            foreach (var kvp in context)
            {
                exception.WithContext(kvp.Key, kvp.Value);
            }
        }

        return exception;
    }

    private void LogException(OeeException exception, string errorId)
    {
        var logLevel = exception.Severity switch
        {
            ErrorSeverity.Critical => LogLevel.Critical,
            ErrorSeverity.High => LogLevel.Error,
            ErrorSeverity.Medium => LogLevel.Warning,
            ErrorSeverity.Low => LogLevel.Information,
            _ => LogLevel.Warning
        };

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["ErrorId"] = errorId,
            ["ErrorCode"] = exception.ErrorCode.ToString(),
            ["ErrorCategory"] = exception.ErrorCode.GetCategory(),
            ["Severity"] = exception.Severity.ToString(),
            ["DeviceId"] = exception.DeviceId ?? "N/A",
            ["WorkOrderId"] = exception.WorkOrderId ?? "N/A"
        });

        _logger.Log(logLevel, exception,
            "OEE Error [{ErrorCode}]: {Message} | {ErrorDetails}",
            exception.ErrorCode, exception.Message, exception.GetErrorDetails());
    }

    private static OeeException ConvertToOeeException(Exception exception, string operation, string? deviceId, string? workOrderId)
    {
        return exception switch
        {
            OeeException oeeEx => oeeEx,

            ArgumentException argEx when argEx.ParamName?.Contains("workOrder", StringComparison.OrdinalIgnoreCase) == true =>
                new WorkOrderException(argEx.Message, workOrderId ?? "Unknown", OeeErrorCode.WorkOrderValidationFailed, null, operation, argEx),

            InvalidOperationException invalidOpEx when invalidOpEx.Message.Contains("work order", StringComparison.OrdinalIgnoreCase) =>
                new WorkOrderException(invalidOpEx.Message, workOrderId ?? "Unknown", OeeErrorCode.InvalidWorkOrderState, null, operation, invalidOpEx),

            TimeoutException timeoutEx =>
                new OeeDataException($"Operation timed out: {operation}", "Database", OeeErrorCode.DatabaseTimeout, deviceId, operation, timeoutEx),

            InvalidOperationException invalidOpEx =>
                new OeeCalculationException($"Calculation failed: {operation}", operation, OeeErrorCode.CalculationFailed, deviceId, null, null, invalidOpEx),

            _ => new OeeDataException($"Unexpected error in {operation}: {exception.Message}", "Unknown", OeeErrorCode.Unknown, deviceId, operation, exception)
        };
    }

    private async Task TriggerAlertAsync(OeeException exception, string errorId)
    {
        // In a real implementation, this would integrate with alerting systems
        // For now, log as critical and potentially send to monitoring systems

        _logger.LogCritical(
            "ALERT TRIGGERED - Error ID: {ErrorId}, Code: {ErrorCode}, Device: {DeviceId}, WorkOrder: {WorkOrderId}, Message: {Message}",
            errorId, exception.ErrorCode, exception.DeviceId ?? "N/A", exception.WorkOrderId ?? "N/A", exception.Message);

        // Future: Send to external alerting systems (email, SMS, Slack, etc.)
        await Task.CompletedTask;
    }
}

/// <summary>
/// Tracks error occurrences for statistics and alerting
/// </summary>
internal sealed class ErrorTracker
{
    private readonly List<ErrorOccurrence> _recentErrors = new();
    private readonly object _lock = new();
    private const int MaxRecentErrors = 1000;

    public async Task TrackErrorAsync(OeeException exception)
    {
        var occurrence = new ErrorOccurrence
        {
            ErrorCode = exception.ErrorCode,
            DeviceId = exception.DeviceId,
            WorkOrderId = exception.WorkOrderId,
            Timestamp = exception.Timestamp,
            Severity = exception.Severity
        };

        lock (_lock)
        {
            _recentErrors.Add(occurrence);

            // Keep only recent errors to prevent memory growth
            if (_recentErrors.Count > MaxRecentErrors)
            {
                var cutoff = DateTimeOffset.UtcNow.AddHours(-24);
                _recentErrors.RemoveAll(e => e.Timestamp < cutoff);
            }
        }

        await Task.CompletedTask;
    }

    public int GetRecentErrorCount(OeeErrorCode errorCode, string? deviceId, TimeSpan window)
    {
        var cutoff = DateTimeOffset.UtcNow - window;

        lock (_lock)
        {
            return _recentErrors.Count(e =>
                e.ErrorCode == errorCode &&
                e.Timestamp >= cutoff &&
                (deviceId == null || e.DeviceId == deviceId));
        }
    }

    public async Task<ErrorStatistics> GetStatisticsAsync(DateTimeOffset since)
    {
        List<ErrorOccurrence> relevantErrors;

        lock (_lock)
        {
            relevantErrors = _recentErrors.Where(e => e.Timestamp >= since).ToList();
        }

        var stats = new ErrorStatistics
        {
            TotalErrors = relevantErrors.Count,
            CriticalErrors = relevantErrors.Count(e => e.Severity == ErrorSeverity.Critical),
            HighErrors = relevantErrors.Count(e => e.Severity == ErrorSeverity.High),
            MediumErrors = relevantErrors.Count(e => e.Severity == ErrorSeverity.Medium),
            LowErrors = relevantErrors.Count(e => e.Severity == ErrorSeverity.Low),
            ErrorsByCode = relevantErrors.GroupBy(e => e.ErrorCode)
                                      .ToDictionary(g => g.Key.ToString(), g => g.Count()),
            ErrorsByDevice = relevantErrors.Where(e => !string.IsNullOrEmpty(e.DeviceId))
                                        .GroupBy(e => e.DeviceId!)
                                        .ToDictionary(g => g.Key, g => g.Count())
        };

        return await Task.FromResult(stats);
    }
}

/// <summary>
/// Alert thresholds for different error types
/// </summary>
internal sealed class AlertThresholds
{
    private readonly Dictionary<OeeErrorCode, int> _thresholds = new()
    {
        { OeeErrorCode.DatabaseConnectionFailed, 1 }, // Alert immediately
        { OeeErrorCode.DatabaseTimeout, 3 }, // Alert after 3 timeouts in 15 minutes
        { OeeErrorCode.DeviceNotResponding, 2 }, // Alert after 2 failures
        { OeeErrorCode.CalculationFailed, 5 }, // Alert after 5 calculation failures
        { OeeErrorCode.DataNotAvailable, 10 }, // Alert after 10 data unavailable errors
    };

    public int GetThreshold(OeeErrorCode errorCode)
    {
        return _thresholds.TryGetValue(errorCode, out var threshold) ? threshold : 10; // Default threshold
    }
}

/// <summary>
/// Represents an error occurrence for tracking
/// </summary>
internal sealed record ErrorOccurrence
{
    public OeeErrorCode ErrorCode { get; init; }
    public string? DeviceId { get; init; }
    public string? WorkOrderId { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public ErrorSeverity Severity { get; init; }
}

/// <summary>
/// Error statistics for monitoring
/// </summary>
public sealed record ErrorStatistics
{
    /// <summary>
    /// Total number of errors across all severity levels
    /// </summary>
    public int TotalErrors { get; init; }

    /// <summary>
    /// Number of critical errors
    /// </summary>
    public int CriticalErrors { get; init; }

    /// <summary>
    /// Number of high severity errors
    /// </summary>
    public int HighErrors { get; init; }

    /// <summary>
    /// Number of medium severity errors
    /// </summary>
    public int MediumErrors { get; init; }

    /// <summary>
    /// Number of low severity errors
    /// </summary>
    public int LowErrors { get; init; }

    /// <summary>
    /// Error count by error code
    /// </summary>
    public Dictionary<string, int> ErrorsByCode { get; init; } = new();

    /// <summary>
    /// Error count by device ID
    /// </summary>
    public Dictionary<string, int> ErrorsByDevice { get; init; } = new();
}
