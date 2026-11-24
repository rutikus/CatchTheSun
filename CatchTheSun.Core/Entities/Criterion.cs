namespace CatchTheSun.Core.Entities;

public class Criterion
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Unit { get; set; }
    public CriterionDirection Direction { get; set; }
    public decimal? TargetValue { get; set; }
    public bool IsRequired { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<CriterionValue> Values { get; set; } = new List<CriterionValue>();
    public ICollection<CalculationCriterion> CalculationCriteria { get; set; } = new List<CalculationCriterion>();
}
