using Dapper;
using Industrial.Adam.Security.Domain.Entities;
using Industrial.Adam.Security.Application.DTOs;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text.Json;

namespace Industrial.Adam.Security.Infrastructure.Repositories;

/// <summary>
/// Repository for audit log operations with TimescaleDB
/// </summary>
public class AuditLogRepository
{
    private readonly DatabaseConnectionFactory _connectionFactory;
    private readonly ILogger<AuditLogRepository> _logger;

    public AuditLogRepository(DatabaseConnectionFactory connectionFactory, ILogger<AuditLogRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    /// <summary>
    /// Logs a login attempt
    /// </summary>
    public async Task LogLoginAttemptAsync(string username, string ipAddress, bool success, string? failureReason = null, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            
            const string sql = """
                INSERT INTO login_attempts (username, ip_address, success, failure_reason, attempted_at)
                VALUES (@Username, @IpAddress::inet, @Success, @FailureReason, @AttemptedAt)
                """;

            await connection.ExecuteAsync(sql, new
            {
                Username = username,
                IpAddress = ipAddress,
                Success = success,
                FailureReason = failureReason,
                AttemptedAt = DateTimeOffset.UtcNow
            });

            _logger.LogInformation("Logged login attempt for user {Username} from {IpAddress}: {Success}", 
                username, ipAddress, success ? "SUCCESS" : "FAILURE");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log login attempt for user {Username}", username);
            throw;
        }
    }

    /// <summary>
    /// Logs an audit trail record
    /// </summary>
    public async Task LogAuditTrailAsync(int userId, string action, string entityType, string entityId, 
        object? oldValue, object? newValue, string ipAddress, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            
            const string sql = """
                INSERT INTO audit_trail (user_id, action, entity_type, entity_id, old_value, new_value, ip_address, created_at)
                VALUES (@UserId, @Action, @EntityType, @EntityId, @OldValue::jsonb, @NewValue::jsonb, @IpAddress::inet, @CreatedAt)
                """;

            await connection.ExecuteAsync(sql, new
            {
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                OldValue = oldValue != null ? JsonSerializer.Serialize(oldValue) : null,
                NewValue = newValue != null ? JsonSerializer.Serialize(newValue) : null,
                IpAddress = ipAddress,
                CreatedAt = DateTimeOffset.UtcNow
            });

            _logger.LogInformation("Logged audit trail: User {UserId} performed {Action} on {EntityType}:{EntityId}", 
                userId, action, entityType, entityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit trail for user {UserId}", userId);
            throw;
        }
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
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            
            var conditions = new List<string>();
            var parameters = new DynamicParameters();

            if (!string.IsNullOrEmpty(username))
            {
                conditions.Add("username ILIKE @Username");
                parameters.Add("Username", $"%{username}%");
            }

            if (success.HasValue)
            {
                conditions.Add("success = @Success");
                parameters.Add("Success", success.Value);
            }

            if (startTime.HasValue)
            {
                conditions.Add("attempted_at >= @StartTime");
                parameters.Add("StartTime", startTime.Value);
            }

            if (endTime.HasValue)
            {
                conditions.Add("attempted_at <= @EndTime");
                parameters.Add("EndTime", endTime.Value);
            }

            var whereClause = conditions.Count > 0 ? $"WHERE {string.Join(" AND ", conditions)}" : "";
            
            // Get total count
            var countSql = $"SELECT COUNT(*) FROM login_attempts {whereClause}";
            var totalCount = await connection.QuerySingleAsync<int>(countSql, parameters);

            // Get successful/failed counts
            var successCountSql = $"SELECT COUNT(*) FROM login_attempts {whereClause} AND success = true";
            var successCount = await connection.QuerySingleAsync<int>(successCountSql, parameters);

            // Get paginated results
            var offset = (page - 1) * pageSize;
            parameters.Add("Limit", pageSize);
            parameters.Add("Offset", offset);

            var dataSql = $"""
                SELECT id, username, ip_address as ipaddress, success, failure_reason as failurereason, attempted_at as attemptedat
                FROM login_attempts 
                {whereClause}
                ORDER BY attempted_at DESC
                LIMIT @Limit OFFSET @Offset
                """;

            var loginAttempts = await connection.QueryAsync<LoginAttempt>(dataSql, parameters);

            // Get last successful login
            DateTimeOffset? lastSuccessfulLogin = null;
            if (!string.IsNullOrEmpty(username))
            {
                var lastSuccessQuery = "SELECT attempted_at FROM login_attempts WHERE username = @Username AND success = true ORDER BY attempted_at DESC LIMIT 1";
                lastSuccessfulLogin = await connection.QuerySingleOrDefaultAsync<DateTimeOffset?>(lastSuccessQuery, new { Username = username });
            }

            return new LoginHistoryResponse
            {
                LoginAttempts = loginAttempts.ToList(),
                TotalCount = totalCount,
                SuccessfulLogins = successCount,
                FailedLogins = totalCount - successCount,
                LastSuccessfulLogin = lastSuccessfulLogin
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get login attempts");
            throw;
        }
    }

