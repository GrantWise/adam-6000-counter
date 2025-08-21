using System.Collections.Concurrent;
using Industrial.Adam.Security.Logging;
using Industrial.Adam.Security.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Industrial.Adam.Security.Monitoring;

/// <summary>
/// Background service for security monitoring and alerting
/// </summary>
public class SecurityMonitoringService : BackgroundService
{
    private readonly ILogger<SecurityMonitoringService> _logger;
    private readonly SecurityEventLogger _securityLogger;
    private readonly SecurityMonitoringOptions _options;
    private readonly ConcurrentDictionary<string, int> _ipAddressAttempts;
    private readonly ConcurrentDictionary<string, int> _usernameAttempts;
    private readonly ConcurrentQueue<SecurityEvent> _recentEvents;
    private readonly Timer _cleanupTimer;
    private const int MaxEventsInMemory = 10000; // Maximum events to keep in memory
    private const int MaxAttemptsEntries = 5000; // Maximum IP/username entries to track

    public SecurityMonitoringService(
        ILogger<SecurityMonitoringService> logger,
        SecurityEventLogger securityLogger,
        IOptions<SecurityMonitoringOptions> options)
    {
        _logger = logger;
        _securityLogger = securityLogger;
        _options = options.Value;
        _ipAddressAttempts = new ConcurrentDictionary<string, int>();
        _usernameAttempts = new ConcurrentDictionary<string, int>();
        _recentEvents = new ConcurrentQueue<SecurityEvent>();

        // Subscribe to security events
        _securityLogger.SecurityEventLogged += OnSecurityEventLogged;

        // Cleanup timer to reset counters periodically
        _cleanupTimer = new Timer(CleanupCounters, null, TimeSpan.Zero, TimeSpan.FromMinutes(15));
    }

    /// <summary>
    /// Main monitoring loop
    /// </summary>
    /// <param name="stoppingToken">Cancellation token</param>
    /// <returns>Task</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Security monitoring service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Check for security threats
                await CheckForThreats(stoppingToken);

                // Generate security alerts
                await GenerateAlerts(stoppingToken);

                // Update security metrics
                UpdateMetrics();

                // Wait for next check interval
                await Task.Delay(_options.CheckInterval, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error in security monitoring service");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        _logger.LogInformation("Security monitoring service stopped");
    }

