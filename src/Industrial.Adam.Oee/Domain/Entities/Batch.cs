using Industrial.Adam.Oee.Domain.Interfaces;
using Industrial.Adam.Oee.Domain.ValueObjects;

namespace Industrial.Adam.Oee.Domain.Entities;

/// <summary>
/// Batch Aggregate Root
/// 
/// Represents a production batch/lot with quality metrics, genealogy tracking,
/// and material consumption. Provides traceability and quality management
/// capabilities for manufacturing processes.
/// </summary>
public sealed class Batch : Entity<string>, IAggregateRoot
{
    /// <summary>
    /// Batch number for identification and traceability
    /// (immutable)
    /// </summary>
    public string BatchNumber { get; private set; }

    /// <summary>
    /// Associated work order reference
    /// (immutable)
    /// </summary>
    public CanonicalReference WorkOrderReference { get; private set; }

    /// <summary>
    /// Product reference being produced in this batch
    /// (immutable)
    /// </summary>
    public CanonicalReference ProductReference { get; private set; }

    /// <summary>
    /// Planned batch size
    /// (immutable)
    /// </summary>
    public decimal PlannedQuantity { get; private set; }

    /// <summary>
    /// Actual quantity produced (good + defective)
    /// </summary>
    public decimal ActualQuantity { get; private set; }

    /// <summary>
    /// Quantity of good pieces produced
    /// </summary>
    public decimal GoodQuantity { get; private set; }

    /// <summary>
    /// Quantity of defective/scrap pieces
    /// </summary>
    public decimal DefectiveQuantity { get; private set; }

    /// <summary>
    /// Unit of measure reference for quantities
    /// (immutable)
    /// </summary>
    public CanonicalReference UnitOfMeasureReference { get; private set; }

    /// <summary>
    /// Current batch status
    /// </summary>
    public BatchStatus Status { get; private set; }

    /// <summary>
    /// Quality score for this batch (0-100)
    /// </summary>
    public decimal QualityScore { get; private set; }

    /// <summary>
    /// Batch start time
    /// </summary>
    public DateTime? StartTime { get; private set; }

    /// <summary>
    /// Batch completion time
    /// </summary>
    public DateTime? CompletionTime { get; private set; }

    /// <summary>
    /// Operator reference responsible for this batch
    /// (immutable)
    /// </summary>
    public CanonicalReference OperatorReference { get; private set; }

    /// <summary>
    /// Shift reference during which this batch was produced
    /// (immutable)
    /// </summary>
    public CanonicalReference? ShiftReference { get; private set; }

    /// <summary>
    /// Parent batch reference for genealogy tracking
    /// (immutable)
    /// </summary>
    public CanonicalReference? ParentBatchReference { get; private set; }

    /// <summary>
    /// Equipment line reference where batch was produced
    /// (immutable)
    /// </summary>
    public CanonicalReference EquipmentLineReference { get; private set; }

    /// <summary>
    /// Material consumption records
    /// </summary>
    private readonly List<MaterialConsumption> _materialConsumptions = new();

    /// <summary>
    /// Quality check records
    /// </summary>
    private readonly List<QualityCheck> _qualityChecks = new();

    /// <summary>
    /// Batch notes and comments
    /// </summary>
    private readonly List<BatchNote> _notes = new();

    /// <summary>
    /// Read-only access to material consumptions
    /// </summary>
    public IReadOnlyList<MaterialConsumption> MaterialConsumptions => _materialConsumptions.AsReadOnly();

    /// <summary>
    /// Read-only access to quality checks
    /// </summary>
    public IReadOnlyList<QualityCheck> QualityChecks => _qualityChecks.AsReadOnly();

    /// <summary>
    /// Read-only access to batch notes
    /// </summary>
    public IReadOnlyList<BatchNote> Notes => _notes.AsReadOnly();

    /// <summary>
    /// When this batch record becomes effective
    /// (immutable)
    /// </summary>
    public DateTime EffectiveFromDate { get; private set; }

    /// <summary>
    /// When this batch record becomes ineffective (null = permanent)
    /// </summary>
    public DateTime? EffectiveToDate { get; private set; }

