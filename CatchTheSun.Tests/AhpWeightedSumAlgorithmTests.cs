using CatchTheSun.Core.CalculationEngine;
using CatchTheSun.Core.CalculationEngine.Algorithms;
using CatchTheSun.Core.CalculationEngine.Models;
using CatchTheSun.Core.Entities;
using Xunit;

namespace CatchTheSun.Tests;

public class AhpWeightedSumAlgorithmTests
{
    private readonly AhpWeightedSumAlgorithm _algorithm = new();

    [Fact]
    public void SingleCriterionUsesWeightOneZeroCrAndNoIterations()
    {
        var criterion = Criterion(1);
        var output = Run([criterion], [[10m], [20m]], []);

        Assert.Equal(1m, Assert.Single(output.CriterionWeights).Weight!.Value);
        Assert.Equal(0d, output.Diagnostics.ConsistencyRatio);
        Assert.Equal(1d, output.Diagnostics.LambdaMax);
        Assert.Equal(0, output.Diagnostics.IterationCount);
        Assert.Equal(new decimal?[] { 0m, 1m }, output.Results.Select(x => x.Score));
    }

    [Fact]
    public void TwoCriteriaHaveZeroConsistencyRatio()
    {
        var criteria = new[] { Criterion(1), Criterion(2) };
        var output = Run(criteria, [[0m, 0m], [1m, 1m]], [Compare(1, 2, 9m)]);

        Assert.Equal(0d, output.Diagnostics.ConsistencyRatio);
        Assert.InRange(output.Diagnostics.LambdaMax!.Value, 2d - 1e-12, 2d + 1e-12);
        Assert.Equal(1m, output.CriterionWeights.Sum(x => x.Weight!.Value));
    }

    [Fact]
    public void ConsistentThreeCriterionExampleProducesExpectedPrincipalWeights()
    {
        var output = Run(
            [Criterion(1), Criterion(2), Criterion(3)],
            [[0m, 0m, 0m], [1m, 1m, 1m]],
            [Compare(1, 2, 2m), Compare(1, 3, 6m), Compare(2, 3, 3m)]);

        Assert.Equal(new[] { 0.6m, 0.3m, 0.1m }, output.CriterionWeights.Select(x => x.Weight!.Value));
        Assert.InRange(output.Diagnostics.ConsistencyRatio!.Value, 0d, 1e-12);
        Assert.InRange(output.Diagnostics.LambdaMax!.Value, 3d - 1e-12, 3d + 1e-12);
        Assert.InRange(output.Diagnostics.IterationCount!.Value, 1, 10_000);
    }

    [Fact]
    public void AcceptsMaximumOfTwentyCriteria()
    {
        var criteria = Enumerable.Range(1, 20).Select(id => Criterion(id)).ToArray();
        var comparisons = AllPairs(criteria.Select(x => x.CriterionId).ToArray(), 1m);
        var values = Enumerable.Repeat(0m, criteria.Length).ToArray();
        var output = Run(criteria, [values, values], comparisons);

        Assert.Equal(20, output.CriterionWeights.Count);
        Assert.Equal(1m, output.CriterionWeights.Sum(x => x.Weight!.Value));
        Assert.All(output.CriterionWeights, x => Assert.Equal(0.05m, x.Weight!.Value));
    }

    [Fact]
    public void RejectsTwentyOneCriteria()
    {
        var criteria = Enumerable.Range(1, 21).Select(id => Criterion(id)).ToArray();
        var values = Enumerable.Repeat(0m, criteria.Length).ToArray();
        var exception = Assert.Throws<CalculationAlgorithmException>(() =>
        {
            Run(criteria, [values, values], []);
        });

        Assert.Equal("calculation.ahp.invalid_input", exception.Code);
    }

    [Fact]
    public void RequiresAtLeastTwoVariants()
    {
        var input = new CalculationInput(
            CalculationMethod.AHP,
            [Criterion(1)],
            [new CalculationAlternativeInput(Id(10), [1m])],
            [],
            new CalculationAlgorithmOptions());

        var exception = Assert.Throws<CalculationAlgorithmException>(() => _algorithm.Calculate(input, default));

        Assert.Contains("at least two", exception.Message);
    }

    [Fact]
    public void RejectsDuplicateVariantIds()
    {
        var input = new CalculationInput(
            CalculationMethod.AHP,
            [Criterion(1)],
            [
                new CalculationAlternativeInput(Id(10), [1m]),
                new CalculationAlternativeInput(Id(10), [2m])
            ],
            [],
            new CalculationAlgorithmOptions());

        var exception = Assert.Throws<CalculationAlgorithmException>(() => _algorithm.Calculate(input, default));

        Assert.Contains("Variant IDs must be unique", exception.Message);
    }

