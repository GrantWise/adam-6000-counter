using System.Data;
using Industrial.Adam.Oee.Application;
using Industrial.Adam.Oee.Application.Interfaces;
using Industrial.Adam.Oee.Domain;
using Industrial.Adam.Oee.Domain.Interfaces;
using Industrial.Adam.Oee.Infrastructure;
using Industrial.Adam.Oee.Infrastructure.Configuration;
using Industrial.Adam.Oee.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Industrial.Adam.Oee.Tests.Integration.Infrastructure;

/// <summary>
/// Integration tests for dependency injection configuration
/// </summary>
public class DependencyInjectionTests
{
    /// <summary>
    /// Test that all services can be resolved successfully with minimal configuration
    /// </summary>
    [Fact]
    public void AddOeeInfrastructure_WithMinimalConfiguration_ResolvesAllServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateMinimalConfiguration();

        // Add logging for tests
        services.AddLogging(builder => builder.AddConsole());

        // Act - Register all layers like Program.cs does
        services.AddOeeDomain();
        services.AddOeeApplication();
        services.AddOeeInfrastructure(configuration);

        // Build service provider
        using var serviceProvider = services.BuildServiceProvider();

        // Assert - Core interfaces can be resolved
        Assert.NotNull(serviceProvider.GetRequiredService<IDbConnectionFactory>());
        Assert.NotNull(serviceProvider.GetRequiredService<ICacheService>());
        Assert.NotNull(serviceProvider.GetRequiredService<IOeeApplicationService>());
        Assert.NotNull(serviceProvider.GetRequiredService<ICounterDataRepository>());
        Assert.NotNull(serviceProvider.GetRequiredService<IWorkOrderRepository>());
        Assert.NotNull(serviceProvider.GetRequiredService<IDatabaseMigrationService>());

        // Assert - Configuration is bound
        var oeeConfig = serviceProvider.GetRequiredService<IOptions<OeeConfiguration>>();
        Assert.NotNull(oeeConfig.Value);

