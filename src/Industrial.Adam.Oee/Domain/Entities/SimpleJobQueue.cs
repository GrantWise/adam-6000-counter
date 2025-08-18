using Industrial.Adam.Oee.Domain.Interfaces;

namespace Industrial.Adam.Oee.Domain.Entities;

/// <summary>
/// Simple Job Queue Aggregate Root
/// 
/// Replaces complex scheduling with basic FIFO/priority queue management.
/// Focuses on essential job sequencing for equipment line work orders.
/// </summary>
public sealed class SimpleJobQueue : Entity<int>, IAggregateRoot
{
    /// <summary>
    /// Equipment line identifier this queue belongs to
    /// </summary>
    public string LineId { get; private set; }

    /// <summary>
    /// When this queue was created
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Jobs in the queue
    /// </summary>
    private readonly List<QueuedJob> _jobs = new();

    /// <summary>
    /// Read-only access to jobs in priority order (lower number = higher priority)
    /// </summary>
    public IReadOnlyList<QueuedJob> Jobs => _jobs
        .OrderBy(j => j.Priority)
        .ThenBy(j => j.QueuedAt)
        .ToList()
        .AsReadOnly();

    /// <summary>
    /// Default constructor for serialization
    /// </summary>
    private SimpleJobQueue() : base()
    {
        LineId = string.Empty;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a new simple job queue for an equipment line
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <exception cref="ArgumentException">Thrown when lineId is invalid</exception>
    public SimpleJobQueue(string lineId) : base()
    {
        if (string.IsNullOrWhiteSpace(lineId))
            throw new ArgumentException("Line ID is required", nameof(lineId));

        LineId = lineId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Add a job to the queue
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="productDescription">Product description for display</param>
    /// <param name="priority">Priority (1 = highest, 10 = lowest, default = 5)</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when job already exists in queue</exception>
    public void AddJob(string workOrderId, string productDescription, int priority = 5)
    {
        if (string.IsNullOrWhiteSpace(workOrderId))
            throw new ArgumentException("Work order ID is required", nameof(workOrderId));

        if (string.IsNullOrWhiteSpace(productDescription))
            throw new ArgumentException("Product description is required", nameof(productDescription));

        if (priority < 1 || priority > 10)
            throw new ArgumentException("Priority must be between 1 and 10", nameof(priority));

        if (_jobs.Any(j => j.WorkOrderId == workOrderId))
            throw new InvalidOperationException($"Work order {workOrderId} is already in the queue");

        var queuedJob = new QueuedJob(
            workOrderId,
            productDescription,
            priority,
            DateTime.UtcNow
        );

        _jobs.Add(queuedJob);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Get the next job to process (highest priority, earliest queued)
    /// </summary>
    /// <returns>Next job or null if queue is empty or all jobs are started</returns>
    public QueuedJob? GetNextJob()
    {
        return Jobs.FirstOrDefault(j => j.OperatorId == null);
    }

    /// <summary>
    /// Start a job by assigning an operator
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="operatorId">Operator identifier</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when job cannot be started</exception>
    public void StartJob(string workOrderId, string operatorId)
    {
        if (string.IsNullOrWhiteSpace(workOrderId))
            throw new ArgumentException("Work order ID is required", nameof(workOrderId));

        if (string.IsNullOrWhiteSpace(operatorId))
            throw new ArgumentException("Operator ID is required", nameof(operatorId));

        var job = _jobs.FirstOrDefault(j => j.WorkOrderId == workOrderId);
        if (job == null)
            throw new InvalidOperationException($"Work order {workOrderId} not found in queue");

        if (job.OperatorId != null)
            throw new InvalidOperationException($"Work order {workOrderId} is already started");

        // Replace job with started version
        _jobs.Remove(job);
        var startedJob = job with { OperatorId = operatorId, StartedAt = DateTime.UtcNow };
        _jobs.Add(startedJob);

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Complete and remove a job from the queue
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <exception cref="ArgumentException">Thrown when work order not found</exception>
    public void CompleteJob(string workOrderId)
    {
        if (string.IsNullOrWhiteSpace(workOrderId))
            throw new ArgumentException("Work order ID is required", nameof(workOrderId));

        var job = _jobs.FirstOrDefault(j => j.WorkOrderId == workOrderId);
        if (job == null)
            throw new ArgumentException($"Work order {workOrderId} not found in queue", nameof(workOrderId));

        _jobs.Remove(job);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Remove a job from the queue (cancel before completion)
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <exception cref="ArgumentException">Thrown when work order not found</exception>
    public void RemoveJob(string workOrderId)
    {
        if (string.IsNullOrWhiteSpace(workOrderId))
            throw new ArgumentException("Work order ID is required", nameof(workOrderId));

        var job = _jobs.FirstOrDefault(j => j.WorkOrderId == workOrderId);
        if (job == null)
            throw new ArgumentException($"Work order {workOrderId} not found in queue", nameof(workOrderId));

        _jobs.Remove(job);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Get position of a job in the queue (1-based)
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <returns>Queue position or -1 if not found</returns>
    public int GetQueuePosition(string workOrderId)
    {
        var sortedJobs = Jobs.ToList();
        for (int i = 0; i < sortedJobs.Count; i++)
        {
            if (sortedJobs[i].WorkOrderId == workOrderId)
                return i + 1;
        }
        return -1;
    }

    /// <summary>
    /// Get count of jobs in queue
    /// </summary>
    public int JobCount => _jobs.Count;

    /// <summary>
    /// Get count of jobs not yet started
    /// </summary>
    public int PendingJobCount => _jobs.Count(j => j.OperatorId == null);

    /// <summary>
    /// Get count of jobs currently in progress
    /// </summary>
    public int InProgressJobCount => _jobs.Count(j => j.OperatorId != null);

    /// <summary>
    /// Check if queue is empty
    /// </summary>
    public bool IsEmpty => _jobs.Count == 0;

    /// <summary>
    /// Check if a specific work order is in the queue
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <returns>True if work order is in queue</returns>
    public bool ContainsJob(string workOrderId)
    {
        return _jobs.Any(j => j.WorkOrderId == workOrderId);
    }

    /// <summary>
    /// String representation of the queue
    /// </summary>
    public override string ToString()
    {
        return $"Job Queue for {LineId}: {JobCount} jobs ({PendingJobCount} pending, {InProgressJobCount} in progress)";
    }
}

/// <summary>
/// Represents a job in the queue
/// </summary>
/// <param name="WorkOrderId">Work order identifier</param>
/// <param name="ProductDescription">Product description</param>
/// <param name="Priority">Priority (1 = highest, 10 = lowest)</param>
/// <param name="QueuedAt">When job was added to queue</param>
/// <param name="OperatorId">Operator ID (null if not started)</param>
/// <param name="StartedAt">When job was started (null if not started)</param>
public record QueuedJob(
    string WorkOrderId,
    string ProductDescription,
    int Priority,
    DateTime QueuedAt,
    string? OperatorId = null,
    DateTime? StartedAt = null
)
{
    /// <summary>
    /// Check if job has been started
    /// </summary>
    public bool IsStarted => OperatorId != null;

    /// <summary>
    /// Get elapsed time since job was queued
    /// </summary>
    public TimeSpan TimeInQueue => DateTime.UtcNow - QueuedAt;

    /// <summary>
    /// Get elapsed time since job was started (null if not started)
    /// </summary>
    public TimeSpan? TimeInProgress => StartedAt.HasValue ? DateTime.UtcNow - StartedAt.Value : null;
}
