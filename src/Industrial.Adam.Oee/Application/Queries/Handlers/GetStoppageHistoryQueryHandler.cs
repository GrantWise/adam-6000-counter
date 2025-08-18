using Industrial.Adam.Oee.Application.DTOs;
using Industrial.Adam.Oee.Application.Queries;
using Industrial.Adam.Oee.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Oee.Application.Queries.Handlers;

/// <summary>
/// Handler for GetStoppageHistoryQuery
/// </summary>
public class GetStoppageHistoryQueryHandler : IRequestHandler<GetStoppageHistoryQuery, IEnumerable<StoppageInfoDto>>
{
    private readonly ICounterDataRepository _counterDataRepository;
    private readonly IOeeCalculationService _oeeCalculationService;
    private readonly ILogger<GetStoppageHistoryQueryHandler> _logger;

    /// <summary>
    /// Constructor for GetStoppageHistoryQueryHandler
    /// </summary>
    /// <param name="counterDataRepository">Counter data repository</param>
    /// <param name="oeeCalculationService">OEE calculation service</param>
    /// <param name="logger">Logger instance</param>
    public GetStoppageHistoryQueryHandler(
        ICounterDataRepository counterDataRepository,
        IOeeCalculationService oeeCalculationService,
        ILogger<GetStoppageHistoryQueryHandler> logger)
    {
        _counterDataRepository = counterDataRepository;
        _oeeCalculationService = oeeCalculationService;
        _logger = logger;
    }

    /// <summary>
    /// Handle the GetStoppageHistoryQuery
    /// </summary>
    /// <param name="request">Query request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Historical stoppage data</returns>
    public async Task<IEnumerable<StoppageInfoDto>> Handle(GetStoppageHistoryQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving stoppage history for device {DeviceId} over {Period} hours",
            request.DeviceId, request.Period);

        if (string.IsNullOrWhiteSpace(request.DeviceId))
        {
            throw new ArgumentException("Device ID cannot be null or empty", nameof(request.DeviceId));
        }

        if (request.Period < 1 || request.Period > 8760)
        {
            throw new ArgumentException("Period must be between 1 and 8760 hours", nameof(request.Period));
        }

        if (request.MinimumStoppageMinutes < 1)
        {
            throw new ArgumentException("Minimum stoppage minutes must be at least 1", nameof(request.MinimumStoppageMinutes));
        }

        // Determine the actual time range
        var endTime = request.EndTime ?? DateTime.UtcNow;
        var startTime = request.StartTime ?? endTime.AddHours(-request.Period);

        // Get the configuration to determine the production channel
        var config = await _oeeCalculationService.GetCalculationConfigurationAsync(request.DeviceId, cancellationToken);

        // Get downtime periods from the repository
        var downtimePeriods = await _counterDataRepository.GetDowntimePeriodsAsync(
            request.DeviceId,
            config.ProductionChannel,
            startTime,
            endTime,
            request.MinimumStoppageMinutes,
            cancellationToken);

        // Convert to DTOs
        var stoppageDtos = downtimePeriods.Select(downtime => new StoppageInfoDto
        {
            StartTime = downtime.StartTime,
            DurationMinutes = downtime.DurationMinutes,
            IsActive = downtime.IsOngoing,
            DeviceId = request.DeviceId,
            EndTime = downtime.IsOngoing ? null : downtime.StartTime.AddMinutes((double)downtime.DurationMinutes),
            // Note: For historical data, we're not calculating impact for performance reasons
            // Impact calculation could be added as a separate endpoint if needed
            EstimatedImpact = null
        }).OrderByDescending(s => s.StartTime);

        _logger.LogInformation("Retrieved {Count} stoppage periods for device {DeviceId}",
            stoppageDtos.Count(), request.DeviceId);

        return stoppageDtos;
    }
}
