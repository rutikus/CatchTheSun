using CatchTheSun.Core.CalculationEngine.Models;
using CatchTheSun.Core.Entities;
using Xunit;

namespace CatchTheSun.Tests;

public class CalculationAlgorithmOutputTests
{
    [Theory]
    [InlineData(CalculationMethod.AHP)]
    [InlineData(CalculationMethod.TOPSIS)]
    public void ScalarMethodsRepresentScoresAndAllowNullParetoFields(CalculationMethod method)
    {
        var variantId = Guid.NewGuid();
        var output = new CalculationAlgorithmOutput(
            method,
            [new CalculationAlternativeOutput(variantId, 0.8123456789m, 1, null, null, null)],
            [new CalculationCriterionWeight(Guid.NewGuid(), 1.00000000m)],
            new CalculationDiagnostics(method == CalculationMethod.AHP ? 0.04d : null, null, null));

        var result = Assert.Single(output.Results);
        Assert.Equal(method, output.Method);
        Assert.Equal(0.8123456789m, result.Score);
        Assert.Equal(1, result.Rank);
        Assert.Null(result.ParetoFront);
        Assert.Null(result.IsParetoOptimal);
        Assert.Null(result.ParetoSecondaryScore);
        Assert.Equal(1.00000000m, Assert.Single(output.CriterionWeights).Weight!.Value);
        Assert.Equal(method == CalculationMethod.AHP ? 0.04d : null, output.Diagnostics.ConsistencyRatio);
    }

    [Fact]
    public void ParetoOutputAllowsNullScoreAndWeightAndCarriesFrontData()
    {
        var variantId = Guid.NewGuid();
        var output = new CalculationAlgorithmOutput(
            CalculationMethod.Pareto,
            [new CalculationAlternativeOutput(variantId, null, 1, 1, true, null)],
            [new CalculationCriterionWeight(Guid.NewGuid(), null)],
            new CalculationDiagnostics(null, null, null));

        var result = Assert.Single(output.Results);
        Assert.Null(result.Score);
        Assert.Equal(1, result.Rank);
        Assert.Equal(1, result.ParetoFront);
        Assert.True(result.IsParetoOptimal);
        Assert.Null(result.ParetoSecondaryScore);
        Assert.Null(Assert.Single(output.CriterionWeights).Weight);
        Assert.Null(output.Diagnostics.ConsistencyRatio);
    }

    [Fact]
    public void ParetoOutputSupportsWeightedSecondaryScore()
    {
        var output = new CalculationAlgorithmOutput(
            CalculationMethod.Pareto,
            [new CalculationAlternativeOutput(Guid.NewGuid(), null, 2, 1, true, 0.75m)],
            [new CalculationCriterionWeight(Guid.NewGuid(), 1m)],
            new CalculationDiagnostics(null, null, null));

        Assert.Equal(0.75m, Assert.Single(output.Results).ParetoSecondaryScore);
        Assert.Equal(1m, Assert.Single(output.CriterionWeights).Weight!.Value);
    }
}
