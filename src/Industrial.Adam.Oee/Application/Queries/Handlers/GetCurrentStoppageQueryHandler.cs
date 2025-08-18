using Industrial.Adam.Oee.Application.DTOs;
using Industrial.Adam.Oee.Application.Queries;
using Industrial.Adam.Oee.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Oee.Application.Queries.Handlers;

/// <summary>
/// Handler for GetCurrentStoppageQuery
/// </summary>
public class GetCurrentStoppageQueryHandler : IRequestHandler<GetCurrentStoppageQuery, StoppageInfoDto?>
{
    private readonly IOeeCalculationService _oeeCalculationService;
    private readonly ILogger<GetCurrentStoppageQueryHandler> _logger;

    /// <summary>
    /// Constructor for GetCurrentStoppageQueryHandler
    /// </summary>
    /// <param name="oeeCalculationService">OEE calculation service</param>
    /// <param name="logger">Logger instance</param>
    public GetCurrentStoppageQueryHandler(
        IOeeCalculationService oeeCalculationService,
        ILogger<GetCurrentStoppageQueryHandler> logger)
    {
        _oeeCalculationService = oeeCalculationService;
        _logger = logger;
    }

    /// <summary>
    /// Handle the GetCurrentStoppageQuery
    /// </summary>
    /// <param name="request">Query request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current stoppage information or null if not stopped</returns>
    public async Task<StoppageInfoDto?> Handle(GetCurrentStoppageQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Detecting current stoppage for device: {DeviceId}", request.DeviceId);

        if (string.IsNullOrWhiteSpace(request.DeviceId))
        {
            throw new ArgumentException("Device ID cannot be null or empty", nameof(request.DeviceId));
        }

        if (request.MinimumStoppageMinutes < 1)
        {
            throw new ArgumentException("Minimum stoppage minutes must be at least 1", nameof(request.MinimumStoppageMinutes));
        }

        var stoppageInfo = await _oeeCalculationService.DetectCurrentStoppageAsync(
            request.DeviceId,
            request.MinimumStoppageMinutes,
            cancellationToken);

        if (stoppageInfo == null)
        {
            _logger.LogInformation("No current stoppage detected for device {DeviceId}", request.DeviceId);
            return null;
        }

        _logger.LogInformation("Current stoppage detected for device {DeviceId}: {DurationMinutes} minutes",
            request.DeviceId, stoppageInfo.DurationMinutes);

        return new StoppageInfoDto
        {
            StartTime = stoppageInfo.StartTime,
            DurationMinutes = stoppageInfo.DurationMinutes,
            IsActive = stoppageInfo.IsActive,
            DeviceId = request.DeviceId,
            EstimatedImpact = stoppageInfo.EstimatedImpact != null ? new StoppageImpactDto
            {
                LostProductionUnits = stoppageInfo.EstimatedImpact.LostProductionUnits,
                LostRevenue = stoppageInfo.EstimatedImpact.LostRevenue,
                AvailabilityImpact = stoppageInfo.EstimatedImpact.AvailabilityImpact
            } : null,
            EndTime = stoppageInfo.IsActive ? null : stoppageInfo.StartTime.AddMinutes((double)stoppageInfo.DurationMinutes)
        };
    }
}
