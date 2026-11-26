using CatchTheSun.Core.Sun.Models;

namespace CatchTheSun.Core.Sun.Interfaces;

public interface IShadowCalculator
{
    IReadOnlyList<Guid> GetBlockingBuildingIds(
        SolarReceiver receiver,
        SunPosition sunPosition,
        IReadOnlyList<BuildingGeometry> buildings);
}
