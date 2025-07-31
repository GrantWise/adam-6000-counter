using Industrial.Adam.Logger.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Logger.Console;

/// <summary>
/// Console application for ADAM device logging
/// </summary>
internal class Program
{
    private static async Task Main(string[] args)
    {
        // Create host builder
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Add logging
                services.AddLogging(builder =>
                {
                    builder.SetMinimumLevel(LogLevel.Information);
                    builder.AddConsole();
                    builder.AddFilter("Industrial.Adam.Logger.Core", LogLevel.Debug);
                });

                // Add ADAM logger from configuration
                services.AddAdamLogger(context.Configuration);

                // Or use demo configuration for testing
                // services.AddAdamLoggerDemo();
            })
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                logging.AddConsole();

                // Configure log levels from appsettings.json
                logging.AddConfiguration(context.Configuration.GetSection("Logging"));
            })
            .Build();

        // Handle Ctrl+C gracefully
        var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
        var logger = host.Services.GetRequiredService<ILogger<Program>>();

        lifetime.ApplicationStarted.Register(() =>
        {
            logger.LogInformation("===========================================");
            logger.LogInformation("ADAM Logger Service Started");
            logger.LogInformation("Press Ctrl+C to stop");
            logger.LogInformation("===========================================");
        });

        lifetime.ApplicationStopping.Register(() =>
        {
            logger.LogInformation("===========================================");
            logger.LogInformation("ADAM Logger Service Stopping...");
            logger.LogInformation("===========================================");
        });

        lifetime.ApplicationStopped.Register(() =>
        {
            logger.LogInformation("ADAM Logger Service Stopped");
        });

        // Run the host
        await host.RunAsync();
    }
}