    [Fact]
    public void RejectsIncompletePairwiseMatrix()
    {
        var exception = Assert.Throws<CalculationAlgorithmException>(() => Run(
            [Criterion(1), Criterion(2), Criterion(3)],
            [[0m, 0m, 0m], [1m, 1m, 1m]],
            [Compare(1, 2, 2m), Compare(1, 3, 3m)]));

        Assert.Contains("exactly 3", exception.Message);
    }

    [Fact]
    public void RejectsDuplicateCriterionPair()
    {
        var exception = Assert.Throws<CalculationAlgorithmException>(() => Run(
            [Criterion(1), Criterion(2), Criterion(3)],
            [[0m, 0m, 0m], [1m, 1m, 1m]],
            [Compare(1, 2, 2m), Compare(2, 1, 2m), Compare(1, 3, 3m)]));

        Assert.Contains("duplicate", exception.Message);
    }

    [Fact]
    public void RejectsComparisonOfCriterionWithItself()
    {
        var exception = Assert.Throws<CalculationAlgorithmException>(() => Run(
            [Criterion(1), Criterion(2)],
            [[0m, 0m], [1m, 1m]],
            [Compare(1, 1, 2m)]));

        Assert.Contains("itself", exception.Message);
    }

    [Theory]
    [MemberData(nameof(InvalidSaatyValues))]
    public void RejectsValuesOutsideExactSaatyScale(decimal value)
    {
        var exception = Assert.Throws<CalculationAlgorithmException>(() => Run(
            [Criterion(1), Criterion(2)],
            [[0m, 0m], [1m, 1m]],
            [Compare(1, 2, value)]));

        Assert.Contains("Saaty", exception.Message);
    }

    [Theory]
    [MemberData(nameof(ExactReciprocalValues))]
    public void ExactReciprocalValuesAreAccepted(int scale, decimal reciprocal)
    {
        var output = Run(
            [Criterion(1), Criterion(2)],
            [[0m, 0m], [1m, 1m]],
            [Compare(1, 2, reciprocal)]);

        Assert.Equal(1m, output.CriterionWeights.Sum(x => x.Weight!.Value));
        Assert.Equal(Math.Round(1m / (scale + 1), 8, MidpointRounding.ToEven), output.CriterionWeights[0].Weight!.Value);
        Assert.Equal(Math.Round((decimal)scale / (scale + 1), 8, MidpointRounding.ToEven), output.CriterionWeights[1].Weight!.Value);
        Assert.Equal(0d, output.Diagnostics.ConsistencyRatio);
    }

    [Fact]
    public void ReversedComparisonIsPlacedInCorrectMatrixDirection()
    {
        var output = Run(
            [Criterion(1), Criterion(2)],
            [[0m, 0m], [1m, 1m]],
            [Compare(2, 1, 2m)]);

        Assert.InRange(output.CriterionWeights[0].Weight!.Value, 0.33333332m, 0.33333334m);
        Assert.InRange(output.CriterionWeights[1].Weight!.Value, 0.66666666m, 0.66666668m);
    }

    [Fact]
    public void InconsistentMatrixAboveThresholdIsRejected()
    {
        var exception = Assert.Throws<CalculationAlgorithmException>(() => Run(
            [Criterion(1), Criterion(2), Criterion(3)],
            [[0m, 0m, 0m], [1m, 1m, 1m]],
            [Compare(1, 2, 1m), Compare(1, 3, 9m), Compare(2, 3, 1m)]));

        Assert.Equal("calculation.ahp.inconsistent_matrix", exception.Code);
    }

    [Fact]
    public void ConsistencyThresholdAcceptsEqualityAndRejectsValuesAboveIt()
    {
        Assert.True(AhpWeightedSumAlgorithm.IsConsistencyAccepted(0.10m, 0.10m));
        Assert.True(AhpWeightedSumAlgorithm.IsConsistencyAccepted(0.099999999999m, 0.10m));
        Assert.False(AhpWeightedSumAlgorithm.IsConsistencyAccepted(0.100000000001m, 0.10m));
    }

