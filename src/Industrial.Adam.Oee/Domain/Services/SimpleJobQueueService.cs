using Industrial.Adam.Oee.Domain.Entities;
using Industrial.Adam.Oee.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Oee.Domain.Services;

/// <summary>
/// Domain service for simple job queue management
/// Implements basic FIFO/priority queue operations for work order sequencing
/// </summary>
public class SimpleJobQueueService : ISimpleJobQueueService
{
    private readonly ISimpleJobQueueRepository _queueRepository;
    private readonly IWorkOrderRepository _workOrderRepository;
    private readonly ILogger<SimpleJobQueueService> _logger;

    /// <summary>
    /// Constructor for SimpleJobQueueService
    /// </summary>
    /// <param name="queueRepository">Repository for job queue operations</param>
    /// <param name="workOrderRepository">Repository for work order validation</param>
    /// <param name="logger">Logger instance</param>
    public SimpleJobQueueService(
        ISimpleJobQueueRepository queueRepository,
        IWorkOrderRepository workOrderRepository,
        ILogger<SimpleJobQueueService> logger)
    {
        _queueRepository = queueRepository ?? throw new ArgumentNullException(nameof(queueRepository));
        _workOrderRepository = workOrderRepository ?? throw new ArgumentNullException(nameof(workOrderRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get the next job to process for a specific line
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Next job or null if no jobs available</returns>
    public async Task<QueuedJob?> GetNextJobAsync(string lineId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting next job for line {LineId}", lineId);

        var queue = await _queueRepository.GetByLineIdAsync(lineId, cancellationToken);
        if (queue == null)
        {
            _logger.LogDebug("No queue found for line {LineId}", lineId);
            return null;
        }

        var nextJob = queue.GetNextJob();
        _logger.LogDebug("Next job for line {LineId}: {WorkOrderId}",
            lineId, nextJob?.WorkOrderId ?? "None");

        return nextJob;
    }

    /// <summary>
    /// Add a job to the queue for a specific line
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="productDescription">Product description</param>
    /// <param name="priority">Job priority (1=highest, 10=lowest, default=5)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Queue position of the added job</returns>
    public async Task<int> AddJobToQueueAsync(string lineId, string workOrderId, string productDescription, int priority = 5, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lineId))
            throw new ArgumentException("Line ID is required", nameof(lineId));

        if (string.IsNullOrWhiteSpace(workOrderId))
            throw new ArgumentException("Work order ID is required", nameof(workOrderId));

        if (string.IsNullOrWhiteSpace(productDescription))
            throw new ArgumentException("Product description is required", nameof(productDescription));

        _logger.LogInformation("Adding job {WorkOrderId} to queue for line {LineId} with priority {Priority}",
            workOrderId, lineId, priority);

        // Validate work order exists
        var workOrder = await _workOrderRepository.GetByIdAsync(workOrderId, cancellationToken);
        if (workOrder == null)
            throw new InvalidOperationException($"Work order {workOrderId} not found");

        // Check if already queued elsewhere
        if (await _queueRepository.IsWorkOrderQueuedAsync(workOrderId, cancellationToken))
            throw new InvalidOperationException($"Work order {workOrderId} is already in a queue");

        // Get or create queue
        var queue = await _queueRepository.GetOrCreateByLineIdAsync(lineId, cancellationToken);

        // Add job to queue
        queue.AddJob(workOrderId, productDescription, priority);

        // Save changes
        await _queueRepository.SaveAsync(queue, cancellationToken);

        var position = queue.GetQueuePosition(workOrderId);
        _logger.LogInformation("Job {WorkOrderId} added to queue for line {LineId} at position {Position}",
            workOrderId, lineId, position);

        return position;
    }

    /// <summary>
    /// Start a job by assigning an operator
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="operatorId">Operator identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task StartJobAsync(string lineId, string workOrderId, string operatorId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lineId))
            throw new ArgumentException("Line ID is required", nameof(lineId));

        if (string.IsNullOrWhiteSpace(workOrderId))
            throw new ArgumentException("Work order ID is required", nameof(workOrderId));

        if (string.IsNullOrWhiteSpace(operatorId))
            throw new ArgumentException("Operator ID is required", nameof(operatorId));

        _logger.LogInformation("Starting job {WorkOrderId} on line {LineId} for operator {OperatorId}",
            workOrderId, lineId, operatorId);

