using Industrial.Adam.Logger.Testing.Models;
using Industrial.Adam.Logger.Utilities;

namespace Industrial.Adam.Logger.Testing;

/// <summary>
/// Interface for running production tests and validation
/// </summary>
public interface ITestRunner
{
    /// <summary>
    /// Execute all available tests
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of test results</returns>
    Task<OperationResult<IReadOnlyList<TestResult>>> RunAllTestsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute tests by category
    /// </summary>
    /// <param name="category">Test category to run</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of test results</returns>
    Task<OperationResult<IReadOnlyList<TestResult>>> RunTestsAsync(TestCategory category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a specific test by ID
    /// </summary>
    /// <param name="testId">Test identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test result</returns>
    Task<OperationResult<TestResult>> RunTestAsync(string testId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of available tests
    /// </summary>
    /// <returns>Available test information</returns>
    OperationResult<IReadOnlyList<TestInfo>> GetAvailableTests();

    /// <summary>
    /// Validate production environment readiness
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Production readiness validation result</returns>
    Task<OperationResult<ProductionReadinessResult>> ValidateProductionReadinessAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate test report in specified format
    /// </summary>
    /// <param name="testResults">Test results to include in report</param>
    /// <param name="format">Report format</param>
    /// <returns>Generated report content</returns>
    Task<OperationResult<string>> GenerateTestReportAsync(IReadOnlyList<TestResult> testResults, ReportFormat format = ReportFormat.Console);
}

/// <summary>
/// Information about an available test
/// </summary>
public sealed class TestInfo
{
    /// <summary>
    /// Test identifier
    /// </summary>
    public required string TestId { get; init; }

    /// <summary>
    /// Test name
    /// </summary>
    public required string TestName { get; init; }

    /// <summary>
    /// Test description
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Test category
    /// </summary>
    public required TestCategory Category { get; init; }

    /// <summary>
    /// Estimated test duration
    /// </summary>
    public required TimeSpan EstimatedDuration { get; init; }

    /// <summary>
    /// Whether test requires device connection
    /// </summary>
    public required bool RequiresDevice { get; init; }

    /// <summary>
    /// Test dependencies
    /// </summary>
    public required IReadOnlyList<string> Dependencies { get; init; }
}

/// <summary>
/// Production readiness validation result
/// </summary>
public sealed class ProductionReadinessResult
{
    /// <summary>
    /// Overall readiness status
    /// </summary>
    public required bool IsReady { get; init; }

    /// <summary>
    /// Overall readiness score (0-100)
    /// </summary>
    public required int ReadinessScore { get; init; }

    /// <summary>
    /// Critical issues that prevent production deployment
    /// </summary>
    public required IReadOnlyList<string> CriticalIssues { get; init; }

    /// <summary>
    /// Warnings that should be addressed
    /// </summary>
    public required IReadOnlyList<string> Warnings { get; init; }

    /// <summary>
    /// Recommendations for improvement
    /// </summary>
    public required IReadOnlyList<string> Recommendations { get; init; }

    /// <summary>
    /// Test results that contributed to this assessment
    /// </summary>
    public required IReadOnlyList<TestResult> TestResults { get; init; }
}

/// <summary>
/// Test report formats
/// </summary>
public enum ReportFormat
{
    /// <summary>
    /// Console-friendly text format
    /// </summary>
    Console,

    /// <summary>
    /// JSON format for API consumption
    /// </summary>
    Json,

    /// <summary>
    /// Markdown format for documentation
    /// </summary>
    Markdown,

    /// <summary>
    /// HTML format for web display
    /// </summary>
    Html
}
