using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Industrial.Adam.Logger.ErrorHandling;

/// <summary>
/// Represents an industrial-grade error message with comprehensive troubleshooting guidance
/// Designed to provide actionable information for operators and maintenance personnel
/// </summary>
public sealed class IndustrialErrorMessage
{
    /// <summary>
    /// Standardized error code for categorization and lookup
    /// Format: {CATEGORY}-{NUMBER} (e.g., CONN-001, DATA-003)
    /// </summary>
    [Required]
    public required string ErrorCode { get; init; }

    /// <summary>
    /// Brief summary of the error condition
    /// </summary>
    [Required]
    public required string Summary { get; init; }

    /// <summary>
    /// Detailed description of what went wrong
    /// </summary>
    [Required]
    public required string DetailedDescription { get; init; }

    /// <summary>
    /// Step-by-step troubleshooting instructions
    /// Ordered from most likely to least likely solutions
    /// </summary>
    [Required]
    public required IReadOnlyList<string> TroubleshootingSteps { get; init; }

    /// <summary>
    /// Contextual information about the error
    /// Device IDs, configuration values, timestamps, etc.
    /// </summary>
    public IReadOnlyDictionary<string, object> Context { get; init; } = new Dictionary<string, object>();

    /// <summary>
    /// Instructions for escalating the issue if troubleshooting fails
    /// </summary>
    public string? EscalationProcedure { get; init; }

    /// <summary>
    /// Severity level of the error
    /// </summary>
    [Required]
    public required ErrorSeverity Severity { get; init; }

    /// <summary>
    /// Error category for classification
    /// </summary>
    [Required]
    public required ErrorCategory Category { get; init; }

    /// <summary>
    /// When the error occurred
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Source location where the error was created
    /// </summary>
    public ErrorSource Source { get; init; }

    /// <summary>
    /// Related device identifier if applicable
    /// </summary>
    public string? DeviceId { get; init; }

    /// <summary>
    /// Channel number if applicable
    /// </summary>
    public int? Channel { get; init; }

    /// <summary>
    /// Original exception that caused this error
    /// </summary>
    public Exception? OriginalException { get; init; }

    /// <summary>
    /// Additional technical details for debugging
    /// </summary>
    public string? TechnicalDetails { get; init; }

    /// <summary>
    /// Create an IndustrialErrorMessage from an existing exception
    /// </summary>
    /// <param name="exception">Original exception</param>
    /// <param name="errorCode">Industrial error code</param>
    /// <param name="summary">Brief error summary</param>
    /// <param name="memberName">Calling member name</param>
    /// <param name="sourceFilePath">Source file path</param>
    /// <param name="sourceLineNumber">Source line number</param>
    /// <returns>IndustrialErrorMessage instance</returns>
    public static IndustrialErrorMessage FromException(
        Exception exception,
        string errorCode,
        string summary,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        return new IndustrialErrorMessage
        {
            ErrorCode = errorCode,
            Summary = summary,
            DetailedDescription = exception.Message,
            TroubleshootingSteps = new List<string>
            {
                "Check the application logs for more detailed error information",
                "Verify system resources are available (CPU, memory, disk space)",
                "Check network connectivity if this is a communication error",
                "Restart the application if the error persists",
                "Contact technical support if the issue continues"
            },
            Context = new Dictionary<string, object>
            {
                ["ExceptionType"] = exception.GetType().Name,
                ["StackTrace"] = exception.StackTrace ?? string.Empty,
                ["InnerException"] = exception.InnerException?.Message ?? string.Empty
            },
            Severity = ErrorSeverity.High,
            Category = ErrorCategory.System,
            Source = new ErrorSource(memberName, sourceFilePath, sourceLineNumber),
            OriginalException = exception,
            TechnicalDetails = exception.ToString()
        };
    }

