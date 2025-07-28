using System.Runtime.CompilerServices;
using Industrial.Adam.Logger.Logging;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Logger.ErrorHandling;

/// <summary>
/// Extension methods for logging industrial error messages with structured context
/// Integrates with the existing structured logging framework
/// </summary>
public static class IndustrialErrorLoggingExtensions
{
    /// <summary>
    /// Log industrial error message with structured context
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="errorMessage">Industrial error message to log</param>
    /// <param name="memberName">Calling member name</param>
    /// <param name="sourceFilePath">Source file path</param>
    /// <param name="sourceLineNumber">Source line number</param>
    public static void LogIndustrialError(
        this ILogger logger,
        IndustrialErrorMessage errorMessage,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        var structuredData = errorMessage.ToStructuredData();

        // Log with appropriate level based on severity
        var logLevel = errorMessage.Severity switch
        {
            ErrorSeverity.Info => LogLevel.Information,
            ErrorSeverity.Low => LogLevel.Warning,
            ErrorSeverity.Medium => LogLevel.Error,
            ErrorSeverity.High => LogLevel.Error,
            ErrorSeverity.Critical => LogLevel.Critical,
            _ => LogLevel.Error
        };

        using (var contextScope = logger.PushCorrelationContext(Guid.NewGuid().ToString()))
        {
            if (errorMessage.OriginalException != null)
            {
                logger.LogStructuredError(
                    errorMessage.OriginalException,
                    "[{ErrorCode}] {Summary} - {DetailedDescription}",
                    memberName,
                    sourceFilePath,
                    sourceLineNumber,
                    errorMessage.ErrorCode,
                    errorMessage.Summary,
                    errorMessage.DetailedDescription);
            }
            else
            {
                logger.Log(logLevel,
                    "[{ErrorCode}] {Summary} - {DetailedDescription}",
                    errorMessage.ErrorCode,
                    errorMessage.Summary,
                    errorMessage.DetailedDescription);
            }

            // Log troubleshooting steps at debug level
            if (errorMessage.TroubleshootingSteps.Count > 0)
            {
                logger.LogDebug(
                    "Troubleshooting steps for {ErrorCode}: {TroubleshootingSteps}",
                    errorMessage.ErrorCode,
                    string.Join("; ", errorMessage.TroubleshootingSteps));
            }

            // Log context information
            foreach (var kvp in structuredData)
            {
                logger.LogDebug("Context {Key}: {Value}", kvp.Key, kvp.Value);
            }
        }
    }

    /// <summary>
    /// Log connection error with industrial error context
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="ipAddress">Device IP address</param>
    /// <param name="port">Device port</param>
    /// <param name="exception">Original exception</param>
    /// <param name="memberName">Calling member name</param>
    /// <param name="sourceFilePath">Source file path</param>
    /// <param name="sourceLineNumber">Source line number</param>
    public static void LogConnectionError(
        this ILogger logger,
        string deviceId,
        string ipAddress,
        int port,
        Exception exception,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        var errorMessage = IndustrialErrorFactory.CreateConnectionFailure(
            deviceId, ipAddress, port, exception, memberName, sourceFilePath, sourceLineNumber);

        logger.LogIndustrialError(errorMessage, memberName, sourceFilePath, sourceLineNumber);
    }

    /// <summary>
    /// Log Modbus communication error with industrial error context
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="operation">Modbus operation</param>
    /// <param name="startAddress">Starting register address</param>
    /// <param name="count">Number of registers</param>
    /// <param name="attempt">Current attempt number</param>
    /// <param name="maxAttempts">Maximum number of attempts</param>
    /// <param name="exception">Original exception</param>
    /// <param name="memberName">Calling member name</param>
    /// <param name="sourceFilePath">Source file path</param>
    /// <param name="sourceLineNumber">Source line number</param>
    public static void LogModbusError(
        this ILogger logger,
        string deviceId,
        string operation,
        ushort startAddress,
        ushort count,
        int attempt,
        int maxAttempts,
        Exception exception,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        var errorMessage = IndustrialErrorFactory.CreateModbusCommunicationFailure(
            deviceId, operation, startAddress, count, attempt, maxAttempts, exception,
            memberName, sourceFilePath, sourceLineNumber);

        logger.LogIndustrialError(errorMessage, memberName, sourceFilePath, sourceLineNumber);
    }

