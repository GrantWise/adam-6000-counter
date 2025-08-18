using Industrial.Adam.Oee.Domain.Entities;
using Industrial.Adam.Oee.Domain.Events;
using Industrial.Adam.Oee.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Oee.Domain.Services;

/// <summary>
/// Domain service for automatic stoppage detection
/// Implements core business logic for identifying production stoppages
/// </summary>
public sealed class StoppageDetectionService : IStoppageDetectionService
{
    private readonly ICounterDataRepository _counterDataRepository;
    private readonly IEquipmentLineRepository _equipmentLineRepository;
    private readonly IEquipmentStoppageRepository _stoppageRepository;
    private readonly IWorkOrderRepository _workOrderRepository;
    private readonly ILogger<StoppageDetectionService> _logger;

    private const int DefaultDetectionThresholdMinutes = 5;
    private const int DefaultClassificationThresholdMinutes = 5;

    /// <summary>
    /// Constructor for stoppage detection service
    /// </summary>
    public StoppageDetectionService(
        ICounterDataRepository counterDataRepository,
        IEquipmentLineRepository equipmentLineRepository,
        IEquipmentStoppageRepository stoppageRepository,
        IWorkOrderRepository workOrderRepository,
        ILogger<StoppageDetectionService> logger)
    {
        _counterDataRepository = counterDataRepository ?? throw new ArgumentNullException(nameof(counterDataRepository));
        _equipmentLineRepository = equipmentLineRepository ?? throw new ArgumentNullException(nameof(equipmentLineRepository));
        _stoppageRepository = stoppageRepository ?? throw new ArgumentNullException(nameof(stoppageRepository));
        _workOrderRepository = workOrderRepository ?? throw new ArgumentNullException(nameof(workOrderRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Monitor all active equipment lines for potential stoppages
    /// </summary>
    public async Task<IEnumerable<StoppageDetectedEvent>> MonitorAllLinesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Starting monitoring of all active equipment lines");

        try
        {
            var activeLines = await GetActiveMonitoringLinesAsync(cancellationToken);
            var detectedEvents = new List<StoppageDetectedEvent>();

            foreach (var line in activeLines)
            {
                try
                {
                    var stoppageEvent = await MonitorLineAsync(line.LineId, cancellationToken);
                    if (stoppageEvent != null)
                    {
                        detectedEvents.Add(stoppageEvent);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to monitor line {LineId}", line.LineId);
                    // Continue monitoring other lines even if one fails
                }
            }

            _logger.LogInformation("Completed monitoring {LineCount} lines, detected {StoppageCount} stoppages",
                activeLines.Count(), detectedEvents.Count);

            return detectedEvents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to monitor equipment lines");
            throw;
        }
    }

    /// <summary>
    /// Monitor a specific equipment line for stoppages
    /// </summary>
    public async Task<StoppageDetectedEvent?> MonitorLineAsync(string lineId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lineId))
            throw new ArgumentException("Line ID cannot be null or empty", nameof(lineId));

        _logger.LogDebug("Monitoring line {LineId} for stoppages", lineId);

        try
        {
            // Get last production time
            var lastProductionTime = await GetLastProductionTimeAsync(lineId, cancellationToken);
            if (!lastProductionTime.HasValue)
            {
                _logger.LogDebug("No production activity found for line {LineId}", lineId);
                return null;
            }

            // Validate if stoppage detection is needed
            var validation = await ValidateStoppageCreationAsync(lineId, lastProductionTime.Value, cancellationToken);

            if (!validation.ShouldCreateStoppage)
            {
                _logger.LogDebug("No stoppage detection needed for line {LineId}: {Reason}", lineId, validation.Reason);
                return null;
            }

            // Create the stoppage
            var workOrderId = await GetActiveWorkOrderIdAsync(lineId, cancellationToken);
            var stoppage = await CreateDetectedStoppageAsync(
                lineId,
                lastProductionTime.Value,
                workOrderId,
                validation.DetectionThreshold,
                cancellationToken);

            // Create the event
            var stoppageEvent = new StoppageDetectedEvent(
                stoppage.Id,
                lineId,
                workOrderId,
                stoppage.StartTime,
                DateTime.UtcNow,
                validation.DetectionThreshold,
                lastProductionTime.Value,
                stoppage.RequiresClassification(),
                stoppage
            );

            _logger.LogInformation("Stoppage detected: {Summary}", stoppageEvent.GetSummary());
            return stoppageEvent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to monitor line {LineId}", lineId);
            throw;
        }
    }

    /// <summary>
    /// Check if a specific equipment line is currently stopped
    /// </summary>
    public async Task<bool> IsLineStopped(string lineId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lineId))
            throw new ArgumentException("Line ID cannot be null or empty", nameof(lineId));

