using Industrial.Adam.Oee.Domain.Entities;

namespace Industrial.Adam.Oee.Domain.Interfaces;

/// <summary>
/// Repository interface for SimpleJobQueue aggregate root
/// Provides data access operations for the simplified job queue system
/// </summary>
public interface ISimpleJobQueueRepository
{
    /// <summary>
    /// Get job queue for a specific equipment line
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Job queue or null if not found</returns>
    public Task<SimpleJobQueue?> GetByLineIdAsync(string lineId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get or create job queue for an equipment line
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Existing or newly created job queue</returns>
    public Task<SimpleJobQueue> GetOrCreateByLineIdAsync(string lineId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save job queue changes
    /// </summary>
    /// <param name="jobQueue">Job queue to save</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task SaveAsync(SimpleJobQueue jobQueue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new job queue for an equipment line
    /// </summary>
    /// <param name="jobQueue">Job queue to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task AddAsync(SimpleJobQueue jobQueue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all job queues with jobs
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of job queues that have jobs</returns>
    public Task<List<SimpleJobQueue>> GetActiveQueuesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a work order is already in any queue
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if work order is already queued</returns>
    public Task<bool> IsWorkOrderQueuedAsync(string workOrderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove a specific job from all queues
    /// </summary>
    /// <param name="workOrderId">Work order identifier to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if job was found and removed</returns>
    public Task<bool> RemoveJobFromAllQueuesAsync(string workOrderId, CancellationToken cancellationToken = default);
}
