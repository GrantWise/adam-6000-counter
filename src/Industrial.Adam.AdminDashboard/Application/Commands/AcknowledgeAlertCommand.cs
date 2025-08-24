using Industrial.Adam.AdminDashboard.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.AdminDashboard.Application.Commands;

/// <summary>
/// Command to acknowledge a system alert
/// </summary>
public record AcknowledgeAlertCommand : IRequest<bool>
{
    /// <summary>
    /// Gets the alert ID to acknowledge
    /// </summary>
    public int AlertId { get; init; }

    /// <summary>
    /// Gets the user ID acknowledging the alert
    /// </summary>
    public string AcknowledgedBy { get; init; } = string.Empty;
}

/// <summary>
/// Handler for AcknowledgeAlertCommand
/// </summary>
public class AcknowledgeAlertCommandHandler : IRequestHandler<AcknowledgeAlertCommand, bool>
{
    private readonly ISystemAlertRepository _alertRepository;
    private readonly ILogger<AcknowledgeAlertCommandHandler> _logger;

    /// <summary>
    /// Constructor for AcknowledgeAlertCommandHandler
    /// </summary>
    /// <param name="alertRepository">System alert repository</param>
    /// <param name="logger">Logger instance</param>
    public AcknowledgeAlertCommandHandler(
        ISystemAlertRepository alertRepository,
        ILogger<AcknowledgeAlertCommandHandler> logger)
    {
        _alertRepository = alertRepository;
        _logger = logger;
    }

    /// <summary>
    /// Handle the command
    /// </summary>
    /// <param name="request">Command request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successfully acknowledged, false otherwise</returns>
    public async Task<bool> Handle(AcknowledgeAlertCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Acknowledging alert {AlertId} by user {UserId}", request.AlertId, request.AcknowledgedBy);

        var result = await _alertRepository.AcknowledgeAlertAsync(
            request.AlertId, 
            request.AcknowledgedBy, 
            cancellationToken);

        if (result)
        {
            _logger.LogInformation("Successfully acknowledged alert {AlertId}", request.AlertId);
        }
        else
        {
            _logger.LogWarning("Failed to acknowledge alert {AlertId} - alert may not exist or already acknowledged", request.AlertId);
        }

        return result;
    }
}