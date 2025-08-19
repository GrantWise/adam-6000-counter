using System.Diagnostics;
using System.Text.Json;
using Industrial.Adam.Oee.Domain.Enums;
using Industrial.Adam.Oee.Domain.Exceptions;
using Industrial.Adam.Oee.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Oee.Domain.Services;

/// <summary>
/// Implementation of industrial OEE error service
/// </summary>
public sealed class IndustrialOeeErrorService : IIndustrialOeeErrorService
{
    private readonly ILogger<IndustrialOeeErrorService> _logger;
    private readonly List<OeeError> _errors; // In-memory storage for simplicity

    /// <summary>
    /// Initialize industrial OEE error service
    /// </summary>
    /// <param name="logger">Logger instance</param>
    public IndustrialOeeErrorService(ILogger<IndustrialOeeErrorService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _errors = new List<OeeError>();
    }

    /// <inheritdoc />
    public async Task<string> LogErrorAsync(
        OeeErrorCode errorCode,
        string message,
        string? deviceId = null,
        string? workOrderId = null,
        Exception? exception = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Error message cannot be null or empty", nameof(message));

        await Task.CompletedTask; // Method is synchronous but interface is async

        var errorId = Guid.NewGuid().ToString();
        var severity = GetSeverityLevel(errorCode);
        var timestamp = DateTime.UtcNow;

        var error = new OeeError(
            errorId,
            errorCode,
            message,
            deviceId,
            workOrderId,
            timestamp,
            severity,
            false,
            null
        );

        _errors.Add(error);

        // Log to structured logging
        var logLevel = severity switch
        {
            "Critical" => LogLevel.Critical,
            "Error" => LogLevel.Error,
            "Warning" => LogLevel.Warning,
            _ => LogLevel.Information
        };

        _logger.Log(logLevel, exception,
            "OEE Error [{ErrorCode}] {Message} | Device: {DeviceId} | WorkOrder: {WorkOrderId} | ErrorId: {ErrorId}",
            errorCode, message, deviceId, workOrderId, errorId);

        return errorId;
    }

    /// <inheritdoc />
    public async Task<OeeErrorStatistics> GetErrorStatisticsAsync(
        string deviceId,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("Device ID cannot be null or empty", nameof(deviceId));

        if (endTime <= startTime)
            throw new ArgumentException("End time must be after start time", nameof(endTime));

        await Task.CompletedTask;

        var deviceErrors = _errors
            .Where(e => e.DeviceId == deviceId &&
                       e.Timestamp >= startTime &&
                       e.Timestamp <= endTime)
            .ToList();

        var totalErrors = deviceErrors.Count;
        var resolvedErrors = deviceErrors.Count(e => e.IsResolved);
        var activeErrors = totalErrors - resolvedErrors;

        var errorsByCode = deviceErrors
            .GroupBy(e => e.ErrorCode)
            .Select(g => new ErrorCodeCount(g.Key, g.Count()))
            .ToList();

        var mostFrequentError = errorsByCode
            .OrderByDescending(e => e.Count)
            .FirstOrDefault()?.ErrorCode;

        var averageResolutionTime = resolvedErrors > 0
            ? deviceErrors
                .Where(e => e.IsResolved && e.ResolvedAt.HasValue)
                .Average(e => (e.ResolvedAt!.Value - e.Timestamp).TotalMinutes)
            : 0;

        return new OeeErrorStatistics(
            deviceId,
            totalErrors,
            resolvedErrors,
            activeErrors,
            errorsByCode,
            mostFrequentError,
            (decimal)averageResolutionTime
        );
    }

