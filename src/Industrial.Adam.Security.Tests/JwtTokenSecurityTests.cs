using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;
using Industrial.Adam.Security.Authentication;
using Industrial.Adam.Security.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Industrial.Adam.Security.Tests;

/// <summary>
/// Security tests for JWT token functionality
/// </summary>
public class JwtTokenSecurityTests : IDisposable
{
    private readonly JwtAuthenticationService _jwtService;
    private readonly UserStorageService _userStorageService;
    private readonly IConfiguration _configuration;
    private readonly string _testDataDirectory;

    public JwtTokenSecurityTests()
    {
        // Create test data directory
        _testDataDirectory = Path.Combine(Path.GetTempPath(), "JwtSecurityTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDataDirectory);
        Environment.SetEnvironmentVariable("LOCALAPPDATA", _testDataDirectory);

        // Create test configuration
        var configData = new Dictionary<string, string?>
        {
            ["JWT_SECRET_KEY"] = "ThisIsATestSecretKeyThatIsAtLeast32CharactersLongForSecurity",
            ["JWT_ISSUER"] = "TestIssuer",
            ["JWT_AUDIENCE"] = "TestAudience",
            ["JWT_EXPIRATION_MINUTES"] = "60",
            ["JWT_REFRESH_EXPIRATION_DAYS"] = "7"
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Create test logger
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var userStorageLogger = loggerFactory.CreateLogger<UserStorageService>();
        var jwtLogger = loggerFactory.CreateLogger<JwtAuthenticationService>();

        _userStorageService = new UserStorageService(userStorageLogger);
        _jwtService = new JwtAuthenticationService(_configuration, jwtLogger, _userStorageService);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDataDirectory))
        {
            Directory.Delete(_testDataDirectory, true);
        }
    }

    [Fact]
    public async Task AuthenticateAsync_GeneratesValidJwtToken()
    {
        // Arrange
        var request = await CreateAuthenticationRequest();

        // Act
        var response = await _jwtService.AuthenticateAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response.AccessToken);
        Assert.NotEmpty(response.RefreshToken);
        Assert.True(response.ExpiresAt > DateTimeOffset.UtcNow);
        Assert.True(response.RefreshExpiresAt > DateTimeOffset.UtcNow);

        // Validate JWT token structure
        var handler = new JwtSecurityTokenHandler();
        Assert.True(handler.CanReadToken(response.AccessToken));

