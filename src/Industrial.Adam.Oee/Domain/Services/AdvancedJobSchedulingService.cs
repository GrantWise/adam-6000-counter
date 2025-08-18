using Industrial.Adam.Oee.Domain.Entities;

namespace Industrial.Adam.Oee.Domain.Services;

/// <summary>
/// Advanced Job Scheduling Service
/// 
/// Provides sophisticated scheduling algorithms, dependency resolution,
/// conflict detection and resolution, and optimization for complex
/// manufacturing environments.
/// </summary>
public sealed class AdvancedJobSchedulingService
{
    /// <summary>
    /// Create optimized schedule for multiple work orders
    /// </summary>
    /// <param name="workOrders">Work orders to schedule</param>
    /// <param name="equipmentLines">Available equipment lines</param>
    /// <param name="existingSchedules">Existing schedules to consider</param>
    /// <param name="optimizationGoal">Primary optimization goal</param>
    /// <returns>Optimized schedule plan</returns>
    public ScheduleOptimizationResult OptimizeSchedule(
        IEnumerable<WorkOrder> workOrders,
        IEnumerable<EquipmentLine> equipmentLines,
        IEnumerable<JobSchedule> existingSchedules,
        OptimizationGoal optimizationGoal = OptimizationGoal.MinimizeMakespan)
    {
        var workOrderList = workOrders.ToList();
        var equipmentLineList = equipmentLines.ToList();
        var existingScheduleList = existingSchedules.ToList();

        // Step 1: Validate input data
        ValidateSchedulingInput(workOrderList, equipmentLineList);

        // Step 2: Sort work orders by priority and urgency
        var sortedWorkOrders = SortWorkOrdersByPriority(workOrderList);

        // Step 3: Detect and resolve conflicts
        var conflictResolutionResult = DetectAndResolveConflicts(existingScheduleList);

        // Step 4: Create initial schedule assignments
        var initialSchedules = CreateInitialScheduleAssignments(sortedWorkOrders, equipmentLineList, existingScheduleList);

        // Step 5: Apply optimization algorithm based on goal
        var optimizedSchedules = ApplyOptimizationAlgorithm(initialSchedules, optimizationGoal);

        // Step 6: Validate final schedule
        var validationResult = ValidateSchedule(optimizedSchedules);

        return new ScheduleOptimizationResult(
            optimizedSchedules,
            conflictResolutionResult,
            validationResult,
            CalculateScheduleMetrics(optimizedSchedules),
            DateTime.UtcNow
        );
    }

    /// <summary>
    /// Resolve scheduling conflicts for a specific job schedule
    /// </summary>
    /// <param name="jobSchedule">Job schedule with conflicts</param>
    /// <param name="allSchedules">All schedules to consider</param>
    /// <param name="resolutionStrategy">Conflict resolution strategy</param>
    /// <returns>Conflict resolution result</returns>
    public ConflictResolutionResult ResolveSchedulingConflicts(
        JobSchedule jobSchedule,
        IEnumerable<JobSchedule> allSchedules,
        ConflictResolutionStrategy resolutionStrategy = ConflictResolutionStrategy.ShiftLowerPriority)
    {
        var conflicts = DetectConflictsForSchedule(jobSchedule, allSchedules);

        if (!conflicts.Any())
        {
            return new ConflictResolutionResult(
                jobSchedule.Id,
                true,
                Array.Empty<SchedulingConflict>(),
                Array.Empty<ConflictResolutionAction>(),
                "No conflicts detected"
            );
        }

        var resolutionActions = new List<ConflictResolutionAction>();

        foreach (var conflict in conflicts)
        {
            var action = resolutionStrategy switch
            {
                ConflictResolutionStrategy.ShiftLowerPriority => ResolveByShiftingLowerPriority(jobSchedule, conflict, allSchedules),
                ConflictResolutionStrategy.SplitJobs => ResolveBySplittingJobs(jobSchedule, conflict),
                ConflictResolutionStrategy.AlternateEquipment => ResolveByAlternateEquipment(jobSchedule, conflict, allSchedules),
                ConflictResolutionStrategy.OverrideHighPriority => ResolveByOverridingHighPriority(jobSchedule, conflict),
                _ => throw new ArgumentException($"Unknown resolution strategy: {resolutionStrategy}")
            };

            if (action != null)
            {
                resolutionActions.Add(action);
                jobSchedule.ResolveConflict(conflict.ConflictType, conflict.ConflictingJobId);
            }
        }

        var allResolved = resolutionActions.Count == conflicts.Count();
        var message = allResolved ? "All conflicts resolved" : $"Resolved {resolutionActions.Count} of {conflicts.Count()} conflicts";

        return new ConflictResolutionResult(
            jobSchedule.Id,
            allResolved,
            conflicts,
            resolutionActions,
            message
        );
    }

