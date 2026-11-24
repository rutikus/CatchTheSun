using CatchTheSun.Core.CalculationEngine;
using CatchTheSun.Core.CalculationEngine.Algorithms;
using CatchTheSun.Core.CalculationEngine.Models;
using CatchTheSun.Core.Entities;
using Xunit;

namespace CatchTheSun.Tests;

public class ParetoAlgorithmTests
{
    private readonly ParetoAlgorithm _algorithm = new();

    [Fact]
    public void SingleMaximizeCriterionAssignsFrontsByDescendingValue()
    {
        var output = Run([Criterion(1, CriterionDirection.Maximize)], [[1m], [3m], [2m]]);

        Assert.Equal(new[] { (Id(11), 1), (Id(12), 2), (Id(10), 3) },
            output.Results.Select(x => (x.VariantId, x.ParetoFront!.Value)));
    }

    [Fact]
    public void MultipleCriteriaReturnOnlyNonDominatedVariantsOnFirstFront()
    {
        var output = Run(
            [Criterion(1, CriterionDirection.Maximize), Criterion(2, CriterionDirection.Maximize)],
            [[1m, 0m], [0m, 1m]]);

        Assert.All(output.Results, result => Assert.Equal(1, result.ParetoFront));
        Assert.All(output.Results, result => Assert.True(result.IsParetoOptimal));
    }

    [Fact]
    public void MinimizeCriterionPrefersLowerValues()
    {
        var output = Run([Criterion(1, CriterionDirection.Minimize)], [[3m], [1m], [2m]]);

        Assert.Equal(new[] { Id(11), Id(12), Id(10) }, output.Results.Select(x => x.VariantId));
        Assert.Equal(new[] { 1, 2, 3 }, output.Results.Select(x => x.ParetoFront!.Value));
    }

    [Fact]
    public void TargetCriterionPrefersValuesClosestToTarget()
    {
        var output = Run(
            [Criterion(1, CriterionDirection.Target, target: 10m)],
            [[0m], [10m], [20m]]);

        Assert.Equal(new[] { Id(11), Id(10), Id(12) }, output.Results.Select(x => x.VariantId));
        Assert.Equal(new[] { 1, 2, 2 }, output.Results.Select(x => x.ParetoFront!.Value));
    }

    [Fact]
    public void NegativeAndZeroValuesAreComparedExactly()
    {
        var output = Run([Criterion(1, CriterionDirection.Maximize)], [[-2m], [0m], [-1m]]);

        Assert.Equal(new[] { Id(11), Id(12), Id(10) }, output.Results.Select(x => x.VariantId));
        Assert.Equal(new[] { 1, 2, 3 }, output.Results.Select(x => x.ParetoFront!.Value));
    }

    [Fact]
    public void DifferenceOfOneMicroIsARealDominanceDifference()
    {
        var output = Run([Criterion(1, CriterionDirection.Maximize)], [[0m], [0.000001m]]);

        Assert.Equal(new[] { 1, 2 }, output.Results.Select(x => x.ParetoFront!.Value));
        Assert.Equal(Id(11), output.Results[0].VariantId);
    }

    [Fact]
    public void IdenticalVariantsDoNotDominateEachOtherAndShareFront()
    {
        var output = Run(
            [Criterion(1, CriterionDirection.Maximize), Criterion(2, CriterionDirection.Minimize)],
            [[-5m, 2m], [-5m, 2m], [-5m, 2m]]);

        Assert.All(output.Results, result => Assert.Equal(1, result.ParetoFront));
        Assert.All(output.Results, result => Assert.True(result.IsParetoOptimal));
        Assert.Equal(new[] { Id(10), Id(11), Id(12) }, output.Results.Select(x => x.VariantId));
    }

    [Fact]
    public void PartiallyEqualVariantCanStillBeDominated()
    {
        var output = Run(
            [Criterion(1, CriterionDirection.Maximize), Criterion(2, CriterionDirection.Maximize)],
            [[1m, 4m], [1m, 5m]]);

        Assert.Equal(new[] { Id(11), Id(10) }, output.Results.Select(x => x.VariantId));
        Assert.Equal(new[] { 1, 2 }, output.Results.Select(x => x.ParetoFront!.Value));
    }

