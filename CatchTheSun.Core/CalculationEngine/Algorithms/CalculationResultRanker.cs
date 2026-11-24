using CatchTheSun.Core.CalculationEngine.Models;
using CatchTheSun.Core.Entities;

namespace CatchTheSun.Core.CalculationEngine.Algorithms;

public static class CalculationResultRanker
{
    public static CalculationAlgorithmOutput Rank(CalculationAlgorithmOutput output)
    {
        ArgumentNullException.ThrowIfNull(output);
        var results = output.Results.ToArray();

        var ordered = output.Method switch
        {
            CalculationMethod.AHP or CalculationMethod.TOPSIS => RankScalar(results),
            CalculationMethod.Pareto => RankPareto(results),
            _ => throw new ArgumentOutOfRangeException(nameof(output), output.Method, "Unsupported calculation method.")
        };

        return new CalculationAlgorithmOutput(output.Method, ordered, output.CriterionWeights, output.Diagnostics);
    }

    private static IReadOnlyList<CalculationAlternativeOutput> RankScalar(
        IReadOnlyList<CalculationAlternativeOutput> results)
    {
        if (results.Any(x => x.Score is null))
            throw new InvalidOperationException("AHP and TOPSIS results must have a Score before ranking.");

        var ordered = results
            .OrderByDescending(x => x.Score!.Value)
            .ThenBy(x => x.VariantId.ToString("N"), StringComparer.Ordinal)
            .ToArray();

        var previousScore = (decimal?)null;
        var previousRank = 0;
        for (var i = 0; i < ordered.Length; i++)
        {
            var score = ordered[i].Score!.Value;
            var rank = previousScore == score ? previousRank : i + 1;
            ordered[i] = ordered[i] with { Rank = rank };
            previousScore = score;
            previousRank = rank;
        }

        return ordered;
    }

    private static IReadOnlyList<CalculationAlternativeOutput> RankPareto(
        IReadOnlyList<CalculationAlternativeOutput> results)
    {
        if (results.Any(x => x.ParetoFront is null or < 1))
            throw new InvalidOperationException("Pareto results must have a positive ParetoFront before ranking.");

        var anySecondaryScore = results.Any(x => x.ParetoSecondaryScore is not null);
        if (anySecondaryScore && results.Any(x => x.ParetoSecondaryScore is null))
            throw new InvalidOperationException("Pareto secondary scores must be present for all results or none.");

        var ordered = results
            .OrderBy(x => x.ParetoFront)
            .ThenByDescending(x => x.ParetoSecondaryScore)
            .ThenBy(x => x.VariantId.ToString("N"), StringComparer.Ordinal)
            .ToArray();

        int? previousFront = null;
        decimal? previousSecondary = null;
        var previousRank = 0;
        for (var i = 0; i < ordered.Length; i++)
        {
            var result = ordered[i];
            var sameKey = previousFront == result.ParetoFront &&
                (!anySecondaryScore || previousSecondary == result.ParetoSecondaryScore);
            var rank = sameKey ? previousRank : i + 1;
            ordered[i] = result with { Rank = rank };
            previousFront = result.ParetoFront;
            previousSecondary = result.ParetoSecondaryScore;
            previousRank = rank;
        }

        return ordered;
    }
}
