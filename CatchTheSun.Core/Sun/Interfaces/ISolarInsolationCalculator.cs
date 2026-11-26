using CatchTheSun.Core.Sun.Models;

namespace CatchTheSun.Core.Sun.Interfaces;

public interface ISolarInsolationCalculator
{
    SolarInsolationOutput Calculate(
        SolarInsolationInput input,
        CancellationToken cancellationToken = default);
}
