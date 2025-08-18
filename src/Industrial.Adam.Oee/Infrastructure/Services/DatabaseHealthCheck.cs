using System.Data;
using System.Diagnostics;
using Dapper;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Oee.Infrastructure.Services;

/// <summary>
/// Enhanced database health check for OEE TimescaleDB connection and schema validation
/// Provides comprehensive health monitoring for database connectivity and performance
/// </summary>
public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IDatabaseMigrationService _migrationService;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    /// <summary>
    /// Constructor for database health check
    /// </summary>
    /// <param name="connectionFactory">Database connection factory</param>
    /// <param name="migrationService">Database migration service</param>
    /// <param name="logger">Logger instance</param>
    public DatabaseHealthCheck(
        IDbConnectionFactory connectionFactory,
        IDatabaseMigrationService migrationService,
        ILogger<DatabaseHealthCheck> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _migrationService = migrationService ?? throw new ArgumentNullException(nameof(migrationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Check database health including connectivity, schema validation, and performance
    /// </summary>
    /// <param name="context">Health check context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health check result</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var data = new Dictionary<string, object>();

        try
        {
            _logger.LogDebug("Starting database health check");

            // Test basic connectivity
            var connectionResult = await CheckConnectivityAsync(data, cancellationToken);
            if (!connectionResult.IsSuccessful)
            {
                return HealthCheckResult.Unhealthy(
                    "Database connectivity failed",
                    connectionResult.Exception,
                    data);
            }

            // Check schema validity
            var schemaResult = await CheckSchemaAsync(data, cancellationToken);
            if (!schemaResult.IsSuccessful)
            {
                return HealthCheckResult.Degraded(
                    "Database schema issues detected",
                    schemaResult.Exception,
                    data);
            }

            // Check counter_data table accessibility (READ-ONLY check)
            var counterDataResult = await CheckCounterDataAccessAsync(data, cancellationToken);
            if (!counterDataResult.IsSuccessful)
            {
                return HealthCheckResult.Unhealthy(
                    "Cannot access counter_data table from Industrial.Adam.Logger",
                    counterDataResult.Exception,
                    data);
            }

            // Performance check
            var performanceResult = await CheckPerformanceAsync(data, cancellationToken);
            if (!performanceResult.IsSuccessful)
            {
                return HealthCheckResult.Degraded(
                    "Database performance below optimal",
                    performanceResult.Exception,
                    data);
            }

            stopwatch.Stop();
            data["total_check_duration_ms"] = stopwatch.ElapsedMilliseconds;

            _logger.LogInformation("Database health check completed successfully in {Duration}ms",
                stopwatch.ElapsedMilliseconds);

            return HealthCheckResult.Healthy("Database is healthy", data);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            data["total_check_duration_ms"] = stopwatch.ElapsedMilliseconds;
            data["error"] = ex.Message;

            _logger.LogError(ex, "Database health check failed after {Duration}ms",
                stopwatch.ElapsedMilliseconds);

            return HealthCheckResult.Unhealthy(
                "Database health check failed",
                ex,
                data);
        }
    }

    /// <summary>
    /// Check basic database connectivity
    /// </summary>
    /// <param name="data">Health check data dictionary</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Check result</returns>
    private async Task<HealthCheckStepResult> CheckConnectivityAsync(
        Dictionary<string, object> data,
        CancellationToken cancellationToken)
    {
        var stepStopwatch = Stopwatch.StartNew();

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            // Simple connectivity test
            var result = await connection.QuerySingleAsync<int>("SELECT 1");

            stepStopwatch.Stop();
            data["connectivity_check_ms"] = stepStopwatch.ElapsedMilliseconds;
            data["connectivity_status"] = "connected";

            return new HealthCheckStepResult(true, null);
        }
        catch (Exception ex)
        {
            stepStopwatch.Stop();
            data["connectivity_check_ms"] = stepStopwatch.ElapsedMilliseconds;
            data["connectivity_status"] = "failed";
            data["connectivity_error"] = ex.Message;

            return new HealthCheckStepResult(false, ex);
        }
    }

    /// <summary>
    /// Check database schema validity
    /// </summary>
    /// <param name="data">Health check data dictionary</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Check result</returns>
    private async Task<HealthCheckStepResult> CheckSchemaAsync(
        Dictionary<string, object> data,
        CancellationToken cancellationToken)
    {
        var stepStopwatch = Stopwatch.StartNew();

        try
        {
            // Check if schema is current
            var isCurrent = await _migrationService.IsSchemaCurrent(cancellationToken);

            if (!isCurrent)
            {
                var pendingMigrations = await _migrationService.GetPendingMigrationsAsync(cancellationToken);
                data["pending_migrations"] = pendingMigrations.ToList();
            }

            // Validate schema
            var validation = await _migrationService.ValidateSchemaAsync(cancellationToken);

            stepStopwatch.Stop();
            data["schema_check_ms"] = stepStopwatch.ElapsedMilliseconds;
            data["schema_is_current"] = isCurrent;
            data["schema_is_valid"] = validation.IsValid;

            if (!validation.IsValid)
            {
                data["schema_issues"] = validation.Issues;
                data["missing_tables"] = validation.MissingTables;
                data["missing_indexes"] = validation.MissingIndexes;
            }

            return new HealthCheckStepResult(validation.IsValid, null);
        }
        catch (Exception ex)
        {
            stepStopwatch.Stop();
            data["schema_check_ms"] = stepStopwatch.ElapsedMilliseconds;
            data["schema_error"] = ex.Message;

            return new HealthCheckStepResult(false, ex);
        }
    }

    /// <summary>
    /// Check access to counter_data table (READ-ONLY validation)
    /// </summary>
    /// <param name="data">Health check data dictionary</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Check result</returns>
    private async Task<HealthCheckStepResult> CheckCounterDataAccessAsync(
        Dictionary<string, object> data,
        CancellationToken cancellationToken)
    {
        var stepStopwatch = Stopwatch.StartNew();

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            // Test read access to counter_data table
            const string sql = @"
                SELECT COUNT(*) as total_count,
                       COUNT(DISTINCT device_id) as device_count,
                       MAX(timestamp) as latest_timestamp
                FROM counter_data 
                WHERE timestamp >= NOW() - INTERVAL '24 hours'
                  AND channel IN (0, 1)";

            var result = await connection.QuerySingleAsync(sql);

            stepStopwatch.Stop();
            data["counter_data_check_ms"] = stepStopwatch.ElapsedMilliseconds;
            data["counter_data_accessible"] = true;
            data["counter_data_24h_count"] = result.total_count;
            data["counter_data_device_count"] = result.device_count;
            data["counter_data_latest_timestamp"] = result.latest_timestamp;

            return new HealthCheckStepResult(true, null);
        }
        catch (Exception ex)
        {
            stepStopwatch.Stop();
            data["counter_data_check_ms"] = stepStopwatch.ElapsedMilliseconds;
            data["counter_data_accessible"] = false;
            data["counter_data_error"] = ex.Message;

            return new HealthCheckStepResult(false, ex);
        }
    }

    /// <summary>
    /// Check database performance metrics
    /// </summary>
    /// <param name="data">Health check data dictionary</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Check result</returns>
    private async Task<HealthCheckStepResult> CheckPerformanceAsync(
        Dictionary<string, object> data,
        CancellationToken cancellationToken)
    {
        var stepStopwatch = Stopwatch.StartNew();

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            // Performance test query similar to OEE calculations
            var queryStopwatch = Stopwatch.StartNew();

            const string performanceTestSql = @"
                SELECT 
                    device_id,
                    COUNT(*) as reading_count,
                    AVG(rate) as avg_rate,
                    MAX(timestamp) as latest_reading
                FROM counter_data 
                WHERE timestamp >= NOW() - INTERVAL '1 hour'
                  AND channel IN (0, 1)
                GROUP BY device_id
                LIMIT 10";

            var performanceResults = await connection.QueryAsync(performanceTestSql);
            queryStopwatch.Stop();

            var queryDurationMs = queryStopwatch.ElapsedMilliseconds;
            var isPerformant = queryDurationMs < 100; // Target: sub-100ms queries

            stepStopwatch.Stop();
            data["performance_check_ms"] = stepStopwatch.ElapsedMilliseconds;
            data["sample_query_duration_ms"] = queryDurationMs;
            data["performance_target_met"] = isPerformant;
            data["performance_threshold_ms"] = 100;

            if (!isPerformant)
            {
                _logger.LogWarning("Database query performance below target: {QueryDuration}ms > 100ms",
                    queryDurationMs);
            }

            return new HealthCheckStepResult(true, null); // Performance issues are not critical failures
        }
        catch (Exception ex)
        {
            stepStopwatch.Stop();
            data["performance_check_ms"] = stepStopwatch.ElapsedMilliseconds;
            data["performance_error"] = ex.Message;

            return new HealthCheckStepResult(false, ex);
        }
    }

    /// <summary>
    /// Health check step result
    /// </summary>
    /// <param name="IsSuccessful">Whether the step was successful</param>
    /// <param name="Exception">Exception if step failed</param>
    private record HealthCheckStepResult(bool IsSuccessful, Exception? Exception);
}
