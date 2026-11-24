using CatchTheSun.Core.Entities;

namespace CatchTheSun.Core.CalculationEngine.Algorithms;

public interface ICalculationAlgorithmFactory
{
    ICalculationAlgorithm Get(CalculationMethod method);
}
