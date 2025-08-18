using Industrial.Adam.Oee.Domain.Entities;
using Industrial.Adam.Oee.Domain.Events;

namespace Industrial.Adam.Oee.Domain.Interfaces;

/// <summary>
/// Service interface for automatic stoppage detection
/// </summary>
public interface IStoppageDetectionService
{
    /// <summary>
    /// Monitor all active equipment lines for potential stoppages
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of detected stoppages</returns>
    public Task<IEnumerable<StoppageDetectedEvent>> MonitorAllLinesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Monitor a specific equipment line for stoppages
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detected stoppage event or null if no stoppage detected</returns>
    public Task<StoppageDetectedEvent?> MonitorLineAsync(string lineId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a specific equipment line is currently stopped
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if line is stopped, false if producing</returns>
    public Task<bool> IsLineStopped(string lineId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the last production activity time for an equipment line
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Last production time or null if no activity found</returns>
    public Task<DateTime?> GetLastProductionTimeAsync(string lineId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate stoppage duration for a line
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Duration since last production or null if line is producing</returns>
    public Task<TimeSpan?> GetCurrentStoppageDurationAsync(string lineId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a detected stoppage should trigger an alert
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="stoppageDuration">Duration of stoppage</param>
    /// <returns>True if alert should be triggered</returns>
    public Task<bool> ShouldTriggerAlertAsync(string lineId, TimeSpan stoppageDuration);

    /// <summary>
    /// Get the detection threshold for a specific equipment line
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <returns>Detection threshold in minutes</returns>
    public Task<int> GetDetectionThresholdAsync(string lineId);

    /// <summary>
    /// Get equipment lines that require monitoring
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of active equipment lines for monitoring</returns>
    public Task<IEnumerable<EquipmentLine>> GetActiveMonitoringLinesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a line already has an active stoppage
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Active stoppage or null if none exists</returns>
    public Task<EquipmentStoppage?> GetActiveStoppageAsync(string lineId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate if a new stoppage should be created or existing one updated
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="lastProductionTime">Last production activity time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stoppage validation result</returns>
    public Task<StoppageValidationResult> ValidateStoppageCreationAsync(
        string lineId,
        DateTime lastProductionTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new stoppage based on detection
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="startTime">Stoppage start time</param>
    /// <param name="workOrderId">Associated work order</param>
    /// <param name="detectionThreshold">Detection threshold used</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created stoppage</returns>
    public Task<EquipmentStoppage> CreateDetectedStoppageAsync(
        string lineId,
        DateTime startTime,
        string? workOrderId,
        int detectionThreshold,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// End an active stoppage when production resumes
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="endTime">When production resumed</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Ended stoppage or null if no active stoppage</returns>
    public Task<EquipmentStoppage?> EndActiveStoppageAsync(
        string lineId,
        DateTime? endTime = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of stoppage validation for creation
/// </summary>
/// <param name="ShouldCreateStoppage">Whether a new stoppage should be created</param>
/// <param name="ExistingStoppage">Existing active stoppage (if any)</param>
/// <param name="Reason">Reason for the validation result</param>
/// <param name="LastProductionTime">Last detected production time</param>
/// <param name="StoppageDuration">Current stoppage duration</param>
/// <param name="DetectionThreshold">Detection threshold for the line</param>
public record StoppageValidationResult(
    bool ShouldCreateStoppage,
    EquipmentStoppage? ExistingStoppage,
    string Reason,
    DateTime? LastProductionTime,
    TimeSpan? StoppageDuration,
    int DetectionThreshold
)
{
    /// <summary>
    /// Create a result indicating stoppage should be created
    /// </summary>
    public static StoppageValidationResult CreateStoppage(DateTime lastProductionTime, TimeSpan stoppageDuration, int threshold)
        => new(true, null, "No active stoppage detected, production stopped", lastProductionTime, stoppageDuration, threshold);

    /// <summary>
    /// Create a result indicating existing stoppage should be used
    /// </summary>
    public static StoppageValidationResult UseExisting(EquipmentStoppage existingStoppage, string reason)
        => new(false, existingStoppage, reason, null, null, existingStoppage.MinimumThresholdMinutes);

    /// <summary>
    /// Create a result indicating no stoppage action needed
    /// </summary>
    public static StoppageValidationResult NoAction(string reason)
        => new(false, null, reason, null, null, 0);
}

/// <summary>
/// Detection configuration for equipment lines
/// </summary>
/// <param name="LineId">Equipment line identifier</param>
/// <param name="DetectionThresholdMinutes">Minutes without production before stoppage detection</param>
/// <param name="ClassificationThresholdMinutes">Minutes before classification is required</param>
/// <param name="AlertThresholdMinutes">Minutes before high-priority alerts are sent</param>
/// <param name="IsEnabled">Whether detection is enabled for this line</param>
/// <param name="MonitoringIntervalSeconds">How often to check for stoppages</param>
public record StoppageDetectionConfiguration(
    string LineId,
    int DetectionThresholdMinutes = 5,
    int ClassificationThresholdMinutes = 5,
    int AlertThresholdMinutes = 15,
    bool IsEnabled = true,
    int MonitoringIntervalSeconds = 30
);
