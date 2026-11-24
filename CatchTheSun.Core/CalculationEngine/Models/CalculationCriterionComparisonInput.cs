namespace CatchTheSun.Core.CalculationEngine.Models;

public sealed record CalculationCriterionComparisonInput(
    Guid CriterionAId,
    Guid CriterionBId,
    decimal Value);