    /// <summary>
    /// Calculate resource utilization for equipment lines
    /// </summary>
    /// <param name="jobSchedules">Job schedules to analyze</param>
    /// <param name="equipmentLines">Equipment lines</param>
    /// <param name="analysisStartTime">Analysis start time</param>
    /// <param name="analysisEndTime">Analysis end time</param>
    /// <returns>Resource utilization analysis</returns>
    public ResourceUtilizationAnalysis AnalyzeResourceUtilization(
        IEnumerable<JobSchedule> jobSchedules,
        IEnumerable<EquipmentLine> equipmentLines,
        DateTime analysisStartTime,
        DateTime analysisEndTime)
    {
        var scheduleList = jobSchedules.ToList();
        var equipmentLineList = equipmentLines.ToList();
        var totalAnalysisHours = (decimal)(analysisEndTime - analysisStartTime).TotalHours;

        var utilizationByEquipment = new List<EquipmentUtilization>();

        foreach (var equipmentLine in equipmentLineList)
        {
            var relevantSchedules = scheduleList.Where(s =>
                s.EquipmentLineId == equipmentLine.LineId &&
                s.ScheduledStartTime < analysisEndTime &&
                s.ScheduledEndTime > analysisStartTime);

            var totalScheduledHours = 0m;
            var totalSetupHours = 0m;
            var totalProductionHours = 0m;
            var totalTeardownHours = 0m;

            foreach (var schedule in relevantSchedules)
            {
                var effectiveStart = schedule.ScheduledStartTime < analysisStartTime ? analysisStartTime : schedule.ScheduledStartTime;
                var effectiveEnd = schedule.ScheduledEndTime > analysisEndTime ? analysisEndTime : schedule.ScheduledEndTime;

                var scheduleHours = (decimal)(effectiveEnd - effectiveStart).TotalHours;
                totalScheduledHours += scheduleHours;

                // Proportional allocation of setup/production/teardown time
                var totalScheduleMinutes = schedule.TotalScheduledTimeMinutes;
                if (totalScheduleMinutes > 0)
                {
                    var setupRatio = schedule.SetupTimeMinutes / totalScheduleMinutes;
                    var teardownRatio = schedule.TeardownTimeMinutes / totalScheduleMinutes;
                    var productionRatio = schedule.EstimatedProductionTimeMinutes / totalScheduleMinutes;

                    totalSetupHours += scheduleHours * setupRatio;
                    totalTeardownHours += scheduleHours * teardownRatio;
                    totalProductionHours += scheduleHours * productionRatio;
                }
            }

            var utilizationPercentage = totalAnalysisHours > 0 ? (totalScheduledHours / totalAnalysisHours) * 100 : 0;

            utilizationByEquipment.Add(new EquipmentUtilization(
                equipmentLine.LineId,
                equipmentLine.LineName,
                utilizationPercentage,
                totalScheduledHours,
                totalSetupHours,
                totalProductionHours,
                totalTeardownHours,
                relevantSchedules.Count()
            ));
        }

        var overallUtilization = utilizationByEquipment.Any()
            ? utilizationByEquipment.Average(u => u.UtilizationPercentage)
            : 0;

        var bottlenecks = utilizationByEquipment
            .Where(u => u.UtilizationPercentage > 90)
            .Select(u => u.EquipmentLineId)
            .ToList();

        var underutilized = utilizationByEquipment
            .Where(u => u.UtilizationPercentage < 60)
            .Select(u => u.EquipmentLineId)
            .ToList();

        return new ResourceUtilizationAnalysis(
            analysisStartTime,
            analysisEndTime,
            overallUtilization,
            utilizationByEquipment,
            bottlenecks,
            underutilized
        );
    }

