namespace Industrial.Adam.Oee.Domain.ValueObjects;

/// <summary>
/// Quality Record Value Object
/// 
/// Represents a simple quality measurement for work order production.
/// Replaces complex quality inspection system with basic good/scrap tracking.
/// </summary>
public sealed class QualityRecord : ValueObject
{
    /// <summary>
    /// Work order this quality record belongs to
    /// </summary>
    public string WorkOrderId { get; private set; }

    /// <summary>
    /// Number of good pieces produced
    /// </summary>
    public int GoodCount { get; private set; }

    /// <summary>
    /// Number of scrap/defective pieces
    /// </summary>
    public int ScrapCount { get; private set; }

    /// <summary>
    /// Optional reason code for scrap (if any)
    /// </summary>
    public string? ScrapReasonCode { get; private set; }

    /// <summary>
    /// When this quality record was captured
    /// </summary>
    public DateTime RecordedAt { get; private set; }

    /// <summary>
    /// Optional notes about quality issues
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Default constructor for serialization
    /// </summary>
    private QualityRecord()
    {
        WorkOrderId = string.Empty;
        RecordedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a new quality record
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="goodCount">Number of good pieces</param>
    /// <param name="scrapCount">Number of scrap pieces</param>
    /// <param name="scrapReasonCode">Optional scrap reason code</param>
    /// <param name="notes">Optional quality notes</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public QualityRecord(
        string workOrderId,
        int goodCount,
        int scrapCount,
        string? scrapReasonCode = null,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(workOrderId))
            throw new ArgumentException("Work order ID is required", nameof(workOrderId));

        if (goodCount < 0)
            throw new ArgumentException("Good count cannot be negative", nameof(goodCount));

        if (scrapCount < 0)
            throw new ArgumentException("Scrap count cannot be negative", nameof(scrapCount));

        if (goodCount == 0 && scrapCount == 0)
            throw new ArgumentException("At least one count must be greater than zero");

        WorkOrderId = workOrderId;
        GoodCount = goodCount;
        ScrapCount = scrapCount;
        ScrapReasonCode = scrapReasonCode;
        Notes = notes;
        RecordedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Total count of all pieces (good + scrap)
    /// </summary>
    public int TotalCount => GoodCount + ScrapCount;

    /// <summary>
    /// Yield percentage (good pieces / total pieces * 100)
    /// </summary>
    public decimal YieldPercentage =>
        TotalCount == 0 ? 100m : ((decimal)GoodCount / TotalCount) * 100m;

    /// <summary>
    /// Scrap rate percentage (scrap pieces / total pieces * 100)
    /// </summary>
    public decimal ScrapRatePercentage =>
        TotalCount == 0 ? 0m : ((decimal)ScrapCount / TotalCount) * 100m;

    /// <summary>
    /// Check if this record indicates good quality (100% yield)
    /// </summary>
    public bool IsGoodQuality => ScrapCount == 0 && GoodCount > 0;

    /// <summary>
    /// Check if this record indicates quality issues (any scrap)
    /// </summary>
    public bool HasQualityIssues => ScrapCount > 0;

    /// <summary>
    /// Check if yield meets a quality threshold
    /// </summary>
    /// <param name="threshold">Quality threshold percentage (e.g., 95.0 for 95%)</param>
    /// <returns>True if yield meets or exceeds threshold</returns>
    public bool MeetsQualityThreshold(decimal threshold)
    {
        return YieldPercentage >= threshold;
    }

    /// <summary>
    /// Create a quality record from counter data
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="goodCount">Good pieces count</param>
    /// <param name="scrapCount">Scrap pieces count</param>
    /// <param name="scrapReasonCode">Optional scrap reason</param>
    /// <returns>New QualityRecord instance</returns>
    public static QualityRecord FromCounterData(
        string workOrderId,
        int goodCount,
        int scrapCount,
        string? scrapReasonCode = null)
    {
        return new QualityRecord(workOrderId, goodCount, scrapCount, scrapReasonCode);
    }

    /// <summary>
    /// Create a perfect quality record (no scrap)
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="goodCount">Good pieces count</param>
    /// <returns>QualityRecord with 100% yield</returns>
    public static QualityRecord Perfect(string workOrderId, int goodCount)
    {
        return new QualityRecord(workOrderId, goodCount, 0);
    }

    /// <summary>
    /// Create a quality record with issues
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="goodCount">Good pieces count</param>
    /// <param name="scrapCount">Scrap pieces count</param>
    /// <param name="reasonCode">Scrap reason code</param>
    /// <param name="notes">Quality issue notes</param>
    /// <returns>QualityRecord with quality issues</returns>
    public static QualityRecord WithIssues(
        string workOrderId,
        int goodCount,
        int scrapCount,
        string reasonCode,
        string? notes = null)
    {
        return new QualityRecord(workOrderId, goodCount, scrapCount, reasonCode, notes);
    }

    /// <summary>
    /// Get equality components for value object comparison
    /// </summary>
    /// <returns>Components used for equality comparison</returns>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return WorkOrderId;
        yield return GoodCount;
        yield return ScrapCount;
        yield return ScrapReasonCode;
        yield return RecordedAt;
        yield return Notes;
    }

    /// <summary>
    /// String representation of the quality record
    /// </summary>
    /// <returns>Formatted string representation</returns>
    public override string ToString()
    {
        if (HasQualityIssues)
        {
            return $"Quality Record: {GoodCount} good, {ScrapCount} scrap ({YieldPercentage:F1}% yield)" +
                   (ScrapReasonCode != null ? $" - Reason: {ScrapReasonCode}" : "");
        }

        return $"Quality Record: {GoodCount} good pieces (100% yield)";
    }
}
