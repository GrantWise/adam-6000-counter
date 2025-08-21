namespace Industrial.Adam.Security.Models;

/// <summary>
/// Security metrics for monitoring and reporting
/// </summary>
public class SecurityMetrics
{
    /// <summary>
    /// Total number of authentication attempts in the time period
    /// </summary>
    public long AuthenticationAttempts { get; set; }

    /// <summary>
    /// Number of successful authentications
    /// </summary>
    public long AuthenticationSuccesses { get; set; }

    /// <summary>
    /// Number of failed authentications
    /// </summary>
    public long AuthenticationFailures { get; set; }

    /// <summary>
    /// Authentication success rate as percentage
    /// </summary>
    public double AuthenticationSuccessRate =>
        AuthenticationAttempts > 0 ? (double)AuthenticationSuccesses / AuthenticationAttempts * 100 : 0;

    /// <summary>
    /// Number of authorization failures
    /// </summary>
    public long AuthorizationFailures { get; set; }

    /// <summary>
    /// Number of input validation failures
    /// </summary>
    public long ValidationFailures { get; set; }

    /// <summary>
    /// Number of suspicious activities detected
    /// </summary>
    public long SuspiciousActivities { get; set; }

    /// <summary>
    /// Number of high-risk events
    /// </summary>
    public long HighRiskEvents { get; set; }

    /// <summary>
    /// Number of critical security events
    /// </summary>
    public long CriticalEvents { get; set; }

    /// <summary>
    /// Top IP addresses by event count
    /// </summary>
    public Dictionary<string, long> TopIpAddresses { get; set; } = new();

    /// <summary>
    /// Top users by event count
    /// </summary>
    public Dictionary<string, long> TopUsers { get; set; } = new();

    /// <summary>
    /// Event count by type
    /// </summary>
    public Dictionary<SecurityEventType, long> EventsByType { get; set; } = new();

    /// <summary>
    /// Event count by severity
    /// </summary>
    public Dictionary<SecurityEventSeverity, long> EventsBySeverity { get; set; } = new();

    /// <summary>
    /// Time period these metrics cover
    /// </summary>
    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    /// End of time period
    /// </summary>
    public DateTimeOffset EndTime { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Current system security health status
    /// </summary>
    public SecurityHealthStatus HealthStatus { get; set; }

    /// <summary>
    /// Security health score (0-100, higher is better)
    /// </summary>
    public int HealthScore { get; set; }

    /// <summary>
    /// Active security alerts
    /// </summary>
    public List<SecurityAlert> ActiveAlerts { get; set; } = new();

    /// <summary>
    /// Calculates overall security health score
    /// </summary>
    /// <returns>Health score (0-100)</returns>
    public int CalculateHealthScore()
    {
        var score = 100;

        // Deduct points for failures
        var failureRate = AuthenticationAttempts > 0 ?
            (double)AuthenticationFailures / AuthenticationAttempts : 0;

        score -= (int)(failureRate * 30); // Max -30 for 100% auth failures

        // Deduct points for suspicious activities
        if (SuspiciousActivities > 10)
            score -= 20;
        else if (SuspiciousActivities > 5)
            score -= 10;
        else if (SuspiciousActivities > 0)
            score -= 5;

        // Deduct points for high-risk events
        if (HighRiskEvents > 5)
            score -= 30;
        else if (HighRiskEvents > 2)
            score -= 15;
        else if (HighRiskEvents > 0)
            score -= 5;

        // Deduct points for critical events
        if (CriticalEvents > 0)
            score -= 40;

        // Ensure score is within bounds
        HealthScore = Math.Max(0, Math.Min(100, score));

        // Determine health status
        HealthStatus = HealthScore switch
        {
            >= 90 => SecurityHealthStatus.Excellent,
            >= 80 => SecurityHealthStatus.Good,
            >= 70 => SecurityHealthStatus.Warning,
            >= 50 => SecurityHealthStatus.Poor,
            _ => SecurityHealthStatus.Critical
        };

        return HealthScore;
    }
}

/// <summary>
/// Security health status levels
/// </summary>
public enum SecurityHealthStatus
{
    /// <summary>
    /// Excellent security health
    /// </summary>
    Excellent,

    /// <summary>
    /// Good security health
    /// </summary>
    Good,

    /// <summary>
    /// Warning - some security concerns
    /// </summary>
    Warning,

    /// <summary>
    /// Poor security health
    /// </summary>
    Poor,

    /// <summary>
    /// Critical security issues
    /// </summary>
    Critical
}

/// <summary>
/// Active security alert
/// </summary>
public class SecurityAlert
{
    /// <summary>
    /// Alert ID
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Alert title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Alert description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Alert severity
    /// </summary>
    public SecurityEventSeverity Severity { get; set; }

    /// <summary>
    /// When the alert was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Alert category
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Associated IP address
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Associated username
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Number of occurrences
    /// </summary>
    public int Count { get; set; } = 1;
}
