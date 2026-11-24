namespace CatchTheSun.Core.Entities;

public class CalculationResult
{
    public Guid Id { get; set; }
    public Guid CalculationId { get; set; }
    public Guid VariantId { get; set; }
    public decimal? Score { get; set; }
    public int? Rank { get; set; }
    public bool? IsParetoOptimal { get; set; }
    public int? ParetoFront { get; set; }

    public Calculation Calculation { get; set; } = null!;
    public Variant Variant { get; set; } = null!;
}
