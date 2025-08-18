using Industrial.Adam.Oee.Domain.Entities;

namespace Industrial.Adam.Oee.Domain.Services;

/// <summary>
/// Batch Management Service
/// 
/// Provides domain logic for managing batch lifecycles, tracking genealogy,
/// coordinating quality gates, and optimizing batch workflows.
/// </summary>
public sealed class BatchManagementService
{
    /// <summary>
    /// Create a new batch for a work order
    /// </summary>
    /// <param name="workOrder">Work order for the batch</param>
    /// <param name="batchNumber">Batch number</param>
    /// <param name="operatorId">Operator creating the batch</param>
    /// <param name="equipmentLineId">Equipment line for production</param>
    /// <param name="parentBatchId">Parent batch for genealogy</param>
    /// <param name="shiftId">Current shift</param>
    /// <returns>New batch instance</returns>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when batch tracking is disabled</exception>
    public Batch CreateBatch(
        WorkOrder workOrder,
        string batchNumber,
        string operatorId,
        string equipmentLineId,
        string? parentBatchId = null,
        string? shiftId = null)
    {
        if (workOrder == null)
            throw new ArgumentException("Work order is required", nameof(workOrder));

        if (!workOrder.BatchTrackingEnabled)
            throw new InvalidOperationException("Batch tracking is not enabled for this work order");

        var batchId = GenerateBatchId(workOrder.Id, batchNumber);
        var plannedQuantity = workOrder.PlannedBatchSize ?? workOrder.PlannedQuantity;

        var batch = new Batch(
            batchId,
            batchNumber,
            workOrder.Id,
            workOrder.ProductId,
            plannedQuantity,
            operatorId,
            equipmentLineId,
            workOrder.UnitOfMeasure,
            parentBatchId,
            shiftId ?? workOrder.ShiftId
        );

        return batch;
    }

    /// <summary>
    /// Start batch production
    /// </summary>
    /// <param name="batch">Batch to start</param>
    /// <param name="qualityGates">Required quality gates</param>
    /// <returns>True if batch can be started</returns>
    /// <exception cref="ArgumentException">Thrown when batch is null</exception>
    public bool StartBatchProduction(Batch batch, IEnumerable<QualityGate> qualityGates)
    {
        if (batch == null)
            throw new ArgumentException("Batch is required", nameof(batch));

        // Check if any start-of-job quality gates need to be executed
        var startGates = qualityGates.Where(qg =>
            qg.IsActive &&
            qg.Trigger == QualityGateTrigger.JobStart);

        foreach (var gate in startGates)
        {
            if (gate.IsMandatory && !gate.AreAllCriteriaMet)
            {
                // Cannot start - mandatory quality gate not satisfied
                return false;
            }
        }

        batch.Start();
        return true;
    }

    /// <summary>
    /// Complete batch production
    /// </summary>
    /// <param name="batch">Batch to complete</param>
    /// <param name="finalGoodQuantity">Final good quantity</param>
    /// <param name="finalDefectiveQuantity">Final defective quantity</param>
    /// <param name="qualityGates">Quality gates to execute</param>
    /// <returns>Batch completion result</returns>
    /// <exception cref="ArgumentException">Thrown when batch is null</exception>
    public BatchCompletionResult CompleteBatchProduction(
        Batch batch,
        decimal finalGoodQuantity,
        decimal finalDefectiveQuantity,
        IEnumerable<QualityGate> qualityGates)
    {
        if (batch == null)
            throw new ArgumentException("Batch is required", nameof(batch));

        // Update final quantities
        batch.UpdateQuantities(finalGoodQuantity, finalDefectiveQuantity);

        // Execute end-of-job quality gates
        var endGates = qualityGates.Where(qg =>
            qg.IsActive &&
            qg.Trigger == QualityGateTrigger.JobEnd);

        var qualityGateResults = new List<QualityGateExecutionResult>();
        bool allQualityGatesPassed = true;
        bool shouldHoldBatch = false;

        foreach (var gate in endGates)
        {
            var result = gate.ExecuteGate(batch.WorkOrderId, batch.Id, batch.OperatorId, "Batch completion quality check");
            qualityGateResults.Add(result);

            if (result.Result == QualityGateResult.Failed && gate.IsMandatory)
            {
                allQualityGatesPassed = false;
                if (result.ShouldHoldJob)
                {
                    shouldHoldBatch = true;
                }
            }
        }

        // Place batch on hold if quality gates failed
        if (shouldHoldBatch)
        {
            batch.PlaceOnHold("Failed mandatory quality gates during batch completion");
        }
        else if (allQualityGatesPassed)
        {
            batch.Complete();
        }

        return new BatchCompletionResult(
            batch.Id,
            allQualityGatesPassed,
            shouldHoldBatch,
            batch.GetYieldPercentage(),
            batch.QualityScore,
            qualityGateResults
        );
    }

