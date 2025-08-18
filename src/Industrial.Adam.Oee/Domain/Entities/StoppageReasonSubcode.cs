using Industrial.Adam.Oee.Domain.Interfaces;

namespace Industrial.Adam.Oee.Domain.Entities;

/// <summary>
/// Stoppage Reason Subcode Aggregate Root
/// 
/// Represents a Level 2 reason code within a specific category.
/// Each category has 9 subcodes arranged in a 3x3 matrix for detailed classification.
/// Provides the second level of granular stoppage classification.
/// </summary>
public sealed class StoppageReasonSubcode : Entity<int>, IAggregateRoot
{
    /// <summary>
    /// Parent category identifier
    /// </summary>
    public int CategoryId { get; private set; }

    /// <summary>
    /// Parent category code (A1, A2, A3, etc.)
    /// </summary>
    public string CategoryCode { get; private set; }

    /// <summary>
    /// Subcode within the category (1-9)
    /// </summary>
    public string Subcode { get; private set; }

    /// <summary>
    /// Human-readable subcode name
    /// </summary>
    public string SubcodeName { get; private set; }

    /// <summary>
    /// Detailed description of the subcode
    /// </summary>
    public string? SubcodeDescription { get; private set; }

    /// <summary>
    /// Matrix row position within category (1-3)
    /// </summary>
    public int MatrixRow { get; private set; }

    /// <summary>
    /// Matrix column position within category (1-3)
    /// </summary>
    public int MatrixCol { get; private set; }

    /// <summary>
    /// Whether this subcode is active and available for selection
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// When this subcode was created
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Default constructor for serialization
    /// </summary>
    private StoppageReasonSubcode() : base()
    {
        CategoryCode = string.Empty;
        Subcode = string.Empty;
        SubcodeName = string.Empty;
        IsActive = true;
    }

