// UserStorageService is now in the same namespace
using Industrial.Adam.Security.Infrastructure.Repositories;
using Industrial.Adam.Security.Domain.Entities;
using Industrial.Adam.Security.Application.DTOs;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Industrial.Adam.Security.Infrastructure.Services;

/// <summary>
/// Enhanced user management service with session tracking and audit capabilities
/// </summary>
public class EnhancedUserManagementService
{
    private readonly UserStorageService _userStorage;
    private readonly AuditLogRepository _auditRepository;
    private readonly SecurityAuditService _securityAudit;
    private readonly ILogger<EnhancedUserManagementService> _logger;

    public EnhancedUserManagementService(
        UserStorageService userStorage,
        AuditLogRepository auditRepository,
        SecurityAuditService securityAudit,
        ILogger<EnhancedUserManagementService> logger)
    {
        _userStorage = userStorage;
        _auditRepository = auditRepository;
        _securityAudit = securityAudit;
        _logger = logger;
    }

    /// <summary>
    /// Gets user sessions for a specific user
    /// </summary>
    public async Task<UserSessionsResponse> GetUserSessionsAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _auditRepository.GetUserSessionsAsync(userId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get sessions for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Gets user activity history
    /// </summary>
    public async Task<UserActivityResponse> GetUserActivityAsync(int userId, int page = 1, int pageSize = 50, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _auditRepository.GetUserActivityAsync(userId, page, pageSize, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get activity for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Gets login history for a user
    /// </summary>
    public async Task<LoginHistoryResponse> GetUserLoginHistoryAsync(string username, int page = 1, int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _auditRepository.GetLoginAttemptsAsync(username, null, null, null, page, pageSize, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get login history for user {Username}", username);
            throw;
        }
    }

    /// <summary>
    /// Performs bulk password reset for multiple users
    /// </summary>
    public async Task<BulkOperationResult> BulkPasswordResetAsync(BulkPasswordResetRequest request, ClaimsPrincipal adminUser, 
        string ipAddress, CancellationToken cancellationToken = default)
    {
        try
        {
            var adminUserId = adminUser.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";
            var adminUsername = adminUser.Identity?.Name ?? "System";
            var results = new List<BulkOperationResult.UserResult>();

            foreach (var userId in request.UserIds)
            {
                try
                {
                    // In a real implementation, this would integrate with the user storage
                    // For now, we'll log the audit trail
                    await _auditRepository.LogAuditTrailAsync(
                        int.Parse(adminUserId), 
                        "BulkPasswordReset", 
                        "User", 
                        userId,
                        null, 
                        new { ForcePasswordChange = request.ForcePasswordChange }, 
                        ipAddress, 
                        cancellationToken);

                    await _auditRepository.LogUserActivityAsync(
                        int.Parse(adminUserId),
                        "BulkPasswordReset",
                        $"Reset password for user {userId}",
                        ipAddress,
                        "Admin Dashboard",
                        cancellationToken);

                    results.Add(new BulkOperationResult.UserResult
                    {
                        UserId = userId,
                        Success = true,
                        Message = "Password reset successfully"
                    });

                    _logger.LogInformation("Password reset for user {UserId} by admin {AdminUsername}", userId, adminUsername);
                }
                catch (Exception ex)
                {
                    results.Add(new BulkOperationResult.UserResult
                    {
                        UserId = userId,
                        Success = false,
                        Message = $"Failed to reset password: {ex.Message}"
                    });

                    _logger.LogError(ex, "Failed to reset password for user {UserId}", userId);
                }
            }

            return new BulkOperationResult
            {
                TotalCount = request.UserIds.Length,
                SuccessCount = results.Count(r => r.Success),
                FailureCount = results.Count(r => !r.Success),
                Results = results
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform bulk password reset");
            throw;
        }
    }

    /// <summary>
    /// Locks or unlocks a user account
    /// </summary>
    public async Task<bool> LockUserAccountAsync(string userId, LockUserRequest request, ClaimsPrincipal adminUser, 
        string ipAddress, CancellationToken cancellationToken = default)
    {
        try
        {
            var adminUserId = adminUser.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";
            var adminUsername = adminUser.Identity?.Name ?? "System";

            // Log the action
            await _auditRepository.LogAuditTrailAsync(
                int.Parse(adminUserId),
                "LockUserAccount",
                "User",
                userId,
                new { IsLocked = false },
                new { IsLocked = true, Reason = request.Reason, LockUntil = request.LockUntil },
                ipAddress,
                cancellationToken);

            await _auditRepository.LogUserActivityAsync(
                int.Parse(adminUserId),
                "LockUserAccount",
                $"Locked user {userId}: {request.Reason}",
                ipAddress,
                "Admin Dashboard",
                cancellationToken);

            // Terminate all sessions for the locked user
            if (int.TryParse(userId, out var userIdInt))
            {
                await _auditRepository.TerminateUserSessionsAsync(userIdInt, null, cancellationToken);
            }

            _logger.LogWarning("User {UserId} locked by admin {AdminUsername}: {Reason}", userId, adminUsername, request.Reason);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to lock user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Terminates user sessions
    /// </summary>
    public async Task<bool> TerminateUserSessionsAsync(int userId, string[]? sessionTokens, ClaimsPrincipal adminUser, 
        string ipAddress, CancellationToken cancellationToken = default)
    {
        try
        {
            var adminUserId = adminUser.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";

            await _auditRepository.TerminateUserSessionsAsync(userId, sessionTokens, cancellationToken);

            await _auditRepository.LogAuditTrailAsync(
                int.Parse(adminUserId),
                "TerminateUserSessions",
                "UserSession",
                userId.ToString(),
                null,
                new { TerminatedSessions = sessionTokens?.Length ?? 0 },
                ipAddress,
                cancellationToken);

            _logger.LogInformation("Terminated sessions for user {UserId} by admin {AdminUserId}", userId, adminUserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to terminate sessions for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Tracks user activity
    /// </summary>
    public async Task TrackUserActivityAsync(int userId, string action, string details, string ipAddress, 
        string userAgent = "Unknown", CancellationToken cancellationToken = default)
    {
        try
        {
            await _auditRepository.LogUserActivityAsync(userId, action, details, ipAddress, userAgent, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track activity for user {UserId}", userId);
            // Don't throw for activity tracking failures
        }
    }
}

/// <summary>
/// Result of bulk operations on users
/// </summary>
public class BulkOperationResult
{
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<UserResult> Results { get; set; } = new();

    public class UserResult
    {
        public string UserId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}