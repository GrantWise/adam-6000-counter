using System.Diagnostics;
using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Industrial.Adam.Security.Logging;
using Industrial.Adam.Security.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.RateLimiting;

namespace Industrial.Adam.Security.RateLimiting;

/// <summary>
/// Polly-based rate limiting middleware for security protection
/// Implements sliding window rate limiting with configurable policies
/// </summary>
public class PollyRateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PollyRateLimitingMiddleware> _logger;
    private readonly SecurityEventLogger _securityLogger;
    private readonly RateLimitingConfiguration _config;
    private readonly ResiliencePipeline<HttpContext> _globalPipeline;
    private readonly ResiliencePipeline<HttpContext> _authPipeline;
    private readonly ResiliencePipeline<HttpContext> _apiPipeline;

    public PollyRateLimitingMiddleware(
        RequestDelegate next,
        ILogger<PollyRateLimitingMiddleware> logger,
        SecurityEventLogger securityLogger,
        IConfiguration configuration)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _securityLogger = securityLogger ?? throw new ArgumentNullException(nameof(securityLogger));

        _config = configuration.GetSection("Security:RateLimiting").Get<RateLimitingConfiguration>()
            ?? new RateLimitingConfiguration();

        // Build resilience pipelines with proper rate limiting
        _globalPipeline = BuildGlobalPipeline();
        _authPipeline = BuildAuthPipeline();
        _apiPipeline = BuildApiPipeline();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip rate limiting for health checks and static files
        if (IsExcludedPath(context.Request.Path))
        {
            await _next(context);
            return;
        }

        try
        {
            // Determine which pipeline to use based on the path
            var pipeline = GetApplicablePipeline(context);

            // Execute the request through the rate limiting pipeline
            await pipeline.ExecuteAsync(async (ctx, ct) =>
            {
                await _next(context);
                return context;
            }, ResilienceContextPool.Shared.Get(context.RequestAborted));
        }
        catch (RateLimiterRejectedException)
        {
            await HandleRateLimitExceeded(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in rate limiting middleware");
            // Allow request to proceed on middleware failure
            await _next(context);
        }
    }

    private ResiliencePipeline<HttpContext> BuildGlobalPipeline()
    {
        return new ResiliencePipelineBuilder<HttpContext>()
            .AddRateLimiter(new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = _config.GlobalLimits.RequestsPerWindow,
                Window = TimeSpan.FromSeconds(_config.GlobalLimits.WindowSeconds),
                SegmentsPerWindow = _config.GlobalLimits.SegmentsPerWindow,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = _config.GlobalLimits.QueueLimit
            }))
            .Build();
    }

    private ResiliencePipeline<HttpContext> BuildAuthPipeline()
    {
        return new ResiliencePipelineBuilder<HttpContext>()
            .AddRateLimiter(new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = _config.AuthenticationLimits.RequestsPerWindow,
                Window = TimeSpan.FromSeconds(_config.AuthenticationLimits.WindowSeconds),
                SegmentsPerWindow = _config.AuthenticationLimits.SegmentsPerWindow,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = _config.AuthenticationLimits.QueueLimit
            }))
            .Build();
    }

    private ResiliencePipeline<HttpContext> BuildApiPipeline()
    {
        return new ResiliencePipelineBuilder<HttpContext>()
            .AddRateLimiter(new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = _config.ApiLimits.RequestsPerWindow,
                Window = TimeSpan.FromSeconds(_config.ApiLimits.WindowSeconds),
                SegmentsPerWindow = _config.ApiLimits.SegmentsPerWindow,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = _config.ApiLimits.QueueLimit
            }))
            .Build();
    }

    private ResiliencePipeline<HttpContext> GetApplicablePipeline(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

        // Authentication endpoints get stricter limits
        if (path.Contains("/auth") || path.Contains("/login") || path.Contains("/token"))
        {
            return _authPipeline;
        }

        // API endpoints get API limits
        if (path.StartsWith("/api/"))
        {
            return _apiPipeline;
        }

        // Everything else gets global limits
        return _globalPipeline;
    }

    private bool IsExcludedPath(PathString path)
    {
        var pathValue = path.Value?.ToLowerInvariant() ?? "";

        // Exclude health checks and metrics
        if (pathValue.Contains("/health") || pathValue.Contains("/metrics"))
            return true;

        // Exclude static files
        if (pathValue.EndsWith(".css") || pathValue.EndsWith(".js") ||
            pathValue.EndsWith(".png") || pathValue.EndsWith(".jpg") ||
            pathValue.EndsWith(".ico") || pathValue.EndsWith(".svg"))
            return true;

        return false;
    }

    private async Task HandleRateLimitExceeded(HttpContext context)
    {
        // Log security event
        var clientInfo = ExtractClientInfo(context);
        var securityEvent = SecurityEvent.CreateSuspiciousActivityEvent(
            "Rate limit exceeded",
            clientInfo.IpAddress,
            clientInfo.UserAgent,
            Activity.Current?.Id ?? Guid.NewGuid().ToString());

        securityEvent.Username = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        securityEvent.Resource = context.Request.Path.Value ?? "";
        securityEvent.Metadata["request_method"] = context.Request.Method;
        securityEvent.Metadata["client_id"] = clientInfo.ClientId;
        securityEvent.Metadata["endpoint"] = context.Request.Path.Value ?? "";

        _securityLogger.LogSecurityEvent(securityEvent);

        // Return 429 Too Many Requests
        context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
        context.Response.Headers.Append("Retry-After", _config.RetryAfterSeconds.ToString());

        await context.Response.WriteAsJsonAsync(new
        {
            error = "rate_limit_exceeded",
            message = "Too many requests. Please retry after some time.",
            retry_after_seconds = _config.RetryAfterSeconds
        });
    }

    private (string IpAddress, string UserAgent, string ClientId) ExtractClientInfo(HttpContext context)
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // Check for forwarded IP (behind proxy/load balancer)
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            ipAddress = forwardedFor.ToString().Split(',').FirstOrDefault()?.Trim() ?? ipAddress;
        }

        var userAgent = context.Request.Headers.UserAgent.ToString();
        var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var clientId = !string.IsNullOrEmpty(userId) ? $"user:{userId}" : $"ip:{ipAddress}";

        return (ipAddress, userAgent, clientId);
    }
}

/// <summary>
/// Rate limiting configuration model
/// </summary>
public class RateLimitingConfiguration
{
    public RateLimitSettings GlobalLimits { get; set; } = new()
    {
        RequestsPerWindow = 1000,
        WindowSeconds = 60,
        SegmentsPerWindow = 4,
        QueueLimit = 100
    };

    public RateLimitSettings ApiLimits { get; set; } = new()
    {
        RequestsPerWindow = 100,
        WindowSeconds = 60,
        SegmentsPerWindow = 4,
        QueueLimit = 10
    };

    public RateLimitSettings AuthenticationLimits { get; set; } = new()
    {
        RequestsPerWindow = 10,
        WindowSeconds = 60,
        SegmentsPerWindow = 2,
        QueueLimit = 0 // No queuing for auth endpoints
    };

    public int RetryAfterSeconds { get; set; } = 60;
}

/// <summary>
/// Rate limit settings for a specific endpoint category
/// </summary>
public class RateLimitSettings
{
    public int RequestsPerWindow { get; set; }
    public int WindowSeconds { get; set; }
    public int SegmentsPerWindow { get; set; }
    public int QueueLimit { get; set; }
}
