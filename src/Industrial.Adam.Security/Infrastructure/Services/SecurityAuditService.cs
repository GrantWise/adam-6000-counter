using Industrial.Adam.Security.Infrastructure.Repositories;
using Industrial.Adam.Security.Domain.Entities;
using Industrial.Adam.Security.Domain.ValueObjects;
using Industrial.Adam.Security.Application.DTOs;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Industrial.Adam.Security.Infrastructure.Services;

/// <summary>
/// Service for comprehensive security audit and monitoring
/// </summary>
public class SecurityAuditService
{
    private readonly AuditLogRepository _auditRepository;
    private readonly ILogger<SecurityAuditService> _logger;

    public SecurityAuditService(AuditLogRepository auditRepository, ILogger<SecurityAuditService> logger)
    {
        _auditRepository = auditRepository;
        _logger = logger;
    }

    /// <summary>
    /// Gets login attempts with filtering and pagination
    /// </summary>
    public async Task<LoginHistoryResponse> GetLoginAttemptsAsync(string? username = null, bool? success = null,
        DateTimeOffset? startTime = null, DateTimeOffset? endTime = null, int page = 1, int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _auditRepository.GetLoginAttemptsAsync(username, success, startTime, endTime, page, pageSize, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get login attempts");
            throw;
        }
    }

    /// <summary>
    /// Gets complete audit trail with filtering and pagination
    /// </summary>
    public async Task<AuditTrailResponse> GetAuditTrailAsync(int? userId = null, string? action = null, string? entityType = null,
        DateTimeOffset? startTime = null, DateTimeOffset? endTime = null, int page = 1, int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _auditRepository.GetAuditTrailAsync(userId, action, entityType, startTime, endTime, page, pageSize, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get audit trail");
            throw;
        }
    }

