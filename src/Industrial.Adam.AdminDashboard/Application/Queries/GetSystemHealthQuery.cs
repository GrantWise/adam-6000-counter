using Industrial.Adam.AdminDashboard.Application.DTOs;
using MediatR;

namespace Industrial.Adam.AdminDashboard.Application.Queries;

/// <summary>
/// Query to get system health status for all services
/// </summary>
public record GetSystemHealthQuery : IRequest<IEnumerable<ServiceHealthDto>>;