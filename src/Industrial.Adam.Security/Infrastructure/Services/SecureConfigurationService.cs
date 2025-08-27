using Industrial.Adam.Security.Domain.Services;
using Industrial.Adam.Security.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Industrial.Adam.Security.Infrastructure.Services;

/// <summary>
/// Secure configuration service implementation with encryption support
/// </summary>
public class SecureConfigurationService : ISecurityConfigurationService
{
    private readonly ILogger<SecureConfigurationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _encryptionKey;
    private static readonly Regex PasswordPattern = new(@"password|secret|key|token|credential", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ConnectionStringPattern = new(@"connectionstring|connstr|database_url", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public SecureConfigurationService(
        ILogger<SecureConfigurationService> logger,
        IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        
        // Get encryption key from environment - in production this should come from a secure key management service
        _encryptionKey = _configuration["SECURITY_ENCRYPTION_KEY"] 
            ?? throw new InvalidOperationException("SECURITY_ENCRYPTION_KEY must be configured");

        ValidateEncryptionKey(_encryptionKey);
    }

    public async Task<string> EncryptConfigurationValueAsync(string value, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentNullException(nameof(value));

            _logger.LogDebug("Encrypting configuration value");

            using var aes = Aes.Create();
            aes.Key = DeriveKeyFromPassword(_encryptionKey, 32);
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            var plainTextBytes = Encoding.UTF8.GetBytes(value);
            
            using var encryptor = aes.CreateEncryptor();
            using var msEncrypt = new MemoryStream();
            
            // Prepend IV to the encrypted data
            await msEncrypt.WriteAsync(aes.IV, cancellationToken);
            
            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            {
                await csEncrypt.WriteAsync(plainTextBytes, cancellationToken);
                await csEncrypt.FlushFinalBlockAsync(cancellationToken);
            }

            var encryptedBytes = msEncrypt.ToArray();
            var encryptedValue = Convert.ToBase64String(encryptedBytes);

            _logger.LogInformation("Successfully encrypted configuration value");
            return encryptedValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encrypting configuration value");
            throw;
        }
    }

    public async Task<string> DecryptConfigurationValueAsync(string encryptedValue, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(encryptedValue))
                throw new ArgumentNullException(nameof(encryptedValue));

            _logger.LogDebug("Decrypting configuration value");

            var encryptedBytes = Convert.FromBase64String(encryptedValue);
            
            using var aes = Aes.Create();
            aes.Key = DeriveKeyFromPassword(_encryptionKey, 32);
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            // Extract IV from the beginning of encrypted data
            var iv = new byte[aes.BlockSize / 8];
            var cipherText = new byte[encryptedBytes.Length - iv.Length];
            
            Buffer.BlockCopy(encryptedBytes, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(encryptedBytes, iv.Length, cipherText, 0, cipherText.Length);
            
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            using var msDecrypt = new MemoryStream(cipherText);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);
            
            var decryptedValue = await srDecrypt.ReadToEndAsync(cancellationToken);
            
            _logger.LogInformation("Successfully decrypted configuration value");
            return decryptedValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting configuration value");
            throw;
        }
    }

    public async Task<ConfigurationValidationResult> ValidateConfigurationAsync(SecurityConfiguration configuration, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Validating configuration {ConfigKey}", configuration.Key);

            var result = new ConfigurationValidationResult { IsValid = true };

            // Validate key format
            if (string.IsNullOrWhiteSpace(configuration.Key))
            {
                result.AddError("Configuration key cannot be empty");
            }
            else if (configuration.Key.Length > 200)
            {
                result.AddError("Configuration key is too long (maximum 200 characters)");
            }

            // Validate value
            if (string.IsNullOrWhiteSpace(configuration.Value))
            {
                result.AddError("Configuration value cannot be empty");
            }
            else if (configuration.Value.Length > 10000)
            {
                result.AddError("Configuration value is too long (maximum 10,000 characters)");
            }

            // Check for security risks in configuration values
            await ValidateSecurityRisksAsync(configuration, result, cancellationToken);

            // Validate encryption requirements
            if (ShouldEncryptConfiguration(configuration.Key, configuration.Value, configuration.Type))
            {
                if (!configuration.IsEncrypted)
                {
                    result.AddWarning($"Configuration '{configuration.Key}' should be encrypted based on its type and content");
                    result.RiskLevel = SecurityRiskLevel.High;
                }
            }

            _logger.LogInformation("Configuration validation completed for {ConfigKey}: {IsValid}", 
                configuration.Key, result.IsValid);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating configuration {ConfigKey}", configuration?.Key);
            throw;
        }
    }

    public bool ShouldEncryptConfiguration(string key, string value, ConfigurationType type)
    {
        // Always encrypt secrets and connection strings
        if (type == ConfigurationType.Secret || type == ConfigurationType.ConnectionString || type == ConfigurationType.Certificate)
        {
            return true;
        }

        // Check key patterns
        if (PasswordPattern.IsMatch(key) || ConnectionStringPattern.IsMatch(key))
        {
            return true;
        }

        // Check value patterns for sensitive data
        if (ContainsSensitiveData(value))
        {
            return true;
        }

        return false;
    }

    public async Task<string> GenerateSecureConfigurationValueAsync(SecretType type, int length = 32, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Generating secure configuration value of type {SecretType}", type);

            return type switch
            {
                SecretType.ApiKey => await GenerateApiKeyAsync(length, cancellationToken),
                SecretType.Password => await GeneratePasswordAsync(length, cancellationToken),
                SecretType.Token => await GenerateTokenAsync(length, cancellationToken),
                SecretType.SymmetricKey => await GenerateSymmetricKeyAsync(length, cancellationToken),
                SecretType.Certificate => await GenerateCertificateSecretAsync(length, cancellationToken),
                _ => throw new ArgumentException($"Unsupported secret type: {type}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating secure configuration value of type {SecretType}", type);
            throw;
        }
    }

    private async Task ValidateSecurityRisksAsync(SecurityConfiguration configuration, ConfigurationValidationResult result, CancellationToken cancellationToken)
    {
        await Task.Yield(); // Make async for future enhancements

        // Check for hardcoded credentials
        if (ContainsSensitiveData(configuration.Value) && !configuration.IsEncrypted)
        {
            result.AddError("Configuration contains sensitive data but is not encrypted");
            result.RiskLevel = SecurityRiskLevel.Critical;
        }

        // Check for SQL injection patterns in connection strings
        if (configuration.Type == ConfigurationType.ConnectionString)
        {
            if (configuration.Value.Contains("';") || configuration.Value.Contains("--"))
            {
                result.AddError("Connection string contains potentially dangerous characters");
                result.RiskLevel = SecurityRiskLevel.High;
            }
        }

        // Check for weak passwords
        if (configuration.Type == ConfigurationType.Secret && IsWeakPassword(configuration.Value))
        {
            result.AddWarning("Configuration value appears to be a weak password");
            result.RiskLevel = SecurityRiskLevel.Medium;
        }

        // Check for default/common values
        if (IsCommonOrDefaultValue(configuration.Key, configuration.Value))
        {
            result.AddWarning("Configuration appears to use a default or common value");
            result.RiskLevel = SecurityRiskLevel.Medium;
        }
    }

    private static bool ContainsSensitiveData(string value)
    {
        // Patterns that might indicate sensitive data
        var sensitivePatterns = new[]
        {
            @"password\s*=\s*[^\s;]+",
            @"pwd\s*=\s*[^\s;]+",
            @"secret\s*=\s*[^\s;]+",
            @"token\s*=\s*[^\s;]+",
            @"api[_-]?key\s*=\s*[^\s;]+",
            @"-----BEGIN\s+[\w\s]+KEY-----"
        };

        return sensitivePatterns.Any(pattern => 
            Regex.IsMatch(value, pattern, RegexOptions.IgnoreCase));
    }

    private static bool IsWeakPassword(string password)
    {
        // Basic weak password detection
        if (password.Length < 8) return true;
        if (!password.Any(char.IsDigit)) return true;
        if (!password.Any(char.IsUpper)) return true;
        if (!password.Any(char.IsLower)) return true;
        if (password.All(char.IsLetterOrDigit)) return true; // No special characters
        
        // Common weak passwords
        var commonPasswords = new[] { "password", "123456", "admin", "root", "default" };
        return commonPasswords.Contains(password.ToLowerInvariant());
    }

    private static bool IsCommonOrDefaultValue(string key, string value)
    {
        var commonValues = new Dictionary<string, string[]>
        {
            ["password"] = new[] { "password", "admin", "root", "123456", "default" },
            ["secret"] = new[] { "secret", "mysecret", "default" },
            ["key"] = new[] { "key", "mykey", "default" }
        };

        var keyLower = key.ToLowerInvariant();
        var valueLower = value.ToLowerInvariant();

        return commonValues.Any(kvp => 
            keyLower.Contains(kvp.Key) && 
            kvp.Value.Contains(valueLower));
    }

    private async Task<string> GenerateApiKeyAsync(int length, CancellationToken cancellationToken)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        using var rng = RandomNumberGenerator.Create();
        
        var result = new StringBuilder(length);
        var bytes = new byte[4];
        
        for (int i = 0; i < length; i++)
        {
            rng.GetBytes(bytes);
            var randomIndex = BitConverter.ToUInt32(bytes, 0) % chars.Length;
            result.Append(chars[(int)randomIndex]);
        }
        
        return await Task.FromResult(result.ToString());
    }

    private async Task<string> GeneratePasswordAsync(int length, CancellationToken cancellationToken)
    {
        const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowercase = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%^&*()_+-=[]{}|;:,.<>?";
        
        var allChars = uppercase + lowercase + digits + special;
        
        using var rng = RandomNumberGenerator.Create();
        var result = new StringBuilder(length);
        
        // Ensure at least one character from each category
        result.Append(GetRandomChar(uppercase, rng));
        result.Append(GetRandomChar(lowercase, rng));
        result.Append(GetRandomChar(digits, rng));
        result.Append(GetRandomChar(special, rng));
        
        // Fill the rest randomly
        for (int i = 4; i < length; i++)
        {
            result.Append(GetRandomChar(allChars, rng));
        }
        
        // Shuffle the result
        return await Task.FromResult(ShuffleString(result.ToString(), rng));
    }

    private async Task<string> GenerateTokenAsync(int length, CancellationToken cancellationToken)
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[length];
        rng.GetBytes(bytes);
        return await Task.FromResult(Convert.ToBase64String(bytes));
    }

    private async Task<string> GenerateSymmetricKeyAsync(int length, CancellationToken cancellationToken)
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[length];
        rng.GetBytes(bytes);
        return await Task.FromResult(Convert.ToHexString(bytes).ToLowerInvariant());
    }

    private async Task<string> GenerateCertificateSecretAsync(int length, CancellationToken cancellationToken)
    {
        // Generate a strong passphrase for certificate protection
        return await GeneratePasswordAsync(Math.Max(length, 16), cancellationToken);
    }

    private static char GetRandomChar(string chars, RandomNumberGenerator rng)
    {
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        var randomIndex = BitConverter.ToUInt32(bytes, 0) % chars.Length;
        return chars[(int)randomIndex];
    }

    private static string ShuffleString(string input, RandomNumberGenerator rng)
    {
        var array = input.ToCharArray();
        var n = array.Length;
        
        for (int i = n - 1; i > 0; i--)
        {
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            var j = (int)(BitConverter.ToUInt32(bytes, 0) % (i + 1));
            (array[i], array[j]) = (array[j], array[i]);
        }
        
        return new string(array);
    }

    private static byte[] DeriveKeyFromPassword(string password, int keyLength)
    {
        using var sha256 = SHA256.Create();
        var passwordBytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(passwordBytes);
        
        // If we need more bytes than SHA256 provides, repeat the hash
        var key = new byte[keyLength];
        var hashLength = hash.Length;
        
        for (int i = 0; i < keyLength; i++)
        {
            key[i] = hash[i % hashLength];
        }
        
        return key;
    }

    private static void ValidateEncryptionKey(string encryptionKey)
    {
        if (string.IsNullOrWhiteSpace(encryptionKey))
            throw new ArgumentException("Encryption key cannot be null or empty");
            
        if (encryptionKey.Length < 32)
            throw new ArgumentException("Encryption key must be at least 32 characters long");
    }
}