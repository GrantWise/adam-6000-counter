using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Industrial.Adam.Logger.Interfaces;
using Industrial.Adam.Logger.Logging;
using Industrial.Adam.Logger.Utilities;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Logger.ErrorHandling;

/// <summary>
/// Service for managing industrial error messages with structured logging integration
/// Provides centralized error handling and troubleshooting guidance
/// </summary>
public sealed class IndustrialErrorService : IIndustrialErrorService
{
    private readonly ILogger<IndustrialErrorService> _logger;
    private readonly ConcurrentDictionary<string, string> _errorMessageTemplates;
    private readonly ConcurrentDictionary<string, IReadOnlyList<string>> _troubleshootingSteps;

    /// <summary>
    /// Initialize the industrial error service
    /// </summary>
    /// <param name="logger">Logger instance</param>
    public IndustrialErrorService(ILogger<IndustrialErrorService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _errorMessageTemplates = new ConcurrentDictionary<string, string>();
        _troubleshootingSteps = new ConcurrentDictionary<string, IReadOnlyList<string>>();

        InitializeErrorTemplates();
    }

    /// <summary>
    /// Create and log an industrial error message from an exception
    /// </summary>
    /// <param name="exception">Original exception</param>
    /// <param name="errorCode">Industrial error code</param>
    /// <param name="summary">Brief error summary</param>
    /// <param name="context">Additional context information</param>
    /// <param name="memberName">Calling member name</param>
    /// <param name="sourceFilePath">Source file path</param>
    /// <param name="sourceLineNumber">Source line number</param>
    /// <returns>Industrial error message</returns>
    public IndustrialErrorMessage CreateAndLogError(
        Exception exception,
        string errorCode,
        string summary,
        Dictionary<string, object>? context = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        var errorMessage = IndustrialErrorMessage.FromException(
            exception, errorCode, summary, memberName, sourceFilePath, sourceLineNumber);

        // Add additional context if provided
        if (context != null)
        {
            var combinedContext = new Dictionary<string, object>(errorMessage.Context);
            foreach (var kvp in context)
            {
                combinedContext[kvp.Key] = kvp.Value;
            }

            // Since IndustrialErrorMessage is not a record, create a new instance with updated context
            errorMessage = new IndustrialErrorMessage
            {
                ErrorCode = errorMessage.ErrorCode,
                Summary = errorMessage.Summary,
                DetailedDescription = errorMessage.DetailedDescription,
                TroubleshootingSteps = errorMessage.TroubleshootingSteps,
                Context = combinedContext,
                EscalationProcedure = errorMessage.EscalationProcedure,
                Severity = errorMessage.Severity,
                Category = errorMessage.Category,
                DeviceId = errorMessage.DeviceId,
                Channel = errorMessage.Channel,
                OriginalException = errorMessage.OriginalException,
                TechnicalDetails = errorMessage.TechnicalDetails,
                Source = errorMessage.Source,
                Timestamp = errorMessage.Timestamp
            };
        }

        LogError(errorMessage);
        return errorMessage;
    }

    /// <summary>
    /// Create and log an industrial error message
    /// </summary>
    /// <param name="errorMessage">Industrial error message</param>
    /// <returns>The same error message for chaining</returns>
    public IndustrialErrorMessage CreateAndLogError(IndustrialErrorMessage errorMessage)
    {
        LogError(errorMessage);
        return errorMessage;
    }

    /// <summary>
    /// Create an OperationResult with industrial error message
    /// </summary>
    /// <param name="errorMessage">Industrial error message</param>
    /// <returns>Failed operation result with industrial error context</returns>
    public OperationResult CreateFailureResult(IndustrialErrorMessage errorMessage)
    {
        LogError(errorMessage);

        var context = new Dictionary<string, object>(errorMessage.Context)
        {
            ["ErrorCode"] = errorMessage.ErrorCode,
            ["ErrorSeverity"] = errorMessage.Severity.ToString(),
            ["ErrorCategory"] = errorMessage.Category.ToString(),
            ["TroubleshootingSteps"] = errorMessage.TroubleshootingSteps
        };

        return OperationResult.Failure(
            errorMessage.OriginalException ?? new InvalidOperationException(errorMessage.DetailedDescription),
            TimeSpan.Zero,
            context);
    }