    [Fact]
    public void DominatedChainIsAssignedToSuccessiveFronts()
    {
        var output = Run(
            [Criterion(1, CriterionDirection.Maximize), Criterion(2, CriterionDirection.Maximize)],
            [[0m, 0m], [1m, 1m], [2m, 2m], [3m, 3m]]);

        Assert.Equal(new[] { Id(13), Id(12), Id(11), Id(10) }, output.Results.Select(x => x.VariantId));
        Assert.Equal(new[] { 1, 2, 3, 4 }, output.Results.Select(x => x.ParetoFront!.Value));
        Assert.Equal(new[] { true, false, false, false }, output.Results.Select(x => x.IsParetoOptimal!.Value));
    }

    [Fact]
    public void ReferenceExampleProducesThreeExpectedFronts()
    {
        var output = Run(
            [
                Criterion(1, CriterionDirection.Maximize),
                Criterion(2, CriterionDirection.Minimize),
                Criterion(3, CriterionDirection.Target, target: 5m)
            ],
            [
                [10m, 5m, 5m],
                [8m, 4m, 5m],
                [7m, 6m, 6m],
                [6m, 7m, 8m],
                [6m, 3m, 8m]
            ]);

        Assert.Equal(new[] { Id(10), Id(11), Id(14), Id(12), Id(13) }, output.Results.Select(x => x.VariantId));
        Assert.Equal(new[] { 1, 1, 1, 2, 3 }, output.Results.Select(x => x.ParetoFront!.Value));
        Assert.Equal(new[] { true, true, true, false, false }, output.Results.Select(x => x.IsParetoOptimal!.Value));
    }

    [Fact]
    public void NoWeightsProduceNullSecondaryScoresAndNullCriterionWeights()
    {
        var output = Run(
            [Criterion(1, CriterionDirection.Maximize), Criterion(2, CriterionDirection.Minimize)],
            [[1m, 2m], [2m, 1m], [3m, 3m]]);

        Assert.All(output.Results, result =>
        {
            Assert.Null(result.Score);
            Assert.Null(result.ParetoSecondaryScore);
            Assert.Null(result.Rank);
        });
        Assert.Equal(2, output.CriterionWeights.Count);
        Assert.All(output.CriterionWeights, criterionWeight => Assert.Null(criterionWeight.Weight));
    }

    [Fact]
    public void WeightedSecondaryScoreUsesFullVariantSetAndFrontStillSortsFirst()
    {
        var output = Run(
            [
                Criterion(1, CriterionDirection.Maximize, 0.5m),
                Criterion(2, CriterionDirection.Minimize, 0m),
                Criterion(3, CriterionDirection.Target, 0.5m, 5m)
            ],
            [
                [10m, 5m, 5m],
                [8m, 4m, 5m],
                [7m, 6m, 6m],
                [6m, 7m, 8m],
                [6m, 3m, 8m]
            ]);

        var variantE = Assert.Single(output.Results, x => x.VariantId == Id(14));
        var variantC = Assert.Single(output.Results, x => x.VariantId == Id(12));
        Assert.Equal(1, variantE.ParetoFront);
        Assert.Equal(2, variantC.ParetoFront);
        Assert.True(variantC.ParetoSecondaryScore > variantE.ParetoSecondaryScore);
        Assert.True(Array.IndexOf(output.Results.ToArray(), variantE) < Array.IndexOf(output.Results.ToArray(), variantC));
    }

    [Fact]
    public void SecondaryWeightsChangeOnlyOrderWithinFront()
    {
        var criteria = new[]
        {
            Criterion(1, CriterionDirection.Maximize, 0.9m),
            Criterion(2, CriterionDirection.Maximize, 0.1m)
        };
        var values = new[] { new[] { 10m, 0m }, new[] { 0m, 10m }, new[] { 4m, 4m } };
        var first = Run(criteria, values);
        var swapped = Run(
            [Criterion(1, CriterionDirection.Maximize, 0.1m), Criterion(2, CriterionDirection.Maximize, 0.9m)],
            values);

        Assert.All(first.Results, x => Assert.Equal(1, x.ParetoFront));
        Assert.All(swapped.Results, x => Assert.Equal(1, x.ParetoFront));
        Assert.Equal(new[] { Id(10), Id(12), Id(11) }, first.Results.Select(x => x.VariantId));
        Assert.Equal(new[] { Id(11), Id(12), Id(10) }, swapped.Results.Select(x => x.VariantId));
    }

