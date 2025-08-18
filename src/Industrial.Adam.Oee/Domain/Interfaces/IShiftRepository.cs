using Industrial.Adam.Oee.Domain.Entities;

namespace Industrial.Adam.Oee.Domain.Interfaces;

/// <summary>
/// Repository interface for Shift aggregate
/// </summary>
public interface IShiftRepository
{
    /// <summary>
    /// Get shift by identifier
    /// </summary>
    /// <param name="shiftId">Shift identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Shift if found, null otherwise</returns>
    public Task<Shift?> GetByIdAsync(string shiftId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get shifts by pattern
    /// </summary>
    /// <param name="shiftPatternId">Shift pattern identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of shifts</returns>
    public Task<IEnumerable<Shift>> GetByPatternIdAsync(string shiftPatternId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get shifts for date range
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of shifts</returns>
    public Task<IEnumerable<Shift>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active shifts
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of active shifts</returns>
    public Task<IEnumerable<Shift>> GetActiveShiftsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current shift for time
    /// </summary>
    /// <param name="dateTime">Date and time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current shift if found, null otherwise</returns>
    public Task<Shift?> GetCurrentShiftAsync(DateTime dateTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get shifts by supervisor
    /// </summary>
    /// <param name="supervisorId">Supervisor identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of shifts</returns>
    public Task<IEnumerable<Shift>> GetBySupervisorIdAsync(string supervisorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get shifts by status
    /// </summary>
    /// <param name="status">Shift status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of shifts</returns>
    public Task<IEnumerable<Shift>> GetByStatusAsync(ShiftStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get shifts for equipment line
    /// </summary>
    /// <param name="equipmentLineId">Equipment line identifier</param>
    /// <param name="startDate">Start date filter</param>
    /// <param name="endDate">End date filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of shifts</returns>
    public Task<IEnumerable<Shift>> GetByEquipmentLineAsync(
        string equipmentLineId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get shifts with operator
    /// </summary>
    /// <param name="operatorId">Operator identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of shifts</returns>
    public Task<IEnumerable<Shift>> GetShiftsWithOperatorAsync(string operatorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get overlapping shifts
    /// </summary>
    /// <param name="startTime">Start time</param>
    /// <param name="endTime">End time</param>
    /// <param name="equipmentLineId">Equipment line filter (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of overlapping shifts</returns>
    public Task<IEnumerable<Shift>> GetOverlappingShiftsAsync(
        DateTime startTime,
        DateTime endTime,
        string? equipmentLineId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search shifts with filters
    /// </summary>
    /// <param name="filter">Shift search filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of shifts</returns>
    public Task<IEnumerable<Shift>> SearchAsync(ShiftSearchFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add new shift
    /// </summary>
    /// <param name="shift">Shift to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    public Task AddAsync(Shift shift, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update existing shift
    /// </summary>
    /// <param name="shift">Shift to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    public Task UpdateAsync(Shift shift, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete shift
    /// </summary>
    /// <param name="shiftId">Shift identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    public Task<bool> DeleteAsync(string shiftId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get shift performance statistics
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="shiftPatternId">Shift pattern filter (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Shift performance statistics</returns>
    public Task<ShiftPerformanceStatistics> GetPerformanceStatisticsAsync(
        DateTime startDate,
        DateTime endDate,
        string? shiftPatternId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get handover notes for time period
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="noteType">Note type filter (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of handover notes</returns>
    public Task<IEnumerable<ShiftHandoverNote>> GetHandoverNotesAsync(
        DateTime startDate,
        DateTime endDate,
        ShiftHandoverNoteType? noteType = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Shift search filter
/// </summary>
/// <param name="ShiftName">Shift name filter</param>
/// <param name="ShiftPatternId">Shift pattern identifier filter</param>
/// <param name="SupervisorId">Supervisor identifier filter</param>
/// <param name="Status">Status filter</param>
/// <param name="ScheduledStartFrom">Scheduled start from filter</param>
/// <param name="ScheduledStartTo">Scheduled start to filter</param>
/// <param name="ScheduledEndFrom">Scheduled end from filter</param>
/// <param name="ScheduledEndTo">Scheduled end to filter</param>
/// <param name="ActualStartFrom">Actual start from filter</param>
/// <param name="ActualStartTo">Actual start to filter</param>
/// <param name="ActualEndFrom">Actual end from filter</param>
/// <param name="ActualEndTo">Actual end to filter</param>
/// <param name="EquipmentLineId">Equipment line identifier filter</param>
/// <param name="OperatorId">Operator identifier filter</param>
/// <param name="HasLateStart">Has late start filter</param>
/// <param name="HasOvertime">Has overtime filter</param>
public record ShiftSearchFilter(
    string? ShiftName = null,
    string? ShiftPatternId = null,
    string? SupervisorId = null,
    ShiftStatus? Status = null,
    DateTime? ScheduledStartFrom = null,
    DateTime? ScheduledStartTo = null,
    DateTime? ScheduledEndFrom = null,
    DateTime? ScheduledEndTo = null,
    DateTime? ActualStartFrom = null,
    DateTime? ActualStartTo = null,
    DateTime? ActualEndFrom = null,
    DateTime? ActualEndTo = null,
    string? EquipmentLineId = null,
    string? OperatorId = null,
    bool? HasLateStart = null,
    bool? HasOvertime = null
);

/// <summary>
/// Shift performance statistics
/// </summary>
/// <param name="TotalShifts">Total number of shifts</param>
/// <param name="CompletedShifts">Number of completed shifts</param>
/// <param name="ActiveShifts">Number of active shifts</param>
/// <param name="CancelledShifts">Number of cancelled shifts</param>
/// <param name="LateStartShifts">Number of shifts with late start</param>
/// <param name="OvertimeShifts">Number of shifts with overtime</param>
/// <param name="AverageActualDurationHours">Average actual duration in hours</param>
/// <param name="AverageScheduledDurationHours">Average scheduled duration in hours</param>
/// <param name="TotalPlannedProductionHours">Total planned production hours</param>
/// <param name="TotalActualProductionHours">Total actual production hours</param>
/// <param name="AverageOeePercentage">Average OEE percentage</param>
/// <param name="AverageAvailabilityPercentage">Average availability percentage</param>
/// <param name="AveragePerformancePercentage">Average performance percentage</param>
/// <param name="AverageQualityPercentage">Average quality percentage</param>
/// <param name="TotalWorkOrdersPlanned">Total work orders planned</param>
/// <param name="TotalWorkOrdersCompleted">Total work orders completed</param>
/// <param name="TotalHandoverNotes">Total handover notes</param>
/// <param name="TotalAlertNotes">Total alert notes</param>
public record ShiftPerformanceStatistics(
    int TotalShifts,
    int CompletedShifts,
    int ActiveShifts,
    int CancelledShifts,
    int LateStartShifts,
    int OvertimeShifts,
    decimal AverageActualDurationHours,
    decimal AverageScheduledDurationHours,
    decimal TotalPlannedProductionHours,
    decimal TotalActualProductionHours,
    decimal AverageOeePercentage,
    decimal AverageAvailabilityPercentage,
    decimal AveragePerformancePercentage,
    decimal AverageQualityPercentage,
    int TotalWorkOrdersPlanned,
    int TotalWorkOrdersCompleted,
    int TotalHandoverNotes,
    int TotalAlertNotes
);
