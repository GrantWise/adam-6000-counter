using Industrial.Adam.AdminDashboard.Application.DTOs;
using MediatR;

namespace Industrial.Adam.AdminDashboard.Application.Queries;

/// <summary>
/// Query to get active system alerts
/// </summary>
public record GetActiveAlertsQuery : IRequest<IEnumerable<SystemAlertDto>>;