    [Theory]
    [InlineData(CriterionDirection.Maximize, 10, 20, null, 0, 1)]
    [InlineData(CriterionDirection.Minimize, 10, 20, null, 1, 0)]
    [InlineData(CriterionDirection.Maximize, -10, 0, null, 0, 1)]
    public void DirectionNormalizationProducesExpectedUtilities(
        CriterionDirection direction,
        decimal first,
        decimal second,
        decimal? target,
        decimal expectedFirst,
        decimal expectedSecond)
    {
        var criterion = Criterion(1, direction, target);
        var output = Run([criterion], [[first], [second]], []);

        Assert.Equal(expectedFirst, output.Results[0].Score);
        Assert.Equal(expectedSecond, output.Results[1].Score);
    }

    [Fact]
    public void TargetUsesAbsoluteDeviationAndRewardsClosestValue()
    {
        var output = Run(
            [Criterion(1, CriterionDirection.Target, 10m)],
            [[0m], [10m], [20m]],
            []);

        Assert.Equal(new decimal?[] { 0m, 1m, 0m }, output.Results.Select(x => x.Score));
    }

    [Fact]
    public void ConstantZeroColumnUsesHalfUtilityWithoutDivisionByZero()
    {
        var output = Run([Criterion(1)], [[0m], [0m], [0m]], []);

        Assert.Equal(new decimal?[] { 0.5m, 0.5m, 0.5m }, output.Results.Select(x => x.Score));
    }

    [Fact]
    public void MultipleCriteriaUseWeightedSumAndLeaveRankForCommonRanker()
    {
        var output = Run(
            [Criterion(1), Criterion(2)],
            [[0m, 10m], [10m, 0m]],
            [Compare(1, 2, 1m)]);

        Assert.Equal(new decimal?[] { 0.5m, 0.5m }, output.Results.Select(x => x.Score));
        Assert.All(output.Results, x => Assert.Null(x.Rank));
    }

    [Fact]
    public void IdenticalAlternativesReceiveEqualScoreAndScoresStayInRange()
    {
        var output = Run([Criterion(1)], [[-3m], [-3m], [9m]], []);

        Assert.Equal(output.Results[0].Score, output.Results[1].Score);
        Assert.All(output.Results, x => Assert.InRange(x.Score!.Value, 0m, 1m));
    }

    [Fact]
    public void WeightRoundingResidualGoesToFirstCriterionAmongEqualLargestWeights()
    {
        var output = Run(
            [Criterion(1), Criterion(2), Criterion(3)],
            [[0m, 0m, 0m], [1m, 1m, 1m]],
            [Compare(1, 2, 1m), Compare(1, 3, 4m), Compare(2, 3, 4m)]);

        Assert.Equal(0.44444445m, output.CriterionWeights[0].Weight!.Value);
        Assert.Equal(0.44444444m, output.CriterionWeights[1].Weight!.Value);
        Assert.Equal(1m, output.CriterionWeights.Sum(x => x.Weight!.Value));
    }

    [Fact]
    public void RejectsComparisonForCriterionOutsideCurrentCalculation()
    {
        var exception = Assert.Throws<CalculationAlgorithmException>(() => Run(
            [Criterion(1), Criterion(2)],
            [[0m, 0m], [1m, 1m]],
            [Compare(1, 3, 2m)]));

        Assert.Contains("not selected", exception.Message);
    }

    [Fact]
    public void RejectsMissingTargetValue()
    {
        var exception = Assert.Throws<CalculationAlgorithmException>(() => Run(
            [Criterion(1, CriterionDirection.Target)],
            [[0m], [1m]],
            []));

        Assert.Contains("TargetValue", exception.Message);
    }

    [Fact]
    public void RejectsSuppliedWeightsAndWrongMethod()
    {
        var weightedInput = NewInput(
            [new CalculationCriterionInput(Id(1), CriterionDirection.Maximize, null, 1m)],
            [[0m], [1m]],
            [],
            method: CalculationMethod.AHP);
        var weightException = Assert.Throws<CalculationAlgorithmException>(() => _algorithm.Calculate(weightedInput, default));
        Assert.Contains("must not be supplied", weightException.Message);

        var wrongMethodInput = NewInput(
            [new CalculationCriterionInput(Id(1), CriterionDirection.Maximize, null, null)],
            [[0m], [1m]],
            [],
            method: CalculationMethod.Pareto);
        var methodException = Assert.Throws<CalculationAlgorithmException>(() => _algorithm.Calculate(wrongMethodInput, default));
        Assert.Contains("only AHP", methodException.Message);
    }

