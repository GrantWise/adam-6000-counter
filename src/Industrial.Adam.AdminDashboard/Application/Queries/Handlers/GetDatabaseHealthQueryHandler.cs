using Industrial.Adam.AdminDashboard.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.AdminDashboard.Application.Queries.Handlers;

/// <summary>
/// Handler for GetDatabaseHealthQuery
/// </summary>
public class GetDatabaseHealthQueryHandler : IRequestHandler<GetDatabaseHealthQuery, object>
{
    private readonly ISystemHealthService _healthService;
    private readonly ILogger<GetDatabaseHealthQueryHandler> _logger;

    /// <summary>
    /// Constructor for GetDatabaseHealthQueryHandler
    /// </summary>
    /// <param name="healthService">System health service</param>
    /// <param name="logger">Logger instance</param>
    public GetDatabaseHealthQueryHandler(
        ISystemHealthService healthService,
        ILogger<GetDatabaseHealthQueryHandler> logger)
    {
        _healthService = healthService;
        _logger = logger;
    }

    /// <summary>
    /// Handle the query
    /// </summary>
    /// <param name="request">Query request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Database health information</returns>
    public async Task<object> Handle(GetDatabaseHealthQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting database health information");

        try
        {
            var result = await _healthService.GetDatabaseHealthAsync(cancellationToken);

            return new
            {
                Status = result.Status.ToString(),
                ResponseTimeMs = result.ResponseTimeMs,
                Version = result.Version,
                DatabaseSizeBytes = result.DatabaseSizeBytes,
                ActiveConnections = result.ActiveConnections,
                MaxConnections = result.MaxConnections,
                ConnectionUtilizationPercent = result.ConnectionUtilizationPercent,
                Timestamp = result.Timestamp,
                ErrorMessage = result.ErrorMessage,
                AdditionalMetrics = result.AdditionalMetrics
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get database health information");
            throw;
        }
    }
}