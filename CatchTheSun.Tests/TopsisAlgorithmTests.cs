using CatchTheSun.Core.CalculationEngine;
using CatchTheSun.Core.CalculationEngine.Algorithms;
using CatchTheSun.Core.CalculationEngine.Models;
using CatchTheSun.Core.Entities;
using Xunit;

namespace CatchTheSun.Tests;

public class TopsisAlgorithmTests
{
    private readonly TopsisAlgorithm _algorithm = new();

    [Fact]
    public void MaximizeRanksHigherValueWithHigherScore()
    {
        var output = Run([Criterion(1, CriterionDirection.Maximize, 1m)], [[1m], [2m]]);

        Assert.Equal(new decimal?[] { 0m, 1m }, output.Results.Select(x => x.Score));
        Assert.All(output.Results, result => Assert.Null(result.Rank));
    }

    [Fact]
    public void MinimizeRanksLowerValueWithHigherScore()
    {
        var output = Run([Criterion(1, CriterionDirection.Minimize, 1m)], [[1m], [2m]]);

        Assert.Equal(new decimal?[] { 1m, 0m }, output.Results.Select(x => x.Score));
    }

    [Fact]
    public void TargetUsesAbsoluteDeviationAndMinimizesIt()
    {
        var output = Run(
            [Criterion(1, CriterionDirection.Target, 1m, target: 10m)],
            [[0m], [10m], [20m]]);

        Assert.Equal(new decimal?[] { 0m, 1m, 0m }, output.Results.Select(x => x.Score));
    }

    [Fact]
    public void ZeroAndConstantColumnsProduceHalfForIndistinguishableAlternatives()
    {
        var zeroColumn = Run([Criterion(1, CriterionDirection.Maximize, 1m)], [[0m], [0m], [0m]]);
        var constantColumn = Run([Criterion(1, CriterionDirection.Minimize, 1m)], [[-5m], [-5m], [-5m]]);

        Assert.Equal(new decimal?[] { 0.5m, 0.5m, 0.5m }, zeroColumn.Results.Select(x => x.Score));
        Assert.Equal(new decimal?[] { 0.5m, 0.5m, 0.5m }, constantColumn.Results.Select(x => x.Score));
    }

    [Fact]
    public void NegativeValuesAreValid()
    {
        var output = Run([Criterion(1, CriterionDirection.Maximize, 1m)], [[-20m], [-10m]]);

        Assert.Equal(new decimal?[] { 0m, 1m }, output.Results.Select(x => x.Score));
    }

    [Fact]
    public void MultipleCriteriaReturnNormalizedWeightsAndScoresInRange()
    {
        var output = Run(
            [Criterion(1, CriterionDirection.Maximize, 0.6m), Criterion(2, CriterionDirection.Minimize, 0.4m)],
            [[0m, 0m], [10m, 10m], [5m, 8m]]);

        Assert.Equal(1m, output.CriterionWeights.Sum(x => x.Weight!.Value));
        Assert.All(output.Results, x => Assert.InRange(x.Score!.Value, 0m, 1m));
        Assert.Null(output.Diagnostics.ConsistencyRatio);
    }

    [Fact]
    public void IdenticalAlternativesReceiveSameScoreAndFullyIdenticalSetGetsHalf()
    {
        var output = Run(
            [Criterion(1, CriterionDirection.Maximize, 0.5m), Criterion(2, CriterionDirection.Target, 0.5m, 12m)],
            [[-3m, 10m], [-3m, 10m], [-3m, 10m]]);

        Assert.Equal(new decimal?[] { 0.5m, 0.5m, 0.5m }, output.Results.Select(x => x.Score));
    }

    [Fact]
    public void TwoAlternativesCanReachScoresZeroAndOne()
    {
        var output = Run([Criterion(1, CriterionDirection.Maximize, 1m)], [[-1m], [1m]]);

        Assert.Equal(0m, output.Results[0].Score);
        Assert.Equal(1m, output.Results[1].Score);
    }

