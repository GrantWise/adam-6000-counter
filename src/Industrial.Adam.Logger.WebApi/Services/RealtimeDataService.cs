using Industrial.Adam.Logger.Interfaces;
using Industrial.Adam.Logger.Models;
using Industrial.Adam.Logger.WebApi.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Industrial.Adam.Logger.WebApi.Services;

/// <summary>
/// Background service that streams real-time data from ADAM devices to SignalR clients
/// </summary>
public class RealtimeDataService : BackgroundService
{
    private readonly IAdamLoggerService _loggerService;
    private readonly IHubContext<CounterDataHub> _counterHub;
    private readonly IHubContext<HealthStatusHub> _healthHub;
    private readonly ILogger<RealtimeDataService> _logger;
    private IDisposable? _dataSubscription;
    private IDisposable? _healthSubscription;

    public RealtimeDataService(
        IAdamLoggerService loggerService,
        IHubContext<CounterDataHub> counterHub,
        IHubContext<HealthStatusHub> healthHub,
        ILogger<RealtimeDataService> logger)
    {
        _loggerService = loggerService;
        _counterHub = counterHub;
        _healthHub = healthHub;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Real-time data service starting");

        // Wait a bit for the application to fully start
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        // Subscribe to data stream
        _dataSubscription = _loggerService.DataStream.Subscribe(
            async data => await OnDataReceivedAsync(data),
            error => _logger.LogError(error, "Error in data stream"),
            () => _logger.LogInformation("Data stream completed")
        );

        // Subscribe to health stream
        _healthSubscription = _loggerService.HealthStream.Subscribe(
            async health => await OnHealthUpdateAsync(health),
            error => _logger.LogError(error, "Error in health stream"),
            () => _logger.LogInformation("Health stream completed")
        );

        // Start the logger service if not already running
        if (!_loggerService.IsRunning)
        {
            try
            {
                await _loggerService.StartAsync(stoppingToken);
                _logger.LogInformation("ADAM logger service started");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start ADAM logger service");
            }
        }

        // Keep the service running
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task OnDataReceivedAsync(AdamDataReading data)
    {
        try
        {
            // Send to all connected clients
            await _counterHub.Clients.All.SendAsync("CounterUpdate", new
            {
                deviceId = data.DeviceId,
                channel = data.Channel,
                timestamp = data.Timestamp,
                rawValue = data.RawValue,
                processedValue = data.ProcessedValue,
                rate = data.Rate,
                quality = data.Quality.ToString(),
                unit = data.Unit
            });

            // Send to device-specific groups
            await _counterHub.Clients.Group($"device-{data.DeviceId}").SendAsync("DeviceCounterUpdate", new
            {
                channel = data.Channel,
                timestamp = data.Timestamp,
                rawValue = data.RawValue,
                processedValue = data.ProcessedValue,
                rate = data.Rate,
                quality = data.Quality.ToString(),
                unit = data.Unit
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send counter data to clients");
        }
    }

    private async Task OnHealthUpdateAsync(AdamDeviceHealth health)
    {
        try
        {
            // Send to all connected clients
            await _healthHub.Clients.All.SendAsync("HealthUpdate", new
            {
                deviceId = health.DeviceId,
                timestamp = health.Timestamp,
                status = health.Status.ToString(),
                isConnected = health.IsConnected,
                lastSuccessfulRead = health.LastSuccessfulRead?.TotalSeconds,
                consecutiveFailures = health.ConsecutiveFailures,
                communicationLatency = health.CommunicationLatency,
                lastError = health.LastError,
                totalReads = health.TotalReads,
                successfulReads = health.SuccessfulReads,
                successRate = health.SuccessRate
            });

            // Send alerts for critical status changes
            if (health.Status == DeviceStatus.Error || health.Status == DeviceStatus.Offline)
            {
                await _healthHub.Clients.All.SendAsync("SystemAlert", new
                {
                    severity = "error",
                    deviceId = health.DeviceId,
                    message = $"Device {health.DeviceId} is {health.Status}",
                    timestamp = health.Timestamp,
                    details = health.LastError
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send health update to clients");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Real-time data service stopping");

        _dataSubscription?.Dispose();
        _healthSubscription?.Dispose();

        if (_loggerService.IsRunning)
        {
            await _loggerService.StopAsync(cancellationToken);
        }

        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _dataSubscription?.Dispose();
        _healthSubscription?.Dispose();
        base.Dispose();
    }
}