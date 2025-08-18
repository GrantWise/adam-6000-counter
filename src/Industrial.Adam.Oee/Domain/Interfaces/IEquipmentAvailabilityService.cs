using Industrial.Adam.Oee.Domain.ValueObjects;

namespace Industrial.Adam.Oee.Domain.Interfaces;

/// <summary>
/// Service interface for retrieving equipment availability data from Equipment Scheduling system.
/// This interface provides the contract for consuming planned availability data to improve OEE accuracy.
/// </summary>
public interface IEquipmentAvailabilityService
{
    /// <summary>
    /// Determines if equipment is planned to be operating at a specific timestamp
    /// </summary>
    /// <param name="lineId">Equipment line identifier (mapped to resource)</param>
    /// <param name="timestamp">Point in time to check availability</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if equipment is planned to be operating, false otherwise</returns>
    public Task<bool> IsPlannedOperatingAsync(string lineId, DateTime timestamp, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets planned operating hours for equipment on a specific date
    /// </summary>
    /// <param name="lineId">Equipment line identifier (mapped to resource)</param>
    /// <param name="date">Date to get planned hours for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Planned hours information for the date</returns>
    public Task<PlannedHours> GetPlannedHoursAsync(string lineId, DateTime date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets weekly equipment availability schedule
    /// </summary>
    /// <param name="lineId">Equipment line identifier (mapped to resource)</param>
    /// <param name="weekStart">Start date of the week (Monday)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Weekly availability schedule</returns>
    public Task<AvailabilitySchedule> GetWeeklyScheduleAsync(string lineId, DateTime weekStart, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets planned availability percentage for a date range
    /// </summary>
    /// <param name="lineId">Equipment line identifier (mapped to resource)</param>
    /// <param name="startDate">Range start date</param>
    /// <param name="endDate">Range end date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Availability summary for the date range</returns>
    public Task<AvailabilitySummary> GetAvailabilitySummaryAsync(string lineId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current active schedules for equipment
    /// </summary>
    /// <param name="lineId">Equipment line identifier (mapped to resource)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Currently active schedules</returns>
    public Task<IEnumerable<ActiveSchedule>> GetCurrentActiveSchedulesAsync(string lineId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the Equipment Scheduling service is available
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health check result</returns>
    public Task<ServiceHealthResult> CheckServiceHealthAsync(CancellationToken cancellationToken = default);
}
