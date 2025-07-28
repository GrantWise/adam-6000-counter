using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using MsLogger = Microsoft.Extensions.Logging.ILogger;

namespace Industrial.Adam.Logger.Logging;

/// <summary>
/// Extension methods for structured logging in industrial applications
/// Provides consistent logging patterns and context enrichment
/// </summary>
public static class StructuredLoggingExtensions
{
    /// <summary>
    /// Push correlation context for request tracing
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="correlationId">Correlation ID for tracing</param>
    /// <returns>Disposable context</returns>
    public static IDisposable PushCorrelationContext(this MsLogger logger, string correlationId)
    {
        return LogContext.PushProperty("CorrelationId", correlationId);
    }

    /// <summary>
    /// Push correlation context for request tracing
    /// </summary>
    /// <param name="logger">Serilog logger instance</param>
    /// <param name="correlationId">Correlation ID for tracing</param>
    /// <returns>Disposable context</returns>
    public static IDisposable PushCorrelationContext(this Serilog.ILogger logger, string correlationId)
    {
        return LogContext.PushProperty("CorrelationId", correlationId);
    }

    /// <summary>
    /// Log device operation with structured context
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="operation">Operation performed</param>
    /// <param name="duration">Operation duration</param>
    /// <param name="success">Operation success status</param>
    /// <param name="memberName">Calling member name</param>
    /// <param name="sourceFilePath">Source file path</param>
    /// <param name="sourceLineNumber">Source line number</param>
    public static void LogDeviceOperation(
        this MsLogger logger,
        string deviceId,
        string operation,
        TimeSpan duration,
        bool success,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        using (LogContext.PushProperty("FunctionName", memberName))
        using (LogContext.PushProperty("LineNumber", sourceLineNumber))
        using (LogContext.PushProperty("SourceFile", Path.GetFileName(sourceFilePath)))
        {
            logger.LogInformation(
                "Device operation: {DeviceId} {Operation} completed in {Duration}ms with {Result}",
                deviceId,
                operation,
                duration.TotalMilliseconds,
                success ? "SUCCESS" : "FAILURE");
        }
    }

    /// <summary>
    /// Log performance metric with structured context
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="metricName">Name of the metric</param>
    /// <param name="value">Metric value</param>
    /// <param name="unit">Unit of measurement</param>
    /// <param name="memberName">Calling member name</param>
    /// <param name="sourceFilePath">Source file path</param>
    /// <param name="sourceLineNumber">Source line number</param>
    public static void LogPerformanceMetric(
        this MsLogger logger,
        string metricName,
        double value,
        string unit = "",
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        using (LogContext.PushProperty("FunctionName", memberName))
        using (LogContext.PushProperty("LineNumber", sourceLineNumber))
        using (LogContext.PushProperty("SourceFile", Path.GetFileName(sourceFilePath)))
        {
            logger.LogInformation(
                "Performance metric: {MetricName} = {Value} {Unit}",
                metricName,
                value,
                unit);
        }
    }

    /// <summary>
    /// Log connection event with structured context
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="endpoint">Connection endpoint</param>
    /// <param name="success">Connection success status</param>
    /// <param name="duration">Connection duration</param>
    /// <param name="memberName">Calling member name</param>
    /// <param name="sourceFilePath">Source file path</param>
    /// <param name="sourceLineNumber">Source line number</param>
    public static void LogConnectionEvent(
        this MsLogger logger,
        string deviceId,
        string endpoint,
        bool success,
        TimeSpan duration,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        using (LogContext.PushProperty("FunctionName", memberName))
        using (LogContext.PushProperty("LineNumber", sourceLineNumber))
        using (LogContext.PushProperty("SourceFile", Path.GetFileName(sourceFilePath)))
        {
            var level = success ? LogLevel.Information : LogLevel.Warning;
            logger.Log(level,
                "Connection event: {DeviceId} at {Endpoint} {Result} in {Duration}ms",
                deviceId,
                endpoint,
                success ? "CONNECTED" : "FAILED",
                duration.TotalMilliseconds);
        }
    }

