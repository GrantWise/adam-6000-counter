using FluentValidation;
using Industrial.Adam.Security.Logging;
using Industrial.Adam.Security.Middleware;
using Industrial.Adam.Security.Monitoring;
using Industrial.Adam.Security.RateLimiting;
using Industrial.Adam.Security.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Industrial.Adam.Security.Extensions;

/// <summary>
/// Extension methods for registering security services
/// </summary>
public static class SecurityExtensions
{
    /// <summary>
    /// Adds comprehensive security logging and monitoring services
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddSecurityLoggingAndMonitoring(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Validate configuration structure early
        ValidateSecurityConfigurationStructure(configuration);

        // Add security event logger
        services.AddSingleton<SecurityEventLogger>();

        // Add security monitoring service
        services.Configure<SecurityMonitoringOptions>(
            configuration.GetSection("Security:Monitoring"));
        services.AddSingleton<SecurityMonitoringService>();
        services.AddHostedService(provider => provider.GetRequiredService<SecurityMonitoringService>());

        // Add input validation options
        services.Configure<InputValidationOptions>(
            configuration.GetSection("Security:Validation"));

        // Add FluentValidation
        services.AddValidatorsFromAssemblyContaining<NoSqlInjectionAttribute>();

        return services;
    }

    /// <summary>
    /// Adds security middleware to the application pipeline
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Application builder for chaining</returns>
    public static IApplicationBuilder UseSecurityMiddleware(
        this IApplicationBuilder app,
        IConfiguration configuration)
    {
        // Add security headers middleware (should be early in pipeline)
        app.UseMiddleware<SecurityHeadersMiddleware>();

        // Add security audit middleware (after authentication but before authorization)
        app.UseMiddleware<SecurityAuditMiddleware>();

        // Add input validation middleware (after routing)
        app.UseMiddleware<InputValidationMiddleware>();

        return app;
    }

