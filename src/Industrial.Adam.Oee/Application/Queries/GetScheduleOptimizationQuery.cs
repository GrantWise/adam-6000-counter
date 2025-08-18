using Industrial.Adam.Oee.Domain.Entities;
using Industrial.Adam.Oee.Domain.Services;
using MediatR;

namespace Industrial.Adam.Oee.Application.Queries;

/// <summary>
/// Query to get schedule optimization recommendations
/// </summary>
/// <param name="WorkOrderIds">Work order identifiers to schedule</param>
/// <param name="EquipmentLineIds">Equipment line identifiers to consider</param>
/// <param name="OptimizationGoal">Primary optimization goal</param>
/// <param name="TimeHorizonStart">Start time for scheduling</param>
/// <param name="TimeHorizonEnd">End time for scheduling</param>
/// <param name="IncludeExistingSchedules">Whether to include existing schedules</param>
public record GetScheduleOptimizationQuery(
    List<string> WorkOrderIds,
    List<string> EquipmentLineIds,
    OptimizationGoal OptimizationGoal,
    DateTime TimeHorizonStart,
    DateTime TimeHorizonEnd,
    bool IncludeExistingSchedules = true
) : IRequest<ScheduleOptimizationQueryResult>;

/// <summary>
/// Result containing schedule optimization recommendations
/// </summary>
/// <param name="IsSuccess">Whether query was successful</param>
/// <param name="OptimizationResult">Schedule optimization results</param>
/// <param name="Recommendations">Individual schedule recommendations</param>
/// <param name="ResourceUtilization">Resource utilization analysis</param>
/// <param name="AlternativeScenarios">Alternative scheduling scenarios</param>
/// <param name="ErrorMessage">Error message if query failed</param>
public record ScheduleOptimizationQueryResult(
    bool IsSuccess,
    ScheduleOptimizationResult? OptimizationResult,
    List<ScheduleRecommendation> Recommendations,
    ResourceUtilizationAnalysis? ResourceUtilization,
    List<SchedulingScenario> AlternativeScenarios,
    string? ErrorMessage
);

/// <summary>
/// Alternative scheduling scenario
/// </summary>
/// <param name="ScenarioName">Scenario name</param>
/// <param name="OptimizationGoal">Optimization goal for scenario</param>
/// <param name="Schedules">Schedules in scenario</param>
/// <param name="Metrics">Scenario performance metrics</param>
/// <param name="Description">Scenario description</param>
public record SchedulingScenario(
    string ScenarioName,
    OptimizationGoal OptimizationGoal,
    List<JobSchedule> Schedules,
    ScheduleMetrics Metrics,
    string Description
);
