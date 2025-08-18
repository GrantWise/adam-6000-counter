using Industrial.Adam.Oee.Domain.Entities;

namespace Industrial.Adam.Oee.Domain.Interfaces;

/// <summary>
/// Repository interface for QualityGate aggregate
/// </summary>
public interface IQualityGateRepository
{
    /// <summary>
    /// Get quality gate by identifier
    /// </summary>
    /// <param name="gateId">Gate identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Quality gate if found, null otherwise</returns>
    public Task<QualityGate?> GetByIdAsync(string gateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get quality gate by name
    /// </summary>
    /// <param name="gateName">Gate name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Quality gate if found, null otherwise</returns>
    public Task<QualityGate?> GetByNameAsync(string gateName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get quality gates by type
    /// </summary>
    /// <param name="gateType">Gate type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of quality gates</returns>
    public Task<IEnumerable<QualityGate>> GetByTypeAsync(QualityGateType gateType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get quality gates by trigger
    /// </summary>
    /// <param name="trigger">Gate trigger</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of quality gates</returns>
    public Task<IEnumerable<QualityGate>> GetByTriggerAsync(QualityGateTrigger trigger, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active quality gates
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of active quality gates</returns>
    public Task<IEnumerable<QualityGate>> GetActiveGatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get mandatory quality gates
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of mandatory quality gates</returns>
    public Task<IEnumerable<QualityGate>> GetMandatoryGatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get quality gates with auto-hold enabled
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of quality gates with auto-hold</returns>
    public Task<IEnumerable<QualityGate>> GetAutoHoldGatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get quality gates by approval level
    /// </summary>
    /// <param name="approvalLevel">Required approval level</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of quality gates</returns>
    public Task<IEnumerable<QualityGate>> GetByApprovalLevelAsync(QualityApprovalLevel approvalLevel, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get quality gates with low pass rates
    /// </summary>
    /// <param name="maxPassRate">Maximum pass rate threshold</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of quality gates with low pass rates</returns>
    public Task<IEnumerable<QualityGate>> GetGatesWithLowPassRatesAsync(decimal maxPassRate = 90m, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get quality gates with open non-conformances
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of quality gates with open non-conformances</returns>
    public Task<IEnumerable<QualityGate>> GetGatesWithOpenNonConformancesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get quality gate execution history
    /// </summary>
    /// <param name="gateId">Gate identifier</param>
    /// <param name="startDate">Start date filter</param>
    /// <param name="endDate">End date filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of quality gate executions</returns>
    public Task<IEnumerable<QualityGateExecution>> GetExecutionHistoryAsync(
        string gateId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get quality gate executions for work order
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of quality gate executions</returns>
    public Task<IEnumerable<QualityGateExecution>> GetExecutionsForWorkOrderAsync(string workOrderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get quality gate executions for batch
    /// </summary>
    /// <param name="batchId">Batch identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of quality gate executions</returns>
    public Task<IEnumerable<QualityGateExecution>> GetExecutionsForBatchAsync(string batchId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get non-conformances for date range
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="status">Status filter (optional)</param>
    /// <param name="severity">Severity filter (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of non-conformances</returns>
    public Task<IEnumerable<NonConformance>> GetNonConformancesAsync(
        DateTime startDate,
        DateTime endDate,
        NonConformanceStatus? status = null,
        NonConformanceSeverity? severity = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search quality gates with filters
    /// </summary>
    /// <param name="filter">Quality gate search filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of quality gates</returns>
    public Task<IEnumerable<QualityGate>> SearchAsync(QualityGateSearchFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add new quality gate
    /// </summary>
    /// <param name="qualityGate">Quality gate to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    public Task AddAsync(QualityGate qualityGate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update existing quality gate
    /// </summary>
    /// <param name="qualityGate">Quality gate to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    public Task UpdateAsync(QualityGate qualityGate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete quality gate
    /// </summary>
    /// <param name="gateId">Gate identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    public Task<bool> DeleteAsync(string gateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get quality gate performance statistics
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="gateId">Gate identifier filter (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Quality gate performance statistics</returns>
    public Task<QualityGatePerformanceStatistics> GetPerformanceStatisticsAsync(
        DateTime startDate,
        DateTime endDate,
        string? gateId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get quality trends data
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="gateId">Gate identifier filter (optional)</param>
    /// <param name="granularity">Data granularity (daily, weekly, monthly)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of quality trend data points</returns>
    public Task<IEnumerable<QualityTrendDataPoint>> GetQualityTrendsAsync(
        DateTime startDate,
        DateTime endDate,
        string? gateId = null,
        string granularity = "daily",
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Quality gate search filter
/// </summary>
/// <param name="GateName">Gate name filter</param>
/// <param name="GateType">Gate type filter</param>
/// <param name="Trigger">Trigger filter</param>
/// <param name="IsActive">Is active filter</param>
/// <param name="IsMandatory">Is mandatory filter</param>
/// <param name="AutoHoldOnFailure">Auto hold on failure filter</param>
/// <param name="RequiredApprovalLevel">Required approval level filter</param>
/// <param name="MinPassRate">Minimum pass rate filter</param>
/// <param name="MaxPassRate">Maximum pass rate filter</param>
/// <param name="HasOpenNonConformances">Has open non-conformances filter</param>
/// <param name="CreatedFrom">Created from filter</param>
/// <param name="CreatedTo">Created to filter</param>
public record QualityGateSearchFilter(
    string? GateName = null,
    QualityGateType? GateType = null,
    QualityGateTrigger? Trigger = null,
    bool? IsActive = null,
    bool? IsMandatory = null,
    bool? AutoHoldOnFailure = null,
    QualityApprovalLevel? RequiredApprovalLevel = null,
    decimal? MinPassRate = null,
    decimal? MaxPassRate = null,
    bool? HasOpenNonConformances = null,
    DateTime? CreatedFrom = null,
    DateTime? CreatedTo = null
);

/// <summary>
/// Quality gate performance statistics
/// </summary>
/// <param name="TotalGates">Total number of quality gates</param>
/// <param name="ActiveGates">Number of active gates</param>
/// <param name="MandatoryGates">Number of mandatory gates</param>
/// <param name="AutoHoldGates">Number of auto-hold gates</param>
/// <param name="TotalExecutions">Total number of executions</param>
/// <param name="PassedExecutions">Number of passed executions</param>
/// <param name="FailedExecutions">Number of failed executions</param>
/// <param name="BypassedExecutions">Number of bypassed executions</param>
/// <param name="PendingExecutions">Number of pending executions</param>
/// <param name="OverallPassRate">Overall pass rate percentage</param>
/// <param name="TotalNonConformances">Total number of non-conformances</param>
/// <param name="OpenNonConformances">Number of open non-conformances</param>
/// <param name="ResolvedNonConformances">Number of resolved non-conformances</param>
/// <param name="CriticalNonConformances">Number of critical non-conformances</param>
/// <param name="AverageResolutionTimeHours">Average resolution time in hours</param>
/// <param name="TopFailingGates">Top failing gates by failure count</param>
/// <param name="TopNonConformanceReasons">Top non-conformance reasons</param>
public record QualityGatePerformanceStatistics(
    int TotalGates,
    int ActiveGates,
    int MandatoryGates,
    int AutoHoldGates,
    int TotalExecutions,
    int PassedExecutions,
    int FailedExecutions,
    int BypassedExecutions,
    int PendingExecutions,
    decimal OverallPassRate,
    int TotalNonConformances,
    int OpenNonConformances,
    int ResolvedNonConformances,
    int CriticalNonConformances,
    decimal AverageResolutionTimeHours,
    List<string> TopFailingGates,
    List<string> TopNonConformanceReasons
);

/// <summary>
/// Quality trend data point
/// </summary>
/// <param name="Date">Date of data point</param>
/// <param name="GateId">Quality gate identifier</param>
/// <param name="GateName">Quality gate name</param>
/// <param name="TotalExecutions">Total executions for period</param>
/// <param name="PassedExecutions">Passed executions for period</param>
/// <param name="FailedExecutions">Failed executions for period</param>
/// <param name="PassRate">Pass rate percentage for period</param>
/// <param name="NonConformanceCount">Non-conformance count for period</param>
/// <param name="AverageQualityScore">Average quality score for period</param>
public record QualityTrendDataPoint(
    DateTime Date,
    string GateId,
    string GateName,
    int TotalExecutions,
    int PassedExecutions,
    int FailedExecutions,
    decimal PassRate,
    int NonConformanceCount,
    decimal AverageQualityScore
);
