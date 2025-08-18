namespace Industrial.Adam.Oee.Infrastructure.Services;

/// <summary>
/// Database migration service interface for OEE schema management
/// Handles creation and migration of OEE-specific database objects
/// CRITICAL: This service must NOT modify existing counter_data table from Industrial.Adam.Logger
/// </summary>
public interface IDatabaseMigrationService
{
    /// <summary>
    /// Apply all pending migrations to bring database schema up to date
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of migrations applied</returns>
    public Task<int> ApplyMigrationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if database schema is up to date
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if schema is current, false if migrations are needed</returns>
    public Task<bool> IsSchemaCurrent(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of applied migrations
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of applied migration names</returns>
    public Task<IEnumerable<string>> GetAppliedMigrationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of pending migrations
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of pending migration names</returns>
    public Task<IEnumerable<string>> GetPendingMigrationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate that required tables exist and have correct structure
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with any issues found</returns>
    public Task<DatabaseValidationResult> ValidateSchemaAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Database validation result
/// </summary>
/// <param name="IsValid">Whether the database schema is valid</param>
/// <param name="Issues">List of validation issues found</param>
/// <param name="MissingTables">List of missing required tables</param>
/// <param name="MissingIndexes">List of missing required indexes</param>
public record DatabaseValidationResult(
    bool IsValid,
    IList<string> Issues,
    IList<string> MissingTables,
    IList<string> MissingIndexes
);
