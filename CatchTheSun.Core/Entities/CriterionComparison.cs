namespace CatchTheSun.Core.Entities;

public class CriterionComparison
{
    public Guid Id { get; set; }
    public Guid CalculationId { get; set; }
    public Guid CriterionAId { get; set; }
    public Guid CriterionBId { get; set; }
    public decimal Value { get; private set; }

    public Calculation Calculation { get; set; } = null!;
    public Criterion CriterionA { get; set; } = null!;
    public Criterion CriterionB { get; set; } = null!;
    public CalculationCriterion CalculationCriterionA { get; set; } = null!;
    public CalculationCriterion CalculationCriterionB { get; set; } = null!;

    private CriterionComparison() { }

    public static CriterionComparison Create(Guid calculationId, Guid criterionAId, Guid criterionBId, decimal value)
    {
        if (calculationId == Guid.Empty)
            throw new ArgumentException("Calculation ID must not be empty.", nameof(calculationId));
        if (criterionAId == criterionBId)
            throw new ArgumentException("A criterion cannot be compared with itself.", nameof(criterionBId));
        if (!IsSaatyValue(value))
            throw new ArgumentOutOfRangeException(nameof(value), "Value must be on the Saaty scale from 1 to 9 or its reciprocal.");

        var aKey = criterionAId.ToString("N");
        var bKey = criterionBId.ToString("N");
        if (string.CompareOrdinal(aKey, bKey) < 0)
        {
            return new CriterionComparison
            {
                CalculationId = calculationId,
                CriterionAId = criterionAId,
                CriterionBId = criterionBId,
                Value = value
            };
        }

        return new CriterionComparison
        {
            CalculationId = calculationId,
            CriterionAId = criterionBId,
            CriterionBId = criterionAId,
                Value = GetReciprocal(value)
        };
    }

    private static bool IsSaatyValue(decimal value)
    {
        if (value is >= 1m and <= 9m && decimal.Truncate(value) == value)
            return true;

        for (var scale = 2; scale <= 9; scale++)
        {
            if (value == 1m / scale)
                return true;
        }

        return false;
    }

    private static decimal GetReciprocal(decimal value) => value switch
    {
        1m => 1m,
        2m => 0.5m,
        3m => 1m / 3m,
        4m => 0.25m,
        5m => 0.2m,
        6m => 1m / 6m,
        7m => 1m / 7m,
        8m => 0.125m,
        9m => 1m / 9m,
        0.5m => 2m,
        1m / 3m => 3m,
        0.25m => 4m,
        0.2m => 5m,
        1m / 6m => 6m,
        1m / 7m => 7m,
        0.125m => 8m,
        1m / 9m => 9m,
        _ => throw new ArgumentOutOfRangeException(nameof(value), "Value must be on the Saaty scale from 1 to 9 or its reciprocal.")
    };
}
