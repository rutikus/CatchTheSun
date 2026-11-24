using CatchTheSun.Core.Entities;

namespace CatchTheSun.Core.CalculationEngine.Models;

public sealed record CalculationCriterionInput(
    Guid CriterionId,
    CriterionDirection Direction,
    decimal? TargetValue,
    decimal? Weight);
