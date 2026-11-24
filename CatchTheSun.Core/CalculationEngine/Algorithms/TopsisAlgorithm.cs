using CatchTheSun.Core.CalculationEngine.Models;
using CatchTheSun.Core.Entities;

namespace CatchTheSun.Core.CalculationEngine.Algorithms;

public sealed class TopsisAlgorithm : ICalculationAlgorithm
{
    private const string InvalidInputCode = "calculation.topsis.invalid_input";
    private const string NumericErrorCode = "calculation.topsis.numeric_error";
    private const decimal NumericMaximum = 99999999999999.999999m;
    private const decimal SquareRootTolerance = 0.000000000000000000000000001m;
    private const int SquareRootMaximumIterations = 100;
    private const int WeightScale = 8;
    private const decimal WeightSumTolerance = 0.000001m;

    public CalculationMethod Method => CalculationMethod.TOPSIS;

    public CalculationAlgorithmOutput Calculate(
        CalculationInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (input.Method != CalculationMethod.TOPSIS)
            throw InvalidInput("TopsisAlgorithm accepts only TOPSIS inputs.");

        ValidateOptions(input.Options);
        var criteria = input.Criteria;
        var alternatives = input.Alternatives
            .OrderBy(x => x.VariantId.ToString("N"), StringComparer.Ordinal)
            .ToArray();

        ValidateInput(input, alternatives, cancellationToken);
        var weights = NormalizeAndRoundWeights(criteria, input.Options, cancellationToken);
        var normalizedMatrix = BuildNormalizedWeightedMatrix(criteria, alternatives, weights, cancellationToken);
        var ideals = BuildIdealSolutions(criteria, normalizedMatrix, cancellationToken);
        var results = CalculateScores(alternatives, normalizedMatrix, ideals, cancellationToken);
        var criterionWeights = criteria
            .Select((criterion, index) => new CalculationCriterionWeight(criterion.CriterionId, weights[index]))
            .ToArray();

        return new CalculationAlgorithmOutput(
            CalculationMethod.TOPSIS,
            results,
            criterionWeights,
            new CalculationDiagnostics(null, null, null));
    }

    private static void ValidateOptions(CalculationAlgorithmOptions options)
    {
        if (options.AlgorithmVersion != "1.0.0" ||
            options.WeightSumTolerance != WeightSumTolerance ||
            options.WeightScale != WeightScale ||
            options.WeightRounding != MidpointRounding.ToEven ||
            options.TopsisNormalization != "Vector" ||
            options.TargetStrategy != "AbsoluteDeviation")
        {
            throw InvalidInput("Unsupported TOPSIS settings for AlgorithmVersion 1.0.0.");
        }
    }

