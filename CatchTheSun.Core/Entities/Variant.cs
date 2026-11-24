namespace CatchTheSun.Core.Entities;

public class Variant
{
    public Guid Id { get; set; }
    public Guid SiteId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ExternalId { get; set; }
    public DateTime CreatedAt { get; set; }

    public Site Site { get; set; } = null!;
    public ICollection<CriterionValue> CriterionValues { get; set; } = new List<CriterionValue>();
    public ICollection<CalculationResult> CalculationResults { get; set; } = new List<CalculationResult>();
}
