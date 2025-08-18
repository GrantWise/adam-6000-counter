using Industrial.Adam.Oee.Application.DTOs;
using MediatR;

namespace Industrial.Adam.Oee.Application.Queries;

/// <summary>
/// Query to get current stoppage information for a device
/// </summary>
public class GetCurrentStoppageQuery : IRequest<StoppageInfoDto?>
{
    /// <summary>
    /// Device/resource identifier
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Minimum stoppage duration in minutes to be considered
    /// </summary>
    public int MinimumStoppageMinutes { get; set; } = 5;

    /// <summary>
    /// Constructor for creating query
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="minimumStoppageMinutes">Minimum stoppage duration</param>
    public GetCurrentStoppageQuery(string deviceId, int minimumStoppageMinutes = 5)
    {
        DeviceId = deviceId;
        MinimumStoppageMinutes = minimumStoppageMinutes;
    }

    /// <summary>
    /// Default constructor for serialization
    /// </summary>
    public GetCurrentStoppageQuery() { }
}
