namespace CatchTheSun.Core.Entities;

public class CriterionComparison
{
    public Guid Id { get; set; }
    public Guid CalculationId { get; set; }
    public Guid CriterionAId { get; set; }
    public Guid CriterionBId { get; set; }
    public decimal Value { get; set; }

    public Calculation Calculation { get; set; } = null!;
    public Criterion CriterionA { get; set; } = null!;
    public Criterion CriterionB { get; set; } = null!;
}
