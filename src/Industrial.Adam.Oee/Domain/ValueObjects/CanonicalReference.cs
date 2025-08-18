using System.Text.Json.Serialization;

namespace Industrial.Adam.Oee.Domain.ValueObjects;

/// <summary>
/// Canonical Reference Pattern following Section 1 of Canonical Manufacturing Model
/// 
/// Represents typed references to other objects in the canonical model using the standard
/// {type: "object_type", id: "identifier"} pattern. This ensures type safety, 
/// interoperability, and compliance with manufacturing industry standards.
/// </summary>
public sealed record CanonicalReference
{
    /// <summary>
    /// Object type being referenced (e.g., "product", "work_order", "batch", "resource")
    /// (immutable)
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; }

    /// <summary>
    /// Unique identifier of the referenced object
    /// (immutable)
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; init; }

    /// <summary>
    /// Default constructor for serialization
    /// </summary>
    private CanonicalReference()
    {
        Type = string.Empty;
        Id = string.Empty;
    }

    /// <summary>
    /// Creates a new canonical reference
    /// </summary>
    /// <param name="type">Object type (immutable)</param>
    /// <param name="id">Object identifier (immutable)</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public CanonicalReference(string type, string id)
    {
        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Reference type is required", nameof(type));

        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Reference ID is required", nameof(id));

        Type = type.ToLowerInvariant(); // Normalize type to lowercase for consistency
        Id = id;
    }

    /// <summary>
    /// Creates a canonical reference to a product
    /// </summary>
    /// <param name="productId">Product identifier</param>
    /// <returns>Product reference</returns>
    public static CanonicalReference ToProduct(string productId) => new("product", productId);

    /// <summary>
    /// Creates a canonical reference to a work order
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <returns>Work order reference</returns>
    public static CanonicalReference ToWorkOrder(string workOrderId) => new("work_order", workOrderId);

    /// <summary>
    /// Creates a canonical reference to a batch/lot
    /// </summary>
    /// <param name="batchId">Batch identifier</param>
    /// <returns>Batch reference</returns>
    public static CanonicalReference ToBatch(string batchId) => new("batch", batchId);

    /// <summary>
    /// Creates a canonical reference to a resource (equipment, operator, etc.)
    /// </summary>
    /// <param name="resourceId">Resource identifier</param>
    /// <returns>Resource reference</returns>
    public static CanonicalReference ToResource(string resourceId) => new("resource", resourceId);

    /// <summary>
    /// Creates a canonical reference to a person
    /// </summary>
    /// <param name="personId">Person identifier</param>
    /// <returns>Person reference</returns>
    public static CanonicalReference ToPerson(string personId) => new("person", personId);

    /// <summary>
    /// Creates a canonical reference to a shift
    /// </summary>
    /// <param name="shiftId">Shift identifier</param>
    /// <returns>Shift reference</returns>
    public static CanonicalReference ToShift(string shiftId) => new("shift", shiftId);

    /// <summary>
    /// Creates a canonical reference to a location
    /// </summary>
    /// <param name="locationId">Location identifier</param>
    /// <returns>Location reference</returns>
    public static CanonicalReference ToLocation(string locationId) => new("location", locationId);

    /// <summary>
    /// Creates a canonical reference to a unit of measure
    /// </summary>
    /// <param name="uomCode">UOM code</param>
    /// <returns>UOM reference</returns>
    public static CanonicalReference ToUom(string uomCode) => new("uom", uomCode);

    /// <summary>
    /// Creates a canonical reference to a specification
    /// </summary>
    /// <param name="specificationId">Specification identifier</param>
    /// <returns>Specification reference</returns>
    public static CanonicalReference ToSpecification(string specificationId) => new("specification", specificationId);

    /// <summary>
    /// Creates a canonical reference to a quality inspection
    /// </summary>
    /// <param name="inspectionId">Inspection identifier</param>
    /// <returns>Quality inspection reference</returns>
    public static CanonicalReference ToQualityInspection(string inspectionId) => new("quality_inspection", inspectionId);

    /// <summary>
    /// Creates a canonical reference to a monitoring point
    /// </summary>
    /// <param name="monitoringPointId">Monitoring point identifier</param>
    /// <returns>Monitoring point reference</returns>
    public static CanonicalReference ToMonitoringPoint(string monitoringPointId) => new("monitoring_point", monitoringPointId);

    /// <summary>
    /// Creates a canonical reference to a production declaration
    /// </summary>
    /// <param name="declarationId">Production declaration identifier</param>
    /// <returns>Production declaration reference</returns>
    public static CanonicalReference ToProductionDeclaration(string declarationId) => new("production_declaration", declarationId);

    /// <summary>
    /// Creates a canonical reference to a work order operation
    /// </summary>
    /// <param name="operationId">Work order operation identifier</param>
    /// <returns>Work order operation reference</returns>
    public static CanonicalReference ToWorkOrderOperation(string operationId) => new("work_order_operation", operationId);

    /// <summary>
    /// Creates a canonical reference to a transaction log entry
    /// </summary>
    /// <param name="transactionId">Transaction log identifier</param>
    /// <returns>Transaction log reference</returns>
    public static CanonicalReference ToTransactionLog(string transactionId) => new("transaction_log", transactionId);

    /// <summary>
    /// Check if this reference is of the specified type
    /// </summary>
    /// <param name="type">Type to check</param>
    /// <returns>True if reference is of specified type</returns>
    public bool IsType(string type) => string.Equals(Type, type, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Check if this is a product reference
    /// </summary>
    public bool IsProduct => IsType("product");

    /// <summary>
    /// Check if this is a work order reference
    /// </summary>
    public bool IsWorkOrder => IsType("work_order");

    /// <summary>
    /// Check if this is a batch reference
    /// </summary>
    public bool IsBatch => IsType("batch");

    /// <summary>
    /// Check if this is a resource reference
    /// </summary>
    public bool IsResource => IsType("resource");

    /// <summary>
    /// Check if this is a person reference
    /// </summary>
    public bool IsPerson => IsType("person");

    /// <summary>
    /// String representation following canonical format
    /// </summary>
    /// <returns>Canonical reference string</returns>
    public override string ToString() => $"{Type}:{Id}";

    /// <summary>
    /// Parse a canonical reference from string
    /// </summary>
    /// <param name="referenceString">String in format "type:id"</param>
    /// <returns>Canonical reference</returns>
    /// <exception cref="ArgumentException">Thrown when format is invalid</exception>
    public static CanonicalReference Parse(string referenceString)
    {
        if (string.IsNullOrWhiteSpace(referenceString))
            throw new ArgumentException("Reference string is required", nameof(referenceString));

        var parts = referenceString.Split(':', 2);
        if (parts.Length != 2)
            throw new ArgumentException("Reference string must be in format 'type:id'", nameof(referenceString));

        return new CanonicalReference(parts[0], parts[1]);
    }

    /// <summary>
    /// Try to parse a canonical reference from string
    /// </summary>
    /// <param name="referenceString">String in format "type:id"</param>
    /// <param name="reference">Parsed reference if successful</param>
    /// <returns>True if parsing succeeded</returns>
    public static bool TryParse(string? referenceString, out CanonicalReference? reference)
    {
        reference = null;

        if (string.IsNullOrWhiteSpace(referenceString))
            return false;

        try
        {
            reference = Parse(referenceString);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Strongly-typed canonical reference for specific object types
/// </summary>
/// <typeparam name="T">Referenced object type</typeparam>
public sealed record CanonicalReference<T> where T : class
{
    /// <summary>
    /// Underlying canonical reference
    /// </summary>
    public CanonicalReference Reference { get; init; }

    /// <summary>
    /// Object type being referenced
    /// </summary>
    public string Type => Reference.Type;

    /// <summary>
    /// Object identifier
    /// </summary>
    public string Id => Reference.Id;

    /// <summary>
    /// Creates a strongly-typed canonical reference
    /// </summary>
    /// <param name="reference">Underlying canonical reference</param>
    public CanonicalReference(CanonicalReference reference)
    {
        Reference = reference ?? throw new ArgumentNullException(nameof(reference));
    }

    /// <summary>
    /// Creates a strongly-typed canonical reference
    /// </summary>
    /// <param name="type">Object type</param>
    /// <param name="id">Object identifier</param>
    public CanonicalReference(string type, string id)
    {
        Reference = new CanonicalReference(type, id);
    }

    /// <summary>
    /// Implicit conversion from underlying reference
    /// </summary>
    /// <param name="reference">Canonical reference</param>
    public static implicit operator CanonicalReference<T>(CanonicalReference reference) => new(reference);

    /// <summary>
    /// Implicit conversion to underlying reference
    /// </summary>
    /// <param name="typedReference">Typed canonical reference</param>
    public static implicit operator CanonicalReference(CanonicalReference<T> typedReference) => typedReference.Reference;

    /// <summary>
    /// String representation
    /// </summary>
    /// <returns>Reference string</returns>
    public override string ToString() => Reference.ToString();
}