    /// <summary>
    /// Process batch genealogy for traceability
    /// </summary>
    /// <param name="batch">Batch to process</param>
    /// <param name="parentBatches">Parent batches</param>
    /// <returns>Genealogy information</returns>
    public BatchGenealogyInfo ProcessBatchGenealogy(Batch batch, IEnumerable<Batch> parentBatches)
    {
        if (batch == null)
            throw new ArgumentException("Batch is required", nameof(batch));

        var genealogyChain = new List<BatchGenealogyNode>();

        // Build genealogy chain
        foreach (var parentBatch in parentBatches.Where(pb => pb.Id == batch.ParentBatchId))
        {
            var node = new BatchGenealogyNode(
                parentBatch.Id,
                parentBatch.BatchNumber,
                parentBatch.ProductId,
                parentBatch.Status.ToString(),
                parentBatch.GetYieldPercentage(),
                parentBatch.QualityScore,
                parentBatch.StartTime,
                parentBatch.CompletionTime,
                parentBatch.OperatorId
            );
            genealogyChain.Add(node);
        }

        // Calculate inherited quality metrics
        var inheritedQualityScore = genealogyChain.Any()
            ? genealogyChain.Average(gc => gc.QualityScore)
            : 100m;

        var inheritedYieldPercentage = genealogyChain.Any()
            ? genealogyChain.Average(gc => gc.YieldPercentage)
            : 100m;

        return new BatchGenealogyInfo(
            batch.Id,
            genealogyChain,
            inheritedQualityScore,
            inheritedYieldPercentage,
            genealogyChain.Count
        );
    }

    /// <summary>
    /// Calculate batch efficiency metrics
    /// </summary>
    /// <param name="batch">Batch to analyze</param>
    /// <param name="standardTime">Standard time for batch</param>
    /// <returns>Batch efficiency metrics</returns>
    public BatchEfficiencyMetrics CalculateBatchEfficiency(Batch batch, TimeSpan? standardTime = null)
    {
        if (batch == null)
            throw new ArgumentException("Batch is required", nameof(batch));

        if (batch.StartTime == null)
        {
            return new BatchEfficiencyMetrics(
                batch.Id,
                0, 0, 0, 0, 0, 0, 0
            );
        }

        var actualTime = batch.CompletionTime ?? DateTime.UtcNow;
        var actualDurationMinutes = (decimal)(actualTime - batch.StartTime.Value).TotalMinutes;

        var standardDurationMinutes = standardTime?.TotalMinutes ?? (double)actualDurationMinutes;
        var timeEfficiency = actualDurationMinutes > 0 ? (decimal)(standardDurationMinutes / (double)actualDurationMinutes) * 100 : 0;

        var completionRate = batch.GetCompletionPercentage();
        var yieldPercentage = batch.GetYieldPercentage();
        var qualityScore = batch.QualityScore;
        var productionRate = batch.GetProductionRate();

        // Overall efficiency is weighted average of time, yield, and quality
        var overallEfficiency = (timeEfficiency * 0.4m) + (yieldPercentage * 0.4m) + (qualityScore * 0.2m);

        return new BatchEfficiencyMetrics(
            batch.Id,
            timeEfficiency,
            yieldPercentage,
            qualityScore,
            completionRate,
            productionRate,
            actualDurationMinutes,
            overallEfficiency
        );
    }

