using CatchTheSun.Core.CalculationEngine.Models;
using CatchTheSun.Core.Entities;

namespace CatchTheSun.Core.CalculationEngine.Algorithms;

public sealed class ParetoAlgorithm : ICalculationAlgorithm
{
    private const string InvalidInputCode = "calculation.pareto.invalid_input";
    private const string NumericErrorCode = "calculation.pareto.numeric_error";
    private const decimal NumericMaximum = 99999999999999.999999m;
    private const decimal WeightSumTolerance = 0.000001m;
    private const int WeightScale = 8;

    public CalculationMethod Method => CalculationMethod.Pareto;

    public CalculationAlgorithmOutput Calculate(
        CalculationInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (input.Method != CalculationMethod.Pareto)
            throw InvalidInput("ParetoAlgorithm accepts only Pareto inputs.");

        ValidateOptions(input.Options);
        var criteria = input.Criteria;
        var alternatives = input.Alternatives
            .OrderBy(x => x.VariantId.ToString("N"), StringComparer.Ordinal)
            .ToArray();
        var hasWeights = ValidateInput(input, alternatives, cancellationToken);

        var costs = BuildCostMatrix(criteria, alternatives, cancellationToken);
        var fronts = BuildFronts(costs, alternatives, cancellationToken);
        var weights = hasWeights
            ? NormalizeAndRoundWeights(criteria, input.Options, cancellationToken)
            : null;
        var secondaryScores = hasWeights
            ? CalculateSecondaryScores(criteria, alternatives, weights!, cancellationToken)
            : null;

        var results = Enumerable.Range(0, alternatives.Length)
            .Select(index => new CalculationAlternativeOutput(
                alternatives[index].VariantId,
                null,
                null,
                fronts[index],
                fronts[index] == 1,
                secondaryScores?[index]))
            .OrderBy(result => result.ParetoFront)
            .ThenByDescending(result => hasWeights ? result.ParetoSecondaryScore : null)
            .ThenBy(result => result.VariantId.ToString("N"), StringComparer.Ordinal)
            .ToArray();

        var criterionWeights = criteria
            .Select((criterion, index) => new CalculationCriterionWeight(
                criterion.CriterionId,
                weights is null ? null : weights[index]))
            .ToArray();

        return new CalculationAlgorithmOutput(
            CalculationMethod.Pareto,
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
            options.TargetStrategy != "AbsoluteDeviation")
        {
            throw InvalidInput("Unsupported Pareto settings for AlgorithmVersion 1.0.0.");
        }
    }

