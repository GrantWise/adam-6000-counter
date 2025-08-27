using Industrial.Adam.Security.Domain.ValueObjects;

namespace Industrial.Adam.Security.Domain.Services;

/// <summary>
/// Security configuration domain service interface
/// </summary>
public interface ISecurityConfigurationService
{
    /// <summary>
    /// Encrypts sensitive configuration values
    /// </summary>
    /// <param name="value">Plain text value</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Encrypted value</returns>
    Task<string> EncryptConfigurationValueAsync(string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypts encrypted configuration values
    /// </summary>
    /// <param name="encryptedValue">Encrypted value</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Decrypted plain text value</returns>
    Task<string> DecryptConfigurationValueAsync(string encryptedValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates configuration value based on type and security requirements
    /// </summary>
    /// <param name="configuration">Configuration to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<ConfigurationValidationResult> ValidateConfigurationAsync(SecurityConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines if a configuration value should be encrypted based on its type and content
    /// </summary>
    /// <param name="key">Configuration key</param>
    /// <param name="value">Configuration value</param>
    /// <param name="type">Configuration type</param>
    /// <returns>True if should be encrypted</returns>
    bool ShouldEncryptConfiguration(string key, string value, ConfigurationType type);

    /// <summary>
    /// Generates a secure random configuration value for secrets
    /// </summary>
    /// <param name="type">Type of secret to generate</param>
    /// <param name="length">Length of generated secret</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated secure value</returns>
    Task<string> GenerateSecureConfigurationValueAsync(SecretType type, int length = 32, CancellationToken cancellationToken = default);
}

/// <summary>
/// Configuration validation result
/// </summary>
public class ConfigurationValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public SecurityRiskLevel RiskLevel { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();

    public void AddError(string error)
    {
        Errors.Add(error);
        IsValid = false;
    }

    public void AddWarning(string warning)
    {
        Warnings.Add(warning);
    }
}

/// <summary>
/// Security risk levels for configuration
/// </summary>
public enum SecurityRiskLevel
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

/// <summary>
/// Types of secrets that can be generated
/// </summary>
public enum SecretType
{
    ApiKey = 1,
    Password = 2,
    Token = 3,
    Certificate = 4,
    SymmetricKey = 5
}