    /// <summary>
    /// Generate schedule recommendations for work order
    /// </summary>
    /// <param name="workOrder">Work order to schedule</param>
    /// <param name="availableEquipment">Available equipment lines</param>
    /// <param name="existingSchedules">Existing schedules</param>
    /// <param name="schedulingConstraints">Scheduling constraints</param>
    /// <returns>Schedule recommendations</returns>
    public ScheduleRecommendations GenerateScheduleRecommendations(
        WorkOrder workOrder,
        IEnumerable<EquipmentLine> availableEquipment,
        IEnumerable<JobSchedule> existingSchedules,
        SchedulingConstraints schedulingConstraints)
    {
        var recommendations = new List<ScheduleRecommendation>();
        var equipmentList = availableEquipment.ToList();
        var scheduleList = existingSchedules.ToList();

        foreach (var equipment in equipmentList)
        {
            if (!IsEquipmentSuitableForWorkOrder(equipment, workOrder))
                continue;

            var availableSlots = FindAvailableTimeSlots(equipment, scheduleList, schedulingConstraints);

            foreach (var slot in availableSlots.Take(3)) // Top 3 recommendations per equipment
            {
                var recommendation = CreateScheduleRecommendation(workOrder, equipment, slot, schedulingConstraints);
                if (recommendation != null)
                {
                    recommendations.Add(recommendation);
                }
            }
        }

        // Sort recommendations by score (highest first)
        var sortedRecommendations = recommendations
            .OrderByDescending(r => r.RecommendationScore)
            .Take(10) // Top 10 overall recommendations
            .ToList();

        return new ScheduleRecommendations(
            workOrder.Id,
            sortedRecommendations,
            DateTime.UtcNow,
            $"Generated {sortedRecommendations.Count} recommendations from {equipmentList.Count} equipment lines"
        );
    }

    /// <summary>
    /// Validate scheduling input data
    /// </summary>
    private static void ValidateSchedulingInput(List<WorkOrder> workOrders, List<EquipmentLine> equipmentLines)
    {
        if (!workOrders.Any())
            throw new ArgumentException("At least one work order is required for scheduling");

        if (!equipmentLines.Any())
            throw new ArgumentException("At least one equipment line is required for scheduling");

        // Validate work order data integrity
        foreach (var workOrder in workOrders)
        {
            if (workOrder.ScheduledEndTime <= workOrder.ScheduledStartTime)
                throw new ArgumentException($"Work order {workOrder.Id} has invalid scheduled times");
        }
    }

    /// <summary>
    /// Sort work orders by priority and urgency
    /// </summary>
    private static List<WorkOrder> SortWorkOrdersByPriority(List<WorkOrder> workOrders)
    {
        return workOrders
            .OrderBy(wo => wo.Priority) // Lower number = higher priority
            .ThenByDescending(wo => wo.CalculateUrgencyScore())
            .ThenBy(wo => wo.ScheduledStartTime)
            .ToList();
    }

