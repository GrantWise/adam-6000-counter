using Industrial.Adam.Oee.Domain.Interfaces;
using Industrial.Adam.Oee.Domain.ValueObjects;

namespace Industrial.Adam.Oee.Domain.Entities;

/// <summary>
/// Shift Aggregate Root
/// 
/// Represents a production shift with patterns, calendars, handover workflows,
/// and performance tracking. Provides foundation for shift-based reporting
/// and shift management capabilities.
/// </summary>
public sealed class Shift : Entity<string>, IAggregateRoot
{
    /// <summary>
    /// Shift identifier/name (e.g., "Day", "Night", "Weekend")
    /// (immutable)
    /// </summary>
    public string ShiftName { get; private set; }

    /// <summary>
    /// Shift pattern identifier for recurring shifts
    /// (immutable)
    /// </summary>
    public string ShiftPatternId { get; private set; }

    /// <summary>
    /// Scheduled start time
    /// (immutable)
    /// </summary>
    public DateTime ScheduledStartTime { get; private set; }

    /// <summary>
    /// Scheduled end time
    /// (immutable)
    /// </summary>
    public DateTime ScheduledEndTime { get; private set; }

    /// <summary>
    /// Actual start time (when shift actually began)
    /// </summary>
    public DateTime? ActualStartTime { get; private set; }

    /// <summary>
    /// Actual end time (when shift actually ended)
    /// </summary>
    public DateTime? ActualEndTime { get; private set; }

    /// <summary>
    /// Current shift status
    /// </summary>
    public ShiftStatus Status { get; private set; }

    /// <summary>
    /// Shift supervisor/lead operator reference
    /// (immutable)
    /// </summary>
    public CanonicalReference SupervisorReference { get; private set; }

    /// <summary>
    /// Equipment line references assigned to this shift
    /// </summary>
    private readonly List<CanonicalReference> _equipmentLineReferences = new();

    /// <summary>
    /// Operators assigned to this shift
    /// </summary>
    private readonly List<ShiftOperator> _operators = new();

    /// <summary>
    /// Work order references planned for this shift
    /// </summary>
    private readonly List<CanonicalReference> _plannedWorkOrderReferences = new();

    /// <summary>
    /// Shift handover notes and communications
    /// </summary>
    private readonly List<ShiftHandoverNote> _handoverNotes = new();

    /// <summary>
    /// Performance metrics for this shift
    /// </summary>
    private ShiftPerformanceMetrics? _performanceMetrics;

    /// <summary>
    /// Read-only access to equipment line references
    /// </summary>
    public IReadOnlyList<CanonicalReference> EquipmentLineReferences => _equipmentLineReferences.AsReadOnly();

    /// <summary>
    /// Read-only access to operators
    /// </summary>
    public IReadOnlyList<ShiftOperator> Operators => _operators.AsReadOnly();

    /// <summary>
    /// Read-only access to planned work order references
    /// </summary>
    public IReadOnlyList<CanonicalReference> PlannedWorkOrderReferences => _plannedWorkOrderReferences.AsReadOnly();

    /// <summary>
    /// Read-only access to handover notes
    /// </summary>
    public IReadOnlyList<ShiftHandoverNote> HandoverNotes => _handoverNotes.AsReadOnly();

    /// <summary>
    /// Shift performance metrics
    /// </summary>
    public ShiftPerformanceMetrics? PerformanceMetrics => _performanceMetrics;

    /// <summary>
    /// When this shift record becomes effective
    /// (immutable)
    /// </summary>
    public DateTime EffectiveFromDate { get; private set; }

    /// <summary>
    /// When this shift record becomes ineffective (null = permanent)
    /// </summary>
    public DateTime? EffectiveToDate { get; private set; }

    /// <summary>
    /// When this shift record was created
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
    private Shift() : base()
    {
        ShiftName = string.Empty;
        ShiftPatternId = string.Empty;
        SupervisorReference = CanonicalReference.ToPerson("unknown");
        Status = ShiftStatus.Planned;
    }