    [Fact]
    public void ZeroWeightIsAllowedAndDoesNotAffectSecondaryScore()
    {
        var output = Run(
            [Criterion(1, CriterionDirection.Maximize, 1m), Criterion(2, CriterionDirection.Maximize, 0m)],
            [[0m, 10m], [10m, 0m], [5m, 5m]]);

        Assert.Equal(1m, output.CriterionWeights[0].Weight);
        Assert.Equal(0m, output.CriterionWeights[1].Weight);
        Assert.All(output.Results, x => Assert.NotNull(x.ParetoSecondaryScore));
    }

    [Fact]
    public void ConstantCriterionHasHalfUtilityInSecondaryScore()
    {
        var output = Run(
            [Criterion(1, CriterionDirection.Maximize, 0.5m), Criterion(2, CriterionDirection.Minimize, 0.5m)],
            [[0m, 5m], [10m, 5m]]);

        Assert.Equal(0.25m, output.Results.Single(x => x.VariantId == Id(10)).ParetoSecondaryScore);
        Assert.Equal(0.75m, output.Results.Single(x => x.VariantId == Id(11)).ParetoSecondaryScore);
    }

    [Fact]
    public void SecondaryUtilityHandlesNegativeValuesAndTarget()
    {
        var output = Run(
            [
                Criterion(1, CriterionDirection.Maximize, 0.5m),
                Criterion(2, CriterionDirection.Target, 0.5m, 0m)
            ],
            [[-10m, -10m], [0m, 0m], [-5m, 5m]]);

        var best = output.Results.Single(x => x.VariantId == Id(11));
        Assert.Equal(1m, best.ParetoSecondaryScore);
        Assert.All(output.Results, x => Assert.InRange(x.ParetoSecondaryScore!.Value, 0m, 1m));
    }

    [Fact]
    public void EqualSecondaryScoresUseVariantIdAsStableTieBreaker()
    {
        var output = Run(
            [Criterion(1, CriterionDirection.Maximize, 1m)],
            [[0m], [0m], [0m]]);

        Assert.Equal(new[] { Id(10), Id(11), Id(12) }, output.Results.Select(x => x.VariantId));
        Assert.All(output.Results, x => Assert.Equal(0.5m, x.ParetoSecondaryScore));
    }

    [Fact]
    public void ChangingWeightsNeverChangesDominanceFronts()
    {
        var values = new[] { new[] { 0m, 0m }, new[] { 1m, 1m }, new[] { 2m, 2m } };
        var first = Run(
            [Criterion(1, CriterionDirection.Maximize, 0.9m), Criterion(2, CriterionDirection.Maximize, 0.1m)],
            values);
        var second = Run(
            [Criterion(1, CriterionDirection.Maximize, 0.1m), Criterion(2, CriterionDirection.Maximize, 0.9m)],
            values);

        Assert.Equal(first.Results.OrderBy(x => x.VariantId).Select(x => x.ParetoFront),
            second.Results.OrderBy(x => x.VariantId).Select(x => x.ParetoFront));
    }

    [Fact]
    public void WeightRoundingResidualUsesFirstCanonicalCriterionOnTie()
    {
        var output = Run(
            [
                Criterion(1, CriterionDirection.Maximize, 0.44444444m),
                Criterion(2, CriterionDirection.Maximize, 0.44444444m),
                Criterion(3, CriterionDirection.Maximize, 0.11111111m)
            ],
            [[0m, 0m, 0m], [1m, 1m, 1m]]);

        Assert.Equal(0.44444445m, output.CriterionWeights[0].Weight);
        Assert.Equal(0.44444444m, output.CriterionWeights[1].Weight);
        Assert.Equal(1m, output.CriterionWeights.Sum(x => x.Weight!.Value));
    }

    [Fact]
    public void WeightSumAtToleranceIsAcceptedAndCorrectedToExactlyOne()
    {
        var output = Run(
            [Criterion(1, CriterionDirection.Maximize, 0.5000005m), Criterion(2, CriterionDirection.Maximize, 0.5000005m)],
            [[0m, 0m], [1m, 1m]]);

        Assert.Equal(new decimal?[] { 0.5m, 0.5m }, output.CriterionWeights.Select(x => x.Weight));
        Assert.Equal(1m, output.CriterionWeights.Sum(x => x.Weight!.Value));

        var outsideTolerance = Assert.Throws<CalculationAlgorithmException>(() => Run(
            [Criterion(1, CriterionDirection.Maximize, 0.5000006m), Criterion(2, CriterionDirection.Maximize, 0.5000005m)],
            [[0m, 0m], [1m, 1m]]));
        Assert.Contains("sum to 1", outsideTolerance.Message);
    }