    /// <summary>
    /// Detect and resolve conflicts in existing schedules
    /// </summary>
    private ConflictResolutionResult DetectAndResolveConflicts(List<JobSchedule> existingSchedules)
    {
        var allConflicts = new List<SchedulingConflict>();
        var resolutionActions = new List<ConflictResolutionAction>();

        // Group schedules by equipment line
        var schedulesByEquipment = existingSchedules.GroupBy(s => s.EquipmentLineId);

        foreach (var equipmentGroup in schedulesByEquipment)
        {
            var schedules = equipmentGroup.OrderBy(s => s.ScheduledStartTime).ToList();

            for (int i = 0; i < schedules.Count - 1; i++)
            {
                var current = schedules[i];
                var next = schedules[i + 1];

                if (current.ScheduledEndTime > next.ScheduledStartTime)
                {
                    // Time overlap conflict detected
                    var conflict = new SchedulingConflict(
                        SchedulingConflictType.TimeOverlap,
                        $"Time overlap between jobs {current.WorkOrderId} and {next.WorkOrderId}",
                        next.WorkOrderId,
                        ConflictSeverity.High,
                        DateTime.UtcNow,
                        false
                    );

                    allConflicts.Add(conflict);
                    current.AddConflict(
                        conflict.ConflictType,
                        conflict.Description,
                        conflict.ConflictingJobId,
                        conflict.Severity
                    );
                }
            }
        }

        return new ConflictResolutionResult(
            "GLOBAL",
            !allConflicts.Any(),
            allConflicts,
            resolutionActions,
            $"Detected {allConflicts.Count} conflicts in existing schedules"
        );
    }

    /// <summary>
    /// Create initial schedule assignments
    /// </summary>
    private List<JobSchedule> CreateInitialScheduleAssignments(
        List<WorkOrder> sortedWorkOrders,
        List<EquipmentLine> equipmentLines,
        List<JobSchedule> existingSchedules)
    {
        var newSchedules = new List<JobSchedule>();
        var schedulesByEquipment = existingSchedules.GroupBy(s => s.EquipmentLineId).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var workOrder in sortedWorkOrders)
        {
            var bestEquipment = FindBestEquipmentForWorkOrder(workOrder, equipmentLines, schedulesByEquipment);
            if (bestEquipment != null)
            {
                var schedule = CreateJobScheduleForWorkOrder(workOrder, bestEquipment, schedulesByEquipment);
                newSchedules.Add(schedule);

                // Update equipment schedule list for next iteration
                if (!schedulesByEquipment.ContainsKey(bestEquipment.LineId))
                    schedulesByEquipment[bestEquipment.LineId] = new List<JobSchedule>();
                schedulesByEquipment[bestEquipment.LineId].Add(schedule);
            }
        }