    [Fact]
    public void ZeroWeightDoesNotInfluenceTheResult()
    {
        var output = Run(
            [Criterion(1, CriterionDirection.Maximize, 1m), Criterion(2, CriterionDirection.Maximize, 0m)],
            [[0m, 100m], [10m, -100m]]);

        Assert.Equal(new[] { 1m, 0m }, output.CriterionWeights.Select(x => x.Weight!.Value));
        Assert.Equal(new decimal?[] { 0m, 1m }, output.Results.Select(x => x.Score));
    }

    [Fact]
    public void NormalizesInputWeightsBeforeCalculation()
    {
        var output = Run(
            [Criterion(1, CriterionDirection.Maximize, 0.3m), Criterion(2, CriterionDirection.Maximize, 0.7m)],
            [[0m, 0m], [10m, 10m]]);

        Assert.Equal(new[] { 0.3m, 0.7m }, output.CriterionWeights.Select(x => x.Weight!.Value));
        Assert.Equal(1m, output.CriterionWeights.Sum(x => x.Weight!.Value));
    }

    [Fact]
    public void WeightSumAtAllowedToleranceIsAcceptedAndNormalized()
    {
        var output = Run(
            [Criterion(1, CriterionDirection.Maximize, 0.5000005m), Criterion(2, CriterionDirection.Maximize, 0.5000005m)],
            [[0m, 0m], [1m, 1m]]);

        Assert.Equal(new[] { 0.5m, 0.5m }, output.CriterionWeights.Select(x => x.Weight!.Value));
    }

    [Fact]
    public void RoundingRemainderGoesToFirstCriterionOnEqualMaximumWeight()
    {
        var output = Run(
            [
                Criterion(1, CriterionDirection.Maximize, 0.44444444m),
                Criterion(2, CriterionDirection.Maximize, 0.44444444m),
                Criterion(3, CriterionDirection.Maximize, 0.11111111m)
            ],
            [[0m, 0m, 0m], [1m, 1m, 1m]]);

        Assert.Equal(0.44444445m, output.CriterionWeights[0].Weight!.Value);
        Assert.Equal(0.44444444m, output.CriterionWeights[1].Weight!.Value);
        Assert.Equal(1m, output.CriterionWeights.Sum(x => x.Weight!.Value));
    }

    [Fact]
    public void CriterionPermutationDoesNotChangeFinalWeightsOrScores()
    {
        var first = Run(
            [Criterion(1, CriterionDirection.Maximize, 0.2m), Criterion(2, CriterionDirection.Minimize, 0.3m), Criterion(3, CriterionDirection.Target, 0.5m, 10m)],
            [[0m, 10m, 10m], [10m, 0m, 20m]],
            variantIds: [10, 11]);
        var second = Run(
            [Criterion(3, CriterionDirection.Target, 0.5m, 10m), Criterion(1, CriterionDirection.Maximize, 0.2m), Criterion(2, CriterionDirection.Minimize, 0.3m)],
            [[10m, 0m, 10m], [20m, 10m, 0m]],
            variantIds: [10, 11]);

        Assert.Equal(first.CriterionWeights, second.CriterionWeights);
        Assert.Equal(first.Results, second.Results);
    }

    [Fact]
    public void EqualMaximumWeightTieUsesCanonicalCriterionOrderEvenWhenInputIsPermuted()
    {
        var weights = new[] { 0.44444444m, 0.44444444m, 0.11111111m };
        var first = Run(
            [Criterion(1, CriterionDirection.Maximize, weights[0]), Criterion(2, CriterionDirection.Maximize, weights[1]), Criterion(3, CriterionDirection.Maximize, weights[2])],
            [[0m, 0m, 0m], [1m, 1m, 1m]]);
        var second = Run(
            [Criterion(3, CriterionDirection.Maximize, weights[2]), Criterion(2, CriterionDirection.Maximize, weights[1]), Criterion(1, CriterionDirection.Maximize, weights[0])],
            [[0m, 0m, 0m], [1m, 1m, 1m]]);

        Assert.Equal(first.CriterionWeights, second.CriterionWeights);
        Assert.Equal(0.44444445m, first.CriterionWeights[0].Weight!.Value);
    }

