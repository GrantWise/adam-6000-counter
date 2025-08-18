using Industrial.Adam.Oee.Domain.Services;

namespace Industrial.Adam.Oee.Domain.Interfaces;

/// <summary>
/// Interface for job sequencing business rules enforcement
/// </summary>
public interface IJobSequencingService
{
    /// <summary>
    /// Validate that a new job can be started on the specified equipment line
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="newWorkOrderId">Work order identifier being started</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    public Task<JobSequencingValidationResult> ValidateJobStartAsync(
        string lineId,
        string newWorkOrderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate that a job can be ended
    /// </summary>
    /// <param name="workOrderId">Work order identifier being ended</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    public Task<JobSequencingValidationResult> ValidateJobEndAsync(
        string workOrderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate job completion with quantity checks
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="minimumCompletionPercentage">Minimum required completion percentage</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Completion validation result</returns>
    public Task<JobCompletionValidationResult> ValidateJobCompletionAsync(
        string workOrderId,
        decimal minimumCompletionPercentage = 85m,
        CancellationToken cancellationToken = default);
}
