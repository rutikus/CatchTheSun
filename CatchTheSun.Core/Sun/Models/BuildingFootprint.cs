namespace CatchTheSun.Core.Sun.Models;

/// <summary>A simple, planar building footprint in local metres; holes are not supported.</summary>
public sealed class BuildingFootprint
{
    private const double GeometryTolerance = 1e-9;

    public IReadOnlyList<Point2D> Vertices { get; }

    public BuildingFootprint(IEnumerable<Point2D> vertices)
    {
        ArgumentNullException.ThrowIfNull(vertices);
        var points = vertices.ToArray();
        if (points.Length > 1 && points[0] == points[^1])
            points = points[..^1];
        if (points.Length < 3)
            throw new ArgumentException("A footprint needs at least three distinct vertices.", nameof(vertices));
        if (points.Any(point => !point.IsFinite))
            throw new ArgumentException("Footprint coordinates must be finite.", nameof(vertices));
        if (points.Distinct().Count() != points.Length)
            throw new ArgumentException("Footprint vertices must not repeat.", nameof(vertices));
        if (Math.Abs(SignedDoubleArea(points)) <= GeometryTolerance)
            throw new ArgumentException("Footprint area must be non-zero.", nameof(vertices));
        if (HasSelfIntersection(points))
            throw new ArgumentException("Footprint must be a simple polygon without self-intersections.", nameof(vertices));

        Vertices = Array.AsReadOnly(points);
    }

    internal bool ContainsOrTouches(double x, double y)
    {
        var inside = false;
        for (var i = 0; i < Vertices.Count; i++)
        {
            var a = Vertices[i];
            var b = Vertices[(i + 1) % Vertices.Count];
            if (PointOnSegment(x, y, a, b))
                return true;

            if ((a.Y > y) != (b.Y > y) &&
                x < (b.X - a.X) * (y - a.Y) / (b.Y - a.Y) + a.X)
                inside = !inside;
        }
        return inside;
    }

    internal bool SegmentIntersectsOrTouches(Point2D start, Point2D end)
    {
        if (ContainsOrTouches(start.X, start.Y) || ContainsOrTouches(end.X, end.Y))
            return true;

        for (var i = 0; i < Vertices.Count; i++)
        {
            var a = Vertices[i];
            var b = Vertices[(i + 1) % Vertices.Count];
            if (SegmentsIntersect(start, end, a, b))
                return true;
        }
        return false;
    }

    private static double SignedDoubleArea(IReadOnlyList<Point2D> points)
    {
        var area = 0d;
        for (var i = 0; i < points.Count; i++)
        {
            var a = points[i];
            var b = points[(i + 1) % points.Count];
            area += a.X * b.Y - b.X * a.Y;
        }
        return area;
    }

    private static bool HasSelfIntersection(IReadOnlyList<Point2D> points)
    {
        for (var i = 0; i < points.Count; i++)
        {
            var a1 = points[i];
            var a2 = points[(i + 1) % points.Count];
            for (var j = i + 1; j < points.Count; j++)
            {
                if (j == i || j == (i + 1) % points.Count || i == (j + 1) % points.Count)
                    continue;
                var b1 = points[j];
                var b2 = points[(j + 1) % points.Count];
                if (SegmentsIntersect(a1, a2, b1, b2))
                    return true;
            }
        }
        return false;
    }

    private static bool SegmentsIntersect(Point2D a, Point2D b, Point2D c, Point2D d)
    {
        var o1 = Orientation(a, b, c);
        var o2 = Orientation(a, b, d);
        var o3 = Orientation(c, d, a);
        var o4 = Orientation(c, d, b);
        if (((o1 > GeometryTolerance && o2 < -GeometryTolerance) ||
             (o1 < -GeometryTolerance && o2 > GeometryTolerance)) &&
            ((o3 > GeometryTolerance && o4 < -GeometryTolerance) ||
             (o3 < -GeometryTolerance && o4 > GeometryTolerance)))
            return true;

        return Math.Abs(o1) <= GeometryTolerance && PointOnSegment(c.X, c.Y, a, b) ||
               Math.Abs(o2) <= GeometryTolerance && PointOnSegment(d.X, d.Y, a, b) ||
               Math.Abs(o3) <= GeometryTolerance && PointOnSegment(a.X, a.Y, c, d) ||
               Math.Abs(o4) <= GeometryTolerance && PointOnSegment(b.X, b.Y, c, d);
    }

    private static double Orientation(Point2D a, Point2D b, Point2D c) =>
        (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);

    private static bool PointOnSegment(double x, double y, Point2D a, Point2D b)
    {
        var cross = (x - a.X) * (b.Y - a.Y) - (y - a.Y) * (b.X - a.X);
        if (Math.Abs(cross) > GeometryTolerance)
            return false;
        return x >= Math.Min(a.X, b.X) - GeometryTolerance && x <= Math.Max(a.X, b.X) + GeometryTolerance &&
               y >= Math.Min(a.Y, b.Y) - GeometryTolerance && y <= Math.Max(a.Y, b.Y) + GeometryTolerance;
    }
}
