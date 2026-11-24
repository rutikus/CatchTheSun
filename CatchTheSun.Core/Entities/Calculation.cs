namespace CatchTheSun.Core.Entities;

public class Calculation
{
    public Guid Id { get; set; }
    public Guid SiteId { get; set; }
    public CalculationMethod Method { get; set; }
    public DateTime CreatedAt { get; set; }

    public Site Site { get; set; } = null!;
    public ICollection<CalculationCriterion> Criteria { get; set; } = new List<CalculationCriterion>();
    public ICollection<CriterionComparison> CriterionComparisons { get; set; } = new List<CriterionComparison>();
    public ICollection<CalculationResult> Results { get; set; } = new List<CalculationResult>();

}
