using Dapper;
using Industrial.Adam.AdminDashboard.Domain.Entities;
using Industrial.Adam.AdminDashboard.Domain.Enums;
using Industrial.Adam.AdminDashboard.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Text.Json;

namespace Industrial.Adam.AdminDashboard.Infrastructure.Repositories;

/// <summary>
/// TimescaleDB repository for system alerts
/// </summary>
public class SystemAlertRepository : ISystemAlertRepository
{
    private readonly string _connectionString;
    private readonly ILogger<SystemAlertRepository> _logger;

    /// <summary>
    /// Constructor for SystemAlertRepository
    /// </summary>
    /// <param name="configuration">Configuration instance</param>
    /// <param name="logger">Logger instance</param>
    public SystemAlertRepository(
        IConfiguration configuration,
        ILogger<SystemAlertRepository> logger)
    {
        _connectionString = GetConnectionString(configuration);
        _logger = logger;
    }

    /// <summary>
    /// Add a new alert
    /// </summary>
    public async Task<SystemAlert> AddAsync(SystemAlert alert, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO system_alerts 
            (alert_type, severity, message, service_name, created_at, additional_data) 
            VALUES (@AlertType, @Severity, @Message, @ServiceName, @CreatedAt, @AdditionalData::jsonb)
            RETURNING id, alert_type, severity, message, acknowledged, acknowledged_by, 
                      acknowledged_at, created_at, service_name, additional_data
            """;

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var parameters = new
            {
                AlertType = alert.AlertType,
                Severity = (int)alert.Severity,
                Message = alert.Message,
                ServiceName = alert.ServiceName,
                CreatedAt = alert.CreatedAt.ToUniversalTime(),
                AdditionalData = alert.AdditionalData
            };

            var result = await connection.QuerySingleAsync<dynamic>(sql, parameters);

            _logger.LogDebug("Added alert {AlertType} for service {ServiceName}", alert.AlertType, alert.ServiceName);

            return MapToSystemAlert(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding alert {AlertType} for service {ServiceName}", alert.AlertType, alert.ServiceName);
            throw;
        }
    }

    /// <summary>
    /// Get an alert by ID
    /// </summary>
    public async Task<SystemAlert?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, alert_type, severity, message, acknowledged, acknowledged_by, 
                   acknowledged_at, created_at, service_name, additional_data
            FROM system_alerts 
            WHERE id = @Id
            """;

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var result = await connection.QuerySingleOrDefaultAsync<dynamic>(sql, new { Id = id });

            return result != null ? MapToSystemAlert(result) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving alert {AlertId}", id);
            throw;
        }
    }

    /// <summary>
    /// Get active (unacknowledged) alerts
    /// </summary>
    public async Task<IEnumerable<SystemAlert>> GetActiveAlertsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, alert_type, severity, message, acknowledged, acknowledged_by, 
                   acknowledged_at, created_at, service_name, additional_data
            FROM active_alerts
            ORDER BY severity DESC, created_at DESC
            """;

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var results = await connection.QueryAsync<dynamic>(sql);

            var alerts = results.Select(MapToSystemAlert).ToList();

            _logger.LogDebug("Retrieved {AlertCount} active alerts", alerts.Count);

            return alerts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active alerts");
            throw;
        }
    }

    /// <summary>
    /// Get alerts by severity level
    /// </summary>
    public async Task<IEnumerable<SystemAlert>> GetAlertsBySeverityAsync(
        AlertSeverity severity, 
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, alert_type, severity, message, acknowledged, acknowledged_by, 
                   acknowledged_at, created_at, service_name, additional_data
            FROM system_alerts 
            WHERE severity = @Severity
            ORDER BY created_at DESC
            """;

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var results = await connection.QueryAsync<dynamic>(sql, new { Severity = (int)severity });

            var alerts = results.Select(MapToSystemAlert).ToList();

            _logger.LogDebug("Retrieved {AlertCount} alerts with severity {Severity}", alerts.Count, severity);

            return alerts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving alerts with severity {Severity}", severity);
            throw;
        }
    }

    /// <summary>
    /// Acknowledge an alert
    /// </summary>
    public async Task<bool> AcknowledgeAlertAsync(
        int alertId, 
        string acknowledgedBy, 
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE system_alerts 
            SET acknowledged = TRUE, acknowledged_by = @AcknowledgedBy, acknowledged_at = @AcknowledgedAt
            WHERE id = @AlertId AND acknowledged = FALSE
            """;

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var parameters = new
            {
                AlertId = alertId,
                AcknowledgedBy = acknowledgedBy,
                AcknowledgedAt = DateTime.UtcNow
            };

            var rowsAffected = await connection.ExecuteAsync(sql, parameters);

            if (rowsAffected > 0)
            {
                _logger.LogInformation("Alert {AlertId} acknowledged by user {UserId}", alertId, acknowledgedBy);
                return true;
            }
            else
            {
                _logger.LogWarning("Alert {AlertId} not found or already acknowledged", alertId);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging alert {AlertId}", alertId);
            throw;
        }
    }

    /// <summary>
    /// Get alerts within a time range
    /// </summary>
    public async Task<IEnumerable<SystemAlert>> GetAlertsAsync(
        DateTime fromTime, 
        DateTime toTime, 
        bool? acknowledged = null, 
        CancellationToken cancellationToken = default)
    {
        var sql = """
            SELECT id, alert_type, severity, message, acknowledged, acknowledged_by, 
                   acknowledged_at, created_at, service_name, additional_data
            FROM system_alerts 
            WHERE created_at >= @FromTime AND created_at <= @ToTime
            """;

        var parameters = new Dictionary<string, object>
        {
            { "FromTime", fromTime.ToUniversalTime() },
            { "ToTime", toTime.ToUniversalTime() }
        };

        if (acknowledged.HasValue)
        {
            sql += " AND acknowledged = @Acknowledged";
            parameters.Add("Acknowledged", acknowledged.Value);
        }

        sql += " ORDER BY created_at DESC";

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var results = await connection.QueryAsync<dynamic>(sql, parameters);

            var alerts = results.Select(MapToSystemAlert).ToList();

            _logger.LogDebug("Retrieved {AlertCount} alerts between {FromTime} and {ToTime}", 
                alerts.Count, fromTime, toTime);

            return alerts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving alerts between {FromTime} and {ToTime}", fromTime, toTime);
            throw;
        }
    }

    private static SystemAlert MapToSystemAlert(dynamic result)
    {
        return new SystemAlert
        {
            Id = result.id,
            AlertType = result.alert_type,
            Severity = (AlertSeverity)result.severity,
            Message = result.message,
            Acknowledged = result.acknowledged,
            AcknowledgedBy = result.acknowledged_by,
            AcknowledgedAt = result.acknowledged_at != null 
                ? DateTime.SpecifyKind(result.acknowledged_at, DateTimeKind.Utc) 
                : null,
            CreatedAt = DateTime.SpecifyKind(result.created_at, DateTimeKind.Utc),
            ServiceName = result.service_name,
            AdditionalData = result.additional_data,
            CreatedBy = "SYSTEM",
            ModifiedBy = "SYSTEM"
        };
    }

    private static string GetConnectionString(IConfiguration configuration)
    {
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