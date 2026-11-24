using CatchTheSun.Core.Entities;

namespace CatchTheSun.Core.CalculationEngine.Models;

/// <summary>
/// Immutable algorithm input. Alternative values are reordered with their criteria
/// into canonical CriterionId order during construction.
/// </summary>
public sealed class CalculationInput
{
    public CalculationMethod Method { get; }
    public IReadOnlyList<CalculationCriterionInput> Criteria { get; }
    public IReadOnlyList<CalculationAlternativeInput> Alternatives { get; }
    public IReadOnlyList<CalculationCriterionComparisonInput> Comparisons { get; }
    public CalculationAlgorithmOptions Options { get; }

    public CalculationInput(
        CalculationMethod method,
        IEnumerable<CalculationCriterionInput> criteria,
        IEnumerable<CalculationAlternativeInput> alternatives,
        IEnumerable<CalculationCriterionComparisonInput> comparisons,
        CalculationAlgorithmOptions options)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        ArgumentNullException.ThrowIfNull(alternatives);
        ArgumentNullException.ThrowIfNull(comparisons);
        ArgumentNullException.ThrowIfNull(options);

        var suppliedCriteria = criteria.ToArray();
        var canonicalCriteria = suppliedCriteria
            .Select((criterion, originalIndex) => (criterion, originalIndex))
            .OrderBy(x => x.criterion.CriterionId.ToString("N"), StringComparer.Ordinal)
            .ToArray();

        if (canonicalCriteria.Select(x => x.criterion.CriterionId).Distinct().Count() != canonicalCriteria.Length)
            throw new ArgumentException("Criterion IDs must be unique.", nameof(criteria));

        var suppliedAlternatives = alternatives.ToArray();
        foreach (var alternative in suppliedAlternatives)
        {
            if (alternative.Values.Count != suppliedCriteria.Length)
                throw new ArgumentException(
                    "Each alternative must have exactly one value for every supplied criterion.",
                    nameof(alternatives));
        }

        Method = method;
        Criteria = Array.AsReadOnly(canonicalCriteria.Select(x => x.criterion).ToArray());
        Alternatives = Array.AsReadOnly(suppliedAlternatives
            .Select(alternative => new CalculationAlternativeInput(
                alternative.VariantId,
                canonicalCriteria.Select(x => alternative.Values[x.originalIndex])))
            .ToArray());
        Comparisons = Array.AsReadOnly(comparisons
            .OrderBy(x => x.CriterionAId.ToString("N"), StringComparer.Ordinal)
            .ThenBy(x => x.CriterionBId.ToString("N"), StringComparer.Ordinal)
            .ToArray());
        Options = options;
    }
}
