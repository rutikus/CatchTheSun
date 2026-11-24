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
            Value = 1m / value
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
}