    /// <summary>
    /// Optimize batch sizes for a work order
    /// </summary>
    /// <param name="workOrder">Work order to optimize</param>
    /// <param name="equipmentConstraints">Equipment constraints</param>
    /// <param name="qualityRequirements">Quality requirements</param>
    /// <returns>Optimal batch configuration</returns>
    public BatchOptimizationResult OptimizeBatchSizes(
        WorkOrder workOrder,
        EquipmentConstraints equipmentConstraints,
        QualityRequirements qualityRequirements)
    {
        if (workOrder == null)
            throw new ArgumentException("Work order is required", nameof(workOrder));

        if (!workOrder.BatchTrackingEnabled)
        {
            // Single batch for entire work order
            return new BatchOptimizationResult(
                workOrder.Id,
                1,
                workOrder.PlannedQuantity,
                workOrder.PlannedQuantity,
                "Single batch - batch tracking disabled"
            );
        }

        var plannedQuantity = workOrder.PlannedQuantity;
        var maxBatchSize = equipmentConstraints.MaxBatchSize ?? plannedQuantity;
        var minBatchSize = equipmentConstraints.MinBatchSize ?? 1;
        var preferredBatchSize = workOrder.PlannedBatchSize ?? maxBatchSize;

        // Calculate optimal batch size based on constraints
        var optimalBatchSize = CalculateOptimalBatchSize(
            plannedQuantity,
            preferredBatchSize,
            minBatchSize,
            maxBatchSize,
            qualityRequirements.SampleFrequency
        );

        var numberOfBatches = (int)Math.Ceiling(plannedQuantity / optimalBatchSize);
        var actualBatchSize = plannedQuantity / numberOfBatches;

        var reasoning = $"Optimized for {numberOfBatches} batches of {actualBatchSize:F1} units each. " +
                       $"Constraints: Min={minBatchSize}, Max={maxBatchSize}, Preferred={preferredBatchSize}";

        return new BatchOptimizationResult(
            workOrder.Id,
            numberOfBatches,
            actualBatchSize,
            optimalBatchSize,
            reasoning
        );
    }

    /// <summary>
    /// Generate unique batch identifier
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="batchNumber">Batch number</param>
    /// <returns>Unique batch identifier</returns>
    private static string GenerateBatchId(string workOrderId, string batchNumber)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        return $"BATCH-{workOrderId}-{batchNumber}-{timestamp}";
    }

    /// <summary>
    /// Calculate optimal batch size
    /// </summary>
    /// <param name="totalQuantity">Total quantity to produce</param>
    /// <param name="preferredSize">Preferred batch size</param>
    /// <param name="minSize">Minimum batch size</param>
    /// <param name="maxSize">Maximum batch size</param>
    /// <param name="sampleFrequency">Quality sample frequency</param>
    /// <returns>Optimal batch size</returns>
    private static decimal CalculateOptimalBatchSize(
        decimal totalQuantity,
        decimal preferredSize,
        decimal minSize,
        decimal maxSize,
        decimal sampleFrequency)
    {
        // Start with preferred size
        var optimalSize = preferredSize;

        // Ensure within constraints
        optimalSize = Math.Max(optimalSize, minSize);
        optimalSize = Math.Min(optimalSize, maxSize);

        // Adjust for quality sampling requirements
        if (sampleFrequency > 0)
        {
            var samplesPerBatch = optimalSize / sampleFrequency;
            if (samplesPerBatch < 1)
            {
                // Ensure at least one sample per batch
                optimalSize = Math.Max(optimalSize, sampleFrequency);
            }
        }

        // Ensure we don't exceed total quantity
        optimalSize = Math.Min(optimalSize, totalQuantity);

        return optimalSize;
    }
}

/// <summary>
/// Batch completion result
/// </summary>
/// <param name="BatchId">Batch identifier</param>
/// <param name="AllQualityGatesPassed">Whether all quality gates passed</param>
/// <param name="ShouldHoldBatch">Whether batch should be held</param>
/// <param name="YieldPercentage">Final yield percentage</param>
/// <param name="QualityScore">Final quality score</param>
/// <param name="QualityGateResults">Quality gate execution results</param>
public record BatchCompletionResult(
    string BatchId,
    bool AllQualityGatesPassed,
    bool ShouldHoldBatch,
    decimal YieldPercentage,
    decimal QualityScore,
    List<QualityGateExecutionResult> QualityGateResults
);

