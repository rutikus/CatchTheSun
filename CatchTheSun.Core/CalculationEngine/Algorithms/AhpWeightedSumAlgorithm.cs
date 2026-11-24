using CatchTheSun.Core.CalculationEngine.Models;
using CatchTheSun.Core.Entities;

namespace CatchTheSun.Core.CalculationEngine.Algorithms;

public sealed class AhpWeightedSumAlgorithm : ICalculationAlgorithm
{
    private const string InvalidInputCode = "calculation.ahp.invalid_input";
    private const string InconsistentMatrixCode = "calculation.ahp.inconsistent_matrix";
    private const string NoConvergenceCode = "calculation.ahp.no_convergence";
    private const string InvalidScoreCode = "calculation.ahp.invalid_score";
    private const int MaxIterations = 10_000;
    private const decimal EigenvectorTolerance = 0.000000000001m;
    private const int WeightScale = 8;

    private static readonly decimal[] SaatyValues =
    [
        1m, 2m, 3m, 4m, 5m, 6m, 7m, 8m, 9m,
        0.5m,
        0.3333333333333333333333333333m,
        0.25m,
        0.2m,
        0.1666666666666666666666666667m,
        0.1428571428571428571428571429m,
        0.125m,
        0.1111111111111111111111111111m
    ];

    private static readonly decimal[] RandomIndex =
    [
        0m, 0m, 0m, 0.58m, 0.90m, 1.12m, 1.24m, 1.32m, 1.41m, 1.45m,
        1.49m, 1.51m, 1.48m, 1.56m, 1.57m, 1.59m, 1.5943m, 1.6064m,
        1.6133m, 1.6207m, 1.6292m
    ];

    public CalculationMethod Method => CalculationMethod.AHP;

    public CalculationAlgorithmOutput Calculate(
        CalculationInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (input.Method != CalculationMethod.AHP)
            throw InvalidInput("AhpWeightedSumAlgorithm accepts only AHP inputs.");

        var criteria = input.Criteria;
        var alternatives = input.Alternatives
            .OrderBy(x => x.VariantId.ToString("N"), StringComparer.Ordinal)
            .ToArray();

        ValidateInput(input, alternatives, cancellationToken);
        var matrix = BuildComparisonMatrix(input, cancellationToken);

        decimal[] rawWeights;
        decimal lambdaMax;
        int iterationCount;

        if (criteria.Count == 1)
        {
            rawWeights = [1m];
            lambdaMax = 1m;
            iterationCount = 0;
        }
        else
        {
            var eigenvector = FindPrincipalEigenvector(matrix, cancellationToken);
            rawWeights = eigenvector.Weights;
            lambdaMax = eigenvector.LambdaMax;
            iterationCount = eigenvector.IterationCount;
        }

        var consistencyRatio = ComputeConsistencyRatio(lambdaMax, criteria.Count);
        if (!IsConsistencyAccepted(consistencyRatio, input.Options.AhpMaximumConsistencyRatio))
        {
            throw new CalculationAlgorithmException(
                InconsistentMatrixCode,
                $"AHP matrix consistency ratio {consistencyRatio} exceeds the allowed value {input.Options.AhpMaximumConsistencyRatio}.");
        }

        var finalWeights = NormalizeAndRoundWeights(rawWeights, input.Options.WeightScale, input.Options.WeightRounding);
        var results = CalculateWeightedScores(criteria, alternatives, finalWeights, cancellationToken);
        var criterionWeights = criteria
            .Select((criterion, index) => new CalculationCriterionWeight(criterion.CriterionId, finalWeights[index]))
            .ToArray();

        return new CalculationAlgorithmOutput(
            CalculationMethod.AHP,
            results,
            criterionWeights,
            new CalculationDiagnostics(
                decimal.ToDouble(consistencyRatio),
                decimal.ToDouble(lambdaMax),
                iterationCount));
    }

