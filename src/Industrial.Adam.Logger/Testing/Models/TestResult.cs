using System.ComponentModel.DataAnnotations;

namespace Industrial.Adam.Logger.Testing.Models;

/// <summary>
/// Represents the result of a production test
/// </summary>
public sealed class TestResult
{
    /// <summary>
    /// Unique identifier for the test
    /// </summary>
    [Required]
    public required string TestId { get; init; }

    /// <summary>
    /// Name of the test
    /// </summary>
    [Required]
    public required string TestName { get; init; }

    /// <summary>
    /// Category of the test
    /// </summary>
    [Required]
    public required TestCategory Category { get; init; }

    /// <summary>
    /// Overall test result
    /// </summary>
    [Required]
    public required TestStatus Status { get; init; }

    /// <summary>
    /// Duration of the test execution
    /// </summary>
    [Required]
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// Timestamp when the test was executed
    /// </summary>
    [Required]
    public required DateTime ExecutedAt { get; init; }

    /// <summary>
    /// Success message if test passed
    /// </summary>
    public string? SuccessMessage { get; init; }

    /// <summary>
    /// Error message if test failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Detailed error information if test failed
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Additional test metrics and measurements
    /// </summary>
    [Required]
    public required Dictionary<string, object> Metrics { get; init; }

    /// <summary>
    /// Recommendations for improving test results
    /// </summary>
    [Required]
    public required IReadOnlyList<string> Recommendations { get; init; }

    /// <summary>
    /// Test severity level
    /// </summary>
    [Required]
    public required TestSeverity Severity { get; init; }

    /// <summary>
    /// Device or component tested
    /// </summary>
    public string? DeviceId { get; init; }

    /// <summary>
    /// Create a successful test result
    /// </summary>
    /// <param name="testId">Test identifier</param>
    /// <param name="testName">Test name</param>
    /// <param name="category">Test category</param>
    /// <param name="duration">Test duration</param>
    /// <param name="successMessage">Success message</param>
    /// <param name="metrics">Test metrics</param>
    /// <param name="recommendations">Recommendations</param>
    /// <param name="deviceId">Device ID if applicable</param>
    /// <returns>Success test result</returns>
    public static TestResult Success(
        string testId,
        string testName,
        TestCategory category,
        TimeSpan duration,
        string successMessage,
        Dictionary<string, object>? metrics = null,
        IReadOnlyList<string>? recommendations = null,
        string? deviceId = null)
    {
        return new TestResult
        {
            TestId = testId,
            TestName = testName,
            Category = category,
            Status = TestStatus.Passed,
            Duration = duration,
            ExecutedAt = DateTime.UtcNow,
            SuccessMessage = successMessage,
            Metrics = metrics ?? new Dictionary<string, object>(),
            Recommendations = recommendations ?? Array.Empty<string>(),
            Severity = TestSeverity.Info,
            DeviceId = deviceId
        };
    }

    /// <summary>
    /// Create a failed test result
    /// </summary>
    /// <param name="testId">Test identifier</param>
    /// <param name="testName">Test name</param>
    /// <param name="category">Test category</param>
    /// <param name="duration">Test duration</param>
    /// <param name="errorMessage">Error message</param>
    /// <param name="exception">Exception if available</param>
    /// <param name="metrics">Test metrics</param>
    /// <param name="recommendations">Recommendations</param>
    /// <param name="severity">Test severity</param>
    /// <param name="deviceId">Device ID if applicable</param>
    /// <returns>Failed test result</returns>
    public static TestResult Failure(
        string testId,
        string testName,
        TestCategory category,
        TimeSpan duration,
        string errorMessage,
        Exception? exception = null,
        Dictionary<string, object>? metrics = null,
        IReadOnlyList<string>? recommendations = null,
        TestSeverity severity = TestSeverity.Error,
        string? deviceId = null)
    {
        return new TestResult
        {
            TestId = testId,
            TestName = testName,
            Category = category,
            Status = TestStatus.Failed,
            Duration = duration,
            ExecutedAt = DateTime.UtcNow,
            ErrorMessage = errorMessage,
            Exception = exception,
            Metrics = metrics ?? new Dictionary<string, object>(),
            Recommendations = recommendations ?? Array.Empty<string>(),
            Severity = severity,
            DeviceId = deviceId
        };
    }

    /// <summary>
    /// Create a warning test result
    /// </summary>
    /// <param name="testId">Test identifier</param>
    /// <param name="testName">Test name</param>
    /// <param name="category">Test category</param>
    /// <param name="duration">Test duration</param>
    /// <param name="warningMessage">Warning message</param>
    /// <param name="metrics">Test metrics</param>
    /// <param name="recommendations">Recommendations</param>
    /// <param name="deviceId">Device ID if applicable</param>
    /// <returns>Warning test result</returns>
    public static TestResult Warning(
        string testId,
        string testName,
        TestCategory category,
        TimeSpan duration,
        string warningMessage,
        Dictionary<string, object>? metrics = null,
        IReadOnlyList<string>? recommendations = null,
        string? deviceId = null)
    {
        return new TestResult
        {
            TestId = testId,
            TestName = testName,
            Category = category,
            Status = TestStatus.Warning,
            Duration = duration,
            ExecutedAt = DateTime.UtcNow,
            SuccessMessage = warningMessage,
            Metrics = metrics ?? new Dictionary<string, object>(),
            Recommendations = recommendations ?? Array.Empty<string>(),
            Severity = TestSeverity.Warning,
            DeviceId = deviceId
        };
    }
}

/// <summary>
/// Test execution status
/// </summary>
public enum TestStatus
{
    /// <summary>
    /// Test passed successfully
    /// </summary>
    Passed,

    /// <summary>
    /// Test failed
    /// </summary>
    Failed,

    /// <summary>
    /// Test passed with warnings
    /// </summary>
    Warning,

    /// <summary>
    /// Test was skipped
    /// </summary>
    Skipped
}

/// <summary>
/// Test categories for organization
/// </summary>
public enum TestCategory
{
    /// <summary>
    /// Connection and communication tests
    /// </summary>
    Connection,

    /// <summary>
    /// Device discovery and identification tests
    /// </summary>
    Discovery,

    /// <summary>
    /// Data quality and validation tests
    /// </summary>
    DataQuality,

    /// <summary>
    /// Configuration validation tests
    /// </summary>
    Configuration,

    /// <summary>
    /// Performance and benchmarking tests
    /// </summary>
    Performance,

    /// <summary>
    /// System health and monitoring tests
    /// </summary>
    Health,

    /// <summary>
    /// End-to-end integration tests
    /// </summary>
    Integration
}

/// <summary>
/// Test severity levels
/// </summary>
public enum TestSeverity
{
    /// <summary>
    /// Informational test result
    /// </summary>
    Info,

    /// <summary>
    /// Warning level test result
    /// </summary>
    Warning,

    /// <summary>
    /// Error level test result
    /// </summary>
    Error,

    /// <summary>
    /// Critical test result
    /// </summary>
    Critical
}
