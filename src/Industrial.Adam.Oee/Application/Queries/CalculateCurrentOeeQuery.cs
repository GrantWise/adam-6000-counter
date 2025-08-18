using Industrial.Adam.Oee.Application.DTOs;
using MediatR;

namespace Industrial.Adam.Oee.Application.Queries;

/// <summary>
/// Query to calculate current OEE metrics for a device
/// </summary>
public class CalculateCurrentOeeQuery : IRequest<OeeCalculationDto>
{
    /// <summary>
    /// Device/resource identifier
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Optional start time for calculation period (defaults to current job start)
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// Optional end time for calculation period (defaults to current time)
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Constructor for creating query
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="startTime">Optional start time</param>
    /// <param name="endTime">Optional end time</param>
    public CalculateCurrentOeeQuery(string deviceId, DateTime? startTime = null, DateTime? endTime = null)
    {
        DeviceId = deviceId;
        StartTime = startTime;
        EndTime = endTime;
    }

    /// <summary>
    /// Default constructor for serialization
    /// </summary>
    public CalculateCurrentOeeQuery() { }
}
