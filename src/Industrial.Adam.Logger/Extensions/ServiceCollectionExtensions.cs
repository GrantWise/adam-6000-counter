// Industrial.Adam.Logger - Dependency Injection Extensions
// Extension methods for registering the ADAM Logger service and its dependencies

using Industrial.Adam.Logger.Configuration;
using Industrial.Adam.Logger.ErrorHandling;
using Industrial.Adam.Logger.Health;
using Industrial.Adam.Logger.Health.Checks;
using Industrial.Adam.Logger.Infrastructure;
using Industrial.Adam.Logger.Interfaces;
using Industrial.Adam.Logger.Logging;
using Industrial.Adam.Logger.Monitoring;
using Industrial.Adam.Logger.Performance;
using Industrial.Adam.Logger.Services;
using Industrial.Adam.Logger.Testing;
using Industrial.Adam.Logger.Testing.Tests;
using Industrial.Adam.Logger.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Industrial.Adam.Logger.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to register ADAM Logger services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register the ADAM Logger as a reusable service with all required dependencies
    /// </summary>
    /// <param name="services">Service collection to register services with</param>
    /// <param name="configureOptions">Configuration action for setting up ADAM Logger options</param>
    /// <returns>Service collection for method chaining</returns>
    /// <example>
    /// <code>
    /// services.AddAdamLogger(config =>
    /// {
    ///     config.PollIntervalMs = 1000;
    ///     config.Devices.Add(new AdamDeviceConfig
    ///     {
    ///         DeviceId = "LINE1_ADAM",
    ///         IpAddress = "192.168.1.100",
    ///         Channels = { /* channel configurations */ }
    ///     });
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddAdamLogger(this IServiceCollection services, Action<AdamLoggerConfig> configureOptions)
    {
        // Register configuration
        services.Configure(configureOptions);

        // Register core services with default implementations
        services.AddSingleton<IDataValidator, DefaultDataValidator>();
        services.AddSingleton<IDataTransformer, DefaultDataTransformer>();
        services.AddSingleton<IRetryPolicyService, RetryPolicyService>();
        services.AddSingleton<IIndustrialErrorService, IndustrialErrorService>();
        services.AddSingleton<IAdamLoggerService, AdamLoggerService>();

        // Register InfluxDB writer - use null writer if not configured
        services.AddSingleton<IInfluxDbWriter>(provider =>
        {
            var config = provider.GetService<IOptions<AdamLoggerConfig>>()?.Value;
            if (config?.InfluxDb != null)
            {
                return new InfluxDbWriter(
                    provider.GetRequiredService<IOptions<AdamLoggerConfig>>(),
                    provider.GetRequiredService<IRetryPolicyService>(),
                    provider.GetRequiredService<ILogger<InfluxDbWriter>>());
            }
            return new NullInfluxDbWriter(provider.GetRequiredService<ILogger<NullInfluxDbWriter>>());
        });

        // Register data processor - use InfluxDB processor if configured, otherwise default
        services.AddSingleton<IDataProcessor>(provider =>
        {
            var config = provider.GetService<IOptions<AdamLoggerConfig>>()?.Value;
            if (config?.InfluxDb != null)
            {
                return provider.GetRequiredService<InfluxDbDataProcessor>();
            }
            return new DefaultDataProcessor(
                provider.GetRequiredService<IDataValidator>(),
                provider.GetRequiredService<IDataTransformer>(),
                provider.GetRequiredService<ILogger<DefaultDataProcessor>>());
        });

        // Register InfluxDB data processor as singleton for injection
        services.AddSingleton<InfluxDbDataProcessor>();

        // Register as hosted service for automatic start/stop lifecycle management
        services.AddHostedService<AdamLoggerService>(provider =>
            (AdamLoggerService)provider.GetRequiredService<IAdamLoggerService>());

        // Register health check for monitoring
        services.AddHealthChecks()
            .AddCheck<AdamLoggerService>("adam_logger");

        return services;
    }

    /// <summary>
    /// Register a custom data processor implementation for application-specific logic
    /// </summary>
    /// <typeparam name="T">Custom data processor type that implements IDataProcessor</typeparam>
    /// <param name="services">Service collection to register with</param>
    /// <returns>Service collection for method chaining</returns>
    /// <example>
    /// <code>
    /// services.AddCustomDataProcessor&lt;MyCustomProcessor&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddCustomDataProcessor<T>(this IServiceCollection services)
        where T : class, IDataProcessor
    {
        services.AddSingleton<IDataProcessor, T>();
        return services;
    }

    /// <summary>
    /// Register a custom data validator implementation for application-specific validation
    /// </summary>
    /// <typeparam name="T">Custom data validator type that implements IDataValidator</typeparam>
    /// <param name="services">Service collection to register with</param>
    /// <returns>Service collection for method chaining</returns>
    /// <example>
    /// <code>
    /// services.AddCustomDataValidator&lt;MyCustomValidator&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddCustomDataValidator<T>(this IServiceCollection services)
        where T : class, IDataValidator
    {
        services.AddSingleton<IDataValidator, T>();
        return services;
    }

    /// <summary>
    /// Register a custom data transformer implementation for application-specific transformations
    /// </summary>
    /// <typeparam name="T">Custom data transformer type that implements IDataTransformer</typeparam>
    /// <param name="services">Service collection to register with</param>
    /// <returns>Service collection for method chaining</returns>
    /// <example>
    /// <code>
    /// services.AddCustomDataTransformer&lt;MyCustomTransformer&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddCustomDataTransformer<T>(this IServiceCollection services)
        where T : class, IDataTransformer
    {
        services.AddSingleton<IDataTransformer, T>();
        return services;
    }

    /// <summary>
    /// Register ADAM Logger with configuration loaded from IConfiguration
    /// </summary>
    /// <param name="services">Service collection to register with</param>
    /// <param name="configurationSectionName">Name of the configuration section (default: "AdamLogger")</param>
    /// <returns>Service collection for method chaining</returns>
    /// <example>
    /// <code>
    /// // Load configuration from appsettings.json "AdamLogger" section
    /// services.AddAdamLoggerFromConfiguration();
    /// 
    /// // Load from custom section name
    /// services.AddAdamLoggerFromConfiguration("MyAdamConfig");
    /// </code>
    /// </example>
    public static IServiceCollection AddAdamLoggerFromConfiguration(
        this IServiceCollection services,
        string configurationSectionName = "AdamLogger")
    {
        // Configuration will be bound from IConfiguration in the service constructor
        services.AddOptions<AdamLoggerConfig>()
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Register core services
        services.AddSingleton<IDataValidator, DefaultDataValidator>();
        services.AddSingleton<IDataTransformer, DefaultDataTransformer>();
        services.AddSingleton<IRetryPolicyService, RetryPolicyService>();
        services.AddSingleton<IIndustrialErrorService, IndustrialErrorService>();
        services.AddSingleton<IAdamLoggerService, AdamLoggerService>();

        // Register InfluxDB writer - use null writer if not configured
        services.AddSingleton<IInfluxDbWriter>(provider =>
        {
            var config = provider.GetService<IOptions<AdamLoggerConfig>>()?.Value;
            if (config?.InfluxDb != null)
            {
                return new InfluxDbWriter(
                    provider.GetRequiredService<IOptions<AdamLoggerConfig>>(),
                    provider.GetRequiredService<IRetryPolicyService>(),
                    provider.GetRequiredService<ILogger<InfluxDbWriter>>());
            }
            return new NullInfluxDbWriter(provider.GetRequiredService<ILogger<NullInfluxDbWriter>>());
        });

        // Register data processor - use InfluxDB processor if configured, otherwise default
        services.AddSingleton<IDataProcessor>(provider =>
        {
            var config = provider.GetService<IOptions<AdamLoggerConfig>>()?.Value;
            if (config?.InfluxDb != null)
            {
                return provider.GetRequiredService<InfluxDbDataProcessor>();
            }
            return new DefaultDataProcessor(
                provider.GetRequiredService<IDataValidator>(),
                provider.GetRequiredService<IDataTransformer>(),
                provider.GetRequiredService<ILogger<DefaultDataProcessor>>());
        });

        // Register InfluxDB data processor as singleton for injection
        services.AddSingleton<InfluxDbDataProcessor>();

        // Register as hosted service
        services.AddHostedService<AdamLoggerService>(provider =>
            (AdamLoggerService)provider.GetRequiredService<IAdamLoggerService>());

        // Register health check
        services.AddHealthChecks()
            .AddCheck<AdamLoggerService>("adam_logger");

        return services;
    }

    /// <summary>
    /// Register ADAM Logger with structured logging support
    /// </summary>
    /// <param name="services">Service collection to register with</param>
    /// <param name="configuration">Application configuration</param>
    /// <param name="configureOptions">Configuration action for setting up ADAM Logger options</param>
    /// <param name="applicationName">Application name for logging context</param>
    /// <param name="version">Application version for logging context</param>
    /// <returns>Service collection for method chaining</returns>
    /// <example>
    /// <code>
    /// services.AddAdamLoggerWithStructuredLogging(configuration, config =>
    /// {
    ///     config.PollIntervalMs = 1000;
    ///     config.Devices.Add(new AdamDeviceConfig
    ///     {
    ///         DeviceId = "LINE1_ADAM",
    ///         IpAddress = "192.168.1.100"
    ///     });
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddAdamLoggerWithStructuredLogging(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<AdamLoggerConfig> configureOptions,
        string applicationName = "Industrial.Adam.Logger",
        string version = "1.0.0")
    {
        // Add structured logging first
        services.AddIndustrialLogging(configuration, applicationName, version);

        // Then add the ADAM Logger services
        services.AddAdamLogger(configureOptions);

        return services;
    }

    /// <summary>
    /// Register ADAM Logger with structured logging and configuration from IConfiguration
    /// </summary>
    /// <param name="services">Service collection to register with</param>
    /// <param name="configuration">Application configuration</param>
    /// <param name="configurationSectionName">Name of the configuration section (default: "AdamLogger")</param>
    /// <param name="applicationName">Application name for logging context</param>
    /// <param name="version">Application version for logging context</param>
    /// <returns>Service collection for method chaining</returns>
    /// <example>
    /// <code>
    /// services.AddAdamLoggerWithStructuredLoggingFromConfiguration(configuration);
    /// </code>
    /// </example>
    public static IServiceCollection AddAdamLoggerWithStructuredLoggingFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configurationSectionName = "AdamLogger",
        string applicationName = "Industrial.Adam.Logger",
        string version = "1.0.0")
    {
        // Add structured logging first
        services.AddIndustrialLogging(configuration, applicationName, version);

        // Then add the ADAM Logger services from configuration
        services.AddAdamLoggerFromConfiguration(configurationSectionName);

        return services;
    }

    /// <summary>
    /// Register production testing services for ADAM Logger
    /// </summary>
    /// <param name="services">Service collection to register with</param>
    /// <returns>Service collection for method chaining</returns>
    /// <example>
    /// <code>
    /// services.AddAdamLoggerTesting();
    /// </code>
    /// </example>
    public static IServiceCollection AddAdamLoggerTesting(this IServiceCollection services)
    {
        // Register test runner and individual test classes
        services.AddSingleton<ITestRunner, TestRunner>();
        services.AddSingleton<ConnectionTest>();
        services.AddSingleton<ConfigurationTest>();
        services.AddSingleton<DataQualityTest>();
        services.AddSingleton<PerformanceBenchmarkTest>();

        return services;
    }

    /// <summary>
    /// Register comprehensive health monitoring services for ADAM Logger
    /// </summary>
    /// <param name="services">Service collection to register with</param>
    /// <returns>Service collection for method chaining</returns>
    /// <example>
    /// <code>
    /// services.AddAdamLoggerHealthMonitoring();
    /// </code>
    /// </example>
    public static IServiceCollection AddAdamLoggerHealthMonitoring(this IServiceCollection services)
    {
        // Register HTTP client for health checks
        services.AddHttpClient<InfluxDbHealthCheck>();

        // Register individual health check components
        services.AddSingleton<ApplicationHealthCheck>();
        services.AddSingleton<InfluxDbHealthCheck>();
        services.AddSingleton<SystemResourceHealthCheck>();

        // Register main health check service
        services.AddSingleton<IHealthCheckService, HealthCheckService>();

        // Register with standard .NET health checks (applications can integrate with ASP.NET Core if needed)
        services.AddHealthChecks()
            .AddCheck<AdamLoggerService>("adam_logger_service");

        return services;
    }

    /// <summary>
    /// Register ADAM Logger with full monitoring, testing, and health checking capabilities
    /// </summary>
    /// <param name="services">Service collection to register with</param>
    /// <param name="configuration">Application configuration</param>
    /// <param name="configurationSectionName">Name of the configuration section (default: "AdamLogger")</param>
    /// <param name="applicationName">Application name for logging context</param>
    /// <param name="version">Application version for logging context</param>
    /// <returns>Service collection for method chaining</returns>
    /// <example>
    /// <code>
    /// services.AddAdamLoggerComplete(configuration);
    /// </code>
    /// </example>
    public static IServiceCollection AddAdamLoggerComplete(
        this IServiceCollection services,
        IConfiguration configuration,
        string configurationSectionName = "AdamLogger",
        string applicationName = "Industrial.Adam.Logger",
        string version = "1.0.0")
    {
        // Add all ADAM Logger components
        services.AddAdamLoggerWithStructuredLoggingFromConfiguration(
            configuration,
            configurationSectionName,
            applicationName,
            version);

        // Add testing capabilities
        services.AddAdamLoggerTesting();

        // Add health monitoring
        services.AddAdamLoggerHealthMonitoring();

        return services;
    }

    /// <summary>
    /// Register performance optimization services for high-frequency counter applications
    /// </summary>
    /// <param name="services">Service collection to register with</param>
    /// <returns>Service collection for method chaining</returns>
    /// <example>
    /// <code>
    /// services.AddAdamLoggerPerformanceOptimization();
    /// </code>
    /// </example>
    public static IServiceCollection AddAdamLoggerPerformanceOptimization(this IServiceCollection services)
    {
        // Register performance optimization interfaces and implementations
        services.AddSingleton<ICounterDataProcessor, CounterDataProcessor>();
        services.AddSingleton<IMemoryManager, MemoryManager>();

        return services;
    }

    /// <summary>
    /// Register comprehensive monitoring and metrics collection services
    /// </summary>
    /// <param name="services">Service collection to register with</param>
    /// <returns>Service collection for method chaining</returns>
    /// <example>
    /// <code>
    /// services.AddAdamLoggerMonitoring();
    /// </code>
    /// </example>
    public static IServiceCollection AddAdamLoggerMonitoring(this IServiceCollection services)
    {
        // Register metrics collection services
        services.AddSingleton<ICounterMetricsCollector, CounterMetricsCollector>();
        services.AddSingleton<IMetricsCollector>(provider =>
            provider.GetRequiredService<ICounterMetricsCollector>());

        return services;
    }

    /// <summary>
    /// Register ADAM Logger with advanced performance optimization and monitoring
    /// </summary>
    /// <param name="services">Service collection to register with</param>
    /// <param name="configuration">Application configuration</param>
    /// <param name="configurationSectionName">Name of the configuration section (default: "AdamLogger")</param>
    /// <param name="applicationName">Application name for logging context</param>
    /// <param name="version">Application version for logging context</param>
    /// <returns>Service collection for method chaining</returns>
    /// <example>
    /// <code>
    /// services.AddAdamLoggerAdvanced(configuration);
    /// </code>
    /// </example>
    public static IServiceCollection AddAdamLoggerAdvanced(
        this IServiceCollection services,
        IConfiguration configuration,
        string configurationSectionName = "AdamLogger",
        string applicationName = "Industrial.Adam.Logger",
        string version = "1.0.0")
    {
        // Add complete ADAM Logger functionality
        services.AddAdamLoggerComplete(
            configuration,
            configurationSectionName,
            applicationName,
            version);

        // Add performance optimization
        services.AddAdamLoggerPerformanceOptimization();

        // Add monitoring
        services.AddAdamLoggerMonitoring();

        return services;
    }
}
