using Industrial.Adam.AdminDashboard.Application.DTOs;
using Industrial.Adam.AdminDashboard.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.AdminDashboard.Application.Queries.Handlers;

/// <summary>
/// Handler for GetSystemMetricsQuery
/// </summary>
public class GetSystemMetricsQueryHandler : IRequestHandler<GetSystemMetricsQuery, SystemMetricsDto>
{
    private readonly ISystemHealthService _healthService;
    private readonly ILogger<GetSystemMetricsQueryHandler> _logger;

    /// <summary>
    /// Constructor for GetSystemMetricsQueryHandler
    /// </summary>
    /// <param name="healthService">System health service</param>
    /// <param name="logger">Logger instance</param>
    public GetSystemMetricsQueryHandler(
        ISystemHealthService healthService,
        ILogger<GetSystemMetricsQueryHandler> logger)
    {
        _healthService = healthService;
        _logger = logger;
    }

    /// <summary>
    /// Handle the query
    /// </summary>
    /// <param name="request">Query request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>System metrics DTO</returns>
    public async Task<SystemMetricsDto> Handle(GetSystemMetricsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting system performance metrics");

        try
        {
            var metrics = await _healthService.GetSystemMetricsAsync(cancellationToken);

            var metricsDto = new SystemMetricsDto
            {
                CpuUsagePercent = metrics.CpuUsagePercent,
                MemoryUsageBytes = metrics.MemoryUsageBytes,
                TotalMemoryBytes = metrics.TotalMemoryBytes,
                DiskUsageBytes = metrics.DiskUsageBytes,
                TotalDiskBytes = metrics.TotalDiskBytes,
                Timestamp = metrics.Timestamp,
                UptimeSeconds = metrics.UptimeSeconds,
                AdditionalMetrics = metrics.AdditionalMetrics
            };

            _logger.LogInformation("Retrieved system metrics - CPU: {CpuUsage}%, Memory: {MemoryUsage}%",
                metricsDto.CpuUsagePercent,
                metricsDto.TotalMemoryBytes > 0 ? (double)metricsDto.MemoryUsageBytes / metricsDto.TotalMemoryBytes * 100 : 0);

            return metricsDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get system metrics");
            throw;
        }
    }
}