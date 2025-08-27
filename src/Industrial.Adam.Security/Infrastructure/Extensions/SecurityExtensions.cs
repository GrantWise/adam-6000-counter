using System.Threading.RateLimiting;
using FluentValidation;
using Industrial.Adam.Security.Application.Services;
using Industrial.Adam.Security.Application.Validators;
using Industrial.Adam.Security.Infrastructure.Repositories;
// Hubs are now in WebApi layer
using Industrial.Adam.Security.Infrastructure.Logging;
using Industrial.Adam.Security.Infrastructure.Middleware;
using Industrial.Adam.Security.Infrastructure.Services;
// RateLimiting middleware is now in Infrastructure.Middleware
// Services are now in Infrastructure.Services
// Validation middleware is now in Infrastructure.Middleware
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Industrial.Adam.Security.Infrastructure.Extensions;

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
        services.AddValidatorsFromAssemblyContaining<SignAuditRecordCommandValidator>();

        return services;
    }

    /// <summary>
    /// Adds Phase 2 admin dashboard backend services
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddAdminDashboardPhase2Services(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add database connection factory
        services.AddSingleton<DatabaseConnectionFactory>();

        // Add data repositories
        services.AddScoped<AuditLogRepository>();

        // Add Phase 2 services
        services.AddScoped<EnhancedUserManagementService>();
        services.AddScoped<SecurityAuditService>();
        services.AddScoped<ConfigurationManagementService>();

        // Add SignalR for real-time events
        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = configuration.GetValue<bool>("SignalR:EnableDetailedErrors", false);
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(configuration.GetValue<int>("SignalR:ClientTimeoutSeconds", 30));
            options.KeepAliveInterval = TimeSpan.FromSeconds(configuration.GetValue<int>("SignalR:KeepAliveSeconds", 15));
        });

        // Add SignalR broadcast service
        services.AddScoped<ISecurityEventsBroadcastService, SecurityEventsBroadcastService>();

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
    /// Adds native ASP.NET Core rate limiting with enhanced security integration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddSecurityRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add native ASP.NET Core rate limiting
        services.AddRateLimiter(options =>
        {
            var rateLimitConfig = configuration.GetSection("Security:RateLimiting");
            var globalLimit = rateLimitConfig.GetValue<int>("GlobalLimit", 1000);
            var apiLimit = rateLimitConfig.GetValue<int>("ApiLimit", 100);
            var authLimit = rateLimitConfig.GetValue<int>("AuthLimit", 10);
            var windowSeconds = rateLimitConfig.GetValue<int>("WindowSeconds", 60);
            var retryAfterSeconds = rateLimitConfig.GetValue<int>("RetryAfterSeconds", 60);

            // Global rate limiting policy with IP-based partitioning
            options.AddPolicy("GlobalPolicy", context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: GetClientPartitionKey(context),
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = globalLimit,
                        Window = TimeSpan.FromSeconds(windowSeconds),
                        SegmentsPerWindow = 4,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 100
                    }));

            // API endpoints rate limiting policy
            options.AddPolicy("ApiPolicy", context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: GetClientPartitionKey(context),
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = apiLimit,
                        Window = TimeSpan.FromSeconds(windowSeconds),
                        SegmentsPerWindow = 4,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 10
                    }));

            // Authentication endpoints rate limiting policy (most restrictive)
            options.AddPolicy("AuthPolicy", context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: GetClientPartitionKey(context),
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = authLimit,
                        Window = TimeSpan.FromSeconds(windowSeconds),
                        SegmentsPerWindow = 2,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0 // No queuing for auth endpoints
                    }));

            // Configure rejection response with enhanced logging
            options.RejectionStatusCode = 429;
            options.OnRejected = async (context, cancellationToken) =>
            {
                // Mark for security event logging in middleware
                context.HttpContext.Items["RateLimitRejected"] = true;
                context.HttpContext.Items["RateLimitPolicy"] = GetPolicyNameFromContext(context.HttpContext);
                
                if (!context.HttpContext.Response.HasStarted)
                {
                    context.HttpContext.Response.StatusCode = 429;
                    context.HttpContext.Response.Headers["Retry-After"] = retryAfterSeconds.ToString();
                    context.HttpContext.Response.Headers["X-RateLimit-Limit"] = GetLimitForPolicy(GetPolicyNameFromContext(context.HttpContext), rateLimitConfig).ToString();
                    context.HttpContext.Response.Headers["X-RateLimit-Remaining"] = "0";
                    context.HttpContext.Response.Headers["X-RateLimit-Reset"] = DateTimeOffset.UtcNow.AddSeconds(retryAfterSeconds).ToUnixTimeSeconds().ToString();
                    context.HttpContext.Response.ContentType = "application/json";
                    
                    var response = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        error = "rate_limit_exceeded",
                        message = "Too many requests. Please retry after some time.",
                        retry_after_seconds = retryAfterSeconds,
                        policy = GetPolicyNameFromContext(context.HttpContext),
                        timestamp = DateTimeOffset.UtcNow
                    });
                    
                    await context.HttpContext.Response.WriteAsync(response, cancellationToken);
                }
            };

            // Global rejection filter for unhandled requests
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                context => RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: GetClientPartitionKey(context),
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = globalLimit * 2, // More lenient global fallback
                        Window = TimeSpan.FromSeconds(windowSeconds),
                        SegmentsPerWindow = 4,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 50
                    }));
        });

        // Keep memory cache for backward compatibility with other components
        services.AddMemoryCache();

        // Add custom security logging middleware for rate limit events
        services.AddScoped<SecurityRateLimitingMiddleware>();

        return services;
    }

    /// <summary>
    /// Gets the client partition key for rate limiting, supporting X-Forwarded-For headers
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>Partition key for rate limiting</returns>
    private static string GetClientPartitionKey(HttpContext context)
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        
        // Check for forwarded IP (behind proxy/load balancer)
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            ipAddress = forwardedFor.ToString().Split(',').FirstOrDefault()?.Trim() ?? ipAddress;
        }
        
        // For authenticated users, use a combination of IP and user ID for better partitioning
        var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        return !string.IsNullOrEmpty(userId) ? $"{ipAddress}:{userId}" : ipAddress;
    }

    /// <summary>
    /// Determines the rate limit policy based on the request path
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>Policy name</returns>
    private static string GetPolicyNameFromContext(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        
        // Authentication endpoints get the most restrictive policy
        if (path.Contains("/auth/") || path.Contains("/login") || path.Contains("/token"))
        {
            return "AuthPolicy";
        }
        
        // API endpoints get API-specific policy
        if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            return "ApiPolicy";
        }
        
        // Everything else gets global policy
        return "GlobalPolicy";
    }

    /// <summary>
    /// Gets the request limit for a specific policy from configuration
    /// </summary>
    /// <param name="policyName">Policy name</param>
    /// <param name="rateLimitConfig">Rate limit configuration section</param>
    /// <returns>Request limit</returns>
    private static int GetLimitForPolicy(string policyName, IConfigurationSection rateLimitConfig)
    {
        return policyName switch
        {
            "AuthPolicy" => rateLimitConfig.GetValue<int>("AuthLimit", 10),
            "ApiPolicy" => rateLimitConfig.GetValue<int>("ApiLimit", 100),
            "GlobalPolicy" => rateLimitConfig.GetValue<int>("GlobalLimit", 1000),
            _ => rateLimitConfig.GetValue<int>("GlobalLimit", 1000)
        };
    }

    /// <summary>
    /// Adds native rate limiting middleware to the pipeline with security logging
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <returns>Application builder for chaining</returns>
    public static IApplicationBuilder UseSecurityRateLimiting(this IApplicationBuilder app)
    {
        // Add custom security logging middleware first
        app.UseMiddleware<SecurityRateLimitingMiddleware>();
        
        // Then add native rate limiting
        app.UseRateLimiter();
        
        return app;
    }
}
