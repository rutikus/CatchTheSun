namespace CatchTheSun.Core.Entities;

public class CalculationResultCriterionSnapshot
{
    public Guid Id { get; set; }
    public Guid CalculationResultId { get; set; }
    public Guid CalculationId { get; set; }
    public Guid CriterionId { get; set; }
    public decimal ValueSnapshot { get; set; }
    public CriterionDirection DirectionSnapshot { get; set; }
    public decimal? TargetValueSnapshot { get; set; }
    public decimal WeightSnapshot { get; set; }

    public CalculationResult CalculationResult { get; set; } = null!;
}
