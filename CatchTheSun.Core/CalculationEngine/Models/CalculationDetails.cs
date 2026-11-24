using CatchTheSun.Core.Entities;

namespace CatchTheSun.Core.CalculationEngine.Models;

public sealed record CalculationDetails(
    Guid Id,
    Guid SiteId,
    CalculationMethod Method,
    DateTime CreatedAt,
    string? AlgorithmVersion,
    string? OptionsJson,
    double? ConsistencyRatio,
    double? LambdaMax,
    int? IterationCount,
    IReadOnlyList<CalculationCriterionDetails> Criteria,
    IReadOnlyList<CalculationComparisonDetails> Comparisons,
    IReadOnlyList<CalculationResultDetails> Results);

public sealed record CalculationCriterionDetails(
    Guid CriterionId,
    string Name,
    string? Unit,
    CriterionDirection Direction,
    decimal? TargetValue,
    bool IsRequired,
    decimal? Weight);

public sealed record CalculationComparisonDetails(Guid CriterionAId, Guid CriterionBId, decimal Value);

public sealed record CalculationResultDetails(
    Guid VariantId,
    string VariantName,
    string? ExternalId,
    decimal? Score,
    int? Rank,
    int? ParetoFront,
    bool? IsParetoOptimal,
    decimal? ParetoSecondaryScore,
    IReadOnlyList<CalculationValueSnapshotDetails> Inputs);

public sealed record CalculationValueSnapshotDetails(Guid CriterionId, decimal Value);
