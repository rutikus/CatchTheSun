using System.Text.Json;
using CatchTheSun.Core.CalculationEngine.Models;

namespace CatchTheSun.API.DTOs.Calculations;

public sealed record CalculationResponseDto(
    Guid Id,
    Guid SiteId,
    string Method,
    DateTime CreatedAt,
    string? AlgorithmVersion,
    JsonElement? Options,
    double? ConsistencyRatio,
    double? LambdaMax,
    int? IterationCount,
    IReadOnlyList<CalculationCriterionResponseDto> Criteria,
    IReadOnlyList<CalculationComparisonResponseDto> Comparisons,
    IReadOnlyList<CalculationResultResponseDto> Results)
{
    public static CalculationResponseDto From(CalculationDetails details) => new(
        details.Id,
        details.SiteId,
        details.Method.ToString(),
        details.CreatedAt,
        details.AlgorithmVersion,
        ParseOptions(details.OptionsJson),
        details.ConsistencyRatio,
        details.LambdaMax,
        details.IterationCount,
        details.Criteria.Select(CalculationCriterionResponseDto.From).ToArray(),
        details.Comparisons.Select(CalculationComparisonResponseDto.From).ToArray(),
        details.Results.Select(CalculationResultResponseDto.From).ToArray());

    private static JsonElement? ParseOptions(string? json)
    {
        if (json is null)
            return null;
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}

public sealed record CalculationCriterionResponseDto(
    Guid CriterionId,
    string Name,
    string? Unit,
    string Direction,
    decimal? TargetValue,
    bool IsRequired,
    decimal? Weight)
{
    public static CalculationCriterionResponseDto From(CalculationCriterionDetails details) => new(
        details.CriterionId,
        details.Name,
        details.Unit,
        details.Direction.ToString(),
        details.TargetValue,
        details.IsRequired,
        details.Weight);
}

public sealed record CalculationComparisonResponseDto(Guid CriterionAId, Guid CriterionBId, decimal Value)
{
    public static CalculationComparisonResponseDto From(CalculationComparisonDetails details) =>
        new(details.CriterionAId, details.CriterionBId, details.Value);
}

public sealed record CalculationResultResponseDto(
    Guid VariantId,
    string VariantName,
    string? ExternalId,
    decimal? Score,
    int? Rank,
    int? ParetoFront,
    bool? IsParetoOptimal,
    decimal? ParetoSecondaryScore,
    IReadOnlyList<CalculationValueSnapshotResponseDto> Inputs)
{
    public static CalculationResultResponseDto From(CalculationResultDetails details) => new(
        details.VariantId,
        details.VariantName,
        details.ExternalId,
        details.Score,
        details.Rank,
        details.ParetoFront,
        details.IsParetoOptimal,
        details.ParetoSecondaryScore,
        details.Inputs.Select(CalculationValueSnapshotResponseDto.From).ToArray());
}

public sealed record CalculationValueSnapshotResponseDto(Guid CriterionId, decimal Value)
{
    public static CalculationValueSnapshotResponseDto From(CalculationValueSnapshotDetails details) =>
        new(details.CriterionId, details.Value);
}

public sealed record CalculationHistoryResponseDto(
    Guid Id,
    Guid SiteId,
    string Method,
    DateTime CreatedAt,
    string? AlgorithmVersion,
    int ResultCount)
{
    public static CalculationHistoryResponseDto From(CalculationHistoryItem item) => new(
        item.Id,
        item.SiteId,
        item.Method.ToString(),
        item.CreatedAt,
        item.AlgorithmVersion,
        item.ResultCount);
}
