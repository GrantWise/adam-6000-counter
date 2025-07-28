using System.Runtime.CompilerServices;
using Industrial.Adam.Logger.ErrorHandling;
using Industrial.Adam.Logger.Utilities;

namespace Industrial.Adam.Logger.Interfaces;

/// <summary>
/// Service for managing industrial error messages with structured logging integration
/// Provides centralized error handling and troubleshooting guidance
/// </summary>
public interface IIndustrialErrorService
{
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
    IndustrialErrorMessage CreateAndLogError(
        Exception exception,
        string errorCode,
        string summary,
        Dictionary<string, object>? context = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0);

    /// <summary>
    /// Create and log an industrial error message
    /// </summary>
    /// <param name="errorMessage">Industrial error message</param>
    /// <returns>The same error message for chaining</returns>
    IndustrialErrorMessage CreateAndLogError(IndustrialErrorMessage errorMessage);

    /// <summary>
    /// Create an OperationResult with industrial error message
    /// </summary>
    /// <param name="errorMessage">Industrial error message</param>
    /// <returns>Failed operation result with industrial error context</returns>
    OperationResult CreateFailureResult(IndustrialErrorMessage errorMessage);

    /// <summary>
    /// Create an OperationResult with industrial error message
    /// </summary>
    /// <typeparam name="T">Result type</typeparam>
    /// <param name="errorMessage">Industrial error message</param>
    /// <returns>Failed operation result with industrial error context</returns>
    OperationResult<T> CreateFailureResult<T>(IndustrialErrorMessage errorMessage);

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
    OperationResult CreateFailureResultFromException(
        Exception exception,
        string errorCode,
        string summary,
        Dictionary<string, object>? context = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0);

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
    OperationResult<T> CreateFailureResultFromException<T>(
        Exception exception,
        string errorCode,
        string summary,
        Dictionary<string, object>? context = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0);

    /// <summary>
    /// Get error message by error code
    /// </summary>
    /// <param name="errorCode">Error code to look up</param>
    /// <returns>Error message template if found</returns>
    string? GetErrorMessageTemplate(string errorCode);

    /// <summary>
    /// Get troubleshooting steps for an error code
    /// </summary>
    /// <param name="errorCode">Error code to look up</param>
    /// <returns>Troubleshooting steps if found</returns>
    IReadOnlyList<string>? GetTroubleshootingSteps(string errorCode);

    /// <summary>
    /// Log error message with structured context
    /// </summary>
    /// <param name="errorMessage">Industrial error message to log</param>
    void LogError(IndustrialErrorMessage errorMessage);

    /// <summary>
    /// Log error message with structured context and correlation ID
    /// </summary>
    /// <param name="errorMessage">Industrial error message to log</param>
    /// <param name="correlationId">Correlation ID for tracing</param>
    void LogError(IndustrialErrorMessage errorMessage, string correlationId);
}