        try
        {
            var stoppageDuration = await GetCurrentStoppageDurationAsync(lineId, cancellationToken);
            var threshold = await GetDetectionThresholdAsync(lineId);

            return stoppageDuration.HasValue && stoppageDuration.Value.TotalMinutes >= threshold;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if line {LineId} is stopped", lineId);
            throw;
        }
    }

    /// <summary>
    /// Get the last production activity time for an equipment line
    /// </summary>
    public async Task<DateTime?> GetLastProductionTimeAsync(string lineId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lineId))
            throw new ArgumentException("Line ID cannot be null or empty", nameof(lineId));

        try
        {
            // Get equipment line configuration
            var equipmentLine = await _equipmentLineRepository.GetByLineIdAsync(lineId, cancellationToken);
            if (equipmentLine == null)
            {
                _logger.LogWarning("Equipment line {LineId} not found", lineId);
                return null;
            }

            // Get latest counter reading with production activity
            var latestReading = await _counterDataRepository.GetLatestReadingAsync(
                equipmentLine.AdamDeviceId,
                equipmentLine.AdamChannel,
                cancellationToken);

            if (latestReading?.Rate > 0)
            {
                return latestReading.Timestamp;
            }

            // If latest reading shows no production, look back for last production activity
            var lookbackTime = DateTime.UtcNow.AddHours(-24); // Look back 24 hours max
            var recentReadings = await _counterDataRepository.GetDataForPeriodAsync(
                equipmentLine.AdamDeviceId,
                lookbackTime,
                DateTime.UtcNow,
                cancellationToken);

            var lastProductionReading = recentReadings
                .Where(r => r.Channel == equipmentLine.AdamChannel && r.Rate > 0)
                .OrderByDescending(r => r.Timestamp)
                .FirstOrDefault();

            return lastProductionReading?.Timestamp;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get last production time for line {LineId}", lineId);
            throw;
        }
    }

    /// <summary>
    /// Calculate stoppage duration for a line
    /// </summary>
    public async Task<TimeSpan?> GetCurrentStoppageDurationAsync(string lineId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lineId))
            throw new ArgumentException("Line ID cannot be null or empty", nameof(lineId));

        try
        {
            var lastProductionTime = await GetLastProductionTimeAsync(lineId, cancellationToken);
            if (!lastProductionTime.HasValue)
                return null;

            var duration = DateTime.UtcNow - lastProductionTime.Value;
            return duration.TotalMinutes > 0 ? duration : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate stoppage duration for line {LineId}", lineId);
            throw;
        }
    }

    /// <summary>
    /// Check if a detected stoppage should trigger an alert
    /// </summary>
    public async Task<bool> ShouldTriggerAlertAsync(string lineId, TimeSpan stoppageDuration)
    {
        if (string.IsNullOrWhiteSpace(lineId))
            throw new ArgumentException("Line ID cannot be null or empty", nameof(lineId));

        try
        {
            var threshold = await GetDetectionThresholdAsync(lineId);
            return stoppageDuration.TotalMinutes >= threshold;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check alert threshold for line {LineId}", lineId);
            return false; // Default to no alert on error
        }
    }

    /// <summary>
    /// Get the detection threshold for a specific equipment line
    /// </summary>
    public async Task<int> GetDetectionThresholdAsync(string lineId)
    {
        if (string.IsNullOrWhiteSpace(lineId))
            throw new ArgumentException("Line ID cannot be null or empty", nameof(lineId));

        try
        {
            var equipmentLine = await _equipmentLineRepository.GetByLineIdAsync(lineId);
            // In future, this could come from line-specific configuration
            // For now, use default threshold
            return DefaultDetectionThresholdMinutes;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get detection threshold for line {LineId}, using default", lineId);
            return DefaultDetectionThresholdMinutes;
        }
    }

    /// <summary>
    /// Get equipment lines that require monitoring
    /// </summary>
    public async Task<IEnumerable<EquipmentLine>> GetActiveMonitoringLinesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var allLines = await _equipmentLineRepository.GetAllActiveAsync(cancellationToken);

            // Filter to only lines that are currently assigned work orders or have recent activity
            var activeLines = new List<EquipmentLine>();

            foreach (var line in allLines)
            {
                try
                {
                    // Check if line has active work order
                    var activeWorkOrder = await _workOrderRepository.GetActiveByLineAsync(line.LineId, cancellationToken);
                    if (activeWorkOrder != null)
                    {
                        activeLines.Add(line);
                        continue;
                    }

                    // Check if line has recent production activity (within last 4 hours)
                    var recentActivityTime = DateTime.UtcNow.AddHours(-4);
                    var hasRecentActivity = await _counterDataRepository.HasProductionActivityAsync(
                        line.AdamDeviceId,
                        line.AdamChannel,
                        recentActivityTime,
                        DateTime.UtcNow,
                        cancellationToken);

                    if (hasRecentActivity)
                    {
                        activeLines.Add(line);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to check activity for line {LineId}, including in monitoring", line.LineId);
                    activeLines.Add(line); // Include line in monitoring on error to be safe
                }
            }

            _logger.LogDebug("Found {ActiveLineCount} of {TotalLineCount} lines requiring monitoring",
                activeLines.Count, allLines.Count());

            return activeLines;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active monitoring lines");
            throw;
        }
    }

    /// <summary>
    /// Check if a line already has an active stoppage
    /// </summary>
    public async Task<EquipmentStoppage?> GetActiveStoppageAsync(string lineId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lineId))
            throw new ArgumentException("Line ID cannot be null or empty", nameof(lineId));

        try
        {
            return await _stoppageRepository.GetActiveByLineAsync(lineId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active stoppage for line {LineId}", lineId);
            throw;
        }
    }

    /// <summary>
    /// Validate if a new stoppage should be created or existing one updated
    /// </summary>
    public async Task<StoppageValidationResult> ValidateStoppageCreationAsync(
        string lineId,
        DateTime lastProductionTime,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lineId))
            throw new ArgumentException("Line ID cannot be null or empty", nameof(lineId));

        try
        {
            // Check for existing active stoppage
            var activeStoppage = await GetActiveStoppageAsync(lineId, cancellationToken);
            if (activeStoppage != null)
            {
                return StoppageValidationResult.UseExisting(activeStoppage, "Active stoppage already exists");
            }

            // Calculate stoppage duration
            var currentTime = DateTime.UtcNow;
            var stoppageDuration = currentTime - lastProductionTime;

            // Get detection threshold
            var threshold = await GetDetectionThresholdAsync(lineId);

            // Check if duration meets threshold
            if (stoppageDuration.TotalMinutes < threshold)
            {
                return StoppageValidationResult.NoAction($"Stoppage duration ({stoppageDuration.TotalMinutes:F1} min) below threshold ({threshold} min)");
            }

            return StoppageValidationResult.CreateStoppage(lastProductionTime, stoppageDuration, threshold);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate stoppage creation for line {LineId}", lineId);
            throw;
        }
    }

    /// <summary>
    /// Create a new stoppage based on detection
    /// </summary>
    public async Task<EquipmentStoppage> CreateDetectedStoppageAsync(
        string lineId,
        DateTime startTime,
        string? workOrderId,
        int detectionThreshold,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lineId))
            throw new ArgumentException("Line ID cannot be null or empty", nameof(lineId));

        try
        {
            var stoppage = new EquipmentStoppage(
                lineId,
                startTime,
                workOrderId,
                autoDetected: true,
                minimumThresholdMinutes: Math.Max(detectionThreshold, DefaultClassificationThresholdMinutes)
            );

            var stoppageId = await _stoppageRepository.CreateAsync(stoppage, cancellationToken);

            _logger.LogInformation("Created auto-detected stoppage {StoppageId} for line {LineId} starting at {StartTime}",
                stoppageId, lineId, startTime);

            // Return stoppage with assigned ID
            return new EquipmentStoppage(
                stoppageId,
                stoppage.LineId,
                stoppage.WorkOrderId,
                stoppage.StartTime,
                stoppage.EndTime,
                stoppage.DurationMinutes,
                stoppage.IsClassified,
                stoppage.CategoryCode,
                stoppage.Subcode,
                stoppage.OperatorComments,
                stoppage.ClassifiedBy,
                stoppage.ClassifiedAt,
                stoppage.AutoDetected,
                stoppage.MinimumThresholdMinutes,
                stoppage.CreatedAt,
                stoppage.UpdatedAt
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create detected stoppage for line {LineId}", lineId);
            throw;
        }
    }

    /// <summary>
    /// End an active stoppage when production resumes
    /// </summary>
    public async Task<EquipmentStoppage?> EndActiveStoppageAsync(
        string lineId,
        DateTime? endTime = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lineId))
            throw new ArgumentException("Line ID cannot be null or empty", nameof(lineId));

        try
        {
            var activeStoppage = await GetActiveStoppageAsync(lineId, cancellationToken);
            if (activeStoppage == null)
            {
                _logger.LogDebug("No active stoppage to end for line {LineId}", lineId);
                return null;
            }

            activeStoppage.EndStoppage(endTime);
            await _stoppageRepository.UpdateAsync(activeStoppage, cancellationToken);

            _logger.LogInformation("Ended stoppage {StoppageId} for line {LineId} at {EndTime}",
                activeStoppage.Id, lineId, activeStoppage.EndTime);

            return activeStoppage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to end active stoppage for line {LineId}", lineId);
            throw;
        }
    }

    /// <summary>
    /// Get active work order ID for a line
    /// </summary>
    private async Task<string?> GetActiveWorkOrderIdAsync(string lineId, CancellationToken cancellationToken)
    {
        try
        {
            var activeWorkOrder = await _workOrderRepository.GetActiveByLineAsync(lineId, cancellationToken);
            return activeWorkOrder?.Id;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get active work order for line {LineId}", lineId);
            return null; // Continue without work order association
        }
    }
}
