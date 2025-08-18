using Industrial.Adam.Oee.Application.DTOs;
using MediatR;

namespace Industrial.Adam.Oee.Application.Queries;

/// <summary>
/// Query to get active work order for a device
/// </summary>
public class GetActiveWorkOrderQuery : IRequest<WorkOrderDto?>
{
    /// <summary>
    /// Device/resource identifier
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Constructor for creating query
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    public GetActiveWorkOrderQuery(string deviceId)
    {
        DeviceId = deviceId;
    }

    /// <summary>
    /// Default constructor for serialization
    /// </summary>
    public GetActiveWorkOrderQuery() { }
}
