using System.Net;
using Industrial.Adam.Security.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Industrial.Adam.Security.Middleware;

/// <summary>
/// Middleware for rate limiting requests to prevent abuse and DoS attacks
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly SecurityEventLogger _securityLogger;
    private readonly IMemoryCache _cache;
    private readonly RateLimitingOptions _options;

    public RateLimitingMiddleware(
        RequestDelegate next,
        ILogger<RateLimitingMiddleware> logger,
        SecurityEventLogger securityLogger,
        IMemoryCache cache,
        IOptions<RateLimitingOptions> options)
    {
        _next = next;
        _logger = logger;
        _securityLogger = securityLogger;
        _cache = cache;
        _options = options.Value;
    }

    /// <summary>
    /// Processes HTTP request with rate limiting
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>Task</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

        var ipAddress = GetClientIpAddress(context);
        var userId = context.User?.Identity?.Name;
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

        try
        {
            // Check different rate limits based on endpoint type
            var rateLimitResult = CheckRateLimit(ipAddress, userId, path, context);

            if (!rateLimitResult.IsAllowed)
            {
                await HandleRateLimitExceeded(context, rateLimitResult, ipAddress, userId);
                return;
            }

            // Update counters for successful requests
            UpdateRequestCounters(ipAddress, userId, path);

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in rate limiting middleware for IP {IpAddress}", ipAddress);
            // On error, allow the request to proceed to avoid blocking legitimate traffic
            await _next(context);
        }
    }

    /// <summary>
    /// Checks if request is within rate limits
    /// </summary>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="userId">User ID if authenticated</param>
    /// <param name="path">Request path</param>
    /// <param name="context">HTTP context</param>
    /// <returns>Rate limit check result</returns>
    private RateLimitResult CheckRateLimit(string? ipAddress, string? userId, string path, HttpContext context)
    {
        var now = DateTimeOffset.UtcNow;

        // Check IP-based rate limits (most restrictive)
        if (!string.IsNullOrEmpty(ipAddress))
        {
            // General requests per minute by IP
            var ipKey = $"rate_limit_ip_{ipAddress}";
            var ipRequests = GetRequestCount(ipKey, TimeSpan.FromMinutes(1));

            if (ipRequests >= _options.RequestsPerMinutePerIp)
            {
                return new RateLimitResult
                {
                    IsAllowed = false,
                    LimitType = "IP",
                    Limit = _options.RequestsPerMinutePerIp,
                    Current = ipRequests,
                    ResetTime = now.AddMinutes(1)
                };
            }

            // Check authentication endpoint limits (more restrictive)
            if (IsAuthenticationEndpoint(path))
            {
                var authKey = $"rate_limit_auth_ip_{ipAddress}";
                var authRequests = GetRequestCount(authKey, TimeSpan.FromMinutes(1));

                if (authRequests >= _options.AuthRequestsPerMinutePerIp)
                {
                    return new RateLimitResult
                    {
                        IsAllowed = false,
                        LimitType = "Auth_IP",
                        Limit = _options.AuthRequestsPerMinutePerIp,
                        Current = authRequests,
                        ResetTime = now.AddMinutes(1)
                    };
                }
            }

            // Check API endpoint limits
            if (IsApiEndpoint(path))
            {
                var apiKey = $"rate_limit_api_ip_{ipAddress}";
                var apiRequests = GetRequestCount(apiKey, TimeSpan.FromMinutes(1));

                if (apiRequests >= _options.ApiRequestsPerMinutePerIp)
                {
                    return new RateLimitResult
                    {
                        IsAllowed = false,
                        LimitType = "API_IP",
                        Limit = _options.ApiRequestsPerMinutePerIp,
                        Current = apiRequests,
                        ResetTime = now.AddMinutes(1)
                    };
                }
            }
        }

        // Check user-based rate limits (if authenticated)
        if (!string.IsNullOrEmpty(userId))
        {
            var userKey = $"rate_limit_user_{userId}";
            var userRequests = GetRequestCount(userKey, TimeSpan.FromMinutes(1));

            // Authenticated users get higher limits
            var userLimit = _options.RequestsPerMinutePerUser;
            if (userRequests >= userLimit)
            {
                return new RateLimitResult
                {
                    IsAllowed = false,
                    LimitType = "User",
                    Limit = userLimit,
                    Current = userRequests,
                    ResetTime = now.AddMinutes(1)
                };
            }
        }

        return new RateLimitResult { IsAllowed = true };
    }

    /// <summary>
    /// Gets current request count for a cache key within the specified window
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="window">Time window</param>
    /// <returns>Request count</returns>
    private int GetRequestCount(string key, TimeSpan window)
    {
        var windowKey = $"{key}_{DateTimeOffset.UtcNow.Ticks / window.Ticks}";

        if (_cache.TryGetValue(windowKey, out var count) && count is int intCount)
        {
            return intCount;
        }

        return 0;
    }

    /// <summary>
    /// Updates request counters for tracking
    /// </summary>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="userId">User ID if authenticated</param>
    /// <param name="path">Request path</param>
    private void UpdateRequestCounters(string? ipAddress, string? userId, string path)
    {
        var window = TimeSpan.FromMinutes(1);
        var windowTicks = DateTimeOffset.UtcNow.Ticks / window.Ticks;

        // Update IP counters
        if (!string.IsNullOrEmpty(ipAddress))
        {
            UpdateCounter($"rate_limit_ip_{ipAddress}_{windowTicks}", window);

            if (IsAuthenticationEndpoint(path))
            {
                UpdateCounter($"rate_limit_auth_ip_{ipAddress}_{windowTicks}", window);
            }

            if (IsApiEndpoint(path))
            {
                UpdateCounter($"rate_limit_api_ip_{ipAddress}_{windowTicks}", window);
            }
        }

        // Update user counters
        if (!string.IsNullOrEmpty(userId))
        {
            UpdateCounter($"rate_limit_user_{userId}_{windowTicks}", window);
        }
    }

    /// <summary>
    /// Updates a counter in the cache
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="expiration">Expiration time</param>
    private void UpdateCounter(string key, TimeSpan expiration)
    {
        var count = _cache.GetOrCreate(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = expiration.Add(TimeSpan.FromMinutes(1)); // Buffer
            return 0;
        });

        _cache.Set(key, count + 1, expiration.Add(TimeSpan.FromMinutes(1)));
    }

    /// <summary>
    /// Handles rate limit exceeded scenario
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <param name="result">Rate limit result</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="userId">User ID if available</param>
    /// <returns>Task</returns>
    private async Task HandleRateLimitExceeded(
        HttpContext context,
        RateLimitResult result,
        string? ipAddress,
        string? userId)
    {
        _logger.LogWarning("Rate limit exceeded for {LimitType}: IP {IpAddress}, User {UserId}. " +
                          "Current: {Current}, Limit: {Limit}",
                          result.LimitType, ipAddress, userId, result.Current, result.Limit);

        // Log security event
        _securityLogger.LogSuspiciousActivity(
            "RateLimitExceeded",
            $"Rate limit exceeded for {result.LimitType}: {result.Current}/{result.Limit} requests",
            ipAddress,
            userId,
            60, // Risk score
            new Dictionary<string, object>
            {
                ["LimitType"] = result.LimitType,
                ["Current"] = result.Current,
                ["Limit"] = result.Limit,
                ["ResetTime"] = result.ResetTime
            });

        // Set response headers
        context.Response.Headers["X-RateLimit-Limit"] = result.Limit.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, result.Limit - result.Current).ToString();
        context.Response.Headers["X-RateLimit-Reset"] = result.ResetTime.ToUnixTimeSeconds().ToString();
        context.Response.Headers["Retry-After"] = "60"; // 1 minute

        // Return 429 Too Many Requests
        context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
        context.Response.ContentType = "application/json";

        var response = System.Text.Json.JsonSerializer.Serialize(new
        {
            error = "Rate limit exceeded",
            message = $"Too many requests. Limit: {result.Limit} per minute.",
            retryAfter = 60,
            resetTime = result.ResetTime.ToUnixTimeSeconds()
        });

        await context.Response.WriteAsync(response);
    }

    /// <summary>
    /// Determines if the path is an authentication endpoint
    /// </summary>
    /// <param name="path">Request path</param>
    /// <returns>True if authentication endpoint</returns>
    private static bool IsAuthenticationEndpoint(string path)
    {
        var authPaths = new[]
        {
            "/api/auth/login",
            "/api/auth/token",
            "/api/auth/refresh",
            "/login",
            "/signin"
        };

        return authPaths.Any(authPath => path.StartsWith(authPath, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Determines if the path is an API endpoint
    /// </summary>
    /// <param name="path">Request path</param>
    /// <returns>True if API endpoint</returns>
    private static bool IsApiEndpoint(string path)
    {
        return path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets client IP address from context
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>Client IP address</returns>
    private static string? GetClientIpAddress(HttpContext context)
    {
        return context.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim() ??
               context.Request.Headers["X-Real-IP"].FirstOrDefault() ??
               context.Connection.RemoteIpAddress?.ToString();
    }
}

/// <summary>
/// Rate limiting configuration options
/// </summary>
public class RateLimitingOptions
{
    /// <summary>
    /// Whether rate limiting is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum requests per minute per IP address
    /// </summary>
    public int RequestsPerMinutePerIp { get; set; } = 100;

    /// <summary>
    /// Maximum authentication requests per minute per IP address
    /// </summary>
    public int AuthRequestsPerMinutePerIp { get; set; } = 10;

    /// <summary>
    /// Maximum API requests per minute per IP address
    /// </summary>
    public int ApiRequestsPerMinutePerIp { get; set; } = 200;

    /// <summary>
    /// Maximum requests per minute per authenticated user (higher limit)
    /// </summary>
    public int RequestsPerMinutePerUser { get; set; } = 500;
}

/// <summary>
/// Result of rate limit check
/// </summary>
public class RateLimitResult
{
    /// <summary>
    /// Whether the request is allowed
    /// </summary>
    public bool IsAllowed { get; set; }

    /// <summary>
    /// Type of limit that was exceeded
    /// </summary>
    public string LimitType { get; set; } = string.Empty;

    /// <summary>
    /// Current request count
    /// </summary>
    public int Current { get; set; }

    /// <summary>
    /// Maximum allowed requests
    /// </summary>
    public int Limit { get; set; }

    /// <summary>
    /// When the rate limit resets
    /// </summary>
    public DateTimeOffset ResetTime { get; set; }
}
