using Industrial.Adam.Oee.Domain.Interfaces;

namespace Industrial.Adam.Oee.Domain.Entities;

/// <summary>
/// Work Order Aggregate Root (Canonical Model)
/// 
/// Represents a production work order that provides business context 
/// to overlay on immutable counter data. This entity bridges the gap between
/// raw counter readings and meaningful production metrics.
/// 
/// Uses exact canonical model field names for future compatibility.
/// </summary>
public sealed class WorkOrder : Entity<string>, IAggregateRoot
{
    /// <summary>
    /// Work order description
    /// </summary>
    public string WorkOrderDescription { get; private set; }

    /// <summary>
    /// Product identifier
    /// </summary>
    public string ProductId { get; private set; }

    /// <summary>
    /// Product description
    /// </summary>
    public string ProductDescription { get; private set; }

    /// <summary>
    /// Planned quantity to produce
    /// </summary>
    public decimal PlannedQuantity { get; private set; }

    /// <summary>
    /// Unit of measure for quantities
    /// </summary>
    public string UnitOfMeasure { get; private set; }

    /// <summary>
    /// Scheduled start time
    /// </summary>
    public DateTime ScheduledStartTime { get; private set; }

    /// <summary>
    /// Scheduled end time
    /// </summary>
    public DateTime ScheduledEndTime { get; private set; }

    /// <summary>
    /// Resource reference (device/machine identifier)
    /// </summary>
    public string ResourceReference { get; private set; }

    /// <summary>
    /// Current work order status
    /// </summary>
    public WorkOrderStatus Status { get; private set; }

    /// <summary>
    /// Actual quantity of good pieces produced
    /// </summary>
    public decimal ActualQuantityGood { get; private set; }

    /// <summary>
    /// Actual quantity of scrap/defective pieces
    /// </summary>
    public decimal ActualQuantityScrap { get; private set; }

    /// <summary>
    /// Actual start time (when work began)
    /// </summary>
    public DateTime? ActualStartTime { get; private set; }

    /// <summary>
    /// Actual end time (when work completed)
    /// </summary>
    public DateTime? ActualEndTime { get; private set; }

    /// <summary>
    /// When this work order was created
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    // Phase 3 Enhancements: Batch Tracking and Enhanced Scheduling

    /// <summary>
    /// Job priority (1 = highest, 10 = lowest)
    /// </summary>
    public int Priority { get; private set; }

    /// <summary>
    /// Current shift identifier for this work order
    /// </summary>
    public string? ShiftId { get; private set; }

    /// <summary>
    /// Job schedule identifier if scheduled
    /// </summary>
    public string? JobScheduleId { get; private set; }

    /// <summary>
    /// Whether this work order uses batch tracking
    /// </summary>
    public bool BatchTrackingEnabled { get; private set; }

    /// <summary>
    /// Planned batch size for batch tracking
    /// </summary>
    public decimal? PlannedBatchSize { get; private set; }

    /// <summary>
    /// Estimated setup time in minutes
    /// </summary>
    public decimal SetupTimeMinutes { get; private set; }

    /// <summary>
    /// Estimated teardown time in minutes
    /// </summary>
    public decimal TeardownTimeMinutes { get; private set; }

    /// <summary>
    /// Work order complexity score (1-10, affects scheduling)
    /// </summary>
    public int ComplexityScore { get; private set; }

    /// <summary>
    /// Customer priority (for customer orders)
    /// </summary>
    public string? CustomerPriority { get; private set; }

    /// <summary>
    /// Due date for work order completion
    /// </summary>
    public DateTime? DueDate { get; private set; }

    /// <summary>
    /// Batches associated with this work order
    /// </summary>
    private readonly List<string> _batchIds = new();

    /// <summary>
    /// Quality gates required for this work order
    /// </summary>
    private readonly List<string> _requiredQualityGateIds = new();

    /// <summary>
    /// Material requirements for this work order
    /// </summary>
    private readonly List<WorkOrderMaterialRequirement> _materialRequirements = new();

    /// <summary>
    /// Work order notes and comments
    /// </summary>
    private readonly List<WorkOrderNote> _notes = new();

    /// <summary>
    /// Read-only access to batch IDs
    /// </summary>
    public IReadOnlyList<string> BatchIds => _batchIds.AsReadOnly();

