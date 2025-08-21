using System.ComponentModel.DataAnnotations;

namespace Industrial.Adam.Security.Models;

/// <summary>
/// Request model for user authentication
/// </summary>
public class AuthenticationRequest
{
    /// <summary>
    /// Username for authentication
    /// </summary>
    [Required(ErrorMessage = "Username is required")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password for authentication
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Whether to remember the user login (affects token expiration)
    /// </summary>
    public bool RememberMe { get; set; } = false;
}

/// <summary>
/// Response model for successful authentication
/// </summary>
public class AuthenticationResponse
{
    /// <summary>
    /// JWT access token
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Refresh token for obtaining new access tokens
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration time
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// Refresh token expiration time
    /// </summary>
    public DateTimeOffset RefreshExpiresAt { get; set; }

    /// <summary>
    /// Authenticated user information
    /// </summary>
    public UserInfo User { get; set; } = new();
}

/// <summary>
/// User information included in authentication response
/// </summary>
public class UserInfo
{
    /// <summary>
    /// User's unique identifier
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// User's display name
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// User's full name
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// User's email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's roles in the system
    /// </summary>
    public string[] Roles { get; set; } = [];

    /// <summary>
    /// Last login timestamp
    /// </summary>
    public DateTimeOffset LastLogin { get; set; }

    /// <summary>
    /// Whether user must change password on next login
    /// </summary>
    public bool MustChangePassword { get; set; }
}

/// <summary>
/// Request model for token refresh
/// </summary>
public class RefreshTokenRequest
{
    /// <summary>
    /// Refresh token to exchange for new access token
    /// </summary>
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// Request model for password change
/// </summary>
public class ChangePasswordRequest
{
    /// <summary>
    /// Current password
    /// </summary>
    [Required(ErrorMessage = "Current password is required")]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// New password
    /// </summary>
    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, MinimumLength = 12, ErrorMessage = "Password must be at least 12 characters")]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Confirm new password
    /// </summary>
    [Required(ErrorMessage = "Password confirmation is required")]
    [Compare("NewPassword", ErrorMessage = "Password confirmation does not match")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
