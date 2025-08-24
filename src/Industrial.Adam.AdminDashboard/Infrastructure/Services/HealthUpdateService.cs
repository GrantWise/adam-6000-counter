using Industrial.Adam.AdminDashboard.Domain.Interfaces;
using Industrial.Adam.AdminDashboard.Infrastructure.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.AdminDashboard.Infrastructure.Services;

/// <summary>
/// Service for broadcasting health updates via SignalR
/// </summary>
public class HealthUpdateService : IHealthUpdateService
{
    private readonly IHubContext<HealthUpdateHub> _hubContext;
    private readonly ILogger<HealthUpdateService> _logger;

    /// <summary>
    /// Constructor for HealthUpdateService
    /// </summary>
    /// <param name="hubContext">SignalR hub context</param>
    /// <param name="logger">Logger instance</param>
    public HealthUpdateService(IHubContext<HealthUpdateHub> hubContext, ILogger<HealthUpdateService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task BroadcastSystemHealthAsync(object healthData, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Broadcasting system health data to all connected clients");
            
            await _hubContext.Clients.All.SendAsync("SystemHealthUpdate", healthData, cancellationToken);
            
            _logger.LogDebug("System health data broadcasted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast system health data");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task BroadcastSystemMetricsAsync(object metrics, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Broadcasting system metrics to all connected clients");
            
            await _hubContext.Clients.All.SendAsync("SystemMetricsUpdate", metrics, cancellationToken);
            
            _logger.LogDebug("System metrics broadcasted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast system metrics");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task BroadcastSystemAlertsAsync(object alerts, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Broadcasting system alerts to all connected clients");
            
            await _hubContext.Clients.All.SendAsync("SystemAlertsUpdate", alerts, cancellationToken);
            
            _logger.LogDebug("System alerts broadcasted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast system alerts");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task SendToGroupAsync(string groupName, object healthData, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Sending health update to group {GroupName}", groupName);
            
            await _hubContext.Clients.Group(groupName).SendAsync("HealthUpdate", healthData, cancellationToken);
            
            _logger.LogDebug("Health update sent to group {GroupName} successfully", groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send health update to group {GroupName}", groupName);
            throw;
        }
    }
}