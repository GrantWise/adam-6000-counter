using Industrial.Adam.Oee.Application.Events;
using Industrial.Adam.Oee.Domain.Events;
using Industrial.Adam.Oee.Domain.Interfaces;
using Industrial.Adam.Oee.WebApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace Industrial.Adam.Oee.WebApi.Controllers;

/// <summary>
/// Controller for stoppage notification and real-time monitoring endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class StoppageNotificationsController : ControllerBase
{
    private readonly IStoppageDetectionService _detectionService;
    private readonly IStoppageNotificationService _notificationService;
    private readonly ILogger<StoppageNotificationsController> _logger;

    /// <summary>
    /// Constructor for stoppage notifications controller
    /// </summary>
    public StoppageNotificationsController(
        IStoppageDetectionService detectionService,
        IStoppageNotificationService notificationService,
        ILogger<StoppageNotificationsController> logger)
    {
        _detectionService = detectionService ?? throw new ArgumentNullException(nameof(detectionService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get current stoppage status for a specific equipment line
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current stoppage status</returns>
    /// <response code="200">Returns the current stoppage status</response>
    /// <response code="404">Equipment line not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("lines/{lineId}/status")]
    [ProducesResponseType(typeof(ApiResponse<StoppageStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<StoppageStatusDto>>> GetLineStoppageStatus(
        string lineId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting stoppage status for line {LineId}", lineId);

            var isStopped = await _detectionService.IsLineStopped(lineId, cancellationToken);
            var lastProductionTime = await _detectionService.GetLastProductionTimeAsync(lineId, cancellationToken);
            var currentDuration = await _detectionService.GetCurrentStoppageDurationAsync(lineId, cancellationToken);
            var activeStoppage = await _detectionService.GetActiveStoppageAsync(lineId, cancellationToken);
            var threshold = await _detectionService.GetDetectionThresholdAsync(lineId);

            var status = new StoppageStatusDto
            {
                LineId = lineId,
                IsStopped = isStopped,
                LastProductionTime = lastProductionTime,
                CurrentStoppageDurationMinutes = currentDuration?.TotalMinutes,
                ActiveStoppageId = activeStoppage?.Id,
                DetectionThresholdMinutes = threshold,
                RequiresClassification = activeStoppage?.RequiresClassification() ?? false,
                CheckedAt = DateTime.UtcNow
            };

            return Ok(ApiResponse.Success(status));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get stoppage status for line {LineId}", lineId);
            return StatusCode(500, ApiResponse.Error($"Failed to get stoppage status: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get stoppage status for all active equipment lines
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stoppage status for all lines</returns>
    /// <response code="200">Returns stoppage status for all lines</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("status")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<StoppageStatusDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<IEnumerable<StoppageStatusDto>>>> GetAllLinesStoppageStatus(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting stoppage status for all lines");

            var activeLines = await _detectionService.GetActiveMonitoringLinesAsync(cancellationToken);
            var statuses = new List<StoppageStatusDto>();

            foreach (var line in activeLines)
            {
                try
                {
                    var isStopped = await _detectionService.IsLineStopped(line.LineId, cancellationToken);
                    var lastProductionTime = await _detectionService.GetLastProductionTimeAsync(line.LineId, cancellationToken);
                    var currentDuration = await _detectionService.GetCurrentStoppageDurationAsync(line.LineId, cancellationToken);
                    var activeStoppage = await _detectionService.GetActiveStoppageAsync(line.LineId, cancellationToken);
                    var threshold = await _detectionService.GetDetectionThresholdAsync(line.LineId);

                    var status = new StoppageStatusDto
                    {
                        LineId = line.LineId,
                        LineName = line.LineName,
                        IsStopped = isStopped,
                        LastProductionTime = lastProductionTime,
                        CurrentStoppageDurationMinutes = currentDuration?.TotalMinutes,
                        ActiveStoppageId = activeStoppage?.Id,
                        DetectionThresholdMinutes = threshold,
                        RequiresClassification = activeStoppage?.RequiresClassification() ?? false,
                        CheckedAt = DateTime.UtcNow
                    };

                    statuses.Add(status);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get status for line {LineId}, skipping", line.LineId);
                    // Continue with other lines
                }
            }

            return Ok(ApiResponse.Success(statuses.AsEnumerable()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get stoppage status for all lines");
            return StatusCode(500, ApiResponse.Error($"Failed to get stoppage status: {ex.Message}"));
        }
    }

    /// <summary>
    /// Trigger manual stoppage detection for a specific equipment line
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detection result</returns>
    /// <response code="200">Detection completed successfully</response>
    /// <response code="404">Equipment line not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("lines/{lineId}/detect")]
    [ProducesResponseType(typeof(ApiResponse<StoppageDetectionResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<StoppageDetectionResultDto>>> TriggerStoppageDetection(
        string lineId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Triggering manual stoppage detection for line {LineId}", lineId);

            var detectionResult = await _detectionService.MonitorLineAsync(lineId, cancellationToken);

            var result = new StoppageDetectionResultDto
            {
                LineId = lineId,
                DetectionTriggered = true,
                StoppageDetected = detectionResult != null,
                StoppageId = detectionResult?.StoppageId,
                DetectedAt = DateTime.UtcNow,
                Message = detectionResult != null
                    ? detectionResult.GetSummary()
                    : "No stoppage detected - line is producing normally"
            };

            if (detectionResult != null)
            {
                // Send notification
                await _notificationService.NotifyLineOperatorsAsync(
                    lineId,
                    detectionResult.ToNotificationData(),
                    cancellationToken);
            }

            return Ok(ApiResponse.Success(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger stoppage detection for line {LineId}", lineId);
            return StatusCode(500, ApiResponse.Error($"Failed to trigger detection: {ex.Message}"));
        }
    }

    /// <summary>
    /// Send test notification to verify SignalR connectivity
    /// </summary>
    /// <param name="request">Test notification request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test result</returns>
    /// <response code="200">Test notification sent successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("test-notification")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object>>> SendTestNotification(
        [FromBody] TestNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.LineId))
            {
                return BadRequest(ApiResponse.Error("LineId is required"));
            }

            _logger.LogInformation("Sending test notification for line {LineId}", request.LineId);

            var testNotification = new StoppageNotificationData(
                999, // Test ID
                request.LineId,
                request.WorkOrderId,
                DateTime.UtcNow.AddMinutes(-5),
                DateTime.UtcNow,
                5.0,
                true,
                NotificationUrgency.Medium,
                request.Message ?? "Test notification from API"
            );

            await _notificationService.NotifyLineOperatorsAsync(
                request.LineId,
                testNotification,
                cancellationToken);

            var result = new
            {
                LineId = request.LineId,
                NotificationSent = true,
                SentAt = DateTime.UtcNow,
                Message = "Test notification sent successfully"
            };

            return Ok(ApiResponse.Success(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send test notification");
            return StatusCode(500, ApiResponse.Error($"Failed to send test notification: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get SignalR connection information and hub status
    /// </summary>
    /// <returns>Hub status information</returns>
    /// <response code="200">Returns hub status</response>
    [HttpGet("hub-status")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<object>> GetHubStatus()
    {
        var status = new
        {
            HubEndpoint = "/stoppageHub",
            Status = "Active",
            SupportedMethods = new[]
            {
                "SubscribeToLine",
                "UnsubscribeFromLine",
                "SubscribeAsOperator",
                "SubscribeAsSupervisor",
                "SubscribeAsMaintenance",
                "Heartbeat"
            },
            ClientEvents = new[]
            {
                "StoppageDetected",
                "StoppageClassified",
                "StoppageEnded",
                "HighPriorityAlert",
                "CriticalAlert",
                "ConnectionEstablished",
                "SubscriptionConfirmed",
                "HeartbeatResponse"
            },
            CheckedAt = DateTime.UtcNow
        };

        return Ok(ApiResponse.Success(status));
    }
}

/// <summary>
/// Data transfer object for stoppage status
/// </summary>
public class StoppageStatusDto
{
    /// <summary>
    /// Equipment line identifier
    /// </summary>
    public string LineId { get; set; } = string.Empty;

    /// <summary>
    /// Equipment line name
    /// </summary>
    public string? LineName { get; set; }

    /// <summary>
    /// Whether the line is currently stopped
    /// </summary>
    public bool IsStopped { get; set; }

    /// <summary>
    /// Last time production activity was detected
    /// </summary>
    public DateTime? LastProductionTime { get; set; }

    /// <summary>
    /// Current stoppage duration in minutes (if stopped)
    /// </summary>
    public double? CurrentStoppageDurationMinutes { get; set; }

    /// <summary>
    /// Active stoppage identifier (if any)
    /// </summary>
    public int? ActiveStoppageId { get; set; }

    /// <summary>
    /// Detection threshold in minutes for this line
    /// </summary>
    public int DetectionThresholdMinutes { get; set; }

    /// <summary>
    /// Whether active stoppage requires classification
    /// </summary>
    public bool RequiresClassification { get; set; }

    /// <summary>
    /// When this status was checked
    /// </summary>
    public DateTime CheckedAt { get; set; }
}

/// <summary>
/// Data transfer object for stoppage detection result
/// </summary>
public class StoppageDetectionResultDto
{
    /// <summary>
    /// Equipment line identifier
    /// </summary>
    public string LineId { get; set; } = string.Empty;

    /// <summary>
    /// Whether detection was triggered
    /// </summary>
    public bool DetectionTriggered { get; set; }

    /// <summary>
    /// Whether a stoppage was detected
    /// </summary>
    public bool StoppageDetected { get; set; }

    /// <summary>
    /// Detected stoppage identifier (if any)
    /// </summary>
    public int? StoppageId { get; set; }

    /// <summary>
    /// When detection was performed
    /// </summary>
    public DateTime DetectedAt { get; set; }

    /// <summary>
    /// Detection result message
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Request model for test notifications
/// </summary>
public class TestNotificationRequest
{
    /// <summary>
    /// Equipment line identifier
    /// </summary>
    public string LineId { get; set; } = string.Empty;

    /// <summary>
    /// Optional work order identifier
    /// </summary>
    public string? WorkOrderId { get; set; }

    /// <summary>
    /// Test message to send
    /// </summary>
    public string? Message { get; set; }
}
