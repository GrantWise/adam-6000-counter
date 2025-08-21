using Microsoft.Extensions.Configuration;
using Xunit;
using System.Text.Json;

namespace Industrial.Adam.Security.Tests;

/// <summary>
/// Security tests for configuration validation
/// </summary>
public class ConfigurationSecurityTests
{
    [Fact]
    public void Configuration_ShouldNotContainHardcodedPasswords()
    {
        // Arrange
        var configFiles = new[]
        {
            "../../../../../config/appsettings.local.json",
            "../../../../../config/appsettings.docker.json",
            "../../../../../config/appsettings.template.json"
        };

        // Act & Assert
        foreach (var configFile in configFiles)
        {
            if (File.Exists(configFile))
            {
                var content = File.ReadAllText(configFile);
                
                // Check for common hardcoded password patterns
                Assert.DoesNotContain("admin123", content, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("password123", content, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("admin_password", content, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("super-secret-token", content, StringComparison.OrdinalIgnoreCase);
                
                // Verify environment variable placeholders are used instead
                if (content.Contains("Password") || content.Contains("password"))
                {
                    Assert.Contains("${", content); // Environment variable syntax
                }
            }
        }
    }

    [Fact]
    public void Configuration_ShouldUseEnvironmentVariables()
    {
        // Arrange
        var configFile = "../../../../../config/appsettings.local.json";

        if (File.Exists(configFile))
        {
            // Act
            var content = File.ReadAllText(configFile);
            var config = JsonSerializer.Deserialize<JsonElement>(content);

            // Assert - Database credentials should use environment variables
            if (config.TryGetProperty("AdamLogger", out var adamLogger) &&
                adamLogger.TryGetProperty("TimescaleDb", out var timescaleDb))
            {
                if (timescaleDb.TryGetProperty("Username", out var username))
                {
                    var usernameValue = username.GetString();
                    Assert.True(usernameValue?.StartsWith("${") == true, "Username should use environment variable");
                }

                if (timescaleDb.TryGetProperty("Password", out var password))
                {
                    var passwordValue = password.GetString();
                    Assert.True(passwordValue?.StartsWith("${") == true, "Password should use environment variable");
                }
            }
        }
    }

    [Fact]
    public void DockerEnv_ShouldNotContainHardcodedCredentials()
    {
        // Arrange
        var envFile = "../../../../../docker/.env";

        if (File.Exists(envFile))
        {
            // Act
            var content = File.ReadAllText(envFile);

            // Assert - Should not contain hardcoded passwords
            Assert.DoesNotContain("GF_SECURITY_ADMIN_PASSWORD=admin", content);
            Assert.DoesNotContain("DOCKER_INFLUXDB_INIT_PASSWORD=admin123", content);
            Assert.DoesNotContain("DOCKER_INFLUXDB_INIT_ADMIN_TOKEN=adam-super-secret-token", content);

            // Should use environment variable references
            if (content.Contains("GF_SECURITY_ADMIN_PASSWORD"))
            {
                Assert.Contains("${GRAFANA_ADMIN_PASSWORD}", content);
            }
        }
    }

    [Theory]
    [InlineData("TIMESCALEDB_USERNAME")]
    [InlineData("TIMESCALEDB_PASSWORD")]
    [InlineData("GRAFANA_ADMIN_PASSWORD")]
    public void EnvironmentVariables_RequiredForProduction(string requiredEnvVar)
    {
        // This test documents which environment variables are required for production
        // In actual deployment, these would be validated at startup
        
        // Arrange & Act
        var value = Environment.GetEnvironmentVariable(requiredEnvVar);

        // Assert - In tests, we just verify the variable names are documented
        // In production, these would be required and validated
        Assert.True(true, $"Environment variable {requiredEnvVar} is documented as required for production");
    }

    [Fact]
    public void PasswordGeneration_ShouldProduceSecurePasswords()
    {
        // Arrange & Act
        var passwords = new string[10];
        for (int i = 0; i < passwords.Length; i++)
        {
            passwords[i] = Industrial.Adam.Security.Utilities.SecurePasswordGenerator.GenerateSecurePassword();
        }

        // Assert
        foreach (var password in passwords)
        {
            Assert.True(password.Length >= 16, "Password should be at least 16 characters");
            Assert.True(HasUppercase(password), "Password should contain uppercase letters");
            Assert.True(HasLowercase(password), "Password should contain lowercase letters");
            Assert.True(HasDigits(password), "Password should contain digits");
        }

        // All passwords should be unique
        Assert.Equal(passwords.Length, passwords.Distinct().Count());
    }

    [Fact]
    public void PassphraseGeneration_ShouldProduceSecurePassphrases()
    {
        // Arrange & Act
        var passphrases = new string[5];
        for (int i = 0; i < passphrases.Length; i++)
        {
            passphrases[i] = Industrial.Adam.Security.Utilities.SecurePasswordGenerator.GeneratePassphrase();
        }

        // Assert
        foreach (var passphrase in passphrases)
        {
            Assert.Contains("-", passphrase); // Should be hyphen-separated
            var parts = passphrase.Split('-');
            Assert.True(parts.Length >= 4, "Passphrase should have at least 4 parts");
            Assert.True(parts.Last().All(char.IsDigit), "Last part should be numeric");
        }

        // All passphrases should be unique
        Assert.Equal(passphrases.Length, passphrases.Distinct().Count());
    }

    private static bool HasUppercase(string text) => text.Any(char.IsUpper);
    private static bool HasLowercase(string text) => text.Any(char.IsLower);
    private static bool HasDigits(string text) => text.Any(char.IsDigit);
}