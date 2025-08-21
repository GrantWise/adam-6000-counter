using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Security.Configuration;

/// <summary>
/// Validates required environment variables and configuration settings
/// </summary>
public static class EnvironmentValidator
{
    /// <summary>
    /// Required environment variables for basic application functionality
    /// </summary>
    public static readonly string[] RequiredEnvironmentVariables = [
        "TIMESCALEDB_USERNAME",
        "TIMESCALEDB_PASSWORD",
        "JWT_SECRET_KEY",
        "JWT_ISSUER",
        "JWT_AUDIENCE"
    ];

    /// <summary>
    /// Optional environment variables with default values
    /// </summary>
    public static readonly Dictionary<string, string> OptionalEnvironmentVariables = new()
    {
        ["JWT_EXPIRATION_MINUTES"] = "60",
        ["JWT_REFRESH_EXPIRATION_DAYS"] = "7",
        ["CORS_ORIGINS"] = "http://localhost:3000,http://localhost:5000",
        ["GRAFANA_ADMIN_PASSWORD"] = "", // Required if using Grafana
        ["INFLUXDB_ADMIN_PASSWORD"] = "", // Required if using InfluxDB
        ["INFLUXDB_ADMIN_TOKEN"] = "" // Required if using InfluxDB
    };

    /// <summary>
    /// Validates that all required environment variables are present and valid
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    /// <param name="logger">Logger for validation messages</param>
    /// <returns>True if all validations pass, false otherwise</returns>
    public static bool ValidateEnvironmentVariables(IConfiguration configuration, ILogger? logger = null)
    {
        var missingVariables = new List<string>();
        var invalidVariables = new List<string>();

        foreach (var variable in RequiredEnvironmentVariables)
        {
            var value = configuration[variable];

            if (string.IsNullOrWhiteSpace(value))
            {
                missingVariables.Add(variable);
                continue;
            }

            // Specific security validations
            switch (variable)
            {
                case "JWT_SECRET_KEY":
                    if (value.Length < 32)
                    {
                        invalidVariables.Add($"{variable} (must be at least 32 characters for security)");
                    }
                    if (IsWeakSecret(value))
                    {
                        invalidVariables.Add($"{variable} (appears to be weak or default - use cryptographically secure random value)");
                    }
                    break;

                case "TIMESCALEDB_PASSWORD":
                    if (value.Length < 12)
                    {
                        invalidVariables.Add($"{variable} (must be at least 12 characters for security)");
                    }
                    if (IsCommonPassword(value))
                    {
                        invalidVariables.Add($"{variable} (appears to be a common password - use secure random password)");
                    }
                    break;

                case "TIMESCALEDB_USERNAME":
                    if (IsDefaultUsername(value))
                    {
                        invalidVariables.Add($"{variable} (using default username - consider using application-specific username)");
                    }
                    break;

                case "JWT_ISSUER":
                case "JWT_AUDIENCE":
                    if (value.Contains("test") || value.Contains("example") || value.Contains("localhost"))
                    {
                        logger?.LogWarning("Environment variable {Variable} contains development-like value: {Value}. Ensure this is appropriate for production.", variable, value);
                    }
                    break;
            }
        }

        // Log validation results
        if (missingVariables.Count > 0)
        {
            var missing = string.Join(", ", missingVariables);
            logger?.LogError("Missing required environment variables: {MissingVariables}", missing);
        }

        if (invalidVariables.Count > 0)
        {
            var invalid = string.Join(", ", invalidVariables);
            logger?.LogError("Invalid environment variables: {InvalidVariables}", invalid);
        }

        var isValid = missingVariables.Count == 0 && invalidVariables.Count == 0;

        if (isValid)
        {
            logger?.LogInformation("Environment variable validation passed");
        }
        else
        {
            logger?.LogError("Environment variable validation failed. Please check .env.local file");
        }

        return isValid;
    }

    /// <summary>
    /// Validates environment variables and throws exception if validation fails
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    /// <param name="logger">Logger for validation messages</param>
    /// <exception cref="InvalidOperationException">Thrown when validation fails</exception>
    public static void ValidateEnvironmentVariablesOrThrow(IConfiguration configuration, ILogger? logger = null)
    {
        if (!ValidateEnvironmentVariables(configuration, logger))
        {
            throw new InvalidOperationException(
                "Environment variable validation failed. Please ensure all required environment variables are set in .env.local file. " +
                "See .env.template for required variables.");
        }
    }

    /// <summary>
    /// Gets a secure configuration summary for logging (no secrets)
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Safe configuration summary</returns>
    public static object GetSecureConfigurationSummary(IConfiguration configuration)
    {
        return new
        {
            Database = new
            {
                Host = configuration["TIMESCALE_HOST"],
                Port = configuration["TIMESCALE_PORT"],
                Database = configuration["TIMESCALE_DATABASE"],
                Username = configuration["TIMESCALE_USERNAME"],
                HasPassword = !string.IsNullOrEmpty(configuration["TIMESCALE_PASSWORD"])
            },
            Authentication = new
            {
                Issuer = configuration["JWT_ISSUER"],
                Audience = configuration["JWT_AUDIENCE"],
                ExpirationMinutes = configuration["JWT_EXPIRATION_MINUTES"],
                HasSecretKey = !string.IsNullOrEmpty(configuration["JWT_SECRET_KEY"])
            },
            Cors = new
            {
                Origins = configuration["CORS_ORIGINS"]?.Split(',', StringSplitOptions.RemoveEmptyEntries),
                Methods = configuration["CORS_METHODS"]?.Split(',', StringSplitOptions.RemoveEmptyEntries),
                Headers = configuration["CORS_HEADERS"]?.Split(',', StringSplitOptions.RemoveEmptyEntries)
            }
        };
    }

