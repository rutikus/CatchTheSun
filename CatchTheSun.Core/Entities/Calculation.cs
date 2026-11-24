namespace CatchTheSun.Core.Entities;

public class Calculation
{
    public Guid Id { get; set; }
    public Guid SiteId { get; set; }
    public CalculationMethod Method { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? AlgorithmVersion { get; set; }
    public string? OptionsJson { get; set; }
    public double? ConsistencyRatio { get; set; }
    public double? LambdaMax { get; set; }
    public int? IterationCount { get; set; }

    public Site Site { get; set; } = null!;
    public ICollection<CalculationCriterion> Criteria { get; set; } = new List<CalculationCriterion>();
    public ICollection<CriterionComparison> CriterionComparisons { get; set; } = new List<CriterionComparison>();
    public ICollection<CalculationResult> Results { get; set; } = new List<CalculationResult>();

}