    [Fact]
    public void AcceptsValuesNearNumericTwentySixScaleBoundaryWithoutOverflow()
    {
        const decimal maximum = 99999999999999.999999m;
        var output = Run(
            [Criterion(1, CriterionDirection.Maximize, 1m)],
            [[maximum], [-maximum]]);

        Assert.Equal(new decimal?[] { 1m, 0m }, output.Results.Select(x => x.Score));
    }

    [Fact]
    public void AcceptsValuesNearZero()
    {
        var output = Run(
            [Criterion(1, CriterionDirection.Maximize, 1m)],
            [[0.000001m], [0.000002m]]);

        Assert.Equal(new decimal?[] { 0m, 1m }, output.Results.Select(x => x.Score));
    }

    [Fact]
    public void DecimalSquareRootAndScaledEuclideanDistanceHandleMultipleNonzeroComponents()
    {
        var output = Run(
            [Criterion(1, CriterionDirection.Maximize, 0.5m), Criterion(2, CriterionDirection.Maximize, 0.5m)],
            [[1m, 0m], [0m, 1m], [0.25m, 0.5m]]);

        var middleScore = output.Results[2].Score!.Value;
        Assert.InRange(middleScore, 0.3m, 0.45m);
        Assert.NotEqual(0.5m, middleScore);
    }

    [Fact]
    public void CloseButUnequalScoresRemainUnequalWithoutEpsilon()
    {
        var output = Run(
            [Criterion(1, CriterionDirection.Maximize, 0.5000001m), Criterion(2, CriterionDirection.Maximize, 0.4999999m)],
            [[1m, 0m], [0m, 1m]]);

        Assert.True(output.Results[0].Score > output.Results[1].Score);
        Assert.NotEqual(output.Results[0].Score, output.Results[1].Score);
    }

    [Fact]
    public void RejectsLessThanTwoVariants()
    {
        var exception = Assert.Throws<CalculationAlgorithmException>(() => Run(
            [Criterion(1, CriterionDirection.Maximize, 1m)],
            [[1m]]));

        Assert.Contains("at least two", exception.Message);
    }

    [Fact]
    public void RejectsNoCriteria()
    {
        var exception = Assert.Throws<CalculationAlgorithmException>(() => Run([], [[], []]));

        Assert.Contains("at least one criterion", exception.Message);
    }

    [Fact]
    public void InputModelRejectsPartialDecisionMatrix()
    {
        Assert.Throws<ArgumentException>(() => new CalculationInput(
            CalculationMethod.TOPSIS,
            [Criterion(1, CriterionDirection.Maximize, 1m), Criterion(2, CriterionDirection.Maximize, 0m)],
            [new CalculationAlternativeInput(Id(10), [1m])],
            [],
            new CalculationAlgorithmOptions()));
    }

    [Fact]
    public void RejectsMissingWeight()
    {
        var exception = Assert.Throws<CalculationAlgorithmException>(() => Run(
            [Criterion(1, CriterionDirection.Maximize, null)],
            [[0m], [1m]]));

        Assert.Contains("weight is required", exception.Message);
    }

    [Theory]
    [MemberData(nameof(InvalidWeights))]
    public void RejectsInvalidWeights(decimal firstWeight, decimal secondWeight, string expectedMessage)
    {
        var exception = Assert.Throws<CalculationAlgorithmException>(() => Run(
            [Criterion(1, CriterionDirection.Maximize, firstWeight), Criterion(2, CriterionDirection.Maximize, secondWeight)],
            [[0m, 0m], [1m, 1m]]));

        Assert.Contains(expectedMessage, exception.Message);
    }

    [Fact]
    public void RejectsMissingTargetAndUnsupportedDirection()
    {
        var missingTarget = Assert.Throws<CalculationAlgorithmException>(() => Run(
            [Criterion(1, CriterionDirection.Target, 1m)],
            [[0m], [1m]]));
        Assert.Contains("TargetValue", missingTarget.Message);

        var invalidDirection = Assert.Throws<CalculationAlgorithmException>(() => Run(
            [new CalculationCriterionInput(Id(1), (CriterionDirection)99, null, 1m)],
            [[0m], [1m]]));
        Assert.Contains("unsupported direction", invalidDirection.Message);
    }

