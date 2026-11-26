using CatchTheSun.Core.Sun.Interfaces;
using CatchTheSun.Core.Sun.Models;

namespace CatchTheSun.Core.Sun.Algorithms;

public sealed class SunCalculator(
    ISolarPositionProvider solarPositionProvider,
    IShadowCalculator shadowCalculator) : ISunCalculator
{
    public SolarCalculationOutput Calculate(SolarCalculationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var position = solarPositionProvider.GetPosition(input.Location, input.Instant);
        var blockers = shadowCalculator.GetBlockingBuildingIds(input.Receiver, position, input.Buildings);
        var receiverFacesSun = input.Receiver.SurfaceNormal is not { } normal ||
                               normal.Dot(position.DirectionToSun) > 1e-12;
        return new SolarCalculationOutput(
            input.Instant,
            position,
            input.Receiver.Id,
            position.AltitudeDegrees > 0 && receiverFacesSun && blockers.Count == 0,
            blockers);
    }
}
