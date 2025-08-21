using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Industrial.Adam.EquipmentScheduling.Application.DTOs;
using Industrial.Adam.Oee.Domain.Interfaces;
using Industrial.Adam.Oee.Domain.ValueObjects;
using Industrial.Adam.Oee.Infrastructure.Configuration;
using Industrial.Adam.Oee.Infrastructure.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Industrial.Adam.Oee.Infrastructure.Services;

/// <summary>
/// HTTP client implementation for accessing Equipment Scheduling availability data
/// Implements caching, circuit breaker patterns, and graceful degradation
/// </summary>
public sealed class EquipmentAvailabilityService : IEquipmentAvailabilityService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<EquipmentAvailabilityService> _logger;
    private readonly EquipmentSchedulingSettings _settings;
    private readonly ActivitySource _activitySource;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Initializes a new instance of the Equipment Availability Service
    /// </summary>
    /// <param name="httpClient">HTTP client instance</param>
    /// <param name="memoryCache">Memory cache for caching responses</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="settings">Equipment Scheduling configuration settings</param>
    public EquipmentAvailabilityService(
        HttpClient httpClient,
        IMemoryCache memoryCache,
        ILogger<EquipmentAvailabilityService> logger,
        IOptions<EquipmentSchedulingSettings> settings)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));

        _activitySource = new ActivitySource("Industrial.Adam.Oee.Infrastructure.EquipmentAvailability");

        // Configure HTTP client from settings
        _httpClient.Timeout = _settings.RequestTimeout;
        if (!string.IsNullOrWhiteSpace(_settings.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(_settings.GetApiBaseUrl());
        }
    }

    /// <summary>
    /// Determines if equipment is planned to be operating at a specific timestamp
    /// </summary>
    /// <param name="lineId">Equipment line identifier (mapped to resource)</param>
    /// <param name="timestamp">Point in time to check availability</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if equipment is planned to be operating, false otherwise</returns>
    public async Task<bool> IsPlannedOperatingAsync(string lineId, DateTime timestamp, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("IsPlannedOperating");
        activity?.SetTag("oee.line_id", lineId);
        activity?.SetTag("oee.timestamp", timestamp.ToString("yyyy-MM-ddTHH:mm:ssZ"));

        var cacheKey = $"is_operating:{lineId}:{timestamp:yyyy-MM-ddTHH:mm}";

        // Check cache first
        if (_settings.EnableCaching && _memoryCache.TryGetValue(cacheKey, out bool cachedResult))
        {
            _logger.LogDebug("Cache hit for is operating check: {LineId} at {Timestamp}", lineId, timestamp);
            activity?.SetTag("oee.cache_hit", true);
            return cachedResult;
        }

        try
        {
            var resourceId = _settings.MapLineIdToResourceId(lineId);
            if (resourceId == null)
            {
                _logger.LogWarning("No resource mapping found for line {LineId}, assuming not operating", lineId);
                return false;
            }

            var endpoint = $"equipment-scheduling/availability/equipment/{resourceId}/is-operating";
            var queryParams = $"?timestamp={timestamp:yyyy-MM-ddTHH:mm:ssZ}";

            var response = await _httpClient.GetAsync($"{endpoint}{queryParams}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(JsonOptions, cancellationToken);

                if (apiResponse?.Success == true && apiResponse.Data != null)
                {
                    // Parse the response data to extract IsOperating flag
                    var responseJson = JsonSerializer.Serialize(apiResponse.Data);
                    var operatingData = JsonSerializer.Deserialize<OperatingStatusResponse>(responseJson, JsonOptions);

                    var isOperating = operatingData?.IsOperating ?? false;

                    // Cache the result
                    if (_settings.EnableCaching)
                    {
                        var cacheExpiry = TimeSpan.FromMinutes(5); // Short cache for real-time checks
                        _memoryCache.Set(cacheKey, isOperating, cacheExpiry);
                    }

                    _logger.LogDebug("Equipment {LineId} operating status at {Timestamp}: {IsOperating}",
                        lineId, timestamp, isOperating);

                    activity?.SetTag("oee.is_operating", isOperating);
                    return isOperating;
                }
            }

            _logger.LogWarning("Failed to get operating status for {LineId} at {Timestamp}: {StatusCode}",
                lineId, timestamp, response.StatusCode);

            return await GetFallbackOperatingStatus(lineId, timestamp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking operating status for {LineId} at {Timestamp}", lineId, timestamp);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            return await GetFallbackOperatingStatus(lineId, timestamp);
        }
    }

    /// <summary>
    /// Gets planned operating hours for equipment on a specific date
    /// </summary>
    /// <param name="lineId">Equipment line identifier (mapped to resource)</param>
    /// <param name="date">Date to get planned hours for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Planned hours information for the date</returns>
    public async Task<PlannedHours> GetPlannedHoursAsync(string lineId, DateTime date, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("GetPlannedHours");
        activity?.SetTag("oee.line_id", lineId);
        activity?.SetTag("oee.date", date.ToString("yyyy-MM-dd"));

        var cacheKey = $"planned_hours:{lineId}:{date:yyyy-MM-dd}";

        // Check cache first
        if (_settings.EnableCaching && _memoryCache.TryGetValue(cacheKey, out PlannedHours? cachedHours))
        {
            _logger.LogDebug("Cache hit for planned hours: {LineId} on {Date}", lineId, date);
            activity?.SetTag("oee.cache_hit", true);
            return cachedHours!;
        }

        try
        {
            var resourceId = _settings.MapLineIdToResourceId(lineId);
            if (resourceId == null)
            {
                _logger.LogWarning("No resource mapping found for line {LineId}, returning default planned hours", lineId);
                return CreateDefaultPlannedHours(lineId, date);
            }

            var endpoint = $"equipment-scheduling/availability/equipment/{resourceId}/daily-summary";
            var queryParams = $"?date={date:yyyy-MM-dd}";

            var response = await _httpClient.GetAsync($"{endpoint}{queryParams}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<DailyScheduleSummaryDto>>(JsonOptions, cancellationToken);

                if (apiResponse?.Success == true && apiResponse.Data != null)
                {
                    var plannedHours = MapDailySummaryToPlannedHours(apiResponse.Data, date);

                    // Cache the result
                    if (_settings.EnableCaching)
                    {
                        _memoryCache.Set(cacheKey, plannedHours, _settings.CacheTtl);
                    }

                    _logger.LogDebug("Retrieved planned hours for {LineId} on {Date}: {TotalHours}h",
                        lineId, date, plannedHours.TotalHours);

                    activity?.SetTag("oee.planned_hours", (double)plannedHours.TotalHours);
                    return plannedHours;
                }
            }

            _logger.LogWarning("Failed to get planned hours for {LineId} on {Date}: {StatusCode}",
                lineId, date, response.StatusCode);

            return CreateDefaultPlannedHours(lineId, date);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting planned hours for {LineId} on {Date}", lineId, date);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            return CreateDefaultPlannedHours(lineId, date);
        }
    }

    /// <summary>
    /// Gets weekly equipment availability schedule
    /// </summary>
    /// <param name="lineId">Equipment line identifier (mapped to resource)</param>
    /// <param name="weekStart">Start date of the week (Monday)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Weekly availability schedule</returns>
    public async Task<AvailabilitySchedule> GetWeeklyScheduleAsync(string lineId, DateTime weekStart, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("GetWeeklySchedule");
        activity?.SetTag("oee.line_id", lineId);
        activity?.SetTag("oee.week_start", weekStart.ToString("yyyy-MM-dd"));

        var cacheKey = $"weekly_schedule:{lineId}:{weekStart:yyyy-MM-dd}";

        // Check cache first
        if (_settings.EnableCaching && _memoryCache.TryGetValue(cacheKey, out AvailabilitySchedule? cachedSchedule))
        {
            _logger.LogDebug("Cache hit for weekly schedule: {LineId} week {WeekStart}", lineId, weekStart);
            activity?.SetTag("oee.cache_hit", true);
            return cachedSchedule!;
        }

        try
        {
            var resourceId = _settings.MapLineIdToResourceId(lineId);
            if (resourceId == null)
            {
                _logger.LogWarning("No resource mapping found for line {LineId}, returning default schedule", lineId);
                return CreateDefaultWeeklySchedule(lineId, weekStart);
            }

            var weekEnd = weekStart.AddDays(6);
            var endpoint = $"equipment-scheduling/availability/equipment/{resourceId}/schedules";
            var queryParams = $"?startDate={weekStart:yyyy-MM-dd}&endDate={weekEnd:yyyy-MM-dd}";

            var response = await _httpClient.GetAsync($"{endpoint}{queryParams}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<EquipmentScheduleDto>>>(JsonOptions, cancellationToken);

                if (apiResponse?.Success == true && apiResponse.Data != null)
                {
                    var schedule = MapSchedulesToAvailabilitySchedule(lineId, weekStart, apiResponse.Data);

                    // Cache the result
                    if (_settings.EnableCaching)
                    {
                        _memoryCache.Set(cacheKey, schedule, _settings.ScheduleCacheTtl);
                    }

                    _logger.LogDebug("Retrieved weekly schedule for {LineId} week {WeekStart}: {TotalHours}h total",
                        lineId, weekStart, schedule.TotalWeeklyHours);

                    activity?.SetTag("oee.weekly_hours", (double)schedule.TotalWeeklyHours);
                    return schedule;
                }
            }

            _logger.LogWarning("Failed to get weekly schedule for {LineId} week {WeekStart}: {StatusCode}",
                lineId, weekStart, response.StatusCode);

            return CreateDefaultWeeklySchedule(lineId, weekStart);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting weekly schedule for {LineId} week {WeekStart}", lineId, weekStart);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            return CreateDefaultWeeklySchedule(lineId, weekStart);
        }
    }

    /// <summary>
    /// Gets planned availability percentage for a date range
    /// </summary>
    /// <param name="lineId">Equipment line identifier (mapped to resource)</param>
    /// <param name="startDate">Range start date</param>
    /// <param name="endDate">Range end date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Availability summary for the date range</returns>
    public async Task<AvailabilitySummary> GetAvailabilitySummaryAsync(string lineId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("GetAvailabilitySummary");
        activity?.SetTag("oee.line_id", lineId);
        activity?.SetTag("oee.start_date", startDate.ToString("yyyy-MM-dd"));
        activity?.SetTag("oee.end_date", endDate.ToString("yyyy-MM-dd"));

        var cacheKey = $"availability_summary:{lineId}:{startDate:yyyy-MM-dd}:{endDate:yyyy-MM-dd}";

        // Check cache first
        if (_settings.EnableCaching && _memoryCache.TryGetValue(cacheKey, out AvailabilitySummary? cachedSummary))
        {
            _logger.LogDebug("Cache hit for availability summary: {LineId} from {StartDate} to {EndDate}",
                lineId, startDate, endDate);
            activity?.SetTag("oee.cache_hit", true);
            return cachedSummary!;
        }

        try
        {
            var resourceId = _settings.MapLineIdToResourceId(lineId);
            if (resourceId == null)
            {
                _logger.LogWarning("No resource mapping found for line {LineId}, returning default summary", lineId);
                return CreateDefaultAvailabilitySummary(lineId, startDate, endDate);
            }

            var endpoint = $"equipment-scheduling/availability/equipment/{resourceId}/availability";
            var queryParams = $"?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}";

            var response = await _httpClient.GetAsync($"{endpoint}{queryParams}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<ScheduleAvailabilityDto>>(JsonOptions, cancellationToken);

                if (apiResponse?.Success == true && apiResponse.Data != null)
                {
                    var summary = MapAvailabilityToSummary(lineId, apiResponse.Data);

                    // Cache the result
                    if (_settings.EnableCaching)
                    {
                        _memoryCache.Set(cacheKey, summary, _settings.CacheTtl);
                    }

                    _logger.LogDebug("Retrieved availability summary for {LineId} from {StartDate} to {EndDate}: {AvailabilityPercentage:P1}",
                        lineId, startDate, endDate, summary.AvailabilityPercentage);

                    activity?.SetTag("oee.availability_percentage", (double)summary.AvailabilityPercentage);
                    return summary;
                }
            }

            _logger.LogWarning("Failed to get availability summary for {LineId} from {StartDate} to {EndDate}: {StatusCode}",
                lineId, startDate, endDate, response.StatusCode);

            return CreateDefaultAvailabilitySummary(lineId, startDate, endDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting availability summary for {LineId} from {StartDate} to {EndDate}",
                lineId, startDate, endDate);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            return CreateDefaultAvailabilitySummary(lineId, startDate, endDate);
        }
    }

    /// <summary>
    /// Gets current active schedules for equipment
    /// </summary>
    /// <param name="lineId">Equipment line identifier (mapped to resource)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Currently active schedules</returns>
    public async Task<IEnumerable<ActiveSchedule>> GetCurrentActiveSchedulesAsync(string lineId, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("GetCurrentActiveSchedules");
        activity?.SetTag("oee.line_id", lineId);

        var cacheKey = $"active_schedules:{lineId}";

        // Check cache first (short TTL for active schedules)
        if (_settings.EnableCaching && _memoryCache.TryGetValue(cacheKey, out IEnumerable<ActiveSchedule>? cachedSchedules))
        {
            _logger.LogDebug("Cache hit for active schedules: {LineId}", lineId);
            activity?.SetTag("oee.cache_hit", true);
            return cachedSchedules!;
        }

        try
        {
            var resourceId = _settings.MapLineIdToResourceId(lineId);
            if (resourceId == null)
            {
                _logger.LogWarning("No resource mapping found for line {LineId}, returning empty active schedules", lineId);
                return Array.Empty<ActiveSchedule>();
            }

            var endpoint = $"equipment-scheduling/availability/current-active";
            var queryParams = $"?resourceId={resourceId}";

            var response = await _httpClient.GetAsync($"{endpoint}{queryParams}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<EquipmentScheduleDto>>>(JsonOptions, cancellationToken);

                if (apiResponse?.Success == true && apiResponse.Data != null)
                {
                    var activeSchedules = apiResponse.Data.Select(s => MapScheduleToActiveSchedule(lineId, s)).ToList();

                    // Cache the result (short TTL for active schedules)
                    if (_settings.EnableCaching)
                    {
                        var cacheExpiry = TimeSpan.FromMinutes(2);
                        _memoryCache.Set(cacheKey, activeSchedules, cacheExpiry);
                    }

                    _logger.LogDebug("Retrieved {Count} active schedules for {LineId}", activeSchedules.Count, lineId);

                    activity?.SetTag("oee.active_schedules_count", activeSchedules.Count);
                    return activeSchedules;
                }
            }

            _logger.LogWarning("Failed to get active schedules for {LineId}: {StatusCode}", lineId, response.StatusCode);

            return Array.Empty<ActiveSchedule>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active schedules for {LineId}", lineId);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            return Array.Empty<ActiveSchedule>();
        }
    }

    /// <summary>
    /// Checks if the Equipment Scheduling service is available
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health check result</returns>
    public async Task<ServiceHealthResult> CheckServiceHealthAsync(CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("CheckServiceHealth");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var endpoint = _settings.HealthCheckEndpoint.TrimStart('/');

            using var healthCheckCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            healthCheckCts.CancelAfter(_settings.HealthCheckTimeout);

            var response = await _httpClient.GetAsync(endpoint, healthCheckCts.Token);
            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
            {
                var healthResult = ServiceHealthResult.Healthy("Equipment Scheduling", stopwatch.Elapsed);
                _logger.LogDebug("Equipment Scheduling service health check passed in {ResponseTime}ms",
                    stopwatch.ElapsedMilliseconds);

                activity?.SetTag("oee.health_status", "healthy");
                activity?.SetTag("oee.response_time_ms", stopwatch.ElapsedMilliseconds);
                return healthResult;
            }
            else
            {
                var healthResult = ServiceHealthResult.Unhealthy("Equipment Scheduling", stopwatch.Elapsed,
                    $"HTTP {response.StatusCode}: {response.ReasonPhrase}");
                _logger.LogWarning("Equipment Scheduling service health check failed: {StatusCode} {ReasonPhrase}",
                    response.StatusCode, response.ReasonPhrase);

                activity?.SetTag("oee.health_status", "unhealthy");
                return healthResult;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            var healthResult = ServiceHealthResult.Unavailable("Equipment Scheduling", "Health check was cancelled");
            _logger.LogWarning("Equipment Scheduling service health check was cancelled");

            activity?.SetTag("oee.health_status", "cancelled");
            return healthResult;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var healthResult = ServiceHealthResult.Unavailable("Equipment Scheduling", ex.Message);
            _logger.LogError(ex, "Equipment Scheduling service health check failed");

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("oee.health_status", "unavailable");
            return healthResult;
        }
    }

    // Private helper methods for mapping and fallback logic

    private async Task<bool> GetFallbackOperatingStatus(string lineId, DateTime timestamp)
    {
        if (!_settings.EnableFallback)
            return false;

        // Simple fallback: assume operating during default hours
        var hour = timestamp.Hour;
        var isBusinessHours = hour >= 8 && hour <= 18; // 8 AM to 6 PM

        _logger.LogDebug("Using fallback operating status for {LineId} at {Timestamp}: {IsOperating}",
            lineId, timestamp, isBusinessHours);

        return await Task.FromResult(isBusinessHours);
    }

    private PlannedHours CreateDefaultPlannedHours(string lineId, DateTime date)
    {
        if (!_settings.EnableFallback)
        {
            return PlannedHours.NoScheduledHours(date);
        }

        var defaultHours = _settings.DefaultOperatingHours;
        var shifts = new List<ScheduledShift>();

        if (defaultHours > 0)
        {
            shifts.Add(new ScheduledShift("DEFAULT", TimeOnly.MinValue,
                TimeOnly.FromTimeSpan(TimeSpan.FromHours((double)defaultHours)), defaultHours));
        }

        _logger.LogDebug("Using default planned hours for {LineId} on {Date}: {Hours}h",
            lineId, date, defaultHours);

        return new PlannedHours(date, defaultHours, shifts, confidence: 0.5m);
    }

    private AvailabilitySchedule CreateDefaultWeeklySchedule(string lineId, DateTime weekStart)
    {
        if (!_settings.EnableFallback)
        {
            return AvailabilitySchedule.CreateEmpty(lineId, weekStart);
        }

        _logger.LogDebug("Using default weekly schedule for {LineId} week {WeekStart}", lineId, weekStart);

        return AvailabilitySchedule.CreateFullWeek(lineId, weekStart);
    }

    private AvailabilitySummary CreateDefaultAvailabilitySummary(string lineId, DateTime startDate, DateTime endDate)
    {
        if (!_settings.EnableFallback)
        {
            return AvailabilitySummary.CreateEmpty(lineId, startDate, endDate);
        }

        var dailyBreakdown = new List<DailyAvailability>();
        var defaultHours = _settings.DefaultOperatingHours;

        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            var shifts = defaultHours > 0
                ? new[] { new ScheduledShift("DEFAULT", TimeOnly.MinValue,
                    TimeOnly.FromTimeSpan(TimeSpan.FromHours((double)defaultHours)), defaultHours) }
                : Array.Empty<ScheduledShift>();

            dailyBreakdown.Add(new DailyAvailability(date, defaultHours, shifts, confidence: 0.5m));
        }

        _logger.LogDebug("Using default availability summary for {LineId} from {StartDate} to {EndDate}",
            lineId, startDate, endDate);

        return new AvailabilitySummary(lineId, startDate, endDate, dailyBreakdown);
    }

    private static PlannedHours MapDailySummaryToPlannedHours(DailyScheduleSummaryDto summary, DateTime date)
    {
        var shifts = summary.Schedules.Select(s => new ScheduledShift(
            s.ShiftCode ?? "SHIFT",
            s.PlannedStartTime?.TimeOfDay.ToTimeOnly() ?? TimeOnly.MinValue,
            s.PlannedEndTime?.TimeOfDay.ToTimeOnly() ?? TimeOnly.MaxValue,
            s.PlannedHours
        )).ToList();

        return new PlannedHours(date, summary.TotalPlannedHours, shifts, summary.HasExceptions);
    }

    private static AvailabilitySchedule MapSchedulesToAvailabilitySchedule(string lineId, DateTime weekStart, IEnumerable<EquipmentScheduleDto> schedules)
    {
        var dailyHours = new Dictionary<DateTime, PlannedHours>();

        // Initialize all days of the week
        for (int i = 0; i < 7; i++)
        {
            var date = weekStart.AddDays(i);
            dailyHours[date] = PlannedHours.NoScheduledHours(date);
        }

        // Group schedules by date and create PlannedHours
        var scheduleGroups = schedules.GroupBy(s => s.ScheduleDate.Date);

        foreach (var group in scheduleGroups)
        {
            var date = group.Key;
            var daySchedules = group.ToList();

            var shifts = daySchedules.Select(s => new ScheduledShift(
                s.ShiftCode ?? "SHIFT",
                s.PlannedStartTime?.TimeOfDay.ToTimeOnly() ?? TimeOnly.MinValue,
                s.PlannedEndTime?.TimeOfDay.ToTimeOnly() ?? TimeOnly.MaxValue,
                s.PlannedHours
            )).ToList();

            var totalHours = daySchedules.Sum(s => s.PlannedHours);
            var hasExceptions = daySchedules.Any(s => s.IsException);

            dailyHours[date] = new PlannedHours(date, totalHours, shifts, hasExceptions);
        }

        return new AvailabilitySchedule(lineId, weekStart, dailyHours.AsReadOnly());
    }

    private static AvailabilitySummary MapAvailabilityToSummary(string lineId, ScheduleAvailabilityDto availability)
    {
        var dailyBreakdown = availability.Schedules.GroupBy(s => s.ScheduleDate.Date)
            .Select(group =>
            {
                var date = group.Key;
                var daySchedules = group.ToList();

                var shifts = daySchedules.Select(s => new ScheduledShift(
                    s.ShiftCode ?? "SHIFT",
                    s.PlannedStartTime?.TimeOfDay.ToTimeOnly() ?? TimeOnly.MinValue,
                    s.PlannedEndTime?.TimeOfDay.ToTimeOnly() ?? TimeOnly.MaxValue,
                    s.PlannedHours
                )).ToList();

                var totalHours = daySchedules.Sum(s => s.PlannedHours);
                var hasExceptions = daySchedules.Any(s => s.IsException);

                return new DailyAvailability(date, totalHours, shifts, hasExceptions);
            }).ToList();

        return new AvailabilitySummary(lineId, availability.StartDate, availability.EndDate, dailyBreakdown);
    }

    private static ActiveSchedule MapScheduleToActiveSchedule(string lineId, EquipmentScheduleDto schedule)
    {
        var status = schedule.ScheduleStatus switch
        {
            Industrial.Adam.EquipmentScheduling.Domain.Enums.ScheduleStatus.Planned => ScheduleStatus.Planned,
            Industrial.Adam.EquipmentScheduling.Domain.Enums.ScheduleStatus.Active => ScheduleStatus.Active,
            Industrial.Adam.EquipmentScheduling.Domain.Enums.ScheduleStatus.Completed => ScheduleStatus.Completed,
            Industrial.Adam.EquipmentScheduling.Domain.Enums.ScheduleStatus.Cancelled => ScheduleStatus.Cancelled,
            _ => ScheduleStatus.Planned
        };

        return new ActiveSchedule(
            schedule.Id,
            lineId,
            schedule.PlannedStartTime ?? schedule.ScheduleDate,
            schedule.PlannedEndTime ?? schedule.ScheduleDate.AddHours((double)schedule.PlannedHours),
            status,
            schedule.ShiftCode,
            schedule.IsException,
            schedule.GeneratedAt,
            schedule.Notes
        );
    }

    /// <summary>
    /// Disposes the service and its resources
    /// </summary>
    public void Dispose()
    {
        _activitySource?.Dispose();
    }
}

/// <summary>
/// Response model for operating status endpoint
/// </summary>
internal sealed class OperatingStatusResponse
{
    public long ResourceId { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsOperating { get; set; }
    public IEnumerable<object>? ActiveSchedules { get; set; }
}

/// <summary>
/// Extension methods for DateTime/TimeSpan conversions
/// </summary>
internal static class TimeExtensions
{
    public static TimeOnly ToTimeOnly(this TimeSpan timeSpan)
    {
        return TimeOnly.FromTimeSpan(timeSpan);
    }
}
