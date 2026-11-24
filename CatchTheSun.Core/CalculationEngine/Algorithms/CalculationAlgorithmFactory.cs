using CatchTheSun.Core.Entities;

namespace CatchTheSun.Core.CalculationEngine.Algorithms;

public sealed class CalculationAlgorithmFactory : ICalculationAlgorithmFactory
{
    private readonly IReadOnlyDictionary<CalculationMethod, ICalculationAlgorithm> _algorithms;

    public CalculationAlgorithmFactory(IEnumerable<ICalculationAlgorithm> algorithms)
    {
        ArgumentNullException.ThrowIfNull(algorithms);
        var registered = algorithms.ToArray();
        if (registered.Select(x => x.Method).Distinct().Count() != registered.Length)
            throw new ArgumentException("Only one algorithm can be registered for each calculation method.", nameof(algorithms));

        _algorithms = registered.ToDictionary(x => x.Method);
    }

    public ICalculationAlgorithm Get(CalculationMethod method) =>
        _algorithms.TryGetValue(method, out var algorithm)
            ? algorithm
            : throw new CalculationAlgorithmException(
                "calculation.algorithm_not_registered",
                $"No calculation algorithm is registered for method '{method}'.");
}
