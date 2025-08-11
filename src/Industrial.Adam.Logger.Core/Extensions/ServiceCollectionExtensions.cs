using Industrial.Adam.Logger.Core.Configuration;
using Industrial.Adam.Logger.Core.Devices;
using Industrial.Adam.Logger.Core.Processing;
using Industrial.Adam.Logger.Core.Services;
using Industrial.Adam.Logger.Core.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Industrial.Adam.Logger.Core.Extensions;

/// <summary>
/// Service collection extensions for ADAM logger
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add ADAM logger services to the service collection
    /// </summary>
    public static IServiceCollection AddAdamLogger(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Validate configuration structure early
        ValidateConfigurationStructure(configuration);

        // Add configuration
        services.Configure<LoggerConfiguration>(configuration.GetSection("AdamLogger"));
        services.Configure<InfluxDbSettings>(configuration.GetSection("AdamLogger:InfluxDb"));

        // Add core services
        services.AddSingleton<DeviceHealthTracker>();
        services.AddSingleton<ModbusDevicePool>();

        // Add data processing
        services.AddSingleton<IDataProcessor>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<DataProcessor>>();
            var config = provider.GetRequiredService<IOptions<LoggerConfiguration>>().Value;
            return new DataProcessor(logger, config);
        });

        // Add storage
        services.AddSingleton<IInfluxDbStorage>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<InfluxDbStorage>>();
            var settings = provider.GetRequiredService<IOptions<InfluxDbSettings>>().Value;
            return new InfluxDbStorage(logger, settings);
        });

        // Add main service
        services.AddHostedService<AdamLoggerService>();
        services.AddSingleton<AdamLoggerService>(provider =>
            provider.GetServices<IHostedService>()
                .OfType<AdamLoggerService>()
                .First());

        return services;
    }

    /// <summary>
    /// Validates the configuration structure to provide helpful error messages
    /// </summary>
    private static void ValidateConfigurationStructure(IConfiguration configuration)
    {
        var errors = new List<string>();

        // Check if AdamLogger section exists
        var adamLoggerSection = configuration.GetSection("AdamLogger");
        if (!adamLoggerSection.Exists())
        {
            errors.Add("Missing 'AdamLogger' configuration section in appsettings.json. " +
                      "The configuration must be structured as: { \"AdamLogger\": { \"Devices\": [...], \"InfluxDb\": {...} } }");
        }
        else
        {
            // Check for common mistake: InfluxDb at root level
            var rootInfluxDb = configuration.GetSection("InfluxDb");
            if (rootInfluxDb.Exists() && !adamLoggerSection.GetSection("InfluxDb").Exists())
            {
                errors.Add("InfluxDB configuration found at root level but should be nested under 'AdamLogger'. " +
                          "Move 'InfluxDb' section inside 'AdamLogger' section: { \"AdamLogger\": { \"InfluxDb\": {...} } }");
            }

            // Check if InfluxDb section exists under AdamLogger
            var influxDbSection = adamLoggerSection.GetSection("InfluxDb");
            if (!influxDbSection.Exists())
            {
                errors.Add("Missing 'AdamLogger:InfluxDb' configuration section. " +
                          "Add InfluxDB settings under AdamLogger: { \"AdamLogger\": { \"InfluxDb\": { \"Url\": \"...\", \"Token\": \"...\" } } }");
            }

            // Check if Devices section exists
            var devicesSection = adamLoggerSection.GetSection("Devices");
            if (!devicesSection.Exists() || !devicesSection.GetChildren().Any())
            {
                errors.Add("Missing or empty 'AdamLogger:Devices' configuration section. " +
                          "Add at least one device: { \"AdamLogger\": { \"Devices\": [{ \"DeviceId\": \"...\", \"IpAddress\": \"...\" }] } }");
            }
        }

        if (errors.Any())
        {
            var message = "Configuration validation failed:\n" + string.Join("\n", errors.Select(e => "  • " + e));
            throw new InvalidOperationException(message);
        }
    }

    /// <summary>
    /// Add ADAM logger with custom configuration
    /// </summary>
    public static IServiceCollection AddAdamLogger(
        this IServiceCollection services,
        Action<LoggerConfiguration> configureLogger,
        Action<InfluxDbSettings> configureInflux)
    {
        // Add configuration with actions
        services.Configure(configureLogger);
        services.Configure(configureInflux);

        // Add core services
        services.AddSingleton<DeviceHealthTracker>();
        services.AddSingleton<ModbusDevicePool>();

        // Add data processing
        services.AddSingleton<IDataProcessor>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<DataProcessor>>();
            var config = provider.GetRequiredService<IOptions<LoggerConfiguration>>().Value;
            return new DataProcessor(logger, config);
        });

        // Add storage
        services.AddSingleton<IInfluxDbStorage>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<InfluxDbStorage>>();
            var settings = provider.GetRequiredService<IOptions<InfluxDbSettings>>().Value;
            return new InfluxDbStorage(logger, settings);
        });

        // Add main service
        services.AddHostedService<AdamLoggerService>();
        services.AddSingleton<AdamLoggerService>(provider =>
            provider.GetServices<IHostedService>()
                .OfType<AdamLoggerService>()
                .First());

        return services;
    }

    /// <summary>
    /// Add ADAM logger with demo/test configuration
    /// </summary>
    public static IServiceCollection AddAdamLoggerDemo(
        this IServiceCollection services,
        string influxUrl = "http://localhost:8086",
        string influxToken = "demo-token")
    {
        return services.AddAdamLogger(
            logger =>
            {
                logger.Devices = new List<DeviceConfig>
                {
                    new DeviceConfig
                    {
                        DeviceId = "DEMO001",
                        Name = "Demo ADAM Device",
                        IpAddress = "127.0.0.1",
                        Port = 502,
                        UnitId = 1,
                        Enabled = true,
                        PollIntervalMs = 1000,
                        TimeoutMs = 3000,
                        MaxRetries = 3,
                        Channels = new List<ChannelConfig>
                        {
                            new ChannelConfig
                            {
                                ChannelNumber = 0,
                                Name = "Demo Counter",
                                StartRegister = 0,
                                RegisterCount = 2,
                                Enabled = true,
                                ScaleFactor = 1.0,
                                Unit = "counts"
                            }
                        }
                    }
                };
            },
            influx =>
            {
                influx.Url = influxUrl;
                influx.Token = influxToken;
                influx.Organization = "demo";
                influx.Bucket = "adam_demo";
                influx.MeasurementName = "counter_data";
                influx.BatchSize = 10;
                influx.FlushIntervalMs = 5000;
            });
    }
}
