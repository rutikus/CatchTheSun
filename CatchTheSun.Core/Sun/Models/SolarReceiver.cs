namespace CatchTheSun.Core.Sun.Models;

/// <summary>
/// A control point in local metres. SurfaceNormal is optional: when supplied, only direct sun
/// in the outward half-space illuminates the receiver; when omitted, the receiver is an unoriented point.
/// </summary>
public sealed record SolarReceiver
{
    public Guid Id { get; }
    public Point3D Position { get; }
    public Vector3D? SurfaceNormal { get; }

    public SolarReceiver(Guid id, Point3D position, Vector3D? surfaceNormal = null)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Receiver id must not be empty.", nameof(id));
        if (!position.IsFinite)
            throw new ArgumentException("Receiver coordinates must be finite.", nameof(position));
        if (surfaceNormal is { } normal)
        {
            if (!normal.IsFinite || normal.Length <= 0)
                throw new ArgumentException("Surface normal must be finite and non-zero.", nameof(surfaceNormal));
            SurfaceNormal = normal.Normalize();
        }

        Id = id;
        Position = position;
    }
}