    [Fact]
    public void RejectsAHPComparisonsAndDuplicateVariants()
    {
        var comparisonInput = NewInput(
            [Criterion(1, CriterionDirection.Maximize, 1m)],
            [[0m], [1m]],
            [new CalculationCriterionComparisonInput(Id(1), Id(1), 3m)]);
        var comparisonException = Assert.Throws<CalculationAlgorithmException>(() => _algorithm.Calculate(comparisonInput, default));
        Assert.Contains("does not accept", comparisonException.Message);

        var duplicateVariants = new CalculationInput(
            CalculationMethod.TOPSIS,
            [Criterion(1, CriterionDirection.Maximize, 1m)],
            [new CalculationAlternativeInput(Id(10), [0m]), new CalculationAlternativeInput(Id(10), [1m])],
            [],
            new CalculationAlgorithmOptions());
        var duplicateException = Assert.Throws<CalculationAlgorithmException>(() => _algorithm.Calculate(duplicateVariants, default));
        Assert.Contains("Variant IDs must be unique", duplicateException.Message);
    }

    [Fact]
    public void RejectsWeightSumOutsideToleranceAndWeightsOutsideRange()
    {
        var invalidSum = Assert.Throws<CalculationAlgorithmException>(() => Run(
            [Criterion(1, CriterionDirection.Maximize, 0.5m), Criterion(2, CriterionDirection.Maximize, 0.499998m)],
            [[0m, 0m], [1m, 1m]]));
        Assert.Contains("sum to 1", invalidSum.Message);

        var outOfRange = Assert.Throws<CalculationAlgorithmException>(() => Run(
            [Criterion(1, CriterionDirection.Maximize, 1.1m), Criterion(2, CriterionDirection.Maximize, 0m)],
            [[0m, 0m], [1m, 1m]]));
        Assert.Contains("must be in [0,1]", outOfRange.Message);
    }

    [Fact]
    public void PermutingVariantsAndRepeatedRunsPreservesScoresAndStableOutputOrder()
    {
        var first = Run(
            [Criterion(1, CriterionDirection.Maximize, 0.6m), Criterion(2, CriterionDirection.Minimize, 0.4m)],
            [[1m, 7m], [3m, 4m], [2m, 9m]],
            variantIds: [12, 10, 11]);
        var permuted = Run(
            [Criterion(1, CriterionDirection.Maximize, 0.6m), Criterion(2, CriterionDirection.Minimize, 0.4m)],
            [[2m, 9m], [1m, 7m], [3m, 4m]],
            variantIds: [11, 12, 10]);
        var repeated = Run(
            [Criterion(1, CriterionDirection.Maximize, 0.6m), Criterion(2, CriterionDirection.Minimize, 0.4m)],
            [[1m, 7m], [3m, 4m], [2m, 9m]],
            variantIds: [12, 10, 11]);

        Assert.Equal(first.Results, permuted.Results);
        Assert.Equal(first.Results, repeated.Results);
        Assert.Equal(first.Results.OrderBy(x => x.VariantId.ToString("N"), StringComparer.Ordinal).Select(x => x.VariantId), first.Results.Select(x => x.VariantId));
    }

    [Fact]
    public void HonorsCancellationToken()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => _algorithm.Calculate(
            NewInput([Criterion(1, CriterionDirection.Maximize, 1m)], [[0m], [1m]], []),
            cancellation.Token));
    }

    public static IEnumerable<object[]> InvalidWeights()
    {
        yield return [-0.1m, 0.8m, "must be in [0,1]"];
        yield return [0.7m, 0.7m, "sum to 1"];
        yield return [0.3m, 0.3m, "sum to 1"];
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
        return new CalculationInput(CalculationMethod.TOPSIS, criteria, alternatives, comparisons, new CalculationAlgorithmOptions());
    }

    private static CalculationCriterionInput Criterion(
        int id,
        CriterionDirection direction,
        decimal? weight,
        decimal? target = null) =>
        new(Id(id), direction, target, weight);

    private static Guid Id(int id) => Guid.Parse($"00000000-0000-0000-0000-{id:D12}");
}