    /// <summary>
    /// Validates startup security configuration and logs findings
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    /// <param name="logger">Logger for security findings</param>
    public static void ValidateSecurityConfiguration(IConfiguration configuration, ILogger? logger = null)
    {
        logger?.LogInformation("Performing security configuration validation...");

        // Check for hardcoded secrets in configuration
        CheckForHardcodedSecrets(configuration, logger);

        // Validate security settings
        ValidateSecuritySettings(configuration, logger);

        // Check for development mode settings in production
        CheckProductionReadiness(configuration, logger);

        logger?.LogInformation("Security configuration validation completed");
    }

    private static void CheckForHardcodedSecrets(IConfiguration configuration, ILogger? logger)
    {
        var suspiciousPatterns = new[] { "admin123", "password123", "secret", "test", "admin_password" };
        var configRoot = (IConfigurationRoot)configuration;

        foreach (var provider in configRoot.Providers)
        {
            if (provider is Microsoft.Extensions.Configuration.Json.JsonConfigurationProvider jsonProvider)
            {
                try
                {
                    var data = jsonProvider.GetType()
                        .GetProperty("Data", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                        .GetValue(jsonProvider) as Dictionary<string, string>;

                    if (data != null)
                    {
                        foreach (var kvp in data)
                        {
                            var value = kvp.Value?.ToLowerInvariant() ?? "";
                            foreach (var pattern in suspiciousPatterns)
                            {
                                if (value.Contains(pattern))
                                {
                                    logger?.LogWarning("SECURITY: Potential hardcoded secret found in configuration key: {Key}", kvp.Key);
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore reflection errors
                }
            }
        }
    }

    private static void ValidateSecuritySettings(IConfiguration configuration, ILogger? logger)
    {
        // Check JWT expiration settings
        if (int.TryParse(configuration["JWT_EXPIRATION_MINUTES"], out var jwtExpiration))
        {
            if (jwtExpiration > 480) // 8 hours
            {
                logger?.LogWarning("SECURITY: JWT expiration is set to {Minutes} minutes. Consider shorter expiration for better security.", jwtExpiration);
            }
        }

        if (int.TryParse(configuration["JWT_REFRESH_EXPIRATION_DAYS"], out var refreshExpiration))
        {
            if (refreshExpiration > 30)
            {
                logger?.LogWarning("SECURITY: Refresh token expiration is set to {Days} days. Consider shorter expiration for better security.", refreshExpiration);
            }
        }

        // Check CORS settings
        var corsOrigins = configuration["CORS_ORIGINS"];
        if (!string.IsNullOrEmpty(corsOrigins) && corsOrigins.Contains("*"))
        {
            logger?.LogWarning("SECURITY: CORS is configured to allow all origins (*). This should only be used in development.");
        }
    }

    private static void CheckProductionReadiness(IConfiguration configuration, ILogger? logger)
    {
        var developmentIndicators = new[]
        {
            ("JWT_ISSUER", "localhost"),
            ("JWT_AUDIENCE", "localhost"),
            ("CORS_ORIGINS", "localhost")
        };

        foreach (var (key, indicator) in developmentIndicators)
        {
            var value = configuration[key];
            if (!string.IsNullOrEmpty(value) && value.Contains(indicator))
            {
                logger?.LogWarning("PRODUCTION: Configuration {Key} contains '{Indicator}' which suggests development environment.", key, indicator);
            }
        }
    }

    private static bool IsWeakSecret(string secret)
    {
        // Check for common weak patterns
        var weakPatterns = new[]
        {
            "secret", "password", "admin", "test", "key", "token",
            "12345", "qwerty", "abc", "default"
        };

        var lowerSecret = secret.ToLowerInvariant();
        return weakPatterns.Any(pattern => lowerSecret.Contains(pattern)) ||
               secret.All(char.IsLetterOrDigit) && // No special characters
               (secret.All(char.IsLetter) || secret.All(char.IsDigit)); // All letters or all digits
    }

    private static bool IsCommonPassword(string password)
    {
        var commonPasswords = new[]
        {
            "password", "admin", "admin123", "password123", "123456",
            "qwerty", "abc123", "admin_password", "adam_password"
        };

        return commonPasswords.Contains(password.ToLowerInvariant());
    }

    private static bool IsDefaultUsername(string username)
    {
        var defaultUsernames = new[]
        {
            "admin", "root", "user", "postgres", "timescale", "adam_user"
        };

        return defaultUsernames.Contains(username.ToLowerInvariant());
    }
}