        var dbConfig = serviceProvider.GetRequiredService<IOptions<OeeDatabaseSettings>>();
        Assert.NotNull(dbConfig.Value);
        Assert.Equal("Host=localhost;Database=test_oee;Username=test;Password=test;", dbConfig.Value.ConnectionString);
    }

    /// <summary>
    /// Test configuration validation with missing sections
    /// </summary>
    [Fact]
    public void AddOeeInfrastructure_WithMissingOeeSection_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build(); // Empty configuration

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddOeeInfrastructure(configuration));

        Assert.Contains("Missing 'Oee' configuration section", exception.Message);
    }

    /// <summary>
    /// Test configuration validation with missing connection string
    /// </summary>
    [Fact]
    public void AddOeeInfrastructure_WithMissingConnectionString_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Oee:Cache:DefaultExpirationMinutes"] = "5",
                ["Oee:Resilience:DatabaseRetry:MaxRetryAttempts"] = "3"
            })
            .Build();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddOeeInfrastructure(configuration));

        Assert.Contains("Missing database connection string", exception.Message);
    }

    /// <summary>
    /// Test health checks registration
    /// </summary>
    [Fact]
    public void AddOeeInfrastructure_RegistersHealthChecks()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateMinimalConfiguration();
        services.AddLogging(builder => builder.AddConsole());

        // Act
        services.AddOeeInfrastructure(configuration);

        // Build service provider
        using var serviceProvider = services.BuildServiceProvider();

        // Assert
        var healthCheckService = serviceProvider.GetRequiredService<HealthCheckService>();
        Assert.NotNull(healthCheckService);
    }

    /// <summary>
    /// Test configuration binding with all sections
    /// </summary>
    [Fact]
    public void AddOeeInfrastructure_WithCompleteConfiguration_BindsAllSettings()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateCompleteConfiguration();
        services.AddLogging(builder => builder.AddConsole());

        // Act - Register all layers
        services.AddOeeDomain();
        services.AddOeeApplication();
        services.AddOeeInfrastructure(configuration);

        // Build service provider
        using var serviceProvider = services.BuildServiceProvider();

        // Assert - Database settings
        var dbSettings = serviceProvider.GetRequiredService<IOptions<OeeDatabaseSettings>>().Value;
        Assert.Equal("Host=localhost;Database=test_oee;Username=test;Password=test;", dbSettings.ConnectionString);
        Assert.Equal(45, dbSettings.ConnectionTimeoutSeconds);
        Assert.Equal(120, dbSettings.CommandTimeoutSeconds);

        // Assert - Cache settings
        var cacheSettings = serviceProvider.GetRequiredService<IOptions<OeeCacheSettings>>().Value;
        Assert.Equal(10, cacheSettings.DefaultExpirationMinutes);
        Assert.Equal(3, cacheSettings.OeeMetricsExpirationMinutes);

        // Assert - Resilience settings
        var resilienceSettings = serviceProvider.GetRequiredService<IOptions<OeeResilienceSettings>>().Value;
        Assert.Equal(5, resilienceSettings.DatabaseRetry.MaxRetryAttempts);
        Assert.Equal(2000, resilienceSettings.DatabaseRetry.BaseDelayMs);
    }

    /// <summary>
    /// Test that resilience configuration is properly bound
    /// </summary>
    [Fact]
    public void AddOeeInfrastructure_ConfiguresResiliencePolicy()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateMinimalConfiguration();
        services.AddLogging(builder => builder.AddConsole());

        // Act
        services.AddOeeDomain();
        services.AddOeeApplication();
        services.AddOeeInfrastructure(configuration);

        // Build service provider
        using var serviceProvider = services.BuildServiceProvider();

        // Assert - Verify resilience configuration is bound (not actual Polly registration as noted in DI comments)
        var resilienceSettings = serviceProvider.GetService<IOptions<OeeResilienceSettings>>();
        Assert.NotNull(resilienceSettings);
        Assert.Equal(3, resilienceSettings.Value.DatabaseRetry.MaxRetryAttempts);
    }

    /// <summary>
    /// Create minimal valid configuration for testing
    /// </summary>
    private static IConfiguration CreateMinimalConfiguration()
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

    /// <summary>
    /// Create complete configuration for testing
    /// </summary>
    private static IConfiguration CreateCompleteConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Database settings
                ["Oee:Database:ConnectionString"] = "Host=localhost;Database=test_oee;Username=test;Password=test;",
                ["Oee:Database:ConnectionTimeoutSeconds"] = "45",
                ["Oee:Database:CommandTimeoutSeconds"] = "120",
                ["Oee:Database:EnableConnectionPooling"] = "true",
                ["Oee:Database:MaxPoolSize"] = "200",

                // Cache settings
                ["Oee:Cache:DefaultExpirationMinutes"] = "10",
                ["Oee:Cache:OeeMetricsExpirationMinutes"] = "3",
                ["Oee:Cache:WorkOrderExpirationMinutes"] = "15",
                ["Oee:Cache:DeviceStatusExpirationMinutes"] = "2",

                // Resilience settings
                ["Oee:Resilience:DatabaseRetry:MaxRetryAttempts"] = "5",
                ["Oee:Resilience:DatabaseRetry:BaseDelayMs"] = "2000",
                ["Oee:Resilience:DatabaseRetry:UseExponentialBackoff"] = "true",
                ["Oee:Resilience:DatabaseRetry:MaxDelayMs"] = "60000",
                ["Oee:Resilience:CircuitBreaker:ExceptionsAllowedBeforeBreaking"] = "10",
                ["Oee:Resilience:CircuitBreaker:DurationOfBreakSeconds"] = "60",

                // Performance settings
                ["Oee:Performance:Enabled"] = "true",
                ["Oee:Performance:EnableDetailedMetrics"] = "true",
                ["Oee:Performance:SlowQueryThresholdMs"] = "2000",
                ["Oee:Performance:LogSlowQueries"] = "true"
            })
            .Build();
    }
}
