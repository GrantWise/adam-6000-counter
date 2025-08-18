using Industrial.Adam.Oee.Domain.Entities;
using MediatR;

namespace Industrial.Adam.Oee.Application.Queries;

/// <summary>
/// Query to get batch summary information
/// </summary>
/// <param name="BatchId">Batch identifier</param>
public record GetBatchSummaryQuery(string BatchId) : IRequest<BatchSummaryResult>;

/// <summary>
/// Result containing batch summary information
/// </summary>
/// <param name="IsSuccess">Whether query was successful</param>
/// <param name="BatchSummary">Batch summary data</param>
/// <param name="MaterialConsumptions">Material consumption records</param>
/// <param name="QualityChecks">Quality check records</param>
/// <param name="Notes">Batch notes</param>
/// <param name="GenealogyInfo">Batch genealogy information</param>
/// <param name="EfficiencyMetrics">Batch efficiency metrics</param>
/// <param name="ErrorMessage">Error message if query failed</param>
public record BatchSummaryResult(
    bool IsSuccess,
    BatchSummary? BatchSummary,
    List<MaterialConsumption> MaterialConsumptions,
    List<QualityCheck> QualityChecks,
    List<BatchNote> Notes,
    BatchGenealogyInfo? GenealogyInfo,
    BatchEfficiencyMetrics? EfficiencyMetrics,
    string? ErrorMessage
);
