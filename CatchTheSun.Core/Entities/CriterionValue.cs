namespace CatchTheSun.Core.Entities;

public class CriterionValue
{
    public Guid Id { get; set; }
    public Guid VariantId { get; set; }
    public Guid CriterionId { get; set; }
    public decimal Value { get; set; }

    public Variant Variant { get; set; } = null!;
    public Criterion Criterion { get; set; } = null!;
}
