using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace Industrial.Adam.Security.Infrastructure.Services;

/// <summary>
/// Multi-Factor Authentication service for enhanced security
/// </summary>
public class MultiFactorAuthenticationService
{
    private readonly ILogger<MultiFactorAuthenticationService> _logger;
    private const int TokenLength = 6;
    private const int TokenValidityMinutes = 5;
    private const string Issuer = "Industrial.Adam.Security";

    public MultiFactorAuthenticationService(ILogger<MultiFactorAuthenticationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generates a new MFA secret for a user
    /// </summary>
    public async Task<MfaSetupResult> SetupMfaAsync(string userId, string username, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Setting up MFA for user {UserId}", userId);

            // Generate a random secret (base32 encoded for TOTP compatibility)
            var secret = GenerateSecret();
            
            // Generate QR code data for authenticator apps
            var qrCodeData = GenerateQrCodeData(username, secret);
            
            // Generate backup codes
            var backupCodes = GenerateBackupCodes(10);

            var result = new MfaSetupResult
            {
                Secret = secret,
                QrCodeData = qrCodeData,
                BackupCodes = backupCodes,
                SetupAt = DateTime.UtcNow
            };

            _logger.LogInformation("MFA setup completed for user {UserId}", userId);
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting up MFA for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Verifies a TOTP token
    /// </summary>
    public async Task<bool> VerifyTotpTokenAsync(string secret, string token, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            _logger.LogDebug("Verifying TOTP token");

            // Allow for time drift by checking current and adjacent time windows
            var currentTimeStep = GetCurrentTimeStep();
            
            for (int i = -1; i <= 1; i++)
            {
                var timeStep = currentTimeStep + i;
                var expectedToken = GenerateTotpToken(secret, timeStep);
                
                if (expectedToken == token)
                {
                    _logger.LogInformation("TOTP token verification successful");
                    return await Task.FromResult(true);
                }
            }

            _logger.LogWarning("TOTP token verification failed");
            return await Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying TOTP token");
            return false;
        }
    }

    /// <summary>
    /// Verifies a backup code
    /// </summary>
    public async Task<MfaVerificationResult> VerifyBackupCodeAsync(
        string userId, 
        string backupCode, 
        List<string> validBackupCodes,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Verifying backup code for user {UserId}", userId);

            if (string.IsNullOrWhiteSpace(backupCode) || validBackupCodes == null || !validBackupCodes.Any())
            {
                return new MfaVerificationResult { IsValid = false, ErrorMessage = "Invalid backup code" };
            }

            // Check if the backup code exists and is valid
            var hashedCode = HashBackupCode(backupCode);
            if (!validBackupCodes.Contains(hashedCode))
            {
                _logger.LogWarning("Invalid backup code provided for user {UserId}", userId);
                return new MfaVerificationResult { IsValid = false, ErrorMessage = "Invalid backup code" };
            }

            // Remove the used backup code
            validBackupCodes.Remove(hashedCode);
            
            _logger.LogInformation("Backup code verification successful for user {UserId}", userId);
            return await Task.FromResult(new MfaVerificationResult 
            { 
                IsValid = true,
                RemainingBackupCodes = validBackupCodes,
                WasBackupCodeUsed = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying backup code for user {UserId}", userId);
            return new MfaVerificationResult { IsValid = false, ErrorMessage = "Internal error during verification" };
        }
    }

    /// <summary>
    /// Generates new backup codes for a user
    /// </summary>
    public async Task<List<string>> RegenerateBackupCodesAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Regenerating backup codes for user {UserId}", userId);
            
            var backupCodes = GenerateBackupCodes(10);
            
            _logger.LogInformation("Successfully regenerated backup codes for user {UserId}", userId);
            return await Task.FromResult(backupCodes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating backup codes for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Validates MFA requirements for a user action
    /// </summary>
    public async Task<bool> IsMfaRequiredForActionAsync(string userId, string action, CancellationToken cancellationToken = default)
    {
        try
        {
            // Define actions that always require MFA
            var mfaRequiredActions = new[]
            {
                "user_management",
                "security_configuration",
                "system_administration",
                "audit_export",
                "password_reset_bulk"
            };

            var requiresMfa = mfaRequiredActions.Contains(action.ToLowerInvariant());
            
            _logger.LogDebug("MFA requirement check for user {UserId} action {Action}: {RequiresMfa}", 
                userId, action, requiresMfa);
            
            return await Task.FromResult(requiresMfa);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking MFA requirements for user {UserId} action {Action}", userId, action);
            return true; // Fail secure - require MFA on error
        }
    }

    private static string GenerateSecret()
    {
        // Generate 160-bit secret (20 bytes) as recommended for TOTP
        using var rng = RandomNumberGenerator.Create();
        var secretBytes = new byte[20];
        rng.GetBytes(secretBytes);
        
        // Convert to Base32 for TOTP compatibility
        return ToBase32(secretBytes);
    }

    private string GenerateQrCodeData(string username, string secret)
    {
        // Generate TOTP URI for QR code
        var uri = $"otpauth://totp/{Uri.EscapeDataString(Issuer)}:{Uri.EscapeDataString(username)}?" +
                  $"secret={secret}&issuer={Uri.EscapeDataString(Issuer)}&algorithm=SHA1&digits={TokenLength}&period=30";
        
        return uri;
    }

    private List<string> GenerateBackupCodes(int count)
    {
        var codes = new List<string>();
        using var rng = RandomNumberGenerator.Create();
        
        for (int i = 0; i < count; i++)
        {
            var codeBytes = new byte[5]; // 10 character code
            rng.GetBytes(codeBytes);
            var code = Convert.ToHexString(codeBytes).ToLowerInvariant();
            codes.Add(HashBackupCode(code));
        }
        
        return codes;
    }

    private static string HashBackupCode(string code)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(code.ToLowerInvariant()));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static long GetCurrentTimeStep()
    {
        var unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return unixTime / 30; // 30-second time window
    }

    private static string GenerateTotpToken(string secret, long timeStep)
    {
        var secretBytes = FromBase32(secret);
        var timeStepBytes = BitConverter.GetBytes(timeStep);
        
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(timeStepBytes);
        }

        using var hmac = new HMACSHA1(secretBytes);
        var hash = hmac.ComputeHash(timeStepBytes);
        
        var offset = hash[^1] & 0x0F;
        var truncatedHash = ((hash[offset] & 0x7F) << 24) |
                           ((hash[offset + 1] & 0xFF) << 16) |
                           ((hash[offset + 2] & 0xFF) << 8) |
                           (hash[offset + 3] & 0xFF);
        
        var token = (truncatedHash % (int)Math.Pow(10, TokenLength)).ToString().PadLeft(TokenLength, '0');
        return token;
    }

    private static string ToBase32(byte[] input)
    {
        if (input == null || input.Length == 0)
            return string.Empty;

        const string base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var output = new StringBuilder();
        
        for (int i = 0; i < input.Length; i += 5)
        {
            var b = new byte[8];
            var count = Math.Min(5, input.Length - i);
            
            Array.Copy(input, i, b, 0, count);
            
            ulong value = ((ulong)b[0] << 32) | ((ulong)b[1] << 24) | ((ulong)b[2] << 16) | ((ulong)b[3] << 8) | b[4];
            
            for (int j = 0; j < 8; j++)
            {
                if (count * 8 / 5 > j)
                {
                    output.Append(base32Chars[(int)((value >> (35 - j * 5)) & 0x1F)]);
                }
            }
        }
        
        return output.ToString();
    }

    private static byte[] FromBase32(string input)
    {
        if (string.IsNullOrEmpty(input))
            return Array.Empty<byte>();

        input = input.ToUpperInvariant();
        var output = new List<byte>();
        var bits = 0;
        var value = 0;

        foreach (char c in input)
        {
            var index = c switch
            {
                >= 'A' and <= 'Z' => c - 'A',
                >= '2' and <= '7' => c - '2' + 26,
                _ => -1
            };

            if (index < 0) continue;

            value = (value << 5) | index;
            bits += 5;

            if (bits >= 8)
            {
                output.Add((byte)(value >> (bits - 8)));
                bits -= 8;
            }
        }

        return output.ToArray();
    }
}

/// <summary>
/// MFA setup result
/// </summary>
public class MfaSetupResult
{
    public string Secret { get; set; } = string.Empty;
    public string QrCodeData { get; set; } = string.Empty;
    public List<string> BackupCodes { get; set; } = new();
    public DateTime SetupAt { get; set; }
}

/// <summary>
/// MFA verification result
/// </summary>
public class MfaVerificationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string>? RemainingBackupCodes { get; set; }
    public bool WasBackupCodeUsed { get; set; }
}