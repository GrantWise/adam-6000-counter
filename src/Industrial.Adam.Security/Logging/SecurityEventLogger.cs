using System.Collections.Concurrent;
using System.Diagnostics;
using Industrial.Adam.Security.Models;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Security.Logging;

/// <summary>
/// Centralized security event logger with correlation ID tracking
/// Following Logger module patterns for consistency
/// </summary>
public class SecurityEventLogger
{
    private readonly ILogger<SecurityEventLogger> _logger;
    private readonly ConcurrentQueue<SecurityEvent> _eventQueue;
    private readonly ConcurrentDictionary<string, SecurityMetrics> _metricsCache;
    private readonly Timer _metricsUpdateTimer;
    private readonly object _metricsLock = new();

    /// <summary>
    /// Event fired when a security event is logged
    /// </summary>
    public event EventHandler<SecurityEvent>? SecurityEventLogged;

    /// <summary>
    /// Current security metrics
    /// </summary>
    public SecurityMetrics CurrentMetrics { get; private set; }

    public SecurityEventLogger(ILogger<SecurityEventLogger> logger)
    {
        _logger = logger;
        _eventQueue = new ConcurrentQueue<SecurityEvent>();
        _metricsCache = new ConcurrentDictionary<string, SecurityMetrics>();
        CurrentMetrics = new SecurityMetrics
        {
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddHours(1)
        };

        // Update metrics every 30 seconds
        _metricsUpdateTimer = new Timer(UpdateMetrics, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Logs a security event with structured logging and correlation ID
    /// </summary>
    /// <param name="securityEvent">Security event to log</param>
    public void LogSecurityEvent(SecurityEvent securityEvent)
    {
        // Ensure correlation ID is set
        if (string.IsNullOrEmpty(securityEvent.CorrelationId))
        {
            securityEvent.CorrelationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        }

        // Add to queue for metrics processing
        _eventQueue.Enqueue(securityEvent);

        // Log structured event
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = securityEvent.CorrelationId,
            ["EventId"] = securityEvent.EventId,
            ["EventType"] = securityEvent.EventType.ToString(),
            ["Severity"] = securityEvent.Severity.ToString(),
            ["RiskScore"] = securityEvent.RiskScore,
            ["Username"] = securityEvent.Username ?? "Anonymous",
            ["IpAddress"] = securityEvent.IpAddress ?? "Unknown",
            ["Resource"] = securityEvent.Resource ?? "Unknown"
        });

        var logLevel = GetLogLevel(securityEvent.Severity);
        _logger.Log(logLevel, "SecurityEvent: {Description} | {EventDetails}",
            securityEvent.Description,
            securityEvent.ToJson());

        // Fire event for subscribers
        SecurityEventLogged?.Invoke(this, securityEvent);
    }

    /// <summary>
    /// Logs authentication attempt
    /// </summary>
    /// <param name="username">Username attempting authentication</param>
    /// <param name="success">Whether authentication succeeded</param>
    /// <param name="ipAddress">IP address of the request</param>
    /// <param name="userAgent">User agent string</param>
    /// <param name="additionalMetadata">Additional context data</param>
    public void LogAuthenticationAttempt(
        string username,
        bool success,
        string? ipAddress,
        string? userAgent,
        Dictionary<string, object>? additionalMetadata = null)
    {
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        var securityEvent = SecurityEvent.CreateAuthenticationEvent(
            username, success, ipAddress, userAgent, correlationId);

        if (additionalMetadata != null)
        {
            foreach (var kvp in additionalMetadata)
            {
                securityEvent.Metadata[kvp.Key] = kvp.Value;
            }
        }

        LogSecurityEvent(securityEvent);
    }

    /// <summary>
    /// Logs authorization failure
    /// </summary>
    /// <param name="username">Username attempting access</param>
    /// <param name="resource">Resource being accessed</param>
    /// <param name="requiredRole">Required role for access</param>
    /// <param name="userRole">User's actual role</param>
    /// <param name="httpMethod">HTTP method</param>
    /// <param name="ipAddress">IP address of the request</param>
    public void LogAuthorizationFailure(
        string? username,
        string resource,
        string requiredRole,
        string? userRole,
        string? httpMethod,
        string? ipAddress)
    {
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        var securityEvent = SecurityEvent.CreateAuthorizationFailureEvent(
            username, resource, requiredRole, userRole, ipAddress, correlationId);

        securityEvent.HttpMethod = httpMethod;
        LogSecurityEvent(securityEvent);
    }

