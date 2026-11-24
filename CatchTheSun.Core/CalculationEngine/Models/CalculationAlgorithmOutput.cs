using CatchTheSun.Core.Entities;

namespace CatchTheSun.Core.CalculationEngine.Models;

public sealed class CalculationAlgorithmOutput
{
    public CalculationMethod Method { get; }
    public IReadOnlyList<CalculationAlternativeOutput> Results { get; }
    public IReadOnlyList<CalculationCriterionWeight> CriterionWeights { get; }
    public CalculationDiagnostics Diagnostics { get; }

    public CalculationAlgorithmOutput(
        CalculationMethod method,
        IEnumerable<CalculationAlternativeOutput> results,
        IEnumerable<CalculationCriterionWeight> criterionWeights,
        CalculationDiagnostics diagnostics)
    {
        ArgumentNullException.ThrowIfNull(results);
        ArgumentNullException.ThrowIfNull(criterionWeights);
        ArgumentNullException.ThrowIfNull(diagnostics);

        Method = method;
        Results = Array.AsReadOnly(results.ToArray());
        CriterionWeights = Array.AsReadOnly(criterionWeights.ToArray());
        Diagnostics = diagnostics;
    }
}
