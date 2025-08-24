using MediatR;

namespace Industrial.Adam.AdminDashboard.Application.Queries;

/// <summary>
/// Query to get database health information
/// </summary>
public record GetDatabaseHealthQuery : IRequest<object>;