namespace Industrial.Adam.Oee.Domain.ValueObjects;

/// <summary>
/// Transaction Log Pattern following Section 17 of Canonical Manufacturing Model
/// 
/// Immutable audit trail record for all changes to manufacturing objects.
/// Essential for regulatory compliance, traceability, and forensic analysis.
/// 
/// Event Type Categories:
/// - Production Events: released, started, completed, scrapped
/// - Inventory Events: received, moved, consumed, adjusted, counted  
/// - Quality Events: inspected, held, released, rejected
/// - Maintenance Events: scheduled, started, completed
/// - Environmental Events: reading_taken, limit_exceeded, condition_restored
/// </summary>
public sealed record TransactionLog
{
    /// <summary>
    /// Unique transaction identifier (UUID)
    /// (immutable)
    /// </summary>
    public string TransactionId { get; init; }

    /// <summary>
    /// When this event occurred (UTC timestamp)
    /// (immutable)
    /// </summary>
    public DateTime EventTimestamp { get; init; }

    /// <summary>
    /// Type of event (see canonical event categories above)
    /// (immutable)
    /// </summary>
    public string EventType { get; init; }

    /// <summary>
    /// Operational category for grouping related events
    /// (immutable)
    /// </summary>
    public TransactionCategory OperationalCategory { get; init; }

    /// <summary>
    /// Reference to the object that was changed
    /// (immutable)
    /// </summary>
    public CanonicalReference ObjectReference { get; init; }

    /// <summary>
    /// List of attribute changes made in this transaction
    /// (immutable)
    /// </summary>
    public IReadOnlyList<AttributeChange> AttributeChanges { get; init; }

    /// <summary>
    /// Reference to what caused this transaction (user, system, external event)
    /// (immutable)
    /// </summary>
    public CanonicalReference? CausedByReference { get; init; }

    /// <summary>
    /// User identifier who initiated the change
    /// (immutable)
    /// </summary>
    public string UserId { get; init; }

    /// <summary>
    /// Source system that generated this transaction
    /// (immutable)
    /// </summary>
    public string SourceSystem { get; init; }

    /// <summary>
    /// Reference to transaction this reverses (for corrections/reversals)
    /// (immutable)
    /// </summary>
    public CanonicalReference? ReversalOfReference { get; init; }

    /// <summary>
    /// Additional context data as JSON
    /// (immutable)
    /// </summary>
    public string? ContextData { get; init; }

    /// <summary>
    /// Default constructor for serialization
    /// </summary>
    private TransactionLog()
    {
        TransactionId = string.Empty;
        EventType = string.Empty;
        ObjectReference = new CanonicalReference("unknown", "unknown");
        AttributeChanges = Array.Empty<AttributeChange>();
        UserId = string.Empty;
        SourceSystem = string.Empty;
    }

