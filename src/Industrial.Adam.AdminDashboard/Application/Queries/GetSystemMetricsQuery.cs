using Industrial.Adam.AdminDashboard.Application.DTOs;
using MediatR;

namespace Industrial.Adam.AdminDashboard.Application.Queries;

/// <summary>
/// Query to get system performance metrics
/// </summary>
public record GetSystemMetricsQuery : IRequest<SystemMetricsDto>;