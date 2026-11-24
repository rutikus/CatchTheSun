namespace CatchTheSun.Core.CalculationEngine.Models;

/// <summary>
/// AHP diagnostics use double because the principal-eigenvector calculation
/// and CR are evaluated in floating-point precision. Scores, weights and inputs remain decimal.
/// </summary>
public sealed record CalculationDiagnostics(
    double? ConsistencyRatio,
    double? LambdaMax,
    int? IterationCount);