    /// <summary>
    /// Create an OperationResult with industrial error message
    /// </summary>
    /// <typeparam name="T">Result type</typeparam>
    /// <param name="errorMessage">Industrial error message</param>
    /// <returns>Failed operation result with industrial error context</returns>
    public OperationResult<T> CreateFailureResult<T>(IndustrialErrorMessage errorMessage)
    {
        LogError(errorMessage);

        var context = new Dictionary<string, object>(errorMessage.Context)
        {
            ["ErrorCode"] = errorMessage.ErrorCode,
            ["ErrorSeverity"] = errorMessage.Severity.ToString(),
            ["ErrorCategory"] = errorMessage.Category.ToString(),
            ["TroubleshootingSteps"] = errorMessage.TroubleshootingSteps
        };

        return OperationResult<T>.Failure(
            errorMessage.OriginalException ?? new InvalidOperationException(errorMessage.DetailedDescription),
            TimeSpan.Zero,
            context);
    }

    /// <summary>
    /// Create an OperationResult from exception with industrial error message
    /// </summary>
    /// <param name="exception">Original exception</param>
    /// <param name="errorCode">Industrial error code</param>
    /// <param name="summary">Brief error summary</param>
    /// <param name="context">Additional context information</param>
    /// <param name="memberName">Calling member name</param>
    /// <param name="sourceFilePath">Source file path</param>
    /// <param name="sourceLineNumber">Source line number</param>
    /// <returns>Failed operation result with industrial error context</returns>
    public OperationResult CreateFailureResultFromException(
        Exception exception,
        string errorCode,
        string summary,
        Dictionary<string, object>? context = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        var errorMessage = CreateAndLogError(
            exception, errorCode, summary, context, memberName, sourceFilePath, sourceLineNumber);

        return CreateFailureResult(errorMessage);
    }

    /// <summary>
    /// Create an OperationResult from exception with industrial error message
    /// </summary>
    /// <typeparam name="T">Result type</typeparam>
    /// <param name="exception">Original exception</param>
    /// <param name="errorCode">Industrial error code</param>
    /// <param name="summary">Brief error summary</param>
    /// <param name="context">Additional context information</param>
    /// <param name="memberName">Calling member name</param>
    /// <param name="sourceFilePath">Source file path</param>
    /// <param name="sourceLineNumber">Source line number</param>
    /// <returns>Failed operation result with industrial error context</returns>
    public OperationResult<T> CreateFailureResultFromException<T>(
        Exception exception,
        string errorCode,
        string summary,
        Dictionary<string, object>? context = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        var errorMessage = CreateAndLogError(
            exception, errorCode, summary, context, memberName, sourceFilePath, sourceLineNumber);

        return CreateFailureResult<T>(errorMessage);
    }

    /// <summary>
    /// Get error message by error code
    /// </summary>
    /// <param name="errorCode">Error code to look up</param>
    /// <returns>Error message template if found</returns>
    public string? GetErrorMessageTemplate(string errorCode)
    {
        return _errorMessageTemplates.TryGetValue(errorCode, out var template) ? template : null;
    }

    /// <summary>
    /// Get troubleshooting steps for an error code
    /// </summary>
    /// <param name="errorCode">Error code to look up</param>
    /// <returns>Troubleshooting steps if found</returns>
    public IReadOnlyList<string>? GetTroubleshootingSteps(string errorCode)
    {
        return _troubleshootingSteps.TryGetValue(errorCode, out var steps) ? steps : null;
    }

