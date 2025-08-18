using System.Data;
using System.Reflection;
using Dapper;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Oee.Infrastructure.Services;

/// <summary>
/// Database migration service implementation for OEE schema management
/// CRITICAL: This service creates OEE-specific tables but NEVER modifies existing counter_data
/// </summary>
public sealed class DatabaseMigrationService : IDatabaseMigrationService
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<DatabaseMigrationService> _logger;

    /// <summary>
    /// Constructor for database migration service
    /// </summary>
    /// <param name="connectionFactory">Database connection factory</param>
    /// <param name="logger">Logger instance</param>
    public DatabaseMigrationService(
        IDbConnectionFactory connectionFactory,
        ILogger<DatabaseMigrationService> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Apply all pending migrations to bring database schema up to date
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of migrations applied</returns>
    public async Task<int> ApplyMigrationsAsync(CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("ApplyMigrations");

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            // Ensure migration tracking table exists
            await EnsureMigrationTableExistsAsync(connection);

            // Get pending migrations
            var pendingMigrations = await GetPendingMigrationsAsync(cancellationToken);
            var pendingList = pendingMigrations.ToList();

            if (!pendingList.Any())
            {
                _logger.LogInformation("No pending migrations to apply");
                return 0;
            }

            _logger.LogInformation("Applying {Count} pending migrations", pendingList.Count);

            var appliedCount = 0;

            foreach (var migrationName in pendingList)
            {
                await ApplyMigrationAsync(connection, migrationName);
                appliedCount++;

                _logger.LogInformation("Applied migration: {MigrationName}", migrationName);
            }

            _logger.LogInformation("Successfully applied {AppliedCount} migrations", appliedCount);
            return appliedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply database migrations");
            throw;
        }
    }

    /// <summary>
    /// Check if database schema is up to date
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if schema is current, false if migrations are needed</returns>
    public async Task<bool> IsSchemaCurrent(CancellationToken cancellationToken = default)
    {
        try
        {
            var pendingMigrations = await GetPendingMigrationsAsync(cancellationToken);
            var isCurrent = !pendingMigrations.Any();

            _logger.LogDebug("Database schema is current: {IsCurrent}", isCurrent);
            return isCurrent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if schema is current");
            throw;
        }
    }

    /// <summary>
    /// Get list of applied migrations
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of applied migration names</returns>
    public async Task<IEnumerable<string>> GetAppliedMigrationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            // Ensure migration table exists
            await EnsureMigrationTableExistsAsync(connection);

            const string sql = @"
                SELECT migration_name 
                FROM oee_migrations 
                ORDER BY applied_at ASC";

            var appliedMigrations = await connection.QueryAsync<string>(sql);
            var migrationList = appliedMigrations.ToList();

            _logger.LogDebug("Found {Count} applied migrations", migrationList.Count);
            return migrationList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get applied migrations");
            throw;
        }
    }

    /// <summary>
    /// Get list of pending migrations
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of pending migration names</returns>
    public async Task<IEnumerable<string>> GetPendingMigrationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var allMigrations = GetAvailableMigrations();
            var appliedMigrations = await GetAppliedMigrationsAsync(cancellationToken);
            var appliedSet = appliedMigrations.ToHashSet();

            var pendingMigrations = allMigrations
                .Where(migration => !appliedSet.Contains(migration))
                .OrderBy(migration => migration)
                .ToList();

            _logger.LogDebug("Found {Count} pending migrations", pendingMigrations.Count);
            return pendingMigrations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pending migrations");
            throw;
        }
    }

    /// <summary>
    /// Validate that required tables exist and have correct structure
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with any issues found</returns>
    public async Task<DatabaseValidationResult> ValidateSchemaAsync(CancellationToken cancellationToken = default)
    {
        var issues = new List<string>();
        var missingTables = new List<string>();
        var missingIndexes = new List<string>();

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            // Check required tables exist
            var requiredTables = new[]
            {
                "work_orders",
                "stoppage_classifications",
                "oee_calculations_cache",
                "device_configurations"
            };

            foreach (var tableName in requiredTables)
            {
                var tableExists = await CheckTableExistsAsync(connection, tableName);
                if (!tableExists)
                {
                    missingTables.Add(tableName);
                    issues.Add($"Required table '{tableName}' is missing");
                }
            }

            // Check critical indexes exist
            var requiredIndexes = new[]
            {
                ("work_orders", "idx_work_orders_resource_status"),
                ("counter_data", "idx_counter_data_device_timestamp_desc"),
                ("stoppage_classifications", "idx_stoppage_classifications_device_time")
            };

            foreach (var (tableName, indexName) in requiredIndexes)
            {
                var indexExists = await CheckIndexExistsAsync(connection, tableName, indexName);
                if (!indexExists)
                {
                    missingIndexes.Add($"{tableName}.{indexName}");
                    issues.Add($"Required index '{indexName}' on table '{tableName}' is missing");
                }
            }

            // Verify counter_data table exists (from Industrial.Adam.Logger)
            var counterDataExists = await CheckTableExistsAsync(connection, "counter_data");
            if (!counterDataExists)
            {
                issues.Add("CRITICAL: counter_data table from Industrial.Adam.Logger is missing");
            }

            var isValid = !issues.Any();

            _logger.LogInformation("Database schema validation completed - Valid: {IsValid}, Issues: {IssueCount}",
                isValid, issues.Count);

            return new DatabaseValidationResult(isValid, issues, missingTables, missingIndexes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate database schema");
            issues.Add($"Validation failed with error: {ex.Message}");
            return new DatabaseValidationResult(false, issues, missingTables, missingIndexes);
        }
    }

    /// <summary>
    /// Ensure the migration tracking table exists
    /// </summary>
    /// <param name="connection">Database connection</param>
    private async Task EnsureMigrationTableExistsAsync(IDbConnection connection)
    {
        const string sql = @"
            CREATE TABLE IF NOT EXISTS oee_migrations (
                migration_name VARCHAR(255) PRIMARY KEY,
                applied_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                checksum VARCHAR(64)
            );";

        await connection.ExecuteAsync(sql);
    }

    /// <summary>
    /// Apply a single migration
    /// </summary>
    /// <param name="connection">Database connection</param>
    /// <param name="migrationName">Name of migration to apply</param>
    private async Task ApplyMigrationAsync(IDbConnection connection, string migrationName)
    {
        var migrationSql = GetMigrationSql(migrationName);

        using var transaction = connection.BeginTransaction();

        try
        {
            // Execute migration SQL
            await connection.ExecuteAsync(migrationSql, transaction: transaction);

            // Record migration as applied
            const string recordSql = @"
                INSERT INTO oee_migrations (migration_name, applied_at) 
                VALUES (@migrationName, NOW())";

            await connection.ExecuteAsync(recordSql, new { migrationName }, transaction);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Get available migration names from embedded resources
    /// </summary>
    /// <returns>List of available migration names</returns>
    private static IEnumerable<string> GetAvailableMigrations()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var migrationPrefix = "Industrial.Adam.Oee.Infrastructure.Data.Migrations.";

        return assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(migrationPrefix) && name.EndsWith(".sql"))
            .Select(name => name.Substring(migrationPrefix.Length, name.Length - migrationPrefix.Length - 4))
            .OrderBy(name => name)
            .ToList();
    }

    /// <summary>
    /// Get migration SQL content from embedded resource
    /// </summary>
    /// <param name="migrationName">Migration name</param>
    /// <returns>Migration SQL content</returns>
    private static string GetMigrationSql(string migrationName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"Industrial.Adam.Oee.Infrastructure.Data.Migrations.{migrationName}.sql";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new InvalidOperationException($"Migration resource not found: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Check if a table exists in the database
    /// </summary>
    /// <param name="connection">Database connection</param>
    /// <param name="tableName">Table name to check</param>
    /// <returns>True if table exists</returns>
    private static async Task<bool> CheckTableExistsAsync(IDbConnection connection, string tableName)
    {
        const string sql = @"
            SELECT EXISTS (
                SELECT FROM information_schema.tables 
                WHERE table_schema = 'public' 
                AND table_name = @tableName
            )";

        return await connection.QuerySingleAsync<bool>(sql, new { tableName });
    }

    /// <summary>
    /// Check if an index exists on a table
    /// </summary>
    /// <param name="connection">Database connection</param>
    /// <param name="tableName">Table name</param>
    /// <param name="indexName">Index name</param>
    /// <returns>True if index exists</returns>
    private static async Task<bool> CheckIndexExistsAsync(IDbConnection connection, string tableName, string indexName)
    {
        const string sql = @"
            SELECT EXISTS (
                SELECT FROM pg_indexes 
                WHERE schemaname = 'public' 
                AND tablename = @tableName 
                AND indexname = @indexName
            )";

        return await connection.QuerySingleAsync<bool>(sql, new { tableName, indexName });
    }

    private static readonly System.Diagnostics.ActivitySource ActivitySource = new("Industrial.Adam.Oee.Infrastructure");
}
