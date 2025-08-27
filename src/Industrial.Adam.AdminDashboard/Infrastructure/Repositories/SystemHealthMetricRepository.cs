using Dapper;
using Industrial.Adam.AdminDashboard.Domain.Entities;
using Industrial.Adam.AdminDashboard.Domain.Enums;
using Industrial.Adam.AdminDashboard.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Industrial.Adam.AdminDashboard.Infrastructure.Repositories;

/// <summary>
/// TimescaleDB repository for system health metrics
/// </summary>
public class SystemHealthMetricRepository : ISystemHealthMetricRepository
{
    private readonly string _connectionString;
    private readonly ILogger<SystemHealthMetricRepository> _logger;

    /// <summary>
    /// Constructor for SystemHealthMetricRepository
    /// </summary>
    /// <param name="configuration">Configuration instance</param>
    /// <param name="logger">Logger instance</param>
    public SystemHealthMetricRepository(
        IConfiguration configuration,
        ILogger<SystemHealthMetricRepository> logger)
    {
        _connectionString = GetConnectionString(configuration);
        _logger = logger;
    }

    /// <summary>
    /// Add a new health metric
    /// </summary>
    /// <param name="metric">The health metric to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The added metric</returns>
    public async Task<SystemHealthMetric> AddAsync(SystemHealthMetric metric, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO system_health_metrics 
            (service_name, status, response_time_ms, error_count, recorded_at, additional_data) 
            VALUES (@ServiceName, @Status, @ResponseTimeMs, @ErrorCount, @RecordedAt, @AdditionalData::jsonb)
            RETURNING id, service_name, status, response_time_ms, error_count, recorded_at, additional_data
            """;

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var parameters = new
            {
                ServiceName = metric.ServiceName,
                Status = (int)metric.Status,
                ResponseTimeMs = metric.ResponseTimeMs,
                ErrorCount = metric.ErrorCount,
                RecordedAt = metric.RecordedAt.ToUniversalTime(),
                AdditionalData = metric.AdditionalData
            };

            var result = await connection.QuerySingleAsync<dynamic>(sql, parameters);

            _logger.LogDebug("Added health metric for service {ServiceName} with status {Status}",
                metric.ServiceName, metric.Status);

            return new SystemHealthMetric
            {
                Id = result.id,
                ServiceName = result.service_name,
                Status = (ServiceStatus)result.status,
                ResponseTimeMs = result.response_time_ms,
                ErrorCount = result.error_count,
                RecordedAt = DateTime.SpecifyKind(result.recorded_at, DateTimeKind.Utc),
                AdditionalData = result.additional_data,
                CreatedBy = "SYSTEM",
                ModifiedBy = "SYSTEM"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding health metric for service {ServiceName}", metric.ServiceName);
            throw;
        }
    }

    /// <summary>
    /// Get the latest health metrics for all services
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of latest health metrics</returns>
    public async Task<IEnumerable<SystemHealthMetric>> GetLatestMetricsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT service_name, status, response_time_ms, error_count, recorded_at, additional_data
            FROM latest_service_health
            ORDER BY service_name
            """;

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var results = await connection.QueryAsync<dynamic>(sql);

            var metrics = results.Select(result => new SystemHealthMetric
            {
                ServiceName = result.service_name,
                Status = (ServiceStatus)result.status,
                ResponseTimeMs = result.response_time_ms,
                ErrorCount = result.error_count,
                RecordedAt = DateTime.SpecifyKind(result.recorded_at, DateTimeKind.Utc),
                AdditionalData = result.additional_data,
                CreatedBy = "SYSTEM",
                ModifiedBy = "SYSTEM"
            }).ToList();

            _logger.LogDebug("Retrieved latest health metrics for {ServiceCount} services", metrics.Count);

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving latest health metrics");
            throw;
        }
    }

    /// <summary>
    /// Get health metrics for a specific service within a time range
    /// </summary>
    /// <param name="serviceName">Service name to filter by</param>
    /// <param name="fromTime">Start time range</param>
    /// <param name="toTime">End time range</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of health metrics for the service</returns>
    public async Task<IEnumerable<SystemHealthMetric>> GetServiceMetricsAsync(
        string serviceName, 
        DateTime fromTime, 
        DateTime toTime, 
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, service_name, status, response_time_ms, error_count, recorded_at, additional_data
            FROM system_health_metrics
            WHERE service_name = @ServiceName 
                AND recorded_at >= @FromTime 
                AND recorded_at <= @ToTime
            ORDER BY recorded_at DESC
            """;

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var parameters = new
            {
                ServiceName = serviceName,
                FromTime = fromTime.ToUniversalTime(),
                ToTime = toTime.ToUniversalTime()
            };

            var results = await connection.QueryAsync<dynamic>(sql, parameters);

            var metrics = results.Select(result => new SystemHealthMetric
            {
                Id = result.id,
                ServiceName = result.service_name,
                Status = (ServiceStatus)result.status,
                ResponseTimeMs = result.response_time_ms,
                ErrorCount = result.error_count,
                RecordedAt = DateTime.SpecifyKind(result.recorded_at, DateTimeKind.Utc),
                AdditionalData = result.additional_data,
                CreatedBy = "SYSTEM",
                ModifiedBy = "SYSTEM"
            }).ToList();

            _logger.LogDebug("Retrieved {MetricCount} health metrics for service {ServiceName} between {FromTime} and {ToTime}",
                metrics.Count, serviceName, fromTime, toTime);

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving health metrics for service {ServiceName}", serviceName);
            throw;
        }
    }

    /// <summary>
    /// Get services with specific status
    /// </summary>
    /// <param name="status">Status to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of services with the specified status</returns>
    public async Task<IEnumerable<SystemHealthMetric>> GetServicesByStatusAsync(
        ServiceStatus status, 
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT service_name, status, response_time_ms, error_count, recorded_at, additional_data
            FROM latest_service_health
            WHERE status = @Status
            ORDER BY service_name
            """;

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var parameters = new { Status = (int)status };

            var results = await connection.QueryAsync<dynamic>(sql, parameters);

            var metrics = results.Select(result => new SystemHealthMetric
            {
                ServiceName = result.service_name,
                Status = (ServiceStatus)result.status,
                ResponseTimeMs = result.response_time_ms,
                ErrorCount = result.error_count,
                RecordedAt = DateTime.SpecifyKind(result.recorded_at, DateTimeKind.Utc),
                AdditionalData = result.additional_data,
                CreatedBy = "SYSTEM",
                ModifiedBy = "SYSTEM"
            }).ToList();

            _logger.LogDebug("Retrieved {ServiceCount} services with status {Status}", metrics.Count, status);

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving services with status {Status}", status);
            throw;
        }
    }

    private static string GetConnectionString(IConfiguration configuration)
    {
        // Try multiple configuration keys for flexibility
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? configuration.GetConnectionString("TimescaleDB")
            ?? BuildConnectionStringFromEnvironment();

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("No TimescaleDB connection string found in configuration");
        }

        return connectionString;
    }

    private static string BuildConnectionStringFromEnvironment()
    {
        var host = Environment.GetEnvironmentVariable("TIMESCALE_HOST") ?? "localhost";
        var port = Environment.GetEnvironmentVariable("TIMESCALE_PORT") ?? "5433";
        var database = Environment.GetEnvironmentVariable("TIMESCALE_DATABASE") ?? "adam_counters";
        var username = Environment.GetEnvironmentVariable("TIMESCALE_USERNAME") ?? "industrial_system";
        var password = Environment.GetEnvironmentVariable("TIMESCALE_PASSWORD") ?? Environment.GetEnvironmentVariable("TIMESCALEDB_PASSWORD");

        if (string.IsNullOrEmpty(password))
        {
            return string.Empty;
        }

        return $"Host={host};Port={port};Database={database};Username={username};Password={password};Include Error Detail=true";
    }
}