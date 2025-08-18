using Industrial.Adam.Oee.Domain.Interfaces;

namespace Industrial.Adam.Oee.Domain.Entities;

/// <summary>
/// Quality Gate Aggregate Root
/// 
/// Represents quality checkpoints in job workflows with automatic job hold
/// capabilities, quality data integration, and non-conformance tracking.
/// Provides foundation for quality management and compliance systems.
/// </summary>
public sealed class QualityGate : Entity<string>, IAggregateRoot
{
    /// <summary>
    /// Quality gate name/identifier
    /// </summary>
    public string GateName { get; private set; }

    /// <summary>
    /// Description of the quality gate
    /// </summary>
    public string Description { get; private set; }

    /// <summary>
    /// Type of quality gate
    /// </summary>
    public QualityGateType GateType { get; private set; }

    /// <summary>
    /// When in the process this gate is triggered
    /// </summary>
    public QualityGateTrigger Trigger { get; private set; }

    /// <summary>
    /// Whether this gate is mandatory or optional
    /// </summary>
    public bool IsMandatory { get; private set; }

    /// <summary>
    /// Whether gate is currently active
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Auto-hold jobs that fail this gate
    /// </summary>
    public bool AutoHoldOnFailure { get; private set; }

    /// <summary>
    /// Required approval level for gate overrides
    /// </summary>
    public QualityApprovalLevel RequiredApprovalLevel { get; private set; }

    /// <summary>
    /// Quality criteria that must be met
    /// </summary>
    private readonly List<QualityCriteria> _qualityCriteria = new();

    /// <summary>
    /// Quality tests/inspections for this gate
    /// </summary>
    private readonly List<QualityTest> _qualityTests = new();

    /// <summary>
    /// Non-conformance records
    /// </summary>
    private readonly List<NonConformance> _nonConformances = new();

    /// <summary>
    /// Gate execution history
    /// </summary>
    private readonly List<QualityGateExecution> _executionHistory = new();

    /// <summary>
    /// Read-only access to quality criteria
    /// </summary>
    public IReadOnlyList<QualityCriteria> QualityCriteria => _qualityCriteria.AsReadOnly();

    /// <summary>
    /// Read-only access to quality tests
    /// </summary>
    public IReadOnlyList<QualityTest> QualityTests => _qualityTests.AsReadOnly();

    /// <summary>
    /// Read-only access to non-conformances
    /// </summary>
    public IReadOnlyList<NonConformance> NonConformances => _nonConformances.AsReadOnly();

    /// <summary>
    /// Read-only access to execution history
    /// </summary>
    public IReadOnlyList<QualityGateExecution> ExecutionHistory => _executionHistory.AsReadOnly();

    /// <summary>
    /// When this quality gate was created
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Default constructor for serialization
    /// </summary>
    private QualityGate() : base()
    {
        GateName = string.Empty;
        Description = string.Empty;
        GateType = QualityGateType.Manual;
        Trigger = QualityGateTrigger.Manual;
        RequiredApprovalLevel = QualityApprovalLevel.Operator;
        IsActive = true;
    }