    private static bool ValidateInput(
        CalculationInput input,
        IReadOnlyList<CalculationAlternativeInput> alternatives,
        CancellationToken cancellationToken)
    {
        var criteria = input.Criteria;
        if (criteria.Count == 0)
            throw InvalidInput("Pareto requires at least one criterion.");
        if (alternatives.Count < 2)
            throw InvalidInput("Pareto requires at least two variants.");
        if (input.Comparisons.Count != 0)
            throw InvalidInput("Pareto does not accept AHP pairwise comparisons.");
        if (criteria.Select(x => x.CriterionId).Distinct().Count() != criteria.Count)
            throw InvalidInput("Criterion IDs must be unique.");

        var variantIds = new HashSet<Guid>();
        foreach (var alternative in alternatives)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!variantIds.Add(alternative.VariantId))
                throw InvalidInput("Variant IDs must be unique.");
            if (alternative.Values.Count != criteria.Count)
                throw InvalidInput("The Pareto decision matrix must contain a value for every variant and criterion.");
        }

        var anyWeight = false;
        var anyMissingWeight = false;
        var weightSum = 0m;
        foreach (var criterion in criteria)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!Enum.IsDefined(criterion.Direction))
                throw InvalidInput($"Criterion {criterion.CriterionId} has an unsupported direction.");
            if (criterion.Direction == CriterionDirection.Target)
            {
                if (criterion.TargetValue is null)
                    throw InvalidInput($"TargetValue is required for Target criterion {criterion.CriterionId}.");
                if (criterion.TargetValue is { } target && (target < -NumericMaximum || target > NumericMaximum))
                    throw InvalidInput($"TargetValue for criterion {criterion.CriterionId} is outside numeric(20,6).");
            }

            if (criterion.Weight is { } weight)
            {
                anyWeight = true;
                if (weight is < 0m or > 1m)
                    throw InvalidInput($"Weight for criterion {criterion.CriterionId} must be in [0,1].");
                weightSum += weight;
            }
            else
            {
                anyMissingWeight = true;
            }
        }

        if (anyWeight && anyMissingWeight)
            throw InvalidInput("Pareto weights must be supplied for all criteria or omitted for all criteria.");
        if (anyWeight && Math.Abs(weightSum - 1m) > WeightSumTolerance)
            throw InvalidInput($"Pareto weights must sum to 1 within an absolute tolerance of {WeightSumTolerance}.");

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

        return anyWeight;
    }

    private static decimal[,] BuildCostMatrix(
        IReadOnlyList<CalculationCriterionInput> criteria,
        IReadOnlyList<CalculationAlternativeInput> alternatives,
        CancellationToken cancellationToken)
    {
        var costs = new decimal[alternatives.Count, criteria.Count];
        for (var criterionIndex = 0; criterionIndex < criteria.Count; criterionIndex++)
        {
            var criterion = criteria[criterionIndex];
            for (var alternativeIndex = 0; alternativeIndex < alternatives.Count; alternativeIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var value = alternatives[alternativeIndex].Values[criterionIndex];
                costs[alternativeIndex, criterionIndex] = criterion.Direction switch
                {
                    CriterionDirection.Maximize => -value,
                    CriterionDirection.Minimize => value,
                    CriterionDirection.Target => Math.Abs(value - criterion.TargetValue!.Value),
                    _ => throw InvalidInput($"Criterion {criterion.CriterionId} has an unsupported direction.")
                };
            }
        }

        return costs;
    }

    private static int[] BuildFronts(
        decimal[,] costs,
        IReadOnlyList<CalculationAlternativeInput> alternatives,
        CancellationToken cancellationToken)
    {
        var count = alternatives.Count;
        var dominatedBy = new List<int>[count];
        var dominationCount = new int[count];
        for (var i = 0; i < count; i++)
            dominatedBy[i] = [];

        for (var left = 0; left < count; left++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var right = left + 1; right < count; right++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var dominance = CompareDominance(costs, left, right, cancellationToken);
                if (dominance > 0)
                {
                    dominatedBy[left].Add(right);
                    dominationCount[right]++;
                }
                else if (dominance < 0)
                {
                    dominatedBy[right].Add(left);
                    dominationCount[left]++;
                }
            }
        }

        var fronts = new int[count];
        var currentFront = Enumerable.Range(0, count)
            .Where(index => dominationCount[index] == 0)
            .ToList();
        var assigned = 0;
        var frontNumber = 1;

        while (assigned < count)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (currentFront.Count == 0)
                throw new CalculationAlgorithmException(NumericErrorCode, "Pareto sorting could not assign all variants to fronts.");

            currentFront.Sort();
            var nextFront = new List<int>();
            foreach (var current in currentFront)
            {
                cancellationToken.ThrowIfCancellationRequested();
                fronts[current] = frontNumber;
                assigned++;

                foreach (var dominated in dominatedBy[current])
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    dominationCount[dominated]--;
                    if (dominationCount[dominated] == 0)
                        nextFront.Add(dominated);
                }
            }

            currentFront = nextFront;
            frontNumber++;
        }

        return fronts;
    }

    // Returns 1 if left dominates right, -1 if right dominates left, and 0 otherwise.
    private static int CompareDominance(
        decimal[,] costs,
        int left,
        int right,
        CancellationToken cancellationToken)
    {
        var leftNoWorse = true;
        var rightNoWorse = true;
        var leftStrictlyBetter = false;
        var rightStrictlyBetter = false;

        for (var criterion = 0; criterion < costs.GetLength(1); criterion++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var leftCost = costs[left, criterion];
            var rightCost = costs[right, criterion];
            if (leftCost > rightCost)
                leftNoWorse = false;
            else if (leftCost < rightCost)
                leftStrictlyBetter = true;

            if (rightCost > leftCost)
                rightNoWorse = false;
            else if (rightCost < leftCost)
                rightStrictlyBetter = true;

            if (!leftNoWorse && !rightNoWorse)
                return 0;
        }

        if (leftNoWorse && leftStrictlyBetter)
            return 1;
        if (rightNoWorse && rightStrictlyBetter)
            return -1;
        return 0;
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
            throw InvalidInput("Pareto weights must have a positive total.");

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
            cancellationToken.ThrowIfCancellationRequested();
            if (normalized[i] > normalized[correctionIndex])
                correctionIndex = i;
        }

        rounded[correctionIndex] += remainder;
        var finalSum = 0m;
        foreach (var weight in rounded)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (weight is < 0m or > 1m)
                throw new CalculationAlgorithmException(NumericErrorCode, "Pareto weight rounding produced invalid weights.");
            finalSum += weight;
        }

        if (finalSum != 1m)
            throw new CalculationAlgorithmException(NumericErrorCode, "Pareto weight rounding produced invalid weights.");

        return rounded;
    }

    private static decimal[] CalculateSecondaryScores(
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
            var transformed = new decimal[alternatives.Count];
            for (var alternativeIndex = 0; alternativeIndex < alternatives.Count; alternativeIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var value = alternatives[alternativeIndex].Values[criterionIndex];
                transformed[alternativeIndex] = criterion.Direction == CriterionDirection.Target
                    ? Math.Abs(value - criterion.TargetValue!.Value)
                    : value;
            }

            var minimum = transformed[0];
            var maximum = transformed[0];
            for (var alternativeIndex = 1; alternativeIndex < transformed.Length; alternativeIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                minimum = Math.Min(minimum, transformed[alternativeIndex]);
                maximum = Math.Max(maximum, transformed[alternativeIndex]);
            }

            for (var alternativeIndex = 0; alternativeIndex < alternatives.Count; alternativeIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var value = transformed[alternativeIndex];
                utilities[alternativeIndex, criterionIndex] = minimum == maximum
                    ? 0.5m
                    : criterion.Direction switch
                    {
                        CriterionDirection.Maximize => (value - minimum) / (maximum - minimum),
                        CriterionDirection.Minimize or CriterionDirection.Target => (maximum - value) / (maximum - minimum),
                        _ => throw InvalidInput($"Criterion {criterion.CriterionId} has an unsupported direction.")
                    };
            }
        }

        var scores = new decimal[alternatives.Count];
        for (var alternativeIndex = 0; alternativeIndex < alternatives.Count; alternativeIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var score = 0m;
            for (var criterionIndex = 0; criterionIndex < criteria.Count; criterionIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                score += weights[criterionIndex] * utilities[alternativeIndex, criterionIndex];
            }

            if (score is < 0m or > 1m)
                throw new CalculationAlgorithmException(NumericErrorCode, "Pareto secondary score is outside [0,1].");
            scores[alternativeIndex] = score;
        }

        return scores;
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