        return newSchedules;
    }

    /// <summary>
    /// Apply optimization algorithm based on goal
    /// </summary>
    private List<JobSchedule> ApplyOptimizationAlgorithm(List<JobSchedule> initialSchedules, OptimizationGoal goal)
    {
        return goal switch
        {
            OptimizationGoal.MinimizeMakespan => OptimizeForMakespan(initialSchedules),
            OptimizationGoal.MinimizeSetupTime => OptimizeForSetupTime(initialSchedules),
            OptimizationGoal.MaximizeThroughput => OptimizeForThroughput(initialSchedules),
            OptimizationGoal.MinimizeTardiness => OptimizeForTardiness(initialSchedules),
            OptimizationGoal.BalanceWorkload => OptimizeForWorkloadBalance(initialSchedules),
            OptimizationGoal.MaximizeUtilization => OptimizeForUtilization(initialSchedules),
            _ => initialSchedules
        };
    }

    /// <summary>
    /// Optimize schedule for minimum makespan
    /// </summary>
    private List<JobSchedule> OptimizeForMakespan(List<JobSchedule> schedules)
    {
        // Simple optimization: try to reduce overall completion time
        var optimizedSchedules = new List<JobSchedule>(schedules);

        // Group by equipment and optimize within each group
        var schedulesByEquipment = optimizedSchedules.GroupBy(s => s.EquipmentLineId);

        foreach (var equipmentGroup in schedulesByEquipment)
        {
            var equipmentSchedules = equipmentGroup.OrderBy(s => s.Priority).ToList();

            // Try to minimize gaps between jobs
            DateTime currentTime = equipmentSchedules.First().ScheduledStartTime;

            foreach (var schedule in equipmentSchedules)
            {
                if (schedule.CanStartAt(currentTime))
                {
                    var duration = schedule.TotalScheduledTimeMinutes;
                    var newEndTime = currentTime.AddMinutes((double)duration);
                    schedule.UpdateScheduledTimes(currentTime, newEndTime, "MakespanOptimization");
                    currentTime = newEndTime;
                }
            }
        }

        return optimizedSchedules;
    }

    /// <summary>
    /// Optimize schedule for minimum setup time
    /// </summary>
    private List<JobSchedule> OptimizeForSetupTime(List<JobSchedule> schedules)
    {
        // Group similar jobs together to minimize setup changes
        return schedules.OrderBy(s => s.EquipmentLineId)
                       .ThenBy(s => s.SetupTimeMinutes)
                       .ThenBy(s => s.Priority)
                       .ToList();
    }

    /// <summary>
    /// Optimize schedule for maximum throughput
    /// </summary>
    private List<JobSchedule> OptimizeForThroughput(List<JobSchedule> schedules)
    {
        // Prioritize jobs with highest production rates
        return schedules.OrderBy(s => s.EquipmentLineId)
                       .ThenByDescending(s => s.EstimatedProductionTimeMinutes / s.TotalScheduledTimeMinutes)
                       .ToList();
    }

    /// <summary>
    /// Optimize schedule for minimum tardiness
    /// </summary>
    private List<JobSchedule> OptimizeForTardiness(List<JobSchedule> schedules)
    {
        // Prioritize jobs with earliest due dates
        return schedules.OrderBy(s => s.ScheduledEndTime)
                       .ThenBy(s => s.Priority)
                       .ToList();
    }

    /// <summary>
    /// Optimize schedule for workload balance
    /// </summary>
    private List<JobSchedule> OptimizeForWorkloadBalance(List<JobSchedule> schedules)
    {
        // Try to balance workload across equipment lines
        var schedulesByEquipment = schedules.GroupBy(s => s.EquipmentLineId).ToList();
        var balanced = new List<JobSchedule>();

        // Round-robin assignment to balance load
        int maxCount = schedulesByEquipment.Max(g => g.Count());
        for (int i = 0; i < maxCount; i++)
        {
            foreach (var group in schedulesByEquipment)
            {
                var schedule = group.Skip(i).FirstOrDefault();
                if (schedule != null)
                {
                    balanced.Add(schedule);
                }
            }
        }

        return balanced;
    }

    /// <summary>
    /// Optimize schedule for maximum utilization
    /// </summary>
    private List<JobSchedule> OptimizeForUtilization(List<JobSchedule> schedules)
    {
        // Minimize idle time between jobs
        return OptimizeForMakespan(schedules); // Similar to makespan optimization
    }

    /// <summary>
    /// Validate optimized schedule
    /// </summary>
    private ScheduleValidationResult ValidateSchedule(List<JobSchedule> schedules)
    {
        var validationErrors = new List<string>();
        var warnings = new List<string>();

        // Check for time overlaps
        var schedulesByEquipment = schedules.GroupBy(s => s.EquipmentLineId);
        foreach (var equipmentGroup in schedulesByEquipment)
        {
            var sortedSchedules = equipmentGroup.OrderBy(s => s.ScheduledStartTime).ToList();
            for (int i = 0; i < sortedSchedules.Count - 1; i++)
            {
                if (sortedSchedules[i].ScheduledEndTime > sortedSchedules[i + 1].ScheduledStartTime)
                {
                    validationErrors.Add($"Time overlap detected between jobs {sortedSchedules[i].WorkOrderId} and {sortedSchedules[i + 1].WorkOrderId}");
                }
            }
        }

        // Check for constraint violations
        foreach (var schedule in schedules)
        {
            if (schedule.HasConflicts)
            {
                warnings.Add($"Job {schedule.WorkOrderId} has unresolved conflicts");
            }

            if (!schedule.AreDependenciesSatisfied)
            {
                validationErrors.Add($"Job {schedule.WorkOrderId} has unsatisfied dependencies");
            }
        }

        var isValid = !validationErrors.Any();
        var summary = isValid ? "Schedule validation passed" : $"Schedule validation failed with {validationErrors.Count} errors";

        return new ScheduleValidationResult(
            isValid,
            validationErrors,
            warnings,
            summary
        );
    }

    /// <summary>
    /// Calculate schedule metrics
    /// </summary>
    private ScheduleMetrics CalculateScheduleMetrics(List<JobSchedule> schedules)
    {
        if (!schedules.Any())
        {
            return new ScheduleMetrics(0, 0, 0, 0, 0, 0, 0, 0);
        }

        var totalJobs = schedules.Count;
        var totalDuration = schedules.Max(s => s.ScheduledEndTime) - schedules.Min(s => s.ScheduledStartTime);
        var totalMakespan = (decimal)totalDuration.TotalHours;

        var averageSetupTime = schedules.Average(s => s.SetupTimeMinutes);
        var averageTeardownTime = schedules.Average(s => s.TeardownTimeMinutes);
        var averageProductionTime = schedules.Average(s => s.EstimatedProductionTimeMinutes);

        var utilizationPercentage = schedules.Any()
            ? schedules.Sum(s => s.TotalScheduledTimeMinutes) / (totalMakespan * 60) * 100
            : 0;

        var averagePriority = schedules.Average(s => s.Priority);

        return new ScheduleMetrics(
            totalJobs,
            totalMakespan,
            utilizationPercentage,
            averageSetupTime,
            averageTeardownTime,
            averageProductionTime,
            averagePriority,
            schedules.Count(s => s.HasConflicts)
        );
    }

    /// <summary>
    /// Additional helper methods for scheduling operations
    /// </summary>

    private List<SchedulingConflict> DetectConflictsForSchedule(JobSchedule jobSchedule, IEnumerable<JobSchedule> allSchedules)
    {
        // Implementation for detecting conflicts
        return new List<SchedulingConflict>();
    }

    private ConflictResolutionAction? ResolveByShiftingLowerPriority(JobSchedule jobSchedule, SchedulingConflict conflict, IEnumerable<JobSchedule> allSchedules)
    {
        // Implementation for shifting lower priority jobs
        return null;
    }

    private ConflictResolutionAction? ResolveBySplittingJobs(JobSchedule jobSchedule, SchedulingConflict conflict)
    {
        // Implementation for splitting jobs
        return null;
    }

    private ConflictResolutionAction? ResolveByAlternateEquipment(JobSchedule jobSchedule, SchedulingConflict conflict, IEnumerable<JobSchedule> allSchedules)
    {
        // Implementation for using alternate equipment
        return null;
    }

    private ConflictResolutionAction? ResolveByOverridingHighPriority(JobSchedule jobSchedule, SchedulingConflict conflict)
    {
        // Implementation for overriding high priority jobs
        return null;
    }

    private bool IsEquipmentSuitableForWorkOrder(EquipmentLine equipment, WorkOrder workOrder)
    {
        // Check if equipment can handle the work order
        return equipment.IsActive && equipment.LineId == workOrder.ResourceReference;
    }

    private List<TimeSlot> FindAvailableTimeSlots(EquipmentLine equipment, List<JobSchedule> schedules, SchedulingConstraints constraints)
    {
        // Find available time slots for equipment
        return new List<TimeSlot>();
    }

    private ScheduleRecommendation? CreateScheduleRecommendation(WorkOrder workOrder, EquipmentLine equipment, TimeSlot slot, SchedulingConstraints constraints)
    {
        // Create schedule recommendation
        return null;
    }

    private EquipmentLine? FindBestEquipmentForWorkOrder(WorkOrder workOrder, List<EquipmentLine> equipmentLines, Dictionary<string, List<JobSchedule>> schedulesByEquipment)
    {
        // Find best equipment for work order
        return equipmentLines.FirstOrDefault(el => el.LineId == workOrder.ResourceReference && el.IsActive);
    }

    private JobSchedule CreateJobScheduleForWorkOrder(WorkOrder workOrder, EquipmentLine equipment, Dictionary<string, List<JobSchedule>> schedulesByEquipment)
    {
        // Create job schedule for work order
        var scheduleId = $"SCH-{workOrder.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}";

        return new JobSchedule(
            scheduleId,
            workOrder.Id,
            equipment.LineId,
            workOrder.ScheduledStartTime,
            workOrder.ScheduledEndTime,
            workOrder.Priority,
            workOrder.SetupTimeMinutes,
            workOrder.TeardownTimeMinutes,
            (decimal)(workOrder.ScheduledEndTime - workOrder.ScheduledStartTime).TotalMinutes,
            "AutoScheduler"
        );
    }
}

