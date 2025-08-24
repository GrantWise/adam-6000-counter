using Industrial.Adam.AdminDashboard.Application.DTOs;
using Industrial.Adam.AdminDashboard.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.AdminDashboard.Application.Queries.Handlers;

/// <summary>
/// Handler for GetSystemHealthQuery
/// </summary>
public class GetSystemHealthQueryHandler : IRequestHandler<GetSystemHealthQuery, IEnumerable<ServiceHealthDto>>
{
    private readonly ISystemHealthService _healthService;
    private readonly ILogger<GetSystemHealthQueryHandler> _logger;

    /// <summary>
    /// Constructor for GetSystemHealthQueryHandler
    /// </summary>
    /// <param name="healthService">System health service</param>
    /// <param name="logger">Logger instance</param>
    public GetSystemHealthQueryHandler(
        ISystemHealthService healthService,
        ILogger<GetSystemHealthQueryHandler> logger)
    {
        _healthService = healthService;
        _logger = logger;
    }

    /// <summary>
    /// Handle the query
    /// </summary>
    /// <param name="request">Query request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of service health DTOs</returns>
    public async Task<IEnumerable<ServiceHealthDto>> Handle(GetSystemHealthQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting system health status for all services");

        try
        {
            var healthResults = await _healthService.CheckAllServicesAsync(cancellationToken);

            var healthDtos = healthResults.Select(result => new ServiceHealthDto
            {
                ServiceName = result.ServiceName,
                Status = result.Status,
                ResponseTimeMs = result.ResponseTimeMs,
                ErrorCount = result.ErrorCount,
                Timestamp = result.Timestamp,
                ErrorMessage = result.ErrorMessage,
                AdditionalData = result.AdditionalData
            }).ToList();

            _logger.LogInformation("Retrieved health status for {ServiceCount} services", healthDtos.Count);

            return healthDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get system health status");
            throw;
        }
    }
}