    public static bool IsConsistencyAccepted(decimal consistencyRatio, decimal maximumConsistencyRatio)
    {
        if (consistencyRatio < 0m)
            throw new ArgumentOutOfRangeException(nameof(consistencyRatio));
        if (maximumConsistencyRatio < 0m)
            throw new ArgumentOutOfRangeException(nameof(maximumConsistencyRatio));

        return consistencyRatio <= maximumConsistencyRatio;
    }

    private static void ValidateInput(
        CalculationInput input,
        IReadOnlyList<CalculationAlternativeInput> alternatives,
        CancellationToken cancellationToken)
    {
        if (input.Options.AlgorithmVersion != "1.0.0" ||
            input.Options.AhpMaximumCriteria != 20 ||
            input.Options.AhpMaximumConsistencyRatio != 0.10m ||
            input.Options.WeightScale != WeightScale ||
            input.Options.WeightRounding != MidpointRounding.ToEven ||
            input.Options.TargetStrategy != "AbsoluteDeviation")
        {
            throw InvalidInput("Unsupported AHP settings for AlgorithmVersion 1.0.0.");
        }

        var criteria = input.Criteria;
        const int maxCriteria = 20;

        if (criteria.Count is < 1 || criteria.Count > maxCriteria || criteria.Count > 20)
            throw InvalidInput($"AHP requires between 1 and {Math.Min(maxCriteria, 20)} criteria.");
        if (alternatives.Count < 2)
            throw InvalidInput("AHP + Weighted Sum requires at least two variants.");
        var variantIds = new HashSet<Guid>();
        foreach (var alternative in alternatives)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!variantIds.Add(alternative.VariantId))
                throw InvalidInput("Variant IDs must be unique.");
        }
        if (criteria.Any(x => x.Weight is not null))
            throw InvalidInput("AHP criterion weights are calculated from pairwise comparisons and must not be supplied as input.");
        if (criteria.Any(x => x.Direction == CriterionDirection.Target && x.TargetValue is null))
            throw InvalidInput("TargetValue is required for every Target criterion.");

        var expectedComparisons = criteria.Count * (criteria.Count - 1) / 2;
        if (input.Comparisons.Count != expectedComparisons)
            throw InvalidInput($"A complete AHP matrix requires exactly {expectedComparisons} pairwise comparisons.");
    }

    private static decimal[,] BuildComparisonMatrix(
        CalculationInput input,
        CancellationToken cancellationToken)
    {
        var count = input.Criteria.Count;
        var positions = input.Criteria
            .Select((criterion, index) => (criterion.CriterionId, index))
            .ToDictionary(x => x.CriterionId, x => x.index);
        var matrix = new decimal[count, count];
        var seenPairs = new HashSet<(int First, int Second)>();

        for (var i = 0; i < count; i++)
            matrix[i, i] = 1m;

        foreach (var comparison in input.Comparisons)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (comparison.CriterionAId == comparison.CriterionBId)
                throw InvalidInput("A criterion cannot be compared with itself.");
            if (!positions.TryGetValue(comparison.CriterionAId, out var a))
                throw InvalidInput($"Criterion {comparison.CriterionAId} in a comparison is not selected for this calculation.");
            if (!positions.TryGetValue(comparison.CriterionBId, out var b))
                throw InvalidInput($"Criterion {comparison.CriterionBId} in a comparison is not selected for this calculation.");
            if (!IsSaatyValue(comparison.Value))
                throw InvalidInput($"Comparison value {comparison.Value} is not an exact value on the Saaty scale.");

            var pair = a < b ? (a, b) : (b, a);
            if (!seenPairs.Add(pair))
                throw InvalidInput("The AHP matrix contains a duplicate criterion pair.");

            matrix[a, b] = comparison.Value;
            matrix[b, a] = GetReciprocal(comparison.Value);
        }

        if (seenPairs.Count != count * (count - 1) / 2)
            throw InvalidInput("The AHP matrix is missing one or more criterion pairs.");

        return matrix;
    }

    private static bool IsSaatyValue(decimal value) => Array.IndexOf(SaatyValues, value) >= 0;

    private static decimal GetReciprocal(decimal value)
    {
        return value switch
        {
            1m => 1m,
            2m => 0.5m,
            3m => 0.3333333333333333333333333333m,
            4m => 0.25m,
            5m => 0.2m,
            6m => 0.1666666666666666666666666667m,
            7m => 0.1428571428571428571428571429m,
            8m => 0.125m,
            9m => 0.1111111111111111111111111111m,
            0.5m => 2m,
            0.3333333333333333333333333333m => 3m,
            0.25m => 4m,
            0.2m => 5m,
            0.1666666666666666666666666667m => 6m,
            0.1428571428571428571428571429m => 7m,
            0.125m => 8m,
            0.1111111111111111111111111111m => 9m,
            _ => throw InvalidInput($"Comparison value {value} is not an exact value on the Saaty scale.")
        };
    }

    private static (decimal[] Weights, decimal LambdaMax, int IterationCount) FindPrincipalEigenvector(
        decimal[,] matrix,
        CancellationToken cancellationToken)
    {
        var count = matrix.GetLength(0);
        var weights = Enumerable.Repeat(1m / count, count).ToArray();

        for (var iteration = 1; iteration <= MaxIterations; iteration++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var product = Multiply(matrix, weights, cancellationToken);
            var productSum = SumInOrder(product);
            if (productSum <= 0m)
                throw new CalculationAlgorithmException(NoConvergenceCode, "AHP power iteration produced a non-positive vector.");

            var next = product.Select(value => value / productSum).ToArray();
            var maxComponentDifference = 0m;
            for (var i = 0; i < count; i++)
                maxComponentDifference = Math.Max(maxComponentDifference, Math.Abs(next[i] - weights[i]));

            var nextProduct = Multiply(matrix, next, cancellationToken);
            var lambdaMax = RayleighQuotient(next, nextProduct);
            var residual = RelativeResidual(next, nextProduct, lambdaMax);

            if (maxComponentDifference <= EigenvectorTolerance && residual <= EigenvectorTolerance)
                return (next, lambdaMax, iteration);

            weights = next;
        }

        throw new CalculationAlgorithmException(
            NoConvergenceCode,
            $"AHP power iteration did not converge within {MaxIterations} iterations.");
    }

    private static decimal[] Multiply(decimal[,] matrix, IReadOnlyList<decimal> vector, CancellationToken cancellationToken)
    {
        var count = vector.Count;
        var result = new decimal[count];
        for (var row = 0; row < count; row++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sum = 0m;
            for (var column = 0; column < count; column++)
                sum += matrix[row, column] * vector[column];
            result[row] = sum;
        }

        return result;
    }

    private static decimal RayleighQuotient(IReadOnlyList<decimal> vector, IReadOnlyList<decimal> product)
    {
        var numerator = 0m;
        var denominator = 0m;
        for (var i = 0; i < vector.Count; i++)
        {
            numerator += vector[i] * product[i];
            denominator += vector[i] * vector[i];
        }

        if (denominator <= 0m)
            throw new CalculationAlgorithmException(NoConvergenceCode, "AHP power iteration produced a zero eigenvector.");

        return numerator / denominator;
    }

    private static decimal RelativeResidual(
        IReadOnlyList<decimal> vector,
        IReadOnlyList<decimal> product,
        decimal lambdaMax)
    {
        var maxResidual = 0m;
        for (var i = 0; i < vector.Count; i++)
            maxResidual = Math.Max(maxResidual, Math.Abs(product[i] - lambdaMax * vector[i]));

        return maxResidual / Math.Abs(lambdaMax);
    }

    private static decimal ComputeConsistencyRatio(decimal lambdaMax, int count)
    {
        if (count <= 2)
            return 0m;

        var consistencyIndex = Math.Max(0m, (lambdaMax - count) / (count - 1));
        var randomIndex = RandomIndex[count];
        return consistencyIndex / randomIndex;
    }

    private static decimal[] NormalizeAndRoundWeights(
        IReadOnlyList<decimal> rawWeights,
        int weightScale,
        MidpointRounding rounding)
    {
        var sum = SumInOrder(rawWeights);
        if (sum <= 0m)
            throw new CalculationAlgorithmException(InvalidInputCode, "AHP produced weights that cannot be normalized.");

        var normalized = rawWeights.Select(weight => weight / sum).ToArray();
        var rounded = normalized
            .Select(weight => Math.Round(weight, weightScale, rounding))
            .ToArray();

        var remainder = 1m - SumInOrder(rounded);
        var correctionIndex = 0;
        for (var i = 1; i < normalized.Length; i++)
        {
            if (normalized[i] > normalized[correctionIndex])
                correctionIndex = i;
        }

        rounded[correctionIndex] += remainder;
        if (rounded.Any(weight => weight < 0m || weight > 1m) || SumInOrder(rounded) != 1m)
            throw new CalculationAlgorithmException(InvalidInputCode, "AHP weight rounding produced invalid weights.");

        return rounded;
    }

    private static IReadOnlyList<CalculationAlternativeOutput> CalculateWeightedScores(
        IReadOnlyList<CalculationCriterionInput> criteria,
        IReadOnlyList<CalculationAlternativeInput> alternatives,
        IReadOnlyList<decimal> weights,
        CancellationToken cancellationToken)
    {
        var utilities = new decimal[alternatives.Count, criteria.Count];

        for (var criterionIndex = 0; criterionIndex < criteria.Count; criterionIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var criterion = criteria[criterionIndex];
            var values = alternatives.Select(x => x.Values[criterionIndex]).ToArray();

            if (criterion.Direction == CriterionDirection.Target && criterion.TargetValue is null)
                throw InvalidInput($"TargetValue is required for Target criterion {criterion.CriterionId}.");

            var transformedValues = criterion.Direction == CriterionDirection.Target
                ? values.Select(value => Math.Abs(value - criterion.TargetValue!.Value)).ToArray()
                : values;
            var minimum = transformedValues.Min();
            var maximum = transformedValues.Max();

            for (var alternativeIndex = 0; alternativeIndex < alternatives.Count; alternativeIndex++)
            {
                var value = transformedValues[alternativeIndex];
                utilities[alternativeIndex, criterionIndex] = minimum == maximum
                    ? 0.5m
                    : criterion.Direction switch
                    {
                        CriterionDirection.Maximize => (value - minimum) / (maximum - minimum),
                        CriterionDirection.Minimize or CriterionDirection.Target => (maximum - value) / (maximum - minimum),
                        _ => throw InvalidInput($"Unsupported criterion direction '{criterion.Direction}'.")
                    };
            }
        }

        var results = new CalculationAlternativeOutput[alternatives.Count];
        for (var alternativeIndex = 0; alternativeIndex < alternatives.Count; alternativeIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var score = 0m;
            for (var criterionIndex = 0; criterionIndex < criteria.Count; criterionIndex++)
                score += weights[criterionIndex] * utilities[alternativeIndex, criterionIndex];

            if (score is < 0m or > 1m)
                throw new CalculationAlgorithmException(InvalidScoreCode, "AHP Weighted Sum produced a score outside [0,1].");

            results[alternativeIndex] = new CalculationAlternativeOutput(
                alternatives[alternativeIndex].VariantId,
                score,
                null,
                null,
                null,
                null);
        }

        return results;
    }

    private static decimal SumInOrder(IEnumerable<decimal> values)
    {
        var sum = 0m;
        foreach (var value in values)
            sum += value;
        return sum;
    }

    private static CalculationAlgorithmException InvalidInput(string message) =>
        new(InvalidInputCode, message);
}
