using Industrial.Adam.Oee.Application.DTOs;
using Industrial.Adam.Oee.Application.Queries;
using Industrial.Adam.Oee.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Oee.Application.Queries.Handlers;

/// <summary>
/// Handler for GetOeeHistoryQuery
/// </summary>
public class GetOeeHistoryQueryHandler : IRequestHandler<GetOeeHistoryQuery, IEnumerable<OeeCalculationDto>>
{
    private readonly IOeeCalculationService _oeeCalculationService;
    private readonly ILogger<GetOeeHistoryQueryHandler> _logger;

    public GetOeeHistoryQueryHandler(
        IOeeCalculationService oeeCalculationService,
        ILogger<GetOeeHistoryQueryHandler> logger)
    {
        _oeeCalculationService = oeeCalculationService;
        _logger = logger;
    }

    public async Task<IEnumerable<OeeCalculationDto>> Handle(GetOeeHistoryQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting OEE history for device {DeviceId} from {StartTime} to {EndTime}",
            request.DeviceId, request.StartTime, request.EndTime);

        try
        {
            // Calculate the appropriate period duration based on the time range
            var totalDuration = request.EndTime - request.StartTime;
            var periodDuration = CalculateOptimalPeriodDuration(totalDuration, request.MaxRecords);

            // Get OEE trends for the specified period
            var oeeCalculations = await _oeeCalculationService.CalculateOeeTrendsAsync(
                request.DeviceId,
                request.StartTime,
                request.EndTime,
                periodDuration,
                cancellationToken);

            // Convert to DTOs
            var results = oeeCalculations.Select(oee => OeeCalculationDto.FromDomain(oee)).ToList();

            // Apply limit if specified
            if (request.MaxRecords.HasValue && results.Count > request.MaxRecords.Value)
            {
                results = results
                    .OrderByDescending(x => x.CalculationPeriodStart)
                    .Take(request.MaxRecords.Value)
                    .OrderBy(x => x.CalculationPeriodStart)
                    .ToList();
            }

            _logger.LogInformation("Retrieved {Count} OEE history records for device {DeviceId}",
                results.Count, request.DeviceId);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting OEE history for device {DeviceId}", request.DeviceId);
            throw;
        }
    }

    /// <summary>
    /// Calculate optimal period duration based on total time range and max records
    /// </summary>
    /// <param name="totalDuration">Total time range</param>
    /// <param name="maxRecords">Maximum number of records requested</param>
    /// <returns>Optimal period duration</returns>
    private static TimeSpan CalculateOptimalPeriodDuration(TimeSpan totalDuration, int? maxRecords)
    {
        var defaultMaxRecords = maxRecords ?? 100;

        // Calculate period duration to get roughly the desired number of records
        var periodMinutes = Math.Max(15, totalDuration.TotalMinutes / defaultMaxRecords);

        // Round to reasonable intervals
        if (periodMinutes <= 15)
            return TimeSpan.FromMinutes(15);
        if (periodMinutes <= 30)
            return TimeSpan.FromMinutes(30);
        if (periodMinutes <= 60)
            return TimeSpan.FromHours(1);
        if (periodMinutes <= 240)
            return TimeSpan.FromHours(4);
        if (periodMinutes <= 480)
            return TimeSpan.FromHours(8);

        return TimeSpan.FromDays(1);
    }
}