    /// <summary>
    /// Handles security events as they are logged
    /// </summary>
    /// <param name="sender">Event sender</param>
    /// <param name="securityEvent">Security event</param>
    private void OnSecurityEventLogged(object? sender, SecurityEvent securityEvent)
    {
        try
        {
            // Add to recent events queue with bounds checking
            _recentEvents.Enqueue(securityEvent);

            // Enforce memory bounds with LRU eviction
            EnforceEventMemoryBounds();

            // Track authentication failures by IP and username with bounds checking
            if (securityEvent.EventType == SecurityEventType.AuthenticationFailure)
            {
                if (!string.IsNullOrEmpty(securityEvent.IpAddress))
                {
                    TrackAttemptWithBounds(_ipAddressAttempts, securityEvent.IpAddress);
                }

                if (!string.IsNullOrEmpty(securityEvent.Username))
                {
                    TrackAttemptWithBounds(_usernameAttempts, securityEvent.Username);
                }
            }

            // Immediate threat detection
            DetectImmediateThreats(securityEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing security event");
        }
    }

    /// <summary>
    /// Checks for security threats based on patterns
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    private async Task CheckForThreats(CancellationToken cancellationToken)
    {
        // Check for brute force attacks by IP address
        foreach (var kvp in _ipAddressAttempts.ToArray())
        {
            if (kvp.Value >= _options.BruteForceThreshold)
            {
                _logger.LogWarning("Potential brute force attack detected from IP: {IpAddress} ({Attempts} attempts)",
                    kvp.Key, kvp.Value);

                _securityLogger.LogSuspiciousActivity(
                    "BruteForceAttack",
                    $"Brute force attack detected from IP {kvp.Key} with {kvp.Value} failed attempts",
                    kvp.Key,
                    null,
                    90,
                    new Dictionary<string, object>
                    {
                        ["AttackType"] = "BruteForce",
                        ["FailedAttempts"] = kvp.Value,
                        ["TimeWindow"] = "15 minutes"
                    });

                // Reset counter after reporting
                _ipAddressAttempts.TryRemove(kvp.Key, out _);
            }
        }

        // Check for credential stuffing attacks by username
        foreach (var kvp in _usernameAttempts.ToArray())
        {
            if (kvp.Value >= _options.CredentialStuffingThreshold)
            {
                _logger.LogWarning("Potential credential stuffing attack detected for user: {Username} ({Attempts} attempts)",
                    kvp.Key, kvp.Value);

                _securityLogger.LogSuspiciousActivity(
                    "CredentialStuffing",
                    $"Credential stuffing attack detected for user {kvp.Key} with {kvp.Value} failed attempts",
                    null,
                    kvp.Key,
                    85,
                    new Dictionary<string, object>
                    {
                        ["AttackType"] = "CredentialStuffing",
                        ["FailedAttempts"] = kvp.Value,
                        ["TimeWindow"] = "15 minutes"
                    });

                // Reset counter after reporting
                _usernameAttempts.TryRemove(kvp.Key, out _);
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Detects immediate threats from individual events
    /// </summary>
    /// <param name="securityEvent">Security event to analyze</param>
    private void DetectImmediateThreats(SecurityEvent securityEvent)
    {
        // High-risk events trigger immediate alerts
        if (securityEvent.RiskScore >= 80)
        {
            _logger.LogWarning("High-risk security event detected: {Description} (Risk Score: {RiskScore})",
                securityEvent.Description, securityEvent.RiskScore);
        }

        // Multiple failed attempts from same IP in short time
        if (securityEvent.EventType == SecurityEventType.AuthenticationFailure &&
            !string.IsNullOrEmpty(securityEvent.IpAddress))
        {
            var recentFailures = _recentEvents
                .Where(e => e.EventType == SecurityEventType.AuthenticationFailure &&
                           e.IpAddress == securityEvent.IpAddress &&
                           e.Timestamp > DateTimeOffset.UtcNow.AddMinutes(-5))
                .Count();

            if (recentFailures >= _options.RapidFailureThreshold)
            {
                _securityLogger.LogSuspiciousActivity(
                    "RapidFailures",
                    $"Rapid authentication failures detected from IP {securityEvent.IpAddress} ({recentFailures} failures in 5 minutes)",
                    securityEvent.IpAddress,
                    securityEvent.Username,
                    75,
                    new Dictionary<string, object>
                    {
                        ["FailureCount"] = recentFailures,
                        ["TimeWindow"] = "5 minutes",
                        ["ThreatType"] = "RapidFailures"
                    });
            }
        }

        // Suspicious patterns in input validation failures
        if (securityEvent.EventType == SecurityEventType.ValidationFailure &&
            securityEvent.Description.Contains("SQL", StringComparison.OrdinalIgnoreCase))
        {
            _securityLogger.LogSuspiciousActivity(
                "SQLInjectionAttempt",
                "Potential SQL injection attempt detected in validation failure",
                securityEvent.IpAddress,
                securityEvent.Username,
                85);
        }
    }

    /// <summary>
    /// Generates security alerts based on patterns
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    private async Task GenerateAlerts(CancellationToken cancellationToken)
    {
        var metrics = _securityLogger.GetMetrics(
            DateTimeOffset.UtcNow.AddMinutes(-_options.AlertWindowMinutes),
            DateTimeOffset.UtcNow);

        // Generate alert for high failure rate
        if (metrics.AuthenticationAttempts > 10 &&
            metrics.AuthenticationSuccessRate < _options.MinSuccessRate)
        {
            _logger.LogWarning("Low authentication success rate detected: {SuccessRate:F1}%",
                metrics.AuthenticationSuccessRate);

            var alert = new SecurityAlert
            {
                Title = "Low Authentication Success Rate",
                Description = $"Authentication success rate is {metrics.AuthenticationSuccessRate:F1}% " +
                             $"({metrics.AuthenticationSuccesses}/{metrics.AuthenticationAttempts} attempts)",
                Severity = SecurityEventSeverity.Medium,
                Category = "Authentication"
            };

            metrics.ActiveAlerts.Add(alert);
        }

        // Generate alert for high-risk events
        if (metrics.HighRiskEvents > _options.HighRiskEventThreshold)
        {
            _logger.LogWarning("High number of high-risk security events: {Count}", metrics.HighRiskEvents);

            var alert = new SecurityAlert
            {
                Title = "Multiple High-Risk Events",
                Description = $"{metrics.HighRiskEvents} high-risk security events detected in the last {_options.AlertWindowMinutes} minutes",
                Severity = SecurityEventSeverity.High,
                Category = "ThreatDetection"
            };

            metrics.ActiveAlerts.Add(alert);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Updates security metrics
    /// </summary>
    private void UpdateMetrics()
    {
        var metrics = _securityLogger.CurrentMetrics;

        _logger.LogDebug("Security metrics updated - Success Rate: {SuccessRate:F1}%, " +
                        "High-Risk Events: {HighRiskEvents}, Health Score: {HealthScore}",
            metrics.AuthenticationSuccessRate,
            metrics.HighRiskEvents,
            metrics.HealthScore);
    }

    /// <summary>
    /// Enforces memory bounds on event queue using LRU eviction
    /// </summary>
    private void EnforceEventMemoryBounds()
    {
        // Remove old events based on time (last hour)
        var cutoffTime = DateTimeOffset.UtcNow.AddHours(-1);
        var removedByTime = 0;

        while (_recentEvents.TryPeek(out var oldEvent) && oldEvent.Timestamp < cutoffTime)
        {
            if (_recentEvents.TryDequeue(out _))
                removedByTime++;
        }

        // Remove oldest events if still over limit (LRU eviction)
        var removedByCount = 0;
        while (_recentEvents.Count > MaxEventsInMemory)
        {
            if (_recentEvents.TryDequeue(out _))
                removedByCount++;
        }

        if (removedByTime > 0 || removedByCount > 0)
        {
            _logger.LogDebug("Evicted {TimeRemoved} old events and {CountRemoved} events due to memory limits. Current count: {CurrentCount}",
                removedByTime, removedByCount, _recentEvents.Count);
        }
    }

    /// <summary>
    /// Tracks attempt with bounds checking and LRU eviction
    /// </summary>
    /// <param name="dictionary">Dictionary to track attempts in</param>
    /// <param name="key">Key to track</param>
    private void TrackAttemptWithBounds(ConcurrentDictionary<string, int> dictionary, string key)
    {
        // Enforce bounds on tracking dictionaries
        if (dictionary.Count >= MaxAttemptsEntries)
        {
            // Remove some entries (simple cleanup - remove entries with low counts)
            var entriesToRemove = dictionary
                .Where(kvp => kvp.Value <= 2)
                .Take(MaxAttemptsEntries / 4) // Remove 25% of max capacity
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var entryKey in entriesToRemove)
            {
                dictionary.TryRemove(entryKey, out _);
            }

            _logger.LogDebug("Cleaned up {RemovedCount} low-activity entries from attempt tracking. Current count: {CurrentCount}",
                entriesToRemove.Count, dictionary.Count);
        }

        dictionary.AddOrUpdate(key, 1, (k, value) => value + 1);
    }

    /// <summary>
    /// Cleans up old counters and data
    /// </summary>
    /// <param name="state">Timer state</param>
    private void CleanupCounters(object? state)
    {
        try
        {
            var ipCountBefore = _ipAddressAttempts.Count;
            var usernameCountBefore = _usernameAttempts.Count;
            var eventCountBefore = _recentEvents.Count;

            // Clean up attempts with lower counts (keep high-risk IPs/users)
            var ipKeysToRemove = _ipAddressAttempts
                .Where(kvp => kvp.Value < _options.BruteForceThreshold / 2)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in ipKeysToRemove)
            {
                _ipAddressAttempts.TryRemove(key, out _);
            }

            var usernameKeysToRemove = _usernameAttempts
                .Where(kvp => kvp.Value < _options.CredentialStuffingThreshold / 2)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in usernameKeysToRemove)
            {
                _usernameAttempts.TryRemove(key, out _);
            }

            // Enforce bounds on events
            EnforceEventMemoryBounds();

            var ipCountAfter = _ipAddressAttempts.Count;
            var usernameCountAfter = _usernameAttempts.Count;
            var eventCountAfter = _recentEvents.Count;

            _logger.LogDebug("Security monitoring cleanup completed. " +
                           "IP attempts: {IpBefore} -> {IpAfter}, " +
                           "Username attempts: {UserBefore} -> {UserAfter}, " +
                           "Events: {EventBefore} -> {EventAfter}",
                           ipCountBefore, ipCountAfter,
                           usernameCountBefore, usernameCountAfter,
                           eventCountBefore, eventCountAfter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up security monitoring counters");
        }
    }

    /// <summary>
    /// Gets current security status
    /// </summary>
    /// <returns>Security status</returns>
    public SecurityStatus GetSecurityStatus()
    {
        var metrics = _securityLogger.CurrentMetrics;

        return new SecurityStatus
        {
            HealthStatus = metrics.HealthStatus,
            HealthScore = metrics.HealthScore,
            ActiveAlerts = metrics.ActiveAlerts,
            ThreatLevel = DetermineThreatLevel(metrics),
            LastUpdated = DateTimeOffset.UtcNow,
            MonitoringActive = true
        };
    }

    /// <summary>
    /// Determines current threat level based on metrics
    /// </summary>
    /// <param name="metrics">Security metrics</param>
    /// <returns>Threat level</returns>
    private static ThreatLevel DetermineThreatLevel(SecurityMetrics metrics)
    {
        if (metrics.CriticalEvents > 0 || metrics.HealthScore < 50)
            return ThreatLevel.Critical;

        if (metrics.HighRiskEvents > 5 || metrics.HealthScore < 70)
            return ThreatLevel.High;

        if (metrics.SuspiciousActivities > 10 || metrics.HealthScore < 80)
            return ThreatLevel.Medium;

        if (metrics.AuthenticationFailures > 20 || metrics.HealthScore < 90)
            return ThreatLevel.Low;

        return ThreatLevel.Minimal;
    }

    public override void Dispose()
    {
        _cleanupTimer?.Dispose();
        _securityLogger.SecurityEventLogged -= OnSecurityEventLogged;
        base.Dispose();
    }
}

/// <summary>
/// Options for security monitoring service
/// </summary>
public class SecurityMonitoringOptions
{
    /// <summary>
    /// Interval between monitoring checks
    /// </summary>
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Threshold for brute force attack detection (failures per IP)
    /// </summary>
    public int BruteForceThreshold { get; set; } = 10;

    /// <summary>
    /// Threshold for credential stuffing attack detection (failures per username)
    /// </summary>
    public int CredentialStuffingThreshold { get; set; } = 15;

    /// <summary>
    /// Threshold for rapid failure detection (failures in 5 minutes)
    /// </summary>
    public int RapidFailureThreshold { get; set; } = 5;

    /// <summary>
    /// Minimum authentication success rate before alerting (percentage)
    /// </summary>
    public double MinSuccessRate { get; set; } = 70.0;

    /// <summary>
    /// Threshold for high-risk event alerting
    /// </summary>
    public int HighRiskEventThreshold { get; set; } = 5;

    /// <summary>
    /// Time window for alerts in minutes
    /// </summary>
    public int AlertWindowMinutes { get; set; } = 30;
}

/// <summary>
/// Current security status
/// </summary>
public class SecurityStatus
{
    /// <summary>
    /// Overall security health status
    /// </summary>
    public SecurityHealthStatus HealthStatus { get; set; }

    /// <summary>
    /// Security health score (0-100)
    /// </summary>
    public int HealthScore { get; set; }

    /// <summary>
    /// Current threat level
    /// </summary>
    public ThreatLevel ThreatLevel { get; set; }

    /// <summary>
    /// Active security alerts
    /// </summary>
    public List<SecurityAlert> ActiveAlerts { get; set; } = new();

    /// <summary>
    /// Whether monitoring is active
    /// </summary>
    public bool MonitoringActive { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTimeOffset LastUpdated { get; set; }
}

/// <summary>
/// Threat level enumeration
/// </summary>
public enum ThreatLevel
{
    /// <summary>
    /// Minimal threat level
    /// </summary>
    Minimal,

    /// <summary>
    /// Low threat level
    /// </summary>
    Low,

    /// <summary>
    /// Medium threat level
    /// </summary>
    Medium,

    /// <summary>
    /// High threat level
    /// </summary>
    High,

    /// <summary>
    /// Critical threat level
    /// </summary>
    Critical
}
