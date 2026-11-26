using CatchTheSun.Core.Sun.Models;

namespace CatchTheSun.Core.Sun.Interfaces;

public interface ISunCalculator
{
    SolarCalculationOutput Calculate(SolarCalculationInput input);
}
