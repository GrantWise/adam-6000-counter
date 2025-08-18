using Industrial.Adam.Oee.Domain.Entities;

namespace Industrial.Adam.Oee.Domain.Interfaces;

/// <summary>
/// Repository interface for stoppage reason code persistence
/// </summary>
public interface IStoppageReasonRepository
{
    #region Category Operations

    /// <summary>
    /// Get a stoppage reason category by its identifier
    /// </summary>
    /// <param name="id">Category identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Category or null if not found</returns>
    public Task<StoppageReasonCategory?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a stoppage reason category by its code
    /// </summary>
    /// <param name="categoryCode">Category code (A1, A2, etc.)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Category or null if not found</returns>
    public Task<StoppageReasonCategory?> GetCategoryByCodeAsync(string categoryCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get category by matrix position
    /// </summary>
    /// <param name="matrixRow">Matrix row (1-3)</param>
    /// <param name="matrixCol">Matrix column (1-3)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Category or null if not found</returns>
    public Task<StoppageReasonCategory?> GetCategoryByMatrixPositionAsync(int matrixRow, int matrixCol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active categories arranged by matrix position
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of active categories</returns>
    public Task<IEnumerable<StoppageReasonCategory>> GetActiveCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all categories (active and inactive)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of all categories</returns>
    public Task<IEnumerable<StoppageReasonCategory>> GetAllCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new category
    /// </summary>
    /// <param name="category">Category to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created category identifier</returns>
    public Task<int> CreateCategoryAsync(StoppageReasonCategory category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing category
    /// </summary>
    /// <param name="category">Category to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if updated successfully, false if not found</returns>
    public Task<bool> UpdateCategoryAsync(StoppageReasonCategory category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a category
    /// </summary>
    /// <param name="id">Category identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully, false if not found</returns>
    public Task<bool> DeleteCategoryAsync(int id, CancellationToken cancellationToken = default);

    #endregion

    #region Subcode Operations

    /// <summary>
    /// Get a stoppage reason subcode by its identifier
    /// </summary>
    /// <param name="id">Subcode identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Subcode or null if not found</returns>
    public Task<StoppageReasonSubcode?> GetSubcodeByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get subcode by category and subcode combination
    /// </summary>
    /// <param name="categoryCode">Category code</param>
    /// <param name="subcode">Subcode within category</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Subcode or null if not found</returns>
    public Task<StoppageReasonSubcode?> GetSubcodeByCodeAsync(string categoryCode, string subcode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get subcodes for a specific category
    /// </summary>
    /// <param name="categoryId">Category identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of subcodes for the category</returns>
    public Task<IEnumerable<StoppageReasonSubcode>> GetSubcodesByCategoryAsync(int categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get subcodes for a specific category by code
    /// </summary>
    /// <param name="categoryCode">Category code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of subcodes for the category</returns>
    public Task<IEnumerable<StoppageReasonSubcode>> GetSubcodesByCategoryCodeAsync(string categoryCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active subcodes
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of active subcodes</returns>
    public Task<IEnumerable<StoppageReasonSubcode>> GetActiveSubcodesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all subcodes (active and inactive)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of all subcodes</returns>
    public Task<IEnumerable<StoppageReasonSubcode>> GetAllSubcodesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new subcode
    /// </summary>
    /// <param name="subcode">Subcode to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created subcode identifier</returns>
    public Task<int> CreateSubcodeAsync(StoppageReasonSubcode subcode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing subcode
    /// </summary>
    /// <param name="subcode">Subcode to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if updated successfully, false if not found</returns>
    public Task<bool> UpdateSubcodeAsync(StoppageReasonSubcode subcode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a subcode
    /// </summary>
    /// <param name="id">Subcode identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully, false if not found</returns>
    public Task<bool> DeleteSubcodeAsync(int id, CancellationToken cancellationToken = default);

    #endregion

    #region Combined Operations

    /// <summary>
    /// Get complete reason code hierarchy (categories with their subcodes)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of reason code combinations</returns>
    public Task<IEnumerable<ReasonCodeCombination>> GetReasonCodeHierarchyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate reason code combination exists and is active
    /// </summary>
    /// <param name="categoryCode">Category code</param>
    /// <param name="subcode">Subcode</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if valid and active, false otherwise</returns>
    public Task<bool> ValidateReasonCodeAsync(string categoryCode, string subcode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get matrix positions for categories
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of matrix positions</returns>
    public Task<IEnumerable<MatrixPosition>> GetCategoryMatrixAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if matrix position is available for category
    /// </summary>
    /// <param name="matrixRow">Matrix row</param>
    /// <param name="matrixCol">Matrix column</param>
    /// <param name="excludeCategoryId">Category ID to exclude (for updates)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if position is available</returns>
    public Task<bool> IsCategoryMatrixPositionAvailableAsync(int matrixRow, int matrixCol, int? excludeCategoryId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if matrix position is available for subcode within category
    /// </summary>
    /// <param name="categoryId">Category identifier</param>
    /// <param name="matrixRow">Matrix row</param>
    /// <param name="matrixCol">Matrix column</param>
    /// <param name="excludeSubcodeId">Subcode ID to exclude (for updates)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if position is available</returns>
    public Task<bool> IsSubcodeMatrixPositionAvailableAsync(int categoryId, int matrixRow, int matrixCol, int? excludeSubcodeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk create standard reason codes (9 categories × 9 subcodes)
    /// </summary>
    /// <param name="standardReasonCodes">Collection of standard reason codes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of reason codes created</returns>
    public Task<int> BulkCreateStandardReasonCodesAsync(IEnumerable<(StoppageReasonCategory Category, IEnumerable<StoppageReasonSubcode> Subcodes)> standardReasonCodes, CancellationToken cancellationToken = default);

    #endregion
}