    /// <summary>
    /// Log data validation error with industrial error context
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="channel">Channel number</param>
    /// <param name="value">Invalid value</param>
    /// <param name="validationRule">Validation rule that failed</param>
    /// <param name="memberName">Calling member name</param>
    /// <param name="sourceFilePath">Source file path</param>
    /// <param name="sourceLineNumber">Source line number</param>
    public static void LogDataValidationError(
        this ILogger logger,
        string deviceId,
        int channel,
        object value,
        string validationRule,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        var errorMessage = IndustrialErrorFactory.CreateDataValidationFailure(
            deviceId, channel, value, validationRule, memberName, sourceFilePath, sourceLineNumber);

        logger.LogIndustrialError(errorMessage, memberName, sourceFilePath, sourceLineNumber);
    }

    /// <summary>
    /// Log configuration validation error with industrial error context
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="configSection">Configuration section name</param>
    /// <param name="propertyName">Property name</param>
    /// <param name="currentValue">Current property value</param>
    /// <param name="constraint">Validation constraint</param>
    /// <param name="memberName">Calling member name</param>
    /// <param name="sourceFilePath">Source file path</param>
    /// <param name="sourceLineNumber">Source line number</param>
    public static void LogConfigurationError(
        this ILogger logger,
        string configSection,
        string propertyName,
        object currentValue,
        string constraint,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        var errorMessage = IndustrialErrorFactory.CreateConfigurationValidationFailure(
            configSection, propertyName, currentValue, constraint, memberName, sourceFilePath, sourceLineNumber);

        logger.LogIndustrialError(errorMessage, memberName, sourceFilePath, sourceLineNumber);
    }

    /// <summary>
    /// Log performance degradation with industrial error context
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="metricName">Performance metric name</param>
    /// <param name="currentValue">Current metric value</param>
    /// <param name="threshold">Performance threshold</param>
    /// <param name="recommendation">Performance recommendation</param>
    /// <param name="memberName">Calling member name</param>
    /// <param name="sourceFilePath">Source file path</param>
    /// <param name="sourceLineNumber">Source line number</param>
    public static void LogPerformanceDegradation(
        this ILogger logger,
        string metricName,
        double currentValue,
        double threshold,
        string recommendation,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        var errorMessage = IndustrialErrorFactory.CreatePerformanceDegradation(
            metricName, currentValue, threshold, recommendation, memberName, sourceFilePath, sourceLineNumber);

        logger.LogIndustrialError(errorMessage, memberName, sourceFilePath, sourceLineNumber);
    }

    /// <summary>
    /// Log counter overflow detection with industrial error context
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="channel">Channel number</param>
    /// <param name="currentValue">Current counter value</param>
    /// <param name="previousValue">Previous counter value</param>
    /// <param name="maxValue">Maximum counter value</param>
    /// <param name="memberName">Calling member name</param>
    /// <param name="sourceFilePath">Source file path</param>
    /// <param name="sourceLineNumber">Source line number</param>
    public static void LogCounterOverflow(
        this ILogger logger,
        string deviceId,
        int channel,
        long currentValue,
        long previousValue,
        long maxValue,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        var errorMessage = IndustrialErrorFactory.CreateCounterOverflowDetection(
            deviceId, channel, currentValue, previousValue, maxValue, memberName, sourceFilePath, sourceLineNumber);

        logger.LogIndustrialError(errorMessage, memberName, sourceFilePath, sourceLineNumber);
    }
}