    /// <summary>
    /// Logs input validation failure
    /// </summary>
    /// <param name="fieldName">Field that failed validation</param>
    /// <param name="value">Value that failed (sanitized)</param>
    /// <param name="validationError">Validation error message</param>
    /// <param name="ipAddress">IP address of the request</param>
    /// <param name="username">Username if available</param>
    public void LogValidationFailure(
        string fieldName,
        string? value,
        string validationError,
        string? ipAddress,
        string? username = null)
    {
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        var securityEvent = new SecurityEvent
        {
            CorrelationId = correlationId,
            EventType = SecurityEventType.ValidationFailure,
            Severity = SecurityEventSeverity.Warning,
            Username = username,
            IpAddress = ipAddress,
            Description = $"Input validation failed for field '{fieldName}': {validationError}",
            RiskScore = 15,
            Metadata = new Dictionary<string, object>
            {
                ["FieldName"] = fieldName,
                ["ValidationError"] = validationError,
                ["SanitizedValue"] = SanitizeValue(value)
            }
        };

        LogSecurityEvent(securityEvent);
    }

    /// <summary>
    /// Logs suspicious activity
    /// </summary>
    /// <param name="activityType">Type of suspicious activity</param>
    /// <param name="description">Description of the activity</param>
    /// <param name="ipAddress">IP address involved</param>
    /// <param name="username">Username if available</param>
    /// <param name="riskScore">Risk score (0-100)</param>
    /// <param name="additionalMetadata">Additional context data</param>
    public void LogSuspiciousActivity(
        string activityType,
        string description,
        string? ipAddress,
        string? username = null,
        int riskScore = 50,
        Dictionary<string, object>? additionalMetadata = null)
    {
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        var securityEvent = SecurityEvent.CreateSuspiciousActivityEvent(
            activityType, description, ipAddress, correlationId, riskScore);

        securityEvent.Username = username;

        if (additionalMetadata != null)
        {
            foreach (var kvp in additionalMetadata)
            {
                securityEvent.Metadata[kvp.Key] = kvp.Value;
            }
        }

        LogSecurityEvent(securityEvent);
    }

    /// <summary>
    /// Logs configuration change
    /// </summary>
    /// <param name="configurationKey">Configuration key that changed</param>
    /// <param name="oldValue">Previous value (sanitized)</param>
    /// <param name="newValue">New value (sanitized)</param>
    /// <param name="username">User who made the change</param>
    /// <param name="ipAddress">IP address of the request</param>
    public void LogConfigurationChange(
        string configurationKey,
        string? oldValue,
        string? newValue,
        string username,
        string? ipAddress)
    {
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        var securityEvent = new SecurityEvent
        {
            CorrelationId = correlationId,
            EventType = SecurityEventType.ConfigurationChange,
            Severity = SecurityEventSeverity.Medium,
            Username = username,
            IpAddress = ipAddress,
            Description = $"Configuration changed: {configurationKey}",
            RiskScore = 20,
            Metadata = new Dictionary<string, object>
            {
                ["ConfigurationKey"] = configurationKey,
                ["OldValue"] = SanitizeValue(oldValue),
                ["NewValue"] = SanitizeValue(newValue),
                ["ChangeType"] = "Configuration"
            }
        };

        LogSecurityEvent(securityEvent);
    }

    /// <summary>
    /// Gets security metrics for a specific time period
    /// </summary>
    /// <param name="startTime">Start of time period</param>
    /// <param name="endTime">End of time period</param>
    /// <returns>Security metrics</returns>
    public SecurityMetrics GetMetrics(DateTimeOffset? startTime = null, DateTimeOffset? endTime = null)
    {
        startTime ??= DateTimeOffset.UtcNow.AddHours(-1);
        endTime ??= DateTimeOffset.UtcNow;

        lock (_metricsLock)
        {
            var metrics = new SecurityMetrics
            {
                StartTime = startTime.Value,
                EndTime = endTime.Value
            };

            // Process events in queue
            var events = _eventQueue.ToArray()
                .Where(e => e.Timestamp >= startTime && e.Timestamp <= endTime)
                .ToList();

            foreach (var evt in events)
            {
                ProcessEventForMetrics(evt, metrics);
            }

            metrics.CalculateHealthScore();
            return metrics;
        }
    }

