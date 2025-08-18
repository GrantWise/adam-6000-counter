using Industrial.Adam.Oee.Domain.Interfaces;

namespace Industrial.Adam.Oee.Domain.Entities;

/// <summary>
/// Equipment Stoppage Aggregate Root
/// 
/// Represents a production stoppage with automatic detection and manual classification.
/// Links to equipment lines and work orders, supporting the two-level reason code system.
/// Maintains complete audit trail for stoppage classification workflow.
/// </summary>
public sealed class EquipmentStoppage : Entity<int>, IAggregateRoot
{
    /// <summary>
    /// Equipment line identifier where stoppage occurred
    /// </summary>
    public string LineId { get; private set; }

    /// <summary>
    /// Associated work order identifier (if any)
    /// </summary>
    public string? WorkOrderId { get; private set; }

    /// <summary>
    /// When the stoppage started
    /// </summary>
    public DateTime StartTime { get; private set; }

    /// <summary>
    /// When the stoppage ended (null if still ongoing)
    /// </summary>
    public DateTime? EndTime { get; private set; }

    /// <summary>
    /// Calculated duration in minutes
    /// </summary>
    public decimal? DurationMinutes { get; private set; }

    /// <summary>
    /// Whether the stoppage has been classified by an operator
    /// </summary>
    public bool IsClassified { get; private set; }

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
    /// Who classified this stoppage
    /// </summary>
    public string? ClassifiedBy { get; private set; }

    /// <summary>
    /// When the stoppage was classified
    /// </summary>
    public DateTime? ClassifiedAt { get; private set; }

    /// <summary>
    /// Whether stoppage was automatically detected by the system
    /// </summary>
    public bool AutoDetected { get; private set; }

    /// <summary>
    /// Minimum threshold in minutes before classification is required
    /// </summary>
    public int MinimumThresholdMinutes { get; private set; }

    /// <summary>
    /// When this stoppage record was created
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Default constructor for serialization
    /// </summary>
    private EquipmentStoppage() : base()
    {
        LineId = string.Empty;
        IsClassified = false;
        AutoDetected = true;
        MinimumThresholdMinutes = 5;
    }