    /// <summary>
    /// Read-only access to required quality gate IDs
    /// </summary>
    public IReadOnlyList<string> RequiredQualityGateIds => _requiredQualityGateIds.AsReadOnly();

    /// <summary>
    /// Read-only access to material requirements
    /// </summary>
    public IReadOnlyList<WorkOrderMaterialRequirement> MaterialRequirements => _materialRequirements.AsReadOnly();

    /// <summary>
    /// Read-only access to work order notes
    /// </summary>
    public IReadOnlyList<WorkOrderNote> Notes => _notes.AsReadOnly();

    /// <summary>
    /// Default constructor for serialization
    /// </summary>
    private WorkOrder() : base()
    {
        WorkOrderDescription = string.Empty;
        ProductId = string.Empty;
        ProductDescription = string.Empty;
        UnitOfMeasure = "pieces";
        ResourceReference = string.Empty;
        Status = WorkOrderStatus.Pending;
        Priority = 5; // Default medium priority
        ComplexityScore = 5; // Default medium complexity
    }

    /// <summary>
    /// Creates a new work order
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="workOrderDescription">Work order description</param>
    /// <param name="productId">Product identifier</param>
    /// <param name="productDescription">Product description</param>
    /// <param name="plannedQuantity">Planned quantity to produce</param>
    /// <param name="scheduledStartTime">Scheduled start time</param>
    /// <param name="scheduledEndTime">Scheduled end time</param>
    /// <param name="resourceReference">Resource/device identifier</param>
    /// <param name="unitOfMeasure">Unit of measure (default: pieces)</param>
    /// <param name="actualQuantityGood">Initial good quantity (default: 0)</param>
    /// <param name="actualQuantityScrap">Initial scrap quantity (default: 0)</param>
    /// <param name="actualStartTime">Actual start time (optional)</param>
    /// <param name="actualEndTime">Actual end time (optional)</param>
    /// <param name="status">Initial status (default: Pending)</param>
    /// <param name="priority">Job priority (1-10, default: 5)</param>
    /// <param name="shiftId">Shift identifier</param>
    /// <param name="jobScheduleId">Job schedule identifier</param>
    /// <param name="batchTrackingEnabled">Enable batch tracking</param>
    /// <param name="plannedBatchSize">Planned batch size</param>
    /// <param name="setupTimeMinutes">Setup time in minutes</param>
    /// <param name="teardownTimeMinutes">Teardown time in minutes</param>
    /// <param name="complexityScore">Complexity score (1-10, default: 5)</param>
    /// <param name="customerPriority">Customer priority</param>
    /// <param name="dueDate">Due date</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public WorkOrder(
        string workOrderId,
        string workOrderDescription,
        string productId,
        string productDescription,
        decimal plannedQuantity,
        DateTime scheduledStartTime,
        DateTime scheduledEndTime,
        string resourceReference,
        string unitOfMeasure = "pieces",
        decimal actualQuantityGood = 0,
        decimal actualQuantityScrap = 0,
        DateTime? actualStartTime = null,
        DateTime? actualEndTime = null,
        WorkOrderStatus status = WorkOrderStatus.Pending,
        int priority = 5,
        string? shiftId = null,
        string? jobScheduleId = null,
        bool batchTrackingEnabled = false,
        decimal? plannedBatchSize = null,
        decimal setupTimeMinutes = 0,
        decimal teardownTimeMinutes = 0,
        int complexityScore = 5,
        string? customerPriority = null,
        DateTime? dueDate = null) : base(workOrderId)
    {
        ValidateConstructorParameters(workOrderId, plannedQuantity, scheduledStartTime, scheduledEndTime);

        WorkOrderDescription = workOrderDescription;
        ProductId = productId;
        ProductDescription = productDescription;
        PlannedQuantity = plannedQuantity;
        UnitOfMeasure = unitOfMeasure;
        ScheduledStartTime = scheduledStartTime;
        ScheduledEndTime = scheduledEndTime;
        ResourceReference = resourceReference;

        ActualQuantityGood = actualQuantityGood;
        ActualQuantityScrap = actualQuantityScrap;
        ActualStartTime = actualStartTime;
        ActualEndTime = actualEndTime;
        Status = status;

        // Phase 3 properties
        Priority = priority;
        ShiftId = shiftId;
        JobScheduleId = jobScheduleId;
        BatchTrackingEnabled = batchTrackingEnabled;
        PlannedBatchSize = plannedBatchSize;
        SetupTimeMinutes = setupTimeMinutes;
        TeardownTimeMinutes = teardownTimeMinutes;
        ComplexityScore = complexityScore;
        CustomerPriority = customerPriority;
        DueDate = dueDate;

        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Get total quantity produced (good + scrap)
    /// </summary>
    public decimal TotalQuantityProduced => ActualQuantityGood + ActualQuantityScrap;

    /// <summary>
    /// Check if work order is currently active
    /// </summary>
    public bool IsActive => Status == WorkOrderStatus.Active;

    /// <summary>
    /// Check if work order is completed
    /// </summary>
    public bool IsCompleted => Status == WorkOrderStatus.Completed;

    /// <summary>
    /// Update production quantities from counter channel data
    /// This is the bridge between immutable counter data and business context
    /// </summary>
    /// <param name="goodCount">Count from production channel (good pieces)</param>
    /// <param name="scrapCount">Count from reject channel (defects)</param>
    /// <exception cref="ArgumentException">Thrown when counter values are negative</exception>
    public void UpdateFromCounterData(decimal goodCount, decimal scrapCount)
    {
        if (goodCount < 0)
            throw new ArgumentException("Good count cannot be negative", nameof(goodCount));

        if (scrapCount < 0)
            throw new ArgumentException("Scrap count cannot be negative", nameof(scrapCount));

        ActualQuantityGood = goodCount;
        ActualQuantityScrap = scrapCount;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Start the work order
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when work order cannot be started</exception>
    public void Start()
    {
        if (Status != WorkOrderStatus.Pending)
            throw new InvalidOperationException($"Cannot start work order with status: {Status}");

        Status = WorkOrderStatus.Active;
        ActualStartTime = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Pause the work order (e.g., for breaks, maintenance)
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when work order cannot be paused</exception>
    public void Pause()
    {
        if (Status != WorkOrderStatus.Active)
            throw new InvalidOperationException($"Cannot pause work order with status: {Status}");

        Status = WorkOrderStatus.Paused;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Resume a paused work order
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when work order cannot be resumed</exception>
    public void Resume()
    {
        if (Status != WorkOrderStatus.Paused)
            throw new InvalidOperationException($"Cannot resume work order with status: {Status}");

        Status = WorkOrderStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Complete the work order
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when work order cannot be completed</exception>
    public void Complete()
    {
        if (Status != WorkOrderStatus.Active && Status != WorkOrderStatus.Paused)
            throw new InvalidOperationException($"Cannot complete work order with status: {Status}");

        Status = WorkOrderStatus.Completed;
        ActualEndTime = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Cancel the work order
    /// </summary>
    /// <param name="reason">Optional cancellation reason</param>
    /// <exception cref="InvalidOperationException">Thrown when work order cannot be cancelled</exception>
    public void Cancel(string? reason = null)
    {
        if (Status == WorkOrderStatus.Completed || Status == WorkOrderStatus.Cancelled)
            throw new InvalidOperationException($"Cannot cancel work order with status: {Status}");

        Status = WorkOrderStatus.Cancelled;
        if (ActualEndTime == null)
            ActualEndTime = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Calculate completion percentage based on planned quantity
    /// </summary>
    /// <returns>Completion percentage (0-100)</returns>
    public decimal GetCompletionPercentage()
    {
        if (PlannedQuantity == 0)
            return 0;

        return (TotalQuantityProduced / PlannedQuantity) * 100;
    }

    /// <summary>
    /// Calculate yield/quality percentage
    /// </summary>
    /// <returns>Yield percentage (0-100)</returns>
    public decimal GetYieldPercentage()
    {
        var total = TotalQuantityProduced;
        if (total == 0)
            return 100; // No production = no defects

        return (ActualQuantityGood / total) * 100;
    }

    /// <summary>
    /// Check if work order is behind schedule
    /// </summary>
    /// <returns>True if behind schedule</returns>
    public bool IsBehindSchedule()
    {
        var now = DateTime.UtcNow;
        var scheduledDuration = (ScheduledEndTime - ScheduledStartTime).TotalMilliseconds;
        var elapsedTime = (now - ScheduledStartTime).TotalMilliseconds;
        var expectedProgress = (elapsedTime / scheduledDuration) * 100;

        return GetCompletionPercentage() < (decimal)expectedProgress;
    }

    /// <summary>
    /// Get production rate (pieces per minute)
    /// </summary>
    /// <returns>Production rate in pieces per minute</returns>
    public decimal GetProductionRate()
    {
        if (ActualStartTime == null || Status == WorkOrderStatus.Pending)
            return 0;

        var endTime = ActualEndTime ?? DateTime.UtcNow;
        var durationMinutes = (decimal)(endTime - ActualStartTime.Value).TotalMinutes;

        if (durationMinutes == 0)
            return 0;

        return TotalQuantityProduced / durationMinutes;
    }

    /// <summary>
    /// Calculate estimated completion time based on current rate
    /// </summary>
    /// <returns>Estimated completion time or null if cannot be calculated</returns>
    public DateTime? GetEstimatedCompletionTime()
    {
        var rate = GetProductionRate();
        if (rate == 0)
            return null;

        var remainingQuantity = PlannedQuantity - TotalQuantityProduced;
        if (remainingQuantity <= 0)
            return DateTime.UtcNow;

        var remainingMinutes = remainingQuantity / rate;
        return DateTime.UtcNow.AddMinutes((double)remainingMinutes);
    }

    /// <summary>
    /// Check if work order requires attention
    /// </summary>
    /// <param name="qualityThreshold">Quality threshold percentage (default 95%)</param>
    /// <returns>True if requires attention</returns>
    public bool RequiresAttention(decimal qualityThreshold = 95m)
    {
        return IsBehindSchedule() ||
               GetYieldPercentage() < qualityThreshold ||
               (Status == WorkOrderStatus.Active && GetProductionRate() == 0);
    }

    /// <summary>
    /// Get work order summary for reporting
    /// </summary>
    /// <returns>Work order summary</returns>
    public WorkOrderSummary ToSummary()
    {
        return new WorkOrderSummary(
            Id,
            ProductDescription,
            Status.ToString(),
            GetCompletionPercentage(),
            GetYieldPercentage(),
            ScheduledStartTime,
            ScheduledEndTime,
            ActualStartTime,
            ActualEndTime,
            new WorkOrderQuantities(PlannedQuantity, ActualQuantityGood, ActualQuantityScrap, TotalQuantityProduced),
            new WorkOrderPerformance(
                GetCompletionPercentage(),
                GetYieldPercentage(),
                GetProductionRate(),
                IsBehindSchedule(),
                RequiresAttention()
            )
        );
    }

    /// <summary>
    /// Create a work order from counter snapshot
    /// </summary>
    /// <param name="workOrderData">Work order creation data</param>
    /// <param name="counterSnapshot">Current counter readings</param>
    /// <returns>New WorkOrder instance</returns>
    public static WorkOrder FromCounterSnapshot(
        WorkOrderCreationData workOrderData,
        CounterSnapshot counterSnapshot)
    {
        return new WorkOrder(
            workOrderData.WorkOrderId,
            workOrderData.WorkOrderDescription,
            workOrderData.ProductId,
            workOrderData.ProductDescription,
            workOrderData.PlannedQuantity,
            workOrderData.ScheduledStartTime,
            workOrderData.ScheduledEndTime,
            workOrderData.ResourceReference,
            workOrderData.UnitOfMeasure,
            counterSnapshot.Channel0Count,
            counterSnapshot.Channel1Count,
            DateTime.UtcNow,
            null,
            WorkOrderStatus.Active
        );
    }

    /// <summary>
    /// Validate constructor parameters
    /// </summary>
    private static void ValidateConstructorParameters(
        string workOrderId,
        decimal plannedQuantity,
        DateTime scheduledStartTime,
        DateTime scheduledEndTime)
    {
        if (string.IsNullOrWhiteSpace(workOrderId))
            throw new ArgumentException("Work order ID is required", nameof(workOrderId));

        if (plannedQuantity <= 0)
            throw new ArgumentException("Planned quantity must be positive", nameof(plannedQuantity));

        if (scheduledEndTime <= scheduledStartTime)
            throw new ArgumentException("Scheduled end time must be after start time", nameof(scheduledEndTime));
    }

    // Phase 3 Enhancement Methods

    /// <summary>
    /// Update job priority
    /// </summary>
    /// <param name="priority">New priority (1-10)</param>
    /// <exception cref="ArgumentException">Thrown when priority is invalid</exception>
    public void UpdatePriority(int priority)
    {
        if (priority < 1 || priority > 10)
            throw new ArgumentException("Priority must be between 1 and 10", nameof(priority));

        Priority = priority;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update shift assignment
    /// </summary>
    /// <param name="shiftId">New shift identifier</param>
    public void UpdateShift(string? shiftId)
    {
        ShiftId = shiftId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update job schedule assignment
    /// </summary>
    /// <param name="jobScheduleId">Job schedule identifier</param>
    public void UpdateJobSchedule(string? jobScheduleId)
    {
        JobScheduleId = jobScheduleId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update due date
    /// </summary>
    /// <param name="dueDate">New due date</param>
    public void UpdateDueDate(DateTime? dueDate)
    {
        DueDate = dueDate;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Add batch to work order
    /// </summary>
    /// <param name="batchId">Batch identifier</param>
    /// <exception cref="ArgumentException">Thrown when batch ID is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when batch tracking is disabled</exception>
    public void AddBatch(string batchId)
    {
        if (string.IsNullOrWhiteSpace(batchId))
            throw new ArgumentException("Batch ID is required", nameof(batchId));

        if (!BatchTrackingEnabled)
            throw new InvalidOperationException("Batch tracking is not enabled for this work order");

        if (_batchIds.Contains(batchId))
            throw new InvalidOperationException($"Batch {batchId} is already associated with this work order");

        _batchIds.Add(batchId);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Remove batch from work order
    /// </summary>
    /// <param name="batchId">Batch identifier</param>
    /// <exception cref="ArgumentException">Thrown when batch not found</exception>
    public void RemoveBatch(string batchId)
    {
        if (!_batchIds.Remove(batchId))
            throw new ArgumentException($"Batch {batchId} not found in work order", nameof(batchId));

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Add required quality gate
    /// </summary>
    /// <param name="qualityGateId">Quality gate identifier</param>
    /// <exception cref="ArgumentException">Thrown when quality gate ID is invalid</exception>
    public void AddRequiredQualityGate(string qualityGateId)
    {
        if (string.IsNullOrWhiteSpace(qualityGateId))
            throw new ArgumentException("Quality gate ID is required", nameof(qualityGateId));

        if (_requiredQualityGateIds.Contains(qualityGateId))
            throw new InvalidOperationException($"Quality gate {qualityGateId} is already required for this work order");

        _requiredQualityGateIds.Add(qualityGateId);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Remove required quality gate
    /// </summary>
    /// <param name="qualityGateId">Quality gate identifier</param>
    /// <exception cref="ArgumentException">Thrown when quality gate not found</exception>
    public void RemoveRequiredQualityGate(string qualityGateId)
    {
        if (!_requiredQualityGateIds.Remove(qualityGateId))
            throw new ArgumentException($"Quality gate {qualityGateId} not found in work order requirements", nameof(qualityGateId));

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Add material requirement
    /// </summary>
    /// <param name="materialId">Material identifier</param>
    /// <param name="quantityRequired">Quantity required</param>
    /// <param name="unitOfMeasure">Unit of measure</param>
    /// <param name="isOptional">Whether material is optional</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public void AddMaterialRequirement(string materialId, decimal quantityRequired, string unitOfMeasure, bool isOptional = false)
    {
        if (string.IsNullOrWhiteSpace(materialId))
            throw new ArgumentException("Material ID is required", nameof(materialId));

        if (quantityRequired <= 0)
            throw new ArgumentException("Quantity required must be positive", nameof(quantityRequired));

        if (string.IsNullOrWhiteSpace(unitOfMeasure))
            throw new ArgumentException("Unit of measure is required", nameof(unitOfMeasure));

        // Check if material requirement already exists
        if (_materialRequirements.Any(mr => mr.MaterialId == materialId))
            throw new InvalidOperationException($"Material requirement for {materialId} already exists");

        var requirement = new WorkOrderMaterialRequirement(
            materialId,
            quantityRequired,
            unitOfMeasure,
            isOptional
        );

        _materialRequirements.Add(requirement);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Add note to work order
    /// </summary>
    /// <param name="noteText">Note content</param>
    /// <param name="authorId">Note author</param>
    /// <param name="noteType">Type of note</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public void AddNote(string noteText, string authorId, WorkOrderNoteType noteType = WorkOrderNoteType.General)
    {
        if (string.IsNullOrWhiteSpace(noteText))
            throw new ArgumentException("Note text is required", nameof(noteText));

        if (string.IsNullOrWhiteSpace(authorId))
            throw new ArgumentException("Author ID is required", nameof(authorId));

        var note = new WorkOrderNote(
            noteText,
            authorId,
            noteType,
            DateTime.UtcNow
        );

        _notes.Add(note);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Enable batch tracking
    /// </summary>
    /// <param name="plannedBatchSize">Planned batch size</param>
    /// <exception cref="ArgumentException">Thrown when batch size is invalid</exception>
    public void EnableBatchTracking(decimal? plannedBatchSize = null)
    {
        if (plannedBatchSize.HasValue && plannedBatchSize <= 0)
            throw new ArgumentException("Planned batch size must be positive", nameof(plannedBatchSize));

        BatchTrackingEnabled = true;
        PlannedBatchSize = plannedBatchSize;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Disable batch tracking
    /// </summary>
    public void DisableBatchTracking()
    {
        BatchTrackingEnabled = false;
        PlannedBatchSize = null;
        _batchIds.Clear();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Check if work order is overdue
    /// </summary>
    /// <returns>True if work order is overdue</returns>
    public bool IsOverdue()
    {
        if (DueDate == null || IsCompleted)
            return false;

        return DateTime.UtcNow > DueDate.Value;
    }

    /// <summary>
    /// Get total estimated time including setup and teardown
    /// </summary>
    /// <returns>Total estimated time in minutes</returns>
    public decimal GetTotalEstimatedTimeMinutes()
    {
        var estimatedProductionTime = (decimal)(ScheduledEndTime - ScheduledStartTime).TotalMinutes;
        return SetupTimeMinutes + estimatedProductionTime + TeardownTimeMinutes;
    }

    /// <summary>
    /// Get batch count
    /// </summary>
    /// <returns>Number of batches</returns>
    public int GetBatchCount() => _batchIds.Count;

    /// <summary>
    /// Check if all required quality gates are assigned
    /// </summary>
    /// <returns>True if all quality gates are assigned</returns>
    public bool HasAllRequiredQualityGates() => _requiredQualityGateIds.Any();

    /// <summary>
    /// Calculate scheduling urgency score
    /// </summary>
    /// <returns>Urgency score (higher is more urgent)</returns>
    public decimal CalculateUrgencyScore()
    {
        decimal score = 0;

        // Priority contributes to urgency (higher priority = higher urgency)
        score += (11 - Priority) * 10;

        // Due date urgency
        if (DueDate.HasValue)
        {
            var daysUntilDue = (DueDate.Value - DateTime.UtcNow).TotalDays;
            if (daysUntilDue <= 0)
                score += 100; // Overdue
            else if (daysUntilDue <= 1)
                score += 50; // Due within 1 day
            else if (daysUntilDue <= 3)
                score += 25; // Due within 3 days
        }

        // Customer priority
        if (!string.IsNullOrEmpty(CustomerPriority))
        {
            score += CustomerPriority.ToLower() switch
            {
                "critical" => 30,
                "high" => 20,
                "medium" => 10,
                "low" => 5,
                _ => 0
            };
        }

        // Complexity penalty (more complex jobs should start earlier)
        score += ComplexityScore * 2;

        return score;
    }

    /// <summary>
    /// String representation of the work order
    /// </summary>
    /// <returns>Formatted string representation</returns>
    public override string ToString()
    {
        return $"Work Order {Id}: {ProductDescription} ({GetCompletionPercentage():F1}% complete, {GetYieldPercentage():F1}% yield)";
    }
}

/// <summary>
/// Work order status enumeration
/// </summary>
public enum WorkOrderStatus
{
    /// <summary>
    /// Work order created but not started
    /// </summary>
    Pending,

    /// <summary>
    /// Work order is currently active
    /// </summary>
    Active,

    /// <summary>
    /// Work order is temporarily paused
    /// </summary>
    Paused,

    /// <summary>
    /// Work order has been completed
    /// </summary>
    Completed,

    /// <summary>
    /// Work order has been cancelled
    /// </summary>
    Cancelled
}

/// <summary>
/// Work order creation data
/// </summary>
/// <param name="WorkOrderId">Work order identifier</param>
/// <param name="WorkOrderDescription">Work order description</param>
/// <param name="ProductId">Product identifier</param>
/// <param name="ProductDescription">Product description</param>
/// <param name="PlannedQuantity">Planned quantity</param>
/// <param name="ScheduledStartTime">Scheduled start time</param>
/// <param name="ScheduledEndTime">Scheduled end time</param>
/// <param name="ResourceReference">Resource reference</param>
/// <param name="UnitOfMeasure">Unit of measure</param>
public record WorkOrderCreationData(
    string WorkOrderId,
    string WorkOrderDescription,
    string ProductId,
    string ProductDescription,
    decimal PlannedQuantity,
    DateTime ScheduledStartTime,
    DateTime ScheduledEndTime,
    string ResourceReference,
    string UnitOfMeasure = "pieces"
);

/// <summary>
/// Counter snapshot data
/// </summary>
/// <param name="Channel0Count">Good pieces count</param>
/// <param name="Channel1Count">Reject pieces count</param>
public record CounterSnapshot(
    decimal Channel0Count,
    decimal Channel1Count
);

/// <summary>
/// Work order quantities
/// </summary>
/// <param name="Planned">Planned quantity</param>
/// <param name="Good">Good quantity produced</param>
/// <param name="Scrap">Scrap quantity produced</param>
/// <param name="Total">Total quantity produced</param>
public record WorkOrderQuantities(
    decimal Planned,
    decimal Good,
    decimal Scrap,
    decimal Total
);

/// <summary>
/// Work order performance metrics
/// </summary>
/// <param name="CompletionPercentage">Completion percentage</param>
/// <param name="YieldPercentage">Yield percentage</param>
/// <param name="ProductionRate">Production rate per minute</param>
/// <param name="IsBehindSchedule">Whether behind schedule</param>
/// <param name="RequiresAttention">Whether requires attention</param>
public record WorkOrderPerformance(
    decimal CompletionPercentage,
    decimal YieldPercentage,
    decimal ProductionRate,
    bool IsBehindSchedule,
    bool RequiresAttention
);

/// <summary>
/// Work order summary for reporting
/// </summary>
/// <param name="WorkOrderId">Work order ID</param>
/// <param name="Product">Product description</param>
/// <param name="Status">Current status</param>
/// <param name="Progress">Progress percentage</param>
/// <param name="Yield">Yield percentage</param>
/// <param name="ScheduledStart">Scheduled start time</param>
/// <param name="ScheduledEnd">Scheduled end time</param>
/// <param name="ActualStart">Actual start time</param>
/// <param name="ActualEnd">Actual end time</param>
/// <param name="Quantities">Quantity information</param>
/// <param name="Performance">Performance metrics</param>
public record WorkOrderSummary(
    string WorkOrderId,
    string Product,
    string Status,
    decimal Progress,
    decimal Yield,
    DateTime ScheduledStart,
    DateTime ScheduledEnd,
    DateTime? ActualStart,
    DateTime? ActualEnd,
    WorkOrderQuantities Quantities,
    WorkOrderPerformance Performance
);

/// <summary>
/// Work order material requirement
/// </summary>
/// <param name="MaterialId">Material identifier</param>
/// <param name="QuantityRequired">Quantity required</param>
/// <param name="UnitOfMeasure">Unit of measure</param>
/// <param name="IsOptional">Whether material is optional</param>
public record WorkOrderMaterialRequirement(
    string MaterialId,
    decimal QuantityRequired,
    string UnitOfMeasure,
    bool IsOptional
);

/// <summary>
/// Work order note type enumeration
/// </summary>
public enum WorkOrderNoteType
{
    /// <summary>
    /// General note
    /// </summary>
    General,

    /// <summary>
    /// Quality issue note
    /// </summary>
    Quality,

    /// <summary>
    /// Production instruction
    /// </summary>
    Production,

    /// <summary>
    /// Maintenance requirement
    /// </summary>
    Maintenance,

    /// <summary>
    /// Setup instruction
    /// </summary>
    Setup,

    /// <summary>
    /// Alert or warning
    /// </summary>
    Alert
}

/// <summary>
/// Work order note record
/// </summary>
/// <param name="NoteText">Note content</param>
/// <param name="AuthorId">Note author</param>
/// <param name="NoteType">Type of note</param>
/// <param name="CreatedAt">Note timestamp</param>
public record WorkOrderNote(
    string NoteText,
    string AuthorId,
    WorkOrderNoteType NoteType,
    DateTime CreatedAt
);