    /// <summary>
    /// Converts security event severity to log level
    /// </summary>
    /// <param name="severity">Security event severity</param>
    /// <returns>Log level</returns>
    private static LogLevel GetLogLevel(SecurityEventSeverity severity)
    {
        return severity switch
        {
            SecurityEventSeverity.Information => LogLevel.Information,
            SecurityEventSeverity.Low => LogLevel.Information,
            SecurityEventSeverity.Warning => LogLevel.Warning,
            SecurityEventSeverity.Medium => LogLevel.Warning,
            SecurityEventSeverity.High => LogLevel.Error,
            SecurityEventSeverity.Critical => LogLevel.Critical,
            _ => LogLevel.Information
        };
    }

    /// <summary>
    /// Sanitizes sensitive values for logging
    /// </summary>
    /// <param name="value">Value to sanitize</param>
    /// <returns>Sanitized value</returns>
    private static string SanitizeValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "[Empty]";

        if (value.Length <= 10)
            return "[Redacted]";

        return $"{value[..3]}***{value[^3..]}";
    }

    /// <summary>
    /// Processes an event for metrics collection
    /// </summary>
    /// <param name="evt">Security event</param>
    /// <param name="metrics">Metrics to update</param>
    private static void ProcessEventForMetrics(SecurityEvent evt, SecurityMetrics metrics)
    {
        // Count by event type
        if (!metrics.EventsByType.ContainsKey(evt.EventType))
            metrics.EventsByType[evt.EventType] = 0;
        metrics.EventsByType[evt.EventType]++;

        // Count by severity
        if (!metrics.EventsBySeverity.ContainsKey(evt.Severity))
            metrics.EventsBySeverity[evt.Severity] = 0;
        metrics.EventsBySeverity[evt.Severity]++;

        // Track authentication events
        if (evt.EventType == SecurityEventType.AuthenticationSuccess)
        {
            metrics.AuthenticationSuccesses++;
            metrics.AuthenticationAttempts++;
        }
        else if (evt.EventType == SecurityEventType.AuthenticationFailure)
        {
            metrics.AuthenticationFailures++;
            metrics.AuthenticationAttempts++;
        }

        // Track other event types
        if (evt.EventType == SecurityEventType.AuthorizationFailure)
            metrics.AuthorizationFailures++;

        if (evt.EventType == SecurityEventType.ValidationFailure)
            metrics.ValidationFailures++;

        if (evt.EventType == SecurityEventType.SuspiciousActivity)
            metrics.SuspiciousActivities++;

        // Track high-risk and critical events
        if (evt.RiskScore >= 70)
            metrics.HighRiskEvents++;

        if (evt.Severity == SecurityEventSeverity.Critical)
            metrics.CriticalEvents++;

        // Track top IP addresses
        if (!string.IsNullOrEmpty(evt.IpAddress))
        {
            if (!metrics.TopIpAddresses.ContainsKey(evt.IpAddress))
                metrics.TopIpAddresses[evt.IpAddress] = 0;
            metrics.TopIpAddresses[evt.IpAddress]++;
        }

        // Track top users
        if (!string.IsNullOrEmpty(evt.Username))
        {
            if (!metrics.TopUsers.ContainsKey(evt.Username))
                metrics.TopUsers[evt.Username] = 0;
            metrics.TopUsers[evt.Username]++;
        }
    }

    /// <summary>
    /// Timer callback to update metrics
    /// </summary>
    /// <param name="state">Timer state</param>
    private void UpdateMetrics(object? state)
    {
        try
        {
            CurrentMetrics = GetMetrics();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating security metrics");
        }
    }

    /// <summary>
    /// Disposes the security event logger
    /// </summary>
    public void Dispose()
    {
        _metricsUpdateTimer?.Dispose();
    }
}