    /// <summary>
    /// Log error message with structured context
    /// </summary>
    /// <param name="errorMessage">Industrial error message to log</param>
    public void LogError(IndustrialErrorMessage errorMessage)
    {
        using var contextScope = _logger.PushCorrelationContext(Guid.NewGuid().ToString());

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

        if (errorMessage.OriginalException != null)
        {
            _logger.LogStructuredError(
                errorMessage.OriginalException,
                "[{ErrorCode}] {Summary} - {DetailedDescription}",
                args: new object[] { errorMessage.ErrorCode, errorMessage.Summary, errorMessage.DetailedDescription });
        }
        else
        {
            _logger.Log(logLevel,
                "[{ErrorCode}] {Summary} - {DetailedDescription}",
                errorMessage.ErrorCode,
                errorMessage.Summary,
                errorMessage.DetailedDescription);
        }

        // Log troubleshooting steps at debug level
        if (errorMessage.TroubleshootingSteps.Count > 0)
        {
            _logger.LogDebug(
                "Troubleshooting steps for {ErrorCode}: {TroubleshootingSteps}",
                errorMessage.ErrorCode,
                string.Join("; ", errorMessage.TroubleshootingSteps));
        }

        // Log context information
        foreach (var kvp in structuredData)
        {
            _logger.LogDebug("Context {Key}: {Value}", kvp.Key, kvp.Value);
        }
    }

    /// <summary>
    /// Log error message with structured context and correlation ID
    /// </summary>
    /// <param name="errorMessage">Industrial error message to log</param>
    /// <param name="correlationId">Correlation ID for tracing</param>
    public void LogError(IndustrialErrorMessage errorMessage, string correlationId)
    {
        using var contextScope = _logger.PushCorrelationContext(correlationId);
        LogError(errorMessage);
    }

    /// <summary>
    /// Initialize error message templates and troubleshooting steps
    /// </summary>
    private void InitializeErrorTemplates()
    {
        // Connection error templates
        _errorMessageTemplates["CONN-001"] = "Failed to establish connection to device '{0}' at {1}:{2}";
        _errorMessageTemplates["CONN-002"] = "Connection timeout to device '{0}' after {1}ms";

        // Communication error templates
        _errorMessageTemplates["COMM-002"] = "Modbus {0} failed for device '{1}' (attempt {2}/{3})";

        // Data error templates
        _errorMessageTemplates["DATA-003"] = "Data validation failed for device '{0}' channel {1}";
        _errorMessageTemplates["DATA-004"] = "Counter overflow detected for device '{0}' channel {1}";

        // Configuration error templates
        _errorMessageTemplates["CONF-004"] = "Configuration validation failed in section '{0}'";

        // Performance error templates
        _errorMessageTemplates["PERF-005"] = "Performance degradation detected: {0}";

        // Initialize troubleshooting steps
        _troubleshootingSteps["CONN-001"] = new List<string>
        {
            "Verify network connectivity with ping",
            "Check firewall settings",
            "Verify device power and network cable",
            "Check device IP configuration"
        };

        _troubleshootingSteps["COMM-002"] = new List<string>
        {
            "Verify Modbus register addresses",
            "Check device unit ID configuration",
            "Ensure device is in normal operation mode",
            "Verify no other clients are accessing device"
        };

        _troubleshootingSteps["DATA-003"] = new List<string>
        {
            "Check sensor connectivity and calibration",
            "Review validation rules configuration",
            "Compare with historical data patterns",
            "Verify environmental conditions"
        };

        _troubleshootingSteps["DATA-004"] = new List<string>
        {
            "Monitor overflow frequency",
            "Verify overflow handling is working",
            "Consider larger counter size if needed",
            "Check data integrity"
        };

        _troubleshootingSteps["CONF-004"] = new List<string>
        {
            "Review configuration value and constraints",
            "Check for dependency conflicts",
            "Compare with last known good configuration",
            "Validate entire configuration file"
        };

        _troubleshootingSteps["PERF-005"] = new List<string>
        {
            "Monitor system resource usage",
            "Check for temporary spikes vs sustained issues",
            "Review device polling configuration",
            "Optimize system settings if needed"
        };
    }
}