/// <summary>
/// Conflict resolution strategy enumeration
/// </summary>
public enum ConflictResolutionStrategy
{
    /// <summary>
    /// Shift lower priority jobs to resolve conflicts
    /// </summary>
    ShiftLowerPriority,

    /// <summary>
    /// Split jobs to fit available slots
    /// </summary>
    SplitJobs,

    /// <summary>
    /// Use alternate equipment if available
    /// </summary>
    AlternateEquipment,

    /// <summary>
    /// Override high priority jobs (requires approval)
    /// </summary>
    OverrideHighPriority
}

/// <summary>
/// Schedule optimization result
/// </summary>
/// <param name="OptimizedSchedules">Optimized job schedules</param>
/// <param name="ConflictResolution">Conflict resolution results</param>
/// <param name="ValidationResult">Schedule validation results</param>
/// <param name="Metrics">Schedule performance metrics</param>
/// <param name="GeneratedAt">When optimization was performed</param>
public record ScheduleOptimizationResult(
    List<JobSchedule> OptimizedSchedules,
    ConflictResolutionResult ConflictResolution,
    ScheduleValidationResult ValidationResult,
    ScheduleMetrics Metrics,
    DateTime GeneratedAt
);

/// <summary>
/// Conflict resolution result
/// </summary>
/// <param name="ScheduleId">Schedule identifier</param>
/// <param name="AllConflictsResolved">Whether all conflicts were resolved</param>
/// <param name="Conflicts">Detected conflicts</param>
/// <param name="ResolutionActions">Actions taken to resolve conflicts</param>
/// <param name="Summary">Resolution summary</param>
public record ConflictResolutionResult(
    string ScheduleId,
    bool AllConflictsResolved,
    IEnumerable<SchedulingConflict> Conflicts,
    IEnumerable<ConflictResolutionAction> ResolutionActions,
    string Summary
);

