using System.Data;
using Dapper;
using Industrial.Adam.Oee.Domain.Entities;
using Industrial.Adam.Oee.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Oee.Infrastructure.Repositories;

/// <summary>
/// TimescaleDB repository for simple job queue persistence
/// Provides full CRUD operations for the simple_job_queues table
/// Handles complex aggregate reconstruction with job collections
/// </summary>
public sealed class SimpleJobQueueRepository : ISimpleJobQueueRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<SimpleJobQueueRepository> _logger;

    /// <summary>
    /// Constructor for simple job queue repository
    /// </summary>
    /// <param name="connectionFactory">Database connection factory</param>
    /// <param name="logger">Logger instance</param>
    public SimpleJobQueueRepository(
        IDbConnectionFactory connectionFactory,
        ILogger<SimpleJobQueueRepository> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get job queue for a specific equipment line
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Job queue or null if not found</returns>
    public async Task<SimpleJobQueue?> GetByLineIdAsync(string lineId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lineId))
            throw new ArgumentException("Line ID cannot be null or empty", nameof(lineId));

        using var activity = ActivitySource.StartActivity("GetSimpleJobQueueByLineId");
        activity?.SetTag("lineId", lineId);

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            // Get all jobs for the line ordered by priority and queue time
            const string sql = @"
                SELECT 
                    id,
                    line_id,
                    work_order_id,
                    product_description,
                    priority,
                    queued_at,
                    operator_id,
                    started_at,
                    created_at,
                    updated_at
                FROM simple_job_queues 
                WHERE line_id = @lineId
                ORDER BY priority ASC, queued_at ASC";

            _logger.LogDebug("Retrieving job queue for line {LineId}", lineId);

            var jobData = await connection.QueryAsync<SimpleJobQueueRowData>(sql, new { lineId });
            var jobDataList = jobData.ToList();

            if (!jobDataList.Any())
            {
                _logger.LogDebug("No job queue found for line {LineId}", lineId);
                return null;
            }

            // Reconstruct the aggregate from the denormalized data
            var queue = ReconstructJobQueueFromRows(jobDataList);

            _logger.LogDebug("Retrieved job queue for line {LineId} with {JobCount} jobs", 
                lineId, queue.JobCount);

            return queue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve job queue for line {LineId}", lineId);
            throw;
        }
    }

    /// <summary>
    /// Get or create job queue for an equipment line
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Existing or newly created job queue</returns>
    public async Task<SimpleJobQueue> GetOrCreateByLineIdAsync(string lineId, CancellationToken cancellationToken = default)
    {
        var existingQueue = await GetByLineIdAsync(lineId, cancellationToken);
        if (existingQueue != null)
        {
            return existingQueue;
        }

        // Create new empty queue
        var newQueue = new SimpleJobQueue(lineId);
        _logger.LogInformation("Created new job queue for line {LineId}", lineId);
        return newQueue;
    }

    /// <summary>
    /// Save job queue changes
    /// </summary>
    /// <param name="jobQueue">Job queue to save</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task SaveAsync(SimpleJobQueue jobQueue, CancellationToken cancellationToken = default)
    {
        if (jobQueue == null)
            throw new ArgumentNullException(nameof(jobQueue));

        using var activity = ActivitySource.StartActivity("SaveSimpleJobQueue");
        activity?.SetTag("lineId", jobQueue.LineId);
        activity?.SetTag("jobCount", jobQueue.JobCount);

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            
            // Begin transaction for atomic updates
            using var transaction = connection.BeginTransaction();

            try
            {
                // Delete existing jobs for this line
                const string deleteSql = "DELETE FROM simple_job_queues WHERE line_id = @lineId";
                await connection.ExecuteAsync(deleteSql, new { lineId = jobQueue.LineId }, transaction);

                // Insert current jobs
                if (jobQueue.Jobs.Any())
                {
                    const string insertSql = @"
                        INSERT INTO simple_job_queues (
                            line_id,
                            work_order_id,
                            product_description,
                            priority,
                            queued_at,
                            operator_id,
                            started_at,
                            created_at,
                            updated_at
                        ) VALUES (
                            @LineId,
                            @WorkOrderId,
                            @ProductDescription,
                            @Priority,
                            @QueuedAt,
                            @OperatorId,
                            @StartedAt,
                            @CreatedAt,
                            @UpdatedAt
                        )";

                    var jobRows = jobQueue.Jobs.Select(job => new
                    {
                        LineId = jobQueue.LineId,
                        job.WorkOrderId,
                        job.ProductDescription,
                        job.Priority,
                        job.QueuedAt,
                        job.OperatorId,
                        job.StartedAt,
                        CreatedAt = jobQueue.CreatedAt,
                        UpdatedAt = jobQueue.UpdatedAt
                    });

                    await connection.ExecuteAsync(insertSql, jobRows, transaction);
                }

                transaction.Commit();

                _logger.LogInformation("Saved job queue for line {LineId} with {JobCount} jobs", 
                    jobQueue.LineId, jobQueue.JobCount);
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save job queue for line {LineId}", jobQueue.LineId);
            throw;
        }
    }

    /// <summary>
    /// Add a new job queue for an equipment line
    /// </summary>
    /// <param name="jobQueue">Job queue to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task AddAsync(SimpleJobQueue jobQueue, CancellationToken cancellationToken = default)
    {
        // For SimpleJobQueue, AddAsync is the same as SaveAsync since we reconstruct from rows
        await SaveAsync(jobQueue, cancellationToken);
    }

    /// <summary>
    /// Get all job queues with jobs
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of job queues that have jobs</returns>
    public async Task<List<SimpleJobQueue>> GetActiveQueuesAsync(CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("GetActiveJobQueues");

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = @"
                SELECT 
                    id,
                    line_id,
                    work_order_id,
                    product_description,
                    priority,
                    queued_at,
                    operator_id,
                    started_at,
                    created_at,
                    updated_at
                FROM simple_job_queues 
                ORDER BY line_id, priority ASC, queued_at ASC";

            _logger.LogDebug("Retrieving all active job queues");

            var allJobData = await connection.QueryAsync<SimpleJobQueueRowData>(sql);
            var jobsByLine = allJobData.GroupBy(job => job.LineId).ToList();

            var activeQueues = new List<SimpleJobQueue>();

            foreach (var lineGroup in jobsByLine)
            {
                var queue = ReconstructJobQueueFromRows(lineGroup.ToList());
                activeQueues.Add(queue);
            }

            _logger.LogInformation("Retrieved {QueueCount} active job queues", activeQueues.Count);

            return activeQueues;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve active job queues");
            throw;
        }
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
            throw new ArgumentException("Work order ID cannot be null or empty", nameof(workOrderId));

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = "SELECT EXISTS(SELECT 1 FROM simple_job_queues WHERE work_order_id = @workOrderId)";
            
            var exists = await connection.QuerySingleAsync<bool>(sql, new { workOrderId });

            _logger.LogDebug("Work order {WorkOrderId} queued status: {IsQueued}", workOrderId, exists);

            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if work order {WorkOrderId} is queued", workOrderId);
            throw;
        }
    }

    /// <summary>
    /// Remove a specific job from all queues
    /// </summary>
    /// <param name="workOrderId">Work order identifier to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if job was found and removed</returns>
    public async Task<bool> RemoveJobFromAllQueuesAsync(string workOrderId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workOrderId))
            throw new ArgumentException("Work order ID cannot be null or empty", nameof(workOrderId));

        using var activity = ActivitySource.StartActivity("RemoveJobFromAllQueues");
        activity?.SetTag("workOrderId", workOrderId);

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string sql = "DELETE FROM simple_job_queues WHERE work_order_id = @workOrderId";

            _logger.LogDebug("Removing work order {WorkOrderId} from all queues", workOrderId);

            var rowsAffected = await connection.ExecuteAsync(sql, new { workOrderId });
            var wasRemoved = rowsAffected > 0;

            if (wasRemoved)
            {
                _logger.LogInformation("Removed work order {WorkOrderId} from {QueueCount} queue(s)", 
                    workOrderId, rowsAffected);
            }
            else
            {
                _logger.LogDebug("Work order {WorkOrderId} not found in any queue", workOrderId);
            }

            return wasRemoved;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove work order {WorkOrderId} from queues", workOrderId);
            throw;
        }
    }

    /// <summary>
    /// Reconstruct SimpleJobQueue aggregate from database rows
    /// </summary>
    /// <param name="jobRows">Database rows for a single line</param>
    /// <returns>Reconstructed SimpleJobQueue</returns>
    private static SimpleJobQueue ReconstructJobQueueFromRows(List<SimpleJobQueueRowData> jobRows)
    {
        if (!jobRows.Any())
            throw new ArgumentException("Cannot reconstruct queue from empty job rows", nameof(jobRows));

        var firstRow = jobRows.First();
        var queue = new SimpleJobQueue(firstRow.LineId);

        // Set the creation/update times from the first row (they should all be the same)
        // Note: We need to use reflection or create a constructor that accepts these times
        // For now, we'll manually add jobs and accept that timestamps may not perfectly match
        
        foreach (var row in jobRows)
        {
            // Add the job to the queue
            queue.AddJob(row.WorkOrderId, row.ProductDescription, row.Priority);
            
            // If the job was started, mark it as started
            if (row.OperatorId != null)
            {
                queue.StartJob(row.WorkOrderId, row.OperatorId);
            }
        }

        return queue;
    }

    private static readonly System.Diagnostics.ActivitySource ActivitySource = new("Industrial.Adam.Oee.Infrastructure");

    /// <summary>
    /// Data structure for mapping simple job queue database rows
    /// </summary>
    private sealed record SimpleJobQueueRowData(
        int Id,
        string LineId,
        string WorkOrderId,
        string ProductDescription,
        int Priority,
        DateTime QueuedAt,
        string? OperatorId,
        DateTime? StartedAt,
        DateTime CreatedAt,
        DateTime UpdatedAt
    );
}