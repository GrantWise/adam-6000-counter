using Industrial.Adam.Oee.Domain.Interfaces;
using Industrial.Adam.Oee.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Industrial.Adam.Oee.Tests.Unit.Infrastructure;

/// <summary>
/// Tests for dependency injection configuration
/// Verifies that all required services can be resolved from the DI container
/// </summary>
public class DependencyInjectionTests
{
    /// <summary>
    /// Test that all new repository interfaces can be resolved from DI container
    /// </summary>
    [Fact]
    public void AddOeeInfrastructure_CanResolveAllRepositories_Successfully()
    {
        // Arrange
        var services = new ServiceCollection();

        // Add required services
        services.AddLogging();

        // Create minimal configuration for OEE infrastructure
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Oee:Database:ConnectionString"] = "Host=localhost;Database=test;Username=test;Password=test;",
                ["Oee:Cache:DefaultExpirationMinutes"] = "5",
                ["Oee:Resilience:DatabaseRetry:MaxRetryAttempts"] = "3",
                ["Oee:Stoppage:DetectionWindowMinutes"] = "2",
                ["Oee:SignalR:EnableDetailedLogging"] = "false"
            })
            .Build();

        // Act
        services.AddOeeInfrastructure(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert - Verify all new repositories can be resolved
        Assert.NotNull(serviceProvider.GetService<ISimpleJobQueueRepository>());
        Assert.NotNull(serviceProvider.GetService<IEquipmentLineRepository>());
        Assert.NotNull(serviceProvider.GetService<IQualityRecordRepository>());

        // Verify existing repositories still work
        Assert.NotNull(serviceProvider.GetService<IWorkOrderRepository>());
        Assert.NotNull(serviceProvider.GetService<ICounterDataRepository>());
    }

    /// <summary>
    /// Test that repository implementations are registered as scoped services
    /// </summary>
    [Fact]
    public void AddOeeInfrastructure_RegistersRepositoriesAsScoped_Successfully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Oee:Database:ConnectionString"] = "Host=localhost;Database=test;Username=test;Password=test;",
                ["Oee:Cache:DefaultExpirationMinutes"] = "5",
                ["Oee:Resilience:DatabaseRetry:MaxRetryAttempts"] = "3",
                ["Oee:Stoppage:DetectionWindowMinutes"] = "2",
                ["Oee:SignalR:EnableDetailedLogging"] = "false"
            })
            .Build();

        // Act
        services.AddOeeInfrastructure(configuration);

        // Assert - Verify services are registered with correct lifetime
        var simpleJobQueueDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ISimpleJobQueueRepository));
        var equipmentLineDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IEquipmentLineRepository));
        var qualityRecordDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IQualityRecordRepository));

        Assert.NotNull(simpleJobQueueDescriptor);
        Assert.Equal(ServiceLifetime.Scoped, simpleJobQueueDescriptor.Lifetime);

        Assert.NotNull(equipmentLineDescriptor);
        Assert.Equal(ServiceLifetime.Scoped, equipmentLineDescriptor.Lifetime);

        Assert.NotNull(qualityRecordDescriptor);
        Assert.Equal(ServiceLifetime.Scoped, qualityRecordDescriptor.Lifetime);
    }
}