    /// <summary>
    /// Creates a new stoppage reason subcode
    /// </summary>
    /// <param name="categoryId">Parent category identifier</param>
    /// <param name="categoryCode">Parent category code</param>
    /// <param name="subcode">Subcode within category (1-9)</param>
    /// <param name="subcodeName">Human-readable subcode name</param>
    /// <param name="matrixRow">Matrix row position (1-3)</param>
    /// <param name="matrixCol">Matrix column position (1-3)</param>
    /// <param name="subcodeDescription">Optional detailed description</param>
    /// <param name="isActive">Whether subcode is active (default: true)</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public StoppageReasonSubcode(
        int categoryId,
        string categoryCode,
        string subcode,
        string subcodeName,
        int matrixRow,
        int matrixCol,
        string? subcodeDescription = null,
        bool isActive = true) : base()
    {
        ValidateConstructorParameters(categoryId, categoryCode, subcode, subcodeName, matrixRow, matrixCol);

        CategoryId = categoryId;
        CategoryCode = categoryCode.ToUpperInvariant();
        Subcode = subcode;
        SubcodeName = subcodeName;
        SubcodeDescription = subcodeDescription;
        MatrixRow = matrixRow;
        MatrixCol = matrixCol;
        IsActive = isActive;

        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Create subcode with specific ID (for repository loading)
    /// </summary>
    /// <param name="id">Database identifier</param>
    /// <param name="categoryId">Parent category identifier</param>
    /// <param name="categoryCode">Parent category code</param>
    /// <param name="subcode">Subcode</param>
    /// <param name="subcodeName">Subcode name</param>
    /// <param name="matrixRow">Matrix row position</param>
    /// <param name="matrixCol">Matrix column position</param>
    /// <param name="subcodeDescription">Subcode description</param>
    /// <param name="isActive">Whether subcode is active</param>
    /// <param name="createdAt">Creation timestamp</param>
    /// <param name="updatedAt">Last update timestamp</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public StoppageReasonSubcode(
        int id,
        int categoryId,
        string categoryCode,
        string subcode,
        string subcodeName,
        int matrixRow,
        int matrixCol,
        string? subcodeDescription,
        bool isActive,
        DateTime createdAt,
        DateTime updatedAt) : base(id)
    {
        ValidateConstructorParameters(categoryId, categoryCode, subcode, subcodeName, matrixRow, matrixCol);

        CategoryId = categoryId;
        CategoryCode = categoryCode.ToUpperInvariant();
        Subcode = subcode;
        SubcodeName = subcodeName;
        SubcodeDescription = subcodeDescription;
        MatrixRow = matrixRow;
        MatrixCol = matrixCol;
        IsActive = isActive;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    /// <summary>
    /// Update subcode name and description
    /// </summary>
    /// <param name="subcodeName">New subcode name</param>
    /// <param name="subcodeDescription">New subcode description</param>
    /// <exception cref="ArgumentException">Thrown when subcode name is invalid</exception>
    public void UpdateDetails(string subcodeName, string? subcodeDescription = null)
    {
        if (string.IsNullOrWhiteSpace(subcodeName))
            throw new ArgumentException("Subcode name cannot be empty", nameof(subcodeName));

        SubcodeName = subcodeName;
        SubcodeDescription = subcodeDescription;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update matrix position
    /// </summary>
    /// <param name="matrixRow">New matrix row (1-3)</param>
    /// <param name="matrixCol">New matrix column (1-3)</param>
    /// <exception cref="ArgumentException">Thrown when matrix position is invalid</exception>
    public void UpdateMatrixPosition(int matrixRow, int matrixCol)
    {
        ValidateMatrixPosition(matrixRow, matrixCol);

        MatrixRow = matrixRow;
        MatrixCol = matrixCol;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activate the subcode
    /// </summary>
    public void Activate()
    {
        if (IsActive)
            return;

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivate the subcode
    /// </summary>
    public void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Check if this subcode matches the specified category and subcode
    /// </summary>
    /// <param name="categoryCode">Category code</param>
    /// <param name="subcode">Subcode</param>
    /// <returns>True if matches, false otherwise</returns>
    public bool MatchesCode(string categoryCode, string subcode)
    {
        return IsActive &&
               string.Equals(CategoryCode, categoryCode, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(Subcode, subcode, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Check if this subcode matches the specified matrix position within its category
    /// </summary>
    /// <param name="matrixRow">Matrix row (1-3)</param>
    /// <param name="matrixCol">Matrix column (1-3)</param>
    /// <returns>True if matches, false otherwise</returns>
    public bool MatchesMatrixPosition(int matrixRow, int matrixCol)
    {
        return IsActive && MatrixRow == matrixRow && MatrixCol == matrixCol;
    }

    /// <summary>
    /// Get full reason code combining category and subcode
    /// </summary>
    /// <returns>Full reason code (e.g., "A1-3")</returns>
    public string GetFullReasonCode()
    {
        return $"{CategoryCode}-{Subcode}";
    }

    /// <summary>
    /// Get subcode summary for reporting
    /// </summary>
    /// <returns>Subcode summary</returns>
    public StoppageReasonSubcodeSummary ToSummary()
    {
        return new StoppageReasonSubcodeSummary(
            Id,
            CategoryId,
            CategoryCode,
            Subcode,
            SubcodeName,
            SubcodeDescription,
            MatrixRow,
            MatrixCol,
            IsActive,
            GetFullReasonCode(),
            CreatedAt,
            UpdatedAt
        );
    }

    /// <summary>
    /// Generate standard subcodes for a category (1-9 in 3x3 matrix)
    /// </summary>
    /// <returns>Collection of standard subcodes</returns>
    public static IEnumerable<(string Subcode, int Row, int Col)> GetStandardSubcodes()
    {
        var subcodes = new List<(string, int, int)>();
        var subcodeNumber = 1;

        for (int row = 1; row <= 3; row++)
        {
            for (int col = 1; col <= 3; col++)
            {
                subcodes.Add((subcodeNumber.ToString(), row, col));
                subcodeNumber++;
            }
        }

        return subcodes;
    }

    /// <summary>
    /// Parse matrix position to subcode number
    /// </summary>
    /// <param name="matrixRow">Matrix row (1-3)</param>
    /// <param name="matrixCol">Matrix column (1-3)</param>
    /// <returns>Subcode number (1-9)</returns>
    /// <exception cref="ArgumentException">Thrown when matrix position is invalid</exception>
    public static int MatrixPositionToSubcodeNumber(int matrixRow, int matrixCol)
    {
        ValidateMatrixPosition(matrixRow, matrixCol);
        return ((matrixRow - 1) * 3) + matrixCol;
    }

    /// <summary>
    /// Parse subcode number to matrix position
    /// </summary>
    /// <param name="subcodeNumber">Subcode number (1-9)</param>
    /// <returns>Matrix position</returns>
    /// <exception cref="ArgumentException">Thrown when subcode number is invalid</exception>
    public static (int Row, int Col) SubcodeNumberToMatrixPosition(int subcodeNumber)
    {
        if (subcodeNumber < 1 || subcodeNumber > 9)
            throw new ArgumentException("Subcode number must be between 1 and 9", nameof(subcodeNumber));

        var row = ((subcodeNumber - 1) / 3) + 1;
        var col = ((subcodeNumber - 1) % 3) + 1;
        return (row, col);
    }

    /// <summary>
    /// Validate constructor parameters
    /// </summary>
    private static void ValidateConstructorParameters(
        int categoryId,
        string categoryCode,
        string subcode,
        string subcodeName,
        int matrixRow,
        int matrixCol)
    {
        if (categoryId <= 0)
            throw new ArgumentException("Category ID must be positive", nameof(categoryId));

        if (string.IsNullOrWhiteSpace(categoryCode))
            throw new ArgumentException("Category code is required", nameof(categoryCode));

        if (string.IsNullOrWhiteSpace(subcode))
            throw new ArgumentException("Subcode is required", nameof(subcode));

        if (string.IsNullOrWhiteSpace(subcodeName))
            throw new ArgumentException("Subcode name is required", nameof(subcodeName));

        ValidateMatrixPosition(matrixRow, matrixCol);

        // Validate subcode format (should be 1-9)
        if (!int.TryParse(subcode, out var subcodeNumber) || subcodeNumber < 1 || subcodeNumber > 9)
            throw new ArgumentException("Subcode must be a number between 1 and 9", nameof(subcode));

        // Validate matrix position matches subcode number
        var (expectedRow, expectedCol) = SubcodeNumberToMatrixPosition(subcodeNumber);
        if (expectedRow != matrixRow || expectedCol != matrixCol)
            throw new ArgumentException(
                $"Subcode '{subcode}' does not match matrix position ({matrixRow},{matrixCol})",
                nameof(subcode));
    }

    /// <summary>
    /// Validate matrix position
    /// </summary>
    private static void ValidateMatrixPosition(int matrixRow, int matrixCol)
    {
        if (matrixRow < 1 || matrixRow > 3)
            throw new ArgumentException("Matrix row must be between 1 and 3", nameof(matrixRow));

        if (matrixCol < 1 || matrixCol > 3)
            throw new ArgumentException("Matrix column must be between 1 and 3", nameof(matrixCol));
    }

    /// <summary>
    /// String representation of the subcode
    /// </summary>
    /// <returns>Formatted string representation</returns>
    public override string ToString()
    {
        return $"Stoppage Subcode {GetFullReasonCode()}: {SubcodeName} ({MatrixRow},{MatrixCol})";
    }
}

/// <summary>
/// Stoppage reason subcode creation data
/// </summary>
/// <param name="CategoryId">Parent category identifier</param>
/// <param name="CategoryCode">Parent category code</param>
/// <param name="Subcode">Subcode</param>
/// <param name="SubcodeName">Subcode name</param>
/// <param name="MatrixRow">Matrix row position</param>
/// <param name="MatrixCol">Matrix column position</param>
/// <param name="SubcodeDescription">Subcode description</param>
/// <param name="IsActive">Whether subcode is active</param>
public record StoppageReasonSubcodeCreationData(
    int CategoryId,
    string CategoryCode,
    string Subcode,
    string SubcodeName,
    int MatrixRow,
    int MatrixCol,
    string? SubcodeDescription = null,
    bool IsActive = true
);

/// <summary>
/// Stoppage reason subcode summary for reporting
/// </summary>
/// <param name="Id">Database identifier</param>
/// <param name="CategoryId">Parent category identifier</param>
/// <param name="CategoryCode">Parent category code</param>
/// <param name="Subcode">Subcode</param>
/// <param name="SubcodeName">Subcode name</param>
/// <param name="SubcodeDescription">Subcode description</param>
/// <param name="MatrixRow">Matrix row position</param>
/// <param name="MatrixCol">Matrix column position</param>
/// <param name="IsActive">Whether subcode is active</param>
/// <param name="FullReasonCode">Full reason code</param>
/// <param name="CreatedAt">Creation timestamp</param>
/// <param name="UpdatedAt">Last update timestamp</param>
public record StoppageReasonSubcodeSummary(
    int Id,
    int CategoryId,
    string CategoryCode,
    string Subcode,
    string SubcodeName,
    string? SubcodeDescription,
    int MatrixRow,
    int MatrixCol,
    bool IsActive,
    string FullReasonCode,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>
/// Reason code combination for classification
/// </summary>
/// <param name="CategoryCode">Category code</param>
/// <param name="CategoryName">Category name</param>
/// <param name="Subcode">Subcode</param>
/// <param name="SubcodeName">Subcode name</param>
/// <param name="FullCode">Full reason code</param>
/// <param name="Description">Combined description</param>
public record ReasonCodeCombination(
    string CategoryCode,
    string CategoryName,
    string Subcode,
    string SubcodeName,
    string FullCode,
    string Description
);