    /// <summary>
    /// Creates a new shift
    /// </summary>
    /// <param name="shiftId">Unique shift identifier (immutable)</param>
    /// <param name="shiftName">Shift name/identifier (immutable)</param>
    /// <param name="shiftPatternId">Shift pattern identifier (immutable)</param>
    /// <param name="scheduledStartTime">Scheduled start time (immutable)</param>
    /// <param name="scheduledEndTime">Scheduled end time (immutable)</param>
    /// <param name="supervisorReference">Shift supervisor reference (immutable)</param>
    /// <param name="effectiveFromDate">When shift becomes effective (immutable)</param>
    /// <param name="effectiveToDate">When shift becomes ineffective</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public Shift(
        string shiftId,
        string shiftName,
        string shiftPatternId,
        DateTime scheduledStartTime,
        DateTime scheduledEndTime,
        CanonicalReference supervisorReference,
        DateTime? effectiveFromDate = null,
        DateTime? effectiveToDate = null) : base(shiftId)
    {
        ValidateConstructorParameters(shiftId, shiftName, shiftPatternId, scheduledStartTime, scheduledEndTime, supervisorReference);

        ShiftName = shiftName;
        ShiftPatternId = shiftPatternId;
        ScheduledStartTime = scheduledStartTime;
        ScheduledEndTime = scheduledEndTime;
        SupervisorReference = supervisorReference;

        Status = ShiftStatus.Planned;

        EffectiveFromDate = effectiveFromDate ?? DateTime.UtcNow;
        EffectiveToDate = effectiveToDate;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Get shift duration in hours
    /// </summary>
    public decimal ScheduledDurationHours => (decimal)(ScheduledEndTime - ScheduledStartTime).TotalHours;

    /// <summary>
    /// Get actual shift duration in hours
    /// </summary>
    public decimal? ActualDurationHours
    {
        get
        {
            if (ActualStartTime == null)
                return null;
            var endTime = ActualEndTime ?? DateTime.UtcNow;
            return (decimal)(endTime - ActualStartTime.Value).TotalHours;
        }
    }

    /// <summary>
    /// Check if shift is currently effective
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
    /// Check if shift is currently active
    /// </summary>
    public bool IsActive => Status == ShiftStatus.Active;

    /// <summary>
    /// Check if shift is completed
    /// </summary>
    public bool IsCompleted => Status == ShiftStatus.Completed;

    /// <summary>
    /// Start the shift
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when shift cannot be started</exception>
    public void Start()
    {
        ShiftStateTransitions.ValidateTransition(Status, ShiftStatus.Active);

        Status = ShiftStatus.Active;
        ActualStartTime = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// End the shift
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when shift cannot be ended</exception>
    public void End()
    {
        ShiftStateTransitions.ValidateTransition(Status, ShiftStatus.Completed);

        Status = ShiftStatus.Completed;
        ActualEndTime = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        // Calculate final performance metrics
        CalculatePerformanceMetrics();
    }

    /// <summary>
    /// Cancel the shift
    /// </summary>
    /// <param name="reason">Cancellation reason</param>
    /// <exception cref="InvalidOperationException">Thrown when shift cannot be cancelled</exception>
    public void Cancel(string reason)
    {
        ShiftStateTransitions.ValidateTransition(Status, ShiftStatus.Cancelled);

        Status = ShiftStatus.Cancelled;
        if (ActualEndTime == null)
            ActualEndTime = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        AddHandoverNote($"Shift cancelled: {reason}", SupervisorReference.Id, ShiftHandoverNoteType.Alert);
    }

    /// <summary>
    /// Add operator to shift
    /// </summary>
    /// <param name="operatorReference">Operator reference</param>
    /// <param name="role">Operator role in shift</param>
    /// <param name="equipmentLineReference">Assigned equipment line reference (optional)</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public void AddOperator(CanonicalReference operatorReference, string role, CanonicalReference? equipmentLineReference = null)
    {
        if (operatorReference == null)
            throw new ArgumentNullException(nameof(operatorReference));

        if (!operatorReference.IsPerson)
            throw new ArgumentException("Reference must be to a person", nameof(operatorReference));

        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("Role is required", nameof(role));

        // Check if operator already assigned
        if (_operators.Any(o => o.OperatorReference.Id == operatorReference.Id))
            throw new InvalidOperationException($"Operator {operatorReference.Id} is already assigned to this shift");

        var shiftOperator = new ShiftOperator(
            operatorReference,
            role,
            DateTime.UtcNow,
            equipmentLineReference
        );

        _operators.Add(shiftOperator);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Remove operator from shift
    /// </summary>
    /// <param name="operatorReference">Operator reference</param>
    /// <exception cref="ArgumentException">Thrown when operator not found</exception>
    public void RemoveOperator(CanonicalReference operatorReference)
    {
        var operatorToRemove = _operators.FirstOrDefault(o => o.OperatorReference.Id == operatorReference.Id);
        if (operatorToRemove == null)
            throw new ArgumentException($"Operator {operatorReference.Id} not found in shift", nameof(operatorReference));

        _operators.Remove(operatorToRemove);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Add equipment line to shift
    /// </summary>
    /// <param name="equipmentLineReference">Equipment line reference</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public void AddEquipmentLine(CanonicalReference equipmentLineReference)
    {
        if (equipmentLineReference == null)
            throw new ArgumentNullException(nameof(equipmentLineReference));

        if (!equipmentLineReference.IsResource)
            throw new ArgumentException("Reference must be to a resource", nameof(equipmentLineReference));

        if (_equipmentLineReferences.Any(e => e.Id == equipmentLineReference.Id))
            throw new InvalidOperationException($"Equipment line {equipmentLineReference.Id} is already assigned to this shift");

        _equipmentLineReferences.Add(equipmentLineReference);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Remove equipment line from shift
    /// </summary>
    /// <param name="equipmentLineReference">Equipment line reference</param>
    /// <exception cref="ArgumentException">Thrown when equipment line not found</exception>
    public void RemoveEquipmentLine(CanonicalReference equipmentLineReference)
    {
        var equipmentToRemove = _equipmentLineReferences.FirstOrDefault(e => e.Id == equipmentLineReference.Id);
        if (equipmentToRemove == null || !_equipmentLineReferences.Remove(equipmentToRemove))
            throw new ArgumentException($"Equipment line {equipmentLineReference.Id} not found in shift", nameof(equipmentLineReference));

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Add planned work order to shift
    /// </summary>
    /// <param name="workOrderReference">Work order reference</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public void AddPlannedWorkOrder(CanonicalReference workOrderReference)
    {
        if (workOrderReference == null)
            throw new ArgumentNullException(nameof(workOrderReference));

        if (!workOrderReference.IsWorkOrder)
            throw new ArgumentException("Reference must be to a work order", nameof(workOrderReference));

        if (_plannedWorkOrderReferences.Any(w => w.Id == workOrderReference.Id))
            throw new InvalidOperationException($"Work order {workOrderReference.Id} is already planned for this shift");

        _plannedWorkOrderReferences.Add(workOrderReference);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Remove planned work order from shift
    /// </summary>
    /// <param name="workOrderReference">Work order reference</param>
    /// <exception cref="ArgumentException">Thrown when work order not found</exception>
    public void RemovePlannedWorkOrder(CanonicalReference workOrderReference)
    {
        var workOrderToRemove = _plannedWorkOrderReferences.FirstOrDefault(w => w.Id == workOrderReference.Id);
        if (workOrderToRemove == null || !_plannedWorkOrderReferences.Remove(workOrderToRemove))
            throw new ArgumentException($"Work order {workOrderReference.Id} not found in shift plan", nameof(workOrderReference));

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Add handover note
    /// </summary>
    /// <param name="noteText">Note content</param>
    /// <param name="authorId">Note author</param>
    /// <param name="noteType">Type of note</param>
    /// <param name="targetShiftId">Target shift for handover</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public void AddHandoverNote(string noteText, string authorId, ShiftHandoverNoteType noteType, string? targetShiftId = null)
    {
        if (string.IsNullOrWhiteSpace(noteText))
            throw new ArgumentException("Note text is required", nameof(noteText));

        if (string.IsNullOrWhiteSpace(authorId))
            throw new ArgumentException("Author ID is required", nameof(authorId));

        var handoverNote = new ShiftHandoverNote(
            noteText,
            authorId,
            noteType,
            DateTime.UtcNow,
            targetShiftId
        );

        _handoverNotes.Add(handoverNote);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update supervisor
    /// </summary>
    /// <param name="supervisorReference">New supervisor reference</param>
    /// <exception cref="ArgumentException">Thrown when supervisor reference is invalid</exception>
    public void UpdateSupervisor(CanonicalReference supervisorReference)
    {
        if (supervisorReference == null)
            throw new ArgumentNullException(nameof(supervisorReference));

        if (!supervisorReference.IsPerson)
            throw new ArgumentException("Reference must be to a person", nameof(supervisorReference));

        var previousSupervisor = SupervisorReference.Id;
        SupervisorReference = supervisorReference;
        UpdatedAt = DateTime.UtcNow;

        AddHandoverNote($"Supervisor changed from {previousSupervisor} to {supervisorReference.Id}", supervisorReference.Id, ShiftHandoverNoteType.Information);
    }

    /// <summary>
    /// Calculate and update performance metrics
    /// </summary>
    private void CalculatePerformanceMetrics()
    {
        // Note: In a full implementation, this would aggregate data from work orders, stoppages, and other sources
        // For now, we'll create placeholder metrics that would be calculated from actual production data

        _performanceMetrics = new ShiftPerformanceMetrics(
            PlannedProductionHours: ScheduledDurationHours,
            ActualProductionHours: ActualDurationHours ?? 0,
            TotalOeePercent: 0, // Would be calculated from work orders
            AverageAvailabilityPercent: 0, // Would be calculated from stoppages
            AveragePerformancePercent: 0, // Would be calculated from work orders
            AverageQualityPercent: 0, // Would be calculated from quality data
            WorkOrdersPlanned: _plannedWorkOrderReferences.Count,
            WorkOrdersCompleted: 0, // Would be queried from work order repository
            TotalStoppageMinutes: 0, // Would be calculated from stoppage data
            TotalUnitsProduced: 0, // Would be aggregated from work orders
            TotalUnitsDefective: 0, // Would be aggregated from quality data
            CalculatedAt: DateTime.UtcNow
        );
    }

    /// <summary>
    /// Check if shift is late starting
    /// </summary>
    /// <param name="toleranceMinutes">Tolerance in minutes</param>
    /// <returns>True if shift started late</returns>
    public bool IsLateStart(int toleranceMinutes = 15)
    {
        if (ActualStartTime == null)
            return DateTime.UtcNow > ScheduledStartTime.AddMinutes(toleranceMinutes);

        return ActualStartTime > ScheduledStartTime.AddMinutes(toleranceMinutes);
    }

    /// <summary>
    /// Check if shift is running overtime
    /// </summary>
    /// <param name="toleranceMinutes">Tolerance in minutes</param>
    /// <returns>True if shift is running overtime</returns>
    public bool IsOvertime(int toleranceMinutes = 15)
    {
        var currentTime = ActualEndTime ?? DateTime.UtcNow;
        return currentTime > ScheduledEndTime.AddMinutes(toleranceMinutes);
    }

    /// <summary>
    /// Get shift summary for reporting
    /// </summary>
    /// <returns>Shift summary</returns>
    public ShiftSummary ToSummary()
    {
        return new ShiftSummary(
            Id,
            ShiftName,
            ShiftPatternId,
            Status.ToString(),
            ScheduledStartTime,
            ScheduledEndTime,
            ActualStartTime,
            ActualEndTime,
            SupervisorReference,
            _operators.Count,
            _equipmentLineReferences.Count,
            _plannedWorkOrderReferences.Count,
            _handoverNotes.Count,
            PerformanceMetrics,
            IsLateStart(),
            IsOvertime()
        );
    }

    /// <summary>
    /// Validate constructor parameters
    /// </summary>
    private static void ValidateConstructorParameters(
        string shiftId,
        string shiftName,
        string shiftPatternId,
        DateTime scheduledStartTime,
        DateTime scheduledEndTime,
        CanonicalReference supervisorReference)
    {
        if (string.IsNullOrWhiteSpace(shiftId))
            throw new ArgumentException("Shift ID is required", nameof(shiftId));

        if (string.IsNullOrWhiteSpace(shiftName))
            throw new ArgumentException("Shift name is required", nameof(shiftName));

        if (string.IsNullOrWhiteSpace(shiftPatternId))
            throw new ArgumentException("Shift pattern ID is required", nameof(shiftPatternId));

        if (scheduledEndTime <= scheduledStartTime)
            throw new ArgumentException("Scheduled end time must be after start time", nameof(scheduledEndTime));

        if (supervisorReference == null)
            throw new ArgumentNullException(nameof(supervisorReference));

        if (!supervisorReference.IsPerson)
            throw new ArgumentException("Reference must be to a person", nameof(supervisorReference));
    }

    /// <summary>
    /// String representation of the shift
    /// </summary>
    /// <returns>Formatted string representation</returns>
    public override string ToString()
    {
        return $"Shift {ShiftName}: {ScheduledStartTime:yyyy-MM-dd HH:mm} - {ScheduledEndTime:HH:mm} ({Status})";
    }
}

/// <summary>
/// Shift status enumeration
/// </summary>
public enum ShiftStatus
{
    /// <summary>
    /// Shift is planned but not started
    /// </summary>
    Planned,

    /// <summary>
    /// Shift is currently active
    /// </summary>
    Active,

    /// <summary>
    /// Shift has been completed
    /// </summary>
    Completed,

    /// <summary>
    /// Shift has been cancelled
    /// </summary>
    Cancelled
}

/// <summary>
/// Shift handover note type enumeration
/// </summary>
public enum ShiftHandoverNoteType
{
    /// <summary>
    /// General information
    /// </summary>
    Information,

    /// <summary>
    /// Alert or warning
    /// </summary>
    Alert,

    /// <summary>
    /// Issue or problem
    /// </summary>
    Issue,

    /// <summary>
    /// Maintenance requirement
    /// </summary>
    Maintenance,

    /// <summary>
    /// Quality concern
    /// </summary>
    Quality,

    /// <summary>
    /// Production instruction
    /// </summary>
    Production
}

/// <summary>
/// Shift operator record
/// </summary>
/// <param name="OperatorReference">Operator reference (immutable)</param>
/// <param name="Role">Operator role in shift (immutable)</param>
/// <param name="AssignedAt">Assignment timestamp (immutable)</param>
/// <param name="EquipmentLineReference">Assigned equipment line reference (immutable)</param>
public record ShiftOperator(
    CanonicalReference OperatorReference,
    string Role,
    DateTime AssignedAt,
    CanonicalReference? EquipmentLineReference = null
);

/// <summary>
/// Shift handover note record
/// </summary>
/// <param name="NoteText">Note content</param>
/// <param name="AuthorId">Note author</param>
/// <param name="NoteType">Type of note</param>
/// <param name="CreatedAt">Note timestamp</param>
/// <param name="TargetShiftId">Target shift for handover</param>
public record ShiftHandoverNote(
    string NoteText,
    string AuthorId,
    ShiftHandoverNoteType NoteType,
    DateTime CreatedAt,
    string? TargetShiftId = null
);

/// <summary>
/// Shift performance metrics
/// </summary>
/// <param name="PlannedProductionHours">Planned production hours</param>
/// <param name="ActualProductionHours">Actual production hours</param>
/// <param name="TotalOeePercent">Total OEE percentage for shift</param>
/// <param name="AverageAvailabilityPercent">Average availability percentage</param>
/// <param name="AveragePerformancePercent">Average performance percentage</param>
/// <param name="AverageQualityPercent">Average quality percentage</param>
/// <param name="WorkOrdersPlanned">Number of work orders planned</param>
/// <param name="WorkOrdersCompleted">Number of work orders completed</param>
/// <param name="TotalStoppageMinutes">Total stoppage time in minutes</param>
/// <param name="TotalUnitsProduced">Total units produced during shift</param>
/// <param name="TotalUnitsDefective">Total defective units during shift</param>
/// <param name="CalculatedAt">When metrics were calculated</param>
public record ShiftPerformanceMetrics(
    decimal PlannedProductionHours,
    decimal ActualProductionHours,
    decimal TotalOeePercent,
    decimal AverageAvailabilityPercent,
    decimal AveragePerformancePercent,
    decimal AverageQualityPercent,
    int WorkOrdersPlanned,
    int WorkOrdersCompleted,
    decimal TotalStoppageMinutes,
    decimal TotalUnitsProduced,
    decimal TotalUnitsDefective,
    DateTime CalculatedAt
);

/// <summary>
/// Shift summary for reporting
/// </summary>
/// <param name="ShiftId">Shift identifier</param>
/// <param name="ShiftName">Shift name</param>
/// <param name="ShiftPatternId">Shift pattern identifier</param>
/// <param name="Status">Current status</param>
/// <param name="ScheduledStartTime">Scheduled start time</param>
/// <param name="ScheduledEndTime">Scheduled end time</param>
/// <param name="ActualStartTime">Actual start time</param>
/// <param name="ActualEndTime">Actual end time</param>
/// <param name="SupervisorReference">Shift supervisor reference</param>
/// <param name="OperatorCount">Number of assigned operators</param>
/// <param name="EquipmentLineCount">Number of assigned equipment lines</param>
/// <param name="PlannedWorkOrderCount">Number of planned work orders</param>
/// <param name="HandoverNoteCount">Number of handover notes</param>
/// <param name="PerformanceMetrics">Performance metrics</param>
/// <param name="IsLateStart">Whether shift started late</param>
/// <param name="IsOvertime">Whether shift is running overtime</param>
public record ShiftSummary(
    string ShiftId,
    string ShiftName,
    string ShiftPatternId,
    string Status,
    DateTime ScheduledStartTime,
    DateTime ScheduledEndTime,
    DateTime? ActualStartTime,
    DateTime? ActualEndTime,
    CanonicalReference SupervisorReference,
    int OperatorCount,
    int EquipmentLineCount,
    int PlannedWorkOrderCount,
    int HandoverNoteCount,
    ShiftPerformanceMetrics? PerformanceMetrics,
    bool IsLateStart,
    bool IsOvertime
);

/// <summary>
/// Shift creation data
/// </summary>
/// <param name="ShiftId">Unique shift identifier</param>
/// <param name="ShiftName">Shift name/identifier</param>
/// <param name="ShiftPatternId">Shift pattern identifier</param>
/// <param name="ScheduledStartTime">Scheduled start time</param>
/// <param name="ScheduledEndTime">Scheduled end time</param>
/// <param name="SupervisorReference">Shift supervisor reference (immutable)</param>
/// <param name="EffectiveFromDate">When shift becomes effective (immutable)</param>
/// <param name="EffectiveToDate">When shift becomes ineffective</param>
public record ShiftCreationData(
    string ShiftId,
    string ShiftName,
    string ShiftPatternId,
    DateTime ScheduledStartTime,
    DateTime ScheduledEndTime,
    CanonicalReference SupervisorReference,
    DateTime? EffectiveFromDate = null,
    DateTime? EffectiveToDate = null
);
