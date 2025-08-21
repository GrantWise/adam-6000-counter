using Industrial.Adam.Security.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Security.Extensions;

/// <summary>
/// Extensions for configuration and environment variable management
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Adds .env file support to configuration builder
    /// </summary>
    /// <param name="builder">Configuration builder</param>
    /// <param name="environmentName">Current environment name</param>
    /// <returns>Configuration builder for chaining</returns>
    public static IConfigurationBuilder AddEnvironmentFiles(this IConfigurationBuilder builder, string? environmentName = null)
    {
        // Load .env.local for local development secrets
        var envLocalPath = Path.Combine(Directory.GetCurrentDirectory(), ".env.local");
        if (File.Exists(envLocalPath))
        {
            builder.AddInMemoryCollection(LoadEnvironmentFile(envLocalPath));
        }

        // Load environment-specific .env file if specified
        if (!string.IsNullOrEmpty(environmentName))
        {
            var envPath = Path.Combine(Directory.GetCurrentDirectory(), $".env.{environmentName.ToLowerInvariant()}");
            if (File.Exists(envPath))
            {
                builder.AddInMemoryCollection(LoadEnvironmentFile(envPath));
            }
        }

        // Override with actual environment variables
        builder.AddEnvironmentVariables();

        return builder;
    }

    /// <summary>
    /// Validates environment variables during application startup
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddEnvironmentValidation(this IServiceCollection services)
    {
        services.AddHostedService<EnvironmentValidationHostedService>();
        return services;
    }

    /// <summary>
    /// Gets database connection string from environment variables
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    /// <param name="useDockerHost">Whether to use Docker host names</param>
    /// <returns>Database connection string</returns>
    public static string GetTimescaleConnectionString(this IConfiguration configuration, bool? useDockerHost = null)
    {
        var isDocker = useDockerHost ?? configuration.GetValue<bool>("DOCKER_ENVIRONMENT");
        var host = isDocker ? configuration["DOCKER_TIMESCALE_HOST"] : configuration["TIMESCALE_HOST"];
        var port = configuration["TIMESCALE_PORT"];
        var database = configuration["TIMESCALE_DATABASE"];
        var username = configuration["TIMESCALE_USERNAME"];
        var password = configuration["TIMESCALE_PASSWORD"];

        return $"Host={host};Port={port};Database={database};Username={username};Password={password};Pooling=true;MinPoolSize=5;MaxPoolSize=20;CommandTimeout=30;";
    }

    /// <summary>
    /// Gets equipment scheduling database connection string from environment variables
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    /// <param name="useDockerHost">Whether to use Docker host names</param>
    /// <returns>Database connection string</returns>
    public static string GetEquipmentSchedulingConnectionString(this IConfiguration configuration, bool? useDockerHost = null)
    {
        var isDocker = useDockerHost ?? configuration.GetValue<bool>("DOCKER_ENVIRONMENT");
        var host = isDocker ? configuration["DOCKER_EQUIPMENT_SCHEDULING_HOST"] : configuration["EQUIPMENT_SCHEDULING_HOST"];
        var port = configuration["EQUIPMENT_SCHEDULING_PORT"];
        var database = configuration["EQUIPMENT_SCHEDULING_DATABASE"];
        var username = configuration["EQUIPMENT_SCHEDULING_USERNAME"];
        var password = configuration["EQUIPMENT_SCHEDULING_PASSWORD"];

        return $"Host={host};Port={port};Database={database};Username={username};Password={password};Pooling=true;MinPoolSize=5;MaxPoolSize=20;CommandTimeout=30;";
    }

    /// <summary>
    /// Gets CORS origins from environment variables
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Array of allowed CORS origins</returns>
    public static string[] GetCorsOrigins(this IConfiguration configuration)
    {
        var origins = configuration["CORS_ORIGINS"];
        if (string.IsNullOrWhiteSpace(origins))
        {
            return [];
        }

        return origins.Split(',', StringSplitOptions.RemoveEmptyEntries)
                     .Select(origin => origin.Trim())
                     .ToArray();
    }

    /// <summary>
    /// Gets CORS methods from environment variables
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Array of allowed CORS methods</returns>
    public static string[] GetCorsMethods(this IConfiguration configuration)
    {
        var methods = configuration["CORS_METHODS"] ?? "GET,POST,PUT,DELETE,OPTIONS";
        return methods.Split(',', StringSplitOptions.RemoveEmptyEntries)
                     .Select(method => method.Trim())
                     .ToArray();
    }

    /// <summary>
    /// Gets CORS headers from environment variables
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Array of allowed CORS headers</returns>
    public static string[] GetCorsHeaders(this IConfiguration configuration)
    {
        var headers = configuration["CORS_HEADERS"] ?? "Content-Type,Authorization,X-Requested-With";
        return headers.Split(',', StringSplitOptions.RemoveEmptyEntries)
                     .Select(header => header.Trim())
                     .ToArray();
    }

    /// <summary>
    /// Loads key-value pairs from .env file
    /// </summary>
    /// <param name="filePath">Path to .env file</param>
    /// <returns>Dictionary of key-value pairs</returns>
    private static IDictionary<string, string?> LoadEnvironmentFile(string filePath)
    {
        var result = new Dictionary<string, string?>();

        foreach (var line in File.ReadAllLines(filePath))
        {
            var trimmedLine = line.Trim();

            // Skip empty lines and comments
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith('#'))
                continue;

            // Parse KEY=VALUE format
            var equalIndex = trimmedLine.IndexOf('=');
            if (equalIndex > 0)
            {
                var key = trimmedLine[..equalIndex].Trim();
                var value = trimmedLine[(equalIndex + 1)..].Trim();

                // Remove quotes if present
                if (value.StartsWith('"') && value.EndsWith('"'))
                {
                    value = value[1..^1];
                }

                result[key] = value;
            }
        }

        return result;
    }
}

/// <summary>
/// Hosted service for validating environment variables at startup
/// </summary>
internal class EnvironmentValidationHostedService : IHostedService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EnvironmentValidationHostedService> _logger;

    public EnvironmentValidationHostedService(IConfiguration configuration, ILogger<EnvironmentValidationHostedService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting environment variable validation...");

        try
        {
            EnvironmentValidator.ValidateEnvironmentVariablesOrThrow(_configuration, _logger);
            _logger.LogInformation("Environment variable validation completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Environment variable validation failed");
            throw;
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
