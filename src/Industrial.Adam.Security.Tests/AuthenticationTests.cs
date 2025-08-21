using Microsoft.Extensions.Logging;
using Xunit;
using Industrial.Adam.Security.Authentication;
using Industrial.Adam.Security.Models;
using Industrial.Adam.Security.Authorization;
using Industrial.Adam.Security.Utilities;

namespace Industrial.Adam.Security.Tests;

/// <summary>
/// Security tests for authentication functionality
/// </summary>
public class AuthenticationTests : IDisposable
{
    private readonly UserStorageService _userStorageService;
    private readonly ILogger<UserStorageService> _logger;
    private readonly string _testDataDirectory;

    public AuthenticationTests()
    {
        // Create test logger
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<UserStorageService>();

        // Use temporary directory for tests
        _testDataDirectory = Path.Combine(Path.GetTempPath(), "SecurityTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDataDirectory);

        // Override data directory for tests
        Environment.SetEnvironmentVariable("LOCALAPPDATA", _testDataDirectory);
        
        _userStorageService = new UserStorageService(_logger);
    }

    public void Dispose()
    {
        // Clean up test data
        if (Directory.Exists(_testDataDirectory))
        {
            Directory.Delete(_testDataDirectory, true);
        }
    }

    [Fact]
    public async Task ValidateUserAsync_WithValidCredentials_ReturnsUserInfo()
    {
        // Arrange
        var username = "admin";
        
        // First get the generated password from the initial passwords file
        var passwordsFile = Path.Combine(_testDataDirectory, "Industrial.Adam.Security", "initial-passwords.txt");
        Assert.True(File.Exists(passwordsFile), "Initial passwords file should exist");
        
        var passwordsContent = await File.ReadAllTextAsync(passwordsFile);
        var adminPasswordLine = passwordsContent.Split('\n').FirstOrDefault(line => line.StartsWith("Admin password:"));
        Assert.NotNull(adminPasswordLine);
        
        var adminPassword = adminPasswordLine.Split(": ")[1];

        // Act
        var result = await _userStorageService.ValidateUserAsync(username, adminPassword);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(username, result.Username);
        Assert.Contains(RoleConstants.SystemAdmin, result.Roles);
        Assert.True(result.MustChangePassword, "Default users must change password on first login");
    }

    [Fact]
    public async Task ValidateUserAsync_WithInvalidPassword_ReturnsNull()
    {
        // Arrange
        var username = "admin";
        var invalidPassword = "wrongpassword";

        // Act
        var result = await _userStorageService.ValidateUserAsync(username, invalidPassword);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateUserAsync_WithNonExistentUser_ReturnsNull()
    {
        // Arrange
        var username = "nonexistent";
        var password = "anypassword";

        // Act
        var result = await _userStorageService.ValidateUserAsync(username, password);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithValidCurrentPassword_ChangesPasswordSuccessfully()
    {
        // Arrange
        var userId = "admin-001";
        
        // Get initial password
        var passwordsFile = Path.Combine(_testDataDirectory, "Industrial.Adam.Security", "initial-passwords.txt");
        var passwordsContent = await File.ReadAllTextAsync(passwordsFile);
        var adminPasswordLine = passwordsContent.Split('\n').FirstOrDefault(line => line.StartsWith("Admin password:"));
        var currentPassword = adminPasswordLine!.Split(": ")[1];
        
        var newPassword = SecurePasswordGenerator.GenerateSecurePassword();

        // Act
        var result = await _userStorageService.ChangePasswordAsync(userId, currentPassword, newPassword);

        // Assert
        Assert.True(result, "Password change should succeed");

        // Verify old password no longer works
        var oldPasswordResult = await _userStorageService.ValidateUserAsync("admin", currentPassword);
        Assert.Null(oldPasswordResult);

        // Verify new password works and password change requirement is cleared
        var newPasswordResult = await _userStorageService.ValidateUserAsync("admin", newPassword);
        Assert.NotNull(newPasswordResult);
        Assert.False(newPasswordResult.MustChangePassword, "Password change requirement should be cleared");
    }

    [Fact]
    public async Task ChangePasswordAsync_WithInvalidCurrentPassword_ReturnsFalse()
    {
        // Arrange
        var userId = "admin-001";
        var invalidCurrentPassword = "wrongpassword";
        var newPassword = SecurePasswordGenerator.GenerateSecurePassword();

        // Act
        var result = await _userStorageService.ChangePasswordAsync(userId, invalidCurrentPassword, newPassword);

        // Assert
        Assert.False(result, "Password change should fail with invalid current password");
    }

    [Fact]
    public async Task StoreRefreshTokenAsync_StoresTokenSuccessfully()
    {
        // Arrange
        var userId = "admin-001";
        var refreshToken = Guid.NewGuid().ToString();
        var expiresAt = DateTimeOffset.UtcNow.AddDays(7);

        // Act
        await _userStorageService.StoreRefreshTokenAsync(userId, refreshToken, expiresAt);

        // Assert
        var result = await _userStorageService.ValidateRefreshTokenAsync(refreshToken);
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_WithExpiredToken_ReturnsNull()
    {
        // Arrange
        var userId = "admin-001";
        var refreshToken = Guid.NewGuid().ToString();
        var expiredDate = DateTimeOffset.UtcNow.AddDays(-1); // Expired

        // Act
        await _userStorageService.StoreRefreshTokenAsync(userId, refreshToken, expiredDate);
        var result = await _userStorageService.ValidateRefreshTokenAsync(refreshToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_RemovesTokenSuccessfully()
    {
        // Arrange
        var userId = "admin-001";
        var refreshToken = Guid.NewGuid().ToString();
        var expiresAt = DateTimeOffset.UtcNow.AddDays(7);

        await _userStorageService.StoreRefreshTokenAsync(userId, refreshToken, expiresAt);

        // Act
        await _userStorageService.RevokeRefreshTokenAsync(refreshToken);

        // Assert
        var result = await _userStorageService.ValidateRefreshTokenAsync(refreshToken);
        Assert.Null(result);
    }

    [Fact]
    public void InitializeDefaultUsers_CreatesSecurePasswords()
    {
        // Arrange & Act - Default users are created in constructor

        // Assert - Check that initial passwords file exists with secure passwords
        var passwordsFile = Path.Combine(_testDataDirectory, "Industrial.Adam.Security", "initial-passwords.txt");
        Assert.True(File.Exists(passwordsFile), "Initial passwords file should exist");

        var passwordsContent = File.ReadAllText(passwordsFile);
        
        // Extract passwords
        var lines = passwordsContent.Split('\n');
        var adminPasswordLine = lines.FirstOrDefault(line => line.StartsWith("Admin password:"));
        var supervisorPasswordLine = lines.FirstOrDefault(line => line.StartsWith("Supervisor password:"));
        var operatorPasswordLine = lines.FirstOrDefault(line => line.StartsWith("Operator password:"));

        Assert.NotNull(adminPasswordLine);
        Assert.NotNull(supervisorPasswordLine);
        Assert.NotNull(operatorPasswordLine);

        var adminPassword = adminPasswordLine.Split(": ")[1];
        var supervisorPassword = supervisorPasswordLine.Split(": ")[1];
        var operatorPassword = operatorPasswordLine.Split(": ")[1];

        // Verify passwords are secure (minimum 16 characters)
        Assert.True(adminPassword.Length >= 16, "Admin password should be at least 16 characters");
        Assert.True(supervisorPassword.Length >= 16, "Supervisor password should be at least 16 characters");
        Assert.True(operatorPassword.Length >= 16, "Operator password should be at least 16 characters");

        // Verify passwords are different
        Assert.NotEqual(adminPassword, supervisorPassword);
        Assert.NotEqual(adminPassword, operatorPassword);
        Assert.NotEqual(supervisorPassword, operatorPassword);

        // Verify no hardcoded passwords
        Assert.DoesNotContain("admin123", passwordsContent);
        Assert.DoesNotContain("supervisor123", passwordsContent);
        Assert.DoesNotContain("operator123", passwordsContent);
    }
}