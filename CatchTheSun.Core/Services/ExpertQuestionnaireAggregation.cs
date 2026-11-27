namespace CatchTheSun.Core.Services;

public sealed record ExpertPairwiseValue(Guid FirstCriterionId, Guid SecondCriterionId, decimal Value);
public sealed record ExpertRankValue(Guid CriterionId, int Rank);
public sealed record ExpertJudgement(IReadOnlyList<ExpertPairwiseValue> Comparisons, IReadOnlyList<ExpertRankValue> Rankings);
public sealed record AggregatedPairwiseValue(Guid FirstCriterionId, Guid SecondCriterionId, decimal Value);
public sealed record AggregatedCriterionWeight(Guid CriterionId, decimal Weight);
public sealed record ExpertQuestionnaireAggregate(int ExpertCount, double ConsistencyRatio, double? KendallW,
    IReadOnlyList<AggregatedPairwiseValue> Comparisons, IReadOnlyList<AggregatedCriterionWeight> Weights);

/// <summary>Aggregates complete Saaty matrices by geometric mean and rankings with Kendall's tie correction.</summary>
public static class ExpertQuestionnaireAggregation
{
    private static readonly decimal[] Saaty = [1m, 2m, 3m, 4m, 5m, 6m, 7m, 8m, 9m,
        0.5m, 0.3333333333333333333333333333m, 0.25m, 0.2m,
        0.1666666666666666666666666667m, 0.1428571428571428571428571429m,
        0.125m, 0.1111111111111111111111111111m];
    private static readonly decimal[] Reciprocal = [1m, 0.5m, 0.3333333333333333333333333333m,
        0.25m, 0.2m, 0.1666666666666666666666666667m, 0.1428571428571428571428571429m,
        0.125m, 0.1111111111111111111111111111m, 2m, 3m, 4m, 5m, 6m, 7m, 8m, 9m];
    private static readonly double[] RandomIndex = [0, 0, 0, 0.58, 0.90, 1.12, 1.24, 1.32, 1.41, 1.45,
        1.49, 1.51, 1.48, 1.56, 1.57, 1.59, 1.5943, 1.6064, 1.6133, 1.6207, 1.6292];

    public static bool IsSaatyValue(decimal value) => Array.IndexOf(Saaty, value) >= 0;

    public static (Guid First, Guid Second, decimal Value) Canonicalize(Guid first, Guid second, decimal value)
    {
        if (first == Guid.Empty || second == Guid.Empty || first == second)
            throw new ArgumentException("Two distinct criteria are required.");
        var index = Array.IndexOf(Saaty, value);
        if (index < 0) throw new ArgumentOutOfRangeException(nameof(value), "Value must be on the Saaty scale from 1 to 9 or its reciprocal.");
        if (string.CompareOrdinal(first.ToString("N"), second.ToString("N")) < 0) return (first, second, value);
        return (second, first, Reciprocal[index]);
    }