        var queue = await _queueRepository.GetByLineIdAsync(lineId, cancellationToken);
        if (queue == null)
            throw new InvalidOperationException($"No queue found for line {lineId}");

        queue.StartJob(workOrderId, operatorId);
        await _queueRepository.SaveAsync(queue, cancellationToken);

        _logger.LogInformation("Job {WorkOrderId} started on line {LineId}", workOrderId, lineId);
    }

    /// <summary>
    /// Complete and remove a job from the queue
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task CompleteJobAsync(string lineId, string workOrderId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lineId))
            throw new ArgumentException("Line ID is required", nameof(lineId));

        if (string.IsNullOrWhiteSpace(workOrderId))
            throw new ArgumentException("Work order ID is required", nameof(workOrderId));

        _logger.LogInformation("Completing job {WorkOrderId} on line {LineId}", workOrderId, lineId);

        var queue = await _queueRepository.GetByLineIdAsync(lineId, cancellationToken);
        if (queue == null)
            throw new InvalidOperationException($"No queue found for line {lineId}");

        queue.CompleteJob(workOrderId);
        await _queueRepository.SaveAsync(queue, cancellationToken);

        _logger.LogInformation("Job {WorkOrderId} completed and removed from queue for line {LineId}",
            workOrderId, lineId);
    }

    /// <summary>
    /// Remove a job from the queue without completion
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task RemoveJobAsync(string lineId, string workOrderId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lineId))
            throw new ArgumentException("Line ID is required", nameof(lineId));

        if (string.IsNullOrWhiteSpace(workOrderId))
            throw new ArgumentException("Work order ID is required", nameof(workOrderId));

        _logger.LogInformation("Removing job {WorkOrderId} from queue for line {LineId}", workOrderId, lineId);

        var queue = await _queueRepository.GetByLineIdAsync(lineId, cancellationToken);
        if (queue == null)
            throw new InvalidOperationException($"No queue found for line {lineId}");

        queue.RemoveJob(workOrderId);
        await _queueRepository.SaveAsync(queue, cancellationToken);

        _logger.LogInformation("Job {WorkOrderId} removed from queue for line {LineId}", workOrderId, lineId);
    }

    /// <summary>
    /// Get current queue status for a line
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of jobs in priority order</returns>
    public async Task<List<QueuedJob>> GetQueueStatusAsync(string lineId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lineId))
            throw new ArgumentException("Line ID is required", nameof(lineId));

        _logger.LogDebug("Getting queue status for line {LineId}", lineId);

        var queue = await _queueRepository.GetByLineIdAsync(lineId, cancellationToken);
        if (queue == null)
        {
            _logger.LogDebug("No queue found for line {LineId}", lineId);
            return new List<QueuedJob>();
        }

        return queue.Jobs.ToList();
    }

    /// <summary>
    /// Get position of a specific job in the queue
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Queue position (1-based) or -1 if not found</returns>
    public async Task<int> GetQueuePositionAsync(string lineId, string workOrderId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lineId))
            throw new ArgumentException("Line ID is required", nameof(lineId));

        if (string.IsNullOrWhiteSpace(workOrderId))
            throw new ArgumentException("Work order ID is required", nameof(workOrderId));

        var queue = await _queueRepository.GetByLineIdAsync(lineId, cancellationToken);
        if (queue == null)
            return -1;

        return queue.GetQueuePosition(workOrderId);
    }

    /// <summary>
    /// Check if a work order is already in any queue
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if work order is already queued</returns>
    public async Task<bool> IsWorkOrderQueuedAsync(string workOrderId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workOrderId))
            throw new ArgumentException("Work order ID is required", nameof(workOrderId));

        return await _queueRepository.IsWorkOrderQueuedAsync(workOrderId, cancellationToken);
    }

    /// <summary>
    /// Get summary of all active queues
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of queue summaries</returns>
    public async Task<List<JobQueueSummary>> GetActiveQueueSummariesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting summaries for all active queues");

        var queues = await _queueRepository.GetActiveQueuesAsync(cancellationToken);
        var summaries = new List<JobQueueSummary>();

        foreach (var queue in queues)
        {
            var nextJob = queue.GetNextJob();
            var summary = new JobQueueSummary(
                queue.LineId,
                queue.JobCount,
                queue.PendingJobCount,
                queue.InProgressJobCount,
                nextJob
            );
            summaries.Add(summary);
        }

        _logger.LogDebug("Found {Count} active queues", summaries.Count);
        return summaries;
    }
}
