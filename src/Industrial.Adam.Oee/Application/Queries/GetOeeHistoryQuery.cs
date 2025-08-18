using Industrial.Adam.Oee.Application.DTOs;
using MediatR;

namespace Industrial.Adam.Oee.Application.Queries;

/// <summary>
/// Query to get historical OEE data for a device
/// </summary>
public class GetOeeHistoryQuery : IRequest<IEnumerable<OeeCalculationDto>>
{
    /// <summary>
    /// Device/resource identifier
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Start time for history period
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// End time for history period
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Maximum number of records to return (optional)
    /// </summary>
    public int? MaxRecords { get; set; }

    /// <summary>
    /// Constructor for creating query
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="startTime">Start time for history</param>
    /// <param name="endTime">End time for history</param>
    /// <param name="maxRecords">Maximum records to return</param>
    public GetOeeHistoryQuery(string deviceId, DateTime startTime, DateTime endTime, int? maxRecords = null)
    {
        DeviceId = deviceId;
        StartTime = startTime;
        EndTime = endTime;
        MaxRecords = maxRecords;
    }

    /// <summary>
    /// Default constructor for serialization
    /// </summary>
    public GetOeeHistoryQuery() { }
}