    /// <summary>
    /// Gets audit trail records with filtering and pagination
    /// </summary>
    public async Task<AuditTrailResponse> GetAuditTrailAsync(int? userId = null, string? action = null, string? entityType = null,
        DateTimeOffset? startTime = null, DateTimeOffset? endTime = null, int page = 1, int pageSize = 50, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            
            var conditions = new List<string>();
            var parameters = new DynamicParameters();

            if (userId.HasValue)
            {
                conditions.Add("user_id = @UserId");
                parameters.Add("UserId", userId.Value);
            }

            if (!string.IsNullOrEmpty(action))
            {
                conditions.Add("action ILIKE @Action");
                parameters.Add("Action", $"%{action}%");
            }

            if (!string.IsNullOrEmpty(entityType))
            {
                conditions.Add("entity_type = @EntityType");
                parameters.Add("EntityType", entityType);
            }

            if (startTime.HasValue)
            {
                conditions.Add("created_at >= @StartTime");
                parameters.Add("StartTime", startTime.Value);
            }

            if (endTime.HasValue)
            {
                conditions.Add("created_at <= @EndTime");
                parameters.Add("EndTime", endTime.Value);
            }

            var whereClause = conditions.Count > 0 ? $"WHERE {string.Join(" AND ", conditions)}" : "";
            
            // Get total count
            var countSql = $"SELECT COUNT(*) FROM audit_trail {whereClause}";
            var totalCount = await connection.QuerySingleAsync<int>(countSql, parameters);

            // Get paginated results
            var offset = (page - 1) * pageSize;
            parameters.Add("Limit", pageSize);
            parameters.Add("Offset", offset);

            var dataSql = $"""
                SELECT id, user_id as userid, action, entity_type as entitytype, entity_id as entityid, 
                       old_value as oldvalue, new_value as newvalue, ip_address as ipaddress, created_at as createdat
                FROM audit_trail 
                {whereClause}
                ORDER BY created_at DESC
                LIMIT @Limit OFFSET @Offset
                """;

            var records = await connection.QueryAsync<AuditTrailRecord>(dataSql, parameters);

            return new AuditTrailResponse
            {
                Records = records.ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                HasNext = (page * pageSize) < totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get audit trail records");
            throw;
        }
    }

