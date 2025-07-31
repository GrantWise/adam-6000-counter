using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace Industrial.Adam.Logger.Logging;

/// <summary>
/// Centralized logging configuration service for industrial applications
/// Implements structured logging with rotation, enrichment, and performance monitoring
/// </summary>
public static class LoggingConfiguration
{
    /// <summary>
    /// Configure Serilog with industrial-grade structured logging
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    /// <param name="applicationName">Name of the application for logging context</param>
    /// <param name="version">Version of the application</param>
    /// <returns>Configured Serilog logger configuration</returns>
    public static LoggerConfiguration CreateIndustrialLogger(
        IConfiguration configuration,
        string applicationName = "Industrial.Adam.Logger",
        string version = "1.0.0")
    {
        var loggerConfig = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.WithProperty("Application", applicationName)
            .Enrich.WithProperty("Version", version)
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithThreadId()
            .Enrich.WithProcessId()
            .Enrich.WithProcessName()
            .Enrich.With<IndustrialLogEnricher>();

        // Configure console output with structured format
        loggerConfig.WriteTo.Console(
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj} {Properties:j}{NewLine}{Exception}");

        // Configure file output with daily rotation
        var logPath = configuration.GetValue<string>("Logging:LogPath") ?? "logs/adam-counter-.log";
        var retainedFileCount = configuration.GetValue<int>("Logging:RetainedFileCount", 30);

        loggerConfig.WriteTo.File(
            path: logPath,
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: retainedFileCount,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] [{FunctionName}:{LineNumber}] {Message:lj} {Properties:j}{NewLine}{Exception}",
            restrictedToMinimumLevel: LogEventLevel.Information);

        // Configure debug file output with shorter retention
        var debugLogPath = configuration.GetValue<string>("Logging:DebugLogPath") ?? "logs/adam-counter-debug-.log";
        loggerConfig.WriteTo.File(
            path: debugLogPath,
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] [{FunctionName}:{LineNumber}] {Message:lj} {Properties:j}{NewLine}{Exception}",
            restrictedToMinimumLevel: LogEventLevel.Debug);

        // Configure structured JSON output for log aggregation
        if (configuration.GetValue<bool>("Logging:EnableJsonOutput", false))
        {
            var jsonLogPath = configuration.GetValue<string>("Logging:JsonLogPath") ?? "logs/adam-counter-json-.log";
            loggerConfig.WriteTo.File(
                new CompactJsonFormatter(),
                path: jsonLogPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: retainedFileCount,
                restrictedToMinimumLevel: LogEventLevel.Information);
        }

        return loggerConfig;
    }

    /// <summary>
    /// Configure logging for service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <param name="applicationName">Application name</param>
    /// <param name="version">Application version</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddIndustrialLogging(
        this IServiceCollection services,
        IConfiguration configuration,
        string applicationName = "Industrial.Adam.Logger",
        string version = "1.0.0")
    {
        // Configure Serilog
        var logger = CreateIndustrialLogger(configuration, applicationName, version)
            .CreateLogger();

        // Set global logger
        Log.Logger = logger;

        // Add Serilog to DI container
        services.AddSingleton(logger);
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog(logger);
        });

        return services;
    }
}

/// <summary>
/// Custom log enricher for industrial context
/// </summary>
public class IndustrialLogEnricher : Serilog.Core.ILogEventEnricher
{
    /// <summary>
    /// Enrich log events with industrial context
    /// </summary>
    /// <param name="logEvent">Log event to enrich</param>
    /// <param name="propertyFactory">Property factory for creating properties</param>
    public void Enrich(LogEvent logEvent, Serilog.Core.ILogEventPropertyFactory propertyFactory)
    {
        // Add facility information (can be configured)
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Facility", "DefaultFacility"));

        // Add production line information (can be configured)
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ProductionLine", "DefaultLine"));

        // Add UTC timestamp for consistency
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UtcTimestamp", DateTimeOffset.UtcNow));

        // Add correlation ID if available in context
        if (logEvent.Properties.TryGetValue("CorrelationId", out var correlationId))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TraceId", correlationId));
        }
    }
}