    public static ExpertQuestionnaireAggregate Calculate(
        IReadOnlyList<Guid> orderedCriterionIds,
        IReadOnlyList<ExpertJudgement> experts)
    {
        ArgumentNullException.ThrowIfNull(orderedCriterionIds);
        ArgumentNullException.ThrowIfNull(experts);
        var m = orderedCriterionIds.Count;
        var n = experts.Count;
        if (m is < 2 or > 20) throw new ArgumentOutOfRangeException(nameof(orderedCriterionIds), "Questionnaires require 2 to 20 criteria.");
        if (n < 5) throw new InvalidOperationException("At least five submitted expert responses are required for aggregation.");
        if (orderedCriterionIds.Distinct().Count() != m) throw new ArgumentException("Criterion IDs must be unique.");

        var indexById = orderedCriterionIds.Select((id, index) => (id, index)).ToDictionary(x => x.id, x => x.index);
        var matrix = new double[m, m];
        for (var i = 0; i < m; i++) matrix[i, i] = 1d;
        var comparisonLogs = new double[m, m];
        foreach (var expert in experts)
        {
            var seen = new HashSet<(int, int)>();
            foreach (var comparison in expert.Comparisons)
            {
                var pair = Canonicalize(comparison.FirstCriterionId, comparison.SecondCriterionId, comparison.Value);
                if (!indexById.TryGetValue(pair.First, out var i) || !indexById.TryGetValue(pair.Second, out var j))
                    throw new ArgumentException("A comparison criterion is outside the questionnaire.");
                if (!seen.Add((i, j))) throw new ArgumentException("An expert response contains a duplicate criterion pair.");
                var logValue = Math.Log((double)pair.Value);
                comparisonLogs[i, j] += logValue;
                comparisonLogs[j, i] -= logValue;
            }
            if (seen.Count != m * (m - 1) / 2) throw new ArgumentException("Every expert must compare every pair of criteria.");

            if (expert.Rankings.Count != m || expert.Rankings.Select(x => x.CriterionId).Distinct().Count() != m ||
                expert.Rankings.Any(x => !indexById.ContainsKey(x.CriterionId) || x.Rank < 1 || x.Rank > m))
                throw new ArgumentException("Every expert must rank every questionnaire criterion with a rank from 1 to the criterion count.");
        }

        var aggregatedComparisons = new List<AggregatedPairwiseValue>(m * (m - 1) / 2);
        for (var i = 0; i < m; i++)
        for (var j = i + 1; j < m; j++)
        {
            var value = Math.Exp(comparisonLogs[i, j] / n);
            matrix[i, j] = value;
            matrix[j, i] = 1d / value;
            var first = orderedCriterionIds[i]; var second = orderedCriterionIds[j];
            var aggregateValue = decimal.Round((decimal)value, 28, MidpointRounding.ToEven);
            if (string.CompareOrdinal(first.ToString("N"), second.ToString("N")) < 0)
                aggregatedComparisons.Add(new(first, second, aggregateValue));
            else
                aggregatedComparisons.Add(new(second, first, decimal.Round(1m / aggregateValue, 28, MidpointRounding.ToEven)));
        }

        var weights = PrincipalWeights(matrix);
        var lambdaMax = weights.Select((weight, i) => MultiplyRow(matrix, i, weights) / weight).Average();
        var consistencyIndex = Math.Max(0d, (lambdaMax - m) / (m - 1));
        var consistencyRatio = RandomIndex[m] == 0d ? 0d : consistencyIndex / RandomIndex[m];
        var kendallW = ComputeKendallW(orderedCriterionIds, experts, indexById);

        var rounded = weights.Select(x => decimal.Round((decimal)x, 8, MidpointRounding.ToEven)).ToArray();
        rounded[^1] += 1m - rounded.Sum();
        return new ExpertQuestionnaireAggregate(n, consistencyRatio, kendallW, aggregatedComparisons,
            orderedCriterionIds.Select((id, i) => new AggregatedCriterionWeight(id, rounded[i])).ToArray());
    }

    private static double[] PrincipalWeights(double[,] matrix)
    {
        var n = matrix.GetLength(0);
        var weights = Enumerable.Repeat(1d / n, n).ToArray();
        for (var iteration = 0; iteration < 10_000; iteration++)
        {
            var next = new double[n];
            for (var i = 0; i < n; i++) next[i] = MultiplyRow(matrix, i, weights);
            var sum = next.Sum();
            for (var i = 0; i < n; i++) next[i] /= sum;
            if (Enumerable.Range(0, n).Max(i => Math.Abs(next[i] - weights[i])) < 1e-13) return next;
            weights = next;
        }
        throw new InvalidOperationException("AHP principal eigenvector did not converge.");
    }

    private static double MultiplyRow(double[,] matrix, int row, IReadOnlyList<double> weights)
    {
        var sum = 0d;
        for (var column = 0; column < weights.Count; column++) sum += matrix[row, column] * weights[column];
        return sum;
    }

    private static double? ComputeKendallW(IReadOnlyList<Guid> ids, IReadOnlyList<ExpertJudgement> experts,
        IReadOnlyDictionary<Guid, int> indexById)
    {
        var m = ids.Count; var n = experts.Count;
        var rankSums = new double[m]; var tieCorrectionTotal = 0d;
        foreach (var expert in experts)
        {
            foreach (var group in expert.Rankings.GroupBy(x => x.Rank))
            {
                var tied = group.Count();
                tieCorrectionTotal += tied * tied * tied - tied;
            }
            var orderedGroups = expert.Rankings.GroupBy(x => x.Rank).OrderBy(x => x.Key).ToArray();
            var offset = 0;
            foreach (var group in orderedGroups)
            {
                var tied = group.Count();
                var averageRank = offset + (tied + 1d) / 2d;
                foreach (var ranking in group) rankSums[indexById[ranking.CriterionId]] += averageRank;
                offset += tied;
            }
        }
        var mean = n * (m + 1d) / 2d;
        var s = rankSums.Sum(x => (x - mean) * (x - mean));
        var denominator = (double)n * n * (m * m * m - m) - n * tieCorrectionTotal;
        return denominator <= 0d ? null : 12d * s / denominator;
    }
}