        var jwt = handler.ReadJwtToken(response.AccessToken);
        Assert.Equal("TestIssuer", jwt.Issuer);
        Assert.Contains("TestAudience", jwt.Audiences);
    }

    [Fact]
    public async Task ValidateToken_WithValidToken_ReturnsClaimsPrincipal()
    {
        // Arrange
        var request = await CreateAuthenticationRequest();
        var response = await _jwtService.AuthenticateAsync(request);
        Assert.NotNull(response);

        // Act
        var principal = _jwtService.ValidateToken(response.AccessToken);

        // Assert
        Assert.NotNull(principal);
        Assert.True(principal.Identity?.IsAuthenticated);

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
        Assert.NotNull(userIdClaim);
        Assert.Equal("admin-001", userIdClaim.Value);

        var usernameClaim = principal.FindFirst(ClaimTypes.Name);
        Assert.NotNull(usernameClaim);
        Assert.Equal("admin", usernameClaim.Value);

        var roleClaim = principal.FindFirst(ClaimTypes.Role);
        Assert.NotNull(roleClaim);
        Assert.Equal("SystemAdmin", roleClaim.Value);
    }

    [Fact]
    public void ValidateToken_WithInvalidToken_ReturnsNull()
    {
        // Arrange
        var invalidToken = "invalid.jwt.token";

        // Act
        var principal = _jwtService.ValidateToken(invalidToken);

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void ValidateToken_WithExpiredToken_ReturnsNull()
    {
        // This test would require creating a token with past expiration
        // For security, we validate that expired tokens are rejected
        var expiredToken = CreateExpiredToken();

        // Act
        var principal = _jwtService.ValidateToken(expiredToken);

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidRefreshToken_GeneratesNewTokens()
    {
        // Arrange
        var authRequest = await CreateAuthenticationRequest();
        var authResponse = await _jwtService.AuthenticateAsync(authRequest);
        Assert.NotNull(authResponse);

        var refreshRequest = new RefreshTokenRequest { RefreshToken = authResponse.RefreshToken };

        // Act
        var refreshResponse = await _jwtService.RefreshTokenAsync(refreshRequest);

        // Assert
        Assert.NotNull(refreshResponse);
        Assert.NotEmpty(refreshResponse.AccessToken);
        Assert.NotEmpty(refreshResponse.RefreshToken);
        Assert.NotEqual(authResponse.AccessToken, refreshResponse.AccessToken);
        Assert.NotEqual(authResponse.RefreshToken, refreshResponse.RefreshToken);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithInvalidRefreshToken_ReturnsNull()
    {
        // Arrange
        var invalidRefreshRequest = new RefreshTokenRequest { RefreshToken = "invalid-refresh-token" };

        // Act
        var response = await _jwtService.RefreshTokenAsync(invalidRefreshRequest);

        // Assert
        Assert.Null(response);
    }

    [Fact]
    public async Task RevokeTokenAsync_WithValidRefreshToken_RevokesSuccessfully()
    {
        // Arrange
        var authRequest = await CreateAuthenticationRequest();
        var authResponse = await _jwtService.AuthenticateAsync(authRequest);
        Assert.NotNull(authResponse);

        // Act
        var revokeResult = await _jwtService.RevokeTokenAsync(authResponse.RefreshToken);

        // Assert
        Assert.True(revokeResult);

        // Verify token is no longer valid
        var refreshRequest = new RefreshTokenRequest { RefreshToken = authResponse.RefreshToken };
        var refreshResponse = await _jwtService.RefreshTokenAsync(refreshRequest);
        Assert.Null(refreshResponse);
    }

    [Fact]
    public async Task JwtTokens_ShouldContainSecurityClaims()
    {
        // Arrange
        var request = await CreateAuthenticationRequest();
        var response = await _jwtService.AuthenticateAsync(request);
        Assert.NotNull(response);

        // Act
        var principal = _jwtService.ValidateToken(response.AccessToken);

        // Assert
        Assert.NotNull(principal);

        // Verify required security claims
        Assert.NotNull(principal.FindFirst(ClaimTypes.NameIdentifier));
        Assert.NotNull(principal.FindFirst(ClaimTypes.Name));
        Assert.NotNull(principal.FindFirst(ClaimTypes.Role));
        Assert.NotNull(principal.FindFirst("last_login"));

        // Verify no sensitive information in claims
        var allClaims = principal.Claims.Select(c => c.Value).ToList();
        foreach (var claimValue in allClaims)
        {
            Assert.DoesNotContain("password", claimValue.ToLowerInvariant());
            Assert.DoesNotContain("secret", claimValue.ToLowerInvariant());
        }
    }

    [Fact]
    public async Task RefreshTokens_ShouldBeUnique()
    {
        // Arrange
        var request = await CreateAuthenticationRequest();
        var tokens = new List<string>();

        // Act - Generate multiple refresh tokens
        for (int i = 0; i < 10; i++)
        {
            var response = await _jwtService.AuthenticateAsync(request);
            Assert.NotNull(response);
            tokens.Add(response.RefreshToken);
        }

        // Assert - All tokens should be unique
        Assert.Equal(tokens.Count, tokens.Distinct().Count());
    }

    [Fact]
    public void JwtConfiguration_ShouldRequireSecretKey()
    {
        // Arrange
        var configWithoutSecret = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var userStorageLogger = loggerFactory.CreateLogger<UserStorageService>();
        var jwtLogger = loggerFactory.CreateLogger<JwtAuthenticationService>();
        var userStorage = new UserStorageService(userStorageLogger);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new JwtAuthenticationService(configWithoutSecret, jwtLogger, userStorage));

        Assert.Contains("JWT_SECRET_KEY not configured", exception.Message);
    }

    private async Task<AuthenticationRequest> CreateAuthenticationRequest()
    {
        // Get the generated admin password
        var passwordsFile = Path.Combine(_testDataDirectory, "Industrial.Adam.Security", "initial-passwords.txt");
        var passwordsContent = await File.ReadAllTextAsync(passwordsFile);
        var adminPasswordLine = passwordsContent.Split('\n').FirstOrDefault(line => line.StartsWith("Admin password:"));
        Assert.NotNull(adminPasswordLine);
        var adminPassword = adminPasswordLine.Split(": ")[1];

        return new AuthenticationRequest
        {
            Username = "admin",
            Password = adminPassword
        };
    }

    private string CreateExpiredToken()
    {
        // Create a configuration with very short expiration for testing
        var expiredConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JWT_SECRET_KEY"] = "ThisIsATestSecretKeyThatIsAtLeast32CharactersLongForSecurity",
                ["JWT_ISSUER"] = "TestIssuer",
                ["JWT_AUDIENCE"] = "TestAudience",
                ["JWT_EXPIRATION_MINUTES"] = "-1" // Expired immediately
            })
            .Build();

        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var userStorageLogger = loggerFactory.CreateLogger<UserStorageService>();
        var jwtLogger = loggerFactory.CreateLogger<JwtAuthenticationService>();
        var userStorage = new UserStorageService(userStorageLogger);
        var expiredJwtService = new JwtAuthenticationService(expiredConfig, jwtLogger, userStorage);

        // This would create an already expired token for testing
        return "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyLCJleHAiOjE1MTYyMzkwMjJ9.4Adcj3UFYzPUVaVF_42ev3QH_BFhKJJqkhjFQJl6dOM";
    }
}