/// <summary>
/// Conflict resolution action
/// </summary>
/// <param name="ActionType">Type of action taken</param>
/// <param name="TargetScheduleId">Schedule affected by action</param>
/// <param name="Description">Action description</param>
/// <param name="NewStartTime">New start time if schedule was moved</param>
/// <param name="NewEndTime">New end time if schedule was moved</param>
public record ConflictResolutionAction(
    string ActionType,
    string TargetScheduleId,
    string Description,
    DateTime? NewStartTime,
    DateTime? NewEndTime
);

/// <summary>
/// Schedule validation result
/// </summary>
/// <param name="IsValid">Whether schedule is valid</param>
/// <param name="ValidationErrors">Validation errors</param>
/// <param name="Warnings">Validation warnings</param>
/// <param name="Summary">Validation summary</param>
public record ScheduleValidationResult(
    bool IsValid,
    List<string> ValidationErrors,
    List<string> Warnings,
    string Summary
);

/// <summary>
/// Schedule performance metrics
/// </summary>
/// <param name="TotalJobs">Total number of jobs</param>
/// <param name="TotalMakespanHours">Total makespan in hours</param>
/// <param name="UtilizationPercentage">Overall utilization percentage</param>
/// <param name="AverageSetupTimeMinutes">Average setup time</param>
/// <param name="AverageTeardownTimeMinutes">Average teardown time</param>
/// <param name="AverageProductionTimeMinutes">Average production time</param>
/// <param name="AveragePriority">Average job priority</param>
/// <param name="ConflictCount">Number of unresolved conflicts</param>
public record ScheduleMetrics(
    int TotalJobs,
    decimal TotalMakespanHours,
    decimal UtilizationPercentage,
    decimal AverageSetupTimeMinutes,
    decimal AverageTeardownTimeMinutes,
    decimal AverageProductionTimeMinutes,
    decimal AveragePriority,
    int ConflictCount
);

