using Industrial.Adam.Security.Domain.Entities;

namespace Industrial.Adam.Security.Domain.Repositories;

/// <summary>
/// User repository interface for domain layer
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Gets a user by ID
    /// </summary>
    Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by username
    /// </summary>
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by email
    /// </summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all users with pagination
    /// </summary>
    Task<(List<User> Users, int TotalCount)> GetAllAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets users by role
    /// </summary>
    Task<List<User>> GetByRoleAsync(UserRole role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets locked users
    /// </summary>
    Task<List<User>> GetLockedUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets users requiring password change
    /// </summary>
    Task<List<User>> GetUsersRequiringPasswordChangeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new user
    /// </summary>
    Task<string> CreateAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing user
    /// </summary>
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a user (soft delete)
    /// </summary>
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if username exists
    /// </summary>
    Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if email exists
    /// </summary>
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets users with failed login attempts above threshold
    /// </summary>
    Task<List<User>> GetUsersWithFailedAttemptsAsync(int threshold, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user login history
    /// </summary>
    Task<List<UserLoginHistory>> GetUserLoginHistoryAsync(string userId, int days = 30, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a login attempt
    /// </summary>
    Task RecordLoginAttemptAsync(string userId, bool success, string ipAddress, string userAgent, CancellationToken cancellationToken = default);
}

/// <summary>
/// User login history record
/// </summary>
public class UserLoginHistory
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool Success { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
}