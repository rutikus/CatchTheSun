namespace CatchTheSun.Core.Sun.Models;

/// <summary>A point in the local horizontal plane, in metres (X east, Y true north).</summary>
public readonly record struct Point2D(double X, double Y)
{
    public bool IsFinite => double.IsFinite(X) && double.IsFinite(Y);
}
