using Industrial.Adam.Security.Application.Validators;
using Industrial.Adam.Security.Domain.Repositories;
using Industrial.Adam.Security.Domain.Services;
using Industrial.Adam.Security.Infrastructure.Authorization;
using Industrial.Adam.Security.Infrastructure.Repositories;
using Industrial.Adam.Security.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Security.Infrastructure;

/// <summary>
/// Infrastructure layer dependency injection configuration
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds infrastructure services to the service collection
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();

        // Add domain services implementations
        services.AddScoped<IDigitalSignatureService, DigitalSignatureService>();
        services.AddScoped<ISecurityConfigurationService, SecureConfigurationService>();

        // Add infrastructure services
        services.AddScoped<MultiFactorAuthenticationService>();

        // Add authorization handlers
        services.AddScoped<IAuthorizationHandler, ResourceBasedAuthorizationHandler>();

        // Authorization policies should be configured at WebApi layer

        // Add database connection factory with connection pooling
        services.Configure<DatabaseConnectionOptions>(configuration.GetSection("Database"));
        services.AddSingleton<DatabaseConnectionFactory>();

        // Add caching for performance
        services.AddMemoryCache();

        // HTTP context accessor should be added at the WebApi layer

        return services;
    }

    /// <summary>
    /// Adds security middleware services
    /// </summary>
    public static IServiceCollection AddSecurityMiddleware(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure rate limiting
        services.Configure<RateLimitingOptions>(configuration.GetSection("Security:RateLimiting"));
        
        // Configure security headers
        services.Configure<SecurityHeadersOptions>(configuration.GetSection("Security:Headers"));
        
        // Configure input validation
        services.Configure<InputValidationOptions>(configuration.GetSection("Security:Validation"));

        return services;
    }
}

/// <summary>
/// IP restriction requirement for authorization
/// </summary>
public class IpRestrictionRequirement : IAuthorizationRequirement
{
    public List<string> AllowedRanges { get; }

    public IpRestrictionRequirement(IEnumerable<string> allowedRanges)
    {
        AllowedRanges = allowedRanges.ToList();
    }
}

/// <summary>
/// Time restriction requirement for authorization
/// </summary>
public class TimeRestrictionRequirement : IAuthorizationRequirement
{
    public TimeSpan StartTime { get; }
    public TimeSpan EndTime { get; }

    public TimeRestrictionRequirement(TimeSpan startTime, TimeSpan endTime)
    {
        StartTime = startTime;
        EndTime = endTime;
    }
}

/// <summary>
/// Database connection options for connection pooling
/// </summary>
public class DatabaseConnectionOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public int MinPoolSize { get; set; } = 5;
    public int MaxPoolSize { get; set; } = 50;
    public int ConnectionTimeout { get; set; } = 30;
    public int CommandTimeout { get; set; } = 30;
    public bool EnableRetryOnFailure { get; set; } = true;
    public int MaxRetryCount { get; set; } = 3;
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// Rate limiting configuration options
/// </summary>
public class RateLimitingOptions
{
    public int RequestsPerMinute { get; set; } = 100;
    public int BurstSize { get; set; } = 10;
    public TimeSpan WindowSize { get; set; } = TimeSpan.FromMinutes(1);
    public bool EnableIpBasedLimiting { get; set; } = true;
    public bool EnableUserBasedLimiting { get; set; } = true;
    public Dictionary<string, int> EndpointLimits { get; set; } = new();
}

/// <summary>
/// Security headers configuration options
/// </summary>
public class SecurityHeadersOptions
{
    public bool EnableHsts { get; set; } = true;
    public int HstsMaxAge { get; set; } = 31536000; // 1 year
    public bool EnableXFrameOptions { get; set; } = true;
    public string XFrameOptions { get; set; } = "DENY";
    public bool EnableXContentTypeOptions { get; set; } = true;
    public bool EnableReferrerPolicy { get; set; } = true;
    public string ReferrerPolicy { get; set; } = "strict-origin-when-cross-origin";
    public bool EnableContentSecurityPolicy { get; set; } = true;
    public string ContentSecurityPolicy { get; set; } = "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:";
    public Dictionary<string, string> CustomHeaders { get; set; } = new();
}

/// <summary>
/// Input validation configuration options
/// </summary>
public class InputValidationOptions
{
    public int MaxRequestSize { get; set; } = 10485760; // 10MB
    public int MaxParameterLength { get; set; } = 4096;
    public int MaxHeaderLength { get; set; } = 8192;
    public bool EnableSqlInjectionDetection { get; set; } = true;
    public bool EnableXssDetection { get; set; } = true;
    public bool EnablePathTraversalDetection { get; set; } = true;
    public bool EnableCommandInjectionDetection { get; set; } = true;
    public SanitizationLevel DefaultSanitizationLevel { get; set; } = SanitizationLevel.Standard;
    public List<string> BypassValidationEndpoints { get; set; } = new();
}