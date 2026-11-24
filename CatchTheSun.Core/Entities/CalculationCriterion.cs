namespace CatchTheSun.Core.Entities;

public class CalculationCriterion
{
    public Guid Id { get; set; }
    public Guid CalculationId { get; set; }
    public Guid CriterionId { get; set; }
    public decimal? Weight { get; set; }
    public string NameSnapshot { get; set; } = string.Empty;
    public string? UnitSnapshot { get; set; }
    public CriterionDirection DirectionSnapshot { get; set; }
    public decimal? TargetValueSnapshot { get; set; }
    public bool IsRequiredSnapshot { get; set; }

    public Calculation Calculation { get; set; } = null!;
    public Criterion Criterion { get; set; } = null!;
}