    /// <summary>
    /// When this batch record was created
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
    private Batch() : base()
    {
        BatchNumber = string.Empty;
        WorkOrderReference = CanonicalReference.ToWorkOrder("unknown");
        ProductReference = CanonicalReference.ToProduct("unknown");
        UnitOfMeasureReference = CanonicalReference.ToUom("pieces");
        OperatorReference = CanonicalReference.ToPerson("unknown");
        EquipmentLineReference = CanonicalReference.ToResource("unknown");
        Status = BatchStatus.Planned;
    }

    /// <summary>
    /// Creates a new batch
    /// </summary>
    /// <param name="batchId">Unique batch identifier (immutable)</param>
    /// <param name="batchNumber">Batch number for identification (immutable)</param>
    /// <param name="workOrderReference">Associated work order (immutable)</param>
    /// <param name="productReference">Product being produced (immutable)</param>
    /// <param name="plannedQuantity">Planned batch size (immutable)</param>
    /// <param name="operatorReference">Responsible operator (immutable)</param>
    /// <param name="equipmentLineReference">Production equipment line (immutable)</param>
    /// <param name="unitOfMeasureReference">Unit of measure (immutable)</param>
    /// <param name="parentBatchReference">Parent batch for genealogy (immutable)</param>
    /// <param name="shiftReference">Production shift (immutable)</param>
    /// <param name="effectiveFromDate">When batch becomes effective (immutable)</param>
    /// <param name="effectiveToDate">When batch becomes ineffective</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public Batch(
        string batchId,
        string batchNumber,
        CanonicalReference workOrderReference,
        CanonicalReference productReference,
        decimal plannedQuantity,
        CanonicalReference operatorReference,
        CanonicalReference equipmentLineReference,
        CanonicalReference? unitOfMeasureReference = null,
        CanonicalReference? parentBatchReference = null,
        CanonicalReference? shiftReference = null,
        DateTime? effectiveFromDate = null,
        DateTime? effectiveToDate = null) : base(batchId)
    {
        ValidateConstructorParameters(batchId, batchNumber, workOrderReference, productReference, plannedQuantity, operatorReference, equipmentLineReference);

        BatchNumber = batchNumber;
        WorkOrderReference = workOrderReference;
        ProductReference = productReference;
        PlannedQuantity = plannedQuantity;
        UnitOfMeasureReference = unitOfMeasureReference ?? CanonicalReference.ToUom("pieces");
        OperatorReference = operatorReference;
        EquipmentLineReference = equipmentLineReference;
        ParentBatchReference = parentBatchReference;
        ShiftReference = shiftReference;

        Status = BatchStatus.Planned;
        QualityScore = 100m; // Start with perfect quality

        EffectiveFromDate = effectiveFromDate ?? DateTime.UtcNow;
        EffectiveToDate = effectiveToDate;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Get total quantity produced (good + defective)
    /// </summary>
    public decimal TotalQuantityProduced => GoodQuantity + DefectiveQuantity;

    /// <summary>
    /// Check if batch is currently active
    /// </summary>
    public bool IsActive => Status == BatchStatus.InProgress;

    /// <summary>
    /// Check if batch is completed
    /// </summary>
    public bool IsCompleted => Status == BatchStatus.Completed;

    /// <summary>
    /// Check if batch is on hold
    /// </summary>
    public bool IsOnHold => Status == BatchStatus.OnHold;

    /// <summary>
    /// Start the batch production
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when batch cannot be started</exception>
    public void Start()
    {
        BatchStateTransitions.ValidateTransition(Status, BatchStatus.InProgress);

        Status = BatchStatus.InProgress;
        StartTime = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Place batch on hold
    /// </summary>
    /// <param name="reason">Reason for hold</param>
    /// <exception cref="InvalidOperationException">Thrown when batch cannot be placed on hold</exception>
    public void PlaceOnHold(string reason)
    {
        BatchStateTransitions.ValidateTransition(Status, BatchStatus.OnHold);

        Status = BatchStatus.OnHold;
        UpdatedAt = DateTime.UtcNow;

        AddNote($"Batch placed on hold: {reason}", OperatorReference.Id);
    }

    /// <summary>
    /// Resume batch from hold
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when batch cannot be resumed</exception>
    public void Resume()
    {
        BatchStateTransitions.ValidateTransition(Status, BatchStatus.InProgress);

        Status = BatchStatus.InProgress;
        UpdatedAt = DateTime.UtcNow;

        AddNote("Batch resumed from hold", OperatorReference.Id);
    }

    /// <summary>
    /// Complete the batch
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when batch cannot be completed</exception>
    public void Complete()
    {
        BatchStateTransitions.ValidateTransition(Status, BatchStatus.Completed);

        Status = BatchStatus.Completed;
        CompletionTime = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        // Recalculate final quality score
        RecalculateQualityScore();
    }

    /// <summary>
    /// Cancel the batch
    /// </summary>
    /// <param name="reason">Cancellation reason</param>
    /// <exception cref="InvalidOperationException">Thrown when batch cannot be cancelled</exception>
    public void Cancel(string reason)
    {
        BatchStateTransitions.ValidateTransition(Status, BatchStatus.Cancelled);

        Status = BatchStatus.Cancelled;
        if (CompletionTime == null)
            CompletionTime = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        AddNote($"Batch cancelled: {reason}", OperatorReference.Id);
    }

    /// <summary>
    /// Update production quantities
    /// </summary>
    /// <param name="goodQuantity">Good pieces produced</param>
    /// <param name="defectiveQuantity">Defective pieces produced</param>
    /// <exception cref="ArgumentException">Thrown when quantities are invalid</exception>
    public void UpdateQuantities(decimal goodQuantity, decimal defectiveQuantity)
    {
        if (goodQuantity < 0)
            throw new ArgumentException("Good quantity cannot be negative", nameof(goodQuantity));

        if (defectiveQuantity < 0)
            throw new ArgumentException("Defective quantity cannot be negative", nameof(defectiveQuantity));

        GoodQuantity = goodQuantity;
        DefectiveQuantity = defectiveQuantity;
        ActualQuantity = goodQuantity + defectiveQuantity;
        UpdatedAt = DateTime.UtcNow;

        // Recalculate quality score based on new quantities
        RecalculateQualityScore();
    }

    /// <summary>
    /// Add material consumption record
    /// </summary>
    /// <param name="materialId">Material identifier</param>
    /// <param name="quantityUsed">Quantity consumed</param>
    /// <param name="unitOfMeasure">Unit of measure</param>
    /// <param name="batchLotNumber">Material batch/lot number</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public void AddMaterialConsumption(string materialId, decimal quantityUsed, string unitOfMeasure, string? batchLotNumber = null)
    {
        if (string.IsNullOrWhiteSpace(materialId))
            throw new ArgumentException("Material ID is required", nameof(materialId));

        if (quantityUsed <= 0)
            throw new ArgumentException("Quantity used must be positive", nameof(quantityUsed));

        var consumption = new MaterialConsumption(
            materialId,
            quantityUsed,
            unitOfMeasure,
            DateTime.UtcNow,
            batchLotNumber
        );

        _materialConsumptions.Add(consumption);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Add quality check record
    /// </summary>
    /// <param name="checkType">Type of quality check</param>
    /// <param name="result">Check result</param>
    /// <param name="value">Measured value</param>
    /// <param name="specification">Quality specification</param>
    /// <param name="operatorId">Operator performing check</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public void AddQualityCheck(string checkType, QualityCheckResult result, decimal? value = null, string? specification = null, string? operatorId = null)
    {
        if (string.IsNullOrWhiteSpace(checkType))
            throw new ArgumentException("Check type is required", nameof(checkType));

        var qualityCheck = new QualityCheck(
            checkType,
            result,
            DateTime.UtcNow,
            operatorId ?? OperatorReference.Id,
            value,
            specification
        );

        _qualityChecks.Add(qualityCheck);
        UpdatedAt = DateTime.UtcNow;

        // Recalculate quality score including this check
        RecalculateQualityScore();
    }

    /// <summary>
    /// Add note to batch
    /// </summary>
    /// <param name="noteText">Note content</param>
    /// <param name="authorId">Note author</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public void AddNote(string noteText, string authorId)
    {
        if (string.IsNullOrWhiteSpace(noteText))
            throw new ArgumentException("Note text is required", nameof(noteText));

        if (string.IsNullOrWhiteSpace(authorId))
            throw new ArgumentException("Author ID is required", nameof(authorId));

        var note = new BatchNote(
            noteText,
            authorId,
            DateTime.UtcNow
        );

        _notes.Add(note);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Calculate completion percentage
    /// </summary>
    /// <returns>Completion percentage (0-100)</returns>
    public decimal GetCompletionPercentage()
    {
        if (PlannedQuantity == 0)
            return 0;

        return Math.Min(100, (TotalQuantityProduced / PlannedQuantity) * 100);
    }

    /// <summary>
    /// Calculate yield percentage (good vs total)
    /// </summary>
    /// <returns>Yield percentage (0-100)</returns>
    public decimal GetYieldPercentage()
    {
        var total = TotalQuantityProduced;
        if (total == 0)
            return 100; // No production = no defects

        return (GoodQuantity / total) * 100;
    }

    /// <summary>
    /// Get production rate (pieces per hour)
    /// </summary>
    /// <returns>Production rate in pieces per hour</returns>
    public decimal GetProductionRate()
    {
        if (StartTime == null)
            return 0;

        var endTime = CompletionTime ?? DateTime.UtcNow;
        var durationHours = (decimal)(endTime - StartTime.Value).TotalHours;

        if (durationHours == 0)
            return 0;

        return TotalQuantityProduced / durationHours;
    }

    /// <summary>
    /// Check if batch requires quality review
    /// </summary>
    /// <param name="qualityThreshold">Minimum quality score threshold</param>
    /// <returns>True if requires review</returns>
    public bool RequiresQualityReview(decimal qualityThreshold = 95m)
    {
        return QualityScore < qualityThreshold ||
               _qualityChecks.Any(q => q.Result == QualityCheckResult.Failed);
    }

    /// <summary>
    /// Get genealogy chain (parent batches)
    /// </summary>
    /// <returns>List of parent batch references</returns>
    public List<CanonicalReference> GetGenealogyChain()
    {
        var chain = new List<CanonicalReference>();
        if (ParentBatchReference != null)
        {
            chain.Add(ParentBatchReference);
            // Note: Full genealogy would require repository lookup of parent batches
        }
        return chain;
    }

    /// <summary>
    /// Get batch summary for reporting
    /// </summary>
    /// <returns>Batch summary</returns>
    public BatchSummary ToSummary()
    {
        return new BatchSummary(
            Id,
            BatchNumber,
            WorkOrderReference,
            ProductReference,
            Status.ToString(),
            GetCompletionPercentage(),
            GetYieldPercentage(),
            QualityScore,
            new BatchQuantities(PlannedQuantity, GoodQuantity, DefectiveQuantity, TotalQuantityProduced),
            StartTime,
            CompletionTime,
            OperatorReference,
            ShiftReference,
            ParentBatchReference,
            EquipmentLineReference,
            _materialConsumptions.Count,
            _qualityChecks.Count(q => q.Result == QualityCheckResult.Passed),
            _qualityChecks.Count(q => q.Result == QualityCheckResult.Failed)
        );
    }

    /// <summary>
    /// Recalculate quality score based on yield and quality checks
    /// </summary>
    private void RecalculateQualityScore()
    {
        var yieldPercentage = GetYieldPercentage();
        var qualityCheckScore = 100m;

        if (_qualityChecks.Any())
        {
            var totalChecks = _qualityChecks.Count;
            var passedChecks = _qualityChecks.Count(q => q.Result == QualityCheckResult.Passed);
            qualityCheckScore = (decimal)passedChecks / totalChecks * 100;
        }

        // Weighted average: 60% yield + 40% quality checks
        QualityScore = (yieldPercentage * 0.6m) + (qualityCheckScore * 0.4m);
    }

    /// <summary>
    /// Validate constructor parameters
    /// </summary>
    private static void ValidateConstructorParameters(
        string batchId,
        string batchNumber,
        CanonicalReference workOrderReference,
        CanonicalReference productReference,
        decimal plannedQuantity,
        CanonicalReference operatorReference,
        CanonicalReference equipmentLineReference)
    {
        if (string.IsNullOrWhiteSpace(batchId))
            throw new ArgumentException("Batch ID is required", nameof(batchId));

        if (string.IsNullOrWhiteSpace(batchNumber))
            throw new ArgumentException("Batch number is required", nameof(batchNumber));

        if (workOrderReference == null)
            throw new ArgumentNullException(nameof(workOrderReference));

        if (!workOrderReference.IsWorkOrder)
            throw new ArgumentException("Reference must be to a work order", nameof(workOrderReference));

        if (productReference == null)
            throw new ArgumentNullException(nameof(productReference));

        if (!productReference.IsProduct)
            throw new ArgumentException("Reference must be to a product", nameof(productReference));

        if (plannedQuantity <= 0)
            throw new ArgumentException("Planned quantity must be positive", nameof(plannedQuantity));

        if (operatorReference == null)
            throw new ArgumentNullException(nameof(operatorReference));

        if (!operatorReference.IsPerson)
            throw new ArgumentException("Reference must be to a person", nameof(operatorReference));

        if (equipmentLineReference == null)
            throw new ArgumentNullException(nameof(equipmentLineReference));

        if (!equipmentLineReference.IsResource)
            throw new ArgumentException("Reference must be to a resource", nameof(equipmentLineReference));
    }

    /// <summary>
    /// String representation of the batch
    /// </summary>
    /// <returns>Formatted string representation</returns>
    public override string ToString()
    {
        return $"Batch {BatchNumber}: {ProductReference.Id} ({GetCompletionPercentage():F1}% complete, {QualityScore:F1} quality score)";
    }
}

/// <summary>
/// Batch status enumeration
/// </summary>
public enum BatchStatus
{
    /// <summary>
    /// Batch is planned but not started
    /// </summary>
    Planned,

    /// <summary>
    /// Batch is currently in progress
    /// </summary>
    InProgress,

    /// <summary>
    /// Batch is temporarily on hold
    /// </summary>
    OnHold,

    /// <summary>
    /// Batch has been completed
    /// </summary>
    Completed,

    /// <summary>
    /// Batch has been cancelled
    /// </summary>
    Cancelled
}

/// <summary>
/// Quality check result enumeration
/// </summary>
public enum QualityCheckResult
{
    /// <summary>
    /// Quality check passed
    /// </summary>
    Passed,

    /// <summary>
    /// Quality check failed
    /// </summary>
    Failed,

    /// <summary>
    /// Quality check pending review
    /// </summary>
    Pending
}

/// <summary>
/// Material consumption record
/// </summary>
/// <param name="MaterialId">Material identifier</param>
/// <param name="QuantityUsed">Quantity consumed</param>
/// <param name="UnitOfMeasure">Unit of measure</param>
/// <param name="ConsumedAt">Consumption timestamp</param>
/// <param name="BatchLotNumber">Material batch/lot number</param>
public record MaterialConsumption(
    string MaterialId,
    decimal QuantityUsed,
    string UnitOfMeasure,
    DateTime ConsumedAt,
    string? BatchLotNumber = null
);

/// <summary>
/// Quality check record
/// </summary>
/// <param name="CheckType">Type of quality check</param>
/// <param name="Result">Check result</param>
/// <param name="CheckedAt">Check timestamp</param>
/// <param name="OperatorId">Operator performing check</param>
/// <param name="Value">Measured value</param>
/// <param name="Specification">Quality specification</param>
public record QualityCheck(
    string CheckType,
    QualityCheckResult Result,
    DateTime CheckedAt,
    string OperatorId,
    decimal? Value = null,
    string? Specification = null
);

/// <summary>
/// Batch note record
/// </summary>
/// <param name="NoteText">Note content</param>
/// <param name="AuthorId">Note author</param>
/// <param name="CreatedAt">Note timestamp</param>
public record BatchNote(
    string NoteText,
    string AuthorId,
    DateTime CreatedAt
);

/// <summary>
/// Batch quantities
/// </summary>
/// <param name="Planned">Planned quantity</param>
/// <param name="Good">Good quantity produced</param>
/// <param name="Defective">Defective quantity produced</param>
/// <param name="Total">Total quantity produced</param>
public record BatchQuantities(
    decimal Planned,
    decimal Good,
    decimal Defective,
    decimal Total
);

/// <summary>
/// Batch summary for reporting
/// </summary>
/// <param name="BatchId">Batch identifier</param>
/// <param name="BatchNumber">Batch number</param>
/// <param name="WorkOrderReference">Associated work order reference</param>
/// <param name="ProductReference">Product reference</param>
/// <param name="Status">Current status</param>
/// <param name="CompletionPercentage">Completion percentage</param>
/// <param name="YieldPercentage">Yield percentage</param>
/// <param name="QualityScore">Quality score</param>
/// <param name="Quantities">Quantity information</param>
/// <param name="StartTime">Batch start time</param>
/// <param name="CompletionTime">Batch completion time</param>
/// <param name="OperatorReference">Responsible operator reference</param>
/// <param name="ShiftReference">Production shift reference</param>
/// <param name="ParentBatchReference">Parent batch reference for genealogy</param>
/// <param name="EquipmentLineReference">Production equipment line reference</param>
/// <param name="MaterialConsumptionCount">Number of material consumption records</param>
/// <param name="QualityChecksPassedCount">Number of passed quality checks</param>
/// <param name="QualityChecksFailedCount">Number of failed quality checks</param>
public record BatchSummary(
    string BatchId,
    string BatchNumber,
    CanonicalReference WorkOrderReference,
    CanonicalReference ProductReference,
    string Status,
    decimal CompletionPercentage,
    decimal YieldPercentage,
    decimal QualityScore,
    BatchQuantities Quantities,
    DateTime? StartTime,
    DateTime? CompletionTime,
    CanonicalReference OperatorReference,
    CanonicalReference? ShiftReference,
    CanonicalReference? ParentBatchReference,
    CanonicalReference EquipmentLineReference,
    int MaterialConsumptionCount,
    int QualityChecksPassedCount,
    int QualityChecksFailedCount
);

/// <summary>
/// Batch creation data
/// </summary>
/// <param name="BatchId">Unique batch identifier (immutable)</param>
/// <param name="BatchNumber">Batch number for identification (immutable)</param>
/// <param name="WorkOrderReference">Associated work order reference (immutable)</param>
/// <param name="ProductReference">Product reference being produced (immutable)</param>
/// <param name="PlannedQuantity">Planned batch size (immutable)</param>
/// <param name="OperatorReference">Responsible operator reference (immutable)</param>
/// <param name="EquipmentLineReference">Production equipment line reference (immutable)</param>
/// <param name="UnitOfMeasureReference">Unit of measure reference (immutable)</param>
/// <param name="ParentBatchReference">Parent batch reference for genealogy (immutable)</param>
/// <param name="ShiftReference">Production shift reference (immutable)</param>
/// <param name="EffectiveFromDate">When batch becomes effective (immutable)</param>
/// <param name="EffectiveToDate">When batch becomes ineffective</param>
public record BatchCreationData(
    string BatchId,
    string BatchNumber,
    CanonicalReference WorkOrderReference,
    CanonicalReference ProductReference,
    decimal PlannedQuantity,
    CanonicalReference OperatorReference,
    CanonicalReference EquipmentLineReference,
    CanonicalReference? UnitOfMeasureReference = null,
    CanonicalReference? ParentBatchReference = null,
    CanonicalReference? ShiftReference = null,
    DateTime? EffectiveFromDate = null,
    DateTime? EffectiveToDate = null
);
