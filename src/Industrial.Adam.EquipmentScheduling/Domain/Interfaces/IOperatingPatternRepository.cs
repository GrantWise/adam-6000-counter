using Industrial.Adam.EquipmentScheduling.Domain.Entities;
using Industrial.Adam.EquipmentScheduling.Domain.Enums;

namespace Industrial.Adam.EquipmentScheduling.Domain.Interfaces;

/// <summary>
/// Repository interface for operating pattern operations
/// </summary>
public interface IOperatingPatternRepository
{
    /// <summary>
    /// Gets an operating pattern by its identifier
    /// </summary>
    /// <param name="id">The pattern identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The operating pattern, or null if not found</returns>
    public Task<OperatingPattern?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an operating pattern by its name
    /// </summary>
    /// <param name="name">The pattern name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The operating pattern, or null if not found</returns>
    public Task<OperatingPattern?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all operating patterns of a specific type
    /// </summary>
    /// <param name="type">The pattern type</param>
    /// <param name="visibleOnly">Whether to include only visible patterns</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of operating patterns</returns>
    public Task<IEnumerable<OperatingPattern>> GetByTypeAsync(PatternType type, bool visibleOnly = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all visible operating patterns
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of visible operating patterns</returns>
    public Task<IEnumerable<OperatingPattern>> GetVisiblePatternsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets operating patterns by weekly hours range
    /// </summary>
    /// <param name="minHours">Minimum weekly hours</param>
    /// <param name="maxHours">Maximum weekly hours</param>
    /// <param name="visibleOnly">Whether to include only visible patterns</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of operating patterns in the hours range</returns>
    public Task<IEnumerable<OperatingPattern>> GetByWeeklyHoursRangeAsync(decimal minHours, decimal maxHours, bool visibleOnly = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new operating pattern
    /// </summary>
    /// <param name="pattern">The pattern to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task AddAsync(OperatingPattern pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing operating pattern
    /// </summary>
    /// <param name="pattern">The pattern to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task UpdateAsync(OperatingPattern pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a pattern name already exists
    /// </summary>
    /// <param name="name">The pattern name to check</param>
    /// <param name="excludeId">Pattern ID to exclude from the check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the name exists</returns>
    public Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);
}
