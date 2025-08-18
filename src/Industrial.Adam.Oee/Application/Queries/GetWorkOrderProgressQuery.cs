using Industrial.Adam.Oee.Application.DTOs;
using MediatR;

namespace Industrial.Adam.Oee.Application.Queries;

/// <summary>
/// Query to get work order progress metrics
/// </summary>
public class GetWorkOrderProgressQuery : IRequest<WorkOrderProgressDto?>
{
    /// <summary>
    /// Work order identifier
    /// </summary>
    public string WorkOrderId { get; set; } = string.Empty;

    /// <summary>
    /// Constructor for creating query
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    public GetWorkOrderProgressQuery(string workOrderId)
    {
        WorkOrderId = workOrderId;
    }

    /// <summary>
    /// Default constructor for serialization
    /// </summary>
    public GetWorkOrderProgressQuery() { }
}
