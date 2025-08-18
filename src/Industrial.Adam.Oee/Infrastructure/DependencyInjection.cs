using System.Data;
using Industrial.Adam.Oee.Application.Events;
using Industrial.Adam.Oee.Application.Interfaces;
using Industrial.Adam.Oee.Application.Services;
using Industrial.Adam.Oee.Domain.Interfaces;
using Industrial.Adam.Oee.Infrastructure.Configuration;
using Industrial.Adam.Oee.Infrastructure.Monitoring;
using Industrial.Adam.Oee.Infrastructure.Repositories;
using Industrial.Adam.Oee.Infrastructure.Services;
using Industrial.Adam.Oee.Infrastructure.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Polly;

namespace Industrial.Adam.Oee.Infrastructure;

/// <summary>
/// Infrastructure layer dependency injection configuration
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Add OEE Infrastructure services to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddOeeInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Validate configuration structure early
        ValidateOeeConfigurationStructure(configuration);

        // Add configuration
        services.Configure<OeeConfiguration>(configuration.GetSection("Oee"));
        services.Configure<OeeDatabaseSettings>(configuration.GetSection("Oee:Database"));
        services.Configure<OeeCacheSettings>(configuration.GetSection("Oee:Cache"));
        services.Configure<OeeResilienceSettings>(configuration.GetSection("Oee:Resilience"));
        services.Configure<OeeStoppageSettings>(configuration.GetSection("Oee:Stoppage"));
        services.Configure<OeeSignalRSettings>(configuration.GetSection("Oee:SignalR"));
        services.Configure<StoppageMonitoringOptions>(configuration.GetSection("StoppageMonitoring"));
        // Add database connection factory with proper error handling
        services.AddSingleton<IDbConnectionFactory>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<NpgsqlConnectionFactory>>();
            var dbSettings = provider.GetRequiredService<IOptions<OeeDatabaseSettings>>().Value;

            if (string.IsNullOrWhiteSpace(dbSettings.ConnectionString))
            {
                var fallbackConnectionString = configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrWhiteSpace(fallbackConnectionString))
                {
                    throw new InvalidOperationException(
                        "Database connection string is required. Configure either 'Oee:Database:ConnectionString' or 'ConnectionStrings:DefaultConnection'.");
                }
                dbSettings.ConnectionString = fallbackConnectionString;
            }

            return new NpgsqlConnectionFactory(dbSettings.ConnectionString, logger);
        });

        // Add health checks for TimescaleDB with enhanced validation  
        var connectionString = configuration.GetSection("Oee:Database:ConnectionString").Value
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("No database connection string configured");

        services.AddHealthChecks()
            .AddNpgSql(
                connectionString: connectionString,
                name: "timescaledb-basic",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "database", "timescale", "basic" })
            .AddCheck<DatabaseHealthCheck>(
                name: "timescaledb-enhanced",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "database", "timescale", "enhanced", "oee" });

        // Add repositories
        services.AddScoped<ICounterDataRepository, SimpleCounterDataRepository>();
        services.AddScoped<IWorkOrderRepository, WorkOrderRepository>();

        // Add cache services
        services.AddMemoryCache();
        services.AddSingleton<ICacheService, CacheService>();

        // Add application services
        services.AddScoped<IOeeApplicationService, OeeApplicationService>();

        // Add infrastructure services
        services.AddScoped<IDatabaseMigrationService, DatabaseMigrationService>();
        services.AddSingleton<DataAccessMetrics>();

        // Add stoppage notification services
        services.AddScoped<IStoppageNotificationService, StoppageNotificationService>();

        // Add SignalR
        services.AddSignalR(options =>
        {
            var signalRSettings = configuration.GetSection("Oee:SignalR").Get<OeeSignalRSettings>() ?? new OeeSignalRSettings();

            options.EnableDetailedErrors = signalRSettings.EnableDetailedLogging;
            options.KeepAliveInterval = TimeSpan.FromSeconds(signalRSettings.KeepAliveIntervalSeconds);
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(signalRSettings.ClientTimeoutIntervalSeconds);
            options.MaximumReceiveMessageSize = signalRSettings.MaxMessageBufferSize;
        });

        // Add resilience policies (basic implementation for now)
        // Note: For simplicity, we're not registering Polly policies here for now
        // This would be implemented in a future iteration with proper Polly v8 ResilienceStrategy

        // Add performance monitoring
        services.AddSingleton<OeePerformanceMetrics>();

        return services;
    }

    /// <summary>
    /// Validates the OEE configuration structure to provide helpful error messages
    /// </summary>
    private static void ValidateOeeConfigurationStructure(IConfiguration configuration)
    {
        var errors = new List<string>();

        // Check if Oee section exists
        var oeeSection = configuration.GetSection("Oee");
        if (!oeeSection.Exists())
        {
            errors.Add("Missing 'Oee' configuration section in appsettings.json. " +
                      "The configuration must be structured as: { \"Oee\": { \"Database\": { \"ConnectionString\": \"...\" } } }");
        }
        else
        {
            // Check for connection string in either location
            var oeeConnectionString = oeeSection.GetSection("Database:ConnectionString");
            var defaultConnectionString = configuration.GetConnectionString("DefaultConnection");

            if (!oeeConnectionString.Exists() && string.IsNullOrWhiteSpace(defaultConnectionString))
            {
                errors.Add("Missing database connection string. Configure either 'Oee:Database:ConnectionString' or 'ConnectionStrings:DefaultConnection'. " +
                          "Example: { \"Oee\": { \"Database\": { \"ConnectionString\": \"Host=localhost;Database=adam_oee;Username=...;Password=...\" } } }");
            }

            // Check cache configuration
            var cacheSection = oeeSection.GetSection("Cache");
            if (!cacheSection.Exists())
            {
                errors.Add("Missing 'Oee:Cache' configuration section. " +
                          "Add cache settings: { \"Oee\": { \"Cache\": { \"DefaultExpirationMinutes\": 5 } } }");
            }

            // Check resilience configuration
            var resilienceSection = oeeSection.GetSection("Resilience");
            if (!resilienceSection.Exists())
            {
                errors.Add("Missing 'Oee:Resilience' configuration section. " +
                          "Add resilience settings: { \"Oee\": { \"Resilience\": { \"DatabaseRetry\": { \"MaxRetryAttempts\": 3 } } } }");
            }
        }

        if (errors.Any())
        {
            var message = "OEE Configuration validation failed:\n" + string.Join("\n", errors.Select(e => "  • " + e));
            throw new InvalidOperationException(message);
        }
    }
}

/// <summary>
/// Database connection factory interface
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// Create a new database connection
    /// </summary>
    /// <returns>Database connection</returns>
    public Task<IDbConnection> CreateConnectionAsync();
}

/// <summary>
/// Npgsql database connection factory implementation
/// </summary>
public class NpgsqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;
    private readonly ILogger<NpgsqlConnectionFactory> _logger;

    /// <summary>
    /// Constructor for Npgsql connection factory
    /// </summary>
    /// <param name="connectionString">Database connection string</param>
    /// <param name="logger">Logger instance</param>
    public NpgsqlConnectionFactory(string connectionString, ILogger<NpgsqlConnectionFactory> logger)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create a new database connection
    /// </summary>
    /// <returns>Open database connection</returns>
    public async Task<IDbConnection> CreateConnectionAsync()
    {
        try
        {
            _logger.LogDebug("Creating new database connection");
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            _logger.LogDebug("Database connection opened successfully");
            return connection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create database connection");
            throw;
        }
    }
}
