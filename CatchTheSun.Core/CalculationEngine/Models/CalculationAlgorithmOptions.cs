namespace CatchTheSun.Core.CalculationEngine.Models;

/// <summary>
/// Effective, server-selected algorithm settings. These are not client-overridable options.
/// </summary>
public sealed record CalculationAlgorithmOptions
{
    public string AlgorithmVersion { get; init; } = "1.0.0";
    public decimal AhpMaximumConsistencyRatio { get; init; } = 0.10m;
    public int AhpMaximumCriteria { get; init; } = 20;
    public decimal WeightSumTolerance { get; init; } = 0.000001m;
    public int WeightScale { get; init; } = 8;
    public MidpointRounding WeightRounding { get; init; } = MidpointRounding.ToEven;
    public string TopsisNormalization { get; init; } = "Vector";
    public string TargetStrategy { get; init; } = "AbsoluteDeviation";
}