    /// <summary>
    /// Creates or updates a user session
    /// </summary>
    public async Task UpsertUserSessionAsync(int userId, string tokenHash, string ipAddress, string userAgent, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            
            const string sql = """
                INSERT INTO user_sessions (user_id, token_hash, ip_address, user_agent, started_at, last_activity)
                VALUES (@UserId, @TokenHash, @IpAddress::inet, @UserAgent, @StartedAt, @LastActivity)
                ON CONFLICT (token_hash) 
                DO UPDATE SET last_activity = @LastActivity
                """;

            await connection.ExecuteAsync(sql, new
            {
                UserId = userId,
                TokenHash = tokenHash,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                StartedAt = DateTimeOffset.UtcNow,
                LastActivity = DateTimeOffset.UtcNow
            });

            _logger.LogDebug("Updated session for user {UserId} from {IpAddress}", userId, ipAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upsert user session for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Gets active sessions for a user
    /// </summary>
    public async Task<UserSessionsResponse> GetUserSessionsAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            
            const string sql = """
                SELECT id, user_id as userid, token_hash as tokenhash, ip_address as ipaddress, 
                       user_agent as useragent, started_at as startedat, last_activity as lastactivity, ended_at as endedat
                FROM user_sessions 
                WHERE user_id = @UserId AND ended_at IS NULL
                ORDER BY last_activity DESC
                """;

            var sessions = await connection.QueryAsync<UserSession>(sql, new { UserId = userId });
            var sessionList = sessions.ToList();

            return new UserSessionsResponse
            {
                ActiveSessions = sessionList,
                TotalCount = sessionList.Count,
                LastUpdated = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user sessions for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Terminates user sessions
    /// </summary>
    public async Task TerminateUserSessionsAsync(int userId, string[]? tokenHashes = null, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            
            string sql;
            object parameters;

            if (tokenHashes?.Length > 0)
            {
                sql = "UPDATE user_sessions SET ended_at = @EndedAt WHERE user_id = @UserId AND token_hash = ANY(@TokenHashes)";
                parameters = new { UserId = userId, TokenHashes = tokenHashes, EndedAt = DateTimeOffset.UtcNow };
            }
            else
            {
                sql = "UPDATE user_sessions SET ended_at = @EndedAt WHERE user_id = @UserId AND ended_at IS NULL";
                parameters = new { UserId = userId, EndedAt = DateTimeOffset.UtcNow };
            }

            var affected = await connection.ExecuteAsync(sql, parameters);
            
            _logger.LogInformation("Terminated {Count} sessions for user {UserId}", affected, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to terminate sessions for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Logs user activity
    /// </summary>
    public async Task LogUserActivityAsync(int userId, string action, string details, string ipAddress, string userAgent, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            
            const string sql = """
                INSERT INTO user_activity (user_id, action, details, ip_address, user_agent, timestamp)
                VALUES (@UserId, @Action, @Details, @IpAddress::inet, @UserAgent, @Timestamp)
                """;

            await connection.ExecuteAsync(sql, new
            {
                UserId = userId,
                Action = action,
                Details = details,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Timestamp = DateTimeOffset.UtcNow
            });

            _logger.LogDebug("Logged activity for user {UserId}: {Action}", userId, action);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log user activity for user {UserId}", userId);
            // Don't throw for activity logging failures
        }
    }

    /// <summary>
    /// Gets user activity history with pagination
    /// </summary>
    public async Task<UserActivityResponse> GetUserActivityAsync(int userId, int page = 1, int pageSize = 50, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            
            // Get total count
            const string countSql = "SELECT COUNT(*) FROM user_activity WHERE user_id = @UserId";
            var totalCount = await connection.QuerySingleAsync<int>(countSql, new { UserId = userId });

            // Get paginated results
            var offset = (page - 1) * pageSize;
            const string dataSql = """
                SELECT id, user_id as userid, action, details, ip_address as ipaddress, 
                       user_agent as useragent, timestamp
                FROM user_activity 
                WHERE user_id = @UserId
                ORDER BY timestamp DESC
                LIMIT @Limit OFFSET @Offset
                """;

            var activities = await connection.QueryAsync<UserActivity>(dataSql, new 
            { 
                UserId = userId, 
                Limit = pageSize, 
                Offset = offset 
            });

            return new UserActivityResponse
            {
                Activities = activities.ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                HasNext = (page * pageSize) < totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user activity for user {UserId}", userId);
            throw;
        }
    }
}