    /// <inheritdoc />
    public async Task<IEnumerable<OeeError>> GetRecentErrorsAsync(
        string deviceId,
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("Device ID cannot be null or empty", nameof(deviceId));

        if (count <= 0)
            throw new ArgumentException("Count must be positive", nameof(count));

        await Task.CompletedTask;

        return _errors
            .Where(e => e.DeviceId == deviceId)
            .OrderByDescending(e => e.Timestamp)
            .Take(count)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<int> ClearResolvedErrorsAsync(
        string deviceId,
        IEnumerable<OeeErrorCode>? errorCodes = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("Device ID cannot be null or empty", nameof(deviceId));

        await Task.CompletedTask;

        var errorsToResolve = _errors
            .Where(e => e.DeviceId == deviceId && !e.IsResolved)
            .ToList();

        if (errorCodes?.Any() == true)
        {
            var codeSet = errorCodes.ToHashSet();
            errorsToResolve = errorsToResolve.Where(e => codeSet.Contains(e.ErrorCode)).ToList();
        }

        var resolvedCount = 0;
        var resolvedAt = DateTime.UtcNow;

        for (int i = 0; i < _errors.Count; i++)
        {
            if (errorsToResolve.Any(e => e.Id == _errors[i].Id))
            {
                // Create a new resolved error record
                _errors[i] = _errors[i] with
                {
                    IsResolved = true,
                    ResolvedAt = resolvedAt
                };
                resolvedCount++;
            }
        }

        _logger.LogInformation(
            "Resolved {ResolvedCount} errors for device {DeviceId}",
            resolvedCount, deviceId);

        return resolvedCount;
    }

    /// <inheritdoc />
    public async Task<OeeErrorReport> CreateErrorReportAsync(
        DateTime startTime,
        DateTime endTime,
        IEnumerable<string>? deviceIds = null,
        CancellationToken cancellationToken = default)
    {
        if (endTime <= startTime)
            throw new ArgumentException("End time must be after start time", nameof(endTime));

        await Task.CompletedTask;

        var reportErrors = _errors
            .Where(e => e.Timestamp >= startTime && e.Timestamp <= endTime)
            .ToList();

        // Filter by devices if specified
        if (deviceIds?.Any() == true)
        {
            var deviceSet = deviceIds.ToHashSet();
            reportErrors = reportErrors
                .Where(e => e.DeviceId != null && deviceSet.Contains(e.DeviceId))
                .ToList();
        }

        var totalErrors = reportErrors.Count;

        // Get statistics by device
        var deviceStatistics = new List<OeeErrorStatistics>();
        var devicesInReport = reportErrors
            .Where(e => e.DeviceId != null)
            .GroupBy(e => e.DeviceId!)
            .Select(g => g.Key);

        foreach (var deviceId in devicesInReport)
        {
            var stats = await GetErrorStatisticsAsync(deviceId, startTime, endTime, cancellationToken);
            deviceStatistics.Add(stats);
        }

        // Get top errors across all devices
        var topErrors = reportErrors
            .GroupBy(e => e.ErrorCode)
            .Select(g => new ErrorCodeCount(g.Key, g.Count()))
            .OrderByDescending(e => e.Count)
            .Take(10)
            .ToList();

        return new OeeErrorReport(
            startTime,
            endTime,
            totalErrors,
            deviceStatistics,
            topErrors,
            DateTime.UtcNow
        );
    }

    /// <summary>
    /// Get severity level for an error code
    /// </summary>
    /// <param name="errorCode">OEE error code</param>
    /// <returns>Severity level string</returns>
    private static string GetSeverityLevel(OeeErrorCode errorCode)
    {
        return errorCode switch
        {
            OeeErrorCode.DatabaseConnectionFailed => "Critical",
            OeeErrorCode.InvalidConfiguration => "Critical",
            OeeErrorCode.WorkOrderNotFound => "Error",
            OeeErrorCode.CalculationFailed => "Error",
            OeeErrorCode.AvailabilityCalculationFailed => "Error",
            OeeErrorCode.PerformanceCalculationFailed => "Error",
            OeeErrorCode.QualityCalculationFailed => "Error",
            OeeErrorCode.InsufficientData => "Warning",
            OeeErrorCode.DataNotAvailable => "Warning",
            OeeErrorCode.MissingConfiguration => "Warning",
            _ => "Information"
        };
    }
}
