using Industrial.Adam.Oee.Domain.Interfaces;
using Industrial.Adam.Oee.WebApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace Industrial.Adam.Oee.WebApi.Controllers;

/// <summary>
/// Controller for simple job queue management
/// Provides basic job sequencing endpoints as part of OEE simplification
/// </summary>
[ApiController]
[Route("api/jobs")]
[Produces("application/json")]
public class SimpleJobQueueController : ControllerBase
{
    private readonly ISimpleJobQueueService _jobQueueService;
    private readonly ILogger<SimpleJobQueueController> _logger;

    /// <summary>
    /// Constructor for SimpleJobQueueController
    /// </summary>
    /// <param name="jobQueueService">Simple job queue service</param>
    /// <param name="logger">Logger instance</param>
    public SimpleJobQueueController(
        ISimpleJobQueueService jobQueueService,
        ILogger<SimpleJobQueueController> logger)
    {
        _jobQueueService = jobQueueService ?? throw new ArgumentNullException(nameof(jobQueueService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get job queue status for a specific equipment line
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current job queue status</returns>
    /// <response code="200">Returns the job queue status</response>
    /// <response code="404">Equipment line not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("queue/{lineId}")]
    [ProducesResponseType(typeof(ApiResponse<JobQueueStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<JobQueueStatusDto>>> GetJobQueue(
        string lineId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting job queue status for line {LineId}", lineId);

            var jobs = await _jobQueueService.GetQueueStatusAsync(lineId, cancellationToken);
            var nextJob = await _jobQueueService.GetNextJobAsync(lineId, cancellationToken);

            var status = new JobQueueStatusDto
            {
                LineId = lineId,
                TotalJobs = jobs.Count,
                PendingJobs = jobs.Count(j => !j.IsStarted),
                InProgressJobs = jobs.Count(j => j.IsStarted),
                NextJob = nextJob,
                Jobs = jobs,
                LastUpdated = DateTime.UtcNow
            };

            return Ok(ApiResponse.WithData(status));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get job queue status for line {LineId}", lineId);
            return StatusCode(500, ApiResponse.Failed($"Failed to get job queue status: {ex.Message}"));
        }
    }

    /// <summary>
    /// Add a job to the queue for a specific equipment line
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="request">Add job request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Queue position of the added job</returns>
    /// <response code="200">Job added successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="409">Job already exists in queue</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("queue/{lineId}")]
    [ProducesResponseType(typeof(ApiResponse<AddJobResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<AddJobResponseDto>>> AddJobToQueue(
        string lineId,
        [FromBody] AddJobRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.WorkOrderId))
                return BadRequest(ApiResponse.Failed("Work order ID is required"));

            if (string.IsNullOrWhiteSpace(request.ProductDescription))
                return BadRequest(ApiResponse.Failed("Product description is required"));

            _logger.LogInformation("Adding job {WorkOrderId} to queue for line {LineId}",
                request.WorkOrderId, lineId);

            var position = await _jobQueueService.AddJobToQueueAsync(
                lineId,
                request.WorkOrderId,
                request.ProductDescription,
                request.Priority ?? 5,
                cancellationToken);

            var response = new AddJobResponseDto
            {
                WorkOrderId = request.WorkOrderId,
                LineId = lineId,
                QueuePosition = position,
                AddedAt = DateTime.UtcNow
            };

            return Ok(ApiResponse.WithData(response));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already in a queue"))
        {
            return Conflict(ApiResponse.Failed(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add job {WorkOrderId} to queue for line {LineId}",
                request.WorkOrderId, lineId);
            return StatusCode(500, ApiResponse.Failed($"Failed to add job to queue: {ex.Message}"));
        }
    }

    /// <summary>
    /// Start the next job in queue for a specific equipment line
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="request">Start job request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Started job information</returns>
    /// <response code="200">Job started successfully</response>
    /// <response code="400">Invalid request data or no jobs available</response>
    /// <response code="500">Internal server error</response>
    [HttpPut("queue/{lineId}/start")]
    [ProducesResponseType(typeof(ApiResponse<StartJobResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<StartJobResponseDto>>> StartNextJob(
        string lineId,
        [FromBody] StartJobRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.OperatorId))
                return BadRequest(ApiResponse.Failed("Operator ID is required"));

            _logger.LogInformation("Starting next job for line {LineId} with operator {OperatorId}",
                lineId, request.OperatorId);

            var nextJob = await _jobQueueService.GetNextJobAsync(lineId, cancellationToken);
            if (nextJob == null)
                return BadRequest(ApiResponse.Failed("No jobs available to start"));

            await _jobQueueService.StartJobAsync(lineId, nextJob.WorkOrderId, request.OperatorId, cancellationToken);

            var response = new StartJobResponseDto
            {
                WorkOrderId = nextJob.WorkOrderId,
                LineId = lineId,
                OperatorId = request.OperatorId,
                ProductDescription = nextJob.ProductDescription,
                StartedAt = DateTime.UtcNow
            };

            return Ok(ApiResponse.WithData(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start next job for line {LineId}", lineId);
            return StatusCode(500, ApiResponse.Failed($"Failed to start job: {ex.Message}"));
        }
    }

    /// <summary>
    /// Complete a specific job and remove it from the queue
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="workOrderId">Work order identifier to complete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Completion confirmation</returns>
    /// <response code="200">Job completed successfully</response>
    /// <response code="404">Job not found in queue</response>
    /// <response code="500">Internal server error</response>
    [HttpPut("queue/{lineId}/complete/{workOrderId}")]
    [ProducesResponseType(typeof(ApiResponse<CompleteJobResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<CompleteJobResponseDto>>> CompleteJob(
        string lineId,
        string workOrderId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Completing job {WorkOrderId} for line {LineId}", workOrderId, lineId);

            await _jobQueueService.CompleteJobAsync(lineId, workOrderId, cancellationToken);

            var response = new CompleteJobResponseDto
            {
                WorkOrderId = workOrderId,
                LineId = lineId,
                CompletedAt = DateTime.UtcNow
            };

            return Ok(ApiResponse.WithData(response));
        }
        catch (ArgumentException ex)
        {
            return NotFound(ApiResponse.Failed(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete job {WorkOrderId} for line {LineId}", workOrderId, lineId);
            return StatusCode(500, ApiResponse.Failed($"Failed to complete job: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get summary of all active job queues
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Summary of all active queues</returns>
    /// <response code="200">Returns queue summaries</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("queues/summary")]
    [ProducesResponseType(typeof(ApiResponse<List<JobQueueSummaryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<JobQueueSummaryDto>>>> GetActiveQueueSummaries(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting summaries for all active job queues");

            var summaries = await _jobQueueService.GetActiveQueueSummariesAsync(cancellationToken);
            var dtos = summaries.Select(s => new JobQueueSummaryDto
            {
                LineId = s.LineId,
                TotalJobs = s.TotalJobs,
                PendingJobs = s.PendingJobs,
                InProgressJobs = s.InProgressJobs,
                NextJob = s.NextJob,
                LastUpdated = DateTime.UtcNow
            }).ToList();

            return Ok(ApiResponse.WithData(dtos));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active queue summaries");
            return StatusCode(500, ApiResponse.Failed($"Failed to get queue summaries: {ex.Message}"));
        }
    }
}
