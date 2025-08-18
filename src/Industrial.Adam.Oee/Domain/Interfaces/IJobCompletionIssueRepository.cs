using Industrial.Adam.Oee.Domain.Entities;

namespace Industrial.Adam.Oee.Domain.Interfaces;

/// <summary>
/// Repository interface for job completion issue persistence
/// </summary>
public interface IJobCompletionIssueRepository
{
    /// <summary>
    /// Get a job completion issue by its identifier
    /// </summary>
    /// <param name="id">Issue identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Issue or null if not found</returns>
    public Task<JobCompletionIssue?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get issues for a specific work order
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of issues for the work order</returns>
    public Task<IEnumerable<JobCompletionIssue>> GetByWorkOrderAsync(string workOrderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get unresolved issues that require attention
    /// </summary>
    /// <param name="issueType">Optional issue type filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of unresolved issues</returns>
    public Task<IEnumerable<JobCompletionIssue>> GetUnresolvedIssuesAsync(JobCompletionIssueType? issueType = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get issues requiring immediate attention (high/critical severity)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of issues requiring immediate attention</returns>
    public Task<IEnumerable<JobCompletionIssue>> GetIssuesRequiringImmediateAttentionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get issues by type
    /// </summary>
    /// <param name="issueType">Issue type filter</param>
    /// <param name="startTime">Optional start time filter</param>
    /// <param name="endTime">Optional end time filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of issues of the specified type</returns>
    public Task<IEnumerable<JobCompletionIssue>> GetByTypeAsync(
        JobCompletionIssueType issueType,
        DateTime? startTime = null,
        DateTime? endTime = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get issues by severity level
    /// </summary>
    /// <param name="severity">Severity level filter</param>
    /// <param name="startTime">Optional start time filter</param>
    /// <param name="endTime">Optional end time filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of issues with the specified severity</returns>
    public Task<IEnumerable<JobCompletionIssue>> GetBySeverityAsync(
        JobCompletionIssueSeverity severity,
        DateTime? startTime = null,
        DateTime? endTime = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get issues by classification
    /// </summary>
    /// <param name="categoryCode">Category code filter</param>
    /// <param name="subcode">Optional subcode filter</param>
    /// <param name="startTime">Optional start time filter</param>
    /// <param name="endTime">Optional end time filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of issues with the specified classification</returns>
    public Task<IEnumerable<JobCompletionIssue>> GetByClassificationAsync(
        string categoryCode,
        string? subcode = null,
        DateTime? startTime = null,
        DateTime? endTime = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get issues within a time range
    /// </summary>
    /// <param name="startTime">Start of time range</param>
    /// <param name="endTime">End of time range</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of issues within the time range</returns>
    public Task<IEnumerable<JobCompletionIssue>> GetByTimeRangeAsync(
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new job completion issue
    /// </summary>
    /// <param name="issue">Issue to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created issue identifier</returns>
    public Task<int> CreateAsync(JobCompletionIssue issue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing job completion issue
    /// </summary>
    /// <param name="issue">Issue to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if updated successfully, false if not found</returns>
    public Task<bool> UpdateAsync(JobCompletionIssue issue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a job completion issue
    /// </summary>
    /// <param name="id">Issue identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully, false if not found</returns>
    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if an issue exists
    /// </summary>
    /// <param name="id">Issue identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if issue exists, false otherwise</returns>
    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get issue statistics for a time period
    /// </summary>
    /// <param name="startTime">Start of time period</param>
    /// <param name="endTime">End of time period</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Issue statistics</returns>
    public Task<JobCompletionIssueStatistics> GetStatisticsAsync(
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search issues by criteria
    /// </summary>
    /// <param name="criteria">Search criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of matching issues</returns>
    public Task<IEnumerable<JobCompletionIssue>> SearchAsync(JobCompletionIssueSearchCriteria criteria, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get top issue reasons for analysis
    /// </summary>
    /// <param name="startTime">Start time</param>
    /// <param name="endTime">End time</param>
    /// <param name="issueType">Optional issue type filter</param>
    /// <param name="topCount">Number of top reasons to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of top issue reasons</returns>
    public Task<IEnumerable<IssueReasonSummary>> GetTopIssueReasonsAsync(
        DateTime startTime,
        DateTime endTime,
        JobCompletionIssueType? issueType = null,
        int topCount = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get issue trends for analysis
    /// </summary>
    /// <param name="startTime">Start time</param>
    /// <param name="endTime">End time</param>
    /// <param name="groupBy">Grouping period (daily, weekly, monthly)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of issue trends</returns>
    public Task<IEnumerable<IssueTrend>> GetIssueTrendsAsync(
        DateTime startTime,
        DateTime endTime,
        IssueTrendGrouping groupBy = IssueTrendGrouping.Daily,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk resolve issues for work order completion
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="resolvedBy">Who resolved the issues</param>
    /// <param name="resolutionData">Resolution data for each issue</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of issues resolved</returns>
    public Task<int> BulkResolveAsync(
        string workOrderId,
        string resolvedBy,
        Dictionary<int, IssueResolutionData> resolutionData,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Job completion issue statistics for a time period
/// </summary>
/// <param name="TotalIssues">Total number of issues</param>
/// <param name="ResolvedIssues">Number of resolved issues</param>
/// <param name="UnresolvedIssues">Number of unresolved issues</param>
/// <param name="UnderCompletionIssues">Number of under-completion issues</param>
/// <param name="OverproductionIssues">Number of overproduction issues</param>
/// <param name="QualityIssues">Number of quality issues</param>
/// <param name="HighSeverityIssues">Number of high severity issues</param>
/// <param name="CriticalSeverityIssues">Number of critical severity issues</param>
/// <param name="AverageResolutionTimeHours">Average time to resolve issues in hours</param>
/// <param name="ResolutionRate">Percentage of issues resolved</param>
/// <param name="TotalQuantityVariance">Total quantity variance across all issues</param>
/// <param name="AverageVariancePercentage">Average variance percentage</param>
public record JobCompletionIssueStatistics(
    int TotalIssues,
    int ResolvedIssues,
    int UnresolvedIssues,
    int UnderCompletionIssues,
    int OverproductionIssues,
    int QualityIssues,
    int HighSeverityIssues,
    int CriticalSeverityIssues,
    decimal AverageResolutionTimeHours,
    decimal ResolutionRate,
    decimal TotalQuantityVariance,
    decimal AverageVariancePercentage
);

/// <summary>
/// Search criteria for job completion issues
/// </summary>
/// <param name="WorkOrderId">Optional work order ID filter</param>
/// <param name="IssueType">Optional issue type filter</param>
/// <param name="StartTime">Optional start time filter</param>
/// <param name="EndTime">Optional end time filter</param>
/// <param name="IsResolved">Optional resolution status filter</param>
/// <param name="CategoryCode">Optional category code filter</param>
/// <param name="Subcode">Optional subcode filter</param>
/// <param name="Severity">Optional severity filter</param>
/// <param name="MinVariancePercentage">Optional minimum variance percentage filter</param>
/// <param name="MaxVariancePercentage">Optional maximum variance percentage filter</param>
/// <param name="RequiresImmediateAttention">Optional requires immediate attention filter</param>
public record JobCompletionIssueSearchCriteria(
    string? WorkOrderId = null,
    JobCompletionIssueType? IssueType = null,
    DateTime? StartTime = null,
    DateTime? EndTime = null,
    bool? IsResolved = null,
    string? CategoryCode = null,
    string? Subcode = null,
    JobCompletionIssueSeverity? Severity = null,
    decimal? MinVariancePercentage = null,
    decimal? MaxVariancePercentage = null,
    bool? RequiresImmediateAttention = null
);

/// <summary>
/// Issue reason summary for top reasons analysis
/// </summary>
/// <param name="CategoryCode">Category code</param>
/// <param name="CategoryName">Category name</param>
/// <param name="Subcode">Subcode</param>
/// <param name="SubcodeName">Subcode name</param>
/// <param name="FullReasonCode">Full reason code</param>
/// <param name="OccurrenceCount">Number of occurrences</param>
/// <param name="TotalQuantityVariance">Total quantity variance</param>
/// <param name="AverageVariancePercentage">Average variance percentage</param>
/// <param name="IssueTypes">Associated issue types</param>
public record IssueReasonSummary(
    string CategoryCode,
    string CategoryName,
    string Subcode,
    string SubcodeName,
    string FullReasonCode,
    int OccurrenceCount,
    decimal TotalQuantityVariance,
    decimal AverageVariancePercentage,
    IEnumerable<JobCompletionIssueType> IssueTypes
);

/// <summary>
/// Issue trend for analysis
/// </summary>
/// <param name="Period">Time period</param>
/// <param name="IssueType">Issue type</param>
/// <param name="IssueCount">Number of issues</param>
/// <param name="ResolvedCount">Number resolved</param>
/// <param name="UnresolvedCount">Number unresolved</param>
/// <param name="AverageVariancePercentage">Average variance percentage</param>
/// <param name="TotalQuantityVariance">Total quantity variance</param>
public record IssueTrend(
    DateTime Period,
    JobCompletionIssueType IssueType,
    int IssueCount,
    int ResolvedCount,
    int UnresolvedCount,
    decimal AverageVariancePercentage,
    decimal TotalQuantityVariance
);

/// <summary>
/// Issue trend grouping options
/// </summary>
public enum IssueTrendGrouping
{
    /// <summary>
    /// Group by day
    /// </summary>
    Daily,

    /// <summary>
    /// Group by week
    /// </summary>
    Weekly,

    /// <summary>
    /// Group by month
    /// </summary>
    Monthly
}
