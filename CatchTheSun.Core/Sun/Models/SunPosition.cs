namespace CatchTheSun.Core.Sun.Models;

/// <summary>
/// Geometric Sun position. Azimuth is clockwise from true north in [0, 360) degrees;
/// altitude is degrees above the astronomical horizon. Direction points from the receiver toward the Sun.
/// </summary>
public sealed record SunPosition
{
    public double AltitudeDegrees { get; }
    public double AzimuthDegrees { get; }
    public Vector3D DirectionToSun { get; }

    public SunPosition(double altitudeDegrees, double azimuthDegrees)
    {
        if (!double.IsFinite(altitudeDegrees) || altitudeDegrees is < -90 or > 90)
            throw new ArgumentOutOfRangeException(nameof(altitudeDegrees));
        if (!double.IsFinite(azimuthDegrees))
            throw new ArgumentOutOfRangeException(nameof(azimuthDegrees));

        AltitudeDegrees = altitudeDegrees;
        AzimuthDegrees = ((azimuthDegrees % 360) + 360) % 360;
        var altitude = DegreesToRadians(altitudeDegrees);
        var azimuth = DegreesToRadians(AzimuthDegrees);
        DirectionToSun = new Vector3D(
            Math.Cos(altitude) * Math.Sin(azimuth),
            Math.Cos(altitude) * Math.Cos(azimuth),
            Math.Sin(altitude));
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;
}
