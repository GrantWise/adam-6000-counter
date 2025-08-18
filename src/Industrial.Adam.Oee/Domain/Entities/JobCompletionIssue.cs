using Industrial.Adam.Oee.Domain.Interfaces;

namespace Industrial.Adam.Oee.Domain.Entities;

/// <summary>
/// Job Completion Issue Aggregate Root
/// 
/// Tracks job completion problems requiring operator input and resolution.
/// Handles under-completion, overproduction, and quality issues using the
/// same two-level reason code system as stoppages.
/// </summary>
public sealed class JobCompletionIssue : Entity<int>, IAggregateRoot
{
    /// <summary>
    /// Associated work order identifier
    /// </summary>
    public string WorkOrderId { get; private set; }

    /// <summary>
    /// Type of completion issue
    /// </summary>
    public JobCompletionIssueType IssueType { get; private set; }

    /// <summary>
    /// Calculated completion percentage
    /// </summary>
    public decimal? CompletionPercentage { get; private set; }

    /// <summary>
    /// Target quantity from work order
    /// </summary>
    public decimal TargetQuantity { get; private set; }

    /// <summary>
    /// Actual quantity produced
    /// </summary>
    public decimal ActualQuantity { get; private set; }

    /// <summary>
    /// Level 1 reason code (category)
    /// </summary>
    public string? CategoryCode { get; private set; }

    /// <summary>
    /// Level 2 reason code (subcode within category)
    /// </summary>
    public string? Subcode { get; private set; }

    /// <summary>
    /// Additional comments from operator
    /// </summary>
    public string? OperatorComments { get; private set; }

    /// <summary>
    /// Who resolved this issue
    /// </summary>
    public string? ResolvedBy { get; private set; }

    /// <summary>
    /// When the issue was resolved
    /// </summary>
    public DateTime? ResolvedAt { get; private set; }

    /// <summary>
    /// When this issue was created
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Default constructor for serialization
    /// </summary>
    private JobCompletionIssue() : base()
    {
        WorkOrderId = string.Empty;
        IssueType = JobCompletionIssueType.UnderCompletion;
    }

