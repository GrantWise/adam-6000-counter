using Industrial.Adam.Oee.Domain.Interfaces;

namespace Industrial.Adam.Oee.Domain.Entities;

/// <summary>
/// Stoppage Reason Category Aggregate Root
/// 
/// Represents a Level 1 reason code in the 3x3 matrix classification system.
/// Nine high-level categories (A1-A3, B1-B3, C1-C3) provide the first level
/// of stoppage classification for intuitive operator selection.
/// </summary>
public sealed class StoppageReasonCategory : Entity<int>, IAggregateRoot
{
    /// <summary>
    /// Category code (A1, A2, A3, B1, B2, B3, C1, C2, C3)
    /// </summary>
    public string CategoryCode { get; private set; }

    /// <summary>
    /// Human-readable category name
    /// </summary>
    public string CategoryName { get; private set; }

    /// <summary>
    /// Detailed description of the category
    /// </summary>
    public string? CategoryDescription { get; private set; }

    /// <summary>
    /// Matrix row position (1-3)
    /// </summary>
    public int MatrixRow { get; private set; }

    /// <summary>
    /// Matrix column position (1-3)
    /// </summary>
    public int MatrixCol { get; private set; }

    /// <summary>
    /// Whether this category is active and available for selection
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// When this category was created
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Default constructor for serialization
    /// </summary>
    private StoppageReasonCategory() : base()
    {
        CategoryCode = string.Empty;
        CategoryName = string.Empty;
        IsActive = true;
    }

