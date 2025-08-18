using Industrial.Adam.Oee.Domain.Services;

namespace Industrial.Adam.Oee.Domain.Interfaces;

/// <summary>
/// Interface for equipment line business operations
/// </summary>
public interface IEquipmentLineService
{
    /// <summary>
    /// Validate that a work order can be assigned to the specified equipment line
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Equipment validation result</returns>
    public Task<EquipmentValidationResult> ValidateWorkOrderEquipmentAsync(
        string workOrderId,
        string lineId,
        CancellationToken cancellationToken = default);
}