    /// <summary>
    /// Creates a new job completion issue
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="issueType">Type of issue</param>
    /// <param name="targetQuantity">Target quantity</param>
    /// <param name="actualQuantity">Actual quantity</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public JobCompletionIssue(
        string workOrderId,
        JobCompletionIssueType issueType,
        decimal targetQuantity,
        decimal actualQuantity) : base()
    {
        ValidateConstructorParameters(workOrderId, targetQuantity, actualQuantity);

        WorkOrderId = workOrderId;
        IssueType = issueType;
        TargetQuantity = targetQuantity;
        ActualQuantity = actualQuantity;
        CompletionPercentage = CalculateCompletionPercentage(targetQuantity, actualQuantity);

        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Create issue with specific ID (for repository loading)
    /// </summary>
    /// <param name="id">Database identifier</param>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="issueType">Issue type</param>
    /// <param name="completionPercentage">Completion percentage</param>
    /// <param name="targetQuantity">Target quantity</param>
    /// <param name="actualQuantity">Actual quantity</param>
    /// <param name="categoryCode">Category code</param>
    /// <param name="subcode">Subcode</param>
    /// <param name="operatorComments">Operator comments</param>
    /// <param name="resolvedBy">Who resolved</param>
    /// <param name="resolvedAt">When resolved</param>
    /// <param name="createdAt">Creation timestamp</param>
    /// <param name="updatedAt">Last update timestamp</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public JobCompletionIssue(
        int id,
        string workOrderId,
        JobCompletionIssueType issueType,
        decimal? completionPercentage,
        decimal targetQuantity,
        decimal actualQuantity,
        string? categoryCode,
        string? subcode,
        string? operatorComments,
        string? resolvedBy,
        DateTime? resolvedAt,
        DateTime createdAt,
        DateTime updatedAt) : base(id)
    {
        ValidateConstructorParameters(workOrderId, targetQuantity, actualQuantity);

        WorkOrderId = workOrderId;
        IssueType = issueType;
        CompletionPercentage = completionPercentage;
        TargetQuantity = targetQuantity;
        ActualQuantity = actualQuantity;
        CategoryCode = categoryCode;
        Subcode = subcode;
        OperatorComments = operatorComments;
        ResolvedBy = resolvedBy;
        ResolvedAt = resolvedAt;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    /// <summary>
    /// Resolve the issue with reason codes
    /// </summary>
    /// <param name="categoryCode">Level 1 reason code</param>
    /// <param name="subcode">Level 2 reason code</param>
    /// <param name="resolvedBy">Who is resolving</param>
    /// <param name="operatorComments">Additional comments</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when issue is already resolved</exception>
    public void Resolve(
        string categoryCode,
        string subcode,
        string resolvedBy,
        string? operatorComments = null)
    {
        if (IsResolved)
            throw new InvalidOperationException("Issue has already been resolved");

        if (string.IsNullOrWhiteSpace(categoryCode))
            throw new ArgumentException("Category code is required", nameof(categoryCode));

        if (string.IsNullOrWhiteSpace(subcode))
            throw new ArgumentException("Subcode is required", nameof(subcode));

        if (string.IsNullOrWhiteSpace(resolvedBy))
            throw new ArgumentException("Resolved by is required", nameof(resolvedBy));

        CategoryCode = categoryCode.ToUpperInvariant();
        Subcode = subcode;
        ResolvedBy = resolvedBy;
        OperatorComments = operatorComments;
        ResolvedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update resolution with new reason codes
    /// </summary>
    /// <param name="categoryCode">New category code</param>
    /// <param name="subcode">New subcode</param>
    /// <param name="resolvedBy">Who is updating</param>
    /// <param name="operatorComments">Updated comments</param>
    /// <exception cref="InvalidOperationException">Thrown when issue is not resolved</exception>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public void UpdateResolution(
        string categoryCode,
        string subcode,
        string resolvedBy,
        string? operatorComments = null)
    {
        if (!IsResolved)
            throw new InvalidOperationException("Issue must be resolved before updating resolution");

        if (string.IsNullOrWhiteSpace(categoryCode))
            throw new ArgumentException("Category code is required", nameof(categoryCode));

        if (string.IsNullOrWhiteSpace(subcode))
            throw new ArgumentException("Subcode is required", nameof(subcode));

        if (string.IsNullOrWhiteSpace(resolvedBy))
            throw new ArgumentException("Resolved by is required", nameof(resolvedBy));

        CategoryCode = categoryCode.ToUpperInvariant();
        Subcode = subcode;
        ResolvedBy = resolvedBy;
        OperatorComments = operatorComments;
        ResolvedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Clear resolution (unresolve)
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when issue is not resolved</exception>
    public void ClearResolution()
    {
        if (!IsResolved)
            throw new InvalidOperationException("Issue is not resolved");

        CategoryCode = null;
        Subcode = null;
        ResolvedBy = null;
        OperatorComments = null;
        ResolvedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update operator comments
    /// </summary>
    /// <param name="operatorComments">New comments</param>
    public void UpdateComments(string? operatorComments)
    {
        OperatorComments = operatorComments;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update quantities and recalculate completion percentage
    /// </summary>
    /// <param name="targetQuantity">New target quantity</param>
    /// <param name="actualQuantity">New actual quantity</param>
    /// <exception cref="ArgumentException">Thrown when quantities are invalid</exception>
    public void UpdateQuantities(decimal targetQuantity, decimal actualQuantity)
    {
        if (targetQuantity < 0)
            throw new ArgumentException("Target quantity cannot be negative", nameof(targetQuantity));

        if (actualQuantity < 0)
            throw new ArgumentException("Actual quantity cannot be negative", nameof(actualQuantity));

        TargetQuantity = targetQuantity;
        ActualQuantity = actualQuantity;
        CompletionPercentage = CalculateCompletionPercentage(targetQuantity, actualQuantity);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Check if issue is resolved
    /// </summary>
    /// <returns>True if resolved, false otherwise</returns>
    public bool IsResolved => ResolvedAt.HasValue;

    /// <summary>
    /// Get variance between target and actual quantities
    /// </summary>
    /// <returns>Variance (positive for overproduction, negative for under-completion)</returns>
    public decimal GetQuantityVariance()
    {
        return ActualQuantity - TargetQuantity;
    }

    /// <summary>
    /// Get variance percentage
    /// </summary>
    /// <returns>Variance percentage</returns>
    public decimal GetVariancePercentage()
    {
        if (TargetQuantity == 0)
            return ActualQuantity > 0 ? 100m : 0m;

        return (GetQuantityVariance() / TargetQuantity) * 100m;
    }

    /// <summary>
    /// Get full reason code
    /// </summary>
    /// <returns>Full reason code or null if not resolved</returns>
    public string? GetFullReasonCode()
    {
        if (!IsResolved || CategoryCode == null || Subcode == null)
            return null;

        return $"{CategoryCode}-{Subcode}";
    }

    /// <summary>
    /// Get severity level based on variance percentage
    /// </summary>
    /// <returns>Severity level</returns>
    public JobCompletionIssueSeverity GetSeverity()
    {
        var variancePercentage = Math.Abs(GetVariancePercentage());

        return variancePercentage switch
        {
            <= 5m => JobCompletionIssueSeverity.Low,
            <= 15m => JobCompletionIssueSeverity.Medium,
            <= 30m => JobCompletionIssueSeverity.High,
            _ => JobCompletionIssueSeverity.Critical
        };
    }

    /// <summary>
    /// Check if issue requires immediate attention
    /// </summary>
    /// <returns>True if requires immediate attention</returns>
    public bool RequiresImmediateAttention()
    {
        return !IsResolved && GetSeverity() >= JobCompletionIssueSeverity.High;
    }

    /// <summary>
    /// Get issue summary for reporting
    /// </summary>
    /// <returns>Issue summary</returns>
    public JobCompletionIssueSummary ToSummary()
    {
        return new JobCompletionIssueSummary(
            Id,
            WorkOrderId,
            IssueType,
            CompletionPercentage,
            TargetQuantity,
            ActualQuantity,
            GetQuantityVariance(),
            GetVariancePercentage(),
            CategoryCode,
            Subcode,
            GetFullReasonCode(),
            OperatorComments,
            ResolvedBy,
            ResolvedAt,
            IsResolved,
            GetSeverity(),
            RequiresImmediateAttention(),
            CreatedAt,
            UpdatedAt
        );
    }

    /// <summary>
    /// Create under-completion issue
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="targetQuantity">Target quantity</param>
    /// <param name="actualQuantity">Actual quantity</param>
    /// <returns>New under-completion issue</returns>
    public static JobCompletionIssue CreateUnderCompletion(
        string workOrderId,
        decimal targetQuantity,
        decimal actualQuantity)
    {
        if (actualQuantity >= targetQuantity)
            throw new ArgumentException("Actual quantity must be less than target for under-completion issue");

        return new JobCompletionIssue(workOrderId, JobCompletionIssueType.UnderCompletion, targetQuantity, actualQuantity);
    }

    /// <summary>
    /// Create overproduction issue
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="targetQuantity">Target quantity</param>
    /// <param name="actualQuantity">Actual quantity</param>
    /// <returns>New overproduction issue</returns>
    public static JobCompletionIssue CreateOverproduction(
        string workOrderId,
        decimal targetQuantity,
        decimal actualQuantity)
    {
        if (actualQuantity <= targetQuantity)
            throw new ArgumentException("Actual quantity must be greater than target for overproduction issue");

        return new JobCompletionIssue(workOrderId, JobCompletionIssueType.Overproduction, targetQuantity, actualQuantity);
    }

    /// <summary>
    /// Create quality issue
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="targetQuantity">Target quantity</param>
    /// <param name="actualQuantity">Actual quantity</param>
    /// <returns>New quality issue</returns>
    public static JobCompletionIssue CreateQualityIssue(
        string workOrderId,
        decimal targetQuantity,
        decimal actualQuantity)
    {
        return new JobCompletionIssue(workOrderId, JobCompletionIssueType.QualityIssue, targetQuantity, actualQuantity);
    }

    /// <summary>
    /// Calculate completion percentage
    /// </summary>
    private static decimal? CalculateCompletionPercentage(decimal targetQuantity, decimal actualQuantity)
    {
        if (targetQuantity == 0)
            return null;

        return (actualQuantity / targetQuantity) * 100m;
    }

    /// <summary>
    /// Validate constructor parameters
    /// </summary>
    private static void ValidateConstructorParameters(
        string workOrderId,
        decimal targetQuantity,
        decimal actualQuantity)
    {
        if (string.IsNullOrWhiteSpace(workOrderId))
            throw new ArgumentException("Work order ID is required", nameof(workOrderId));

        if (targetQuantity < 0)
            throw new ArgumentException("Target quantity cannot be negative", nameof(targetQuantity));

        if (actualQuantity < 0)
            throw new ArgumentException("Actual quantity cannot be negative", nameof(actualQuantity));
    }

    /// <summary>
    /// String representation of the issue
    /// </summary>
    /// <returns>Formatted string representation</returns>
    public override string ToString()
    {
        var status = IsResolved ? "Resolved" : "Unresolved";
        var reason = IsResolved ? $" ({GetFullReasonCode()})" : "";
        return $"Job Issue {Id}: {IssueType} for {WorkOrderId} - {status}{reason}";
    }
}

/// <summary>
/// Job completion issue type enumeration
/// </summary>
public enum JobCompletionIssueType
{
    /// <summary>
    /// Job completed with less than expected quantity
    /// </summary>
    UnderCompletion,

    /// <summary>
    /// Job produced more than planned quantity
    /// </summary>
    Overproduction,

    /// <summary>
    /// Quality-related completion issue
    /// </summary>
    QualityIssue
}

/// <summary>
/// Job completion issue severity levels
/// </summary>
public enum JobCompletionIssueSeverity
{
    /// <summary>
    /// Low severity (≤5% variance)
    /// </summary>
    Low,

    /// <summary>
    /// Medium severity (6-15% variance)
    /// </summary>
    Medium,

    /// <summary>
    /// High severity (16-30% variance)
    /// </summary>
    High,

    /// <summary>
    /// Critical severity (>30% variance)
    /// </summary>
    Critical
}

/// <summary>
/// Job completion issue creation data
/// </summary>
/// <param name="WorkOrderId">Work order identifier</param>
/// <param name="IssueType">Issue type</param>
/// <param name="TargetQuantity">Target quantity</param>
/// <param name="ActualQuantity">Actual quantity</param>
public record JobCompletionIssueCreationData(
    string WorkOrderId,
    JobCompletionIssueType IssueType,
    decimal TargetQuantity,
    decimal ActualQuantity
);

/// <summary>
/// Job completion issue summary for reporting
/// </summary>
/// <param name="Id">Database identifier</param>
/// <param name="WorkOrderId">Work order identifier</param>
/// <param name="IssueType">Issue type</param>
/// <param name="CompletionPercentage">Completion percentage</param>
/// <param name="TargetQuantity">Target quantity</param>
/// <param name="ActualQuantity">Actual quantity</param>
/// <param name="QuantityVariance">Quantity variance</param>
/// <param name="VariancePercentage">Variance percentage</param>
/// <param name="CategoryCode">Category code</param>
/// <param name="Subcode">Subcode</param>
/// <param name="FullReasonCode">Full reason code</param>
/// <param name="OperatorComments">Operator comments</param>
/// <param name="ResolvedBy">Who resolved</param>
/// <param name="ResolvedAt">When resolved</param>
/// <param name="IsResolved">Whether resolved</param>
/// <param name="Severity">Severity level</param>
/// <param name="RequiresImmediateAttention">Whether requires immediate attention</param>
/// <param name="CreatedAt">Creation timestamp</param>
/// <param name="UpdatedAt">Last update timestamp</param>
public record JobCompletionIssueSummary(
    int Id,
    string WorkOrderId,
    JobCompletionIssueType IssueType,
    decimal? CompletionPercentage,
    decimal TargetQuantity,
    decimal ActualQuantity,
    decimal QuantityVariance,
    decimal VariancePercentage,
    string? CategoryCode,
    string? Subcode,
    string? FullReasonCode,
    string? OperatorComments,
    string? ResolvedBy,
    DateTime? ResolvedAt,
    bool IsResolved,
    JobCompletionIssueSeverity Severity,
    bool RequiresImmediateAttention,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>
/// Issue resolution data
/// </summary>
/// <param name="CategoryCode">Category code</param>
/// <param name="Subcode">Subcode</param>
/// <param name="ResolvedBy">Who resolved</param>
/// <param name="OperatorComments">Operator comments</param>
public record IssueResolutionData(
    string CategoryCode,
    string Subcode,
    string ResolvedBy,
    string? OperatorComments = null
);
