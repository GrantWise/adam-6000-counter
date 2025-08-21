using System.Text.Json;
using Industrial.Adam.Security.Authorization;
using Industrial.Adam.Security.Models;
using Industrial.Adam.Security.Utilities;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Security.Authentication;

/// <summary>
/// File-based user storage service for local deployment
/// In production, this would be replaced with a database-backed service
/// </summary>
public class UserStorageService
{
    private readonly string _usersFilePath;
    private readonly string _refreshTokensFilePath;
    private readonly ILogger<UserStorageService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public UserStorageService(ILogger<UserStorageService> logger)
    {
        _logger = logger;

        // Store user data in application data directory
        var dataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Industrial.Adam.Security");
        Directory.CreateDirectory(dataDirectory);

        _usersFilePath = Path.Combine(dataDirectory, "users.json");
        _refreshTokensFilePath = Path.Combine(dataDirectory, "refresh-tokens.json");

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        InitializeDefaultUsers();
    }

    /// <summary>
    /// Validates user credentials
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="password">Password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User info if valid, null otherwise</returns>
    public async Task<UserInfo?> ValidateUserAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            var users = await LoadUsersAsync(cancellationToken);
            var user = users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

            if (user == null)
            {
                _logger.LogWarning("User not found: {Username}", username);
                return null;
            }

            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                _logger.LogWarning("Invalid password for user: {Username}", username);
                return null;
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Inactive user attempted login: {Username}", username);
                return null;
            }

            return new UserInfo
            {
                UserId = user.UserId,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Roles = user.Roles,
                LastLogin = user.LastLogin,
                MustChangePassword = user.MustChangePassword
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating user: {Username}", username);
            return null;
        }
    }

    /// <summary>
    /// Stores refresh token for user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="refreshToken">Refresh token</param>
    /// <param name="expiresAt">Token expiration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task StoreRefreshTokenAsync(string userId, string refreshToken, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
    {
        try
        {
            var tokens = await LoadRefreshTokensAsync(cancellationToken);

            // Remove expired tokens for this user
            tokens.RemoveAll(t => t.UserId == userId && t.ExpiresAt <= DateTimeOffset.UtcNow);

            // Add new token
            tokens.Add(new RefreshTokenInfo
            {
                UserId = userId,
                Token = refreshToken,
                ExpiresAt = expiresAt,
                CreatedAt = DateTimeOffset.UtcNow
            });

            await SaveRefreshTokensAsync(tokens, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing refresh token for user: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Validates refresh token and returns user info
    /// </summary>
    /// <param name="refreshToken">Refresh token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User info if valid, null otherwise</returns>
    public async Task<UserInfo?> ValidateRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var tokens = await LoadRefreshTokensAsync(cancellationToken);
            var tokenInfo = tokens.FirstOrDefault(t => t.Token == refreshToken && t.ExpiresAt > DateTimeOffset.UtcNow);

            if (tokenInfo == null)
            {
                return null;
            }

            var users = await LoadUsersAsync(cancellationToken);
            var user = users.FirstOrDefault(u => u.UserId == tokenInfo.UserId && u.IsActive);

            if (user == null)
            {
                return null;
            }

            return new UserInfo
            {
                UserId = user.UserId,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Roles = user.Roles,
                LastLogin = user.LastLogin,
                MustChangePassword = user.MustChangePassword
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating refresh token");
            return null;
        }
    }

    /// <summary>
    /// Replaces refresh token with new one
    /// </summary>
    /// <param name="oldToken">Old refresh token</param>
    /// <param name="newToken">New refresh token</param>
    /// <param name="expiresAt">New token expiration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task ReplaceRefreshTokenAsync(string oldToken, string newToken, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
    {
        try
        {
            var tokens = await LoadRefreshTokensAsync(cancellationToken);
            var tokenInfo = tokens.FirstOrDefault(t => t.Token == oldToken);

            if (tokenInfo != null)
            {
                tokens.Remove(tokenInfo);
                tokens.Add(new RefreshTokenInfo
                {
                    UserId = tokenInfo.UserId,
                    Token = newToken,
                    ExpiresAt = expiresAt,
                    CreatedAt = DateTimeOffset.UtcNow
                });

                await SaveRefreshTokensAsync(tokens, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error replacing refresh token");
            throw;
        }
    }

    /// <summary>
    /// Revokes refresh token
    /// </summary>
    /// <param name="refreshToken">Refresh token to revoke</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var tokens = await LoadRefreshTokensAsync(cancellationToken);
            tokens.RemoveAll(t => t.Token == refreshToken);
            await SaveRefreshTokensAsync(tokens, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking refresh token");
            throw;
        }
    }

    /// <summary>
    /// Updates user's last login time
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task UpdateLastLoginAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var users = await LoadUsersAsync(cancellationToken);
            var user = users.FirstOrDefault(u => u.UserId == userId);

            if (user != null)
            {
                user.LastLogin = DateTimeOffset.UtcNow;
                await SaveUsersAsync(users, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating last login for user: {UserId}", userId);
            // Don't throw - this is not critical
        }
    }

    /// <summary>
    /// Changes user password
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="currentPassword">Current password</param>
    /// <param name="newPassword">New password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if password was changed successfully</returns>
    public async Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        try
        {
            var users = await LoadUsersAsync(cancellationToken);
            var user = users.FirstOrDefault(u => u.UserId == userId && u.IsActive);

            if (user == null)
            {
                _logger.LogWarning("User not found for password change: {UserId}", userId);
                return false;
            }

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            {
                _logger.LogWarning("Invalid current password for user: {UserId}", userId);
                return false;
            }

            // Update password and clear change requirement
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.MustChangePassword = false;

            await SaveUsersAsync(users, cancellationToken);

            _logger.LogInformation("Password changed successfully for user: {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user: {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Initializes default users if users file doesn't exist
    /// </summary>
    private void InitializeDefaultUsers()
    {
        if (File.Exists(_usersFilePath))
            return;

        // Generate secure temporary passwords for default accounts
        var adminPassword = SecurePasswordGenerator.GenerateSecurePassword();
        var supervisorPassword = SecurePasswordGenerator.GenerateSecurePassword();
        var operatorPassword = SecurePasswordGenerator.GenerateSecurePassword();

        var defaultUsers = new List<User>
        {
            new()
            {
                UserId = "admin-001",
                Username = "admin",
                FullName = "System Administrator",
                Email = "admin@company.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                Roles = [RoleConstants.SystemAdmin],
                IsActive = true,
                MustChangePassword = true,
                CreatedAt = DateTimeOffset.UtcNow,
                LastLogin = DateTimeOffset.UtcNow
            },
            new()
            {
                UserId = "supervisor-001",
                Username = "supervisor",
                FullName = "Production Supervisor",
                Email = "supervisor@company.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(supervisorPassword),
                Roles = [RoleConstants.Supervisor],
                IsActive = true,
                MustChangePassword = true,
                CreatedAt = DateTimeOffset.UtcNow,
                LastLogin = DateTimeOffset.UtcNow
            },
            new()
            {
                UserId = "operator-001",
                Username = "operator",
                FullName = "Machine Operator",
                Email = "operator@company.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(operatorPassword),
                Roles = [RoleConstants.Operator],
                IsActive = true,
                MustChangePassword = true,
                CreatedAt = DateTimeOffset.UtcNow,
                LastLogin = DateTimeOffset.UtcNow
            }
        };

        // Write initial passwords to a secure temporary file
        var passwordsFilePath = Path.Combine(Path.GetDirectoryName(_usersFilePath)!, "initial-passwords.txt");
        var passwordsContent = $"INITIAL PASSWORDS - DELETE THIS FILE AFTER FIRST LOGIN\n" +
                              $"=====================================================\n" +
                              $"Admin password: {adminPassword}\n" +
                              $"Supervisor password: {supervisorPassword}\n" +
                              $"Operator password: {operatorPassword}\n" +
                              $"\nAll users MUST change their passwords on first login.\n" +
                              $"This file will be automatically deleted after 24 hours.";

        try
        {
            File.WriteAllText(passwordsFilePath, passwordsContent);
            _logger.LogWarning("Initial passwords written to: {PasswordsFile}. DELETE this file after setting up user passwords!", passwordsFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write initial passwords file. Passwords: Admin={Admin}, Supervisor={Supervisor}, Operator={Operator}",
                adminPassword, supervisorPassword, operatorPassword);
        }

        try
        {
            var json = JsonSerializer.Serialize(defaultUsers, _jsonOptions);
            File.WriteAllText(_usersFilePath, json);
            _logger.LogInformation("Default users initialized at: {FilePath}", _usersFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize default users");
        }
    }

    private async Task<List<User>> LoadUsersAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_usersFilePath))
        {
            return [];
        }

        var json = await File.ReadAllTextAsync(_usersFilePath, cancellationToken);
        return JsonSerializer.Deserialize<List<User>>(json, _jsonOptions) ?? [];
    }

    private async Task SaveUsersAsync(List<User> users, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(users, _jsonOptions);
        await File.WriteAllTextAsync(_usersFilePath, json, cancellationToken);
    }

    private async Task<List<RefreshTokenInfo>> LoadRefreshTokensAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_refreshTokensFilePath))
        {
            return [];
        }

        var json = await File.ReadAllTextAsync(_refreshTokensFilePath, cancellationToken);
        return JsonSerializer.Deserialize<List<RefreshTokenInfo>>(json, _jsonOptions) ?? [];
    }

    private async Task SaveRefreshTokensAsync(List<RefreshTokenInfo> tokens, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(tokens, _jsonOptions);
        await File.WriteAllTextAsync(_refreshTokensFilePath, json, cancellationToken);
    }
}

/// <summary>
/// Internal user model for storage
/// </summary>
internal class User
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string[] Roles { get; set; } = [];
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; } = false;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastLogin { get; set; }
}

/// <summary>
/// Internal refresh token model for storage
/// </summary>
internal class RefreshTokenInfo
{
    public string UserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