    /// <summary>
    /// Gets user activity across the system
    /// </summary>
    public async Task<List<UserActivity>> GetUserActivityAsync(int? userId = null, DateTimeOffset? startTime = null,
        DateTimeOffset? endTime = null, int limit = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            if (userId.HasValue)
            {
                var response = await _auditRepository.GetUserActivityAsync(userId.Value, 1, limit, cancellationToken);
                return response.Activities;
            }

            // For all users, we'd need a different query - simplified for now
            var userResponse = await _auditRepository.GetUserActivityAsync(userId ?? 1, 1, limit, cancellationToken);
            return userResponse.Activities;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user activity");
            throw;
        }
    }

    /// <summary>
    /// Gets all active user sessions
    /// </summary>
    public Task<List<UserSession>> GetActiveSessionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // This would typically query all active sessions across all users
            // For now, return empty list as this requires a more complex query
            return Task.FromResult(new List<UserSession>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active sessions");
            throw;
        }
    }

    /// <summary>
    /// Generates CFR Part 11 compliance report
    /// </summary>
    public async Task<ComplianceReport> GenerateComplianceReportAsync(DateTimeOffset startDate, DateTimeOffset endDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get login attempts for the period
            var loginData = await _auditRepository.GetLoginAttemptsAsync(null, null, startDate, endDate, 1, 10000, cancellationToken);
            
            // Get audit trail for the period
            var auditData = await _auditRepository.GetAuditTrailAsync(null, null, null, startDate, endDate, 1, 10000, cancellationToken);

            // Calculate compliance metrics
            var metrics = new ComplianceMetrics
            {
                TotalLoginAttempts = loginData.TotalCount,
                FailedLoginAttempts = loginData.FailedLogins,
                AuditRecords = auditData.TotalCount,
                SecurityViolations = CalculateSecurityViolations(loginData, auditData),
                ComplianceScore = CalculateComplianceScore(loginData, auditData)
            };

            // Identify compliance violations
            var violations = await IdentifyComplianceViolationsAsync(loginData, auditData, cancellationToken);

            var report = new ComplianceReport
            {
                ReportType = "CFR Part 11 Compliance",
                GeneratedAt = DateTimeOffset.UtcNow,
                StartDate = startDate,
                EndDate = endDate,
                Metrics = metrics,
                Violations = violations
            };

            _logger.LogInformation("Generated compliance report for period {StartDate} to {EndDate}", startDate, endDate);
            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate compliance report");
            throw;
        }
    }

    /// <summary>
    /// Exports audit logs in various formats
    /// </summary>
    public async Task<AuditExportResult> ExportAuditLogsAsync(AuditExportRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get audit data
            var auditData = await _auditRepository.GetAuditTrailAsync(
                request.UserId, 
                request.Action, 
                request.EntityType,
                request.StartDate, 
                request.EndDate, 
                1, 
                10000, // Export limit
                cancellationToken);

            // Get login data
            var loginData = await _auditRepository.GetLoginAttemptsAsync(
                request.Username,
                null,
                request.StartDate,
                request.EndDate,
                1,
                10000,
                cancellationToken);

            var exportData = new
            {
                ExportMetadata = new
                {
                    GeneratedAt = DateTimeOffset.UtcNow,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    Format = request.Format,
                    TotalAuditRecords = auditData.TotalCount,
                    TotalLoginAttempts = loginData.TotalCount
                },
                AuditTrail = auditData.Records,
                LoginAttempts = loginData.LoginAttempts
            };

            // Serialize to JSON (could be extended to support CSV, XML, etc.)
            var exportContent = JsonSerializer.Serialize(exportData, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var result = new AuditExportResult
            {
                Content = exportContent,
                ContentType = request.Format.ToLower() == "csv" ? "text/csv" : "application/json",
                FileName = $"audit_export_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}.{request.Format.ToLower()}",
                RecordCount = auditData.TotalCount + loginData.TotalCount,
                GeneratedAt = DateTimeOffset.UtcNow
            };

            _logger.LogInformation("Exported {RecordCount} audit records in {Format} format", result.RecordCount, request.Format);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export audit logs");
            throw;
        }
    }

    /// <summary>
    /// Logs security events for audit trail
    /// </summary>
    public async Task LogSecurityEventAsync(int userId, string action, string entityType, string entityId,
        object? oldValue, object? newValue, string ipAddress, CancellationToken cancellationToken = default)
    {
        try
        {
            await _auditRepository.LogAuditTrailAsync(userId, action, entityType, entityId, oldValue, newValue, ipAddress, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log security event");
            throw;
        }
    }

    private static int CalculateSecurityViolations(LoginHistoryResponse loginData, AuditTrailResponse auditData)
    {
        // Simple calculation - in reality, this would analyze patterns
        var suspiciousLogins = loginData.LoginAttempts.Count(l => !l.Success);
        var suspiciousActions = auditData.Records.Count(r => r.Action.Contains("Delete") || r.Action.Contains("Modify"));
        
        return suspiciousLogins + suspiciousActions;
    }

    private static double CalculateComplianceScore(LoginHistoryResponse loginData, AuditTrailResponse auditData)
    {
        // Simple scoring algorithm - in reality, this would be more sophisticated
        var successRate = loginData.TotalCount > 0 ? (double)loginData.SuccessfulLogins / loginData.TotalCount : 1.0;
        var auditCoverage = auditData.TotalCount > 0 ? Math.Min(auditData.TotalCount / 1000.0, 1.0) : 0.0;
        
        return Math.Round((successRate * 0.6 + auditCoverage * 0.4) * 100, 2);
    }

    private async Task<List<ComplianceViolation>> IdentifyComplianceViolationsAsync(LoginHistoryResponse loginData, 
        AuditTrailResponse auditData, CancellationToken cancellationToken = default)
    {
        var violations = new List<ComplianceViolation>();

        // Check for excessive failed login attempts
        var failedLoginsByUser = loginData.LoginAttempts
            .Where(l => !l.Success)
            .GroupBy(l => l.Username)
            .Where(g => g.Count() > 10) // More than 10 failed attempts
            .ToList();

        foreach (var userFailures in failedLoginsByUser)
        {
            violations.Add(new ComplianceViolation
            {
                ViolationType = "ExcessiveFailedLogins",
                Description = $"User {userFailures.Key} has {userFailures.Count()} failed login attempts",
                Severity = "High",
                UserId = userFailures.Key,
                OccurredAt = userFailures.Max(f => f.AttemptedAt),
                Status = "Active"
            });
        }

        // Check for missing audit trails (gaps in activity)
        // This is simplified - in reality, you'd check for expected activities
        
        await Task.CompletedTask; // Placeholder for async operations
        return violations;
    }
}

/// <summary>
/// Request for exporting audit logs
/// </summary>
public class AuditExportRequest
{
    public int? UserId { get; set; }
    public string? Username { get; set; }
    public string? Action { get; set; }
    public string? EntityType { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public string Format { get; set; } = "json"; // json, csv
}

/// <summary>
/// Result of audit log export
/// </summary>
public class AuditExportResult
{
    public string Content { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int RecordCount { get; set; }
    public DateTimeOffset GeneratedAt { get; set; }
}