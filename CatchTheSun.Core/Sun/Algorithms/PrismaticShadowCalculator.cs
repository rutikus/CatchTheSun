using CatchTheSun.Core.Sun.Interfaces;
using CatchTheSun.Core.Sun.Models;

namespace CatchTheSun.Core.Sun.Algorithms;

/// <summary>Ray/prism occlusion for vertical extrusions of simple footprints.</summary>
public sealed class PrismaticShadowCalculator : IShadowCalculator
{
    private const double DirectionTolerance = 1e-12;
    private const double RayOriginOffsetMeters = 1e-7;

    public IReadOnlyList<Guid> GetBlockingBuildingIds(
        SolarReceiver receiver,
        SunPosition sunPosition,
        IReadOnlyList<BuildingGeometry> buildings)
    {
        ArgumentNullException.ThrowIfNull(receiver);
        ArgumentNullException.ThrowIfNull(sunPosition);
        ArgumentNullException.ThrowIfNull(buildings);

        var direction = sunPosition.DirectionToSun;
        if (sunPosition.AltitudeDegrees <= 0 || direction.Z <= DirectionTolerance)
            return Array.Empty<Guid>();
        if (receiver.SurfaceNormal is { } normal && normal.Dot(direction) <= DirectionTolerance)
            return Array.Empty<Guid>();

        return buildings
            .Where(building => building is not null && BlocksRay(receiver.Position, direction, building))
            .Select(building => building.Id)
            .OrderBy(id => id)
            .ToArray();
    }

    private static bool BlocksRay(Point3D origin, Vector3D direction, BuildingGeometry building)
    {
        var top = building.BaseElevationMeters + building.HeightMeters;
        var entersAt = (building.BaseElevationMeters - origin.Z) / direction.Z;
        var exitsAt = (top - origin.Z) / direction.Z;
        if (exitsAt <= RayOriginOffsetMeters)
            return false;

        var startAt = Math.Max(entersAt, RayOriginOffsetMeters);
        if (startAt > exitsAt)
            return false;

        var start = new Point2D(origin.X + startAt * direction.X, origin.Y + startAt * direction.Y);
        var end = new Point2D(origin.X + exitsAt * direction.X, origin.Y + exitsAt * direction.Y);
        return building.Footprint.SegmentIntersectsOrTouches(start, end);
    }
}