    /// <summary>
    /// Creates a new stoppage reason category
    /// </summary>
    /// <param name="categoryCode">Category code (A1-C3)</param>
    /// <param name="categoryName">Human-readable category name</param>
    /// <param name="matrixRow">Matrix row position (1-3)</param>
    /// <param name="matrixCol">Matrix column position (1-3)</param>
    /// <param name="categoryDescription">Optional detailed description</param>
    /// <param name="isActive">Whether category is active (default: true)</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public StoppageReasonCategory(
        string categoryCode,
        string categoryName,
        int matrixRow,
        int matrixCol,
        string? categoryDescription = null,
        bool isActive = true) : base()
    {
        ValidateConstructorParameters(categoryCode, categoryName, matrixRow, matrixCol);

        CategoryCode = categoryCode.ToUpperInvariant();
        CategoryName = categoryName;
        CategoryDescription = categoryDescription;
        MatrixRow = matrixRow;
        MatrixCol = matrixCol;
        IsActive = isActive;

        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Create category with specific ID (for repository loading)
    /// </summary>
    /// <param name="id">Database identifier</param>
    /// <param name="categoryCode">Category code</param>
    /// <param name="categoryName">Category name</param>
    /// <param name="matrixRow">Matrix row position</param>
    /// <param name="matrixCol">Matrix column position</param>
    /// <param name="categoryDescription">Category description</param>
    /// <param name="isActive">Whether category is active</param>
    /// <param name="createdAt">Creation timestamp</param>
    /// <param name="updatedAt">Last update timestamp</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public StoppageReasonCategory(
        int id,
        string categoryCode,
        string categoryName,
        int matrixRow,
        int matrixCol,
        string? categoryDescription,
        bool isActive,
        DateTime createdAt,
        DateTime updatedAt) : base(id)
    {
        ValidateConstructorParameters(categoryCode, categoryName, matrixRow, matrixCol);

        CategoryCode = categoryCode.ToUpperInvariant();
        CategoryName = categoryName;
        CategoryDescription = categoryDescription;
        MatrixRow = matrixRow;
        MatrixCol = matrixCol;
        IsActive = isActive;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    /// <summary>
    /// Update category name and description
    /// </summary>
    /// <param name="categoryName">New category name</param>
    /// <param name="categoryDescription">New category description</param>
    /// <exception cref="ArgumentException">Thrown when category name is invalid</exception>
    public void UpdateDetails(string categoryName, string? categoryDescription = null)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
            throw new ArgumentException("Category name cannot be empty", nameof(categoryName));

        CategoryName = categoryName;
        CategoryDescription = categoryDescription;
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
    /// Activate the category
    /// </summary>
    public void Activate()
    {
        if (IsActive)
            return;

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivate the category
    /// </summary>
    public void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Check if this category matches the specified matrix position
    /// </summary>
    /// <param name="matrixRow">Matrix row (1-3)</param>
    /// <param name="matrixCol">Matrix column (1-3)</param>
    /// <returns>True if matches, false otherwise</returns>
    public bool MatchesMatrixPosition(int matrixRow, int matrixCol)
    {
        return IsActive && MatrixRow == matrixRow && MatrixCol == matrixCol;
    }

    /// <summary>
    /// Get category summary for reporting
    /// </summary>
    /// <returns>Category summary</returns>
    public StoppageReasonCategorySummary ToSummary()
    {
        return new StoppageReasonCategorySummary(
            Id,
            CategoryCode,
            CategoryName,
            CategoryDescription,
            MatrixRow,
            MatrixCol,
            IsActive,
            CreatedAt,
            UpdatedAt
        );
    }

    /// <summary>
    /// Generate standard category codes for 3x3 matrix
    /// </summary>
    /// <returns>Collection of standard category codes</returns>
    public static IEnumerable<string> GetStandardCategoryCodes()
    {
        var rows = new[] { "A", "B", "C" };
        var cols = new[] { "1", "2", "3" };

        return rows.SelectMany(row => cols.Select(col => $"{row}{col}"));
    }

    /// <summary>
    /// Parse category code to matrix position
    /// </summary>
    /// <param name="categoryCode">Category code (A1-C3)</param>
    /// <returns>Matrix position</returns>
    /// <exception cref="ArgumentException">Thrown when category code is invalid</exception>
    public static (int Row, int Col) ParseCategoryCodeToMatrix(string categoryCode)
    {
        if (string.IsNullOrWhiteSpace(categoryCode) || categoryCode.Length != 2)
            throw new ArgumentException("Category code must be 2 characters (e.g., A1, B2, C3)", nameof(categoryCode));

        var upperCode = categoryCode.ToUpperInvariant();
        var rowChar = upperCode[0];
        var colChar = upperCode[1];

        var row = rowChar switch
        {
            'A' => 1,
            'B' => 2,
            'C' => 3,
            _ => throw new ArgumentException($"Invalid row character '{rowChar}'. Must be A, B, or C", nameof(categoryCode))
        };

        var col = colChar switch
        {
            '1' => 1,
            '2' => 2,
            '3' => 3,
            _ => throw new ArgumentException($"Invalid column character '{colChar}'. Must be 1, 2, or 3", nameof(categoryCode))
        };

        return (row, col);
    }

    /// <summary>
    /// Generate category code from matrix position
    /// </summary>
    /// <param name="matrixRow">Matrix row (1-3)</param>
    /// <param name="matrixCol">Matrix column (1-3)</param>
    /// <returns>Category code</returns>
    /// <exception cref="ArgumentException">Thrown when matrix position is invalid</exception>
    public static string GenerateCategoryCode(int matrixRow, int matrixCol)
    {
        ValidateMatrixPosition(matrixRow, matrixCol);

        var rowChar = matrixRow switch
        {
            1 => 'A',
            2 => 'B',
            3 => 'C',
            _ => throw new ArgumentException($"Invalid matrix row {matrixRow}. Must be 1, 2, or 3", nameof(matrixRow))
        };

        return $"{rowChar}{matrixCol}";
    }

    /// <summary>
    /// Validate constructor parameters
    /// </summary>
    private static void ValidateConstructorParameters(
        string categoryCode,
        string categoryName,
        int matrixRow,
        int matrixCol)
    {
        if (string.IsNullOrWhiteSpace(categoryCode))
            throw new ArgumentException("Category code is required", nameof(categoryCode));

        if (string.IsNullOrWhiteSpace(categoryName))
            throw new ArgumentException("Category name is required", nameof(categoryName));

        ValidateMatrixPosition(matrixRow, matrixCol);

        // Validate category code format
        var (expectedRow, expectedCol) = ParseCategoryCodeToMatrix(categoryCode);
        if (expectedRow != matrixRow || expectedCol != matrixCol)
            throw new ArgumentException(
                $"Category code '{categoryCode}' does not match matrix position ({matrixRow},{matrixCol})",
                nameof(categoryCode));
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
    /// String representation of the category
    /// </summary>
    /// <returns>Formatted string representation</returns>
    public override string ToString()
    {
        return $"Stoppage Category {CategoryCode}: {CategoryName} ({MatrixRow},{MatrixCol})";
    }
}

/// <summary>
/// Stoppage reason category creation data
/// </summary>
/// <param name="CategoryCode">Category code</param>
/// <param name="CategoryName">Category name</param>
/// <param name="MatrixRow">Matrix row position</param>
/// <param name="MatrixCol">Matrix column position</param>
/// <param name="CategoryDescription">Category description</param>
/// <param name="IsActive">Whether category is active</param>
public record StoppageReasonCategoryCreationData(
    string CategoryCode,
    string CategoryName,
    int MatrixRow,
    int MatrixCol,
    string? CategoryDescription = null,
    bool IsActive = true
);

/// <summary>
/// Stoppage reason category summary for reporting
/// </summary>
/// <param name="Id">Database identifier</param>
/// <param name="CategoryCode">Category code</param>
/// <param name="CategoryName">Category name</param>
/// <param name="CategoryDescription">Category description</param>
/// <param name="MatrixRow">Matrix row position</param>
/// <param name="MatrixCol">Matrix column position</param>
/// <param name="IsActive">Whether category is active</param>
/// <param name="CreatedAt">Creation timestamp</param>
/// <param name="UpdatedAt">Last update timestamp</param>
public record StoppageReasonCategorySummary(
    int Id,
    string CategoryCode,
    string CategoryName,
    string? CategoryDescription,
    int MatrixRow,
    int MatrixCol,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>
/// Matrix position information
/// </summary>
/// <param name="Row">Matrix row (1-3)</param>
/// <param name="Col">Matrix column (1-3)</param>
/// <param name="CategoryCode">Associated category code</param>
/// <param name="CategoryName">Associated category name</param>
public record MatrixPosition(
    int Row,
    int Col,
    string CategoryCode,
    string CategoryName
);
