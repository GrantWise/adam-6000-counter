using Industrial.Adam.Oee.Domain.Entities;

namespace Industrial.Adam.Oee.Domain.Interfaces;

/// <summary>
/// Service interface for simple job queue management
/// Provides business logic for basic job sequencing and queue operations
/// </summary>
public interface ISimpleJobQueueService
{
    /// <summary>
    /// Get the next job to process for a specific line
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Next job or null if no jobs available</returns>
    public Task<QueuedJob?> GetNextJobAsync(string lineId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a job to the queue for a specific line
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="productDescription">Product description</param>
    /// <param name="priority">Job priority (1=highest, 10=lowest, default=5)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Queue position of the added job</returns>
    public Task<int> AddJobToQueueAsync(string lineId, string workOrderId, string productDescription, int priority = 5, CancellationToken cancellationToken = default);

    /// <summary>
    /// Start a job by assigning an operator
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="operatorId">Operator identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task StartJobAsync(string lineId, string workOrderId, string operatorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Complete and remove a job from the queue
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task CompleteJobAsync(string lineId, string workOrderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove a job from the queue without completion
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task RemoveJobAsync(string lineId, string workOrderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current queue status for a line
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of jobs in priority order</returns>
    public Task<List<QueuedJob>> GetQueueStatusAsync(string lineId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get position of a specific job in the queue
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Queue position (1-based) or -1 if not found</returns>
    public Task<int> GetQueuePositionAsync(string lineId, string workOrderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a work order is already in any queue
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if work order is already queued</returns>
    public Task<bool> IsWorkOrderQueuedAsync(string workOrderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get summary of all active queues
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of queue summaries</returns>
    public Task<List<JobQueueSummary>> GetActiveQueueSummariesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Summary information for a job queue
/// </summary>
/// <param name="LineId">Equipment line identifier</param>
/// <param name="TotalJobs">Total number of jobs in queue</param>
/// <param name="PendingJobs">Number of jobs not yet started</param>
/// <param name="InProgressJobs">Number of jobs currently in progress</param>
/// <param name="NextJob">Next job to be processed (if any)</param>
public record JobQueueSummary(
    string LineId,
    int TotalJobs,
    int PendingJobs,
    int InProgressJobs,
    QueuedJob? NextJob
);