/// <summary>
/// Batch genealogy information
/// </summary>
/// <param name="BatchId">Current batch identifier</param>
/// <param name="GenealogyChain">Chain of parent batches</param>
/// <param name="InheritedQualityScore">Inherited quality score from parents</param>
/// <param name="InheritedYieldPercentage">Inherited yield percentage from parents</param>
/// <param name="GenerationLevel">Generation level in genealogy</param>
public record BatchGenealogyInfo(
    string BatchId,
    List<BatchGenealogyNode> GenealogyChain,
    decimal InheritedQualityScore,
    decimal InheritedYieldPercentage,
    int GenerationLevel
);

/// <summary>
/// Batch genealogy node
/// </summary>
/// <param name="BatchId">Batch identifier</param>
/// <param name="BatchNumber">Batch number</param>
/// <param name="ProductId">Product identifier</param>
/// <param name="Status">Batch status</param>
/// <param name="YieldPercentage">Yield percentage</param>
/// <param name="QualityScore">Quality score</param>
/// <param name="StartTime">Start time</param>
/// <param name="CompletionTime">Completion time</param>
/// <param name="OperatorId">Operator identifier</param>
public record BatchGenealogyNode(
    string BatchId,
    string BatchNumber,
    string ProductId,
    string Status,
    decimal YieldPercentage,
    decimal QualityScore,
    DateTime? StartTime,
    DateTime? CompletionTime,
    string OperatorId
);

/// <summary>
/// Batch efficiency metrics
/// </summary>
/// <param name="BatchId">Batch identifier</param>
/// <param name="TimeEfficiency">Time efficiency percentage</param>
/// <param name="YieldPercentage">Yield percentage</param>
/// <param name="QualityScore">Quality score</param>
/// <param name="CompletionRate">Completion rate percentage</param>
/// <param name="ProductionRate">Production rate per hour</param>
/// <param name="ActualDurationMinutes">Actual duration in minutes</param>
/// <param name="OverallEfficiency">Overall efficiency score</param>
public record BatchEfficiencyMetrics(
    string BatchId,
    decimal TimeEfficiency,
    decimal YieldPercentage,
    decimal QualityScore,
    decimal CompletionRate,
    decimal ProductionRate,
    decimal ActualDurationMinutes,
    decimal OverallEfficiency
);

/// <summary>
/// Batch optimization result
/// </summary>
/// <param name="WorkOrderId">Work order identifier</param>
/// <param name="NumberOfBatches">Recommended number of batches</param>
/// <param name="ActualBatchSize">Actual batch size to use</param>
/// <param name="OptimalBatchSize">Calculated optimal batch size</param>
/// <param name="Reasoning">Optimization reasoning</param>
public record BatchOptimizationResult(
    string WorkOrderId,
    int NumberOfBatches,
    decimal ActualBatchSize,
    decimal OptimalBatchSize,
    string Reasoning
);

/// <summary>
/// Equipment constraints for batch optimization
/// </summary>
/// <param name="MaxBatchSize">Maximum batch size</param>
/// <param name="MinBatchSize">Minimum batch size</param>
/// <param name="SetupTimeMinutes">Setup time between batches</param>
/// <param name="TeardownTimeMinutes">Teardown time after batches</param>
public record EquipmentConstraints(
    decimal? MaxBatchSize,
    decimal? MinBatchSize,
    decimal SetupTimeMinutes,
    decimal TeardownTimeMinutes
);

/// <summary>
/// Quality requirements for batch optimization
/// </summary>
/// <param name="SampleFrequency">Quality sample frequency</param>
/// <param name="MinimumQualityScore">Minimum quality score required</param>
/// <param name="RequiredYieldPercentage">Required yield percentage</param>
public record QualityRequirements(
    decimal SampleFrequency,
    decimal MinimumQualityScore,
    decimal RequiredYieldPercentage
);
