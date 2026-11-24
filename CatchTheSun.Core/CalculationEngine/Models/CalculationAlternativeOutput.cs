namespace CatchTheSun.Core.CalculationEngine.Models;

public sealed record CalculationAlternativeOutput(
    Guid VariantId,
    decimal? Score,
    int? Rank,
    int? ParetoFront,
    bool? IsParetoOptimal,
    decimal? ParetoSecondaryScore);
