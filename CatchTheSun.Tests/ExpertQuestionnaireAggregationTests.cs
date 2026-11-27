using CatchTheSun.Core.Services;
using Xunit;

namespace CatchTheSun.Tests;

public sealed class ExpertQuestionnaireAggregationTests
{
    private static readonly Guid[] Criteria =
    [
        Guid.Parse("00000000-0000-0000-0000-000000000001"),
        Guid.Parse("00000000-0000-0000-0000-000000000002"),
        Guid.Parse("00000000-0000-0000-0000-000000000003")
    ];

    [Fact]
    public void GeometricMeanAggregatesSaatyValuesAndNormalizesWeights()
    {
        var judgments = Enumerable.Range(0, 5).Select(i => new ExpertJudgement(
            [new(Criteria[0], Criteria[1], i == 4 ? 8m : 2m),
             new(Criteria[0], Criteria[2], 4m), new(Criteria[1], Criteria[2], 2m)],
            [new(Criteria[0], 1), new(Criteria[1], 1), new(Criteria[2], 3)])).ToArray();

        var result = ExpertQuestionnaireAggregation.Calculate(Criteria, judgments);

        var pair = Assert.Single(result.Comparisons, x => x.FirstCriterionId == Criteria[0] && x.SecondCriterionId == Criteria[1]);
        Assert.InRange((double)pair.Value, 2.6390157, 2.6390159);
        Assert.Equal(5, result.ExpertCount);
        Assert.Equal(1d, result.KendallW!.Value, 12);
        Assert.Equal(1m, result.Weights.Sum(x => x.Weight));
        Assert.All(result.Weights, x => Assert.True(x.Weight > 0m));
    }

    [Fact]
    public void ReverseSaatyInputIsCanonicalizedToItsReciprocal()
    {
        var result = ExpertQuestionnaireAggregation.Canonicalize(Criteria[1], Criteria[0], 2m);
        Assert.Equal(Criteria[0], result.First);
        Assert.Equal(Criteria[1], result.Second);
        Assert.Equal(0.5m, result.Value);
    }

    [Fact]
    public void AggregationRequiresAtLeastFiveExperts()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => ExpertQuestionnaireAggregation.Calculate(Criteria, []));
        Assert.Contains("five", exception.Message);
    }

    [Fact]
    public void RejectsIncompletePairwiseMatrices()
    {
        var judgments = Enumerable.Range(0, 5).Select(_ => new ExpertJudgement(
            [new(Criteria[0], Criteria[1], 1m)], Criteria.Select((id, i) => new ExpertRankValue(id, i + 1)).ToArray())).ToArray();
        Assert.Throws<ArgumentException>(() => ExpertQuestionnaireAggregation.Calculate(Criteria, judgments));
    }
}
