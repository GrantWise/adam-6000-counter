using Industrial.Adam.Oee.Application.DTOs;
using MediatR;

namespace Industrial.Adam.Oee.Application.Queries;

/// <summary>
/// Query to get a specific work order by ID
/// </summary>
public class GetWorkOrderQuery : IRequest<WorkOrderDto?>
{
    /// <summary>
    /// Work order identifier
    /// </summary>
    public string WorkOrderId { get; set; } = string.Empty;

    /// <summary>
    /// Constructor for creating query
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    public GetWorkOrderQuery(string workOrderId)
    {
        WorkOrderId = workOrderId;
    }

    /// <summary>
    /// Default constructor for serialization
    /// </summary>
    public GetWorkOrderQuery() { }
}