    /// <summary>
    /// Creates a new transaction log entry
    /// </summary>
    /// <param name="eventType">Type of event (immutable)</param>
    /// <param name="operationalCategory">Operational category (immutable)</param>
    /// <param name="objectReference">Reference to changed object (immutable)</param>
    /// <param name="attributeChanges">List of attribute changes (immutable)</param>
    /// <param name="userId">User who made the change (immutable)</param>
    /// <param name="sourceSystem">Source system (immutable)</param>
    /// <param name="causedByReference">What caused this change (immutable)</param>
    /// <param name="reversalOfReference">Transaction being reversed (immutable)</param>
    /// <param name="contextData">Additional context (immutable)</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public TransactionLog(
        string eventType,
        TransactionCategory operationalCategory,
        CanonicalReference objectReference,
        IEnumerable<AttributeChange> attributeChanges,
        string userId,
        string sourceSystem,
        CanonicalReference? causedByReference = null,
        CanonicalReference? reversalOfReference = null,
        string? contextData = null)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("Event type is required", nameof(eventType));

        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID is required", nameof(userId));

        if (string.IsNullOrWhiteSpace(sourceSystem))
            throw new ArgumentException("Source system is required", nameof(sourceSystem));

        TransactionId = Guid.NewGuid().ToString();
        EventTimestamp = DateTime.UtcNow;
        EventType = eventType;
        OperationalCategory = operationalCategory;
        ObjectReference = objectReference ?? throw new ArgumentNullException(nameof(objectReference));
        AttributeChanges = attributeChanges?.ToList().AsReadOnly() ?? Array.Empty<AttributeChange>();
        UserId = userId;
        SourceSystem = sourceSystem;
        CausedByReference = causedByReference;
        ReversalOfReference = reversalOfReference;
        ContextData = contextData;
    }

    /// <summary>
    /// Create transaction log for object creation
    /// </summary>
    /// <param name="objectReference">Created object reference</param>
    /// <param name="initialValues">Initial attribute values</param>
    /// <param name="userId">User who created the object</param>
    /// <param name="sourceSystem">Source system</param>
    /// <param name="causedByReference">What caused the creation</param>
    /// <returns>Transaction log entry</returns>
    public static TransactionLog ForCreation(
        CanonicalReference objectReference,
        Dictionary<string, object?> initialValues,
        string userId,
        string sourceSystem = "OEE-API",
        CanonicalReference? causedByReference = null)
    {
        var attributeChanges = initialValues.Select(kv =>
            new AttributeChange(kv.Key, null, kv.Value?.ToString())).ToList();

        var category = DetermineCategory(objectReference.Type);

        return new TransactionLog(
            "created",
            category,
            objectReference,
            attributeChanges,
            userId,
            sourceSystem,
            causedByReference);
    }

    /// <summary>
    /// Create transaction log for object update
    /// </summary>
    /// <param name="objectReference">Updated object reference</param>
    /// <param name="changes">Attribute changes</param>
    /// <param name="userId">User who made the update</param>
    /// <param name="sourceSystem">Source system</param>
    /// <param name="causedByReference">What caused the update</param>
    /// <returns>Transaction log entry</returns>
    public static TransactionLog ForUpdate(
        CanonicalReference objectReference,
        IEnumerable<AttributeChange> changes,
        string userId,
        string sourceSystem = "OEE-API",
        CanonicalReference? causedByReference = null)
    {
        var category = DetermineCategory(objectReference.Type);

        return new TransactionLog(
            "updated",
            category,
            objectReference,
            changes,
            userId,
            sourceSystem,
            causedByReference);
    }

    /// <summary>
    /// Create transaction log for state change
    /// </summary>
    /// <param name="objectReference">Object reference</param>
    /// <param name="eventType">Specific event type (started, completed, etc.)</param>
    /// <param name="oldState">Previous state</param>
    /// <param name="newState">New state</param>
    /// <param name="userId">User who made the change</param>
    /// <param name="sourceSystem">Source system</param>
    /// <param name="causedByReference">What caused the state change</param>
    /// <returns>Transaction log entry</returns>
    public static TransactionLog ForStateChange(
        CanonicalReference objectReference,
        string eventType,
        string oldState,
        string newState,
        string userId,
        string sourceSystem = "OEE-API",
        CanonicalReference? causedByReference = null)
    {
        var changes = new[] { new AttributeChange("status", oldState, newState) };
        var category = DetermineCategory(objectReference.Type);

        return new TransactionLog(
            eventType,
            category,
            objectReference,
            changes,
            userId,
            sourceSystem,
            causedByReference);
    }

    /// <summary>
    /// Create transaction log for reversal/correction
    /// </summary>
    /// <param name="originalTransaction">Original transaction being reversed</param>
    /// <param name="reversalChanges">Changes made in reversal</param>
    /// <param name="userId">User making the reversal</param>
    /// <param name="sourceSystem">Source system</param>
    /// <returns>Transaction log entry</returns>
    public static TransactionLog ForReversal(
        TransactionLog originalTransaction,
        IEnumerable<AttributeChange> reversalChanges,
        string userId,
        string sourceSystem = "OEE-API")
    {
        return new TransactionLog(
            "reversed",
            originalTransaction.OperationalCategory,
            originalTransaction.ObjectReference,
            reversalChanges,
            userId,
            sourceSystem,
            reversalOfReference: CanonicalReference.ToTransactionLog(originalTransaction.TransactionId));
    }

    /// <summary>
    /// Determine transaction category from object type
    /// </summary>
    /// <param name="objectType">Object type</param>
    /// <returns>Transaction category</returns>
    private static TransactionCategory DetermineCategory(string objectType)
    {
        return objectType.ToLowerInvariant() switch
        {
            "work_order" or "batch" or "production_declaration" => TransactionCategory.Production,
            "inventory_record" or "material_consumption" => TransactionCategory.Inventory,
            "quality_inspection" or "non_conformance" => TransactionCategory.Quality,
            "maintenance_work_order" or "maintenance_schedule" => TransactionCategory.Maintenance,
            "environmental_monitoring_record" or "environmental_alert" => TransactionCategory.Environmental,
            _ => TransactionCategory.Other
        };
    }

    /// <summary>
    /// Check if this transaction is a reversal
    /// </summary>
    public bool IsReversal => ReversalOfReference != null;

    /// <summary>
    /// Get summary of changes made
    /// </summary>
    /// <returns>Summary string</returns>
    public string GetChangesSummary()
    {
        if (!AttributeChanges.Any())
            return "No attribute changes";

        var changedAttributes = AttributeChanges.Select(c => c.AttributeName).ToList();
        return $"Changed: {string.Join(", ", changedAttributes)}";
    }

    /// <summary>
    /// String representation of transaction log
    /// </summary>
    /// <returns>Formatted string</returns>
    public override string ToString()
    {
        return $"Transaction {TransactionId}: {EventType} on {ObjectReference} at {EventTimestamp:yyyy-MM-dd HH:mm:ss} UTC";
    }
}

