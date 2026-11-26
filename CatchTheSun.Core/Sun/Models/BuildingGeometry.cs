namespace CatchTheSun.Core.Sun.Models;

/// <summary>A vertical prism with a simple flat footprint and vertical sides.</summary>
public sealed record BuildingGeometry
{
    public Guid Id { get; }
    public BuildingFootprint Footprint { get; }
    public double BaseElevationMeters { get; }
    public double HeightMeters { get; }

    public BuildingGeometry(Guid id, BuildingFootprint footprint, double baseElevationMeters, double heightMeters)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Building id must not be empty.", nameof(id));
        ArgumentNullException.ThrowIfNull(footprint);
        if (!double.IsFinite(baseElevationMeters))
            throw new ArgumentOutOfRangeException(nameof(baseElevationMeters));
        if (!double.IsFinite(heightMeters) || heightMeters <= 0)
            throw new ArgumentOutOfRangeException(nameof(heightMeters));
        if (!double.IsFinite(baseElevationMeters + heightMeters))
            throw new ArgumentOutOfRangeException(nameof(heightMeters));

        Id = id;
        Footprint = footprint;
        BaseElevationMeters = baseElevationMeters;
        HeightMeters = heightMeters;
    }
}
