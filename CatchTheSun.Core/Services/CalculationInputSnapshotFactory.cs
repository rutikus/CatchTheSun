using CatchTheSun.Core.Entities;

namespace CatchTheSun.Core.Services;

public static class CalculationInputSnapshotFactory
{
    public static IReadOnlyList<CalculationResultCriterionSnapshot> Capture(CalculationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(result.Calculation);
        ArgumentNullException.ThrowIfNull(result.Variant);

        var valuesByCriterion = result.Variant.CriterionValues.ToDictionary(x => x.CriterionId);
        var snapshots = new List<CalculationResultCriterionSnapshot>();

        foreach (var setting in result.Calculation.Criteria)
        {
            if (setting.CalculationId != result.CalculationId)
                throw new InvalidOperationException("Calculation criterion belongs to a different calculation.");

            if (!valuesByCriterion.TryGetValue(setting.CriterionId, out var inputValue))
            {
                if (setting.Criterion.IsRequired)
                    throw new InvalidOperationException($"Required criterion {setting.CriterionId} has no value for variant {result.VariantId}.");

                continue;
            }

            snapshots.Add(new CalculationResultCriterionSnapshot
            {
                CalculationResultId = result.Id,
                CalculationId = result.CalculationId,
                CriterionId = setting.CriterionId,
                ValueSnapshot = inputValue.Value,
                DirectionSnapshot = setting.DirectionSnapshot,
                TargetValueSnapshot = setting.TargetValueSnapshot,
                WeightSnapshot = setting.Weight
            });
        }

        return snapshots;
    }
}
