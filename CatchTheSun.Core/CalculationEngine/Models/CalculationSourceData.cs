using CatchTheSun.Core.Entities;

namespace CatchTheSun.Core.CalculationEngine.Models;

public sealed record CalculationSourceData(
    Site? Site,
    IReadOnlyList<Variant> Variants,
    IReadOnlyList<Criterion> Criteria,
    IReadOnlyList<CriterionValue> CriterionValues);
