using Industrial.Adam.Oee.Domain.Interfaces;
using Industrial.Adam.Oee.Domain.ValueObjects;

namespace Industrial.Adam.Oee.Domain.Entities;

/// <summary>
/// Quality Inspection Aggregate Root following Section 15 of Canonical Manufacturing Model
/// 
/// Represents quality checks at any level (product/process/environmental) with proper
/// canonical patterns, specification references, and measurement recording capabilities.
/// Replaces the legacy QualityGate with industry-standard quality inspection pattern.
/// 
/// Three Inspection Levels:
/// - Product: Final/receiving/in-process product inspections
/// - Process: Process variable monitoring and control
/// - Environmental: Environmental condition monitoring
/// </summary>
public sealed class QualityInspection : Entity<string>, IAggregateRoot
{
    /// <summary>
    /// Type of quality inspection
    /// (immutable)
    /// </summary>
    public QualityInspectionType InspectionType { get; private set; }

    /// <summary>
    /// Level of inspection (product/process/environmental)
    /// (immutable)
    /// </summary>
    public QualityInspectionLevel InspectionLevel { get; private set; }

    /// <summary>
    /// Canonical reference to what is being inspected
    /// (immutable)
    /// </summary>
    public CanonicalReference ContextReference { get; private set; }

    /// <summary>
    /// List of applicable specification references
    /// (immutable collection)
    /// </summary>
    private readonly List<CanonicalReference> _specificationReferences = new();

    /// <summary>
    /// Measurements recorded during inspection
    /// </summary>
    private readonly List<QualityMeasurement> _measurements = new();

    /// <summary>
    /// Overall inspection result
    /// </summary>
    public QualityInspectionResult OverallResult { get; private set; }

    /// <summary>
    /// Disposition decision for the inspected item
    /// </summary>
    public QualityDisposition Disposition { get; private set; }

    /// <summary>
    /// Inspector who performed the inspection
    /// (immutable)
    /// </summary>
    public CanonicalReference InspectorReference { get; private set; }

    /// <summary>
    /// When the inspection was performed
    /// (immutable)
    /// </summary>
    public DateTime InspectionDate { get; private set; }

    /// <summary>
    /// Whether this inspection requires an alert/notification
    /// </summary>
    public bool RequiresAlert { get; private set; }

    /// <summary>
    /// Current inspection status
    /// </summary>
    public QualityInspectionStatus Status { get; private set; }

    /// <summary>
    /// Notes and comments about the inspection
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// When this inspection became effective
    /// (immutable)
    /// </summary>
    public DateTime EffectiveFromDate { get; private set; }

    /// <summary>
    /// When this inspection becomes ineffective (null = permanent)
    /// </summary>
    public DateTime? EffectiveToDate { get; private set; }

    /// <summary>
    /// Read-only access to specification references
    /// </summary>
    public IReadOnlyList<CanonicalReference> SpecificationReferences => _specificationReferences.AsReadOnly();

    /// <summary>
    /// Read-only access to measurements
    /// </summary>
    public IReadOnlyList<QualityMeasurement> Measurements => _measurements.AsReadOnly();

    /// <summary>
    /// When this inspection record was created
    /// (immutable)
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Default constructor for serialization
    /// </summary>
    private QualityInspection() : base()
    {
        ContextReference = new CanonicalReference("unknown", "unknown");
        InspectorReference = new CanonicalReference("person", "unknown");
        Status = QualityInspectionStatus.Planned;
        OverallResult = QualityInspectionResult.Pending;
        Disposition = QualityDisposition.Pending;
    }