    /// <summary>
    /// Log data processing event with structured context
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="channel">Channel number</param>
    /// <param name="value">Processed value</param>
    /// <param name="quality">Data quality</param>
    /// <param name="memberName">Calling member name</param>
    /// <param name="sourceFilePath">Source file path</param>
    /// <param name="sourceLineNumber">Source line number</param>
    public static void LogDataProcessing(
        this MsLogger logger,
        string deviceId,
        int channel,
        object value,
        string quality,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        using (LogContext.PushProperty("FunctionName", memberName))
        using (LogContext.PushProperty("LineNumber", sourceLineNumber))
        using (LogContext.PushProperty("SourceFile", Path.GetFileName(sourceFilePath)))
        {
            logger.LogDebug(
                "Data processing: {DeviceId} Ch{Channel} = {Value} ({Quality})",
                deviceId,
                channel,
                value,
                quality);
        }
    }

    /// <summary>
    /// Log error with enhanced structured context
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="exception">Exception that occurred</param>
    /// <param name="message">Error message</param>
    /// <param name="memberName">Calling member name</param>
    /// <param name="sourceFilePath">Source file path</param>
    /// <param name="sourceLineNumber">Source line number</param>
    /// <param name="args">Message arguments</param>
    public static void LogStructuredError(
        this MsLogger logger,
        Exception exception,
        string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0,
        params object[] args)
    {
        using (LogContext.PushProperty("FunctionName", memberName))
        using (LogContext.PushProperty("LineNumber", sourceLineNumber))
        using (LogContext.PushProperty("SourceFile", Path.GetFileName(sourceFilePath)))
        using (LogContext.PushProperty("ExceptionType", exception.GetType().Name))
        using (LogContext.PushProperty("StackTrace", exception.StackTrace))
        {
            logger.LogError(exception, message, args);
        }
    }

    /// <summary>
    /// Create a timed operation logger
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="operationName">Name of the operation</param>
    /// <param name="memberName">Calling member name</param>
    /// <param name="sourceFilePath">Source file path</param>
    /// <param name="sourceLineNumber">Source line number</param>
    /// <returns>Disposable timed operation</returns>
    public static IDisposable BeginTimedOperation(
        this MsLogger logger,
        string operationName,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        return new TimedOperation(logger, operationName, memberName, sourceFilePath, sourceLineNumber);
    }

    /// <summary>
    /// Disposable timed operation for performance monitoring
    /// </summary>
    private sealed class TimedOperation : IDisposable
    {
        private readonly MsLogger _logger;
        private readonly string _operationName;
        private readonly string _memberName;
        private readonly string _sourceFilePath;
        private readonly int _sourceLineNumber;
        private readonly Stopwatch _stopwatch;

        public TimedOperation(
            MsLogger logger,
            string operationName,
            string memberName,
            string sourceFilePath,
            int sourceLineNumber)
        {
            _logger = logger;
            _operationName = operationName;
            _memberName = memberName;
            _sourceFilePath = sourceFilePath;
            _sourceLineNumber = sourceLineNumber;
            _stopwatch = Stopwatch.StartNew();

            using (LogContext.PushProperty("FunctionName", memberName))
            using (LogContext.PushProperty("LineNumber", sourceLineNumber))
            using (LogContext.PushProperty("SourceFile", Path.GetFileName(sourceFilePath)))
            {
                _logger.LogDebug("Started operation: {OperationName}", operationName);
            }
        }

        public void Dispose()
        {
            _stopwatch.Stop();

            using (LogContext.PushProperty("FunctionName", _memberName))
            using (LogContext.PushProperty("LineNumber", _sourceLineNumber))
            using (LogContext.PushProperty("SourceFile", Path.GetFileName(_sourceFilePath)))
            {
                _logger.LogInformation(
                    "Completed operation: {OperationName} in {Duration}ms",
                    _operationName,
                    _stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
