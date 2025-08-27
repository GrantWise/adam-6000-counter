using Dapper;
using Industrial.Adam.Security.Infrastructure.Repositories;
using Industrial.Adam.Security.Domain.Entities;
using Industrial.Adam.Security.Application.DTOs;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Text.Json;

namespace Industrial.Adam.Security.Infrastructure.Services;

/// <summary>
/// Service for managing system configuration and feature flags
/// </summary>
public class ConfigurationManagementService
{
    private readonly DatabaseConnectionFactory _connectionFactory;
    private readonly AuditLogRepository _auditRepository;
    private readonly ILogger<ConfigurationManagementService> _logger;

    public ConfigurationManagementService(
        DatabaseConnectionFactory connectionFactory,
        AuditLogRepository auditRepository,
        ILogger<ConfigurationManagementService> logger)
    {
        _connectionFactory = connectionFactory;
        _auditRepository = auditRepository;
        _logger = logger;
    }

    /// <summary>
    /// Gets all system configuration settings
    /// </summary>
    public async Task<List<SystemConfiguration>> GetAllConfigurationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            
            const string sql = """
                SELECT key, value, category, description, updated_by as updatedby, updated_at as updatedat
                FROM system_configuration
                ORDER BY category, key
                """;

            var configs = await connection.QueryAsync<SystemConfiguration>(sql);
            return configs.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get system configurations");
            throw;
        }
    }

    /// <summary>
    /// Gets configuration settings by category
    /// </summary>
    public async Task<List<SystemConfiguration>> GetConfigurationsByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            
            const string sql = """
                SELECT key, value, category, description, updated_by as updatedby, updated_at as updatedat
                FROM system_configuration
                WHERE category = @Category
                ORDER BY key
                """;

            var configs = await connection.QueryAsync<SystemConfiguration>(sql, new { Category = category });
            return configs.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get configurations for category {Category}", category);
            throw;
        }
    }

    /// <summary>
    /// Updates system configuration settings
    /// </summary>
    public async Task<bool> UpdateConfigurationsAsync(Dictionary<string, object> configurations, ClaimsPrincipal user, 
        string ipAddress, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            using var transaction = connection.BeginTransaction();

            try
            {
                foreach (var config in configurations)
                {
                    // Get old value for audit trail
                    var oldValue = await connection.QuerySingleOrDefaultAsync<string>(
                        "SELECT value FROM system_configuration WHERE key = @Key", 
                        new { Key = config.Key }, transaction);

                    // Upsert configuration
                    const string upsertSql = """
                        INSERT INTO system_configuration (key, value, category, description, updated_by, updated_at)
                        VALUES (@Key, @Value, @Category, @Description, @UpdatedBy, @UpdatedAt)
                        ON CONFLICT (key) 
                        DO UPDATE SET 
                            value = @Value,
                            updated_by = @UpdatedBy,
                            updated_at = @UpdatedAt
                        """;

                    var valueJson = JsonSerializer.Serialize(config.Value);
                    
                    await connection.ExecuteAsync(upsertSql, new
                    {
                        Key = config.Key,
                        Value = valueJson,
                        Category = "System", // Default category
                        Description = $"Configuration setting {config.Key}",
                        UpdatedBy = userId,
                        UpdatedAt = DateTimeOffset.UtcNow
                    }, transaction);

                    // Log audit trail
                    await _auditRepository.LogAuditTrailAsync(
                        userId,
                        "UpdateConfiguration",
                        "SystemConfiguration",
                        config.Key,
                        oldValue != null ? JsonSerializer.Deserialize<object>(oldValue) : null,
                        config.Value,
                        ipAddress,
                        cancellationToken);
                }

                transaction.Commit();
                _logger.LogInformation("Updated {Count} configuration settings by user {UserId}", configurations.Count, userId);
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update configurations");
            return false;
        }
    }

    /// <summary>
    /// Gets all feature flags
    /// </summary>
    public async Task<List<FeatureFlag>> GetAllFeatureFlagsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            
            const string sql = """
                SELECT name, enabled, description, conditions, updated_at as updatedat
                FROM feature_flags
                ORDER BY name
                """;

            var flags = await connection.QueryAsync<FeatureFlag>(sql);
            return flags.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get feature flags");
            throw;
        }
    }

    /// <summary>
    /// Updates feature flags
    /// </summary>
    public async Task<bool> UpdateFeatureFlagsAsync(Dictionary<string, bool> flags, ClaimsPrincipal user, 
        string ipAddress, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            using var transaction = connection.BeginTransaction();

            try
            {
                foreach (var flag in flags)
                {
                    // Get old value for audit trail
                    var oldValue = await connection.QuerySingleOrDefaultAsync<bool?>(
                        "SELECT enabled FROM feature_flags WHERE name = @Name", 
                        new { Name = flag.Key }, transaction);

                    // Upsert feature flag
                    const string upsertSql = """
                        INSERT INTO feature_flags (name, enabled, description, conditions, updated_at)
                        VALUES (@Name, @Enabled, @Description, @Conditions, @UpdatedAt)
                        ON CONFLICT (name) 
                        DO UPDATE SET 
                            enabled = @Enabled,
                            updated_at = @UpdatedAt
                        """;

                    await connection.ExecuteAsync(upsertSql, new
                    {
                        Name = flag.Key,
                        Enabled = flag.Value,
                        Description = $"Feature flag {flag.Key}",
                        Conditions = (string?)null,
                        UpdatedAt = DateTimeOffset.UtcNow
                    }, transaction);

                    // Log audit trail
                    await _auditRepository.LogAuditTrailAsync(
                        userId,
                        "UpdateFeatureFlag",
                        "FeatureFlag",
                        flag.Key,
                        oldValue,
                        flag.Value,
                        ipAddress,
                        cancellationToken);
                }

                transaction.Commit();
                _logger.LogInformation("Updated {Count} feature flags by user {UserId}", flags.Count, userId);
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update feature flags");
            return false;
        }
    }

    /// <summary>
    /// Schedules maintenance window
    /// </summary>
    public async Task<bool> ScheduleMaintenanceAsync(MaintenanceScheduleRequest request, ClaimsPrincipal user, 
        string ipAddress, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Log the maintenance schedule request
            await _auditRepository.LogAuditTrailAsync(
                userId,
                "ScheduleMaintenance",
                "MaintenanceWindow",
                request.Id,
                null,
                new
                {
                    StartTime = request.StartTime,
                    EndTime = request.EndTime,
                    Description = request.Description,
                    NotifyUsers = request.NotifyUsers
                },
                ipAddress,
                cancellationToken);

            await _auditRepository.LogUserActivityAsync(
                userId,
                "ScheduleMaintenance",
                $"Scheduled maintenance window: {request.Description}",
                ipAddress,
                "Admin Dashboard",
                cancellationToken);

            _logger.LogInformation("Scheduled maintenance window {Id} by user {UserId}", request.Id, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule maintenance");
            return false;
        }
    }

    /// <summary>
    /// Gets notification settings
    /// </summary>
    public async Task<NotificationSettings> GetNotificationSettingsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var configs = await GetConfigurationsByCategoryAsync("Notifications", cancellationToken);
            
            var settings = new NotificationSettings();
            
            foreach (var config in configs)
            {
                var value = JsonSerializer.Deserialize<JsonElement>(config.Value);
                
                switch (config.Key)
                {
                    case "Email.Enabled":
                        settings.EmailEnabled = value.GetBoolean();
                        break;
                    case "Email.SmtpHost":
                        settings.SmtpHost = value.GetString() ?? string.Empty;
                        break;
                    case "Email.SmtpPort":
                        settings.SmtpPort = value.GetInt32();
                        break;
                    case "Webhook.Enabled":
                        settings.WebhookEnabled = value.GetBoolean();
                        break;
                    case "Webhook.Url":
                        settings.WebhookUrl = value.GetString() ?? string.Empty;
                        break;
                }
            }

            return settings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notification settings");
            throw;
        }
    }

    /// <summary>
    /// Updates notification settings
    /// </summary>
    public async Task<bool> UpdateNotificationSettingsAsync(NotificationSettings settings, ClaimsPrincipal user, 
        string ipAddress, CancellationToken cancellationToken = default)
    {
        try
        {
            var configurations = new Dictionary<string, object>
            {
                ["Email.Enabled"] = settings.EmailEnabled,
                ["Email.SmtpHost"] = settings.SmtpHost,
                ["Email.SmtpPort"] = settings.SmtpPort,
                ["Email.UseSsl"] = settings.UseSsl,
                ["Email.Username"] = settings.Username,
                ["Webhook.Enabled"] = settings.WebhookEnabled,
                ["Webhook.Url"] = settings.WebhookUrl
            };

            return await UpdateConfigurationsAsync(configurations, user, ipAddress, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update notification settings");
            return false;
        }
    }
}

/// <summary>
/// Request for scheduling maintenance window
/// </summary>
public class MaintenanceScheduleRequest
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool NotifyUsers { get; set; } = true;
    public List<string> AffectedServices { get; set; } = new();
}

/// <summary>
/// Notification settings model
/// </summary>
public class NotificationSettings
{
    public bool EmailEnabled { get; set; }
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    
    public bool WebhookEnabled { get; set; }
    public string WebhookUrl { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    
    public bool SlackEnabled { get; set; }
    public string SlackWebhookUrl { get; set; } = string.Empty;
}