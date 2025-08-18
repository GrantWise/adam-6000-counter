using Industrial.Adam.Oee.Infrastructure;
using Industrial.Adam.Oee.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Industrial.Adam.Oee.Tests.Integration.Configuration;

/// <summary>
/// Integration tests for OEE configuration validation
/// </summary>
public class ConfigurationValidationTests
{
    /// <summary>
    /// Test that valid configuration passes validation
    /// </summary>
    [Fact]
    public void ValidConfiguration_PassesValidation()
    {
        // Arrange
        var configuration = CreateValidConfiguration();
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());

        // Act & Assert - Should not throw
        services.AddOeeInfrastructure(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        var oeeConfig = serviceProvider.GetRequiredService<IOptions<OeeConfiguration>>();

        Assert.NotNull(oeeConfig.Value);
        Assert.NotEmpty(oeeConfig.Value.Database.ConnectionString);
    }

    /// <summary>
    /// Test that missing Oee section fails validation
    /// </summary>
    [Fact]
    public void MissingOeeSection_FailsValidation()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SomeOtherSection:Value"] = "test"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddOeeInfrastructure(configuration));

        Assert.Contains("Missing 'Oee' configuration section", exception.Message);
        Assert.Contains("The configuration must be structured as: { \"Oee\": { \"Database\": { \"ConnectionString\": \"...\" } } }", exception.Message);
    }

    /// <summary>
    /// Test that missing database connection string fails validation
    /// </summary>
    [Fact]
    public void MissingDatabaseConnectionString_FailsValidation()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Oee:Cache:DefaultExpirationMinutes"] = "5",
                ["Oee:Resilience:DatabaseRetry:MaxRetryAttempts"] = "3"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddOeeInfrastructure(configuration));

        Assert.Contains("Missing database connection string", exception.Message);
        Assert.Contains("Configure either 'Oee:Database:ConnectionString' or 'ConnectionStrings:DefaultConnection'", exception.Message);
    }

    /// <summary>
    /// Test that missing cache section fails validation
    /// </summary>
    [Fact]
    public void MissingCacheSection_FailsValidation()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Oee:Database:ConnectionString"] = "Host=localhost;Database=test;",
                ["Oee:Resilience:DatabaseRetry:MaxRetryAttempts"] = "3"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddOeeInfrastructure(configuration));

        Assert.Contains("Missing 'Oee:Cache' configuration section", exception.Message);
        Assert.Contains("Add cache settings: { \"Oee\": { \"Cache\": { \"DefaultExpirationMinutes\": 5 } } }", exception.Message);
    }

    /// <summary>
    /// Test that missing resilience section fails validation
    /// </summary>
    [Fact]
    public void MissingResilienceSection_FailsValidation()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Oee:Database:ConnectionString"] = "Host=localhost;Database=test;",
                ["Oee:Cache:DefaultExpirationMinutes"] = "5"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddOeeInfrastructure(configuration));

        Assert.Contains("Missing 'Oee:Resilience' configuration section", exception.Message);
        Assert.Contains("Add resilience settings: { \"Oee\": { \"Resilience\": { \"DatabaseRetry\": { \"MaxRetryAttempts\": 3 } } } }", exception.Message);
    }

    /// <summary>
    /// Test that DefaultConnection fallback works
    /// </summary>
    [Fact]
    public void DefaultConnectionFallback_Works()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=fallback_test;",
                ["Oee:Cache:DefaultExpirationMinutes"] = "5",
                ["Oee:Resilience:DatabaseRetry:MaxRetryAttempts"] = "3",
                ["Oee:Resilience:DatabaseRetry:BaseDelayMs"] = "1000",
                ["Oee:Resilience:DatabaseRetry:UseExponentialBackoff"] = "true",
                ["Oee:Resilience:DatabaseRetry:MaxDelayMs"] = "30000"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());

        // Act & Assert - Should not throw
        services.AddOeeInfrastructure(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        var dbSettings = serviceProvider.GetRequiredService<IOptions<OeeDatabaseSettings>>();

        // Should use the fallback connection string
        Assert.Equal("Host=localhost;Database=fallback_test;", dbSettings.Value.ConnectionString);
    }

    /// <summary>
    /// Test that Oee database connection string takes precedence over DefaultConnection
    /// </summary>
    [Fact]
    public void OeeDatabaseConnectionString_TakesPrecedence()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=fallback_test;",
                ["Oee:Database:ConnectionString"] = "Host=localhost;Database=oee_primary;",
                ["Oee:Cache:DefaultExpirationMinutes"] = "5",
                ["Oee:Resilience:DatabaseRetry:MaxRetryAttempts"] = "3",
                ["Oee:Resilience:DatabaseRetry:BaseDelayMs"] = "1000",
                ["Oee:Resilience:DatabaseRetry:UseExponentialBackoff"] = "true",
                ["Oee:Resilience:DatabaseRetry:MaxDelayMs"] = "30000"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());

        // Act
        services.AddOeeInfrastructure(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        var dbSettings = serviceProvider.GetRequiredService<IOptions<OeeDatabaseSettings>>();

        // Assert - Should use the Oee-specific connection string
        Assert.Equal("Host=localhost;Database=oee_primary;", dbSettings.Value.ConnectionString);
    }

    /// <summary>
    /// Test comprehensive error message with multiple missing sections
    /// </summary>
    [Fact]
    public void MultipleMissingSections_ShowsAllErrors()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Oee:SomeOtherProperty"] = "value"
                // Missing Database, Cache, and Resilience sections
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddOeeInfrastructure(configuration));

        // Should contain all validation errors
        Assert.Contains("Missing database connection string", exception.Message);
        Assert.Contains("Missing 'Oee:Cache' configuration section", exception.Message);
        Assert.Contains("Missing 'Oee:Resilience' configuration section", exception.Message);
        Assert.Contains("OEE Configuration validation failed:", exception.Message);
    }

    /// <summary>
    /// Test configuration binding with all default values
    /// </summary>
    [Fact]
    public void ConfigurationBinding_WithDefaults_Works()
    {
        // Arrange
        var configuration = CreateValidConfiguration();
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());

        // Act
        services.AddOeeInfrastructure(configuration);

        using var serviceProvider = services.BuildServiceProvider();

        // Assert default values are applied
        var oeeConfig = serviceProvider.GetRequiredService<IOptions<OeeConfiguration>>().Value;

        Assert.Equal(30, oeeConfig.Database.ConnectionTimeoutSeconds);
        Assert.Equal(60, oeeConfig.Database.CommandTimeoutSeconds);
        Assert.True(oeeConfig.Database.EnableConnectionPooling);
        Assert.Equal(100, oeeConfig.Database.MaxPoolSize);

        Assert.Equal(5, oeeConfig.Cache.DefaultExpirationMinutes);
        Assert.Equal(2, oeeConfig.Cache.OeeMetricsExpirationMinutes);
        Assert.Equal(10, oeeConfig.Cache.WorkOrderExpirationMinutes);
        Assert.Equal(1, oeeConfig.Cache.DeviceStatusExpirationMinutes);

        Assert.Equal(3, oeeConfig.Resilience.DatabaseRetry.MaxRetryAttempts);
        Assert.Equal(1000, oeeConfig.Resilience.DatabaseRetry.BaseDelayMs);
        Assert.True(oeeConfig.Resilience.DatabaseRetry.UseExponentialBackoff);
        Assert.Equal(30000, oeeConfig.Resilience.DatabaseRetry.MaxDelayMs);
    }

    /// <summary>
    /// Create valid configuration for testing
    /// </summary>
    private static IConfiguration CreateValidConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Oee:Database:ConnectionString"] = "Host=localhost;Database=test_oee;Username=test;Password=test;",
                ["Oee:Cache:DefaultExpirationMinutes"] = "5",
                ["Oee:Resilience:DatabaseRetry:MaxRetryAttempts"] = "3",
                ["Oee:Resilience:DatabaseRetry:BaseDelayMs"] = "1000",
                ["Oee:Resilience:DatabaseRetry:UseExponentialBackoff"] = "true",
                ["Oee:Resilience:DatabaseRetry:MaxDelayMs"] = "30000"
            })
            .Build();
    }
}