    /// <summary>
    /// Creates a new equipment stoppage
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="startTime">When stoppage started</param>
    /// <param name="workOrderId">Associated work order (optional)</param>
    /// <param name="autoDetected">Whether automatically detected (default: true)</param>
    /// <param name="minimumThresholdMinutes">Minimum threshold for classification (default: 5)</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public EquipmentStoppage(
        string lineId,
        DateTime startTime,
        string? workOrderId = null,
        bool autoDetected = true,
        int minimumThresholdMinutes = 5) : base()
    {
        ValidateConstructorParameters(lineId, startTime, minimumThresholdMinutes);

        LineId = lineId;
        WorkOrderId = workOrderId;
        StartTime = startTime;
        AutoDetected = autoDetected;
        MinimumThresholdMinutes = minimumThresholdMinutes;
        IsClassified = false;

        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Create stoppage with specific ID (for repository loading)
    /// </summary>
    /// <param name="id">Database identifier</param>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="startTime">Start time</param>
    /// <param name="endTime">End time</param>
    /// <param name="durationMinutes">Duration in minutes</param>
    /// <param name="isClassified">Whether classified</param>
    /// <param name="categoryCode">Category code</param>
    /// <param name="subcode">Subcode</param>
    /// <param name="operatorComments">Operator comments</param>
    /// <param name="classifiedBy">Who classified</param>
    /// <param name="classifiedAt">When classified</param>
    /// <param name="autoDetected">Whether auto detected</param>
    /// <param name="minimumThresholdMinutes">Minimum threshold</param>
    /// <param name="createdAt">Creation timestamp</param>
    /// <param name="updatedAt">Last update timestamp</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public EquipmentStoppage(
        int id,
        string lineId,
        string? workOrderId,
        DateTime startTime,
        DateTime? endTime,
        decimal? durationMinutes,
        bool isClassified,
        string? categoryCode,
        string? subcode,
        string? operatorComments,
        string? classifiedBy,
        DateTime? classifiedAt,
        bool autoDetected,
        int minimumThresholdMinutes,
        DateTime createdAt,
        DateTime updatedAt) : base(id)
    {
        ValidateConstructorParameters(lineId, startTime, minimumThresholdMinutes);

        LineId = lineId;
        WorkOrderId = workOrderId;
        StartTime = startTime;
        EndTime = endTime;
        DurationMinutes = durationMinutes;
        IsClassified = isClassified;
        CategoryCode = categoryCode;
        Subcode = subcode;
        OperatorComments = operatorComments;
        ClassifiedBy = classifiedBy;
        ClassifiedAt = classifiedAt;
        AutoDetected = autoDetected;
        MinimumThresholdMinutes = minimumThresholdMinutes;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    /// <summary>
    /// End the stoppage and calculate duration
    /// </summary>
    /// <param name="endTime">When stoppage ended (default: now)</param>
    /// <exception cref="InvalidOperationException">Thrown when stoppage is already ended</exception>
    /// <exception cref="ArgumentException">Thrown when end time is invalid</exception>
    public void EndStoppage(DateTime? endTime = null)
    {
        if (EndTime.HasValue)
            throw new InvalidOperationException("Stoppage has already been ended");

        var actualEndTime = endTime ?? DateTime.UtcNow;

        if (actualEndTime <= StartTime)
            throw new ArgumentException("End time must be after start time", nameof(endTime));

        EndTime = actualEndTime;
        DurationMinutes = (decimal)(actualEndTime - StartTime).TotalMinutes;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Classify the stoppage with reason codes
    /// </summary>
    /// <param name="categoryCode">Level 1 reason code</param>
    /// <param name="subcode">Level 2 reason code</param>
    /// <param name="classifiedBy">Who is classifying</param>
    /// <param name="operatorComments">Additional comments</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public void Classify(
        string categoryCode,
        string subcode,
        string classifiedBy,
        string? operatorComments = null)
    {
        if (string.IsNullOrWhiteSpace(categoryCode))
            throw new ArgumentException("Category code is required", nameof(categoryCode));

        if (string.IsNullOrWhiteSpace(subcode))
            throw new ArgumentException("Subcode is required", nameof(subcode));

        if (string.IsNullOrWhiteSpace(classifiedBy))
            throw new ArgumentException("Classified by is required", nameof(classifiedBy));

        CategoryCode = categoryCode.ToUpperInvariant();
        Subcode = subcode;
        ClassifiedBy = classifiedBy;
        OperatorComments = operatorComments;
        ClassifiedAt = DateTime.UtcNow;
        IsClassified = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update classification with new reason codes
    /// </summary>
    /// <param name="categoryCode">New category code</param>
    /// <param name="subcode">New subcode</param>
    /// <param name="reclassifiedBy">Who is reclassifying</param>
    /// <param name="operatorComments">Updated comments</param>
    /// <exception cref="InvalidOperationException">Thrown when stoppage is not classified</exception>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public void UpdateClassification(
        string categoryCode,
        string subcode,
        string reclassifiedBy,
        string? operatorComments = null)
    {
        if (!IsClassified)
            throw new InvalidOperationException("Stoppage must be classified before updating classification");

        if (string.IsNullOrWhiteSpace(categoryCode))
            throw new ArgumentException("Category code is required", nameof(categoryCode));

        if (string.IsNullOrWhiteSpace(subcode))
            throw new ArgumentException("Subcode is required", nameof(subcode));

        if (string.IsNullOrWhiteSpace(reclassifiedBy))
            throw new ArgumentException("Reclassified by is required", nameof(reclassifiedBy));

        CategoryCode = categoryCode.ToUpperInvariant();
        Subcode = subcode;
        ClassifiedBy = reclassifiedBy;
        OperatorComments = operatorComments;
        ClassifiedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Clear classification (unclassify)
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when stoppage is not classified</exception>
    public void ClearClassification()
    {
        if (!IsClassified)
            throw new InvalidOperationException("Stoppage is not classified");

        CategoryCode = null;
        Subcode = null;
        ClassifiedBy = null;
        OperatorComments = null;
        ClassifiedAt = null;
        IsClassified = false;
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
    /// Associate with a work order
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <exception cref="ArgumentException">Thrown when work order ID is invalid</exception>
    public void AssociateWithWorkOrder(string workOrderId)
    {
        if (string.IsNullOrWhiteSpace(workOrderId))
            throw new ArgumentException("Work order ID cannot be empty", nameof(workOrderId));

        WorkOrderId = workOrderId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Remove work order association
    /// </summary>
    public void RemoveWorkOrderAssociation()
    {
        WorkOrderId = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Check if stoppage is currently active (not ended)
    /// </summary>
    /// <returns>True if active, false if ended</returns>
    public bool IsActive => !EndTime.HasValue;

    /// <summary>
    /// Check if stoppage requires classification based on duration
    /// </summary>
    /// <returns>True if requires classification, false otherwise</returns>
    public bool RequiresClassification()
    {
        if (IsClassified)
            return false;

        var currentDuration = GetCurrentDurationMinutes();
        return currentDuration >= MinimumThresholdMinutes;
    }

    /// <summary>
    /// Get current duration in minutes
    /// </summary>
    /// <returns>Duration in minutes</returns>
    public decimal GetCurrentDurationMinutes()
    {
        if (DurationMinutes.HasValue)
            return DurationMinutes.Value;

        var endTime = EndTime ?? DateTime.UtcNow;
        return (decimal)(endTime - StartTime).TotalMinutes;
    }

    /// <summary>
    /// Get full reason code
    /// </summary>
    /// <returns>Full reason code or null if not classified</returns>
    public string? GetFullReasonCode()
    {
        if (!IsClassified || CategoryCode == null || Subcode == null)
            return null;

        return $"{CategoryCode}-{Subcode}";
    }

    /// <summary>
    /// Check if stoppage is a short stop (below threshold)
    /// </summary>
    /// <returns>True if short stop, false otherwise</returns>
    public bool IsShortStop()
    {
        return GetCurrentDurationMinutes() < MinimumThresholdMinutes;
    }

    /// <summary>
    /// Get stoppage summary for reporting
    /// </summary>
    /// <returns>Stoppage summary</returns>
    public EquipmentStoppageSummary ToSummary()
    {
        return new EquipmentStoppageSummary(
            Id,
            LineId,
            WorkOrderId,
            StartTime,
            EndTime,
            GetCurrentDurationMinutes(),
            IsClassified,
            CategoryCode,
            Subcode,
            GetFullReasonCode(),
            OperatorComments,
            ClassifiedBy,
            ClassifiedAt,
            AutoDetected,
            MinimumThresholdMinutes,
            IsActive,
            RequiresClassification(),
            IsShortStop(),
            CreatedAt,
            UpdatedAt
        );
    }

    /// <summary>
    /// Validate constructor parameters
    /// </summary>
    private static void ValidateConstructorParameters(
        string lineId,
        DateTime startTime,
        int minimumThresholdMinutes)
    {
        if (string.IsNullOrWhiteSpace(lineId))
            throw new ArgumentException("Line ID is required", nameof(lineId));

        if (startTime == default)
            throw new ArgumentException("Start time is required", nameof(startTime));

        if (minimumThresholdMinutes <= 0)
            throw new ArgumentException("Minimum threshold minutes must be positive", nameof(minimumThresholdMinutes));
    }

    /// <summary>
    /// String representation of the stoppage
    /// </summary>
    /// <returns>Formatted string representation</returns>
    public override string ToString()
    {
        var status = IsActive ? "Active" : "Ended";
        var classification = IsClassified ? $" ({GetFullReasonCode()})" : " (Unclassified)";
        return $"Stoppage {Id}: Line {LineId} - {status} ({GetCurrentDurationMinutes():F1}min){classification}";
    }
}

/// <summary>
/// Equipment stoppage creation data
/// </summary>
/// <param name="LineId">Equipment line identifier</param>
/// <param name="StartTime">Start time</param>
/// <param name="WorkOrderId">Work order identifier</param>
/// <param name="AutoDetected">Whether auto detected</param>
/// <param name="MinimumThresholdMinutes">Minimum threshold</param>
public record EquipmentStoppageCreationData(
    string LineId,
    DateTime StartTime,
    string? WorkOrderId = null,
    bool AutoDetected = true,
    int MinimumThresholdMinutes = 5
);

/// <summary>
/// Equipment stoppage summary for reporting
/// </summary>
/// <param name="Id">Database identifier</param>
/// <param name="LineId">Equipment line identifier</param>
/// <param name="WorkOrderId">Work order identifier</param>
/// <param name="StartTime">Start time</param>
/// <param name="EndTime">End time</param>
/// <param name="DurationMinutes">Duration in minutes</param>
/// <param name="IsClassified">Whether classified</param>
/// <param name="CategoryCode">Category code</param>
/// <param name="Subcode">Subcode</param>
/// <param name="FullReasonCode">Full reason code</param>
/// <param name="OperatorComments">Operator comments</param>
/// <param name="ClassifiedBy">Who classified</param>
/// <param name="ClassifiedAt">When classified</param>
/// <param name="AutoDetected">Whether auto detected</param>
/// <param name="MinimumThresholdMinutes">Minimum threshold</param>
/// <param name="IsActive">Whether currently active</param>
/// <param name="RequiresClassification">Whether requires classification</param>
/// <param name="IsShortStop">Whether is short stop</param>
/// <param name="CreatedAt">Creation timestamp</param>
/// <param name="UpdatedAt">Last update timestamp</param>
public record EquipmentStoppageSummary(
    int Id,
    string LineId,
    string? WorkOrderId,
    DateTime StartTime,
    DateTime? EndTime,
    decimal DurationMinutes,
    bool IsClassified,
    string? CategoryCode,
    string? Subcode,
    string? FullReasonCode,
    string? OperatorComments,
    string? ClassifiedBy,
    DateTime? ClassifiedAt,
    bool AutoDetected,
    int MinimumThresholdMinutes,
    bool IsActive,
    bool RequiresClassification,
    bool IsShortStop,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>
/// Stoppage classification data
/// </summary>
/// <param name="CategoryCode">Category code</param>
/// <param name="Subcode">Subcode</param>
/// <param name="ClassifiedBy">Who classified</param>
/// <param name="OperatorComments">Operator comments</param>
public record StoppageClassificationData(
    string CategoryCode,
    string Subcode,
    string ClassifiedBy,
    string? OperatorComments = null
);