    /// <summary>
    /// Creates a new quality inspection
    /// </summary>
    /// <param name="inspectionId">Unique inspection identifier (immutable)</param>
    /// <param name="inspectionType">Type of inspection (immutable)</param>
    /// <param name="inspectionLevel">Level of inspection (immutable)</param>
    /// <param name="contextReference">What is being inspected (immutable)</param>
    /// <param name="inspectorReference">Who is performing inspection (immutable)</param>
    /// <param name="specificationReferences">Applicable specifications (immutable)</param>
    /// <param name="effectiveFromDate">When inspection becomes effective (immutable)</param>
    /// <param name="effectiveToDate">When inspection becomes ineffective</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public QualityInspection(
        string inspectionId,
        QualityInspectionType inspectionType,
        QualityInspectionLevel inspectionLevel,
        CanonicalReference contextReference,
        CanonicalReference inspectorReference,
        IEnumerable<CanonicalReference>? specificationReferences = null,
        DateTime? effectiveFromDate = null,
        DateTime? effectiveToDate = null) : base(inspectionId)
    {
        ValidateConstructorParameters(inspectionId, contextReference, inspectorReference);

        InspectionType = inspectionType;
        InspectionLevel = inspectionLevel;
        ContextReference = contextReference;
        InspectorReference = inspectorReference;

        if (specificationReferences != null)
            _specificationReferences.AddRange(specificationReferences);

        EffectiveFromDate = effectiveFromDate ?? DateTime.UtcNow;
        EffectiveToDate = effectiveToDate;
        InspectionDate = DateTime.UtcNow;

        Status = QualityInspectionStatus.Planned;
        OverallResult = QualityInspectionResult.Pending;
        Disposition = QualityDisposition.Pending;

        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Check if inspection is currently effective
    /// </summary>
    public bool IsEffective
    {
        get
        {
            var now = DateTime.UtcNow;
            return now >= EffectiveFromDate && (EffectiveToDate == null || now <= EffectiveToDate);
        }
    }

    /// <summary>
    /// Check if inspection is completed
    /// </summary>
    public bool IsCompleted => Status == QualityInspectionStatus.Complete;

    /// <summary>
    /// Check if inspection passed
    /// </summary>
    public bool HasPassed => OverallResult == QualityInspectionResult.Passed;

    /// <summary>
    /// Check if inspection failed
    /// </summary>
    public bool HasFailed => OverallResult == QualityInspectionResult.Failed;

    /// <summary>
    /// Start the inspection
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when inspection cannot be started</exception>
    public void Start()
    {
        QualityInspectionStateTransitions.ValidateTransition(Status, QualityInspectionStatus.InProgress);

        if (!IsEffective)
            throw new InvalidOperationException("Cannot start inspection outside effective date range");

        Status = QualityInspectionStatus.InProgress;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Add measurement to inspection
    /// </summary>
    /// <param name="characteristic">Characteristic being measured</param>
    /// <param name="value">Measured value</param>
    /// <param name="units">Units of measurement</param>
    /// <param name="specification">Specification reference</param>
    /// <param name="result">Measurement result</param>
    /// <param name="notes">Measurement notes</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public void AddMeasurement(
        string characteristic,
        decimal value,
        string units,
        CanonicalReference? specification = null,
        QualityMeasurementResult result = QualityMeasurementResult.InSpec,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(characteristic))
            throw new ArgumentException("Characteristic is required", nameof(characteristic));

        if (string.IsNullOrWhiteSpace(units))
            throw new ArgumentException("Units are required", nameof(units));

        var measurement = new QualityMeasurement(
            characteristic,
            value,
            units,
            DateTime.UtcNow,
            specification,
            result,
            notes
        );

        _measurements.Add(measurement);
        UpdatedAt = DateTime.UtcNow;

        // Update overall result based on measurements
        UpdateOverallResult();
    }

    /// <summary>
    /// Add specification reference to inspection
    /// </summary>
    /// <param name="specificationReference">Specification reference to add</param>
    /// <exception cref="ArgumentException">Thrown when specification is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when specification already exists</exception>
    public void AddSpecificationReference(CanonicalReference specificationReference)
    {
        if (specificationReference == null)
            throw new ArgumentNullException(nameof(specificationReference));

        if (!specificationReference.IsType("specification"))
            throw new ArgumentException("Reference must be to a specification", nameof(specificationReference));

        if (_specificationReferences.Any(s => s.Id == specificationReference.Id))
            throw new InvalidOperationException($"Specification {specificationReference.Id} already referenced");

        _specificationReferences.Add(specificationReference);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Complete the inspection
    /// </summary>
    /// <param name="finalNotes">Final inspection notes</param>
    /// <exception cref="InvalidOperationException">Thrown when inspection cannot be completed</exception>
    public void Complete(string? finalNotes = null)
    {
        QualityInspectionStateTransitions.ValidateTransition(Status, QualityInspectionStatus.Complete);

        Status = QualityInspectionStatus.Complete;
        Notes = finalNotes;
        UpdatedAt = DateTime.UtcNow;

        // Final determination of overall result
        UpdateOverallResult();

        // Determine disposition if not already set
        if (Disposition == QualityDisposition.Pending)
        {
            Disposition = OverallResult switch
            {
                QualityInspectionResult.Passed => QualityDisposition.Accept,
                QualityInspectionResult.Failed => QualityDisposition.Reject,
                _ => QualityDisposition.Review
            };
        }

        // Set alert flag for failures
        if (OverallResult == QualityInspectionResult.Failed)
        {
            RequiresAlert = true;
        }
    }

    /// <summary>
    /// Approve the inspection results
    /// </summary>
    /// <param name="approverId">Who approved the inspection</param>
    /// <exception cref="InvalidOperationException">Thrown when inspection cannot be approved</exception>
    public void Approve(string approverId)
    {
        QualityInspectionStateTransitions.ValidateTransition(Status, QualityInspectionStatus.Approved);

        if (string.IsNullOrWhiteSpace(approverId))
            throw new ArgumentException("Approver ID is required", nameof(approverId));

        Status = QualityInspectionStatus.Approved;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Set disposition for inspected item
    /// </summary>
    /// <param name="disposition">Quality disposition</param>
    /// <param name="reason">Reason for disposition</param>
    /// <exception cref="InvalidOperationException">Thrown when disposition cannot be set</exception>
    public void SetDisposition(QualityDisposition disposition, string? reason = null)
    {
        if (Status == QualityInspectionStatus.Planned)
            throw new InvalidOperationException("Cannot set disposition on planned inspection");

        Disposition = disposition;

        if (!string.IsNullOrEmpty(reason))
        {
            Notes = string.IsNullOrEmpty(Notes) ? reason : $"{Notes}\nDisposition: {reason}";
        }

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Set effective end date (expire the inspection)
    /// </summary>
    /// <param name="effectiveToDate">When inspection becomes ineffective</param>
    /// <exception cref="ArgumentException">Thrown when date is invalid</exception>
    public void SetEffectiveToDate(DateTime effectiveToDate)
    {
        if (effectiveToDate <= EffectiveFromDate)
            throw new ArgumentException("Effective to date must be after effective from date", nameof(effectiveToDate));

        EffectiveToDate = effectiveToDate;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Get inspection summary
    /// </summary>
    /// <returns>Quality inspection summary</returns>
    public QualityInspectionSummary ToSummary()
    {
        return new QualityInspectionSummary(
            Id,
            InspectionType.ToString(),
            InspectionLevel.ToString(),
            ContextReference,
            OverallResult.ToString(),
            Disposition.ToString(),
            InspectorReference,
            InspectionDate,
            Status.ToString(),
            RequiresAlert,
            _specificationReferences.Count,
            _measurements.Count,
            _measurements.Count(m => m.Result == QualityMeasurementResult.InSpec),
            _measurements.Count(m => m.Result == QualityMeasurementResult.OutOfSpec),
            EffectiveFromDate,
            EffectiveToDate,
            Notes
        );
    }

    /// <summary>
    /// Update overall result based on measurements
    /// </summary>
    private void UpdateOverallResult()
    {
        if (!_measurements.Any())
        {
            OverallResult = QualityInspectionResult.Pending;
            return;
        }

        // If any measurement is out of spec, overall result is failed
        if (_measurements.Any(m => m.Result == QualityMeasurementResult.OutOfSpec))
        {
            OverallResult = QualityInspectionResult.Failed;
            return;
        }

        // If all measurements are in spec, overall result is passed
        if (_measurements.All(m => m.Result == QualityMeasurementResult.InSpec))
        {
            OverallResult = QualityInspectionResult.Passed;
            return;
        }

        // Mixed results or warnings - needs review
        OverallResult = QualityInspectionResult.Warning;
    }

    /// <summary>
    /// Validate constructor parameters
    /// </summary>
    private static void ValidateConstructorParameters(
        string inspectionId,
        CanonicalReference contextReference,
        CanonicalReference inspectorReference)
    {
        if (string.IsNullOrWhiteSpace(inspectionId))
            throw new ArgumentException("Inspection ID is required", nameof(inspectionId));

        if (contextReference == null)
            throw new ArgumentNullException(nameof(contextReference));

        if (inspectorReference == null)
            throw new ArgumentNullException(nameof(inspectorReference));

        if (!inspectorReference.IsType("person"))
            throw new ArgumentException("Inspector reference must be to a person", nameof(inspectorReference));
    }

    /// <summary>
    /// String representation of the quality inspection
    /// </summary>
    /// <returns>Formatted string representation</returns>
    public override string ToString()
    {
        return $"Quality Inspection {Id}: {InspectionType} {InspectionLevel} on {ContextReference} ({OverallResult})";
    }
}

/// <summary>
/// Quality inspection type enumeration following canonical patterns
/// </summary>
public enum QualityInspectionType
{
    /// <summary>
    /// Receiving inspection of incoming materials
    /// </summary>
    Receiving,

    /// <summary>
    /// In-process inspection during production
    /// </summary>
    InProcess,

    /// <summary>
    /// Final inspection of finished products
    /// </summary>
    Final,

    /// <summary>
    /// Equipment check and verification
    /// </summary>
    EquipmentCheck,

    /// <summary>
    /// Environmental condition inspection
    /// </summary>
    Environmental
}

/// <summary>
/// Quality inspection level enumeration (canonical three levels)
/// </summary>
public enum QualityInspectionLevel
{
    /// <summary>
    /// Product-level inspection (characteristics, dimensions, etc.)
    /// </summary>
    Product,

    /// <summary>
    /// Process-level inspection (process variables, parameters)
    /// </summary>
    Process,

    /// <summary>
    /// Environmental-level inspection (temperature, humidity, etc.)
    /// </summary>
    Environmental
}

/// <summary>
/// Quality inspection result enumeration
/// </summary>
public enum QualityInspectionResult
{
    /// <summary>
    /// Inspection pending
    /// </summary>
    Pending,

    /// <summary>
    /// Inspection passed all criteria
    /// </summary>
    Passed,

    /// <summary>
    /// Inspection failed criteria
    /// </summary>
    Failed,

    /// <summary>
    /// Inspection has warnings but is acceptable
    /// </summary>
    Warning
}

/// <summary>
/// Quality inspection status enumeration
/// </summary>
public enum QualityInspectionStatus
{
    /// <summary>
    /// Inspection is planned but not started
    /// </summary>
    Planned,

    /// <summary>
    /// Inspection is in progress
    /// </summary>
    InProgress,

    /// <summary>
    /// Inspection is complete
    /// </summary>
    Complete,

    /// <summary>
    /// Inspection is approved
    /// </summary>
    Approved
}

/// <summary>
/// Quality disposition enumeration for inspection results
/// </summary>
public enum QualityDisposition
{
    /// <summary>
    /// Disposition pending determination
    /// </summary>
    Pending,

    /// <summary>
    /// Accept the inspected item
    /// </summary>
    Accept,

    /// <summary>
    /// Reject the inspected item
    /// </summary>
    Reject,

    /// <summary>
    /// Rework required
    /// </summary>
    Rework,

    /// <summary>
    /// Conditional acceptance
    /// </summary>
    ConditionalAccept,

    /// <summary>
    /// Requires review by quality engineer
    /// </summary>
    Review
}

/// <summary>
/// Quality measurement result enumeration
/// </summary>
public enum QualityMeasurementResult
{
    /// <summary>
    /// Measurement is within specification
    /// </summary>
    InSpec,

    /// <summary>
    /// Measurement is out of specification
    /// </summary>
    OutOfSpec,

    /// <summary>
    /// Measurement has warning (close to limit)
    /// </summary>
    Warning
}

/// <summary>
/// Quality measurement record following canonical patterns
/// </summary>
/// <param name="Characteristic">Characteristic being measured (immutable)</param>
/// <param name="Value">Measured value (immutable)</param>
/// <param name="Units">Units of measurement (immutable)</param>
/// <param name="MeasuredAt">When measurement was taken (immutable)</param>
/// <param name="SpecificationReference">Reference to specification (immutable)</param>
/// <param name="Result">Measurement result (immutable)</param>
/// <param name="Notes">Measurement notes (immutable)</param>
public record QualityMeasurement(
    string Characteristic,
    decimal Value,
    string Units,
    DateTime MeasuredAt,
    CanonicalReference? SpecificationReference,
    QualityMeasurementResult Result,
    string? Notes
);

/// <summary>
/// Quality inspection summary for reporting
/// </summary>
/// <param name="InspectionId">Inspection identifier</param>
/// <param name="InspectionType">Type of inspection</param>
/// <param name="InspectionLevel">Level of inspection</param>
/// <param name="ContextReference">What was inspected</param>
/// <param name="OverallResult">Overall inspection result</param>
/// <param name="Disposition">Quality disposition</param>
/// <param name="InspectorReference">Who performed inspection</param>
/// <param name="InspectionDate">When inspection was performed</param>
/// <param name="Status">Current status</param>
/// <param name="RequiresAlert">Whether alert is required</param>
/// <param name="SpecificationCount">Number of specifications</param>
/// <param name="MeasurementCount">Number of measurements</param>
/// <param name="InSpecMeasurements">Number of in-spec measurements</param>
/// <param name="OutOfSpecMeasurements">Number of out-of-spec measurements</param>
/// <param name="EffectiveFromDate">When inspection becomes effective</param>
/// <param name="EffectiveToDate">When inspection becomes ineffective</param>
/// <param name="Notes">Inspection notes</param>
public record QualityInspectionSummary(
    string InspectionId,
    string InspectionType,
    string InspectionLevel,
    CanonicalReference ContextReference,
    string OverallResult,
    string Disposition,
    CanonicalReference InspectorReference,
    DateTime InspectionDate,
    string Status,
    bool RequiresAlert,
    int SpecificationCount,
    int MeasurementCount,
    int InSpecMeasurements,
    int OutOfSpecMeasurements,
    DateTime EffectiveFromDate,
    DateTime? EffectiveToDate,
    string? Notes
);

/// <summary>
/// Quality inspection creation data
/// </summary>
/// <param name="InspectionId">Unique inspection identifier</param>
/// <param name="InspectionType">Type of inspection</param>
/// <param name="InspectionLevel">Level of inspection</param>
/// <param name="ContextReference">What is being inspected</param>
/// <param name="InspectorReference">Who is performing inspection</param>
/// <param name="SpecificationReferences">Applicable specifications</param>
/// <param name="EffectiveFromDate">When inspection becomes effective</param>
/// <param name="EffectiveToDate">When inspection becomes ineffective</param>
public record QualityInspectionCreationData(
    string InspectionId,
    QualityInspectionType InspectionType,
    QualityInspectionLevel InspectionLevel,
    CanonicalReference ContextReference,
    CanonicalReference InspectorReference,
    IEnumerable<CanonicalReference>? SpecificationReferences = null,
    DateTime? EffectiveFromDate = null,
    DateTime? EffectiveToDate = null
);