    [Fact]
    public void CriterionPermutationPreservesWeightsScoresAndFronts()
    {
        var first = Run(
            [Criterion(1, CriterionDirection.Maximize, 0.2m), Criterion(2, CriterionDirection.Minimize, 0.3m), Criterion(3, CriterionDirection.Target, 0.5m, 10m)],
            [[0m, 10m, 10m], [10m, 0m, 20m], [5m, 5m, 10m]]);
        var permuted = Run(
            [Criterion(3, CriterionDirection.Target, 0.5m, 10m), Criterion(1, CriterionDirection.Maximize, 0.2m), Criterion(2, CriterionDirection.Minimize, 0.3m)],
            [[10m, 0m, 10m], [20m, 10m, 0m], [10m, 5m, 5m]]);

        Assert.Equal(first.Results, permuted.Results);
        Assert.Equal(first.CriterionWeights, permuted.CriterionWeights);
    }

    [Fact]
    public void VariantPermutationAndRepeatedRunAreDeterministic()
    {
        var criteria = new[] { Criterion(1, CriterionDirection.Maximize), Criterion(2, CriterionDirection.Minimize) };
        var first = Run(criteria, [[1m, 8m], [3m, 4m], [2m, 9m]], variantIds: [12, 10, 11]);
        var permuted = Run(criteria,
            [[2m, 9m], [1m, 8m], [3m, 4m]],
            variantIds: [11, 12, 10]);
        var repeated = Run(criteria, [[1m, 8m], [3m, 4m], [2m, 9m]], variantIds: [12, 10, 11]);

        Assert.Equal(first.Results, permuted.Results);
        Assert.Equal(first.Results, repeated.Results);
    }

    [Fact]
    public void RejectsLessThanTwoVariantsAndNoCriteria()
    {
        var tooFew = Assert.Throws<CalculationAlgorithmException>(() => Run(
            [Criterion(1, CriterionDirection.Maximize)],
            [[0m]]));
        Assert.Contains("at least two", tooFew.Message);

        var noCriteria = Assert.Throws<CalculationAlgorithmException>(() => Run([], [[], []]));
        Assert.Contains("at least one criterion", noCriteria.Message);
    }

    [Fact]
    public void RejectsDuplicateVariantsAHPComparisonsAndUnsupportedDirection()
    {
        var duplicateVariants = new CalculationInput(
            CalculationMethod.Pareto,
            [Criterion(1, CriterionDirection.Maximize)],
            [new CalculationAlternativeInput(Id(10), [0m]), new CalculationAlternativeInput(Id(10), [1m])],
            [],
            new CalculationAlgorithmOptions());
        var duplicate = Assert.Throws<CalculationAlgorithmException>(() => _algorithm.Calculate(duplicateVariants, default));
        Assert.Contains("Variant IDs must be unique", duplicate.Message);

        var comparisonInput = NewInput(
            [Criterion(1, CriterionDirection.Maximize)],
            [[0m], [1m]],
            [new CalculationCriterionComparisonInput(Id(1), Id(1), 3m)]);
        var comparisons = Assert.Throws<CalculationAlgorithmException>(() => _algorithm.Calculate(comparisonInput, default));
        Assert.Contains("does not accept", comparisons.Message);

        var invalidDirection = Assert.Throws<CalculationAlgorithmException>(() => Run(
            [new CalculationCriterionInput(Id(1), (CriterionDirection)99, null, null)],
            [[0m], [1m]]));
        Assert.Contains("unsupported direction", invalidDirection.Message);
    }

    [Fact]
    public void RejectsMissingTargetAndValuesOutsideNumericRange()
    {
        var missingTarget = Assert.Throws<CalculationAlgorithmException>(() => Run(
            [Criterion(1, CriterionDirection.Target)],
            [[0m], [1m]]));
        Assert.Contains("TargetValue", missingTarget.Message);

        var tooLarge = Assert.Throws<CalculationAlgorithmException>(() => Run(
            [Criterion(1, CriterionDirection.Maximize)],
            [[0m], [100000000000000m]]));
        Assert.Contains("numeric(20,6)", tooLarge.Message);
    }

