using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Industrial.Adam.Security.Authorization;
using Industrial.Adam.Security.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Industrial.Adam.Security.Authentication;

/// <summary>
/// Service for JWT token generation and validation
/// </summary>
public class JwtAuthenticationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtAuthenticationService> _logger;
    private readonly UserStorageService _userStorage;
    private readonly SymmetricSecurityKey _securityKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expirationMinutes;
    private readonly int _refreshExpirationDays;

    public JwtAuthenticationService(
        IConfiguration configuration,
        ILogger<JwtAuthenticationService> logger,
        UserStorageService userStorage)
    {
        _configuration = configuration;
        _logger = logger;
        _userStorage = userStorage;

        var secretKey = _configuration["JWT_SECRET_KEY"] ?? throw new InvalidOperationException("JWT_SECRET_KEY not configured");
        _securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

        _issuer = _configuration["JWT_ISSUER"] ?? "Industrial.Adam.System";
        _audience = _configuration["JWT_AUDIENCE"] ?? "Industrial.Adam.APIs";
        _expirationMinutes = _configuration.GetValue<int>("JWT_EXPIRATION_MINUTES", 60);
        _refreshExpirationDays = _configuration.GetValue<int>("JWT_REFRESH_EXPIRATION_DAYS", 7);
    }

    /// <summary>
    /// Authenticates user credentials and returns JWT tokens
    /// </summary>
    /// <param name="request">Authentication request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication response with tokens</returns>
    public async Task<AuthenticationResponse?> AuthenticateAsync(AuthenticationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Attempting authentication for user: {Username}", request.Username);

            var user = await _userStorage.ValidateUserAsync(request.Username, request.Password, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Authentication failed for user: {Username}", request.Username);
                return null;
            }

            var accessToken = GenerateAccessToken(user);
            var refreshToken = GenerateRefreshToken();
            var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_expirationMinutes);
            var refreshExpiresAt = DateTimeOffset.UtcNow.AddDays(_refreshExpirationDays);

            // Store refresh token
            await _userStorage.StoreRefreshTokenAsync(user.UserId, refreshToken, refreshExpiresAt, cancellationToken);

            // Update last login
            await _userStorage.UpdateLastLoginAsync(user.UserId, cancellationToken);

            _logger.LogInformation("Authentication successful for user: {Username}, Role: {Role}", user.Username, string.Join(",", user.Roles));

            return new AuthenticationResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                RefreshExpiresAt = refreshExpiresAt,
                User = user
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authentication for user: {Username}", request.Username);
            return null;
        }
    }

    /// <summary>
    /// Refreshes access token using refresh token
    /// </summary>
    /// <param name="request">Refresh token request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New authentication response</returns>
    public async Task<AuthenticationResponse?> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userStorage.ValidateRefreshTokenAsync(request.RefreshToken, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Invalid refresh token attempted");
                return null;
            }

            var accessToken = GenerateAccessToken(user);
            var newRefreshToken = GenerateRefreshToken();
            var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_expirationMinutes);
            var refreshExpiresAt = DateTimeOffset.UtcNow.AddDays(_refreshExpirationDays);

            // Replace old refresh token with new one
            await _userStorage.ReplaceRefreshTokenAsync(request.RefreshToken, newRefreshToken, refreshExpiresAt, cancellationToken);

            _logger.LogInformation("Token refreshed for user: {Username}", user.Username);

            return new AuthenticationResponse
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = expiresAt,
                RefreshExpiresAt = refreshExpiresAt,
                User = user
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return null;
        }
    }

    /// <summary>
    /// Revokes a refresh token (logout)
    /// </summary>
    /// <param name="refreshToken">Refresh token to revoke</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful</returns>
    public async Task<bool> RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        try
        {
            await _userStorage.RevokeRefreshTokenAsync(refreshToken, cancellationToken);
            _logger.LogInformation("Refresh token revoked");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking refresh token");
            return false;
        }
    }

    /// <summary>
    /// Validates JWT access token
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>ClaimsPrincipal if valid, null otherwise</returns>
    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _securityKey,
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Token validation failed");
            return null;
        }
    }

    /// <summary>
    /// Generates JWT access token for user
    /// </summary>
    /// <param name="user">User information</param>
    /// <returns>JWT token string</returns>
    private string GenerateAccessToken(UserInfo user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.GivenName, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new("last_login", user.LastLogin.ToString("O"))
        };

        // Add role claims
        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_expirationMinutes),
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(_securityKey, SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Generates cryptographically secure refresh token
    /// </summary>
    /// <returns>Refresh token string</returns>
    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