    [Fact]
    public void PermutingCriteriaComparisonsAndVariantsDoesNotChangeResults()
    {
        var c1 = Criterion(1);
        var c2 = Criterion(2);
        var c3 = Criterion(3);
        var first = NewInput(
            [c1, c2, c3],
            [[0m, 10m, 2m], [10m, 0m, 8m], [5m, 5m, 5m]],
            [Compare(1, 2, 2m), Compare(1, 3, 6m), Compare(2, 3, 3m)]);
        var second = NewInput(
            [c3, c1, c2],
            [[5m, 5m, 5m], [2m, 0m, 10m], [8m, 10m, 0m]],
            [Compare(2, 3, 3m), Compare(1, 3, 6m), Compare(1, 2, 2m)],
            [12, 10, 11]);

        var firstOutput = _algorithm.Calculate(first, default);
        var secondOutput = _algorithm.Calculate(second, default);

        Assert.Equal(firstOutput.Results.Select(x => x.VariantId), secondOutput.Results.Select(x => x.VariantId));
        Assert.Equal(firstOutput.Results.Select(x => x.Score), secondOutput.Results.Select(x => x.Score));
        Assert.Equal(firstOutput.CriterionWeights, secondOutput.CriterionWeights);
        Assert.Equal(firstOutput.Diagnostics, secondOutput.Diagnostics);
    }

    [Fact]
    public void RepeatedRunsWithSameInputAreDeterministic()
    {
        var input = NewInput(
            [Criterion(1), Criterion(2), Criterion(3)],
            [[0m, 1m, 5m], [3m, 9m, 2m]],
            [Compare(1, 2, 2m), Compare(1, 3, 6m), Compare(2, 3, 3m)]);

        var first = _algorithm.Calculate(input, default);
        var second = _algorithm.Calculate(input, default);

        Assert.Equal(first.Results, second.Results);
        Assert.Equal(first.CriterionWeights, second.CriterionWeights);
        Assert.Equal(first.Diagnostics, second.Diagnostics);
    }

    [Fact]
    public void HonorsCancellationToken()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => _algorithm.Calculate(
            NewInput([Criterion(1)], [[0m], [1m]], []),
            cancellation.Token));
    }

    private CalculationAlgorithmOutput Run(
        IReadOnlyList<CalculationCriterionInput> criteria,
        IReadOnlyList<decimal[]> values,
        IReadOnlyList<CalculationCriterionComparisonInput> comparisons)
    {
        return _algorithm.Calculate(NewInput(criteria, values, comparisons), CancellationToken.None);
    }

    private static CalculationInput NewInput(
        IEnumerable<CalculationCriterionInput> criteria,
        IEnumerable<decimal[]> values,
        IEnumerable<CalculationCriterionComparisonInput> comparisons,
        IReadOnlyList<int>? variantIds = null,
        CalculationMethod method = CalculationMethod.AHP)
    {
        var suppliedValues = values.ToArray();
        variantIds ??= Enumerable.Range(0, suppliedValues.Length).Select(index => 10 + index).ToArray();
        if (variantIds.Count != suppliedValues.Length)
            throw new ArgumentException("Variant ID count must match alternative count.", nameof(variantIds));

        var alternatives = suppliedValues.Select((criterionValues, index) =>
            new CalculationAlternativeInput(Id(variantIds[index]), criterionValues));
        return new CalculationInput(method, criteria, alternatives, comparisons, new CalculationAlgorithmOptions());
    }

    public static IEnumerable<object[]> InvalidSaatyValues()
    {
        yield return [0.3333333333333333333333333332m];
        yield return [3.1m];
        yield return [10m];
    }

    public static IEnumerable<object[]> ExactReciprocalValues()
    {
        yield return [2, 0.5m];
        yield return [3, 0.3333333333333333333333333333m];
        yield return [4, 0.25m];
        yield return [5, 0.2m];
        yield return [6, 0.1666666666666666666666666667m];
        yield return [7, 0.1428571428571428571428571429m];
        yield return [8, 0.125m];
        yield return [9, 0.1111111111111111111111111111m];
    }

    private static CalculationCriterionInput Criterion(
        int id,
        CriterionDirection direction = CriterionDirection.Maximize,
        decimal? target = null) =>
        new(Id(id), direction, target, null);

    private static CalculationCriterionComparisonInput Compare(int a, int b, decimal value) =>
        new(Id(a), Id(b), value);

    private static Guid Id(int id) => Guid.Parse($"00000000-0000-0000-0000-{id:D12}");

    private static IReadOnlyList<CalculationCriterionComparisonInput> AllPairs(Guid[] ids, decimal value)
    {
        var comparisons = new List<CalculationCriterionComparisonInput>();
        for (var i = 0; i < ids.Length; i++)
        for (var j = i + 1; j < ids.Length; j++)
            comparisons.Add(new CalculationCriterionComparisonInput(ids[i], ids[j], value));
        return comparisons;
    }
}