    /// <summary>
    /// Adds complete security package with logging, monitoring, and middleware
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddComprehensiveSecurity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services
            .AddSecurityLoggingAndMonitoring(configuration)
            .AddSecurityRateLimiting(configuration);
    }

    /// <summary>
    /// Adds complete security middleware pipeline
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Application builder for chaining</returns>
    public static IApplicationBuilder UseComprehensiveSecurityPipeline(
        this IApplicationBuilder app,
        IConfiguration configuration)
    {
        return app
            .UseSecurityRateLimiting()
            .UseSecurityMiddleware(configuration);
    }

    /// <summary>
    /// Validates the security configuration structure to provide helpful error messages
    /// </summary>
    private static void ValidateSecurityConfigurationStructure(IConfiguration configuration)
    {
        var errors = new List<string>();

        // Check if Security section exists
        var securitySection = configuration.GetSection("Security");
        if (!securitySection.Exists())
        {
            errors.Add("Missing 'Security' configuration section in appsettings.json. " +
                      "The configuration must be structured as: { \"Security\": { \"Headers\": {...}, \"Monitoring\": {...}, \"Validation\": {...} } }");
        }
        else
        {
            // Check for Headers section
            var headersSection = securitySection.GetSection("Headers");
            if (!headersSection.Exists())
            {
                errors.Add("Missing 'Security:Headers' configuration section. " +
                          "Add security headers configuration: { \"Security\": { \"Headers\": { \"ContentSecurityPolicy\": {...}, \"XFrameOptions\": {...} } } }");
            }
            else
            {
                // Check for CSP configuration
                var cspSection = headersSection.GetSection("ContentSecurityPolicy");
                if (!cspSection.Exists())
                {
                    errors.Add("Missing 'Security:Headers:ContentSecurityPolicy' configuration section. " +
                              "Content Security Policy is required for XSS protection.");
                }
                else
                {
                    // Validate CSP settings
                    var scriptSrc = cspSection["ScriptSrc"];
                    if (!string.IsNullOrEmpty(scriptSrc) &&
                        (scriptSrc.Contains("'unsafe-inline'") || scriptSrc.Contains("'unsafe-eval'")))
                    {
                        errors.Add("Content Security Policy contains unsafe directives ('unsafe-inline' or 'unsafe-eval'). " +
                                  "These should be removed for security. Use nonces or hashes instead.");
                    }
                }
            }

            // Check for Monitoring section
            var monitoringSection = securitySection.GetSection("Monitoring");
            if (!monitoringSection.Exists())
            {
                errors.Add("Missing 'Security:Monitoring' configuration section. " +
                          "Add monitoring settings: { \"Security\": { \"Monitoring\": { \"CheckInterval\": \"00:01:00\", \"BruteForceThreshold\": 10 } } }");
            }

            // Check for Validation section
            var validationSection = securitySection.GetSection("Validation");
            if (!validationSection.Exists())
            {
                errors.Add("Missing 'Security:Validation' configuration section. " +
                          "Add validation settings: { \"Security\": { \"Validation\": { \"MaxRequestSize\": 10485760, \"MaxParameterLength\": 4096 } } }");
            }
        }

        // Validate JWT configuration
        if (string.IsNullOrEmpty(configuration["JWT_SECRET_KEY"]))
        {
            errors.Add("JWT_SECRET_KEY environment variable is required for security functionality. " +
                      "Set this environment variable with a strong secret key (minimum 256 bits).");
        }

        // Check for CORS configuration (warning, not error)
        if (string.IsNullOrEmpty(configuration["CORS_ORIGINS"]))
        {
            errors.Add("CORS_ORIGINS environment variable should be configured for production. " +
                      "Example: CORS_ORIGINS=https://app1.example.com,https://app2.example.com");
        }

        if (errors.Any())
        {
            var message = "Security configuration validation failed:\n" +
                         string.Join("\n", errors.Select(e => "  • " + e));
            throw new InvalidOperationException(message);
        }
    }

    /// <summary>
    /// Validates security configuration at startup (kept for backward compatibility)
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection ValidateSecurityConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ValidateSecurityConfigurationStructure(configuration);
        return services;
    }

    /// <summary>
    /// Adds security controllers for monitoring endpoints
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddSecurityControllers(this IServiceCollection services)
    {
        // Controllers will be automatically registered by MVC
        // This method is for explicit registration if needed
        return services;
    }

    /// <summary>
    /// Configures security middleware with custom options
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <param name="configureHeaders">Security headers configuration</param>
    /// <param name="configureValidation">Input validation configuration</param>
    /// <returns>Application builder for chaining</returns>
    public static IApplicationBuilder UseSecurityMiddlewareWithOptions(
        this IApplicationBuilder app,
        Action<SecurityHeadersOptions>? configureHeaders = null,
        Action<InputValidationOptions>? configureValidation = null)
    {
        // Configure security headers
        var headerOptions = new SecurityHeadersOptions();
        configureHeaders?.Invoke(headerOptions);

        // Configure input validation
        var validationOptions = new InputValidationOptions();
        configureValidation?.Invoke(validationOptions);

        // Add middleware with custom options
        app.UseMiddleware<SecurityHeadersMiddleware>(headerOptions);
        app.UseMiddleware<SecurityAuditMiddleware>();
        app.UseMiddleware<InputValidationMiddleware>(validationOptions);

        return app;
    }

    /// <summary>
    /// Adds security event logging to specific services
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddSecurityEventLogging(this IServiceCollection services)
    {
        services.AddSingleton<SecurityEventLogger>();
        return services;
    }

    /// <summary>
    /// Adds Polly-based security rate limiting with resilience patterns
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddSecurityRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add Polly rate limiting configuration
        services.Configure<RateLimitingConfiguration>(
            configuration.GetSection("Security:RateLimiting"));

        // Keep memory cache for backward compatibility with other components
        services.AddMemoryCache();

        // Add both middleware implementations for migration period
        services.AddScoped<RateLimitingMiddleware>(); // Legacy
        services.AddScoped<PollyRateLimitingMiddleware>(); // New Polly-based

        return services;
    }

    /// <summary>
    /// Adds Polly-based rate limiting middleware to the pipeline
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <returns>Application builder for chaining</returns>
    public static IApplicationBuilder UseSecurityRateLimiting(this IApplicationBuilder app)
    {
        // Use the new Polly-based implementation
        app.UseMiddleware<PollyRateLimitingMiddleware>();
        return app;
    }
}
