using Industrial.Adam.Oee.Domain.Entities;

namespace Industrial.Adam.Oee.Domain.Interfaces;

/// <summary>
/// Repository interface for JobSchedule aggregate
/// </summary>
public interface IJobScheduleRepository
{
    /// <summary>
    /// Get job schedule by identifier
    /// </summary>
    /// <param name="scheduleId">Schedule identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Job schedule if found, null otherwise</returns>
    public Task<JobSchedule?> GetByIdAsync(string scheduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get job schedule by work order
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Job schedule if found, null otherwise</returns>
    public Task<JobSchedule?> GetByWorkOrderIdAsync(string workOrderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get job schedules for equipment line
    /// </summary>
    /// <param name="equipmentLineId">Equipment line identifier</param>
    /// <param name="startDate">Start date filter</param>
    /// <param name="endDate">End date filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of job schedules</returns>
    public Task<IEnumerable<JobSchedule>> GetByEquipmentLineAsync(
        string equipmentLineId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get job schedules by status
    /// </summary>
    /// <param name="status">Schedule status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of job schedules</returns>
    public Task<IEnumerable<JobSchedule>> GetByStatusAsync(JobScheduleStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get job schedules by priority range
    /// </summary>
    /// <param name="minPriority">Minimum priority (inclusive)</param>
    /// <param name="maxPriority">Maximum priority (inclusive)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of job schedules</returns>
    public Task<IEnumerable<JobSchedule>> GetByPriorityRangeAsync(int minPriority, int maxPriority, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get job schedules with conflicts
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of job schedules with unresolved conflicts</returns>
    public Task<IEnumerable<JobSchedule>> GetSchedulesWithConflictsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get job schedules with unsatisfied dependencies
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of job schedules with unsatisfied dependencies</returns>
    public Task<IEnumerable<JobSchedule>> GetSchedulesWithUnsatisfiedDependenciesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get overlapping job schedules
    /// </summary>
    /// <param name="equipmentLineId">Equipment line identifier</param>
    /// <param name="startTime">Start time</param>
    /// <param name="endTime">End time</param>
    /// <param name="excludeScheduleId">Schedule to exclude from results</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of overlapping job schedules</returns>
    public Task<IEnumerable<JobSchedule>> GetOverlappingSchedulesAsync(
        string equipmentLineId,
        DateTime startTime,
        DateTime endTime,
        string? excludeScheduleId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get job queue for equipment line
    /// </summary>
    /// <param name="equipmentLineId">Equipment line identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of job schedules ordered by queue sequence</returns>
    public Task<IEnumerable<JobSchedule>> GetJobQueueAsync(string equipmentLineId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get next scheduled job for equipment line
    /// </summary>
    /// <param name="equipmentLineId">Equipment line identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Next scheduled job if found, null otherwise</returns>
    public Task<JobSchedule?> GetNextScheduledJobAsync(string equipmentLineId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get job schedules scheduled by user
    /// </summary>
    /// <param name="scheduledBy">User who scheduled the jobs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of job schedules</returns>
    public Task<IEnumerable<JobSchedule>> GetByScheduledByAsync(string scheduledBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get job schedules for date range
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of job schedules</returns>
    public Task<IEnumerable<JobSchedule>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Search job schedules with filters
    /// </summary>
    /// <param name="filter">Job schedule search filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of job schedules</returns>
    public Task<IEnumerable<JobSchedule>> SearchAsync(JobScheduleSearchFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add new job schedule
    /// </summary>
    /// <param name="jobSchedule">Job schedule to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    public Task AddAsync(JobSchedule jobSchedule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update existing job schedule
    /// </summary>
    /// <param name="jobSchedule">Job schedule to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    public Task UpdateAsync(JobSchedule jobSchedule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete job schedule
    /// </summary>
    /// <param name="scheduleId">Schedule identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    public Task<bool> DeleteAsync(string scheduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update queue sequences for equipment line
    /// </summary>
    /// <param name="equipmentLineId">Equipment line identifier</param>
    /// <param name="scheduleIdSequences">Dictionary of schedule IDs and their new sequences</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    public Task UpdateQueueSequencesAsync(
        string equipmentLineId,
        Dictionary<string, int> scheduleIdSequences,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get scheduling statistics
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="equipmentLineId">Equipment line filter (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Scheduling statistics</returns>
    public Task<SchedulingStatistics> GetStatisticsAsync(
        DateTime startDate,
        DateTime endDate,
        string? equipmentLineId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get resource utilization for time period
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Resource utilization data</returns>
    public Task<IEnumerable<ResourceUtilizationData>> GetResourceUtilizationAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Job schedule search filter
/// </summary>
/// <param name="WorkOrderId">Work order identifier filter</param>
/// <param name="EquipmentLineId">Equipment line identifier filter</param>
/// <param name="Status">Status filter</param>
/// <param name="MinPriority">Minimum priority filter</param>
/// <param name="MaxPriority">Maximum priority filter</param>
/// <param name="ScheduledStartFrom">Scheduled start from filter</param>
/// <param name="ScheduledStartTo">Scheduled start to filter</param>
/// <param name="ScheduledEndFrom">Scheduled end from filter</param>
/// <param name="ScheduledEndTo">Scheduled end to filter</param>
/// <param name="ScheduledBy">Scheduled by filter</param>
/// <param name="ScheduledAtFrom">Scheduled at from filter</param>
/// <param name="ScheduledAtTo">Scheduled at to filter</param>
/// <param name="HasConflicts">Has conflicts filter</param>
/// <param name="HasUnsatisfiedDependencies">Has unsatisfied dependencies filter</param>
/// <param name="MinQueueSequence">Minimum queue sequence filter</param>
/// <param name="MaxQueueSequence">Maximum queue sequence filter</param>
public record JobScheduleSearchFilter(
    string? WorkOrderId = null,
    string? EquipmentLineId = null,
    JobScheduleStatus? Status = null,
    int? MinPriority = null,
    int? MaxPriority = null,
    DateTime? ScheduledStartFrom = null,
    DateTime? ScheduledStartTo = null,
    DateTime? ScheduledEndFrom = null,
    DateTime? ScheduledEndTo = null,
    string? ScheduledBy = null,
    DateTime? ScheduledAtFrom = null,
    DateTime? ScheduledAtTo = null,
    bool? HasConflicts = null,
    bool? HasUnsatisfiedDependencies = null,
    int? MinQueueSequence = null,
    int? MaxQueueSequence = null
);

/// <summary>
/// Scheduling statistics
/// </summary>
/// <param name="TotalSchedules">Total number of schedules</param>
/// <param name="PlannedSchedules">Number of planned schedules</param>
/// <param name="ConfirmedSchedules">Number of confirmed schedules</param>
/// <param name="ActiveSchedules">Number of active schedules</param>
/// <param name="CompletedSchedules">Number of completed schedules</param>
/// <param name="CancelledSchedules">Number of cancelled schedules</param>
/// <param name="SchedulesWithConflicts">Number of schedules with conflicts</param>
/// <param name="SchedulesWithUnsatisfiedDependencies">Number of schedules with unsatisfied dependencies</param>
/// <param name="AveragePriority">Average priority</param>
/// <param name="AverageSetupTimeMinutes">Average setup time in minutes</param>
/// <param name="AverageTeardownTimeMinutes">Average teardown time in minutes</param>
/// <param name="AverageProductionTimeMinutes">Average production time in minutes</param>
/// <param name="TotalScheduledTimeHours">Total scheduled time in hours</param>
/// <param name="AverageSchedulingScore">Average scheduling score</param>
/// <param name="OnTimeCompletionRate">On-time completion rate percentage</param>
public record SchedulingStatistics(
    int TotalSchedules,
    int PlannedSchedules,
    int ConfirmedSchedules,
    int ActiveSchedules,
    int CompletedSchedules,
    int CancelledSchedules,
    int SchedulesWithConflicts,
    int SchedulesWithUnsatisfiedDependencies,
    decimal AveragePriority,
    decimal AverageSetupTimeMinutes,
    decimal AverageTeardownTimeMinutes,
    decimal AverageProductionTimeMinutes,
    decimal TotalScheduledTimeHours,
    decimal AverageSchedulingScore,
    decimal OnTimeCompletionRate
);

/// <summary>
/// Resource utilization data
/// </summary>
/// <param name="EquipmentLineId">Equipment line identifier</param>
/// <param name="EquipmentLineName">Equipment line name</param>
/// <param name="TotalScheduledHours">Total scheduled hours</param>
/// <param name="TotalAvailableHours">Total available hours</param>
/// <param name="UtilizationPercentage">Utilization percentage</param>
/// <param name="SetupHours">Setup hours</param>
/// <param name="ProductionHours">Production hours</param>
/// <param name="TeardownHours">Teardown hours</param>
/// <param name="IdleHours">Idle hours</param>
/// <param name="JobCount">Number of jobs scheduled</param>
/// <param name="AveragePriority">Average job priority</param>
public record ResourceUtilizationData(
    string EquipmentLineId,
    string EquipmentLineName,
    decimal TotalScheduledHours,
    decimal TotalAvailableHours,
    decimal UtilizationPercentage,
    decimal SetupHours,
    decimal ProductionHours,
    decimal TeardownHours,
    decimal IdleHours,
    int JobCount,
    decimal AveragePriority
);
