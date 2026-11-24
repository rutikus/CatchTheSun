using CatchTheSun.Core.CalculationEngine.Models;
using CatchTheSun.Core.Entities;

namespace CatchTheSun.Core.CalculationEngine.Algorithms;

public interface ICalculationAlgorithm
{
    CalculationMethod Method { get; }

    CalculationAlgorithmOutput Calculate(
        CalculationInput input,
        CancellationToken cancellationToken);
}