    /// <summary>
    /// Creates a new quality gate
    /// </summary>
    /// <param name="gateId">Unique gate identifier</param>
    /// <param name="gateName">Gate name/identifier</param>
    /// <param name="description">Gate description</param>
    /// <param name="gateType">Type of quality gate</param>
    /// <param name="trigger">When gate is triggered</param>
    /// <param name="isMandatory">Whether gate is mandatory</param>
    /// <param name="autoHoldOnFailure">Auto-hold on failure</param>
    /// <param name="requiredApprovalLevel">Required approval level</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public QualityGate(
        string gateId,
        string gateName,
        string description,
        QualityGateType gateType,
        QualityGateTrigger trigger,
        bool isMandatory = true,
        bool autoHoldOnFailure = true,
        QualityApprovalLevel requiredApprovalLevel = QualityApprovalLevel.QualityEngineer) : base(gateId)
    {
        ValidateConstructorParameters(gateId, gateName, description);

        GateName = gateName;
        Description = description;
        GateType = gateType;
        Trigger = trigger;
        IsMandatory = isMandatory;
        AutoHoldOnFailure = autoHoldOnFailure;
        RequiredApprovalLevel = requiredApprovalLevel;
        IsActive = true;

        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Check if all criteria are met
    /// </summary>
    public bool AreAllCriteriaMet => _qualityCriteria.All(c => c.IsMet);

    /// <summary>
    /// Check if any mandatory criteria are failed
    /// </summary>
    public bool HasMandatoryCriteriaFailures => _qualityCriteria.Any(c => c.IsMandatory && !c.IsMet);

    /// <summary>
    /// Get pass rate percentage
    /// </summary>
    public decimal PassRatePercentage
    {
        get
        {
            if (!_executionHistory.Any())
                return 100m;
            var totalExecutions = _executionHistory.Count;
            var passedExecutions = _executionHistory.Count(e => e.Result == QualityGateResult.Passed);
            return (decimal)passedExecutions / totalExecutions * 100;
        }
    }

    /// <summary>
    /// Activate the quality gate
    /// </summary>
    public void Activate()
    {
        if (IsActive)
            return;

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivate the quality gate
    /// </summary>
    public void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Add quality criteria to gate
    /// </summary>
    /// <param name="criteriaName">Criteria name</param>
    /// <param name="description">Criteria description</param>
    /// <param name="measurementType">Type of measurement</param>
    /// <param name="targetValue">Target value</param>
    /// <param name="tolerance">Acceptable tolerance</param>
    /// <param name="isMandatory">Whether criteria is mandatory</param>
    /// <param name="unit">Unit of measurement</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public void AddQualityCriteria(
        string criteriaName,
        string description,
        QualityMeasurementType measurementType,
        decimal? targetValue = null,
        decimal? tolerance = null,
        bool isMandatory = true,
        string? unit = null)
    {
        if (string.IsNullOrWhiteSpace(criteriaName))
            throw new ArgumentException("Criteria name is required", nameof(criteriaName));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Criteria description is required", nameof(description));

        // Check if criteria already exists
        if (_qualityCriteria.Any(c => c.CriteriaName == criteriaName))
            throw new InvalidOperationException($"Quality criteria '{criteriaName}' already exists");

        var criteria = new QualityCriteria(
            criteriaName,
            description,
            measurementType,
            targetValue,
            tolerance,
            isMandatory,
            unit,
            false, // Not met initially
            null,  // No actual value initially
            DateTime.UtcNow
        );

        _qualityCriteria.Add(criteria);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update quality criteria measurement
    /// </summary>
    /// <param name="criteriaName">Criteria name</param>
    /// <param name="actualValue">Actual measured value</param>
    /// <param name="measuredBy">Who performed measurement</param>
    /// <exception cref="ArgumentException">Thrown when criteria not found</exception>
    public void UpdateCriteriaMeasurement(string criteriaName, decimal actualValue, string? measuredBy = null)
    {
        var criteria = _qualityCriteria.FirstOrDefault(c => c.CriteriaName == criteriaName);
        if (criteria == null)
            throw new ArgumentException($"Quality criteria '{criteriaName}' not found", nameof(criteriaName));

        // Evaluate if criteria is met
        bool isMet = EvaluateCriteria(criteria, actualValue);

        var index = _qualityCriteria.IndexOf(criteria);
        _qualityCriteria[index] = criteria with
        {
            ActualValue = actualValue,
            IsMet = isMet,
            MeasuredAt = DateTime.UtcNow
        };

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Add quality test to gate
    /// </summary>
    /// <param name="testName">Test name</param>
    /// <param name="testType">Type of test</param>
    /// <param name="description">Test description</param>
    /// <param name="isMandatory">Whether test is mandatory</param>
    /// <param name="sampleSize">Required sample size</param>
    /// <param name="acceptanceCriteria">Acceptance criteria</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public void AddQualityTest(
        string testName,
        QualityTestType testType,
        string description,
        bool isMandatory = true,
        int sampleSize = 1,
        string? acceptanceCriteria = null)
    {
        if (string.IsNullOrWhiteSpace(testName))
            throw new ArgumentException("Test name is required", nameof(testName));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Test description is required", nameof(description));

        if (sampleSize <= 0)
            throw new ArgumentException("Sample size must be positive", nameof(sampleSize));

        // Check if test already exists
        if (_qualityTests.Any(t => t.TestName == testName))
            throw new InvalidOperationException($"Quality test '{testName}' already exists");

        var test = new QualityTest(
            testName,
            testType,
            description,
            isMandatory,
            sampleSize,
            acceptanceCriteria,
            QualityTestResult.NotExecuted,
            null, // No operator initially
            null, // No execution time initially
            null  // No result notes initially
        );

        _qualityTests.Add(test);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Execute quality test
    /// </summary>
    /// <param name="testName">Test name</param>
    /// <param name="result">Test result</param>
    /// <param name="operatorId">Operator performing test</param>
    /// <param name="resultNotes">Result notes</param>
    /// <exception cref="ArgumentException">Thrown when test not found</exception>
    public void ExecuteQualityTest(string testName, QualityTestResult result, string operatorId, string? resultNotes = null)
    {
        var test = _qualityTests.FirstOrDefault(t => t.TestName == testName);
        if (test == null)
            throw new ArgumentException($"Quality test '{testName}' not found", nameof(testName));

        var index = _qualityTests.IndexOf(test);
        _qualityTests[index] = test with
        {
            Result = result,
            OperatorId = operatorId,
            ExecutedAt = DateTime.UtcNow,
            ResultNotes = resultNotes
        };

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Execute quality gate for a work order/batch
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="batchId">Batch identifier (optional)</param>
    /// <param name="executedBy">Who executed the gate</param>
    /// <param name="notes">Execution notes</param>
    /// <returns>Quality gate execution result</returns>
    /// <exception cref="InvalidOperationException">Thrown when gate cannot be executed</exception>
    public QualityGateExecutionResult ExecuteGate(string workOrderId, string? batchId = null, string? executedBy = null, string? notes = null)
    {
        if (!IsActive)
            throw new InvalidOperationException("Cannot execute inactive quality gate");

        // Determine overall result
        var result = DetermineGateResult();

        // Create execution record
        var execution = new QualityGateExecution(
            workOrderId,
            batchId,
            result,
            DateTime.UtcNow,
            executedBy,
            notes,
            _qualityCriteria.ToList(),
            _qualityTests.ToList()
        );

        _executionHistory.Add(execution);

        // Create non-conformance if gate failed
        if (result == QualityGateResult.Failed && IsMandatory)
        {
            CreateNonConformance(workOrderId, batchId, executedBy);
        }

        UpdatedAt = DateTime.UtcNow;

        return new QualityGateExecutionResult(
            Id,
            GateName,
            result,
            AutoHoldOnFailure && result == QualityGateResult.Failed,
            execution
        );
    }

    /// <summary>
    /// Create non-conformance record
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="batchId">Batch identifier</param>
    /// <param name="reportedBy">Who reported the non-conformance</param>
    /// <param name="description">Non-conformance description</param>
    /// <param name="severity">Severity level</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public void CreateNonConformance(
        string workOrderId,
        string? batchId = null,
        string? reportedBy = null,
        string? description = null,
        NonConformanceSeverity severity = NonConformanceSeverity.Medium)
    {
        if (string.IsNullOrWhiteSpace(workOrderId))
            throw new ArgumentException("Work order ID is required", nameof(workOrderId));

        var nonConformanceId = Guid.NewGuid().ToString();
        var failedCriteria = _qualityCriteria.Where(c => !c.IsMet).ToList();
        var failedTests = _qualityTests.Where(t => t.Result == QualityTestResult.Failed).ToList();

        var nonConformanceDescription = description ?? $"Quality gate '{GateName}' failed. " +
            $"Failed criteria: {string.Join(", ", failedCriteria.Select(c => c.CriteriaName))}. " +
            $"Failed tests: {string.Join(", ", failedTests.Select(t => t.TestName))}.";

        var nonConformance = new NonConformance(
            nonConformanceId,
            workOrderId,
            batchId,
            nonConformanceDescription,
            severity,
            NonConformanceStatus.Open,
            DateTime.UtcNow,
            reportedBy,
            null, // No assigned to initially
            null, // No resolution initially
            null, // No resolved at initially
            null  // No resolved by initially
        );

        _nonConformances.Add(nonConformance);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Resolve non-conformance
    /// </summary>
    /// <param name="nonConformanceId">Non-conformance identifier</param>
    /// <param name="resolution">Resolution description</param>
    /// <param name="resolvedBy">Who resolved it</param>
    /// <exception cref="ArgumentException">Thrown when non-conformance not found</exception>
    public void ResolveNonConformance(string nonConformanceId, string resolution, string resolvedBy)
    {
        var nonConformance = _nonConformances.FirstOrDefault(nc => nc.NonConformanceId == nonConformanceId);
        if (nonConformance == null)
            throw new ArgumentException($"Non-conformance '{nonConformanceId}' not found", nameof(nonConformanceId));

        var index = _nonConformances.IndexOf(nonConformance);
        _nonConformances[index] = nonConformance with
        {
            Status = NonConformanceStatus.Resolved,
            Resolution = resolution,
            ResolvedAt = DateTime.UtcNow,
            ResolvedBy = resolvedBy
        };

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Check if gate can be bypassed with approval
    /// </summary>
    /// <param name="approverLevel">Approver's authorization level</param>
    /// <returns>True if gate can be bypassed</returns>
    public bool CanBypass(QualityApprovalLevel approverLevel)
    {
        return approverLevel >= RequiredApprovalLevel;
    }

    /// <summary>
    /// Bypass quality gate with approval
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="approverLevel">Approver's authorization level</param>
    /// <param name="approverId">Who approved the bypass</param>
    /// <param name="reason">Bypass reason</param>
    /// <param name="batchId">Batch identifier (optional)</param>
    /// <returns>Quality gate execution result</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when approval level insufficient</exception>
    public QualityGateExecutionResult BypassGate(
        string workOrderId,
        QualityApprovalLevel approverLevel,
        string approverId,
        string reason,
        string? batchId = null)
    {
        if (!CanBypass(approverLevel))
            throw new UnauthorizedAccessException($"Approval level {approverLevel} insufficient to bypass gate requiring {RequiredApprovalLevel}");

        var execution = new QualityGateExecution(
            workOrderId,
            batchId,
            QualityGateResult.Bypassed,
            DateTime.UtcNow,
            approverId,
            $"Gate bypassed by {approverLevel}: {reason}",
            _qualityCriteria.ToList(),
            _qualityTests.ToList()
        );

        _executionHistory.Add(execution);
        UpdatedAt = DateTime.UtcNow;

        return new QualityGateExecutionResult(
            Id,
            GateName,
            QualityGateResult.Bypassed,
            false, // No hold on bypass
            execution
        );
    }

    /// <summary>
    /// Get quality gate summary
    /// </summary>
    /// <returns>Quality gate summary</returns>
    public QualityGateSummary ToSummary()
    {
        return new QualityGateSummary(
            Id,
            GateName,
            Description,
            GateType.ToString(),
            Trigger.ToString(),
            IsMandatory,
            IsActive,
            AutoHoldOnFailure,
            RequiredApprovalLevel.ToString(),
            _qualityCriteria.Count,
            _qualityCriteria.Count(c => c.IsMet),
            _qualityTests.Count,
            _qualityTests.Count(t => t.Result == QualityTestResult.Passed),
            _nonConformances.Count,
            _nonConformances.Count(nc => nc.Status == NonConformanceStatus.Open),
            _executionHistory.Count,
            PassRatePercentage
        );
    }

    /// <summary>
    /// Determine overall gate result based on criteria and tests
    /// </summary>
    /// <returns>Quality gate result</returns>
    private QualityGateResult DetermineGateResult()
    {
        // Check mandatory criteria
        if (HasMandatoryCriteriaFailures)
            return QualityGateResult.Failed;

        // Check mandatory tests
        var mandatoryTests = _qualityTests.Where(t => t.IsMandatory);
        if (mandatoryTests.Any(t => t.Result == QualityTestResult.Failed))
            return QualityGateResult.Failed;

        // Check if all mandatory tests are executed
        if (mandatoryTests.Any(t => t.Result == QualityTestResult.NotExecuted))
            return QualityGateResult.Pending;

        // Check if all criteria are met
        if (!AreAllCriteriaMet)
            return QualityGateResult.Failed;

        return QualityGateResult.Passed;
    }

    /// <summary>
    /// Evaluate if criteria is met based on measurement
    /// </summary>
    /// <param name="criteria">Quality criteria</param>
    /// <param name="actualValue">Actual measured value</param>
    /// <returns>True if criteria is met</returns>
    private bool EvaluateCriteria(QualityCriteria criteria, decimal actualValue)
    {
        if (criteria.TargetValue == null)
            return true; // No target value to compare against

        var target = criteria.TargetValue.Value;
        var tolerance = criteria.Tolerance ?? 0;

        return criteria.MeasurementType switch
        {
            QualityMeasurementType.Equals => Math.Abs(actualValue - target) <= tolerance,
            QualityMeasurementType.LessThan => actualValue < target + tolerance,
            QualityMeasurementType.LessThanOrEqual => actualValue <= target + tolerance,
            QualityMeasurementType.GreaterThan => actualValue > target - tolerance,
            QualityMeasurementType.GreaterThanOrEqual => actualValue >= target - tolerance,
            QualityMeasurementType.Between => actualValue >= target - tolerance && actualValue <= target + tolerance,
            QualityMeasurementType.PassFail => actualValue > 0, // Pass if value > 0
            _ => false
        };
    }

    /// <summary>
    /// Validate constructor parameters
    /// </summary>
    private static void ValidateConstructorParameters(string gateId, string gateName, string description)
    {
        if (string.IsNullOrWhiteSpace(gateId))
            throw new ArgumentException("Gate ID is required", nameof(gateId));

        if (string.IsNullOrWhiteSpace(gateName))
            throw new ArgumentException("Gate name is required", nameof(gateName));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required", nameof(description));
    }

    /// <summary>
    /// String representation of the quality gate
    /// </summary>
    /// <returns>Formatted string representation</returns>
    public override string ToString()
    {
        return $"Quality Gate {GateName}: {Description} ({GateType}, {PassRatePercentage:F1}% pass rate)";
    }
}

/// <summary>
/// Quality gate type enumeration
/// </summary>
public enum QualityGateType
{
    /// <summary>
    /// Manual inspection gate
    /// </summary>
    Manual,

    /// <summary>
    /// Automated measurement gate
    /// </summary>
    Automated,

    /// <summary>
    /// Statistical process control gate
    /// </summary>
    Statistical,

    /// <summary>
    /// First article inspection gate
    /// </summary>
    FirstArticle,

    /// <summary>
    /// Final inspection gate
    /// </summary>
    FinalInspection,

    /// <summary>
    /// In-process inspection gate
    /// </summary>
    InProcess,

    /// <summary>
    /// Receiving inspection gate
    /// </summary>
    Receiving
}

/// <summary>
/// Quality gate trigger enumeration
/// </summary>
public enum QualityGateTrigger
{
    /// <summary>
    /// Triggered manually by operator
    /// </summary>
    Manual,

    /// <summary>
    /// Triggered at job start
    /// </summary>
    JobStart,

    /// <summary>
    /// Triggered at job completion
    /// </summary>
    JobEnd,

    /// <summary>
    /// Triggered at specific quantity
    /// </summary>
    Quantity,

    /// <summary>
    /// Triggered at time intervals
    /// </summary>
    TimeInterval,

    /// <summary>
    /// Triggered by batch completion
    /// </summary>
    BatchComplete,

    /// <summary>
    /// Triggered by stoppage event
    /// </summary>
    Stoppage,

    /// <summary>
    /// Triggered by quality alert
    /// </summary>
    QualityAlert
}

/// <summary>
/// Quality approval level enumeration
/// </summary>
public enum QualityApprovalLevel
{
    /// <summary>
    /// Operator level approval
    /// </summary>
    Operator = 1,

    /// <summary>
    /// Lead operator approval
    /// </summary>
    Lead = 2,

    /// <summary>
    /// Supervisor approval
    /// </summary>
    Supervisor = 3,

    /// <summary>
    /// Quality engineer approval
    /// </summary>
    QualityEngineer = 4,

    /// <summary>
    /// Quality manager approval
    /// </summary>
    QualityManager = 5,

    /// <summary>
    /// Plant manager approval
    /// </summary>
    PlantManager = 6
}

/// <summary>
/// Quality measurement type enumeration
/// </summary>
public enum QualityMeasurementType
{
    /// <summary>
    /// Value must equal target
    /// </summary>
    Equals,

    /// <summary>
    /// Value must be less than target
    /// </summary>
    LessThan,

    /// <summary>
    /// Value must be less than or equal to target
    /// </summary>
    LessThanOrEqual,

    /// <summary>
    /// Value must be greater than target
    /// </summary>
    GreaterThan,

    /// <summary>
    /// Value must be greater than or equal to target
    /// </summary>
    GreaterThanOrEqual,

    /// <summary>
    /// Value must be within tolerance range
    /// </summary>
    Between,

    /// <summary>
    /// Simple pass/fail check
    /// </summary>
    PassFail
}

/// <summary>
/// Quality test type enumeration
/// </summary>
public enum QualityTestType
{
    /// <summary>
    /// Visual inspection
    /// </summary>
    Visual,

    /// <summary>
    /// Dimensional measurement
    /// </summary>
    Dimensional,

    /// <summary>
    /// Functional test
    /// </summary>
    Functional,

    /// <summary>
    /// Material test
    /// </summary>
    Material,

    /// <summary>
    /// Performance test
    /// </summary>
    Performance,

    /// <summary>
    /// Safety test
    /// </summary>
    Safety,

    /// <summary>
    /// Compliance test
    /// </summary>
    Compliance,

    /// <summary>
    /// Statistical sampling
    /// </summary>
    Statistical
}

/// <summary>
/// Quality test result enumeration
/// </summary>
public enum QualityTestResult
{
    /// <summary>
    /// Test not yet executed
    /// </summary>
    NotExecuted,

    /// <summary>
    /// Test passed
    /// </summary>
    Passed,

    /// <summary>
    /// Test failed
    /// </summary>
    Failed,

    /// <summary>
    /// Test skipped
    /// </summary>
    Skipped,

    /// <summary>
    /// Test in progress
    /// </summary>
    InProgress
}

/// <summary>
/// Quality gate result enumeration
/// </summary>
public enum QualityGateResult
{
    /// <summary>
    /// Gate passed all criteria
    /// </summary>
    Passed,

    /// <summary>
    /// Gate failed criteria
    /// </summary>
    Failed,

    /// <summary>
    /// Gate execution pending
    /// </summary>
    Pending,

    /// <summary>
    /// Gate bypassed with approval
    /// </summary>
    Bypassed
}

/// <summary>
/// Non-conformance severity enumeration
/// </summary>
public enum NonConformanceSeverity
{
    /// <summary>
    /// Low severity
    /// </summary>
    Low,

    /// <summary>
    /// Medium severity
    /// </summary>
    Medium,

    /// <summary>
    /// High severity
    /// </summary>
    High,

    /// <summary>
    /// Critical severity
    /// </summary>
    Critical
}

/// <summary>
/// Non-conformance status enumeration
/// </summary>
public enum NonConformanceStatus
{
    /// <summary>
    /// Non-conformance is open
    /// </summary>
    Open,

    /// <summary>
    /// Non-conformance is under investigation
    /// </summary>
    InvestigationInProgress,

    /// <summary>
    /// Non-conformance is resolved
    /// </summary>
    Resolved,

    /// <summary>
    /// Non-conformance is closed
    /// </summary>
    Closed
}

/// <summary>
/// Quality criteria record
/// </summary>
/// <param name="CriteriaName">Criteria name</param>
/// <param name="Description">Criteria description</param>
/// <param name="MeasurementType">Type of measurement</param>
/// <param name="TargetValue">Target value</param>
/// <param name="Tolerance">Acceptable tolerance</param>
/// <param name="IsMandatory">Whether criteria is mandatory</param>
/// <param name="Unit">Unit of measurement</param>
/// <param name="IsMet">Whether criteria is met</param>
/// <param name="ActualValue">Actual measured value</param>
/// <param name="MeasuredAt">When measurement was taken</param>
public record QualityCriteria(
    string CriteriaName,
    string Description,
    QualityMeasurementType MeasurementType,
    decimal? TargetValue,
    decimal? Tolerance,
    bool IsMandatory,
    string? Unit,
    bool IsMet,
    decimal? ActualValue,
    DateTime? MeasuredAt
);

/// <summary>
/// Quality test record
/// </summary>
/// <param name="TestName">Test name</param>
/// <param name="TestType">Type of test</param>
/// <param name="Description">Test description</param>
/// <param name="IsMandatory">Whether test is mandatory</param>
/// <param name="SampleSize">Required sample size</param>
/// <param name="AcceptanceCriteria">Acceptance criteria</param>
/// <param name="Result">Test result</param>
/// <param name="OperatorId">Operator performing test</param>
/// <param name="ExecutedAt">When test was executed</param>
/// <param name="ResultNotes">Result notes</param>
public record QualityTest(
    string TestName,
    QualityTestType TestType,
    string Description,
    bool IsMandatory,
    int SampleSize,
    string? AcceptanceCriteria,
    QualityTestResult Result,
    string? OperatorId,
    DateTime? ExecutedAt,
    string? ResultNotes
);

/// <summary>
/// Non-conformance record
/// </summary>
/// <param name="NonConformanceId">Non-conformance identifier</param>
/// <param name="WorkOrderId">Work order identifier</param>
/// <param name="BatchId">Batch identifier</param>
/// <param name="Description">Non-conformance description</param>
/// <param name="Severity">Severity level</param>
/// <param name="Status">Current status</param>
/// <param name="ReportedAt">When reported</param>
/// <param name="ReportedBy">Who reported</param>
/// <param name="AssignedTo">Who is assigned to resolve</param>
/// <param name="Resolution">Resolution description</param>
/// <param name="ResolvedAt">When resolved</param>
/// <param name="ResolvedBy">Who resolved</param>
public record NonConformance(
    string NonConformanceId,
    string WorkOrderId,
    string? BatchId,
    string Description,
    NonConformanceSeverity Severity,
    NonConformanceStatus Status,
    DateTime ReportedAt,
    string? ReportedBy,
    string? AssignedTo,
    string? Resolution,
    DateTime? ResolvedAt,
    string? ResolvedBy
);

/// <summary>
/// Quality gate execution record
/// </summary>
/// <param name="WorkOrderId">Work order identifier</param>
/// <param name="BatchId">Batch identifier</param>
/// <param name="Result">Execution result</param>
/// <param name="ExecutedAt">When executed</param>
/// <param name="ExecutedBy">Who executed</param>
/// <param name="Notes">Execution notes</param>
/// <param name="CriteriaSnapshot">Snapshot of criteria at execution</param>
/// <param name="TestsSnapshot">Snapshot of tests at execution</param>
public record QualityGateExecution(
    string WorkOrderId,
    string? BatchId,
    QualityGateResult Result,
    DateTime ExecutedAt,
    string? ExecutedBy,
    string? Notes,
    List<QualityCriteria> CriteriaSnapshot,
    List<QualityTest> TestsSnapshot
);

/// <summary>
/// Quality gate execution result
/// </summary>
/// <param name="GateId">Quality gate identifier</param>
/// <param name="GateName">Quality gate name</param>
/// <param name="Result">Execution result</param>
/// <param name="ShouldHoldJob">Whether job should be held</param>
/// <param name="Execution">Execution details</param>
public record QualityGateExecutionResult(
    string GateId,
    string GateName,
    QualityGateResult Result,
    bool ShouldHoldJob,
    QualityGateExecution Execution
);

/// <summary>
/// Quality gate summary for reporting
/// </summary>
/// <param name="GateId">Gate identifier</param>
/// <param name="GateName">Gate name</param>
/// <param name="Description">Gate description</param>
/// <param name="GateType">Gate type</param>
/// <param name="Trigger">Gate trigger</param>
/// <param name="IsMandatory">Whether gate is mandatory</param>
/// <param name="IsActive">Whether gate is active</param>
/// <param name="AutoHoldOnFailure">Auto-hold on failure</param>
/// <param name="RequiredApprovalLevel">Required approval level</param>
/// <param name="CriteriaCount">Number of criteria</param>
/// <param name="CriteriaMetCount">Number of met criteria</param>
/// <param name="TestCount">Number of tests</param>
/// <param name="TestPassedCount">Number of passed tests</param>
/// <param name="NonConformanceCount">Number of non-conformances</param>
/// <param name="OpenNonConformanceCount">Number of open non-conformances</param>
/// <param name="ExecutionCount">Number of executions</param>
/// <param name="PassRatePercentage">Pass rate percentage</param>
public record QualityGateSummary(
    string GateId,
    string GateName,
    string Description,
    string GateType,
    string Trigger,
    bool IsMandatory,
    bool IsActive,
    bool AutoHoldOnFailure,
    string RequiredApprovalLevel,
    int CriteriaCount,
    int CriteriaMetCount,
    int TestCount,
    int TestPassedCount,
    int NonConformanceCount,
    int OpenNonConformanceCount,
    int ExecutionCount,
    decimal PassRatePercentage
);

/// <summary>
/// Quality gate creation data
/// </summary>
/// <param name="GateId">Unique gate identifier</param>
/// <param name="GateName">Gate name/identifier</param>
/// <param name="Description">Gate description</param>
/// <param name="GateType">Type of quality gate</param>
/// <param name="Trigger">When gate is triggered</param>
/// <param name="IsMandatory">Whether gate is mandatory</param>
/// <param name="AutoHoldOnFailure">Auto-hold on failure</param>
/// <param name="RequiredApprovalLevel">Required approval level</param>
public record QualityGateCreationData(
    string GateId,
    string GateName,
    string Description,
    QualityGateType GateType,
    QualityGateTrigger Trigger,
    bool IsMandatory = true,
    bool AutoHoldOnFailure = true,
    QualityApprovalLevel RequiredApprovalLevel = QualityApprovalLevel.QualityEngineer
);
