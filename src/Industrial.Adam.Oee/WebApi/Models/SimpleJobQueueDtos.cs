using Industrial.Adam.Oee.Domain.Entities;

namespace Industrial.Adam.Oee.WebApi.Models;

/// <summary>
/// DTO for job queue status response
/// </summary>
public class JobQueueStatusDto
{
    /// <summary>
    /// Equipment line identifier
    /// </summary>
    public string LineId { get; set; } = string.Empty;

    /// <summary>
    /// Total number of jobs in queue
    /// </summary>
    public int TotalJobs { get; set; }

    /// <summary>
    /// Number of jobs not yet started
    /// </summary>
    public int PendingJobs { get; set; }

    /// <summary>
    /// Number of jobs currently in progress
    /// </summary>
    public int InProgressJobs { get; set; }

    /// <summary>
    /// Next job to be processed (if any)
    /// </summary>
    public QueuedJob? NextJob { get; set; }

    /// <summary>
    /// All jobs in the queue (ordered by priority)
    /// </summary>
    public List<QueuedJob> Jobs { get; set; } = new();

    /// <summary>
    /// When this status was last updated
    /// </summary>
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// DTO for job queue summary
/// </summary>
public class JobQueueSummaryDto
{
    /// <summary>
    /// Equipment line identifier
    /// </summary>
    public string LineId { get; set; } = string.Empty;

    /// <summary>
    /// Total number of jobs in queue
    /// </summary>
    public int TotalJobs { get; set; }

    /// <summary>
    /// Number of jobs not yet started
    /// </summary>
    public int PendingJobs { get; set; }

    /// <summary>
    /// Number of jobs currently in progress
    /// </summary>
    public int InProgressJobs { get; set; }

    /// <summary>
    /// Next job to be processed (if any)
    /// </summary>
    public QueuedJob? NextJob { get; set; }

    /// <summary>
    /// When this summary was last updated
    /// </summary>
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Request DTO for adding a job to queue
/// </summary>
public class AddJobRequestDto
{
    /// <summary>
    /// Work order identifier
    /// </summary>
    public string WorkOrderId { get; set; } = string.Empty;

    /// <summary>
    /// Product description for display
    /// </summary>
    public string ProductDescription { get; set; } = string.Empty;

    /// <summary>
    /// Job priority (1=highest, 10=lowest, default=5)
    /// </summary>
    public int? Priority { get; set; } = 5;
}

/// <summary>
/// Response DTO for adding a job to queue
/// </summary>
public class AddJobResponseDto
{
    /// <summary>
    /// Work order identifier
    /// </summary>
    public string WorkOrderId { get; set; } = string.Empty;

    /// <summary>
    /// Equipment line identifier
    /// </summary>
    public string LineId { get; set; } = string.Empty;

    /// <summary>
    /// Position in queue (1-based)
    /// </summary>
    public int QueuePosition { get; set; }

    /// <summary>
    /// When the job was added to queue
    /// </summary>
    public DateTime AddedAt { get; set; }
}

/// <summary>
/// Request DTO for starting a job
/// </summary>
public class StartJobRequestDto
{
    /// <summary>
    /// Operator identifier who will run the job
    /// </summary>
    public string OperatorId { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for starting a job
/// </summary>
public class StartJobResponseDto
{
    /// <summary>
    /// Work order identifier
    /// </summary>
    public string WorkOrderId { get; set; } = string.Empty;

    /// <summary>
    /// Equipment line identifier
    /// </summary>
    public string LineId { get; set; } = string.Empty;

    /// <summary>
    /// Operator identifier
    /// </summary>
    public string OperatorId { get; set; } = string.Empty;

    /// <summary>
    /// Product description
    /// </summary>
    public string ProductDescription { get; set; } = string.Empty;

    /// <summary>
    /// When the job was started
    /// </summary>
    public DateTime StartedAt { get; set; }
}

/// <summary>
/// Response DTO for completing a job
/// </summary>
public class CompleteJobResponseDto
{
    /// <summary>
    /// Work order identifier
    /// </summary>
    public string WorkOrderId { get; set; } = string.Empty;

    /// <summary>
    /// Equipment line identifier
    /// </summary>
    public string LineId { get; set; } = string.Empty;

    /// <summary>
    /// When the job was completed
    /// </summary>
    public DateTime CompletedAt { get; set; }
}