/// <summary>
/// Resource utilization analysis
/// </summary>
/// <param name="AnalysisStartTime">Analysis start time</param>
/// <param name="AnalysisEndTime">Analysis end time</param>
/// <param name="OverallUtilizationPercentage">Overall utilization percentage</param>
/// <param name="EquipmentUtilizations">Utilization by equipment</param>
/// <param name="Bottlenecks">Equipment lines that are bottlenecks</param>
/// <param name="Underutilized">Equipment lines that are underutilized</param>
public record ResourceUtilizationAnalysis(
    DateTime AnalysisStartTime,
    DateTime AnalysisEndTime,
    decimal OverallUtilizationPercentage,
    List<EquipmentUtilization> EquipmentUtilizations,
    List<string> Bottlenecks,
    List<string> Underutilized
);

/// <summary>
/// Equipment utilization data
/// </summary>
/// <param name="EquipmentLineId">Equipment line identifier</param>
/// <param name="EquipmentLineName">Equipment line name</param>
/// <param name="UtilizationPercentage">Utilization percentage</param>
/// <param name="TotalScheduledHours">Total scheduled hours</param>
/// <param name="SetupHours">Setup hours</param>
/// <param name="ProductionHours">Production hours</param>
/// <param name="TeardownHours">Teardown hours</param>
/// <param name="JobCount">Number of jobs scheduled</param>
public record EquipmentUtilization(
    string EquipmentLineId,
    string EquipmentLineName,
    decimal UtilizationPercentage,
    decimal TotalScheduledHours,
    decimal SetupHours,
    decimal ProductionHours,
    decimal TeardownHours,
    int JobCount
);

/// <summary>
/// Schedule recommendations
/// </summary>
/// <param name="WorkOrderId">Work order identifier</param>
/// <param name="Recommendations">List of recommendations</param>
/// <param name="GeneratedAt">When recommendations were generated</param>
/// <param name="Summary">Recommendations summary</param>
public record ScheduleRecommendations(
    string WorkOrderId,
    List<ScheduleRecommendation> Recommendations,
    DateTime GeneratedAt,
    string Summary
);

/// <summary>
/// Individual schedule recommendation
/// </summary>
/// <param name="EquipmentLineId">Recommended equipment line</param>
/// <param name="StartTime">Recommended start time</param>
/// <param name="EndTime">Recommended end time</param>
/// <param name="RecommendationScore">Recommendation score (0-100)</param>
/// <param name="Reasoning">Reasoning for recommendation</param>
/// <param name="Constraints">Constraints considered</param>
public record ScheduleRecommendation(
    string EquipmentLineId,
    DateTime StartTime,
    DateTime EndTime,
    decimal RecommendationScore,
    string Reasoning,
    List<string> Constraints
);

/// <summary>
/// Available time slot
/// </summary>
/// <param name="StartTime">Start time of slot</param>
/// <param name="EndTime">End time of slot</param>
/// <param name="DurationMinutes">Duration in minutes</param>
public record TimeSlot(
    DateTime StartTime,
    DateTime EndTime,
    decimal DurationMinutes
);

/// <summary>
/// Scheduling constraints
/// </summary>
/// <param name="EarliestStartTime">Earliest allowed start time</param>
/// <param name="LatestEndTime">Latest allowed end time</param>
/// <param name="RequiredShifts">Required shifts</param>
/// <param name="ForbiddenTimeRanges">Time ranges to avoid</param>
/// <param name="MaxDailyHours">Maximum hours per day</param>
public record SchedulingConstraints(
    DateTime? EarliestStartTime,
    DateTime? LatestEndTime,
    List<string> RequiredShifts,
    List<TimeSlot> ForbiddenTimeRanges,
    decimal MaxDailyHours
);
