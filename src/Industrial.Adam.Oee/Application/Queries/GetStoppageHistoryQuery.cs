using Industrial.Adam.Oee.Application.DTOs;
using MediatR;

namespace Industrial.Adam.Oee.Application.Queries;

/// <summary>
/// Query to get historical stoppage data for a device
/// </summary>
public class GetStoppageHistoryQuery : IRequest<IEnumerable<StoppageInfoDto>>
{
    /// <summary>
    /// Device/resource identifier
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Number of hours to look back from current time
    /// </summary>
    public int Period { get; set; } = 24;

    /// <summary>
    /// Optional start time for custom date range
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// Optional end time for custom date range
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Minimum stoppage duration in minutes to be included
    /// </summary>
    public int MinimumStoppageMinutes { get; set; } = 5;

    /// <summary>
    /// Constructor for creating query
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="period">Hours to look back</param>
    /// <param name="minimumStoppageMinutes">Minimum stoppage duration</param>
    public GetStoppageHistoryQuery(string deviceId, int period = 24, int minimumStoppageMinutes = 5)
    {
        DeviceId = deviceId;
        Period = period;
        MinimumStoppageMinutes = minimumStoppageMinutes;
    }

    /// <summary>
    /// Default constructor for serialization
    /// </summary>
    public GetStoppageHistoryQuery() { }
}
