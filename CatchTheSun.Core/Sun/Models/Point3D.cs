namespace CatchTheSun.Core.Sun.Models;

/// <summary>A local point in metres (X east, Y true north, Z up).</summary>
public readonly record struct Point3D(double X, double Y, double Z)
{
    public bool IsFinite => double.IsFinite(X) && double.IsFinite(Y) && double.IsFinite(Z);
}
