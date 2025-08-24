using Industrial.Adam.AdminDashboard.Application.DTOs;
using Industrial.Adam.AdminDashboard.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.AdminDashboard.Application.Queries.Handlers;

/// <summary>
/// Handler for GetActiveAlertsQuery
/// </summary>
public class GetActiveAlertsQueryHandler : IRequestHandler<GetActiveAlertsQuery, IEnumerable<SystemAlertDto>>
{
    private readonly ISystemAlertRepository _alertRepository;
    private readonly ILogger<GetActiveAlertsQueryHandler> _logger;

    /// <summary>
    /// Constructor for GetActiveAlertsQueryHandler
    /// </summary>
    /// <param name="alertRepository">System alert repository</param>
    /// <param name="logger">Logger instance</param>
    public GetActiveAlertsQueryHandler(
        ISystemAlertRepository alertRepository,
        ILogger<GetActiveAlertsQueryHandler> logger)
    {
        _alertRepository = alertRepository;
        _logger = logger;
    }

    /// <summary>
    /// Handle the query
    /// </summary>
    /// <param name="request">Query request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of active alert DTOs</returns>
    public async Task<IEnumerable<SystemAlertDto>> Handle(GetActiveAlertsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting active system alerts");

        try
        {
            var alerts = await _alertRepository.GetActiveAlertsAsync(cancellationToken);

            var alertDtos = alerts.Select(alert => new SystemAlertDto
            {
                Id = alert.Id,
                AlertType = alert.AlertType,
                Severity = alert.Severity,
                Message = alert.Message,
                Acknowledged = alert.Acknowledged,
                AcknowledgedBy = alert.AcknowledgedBy,
                AcknowledgedAt = alert.AcknowledgedAt,
                CreatedAt = alert.CreatedAt,
                ServiceName = alert.ServiceName,
                AdditionalData = string.IsNullOrEmpty(alert.AdditionalData)
                    ? null
                    : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(alert.AdditionalData)
            }).ToList();

            _logger.LogInformation("Retrieved {AlertCount} active alerts", alertDtos.Count);

            return alertDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active alerts");
            throw;
        }
    }
}