/// <summary>
/// Transaction category enumeration for grouping related events
/// </summary>
public enum TransactionCategory
{
    /// <summary>
    /// Production-related events (work orders, batches, declarations)
    /// </summary>
    Production,

    /// <summary>
    /// Inventory-related events (receiving, consumption, adjustments)
    /// </summary>
    Inventory,

    /// <summary>
    /// Quality-related events (inspections, non-conformances)
    /// </summary>
    Quality,

    /// <summary>
    /// Maintenance-related events (schedules, work orders)
    /// </summary>
    Maintenance,

    /// <summary>
    /// Environmental monitoring events (readings, alerts)
    /// </summary>
    Environmental,

    /// <summary>
    /// Other miscellaneous events
    /// </summary>
    Other
}

/// <summary>
/// Attribute change record for transaction log
/// </summary>
/// <param name="AttributeName">Name of changed attribute (immutable)</param>
/// <param name="OldValue">Previous value (immutable)</param>
/// <param name="NewValue">New value (immutable)</param>
public sealed record AttributeChange(
    string AttributeName,
    string? OldValue,
    string? NewValue
)
{
    /// <summary>
    /// Check if this represents a value change
    /// </summary>
    public bool IsValueChange => !string.Equals(OldValue, NewValue, StringComparison.Ordinal);

    /// <summary>
    /// Check if this represents a new value (creation)
    /// </summary>
    public bool IsNewValue => OldValue == null && NewValue != null;

    /// <summary>
    /// Check if this represents a value deletion
    /// </summary>
    public bool IsValueDeletion => OldValue != null && NewValue == null;

    /// <summary>
    /// String representation of attribute change
    /// </summary>
    /// <returns>Formatted change description</returns>
    public override string ToString()
    {
        return IsNewValue ? $"{AttributeName}: {NewValue}" :
               IsValueDeletion ? $"{AttributeName}: deleted" :
               $"{AttributeName}: {OldValue} → {NewValue}";
    }
}