    /// <summary>
    /// Get formatted error message for display
    /// </summary>
    /// <param name="includeContext">Whether to include context information</param>
    /// <returns>Formatted error message</returns>
    public string FormatForDisplay(bool includeContext = true)
    {
        var message = new System.Text.StringBuilder();

        message.AppendLine($"[{ErrorCode}] {Summary}");
        message.AppendLine();
        message.AppendLine($"Description: {DetailedDescription}");
        message.AppendLine();

        if (TroubleshootingSteps.Count > 0)
        {
            message.AppendLine("Troubleshooting Steps:");
            for (int i = 0; i < TroubleshootingSteps.Count; i++)
            {
                message.AppendLine($"  {i + 1}. {TroubleshootingSteps[i]}");
            }
            message.AppendLine();
        }

        if (includeContext && Context.Count > 0)
        {
            message.AppendLine("Context:");
            foreach (var kvp in Context)
            {
                message.AppendLine($"  {kvp.Key}: {kvp.Value}");
            }
            message.AppendLine();
        }

        if (!string.IsNullOrEmpty(EscalationProcedure))
        {
            message.AppendLine($"Escalation: {EscalationProcedure}");
        }

        return message.ToString();
    }

    /// <summary>
    /// Get structured data for logging
    /// </summary>
    /// <returns>Dictionary of structured data</returns>
    public Dictionary<string, object> ToStructuredData()
    {
        var data = new Dictionary<string, object>
        {
            ["ErrorCode"] = ErrorCode,
            ["Summary"] = Summary,
            ["DetailedDescription"] = DetailedDescription,
            ["Severity"] = Severity.ToString(),
            ["Category"] = Category.ToString(),
            ["Timestamp"] = Timestamp,
            ["Source"] = Source.ToString()
        };

        if (!string.IsNullOrEmpty(DeviceId))
            data["DeviceId"] = DeviceId;

        if (Channel.HasValue)
            data["Channel"] = Channel.Value;

        if (OriginalException != null)
        {
            data["ExceptionType"] = OriginalException.GetType().Name;
            data["ExceptionMessage"] = OriginalException.Message;
        }

        // Add context data
        foreach (var kvp in Context)
        {
            data[$"Context_{kvp.Key}"] = kvp.Value;
        }

        return data;
    }
}

/// <summary>
/// Severity levels for industrial error messages
/// </summary>
public enum ErrorSeverity
{
    /// <summary>
    /// Informational message, no action required
    /// </summary>
    Info = 0,

    /// <summary>
    /// Warning condition, monitor but system continues
    /// </summary>
    Low = 1,

    /// <summary>
    /// Error condition requiring attention but system continues
    /// </summary>
    Medium = 2,

    /// <summary>
    /// Serious error requiring immediate attention
    /// </summary>
    High = 3,

    /// <summary>
    /// Critical error causing system failure
    /// </summary>
    Critical = 4
}

/// <summary>
/// Categories for industrial error messages
/// </summary>
public enum ErrorCategory
{
    /// <summary>
    /// Connection-related errors
    /// </summary>
    Connection,

    /// <summary>
    /// Communication protocol errors
    /// </summary>
    Communication,

    /// <summary>
    /// Data validation and processing errors
    /// </summary>
    Data,

    /// <summary>
    /// Configuration-related errors
    /// </summary>
    Configuration,

    /// <summary>
    /// Performance degradation warnings
    /// </summary>
    Performance,

    /// <summary>
    /// Hardware-related issues
    /// </summary>
    Hardware,

    /// <summary>
    /// System-level errors
    /// </summary>
    System,

    /// <summary>
    /// Security-related issues
    /// </summary>
    Security
}

/// <summary>
/// Source location information for error messages
/// </summary>
public readonly struct ErrorSource
{
    /// <summary>
    /// Method or member name where error originated
    /// </summary>
    public string MemberName { get; }

    /// <summary>
    /// Source file name
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// Line number in source file
    /// </summary>
    public int LineNumber { get; }

    /// <summary>
    /// Initialize error source information
    /// </summary>
    /// <param name="memberName">Member name</param>
    /// <param name="sourceFilePath">Source file path</param>
    /// <param name="lineNumber">Line number</param>
    public ErrorSource(string memberName, string sourceFilePath, int lineNumber)
    {
        MemberName = memberName;
        FileName = Path.GetFileName(sourceFilePath);
        LineNumber = lineNumber;
    }

    /// <summary>
    /// Get formatted source location
    /// </summary>
    /// <returns>Formatted source location</returns>
    public override string ToString()
    {
        return $"{FileName}:{MemberName}:{LineNumber}";
    }
}