    [Fact]
    public void RejectsPartiallySuppliedInvalidAndOutOfRangeWeights()
    {
        var partial = Assert.Throws<CalculationAlgorithmException>(() => Run(
            [Criterion(1, CriterionDirection.Maximize, 1m), Criterion(2, CriterionDirection.Maximize)],
            [[0m, 0m], [1m, 1m]]));
        Assert.Contains("all criteria or omitted", partial.Message);

        var negative = Assert.Throws<CalculationAlgorithmException>(() => Run(
            [Criterion(1, CriterionDirection.Maximize, -0.1m), Criterion(2, CriterionDirection.Maximize, 1.1m)],
            [[0m, 0m], [1m, 1m]]));
        Assert.Contains("must be in [0,1]", negative.Message);

        var sum = Assert.Throws<CalculationAlgorithmException>(() => Run(
            [Criterion(1, CriterionDirection.Maximize, 0.7m), Criterion(2, CriterionDirection.Maximize, 0.7m)],
            [[0m, 0m], [1m, 1m]]));
        Assert.Contains("sum to 1", sum.Message);
    }

    [Fact]
    public void InputModelRejectsIncompleteDecisionMatrix()
    {
        Assert.Throws<ArgumentException>(() => new CalculationInput(
            CalculationMethod.Pareto,
            [Criterion(1, CriterionDirection.Maximize), Criterion(2, CriterionDirection.Maximize)],
            [new CalculationAlternativeInput(Id(10), [1m])],
            [],
            new CalculationAlgorithmOptions()));
    }

    [Fact]
    public void InputModelRejectsDuplicateCriterionIds()
    {
        var criterion = Criterion(1, CriterionDirection.Maximize);

        Assert.Throws<ArgumentException>(() => NewInput(
            [criterion, criterion],
            [[0m, 0m], [1m, 1m]],
            []));
    }

    [Fact]
    public void HonorsCancellationTokenDuringPairwiseSorting()
    {
        const int variantCount = 1_500;
        var criteria = new[] { Criterion(1, CriterionDirection.Maximize), Criterion(2, CriterionDirection.Maximize) };
        var alternatives = Enumerable.Range(0, variantCount)
            .Select(index => new CalculationAlternativeInput(Id(index + 1), [index % 97, index % 89]))
            .ToArray();
        var input = new CalculationInput(CalculationMethod.Pareto, criteria, alternatives, [], new CalculationAlgorithmOptions());
        using var cancellation = new CancellationTokenSource();
        var started = new ManualResetEventSlim();

        var calculation = Task.Run(() =>
        {
            started.Set();
            return _algorithm.Calculate(input, cancellation.Token);
        });
        started.Wait();
        cancellation.CancelAfter(TimeSpan.FromMilliseconds(1));

        Assert.ThrowsAny<OperationCanceledException>(() => calculation.GetAwaiter().GetResult());
    }

    private CalculationAlgorithmOutput Run(
        IReadOnlyList<CalculationCriterionInput> criteria,
        IReadOnlyList<decimal[]> values,
        IReadOnlyList<CalculationCriterionComparisonInput>? comparisons = null,
        IReadOnlyList<int>? variantIds = null)
    {
        return _algorithm.Calculate(NewInput(criteria, values, comparisons ?? [], variantIds), CancellationToken.None);
    }

    private static CalculationInput NewInput(
        IEnumerable<CalculationCriterionInput> criteria,
        IEnumerable<decimal[]> values,
        IEnumerable<CalculationCriterionComparisonInput> comparisons,
        IReadOnlyList<int>? variantIds = null)
    {
        var suppliedValues = values.ToArray();
        variantIds ??= Enumerable.Range(0, suppliedValues.Length).Select(index => 10 + index).ToArray();
        if (variantIds.Count != suppliedValues.Length)
            throw new ArgumentException("Variant ID count must match alternative count.", nameof(variantIds));

        var alternatives = suppliedValues.Select((criterionValues, index) =>
            new CalculationAlternativeInput(Id(variantIds[index]), criterionValues));
        return new CalculationInput(CalculationMethod.Pareto, criteria, alternatives, comparisons, new CalculationAlgorithmOptions());
    }

    private static CalculationCriterionInput Criterion(
        int id,
        CriterionDirection direction,
        decimal? weight = null,
        decimal? target = null) =>
        new(Id(id), direction, target, weight);

    private static Guid Id(int id) => Guid.Parse($"00000000-0000-0000-0000-{id:D12}");
}