    private static void ValidateInput(
        CalculationInput input,
        IReadOnlyList<CalculationAlternativeInput> alternatives,
        CancellationToken cancellationToken)
    {
        var criteria = input.Criteria;
        if (criteria.Count == 0)
            throw InvalidInput("TOPSIS requires at least one criterion.");
        if (alternatives.Count < 2)
            throw InvalidInput("TOPSIS requires at least two variants.");
        if (input.Comparisons.Count != 0)
            throw InvalidInput("TOPSIS does not accept AHP pairwise comparisons.");

        var variantIds = new HashSet<Guid>();
        foreach (var alternative in alternatives)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!variantIds.Add(alternative.VariantId))
                throw InvalidInput("Variant IDs must be unique.");
            if (alternative.Values.Count != criteria.Count)
                throw InvalidInput("The TOPSIS decision matrix must contain a value for every variant and criterion.");
        }

        var weightSum = 0m;
        foreach (var criterion in criteria)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (criterion.Weight is null)
                throw InvalidInput($"A weight is required for criterion {criterion.CriterionId}.");
            if (criterion.Weight is < 0m or > 1m)
                throw InvalidInput($"Weight for criterion {criterion.CriterionId} must be in [0,1].");
            if (!Enum.IsDefined(criterion.Direction))
                throw InvalidInput($"Criterion {criterion.CriterionId} has an unsupported direction.");
            if (criterion.Direction == CriterionDirection.Target)
            {
                if (criterion.TargetValue is null)
                    throw InvalidInput($"TargetValue is required for Target criterion {criterion.CriterionId}.");
                if (criterion.TargetValue is { } target && (target < -NumericMaximum || target > NumericMaximum))
                    throw InvalidInput($"TargetValue for criterion {criterion.CriterionId} is outside numeric(20,6).");
            }

            weightSum += criterion.Weight.Value;
        }

        if (Math.Abs(weightSum - 1m) > WeightSumTolerance)
            throw InvalidInput($"TOPSIS weights must sum to 1 within an absolute tolerance of {WeightSumTolerance}.");

        foreach (var alternative in alternatives)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var value in alternative.Values)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (value < -NumericMaximum || value > NumericMaximum)
                    throw InvalidInput("Criterion values must be within the PostgreSQL numeric(20,6) range.");
            }
        }
    }

    private static decimal[] NormalizeAndRoundWeights(
        IReadOnlyList<CalculationCriterionInput> criteria,
        CalculationAlgorithmOptions options,
        CancellationToken cancellationToken)
    {
        var sum = 0m;
        foreach (var criterion in criteria)
        {
            cancellationToken.ThrowIfCancellationRequested();
            sum += criterion.Weight!.Value;
        }

        if (sum <= 0m)
            throw InvalidInput("TOPSIS weights must have a positive total.");

        var normalized = new decimal[criteria.Count];
        var rounded = new decimal[criteria.Count];
        for (var i = 0; i < criteria.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            normalized[i] = criteria[i].Weight!.Value / sum;
            rounded[i] = Math.Round(normalized[i], options.WeightScale, options.WeightRounding);
        }

        var remainder = 1m - SumInOrder(rounded, cancellationToken);
        var correctionIndex = 0;
        for (var i = 1; i < normalized.Length; i++)
        {
            if (normalized[i] > normalized[correctionIndex])
                correctionIndex = i;
        }

        cancellationToken.ThrowIfCancellationRequested();
        rounded[correctionIndex] += remainder;
        var finalSum = 0m;
        for (var i = 0; i < rounded.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (rounded[i] is < 0m or > 1m)
                throw new CalculationAlgorithmException(NumericErrorCode, "TOPSIS weight rounding produced invalid weights.");
            finalSum += rounded[i];
        }

        if (finalSum != 1m)
            throw new CalculationAlgorithmException(NumericErrorCode, "TOPSIS weight rounding produced invalid weights.");

        return rounded;
    }

    private static decimal[,] BuildNormalizedWeightedMatrix(
        IReadOnlyList<CalculationCriterionInput> criteria,
        IReadOnlyList<CalculationAlternativeInput> alternatives,
        IReadOnlyList<decimal> weights,
        CancellationToken cancellationToken)
    {
        var matrix = new decimal[alternatives.Count, criteria.Count];

        for (var criterionIndex = 0; criterionIndex < criteria.Count; criterionIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var criterion = criteria[criterionIndex];
            var values = new decimal[alternatives.Count];
            var scale = 0m;
            for (var alternativeIndex = 0; alternativeIndex < alternatives.Count; alternativeIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var sourceValue = alternatives[alternativeIndex].Values[criterionIndex];
                var value = criterion.Direction == CriterionDirection.Target
                    ? Math.Abs(sourceValue - criterion.TargetValue!.Value)
                    : sourceValue;
                values[alternativeIndex] = value;
                scale = Math.Max(scale, SafeAbs(value));
            }

            if (scale == 0m)
            {
                for (var alternativeIndex = 0; alternativeIndex < alternatives.Count; alternativeIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    matrix[alternativeIndex, criterionIndex] = 0m;
                }
                continue;
            }

            var scaledValues = new decimal[values.Length];
            var sumSquares = 0m;
            for (var i = 0; i < values.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                scaledValues[i] = values[i] / scale;
                sumSquares += scaledValues[i] * scaledValues[i];
            }

            var normScaled = DecimalSquareRoot(sumSquares, cancellationToken);
            if (normScaled == 0m)
                throw new CalculationAlgorithmException(NumericErrorCode, "TOPSIS encountered a zero scaled vector norm.");

            for (var alternativeIndex = 0; alternativeIndex < alternatives.Count; alternativeIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var normalized = scaledValues[alternativeIndex] / normScaled;
                matrix[alternativeIndex, criterionIndex] = normalized * weights[criterionIndex];
            }
        }

        return matrix;
    }

    private static (decimal[] Positive, decimal[] Negative) BuildIdealSolutions(
        IReadOnlyList<CalculationCriterionInput> criteria,
        decimal[,] matrix,
        CancellationToken cancellationToken)
    {
        var alternativeCount = matrix.GetLength(0);
        var criterionCount = matrix.GetLength(1);
        var positive = new decimal[criterionCount];
        var negative = new decimal[criterionCount];

        for (var criterionIndex = 0; criterionIndex < criterionCount; criterionIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var minimum = matrix[0, criterionIndex];
            var maximum = minimum;
            for (var alternativeIndex = 1; alternativeIndex < alternativeCount; alternativeIndex++)
            {
                var value = matrix[alternativeIndex, criterionIndex];
                cancellationToken.ThrowIfCancellationRequested();
                if (value < minimum)
                    minimum = value;
                if (value > maximum)
                    maximum = value;
            }

            if (criteria[criterionIndex].Direction == CriterionDirection.Maximize)
            {
                positive[criterionIndex] = maximum;
                negative[criterionIndex] = minimum;
            }
            else
            {
                positive[criterionIndex] = minimum;
                negative[criterionIndex] = maximum;
            }
        }

        return (positive, negative);
    }

    private static IReadOnlyList<CalculationAlternativeOutput> CalculateScores(
        IReadOnlyList<CalculationAlternativeInput> alternatives,
        decimal[,] matrix,
        (decimal[] Positive, decimal[] Negative) ideals,
        CancellationToken cancellationToken)
    {
        var results = new CalculationAlternativeOutput[alternatives.Count];
        for (var alternativeIndex = 0; alternativeIndex < alternatives.Count; alternativeIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var positiveDifferences = new decimal[ideals.Positive.Length];
            var negativeDifferences = new decimal[ideals.Negative.Length];
            for (var criterionIndex = 0; criterionIndex < ideals.Positive.Length; criterionIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                positiveDifferences[criterionIndex] = matrix[alternativeIndex, criterionIndex] - ideals.Positive[criterionIndex];
                negativeDifferences[criterionIndex] = matrix[alternativeIndex, criterionIndex] - ideals.Negative[criterionIndex];
            }

            var distancePositive = ScaledEuclideanNorm(positiveDifferences, cancellationToken);
            var distanceNegative = ScaledEuclideanNorm(negativeDifferences, cancellationToken);
            var denominator = distancePositive + distanceNegative;
            var score = denominator == 0m
                ? 0.5m
                : distanceNegative / denominator;

            if (score is < 0m or > 1m)
                throw new CalculationAlgorithmException(NumericErrorCode, "TOPSIS closeness score is outside [0,1].");

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

    private static decimal ScaledEuclideanNorm(
        IReadOnlyList<decimal> differences,
        CancellationToken cancellationToken)
    {
        var scale = 0m;
        foreach (var difference in differences)
        {
            cancellationToken.ThrowIfCancellationRequested();
            scale = Math.Max(scale, SafeAbs(difference));
        }
        if (scale == 0m)
            return 0m;

        var sumSquares = 0m;
        foreach (var difference in differences)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var scaled = difference / scale;
            sumSquares += scaled * scaled;
        }

        return scale * DecimalSquareRoot(sumSquares, cancellationToken);
    }

    private static decimal DecimalSquareRoot(decimal value, CancellationToken cancellationToken)
    {
        if (value < 0m)
            throw new CalculationAlgorithmException(NumericErrorCode, "Cannot calculate the square root of a negative value.");
        if (value == 0m)
            return 0m;

        var estimate = value >= 1m ? value : 1m;
        for (var iteration = 0; iteration < SquareRootMaximumIterations; iteration++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var next = (estimate + value / estimate) / 2m;
            if (next == estimate || Math.Abs(next - estimate) <= SquareRootTolerance)
                return next;
            estimate = next;
        }

        throw new CalculationAlgorithmException(
            NumericErrorCode,
            $"Decimal square root did not converge within {SquareRootMaximumIterations} iterations.");
    }

    private static decimal SafeAbs(decimal value)
    {
        if (value == decimal.MinValue)
            throw new CalculationAlgorithmException(NumericErrorCode, "TOPSIS cannot process decimal.MinValue.");
        return Math.Abs(value);
    }

    private static decimal SumInOrder(IEnumerable<decimal> values, CancellationToken cancellationToken)
    {
        var sum = 0m;
        foreach (var value in values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            sum += value;
        }
        return sum;
    }

    private static CalculationAlgorithmException InvalidInput(string message) =>
        new(InvalidInputCode, message);
}
