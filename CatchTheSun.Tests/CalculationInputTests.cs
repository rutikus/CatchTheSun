using CatchTheSun.Core.CalculationEngine.Models;
using CatchTheSun.Core.Entities;
using Xunit;

namespace CatchTheSun.Tests;

public class CalculationInputTests
{
    [Fact]
    public void ConstructorCanonicalizesCriteriaAndReordersAlternativeValues()
    {
        var criterion2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var criterion1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var variantId = Guid.NewGuid();
        var input = new CalculationInput(
            CalculationMethod.TOPSIS,
            [
                new CalculationCriterionInput(criterion2, CriterionDirection.Minimize, null, 0.4m),
                new CalculationCriterionInput(criterion1, CriterionDirection.Maximize, null, 0.6m)
            ],
            [new CalculationAlternativeInput(variantId, [22.5m, 11.25m])],
            [],
            new CalculationAlgorithmOptions());

        Assert.Equal(new[] { criterion1, criterion2 }, input.Criteria.Select(x => x.CriterionId));
        var alternative = Assert.Single(input.Alternatives);
        Assert.Equal(variantId, alternative.VariantId);
        Assert.Equal(new[] { 11.25m, 22.5m }, alternative.Values);
        Assert.Equal(new decimal?[] { 0.6m, 0.4m }, input.Criteria.Select(x => x.Weight));
    }

    [Fact]
    public void ConstructorSortsComparisonsByCanonicalCriterionPair()
    {
        var criterion1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var criterion2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var criterion3 = Guid.Parse("00000000-0000-0000-0000-000000000003");
        var input = new CalculationInput(
            CalculationMethod.AHP,
            [
                new CalculationCriterionInput(criterion1, CriterionDirection.Maximize, null, null),
                new CalculationCriterionInput(criterion2, CriterionDirection.Minimize, null, null),
                new CalculationCriterionInput(criterion3, CriterionDirection.Target, 5m, null)
            ],
            [],
            [
                new CalculationCriterionComparisonInput(criterion2, criterion3, 3m),
                new CalculationCriterionComparisonInput(criterion1, criterion3, 5m),
                new CalculationCriterionComparisonInput(criterion1, criterion2, 2m)
            ],
            new CalculationAlgorithmOptions());

        Assert.Equal(
            [(criterion1, criterion2), (criterion1, criterion3), (criterion2, criterion3)],
            input.Comparisons.Select(x => (x.CriterionAId, x.CriterionBId)));
    }

    [Fact]
    public void ConstructorRejectsAlternativeWithWrongNumberOfValues()
    {
        var criterionId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => new CalculationInput(
            CalculationMethod.Pareto,
            [new CalculationCriterionInput(criterionId, CriterionDirection.Maximize, null, null)],
            [new CalculationAlternativeInput(Guid.NewGuid(), [])],
            [],
            new CalculationAlgorithmOptions()));
    }
}
