using CatchTheSun.Core.Entities;

namespace CatchTheSun.Core.CalculationEngine.Models;

public sealed record CreateCalculationCommand(
    Guid SiteId,
    IReadOnlyList<Guid> VariantIds,
    CalculationMethod Method,
    IReadOnlyList<CalculationCriterionSelection> Criteria,
    IReadOnlyList<CalculationCriterionComparisonInput> Comparisons);

public sealed record CalculationCriterionSelection(Guid CriterionId, decimal? Weight);
