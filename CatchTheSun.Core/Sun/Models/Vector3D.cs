namespace CatchTheSun.Core.Sun.Models;

/// <summary>A vector in local ENU axes: X east, Y true north, Z up.</summary>
public readonly record struct Vector3D(double X, double Y, double Z)
{
    public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);
    public bool IsFinite => double.IsFinite(X) && double.IsFinite(Y) && double.IsFinite(Z);

    public double Dot(Vector3D other) => X * other.X + Y * other.Y + Z * other.Z;

    public Vector3D Normalize()
    {
        var length = Length;
        if (!double.IsFinite(length) || length <= 0)
            throw new ArgumentException("A finite, non-zero vector is required.");
        return new Vector3D(X / length, Y / length, Z / length);
    }
}
