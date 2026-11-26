using CatchTheSun.Core.Sun.Interfaces;
using CatchTheSun.Core.Sun.Models;

namespace CatchTheSun.Core.Sun.Algorithms;

/// <summary>
/// NOAA's published approximate solar-position equations. This implementation returns geometric
/// altitude (no atmospheric-refraction correction) and azimuth clockwise from true north.
/// It is an approximation, not a claim of NREL SPA accuracy.
/// </summary>
public sealed class NoaaSolarPositionProvider : ISolarPositionProvider
{
    public SunPosition GetPosition(SolarLocation location, DateTimeOffset instant)
    {
        ArgumentNullException.ThrowIfNull(location);
        var utc = instant.ToUniversalTime();
        var julianDay = utc.UtcDateTime.Ticks / (double)TimeSpan.TicksPerDay + 1721425.5;
        var century = (julianDay - 2451545.0) / 36525.0;

        var meanLongitude = NormalizeDegrees(280.46646 + century * (36000.76983 + century * 0.0003032));
        var meanAnomaly = NormalizeDegrees(357.52911 + century * (35999.05029 - 0.0001537 * century));
        var eccentricity = 0.016708634 - century * (0.000042037 + 0.0000001267 * century);
        var anomalyRadians = ToRadians(meanAnomaly);

        var equationOfCenter =
            Math.Sin(anomalyRadians) * (1.914602 - century * (0.004817 + 0.000014 * century)) +
            Math.Sin(2 * anomalyRadians) * (0.019993 - 0.000101 * century) +
            Math.Sin(3 * anomalyRadians) * 0.000289;
        var trueLongitude = meanLongitude + equationOfCenter;
        var omega = 125.04 - 1934.136 * century;
        var apparentLongitude = trueLongitude - 0.00569 - 0.00478 * Math.Sin(ToRadians(omega));

        var meanObliquityArcSeconds =
            21.448 - century * (46.815 + century * (0.00059 - century * 0.001813));
        var meanObliquity = 23 + (26 + meanObliquityArcSeconds / 60) / 60;
        var obliquity = meanObliquity + 0.00256 * Math.Cos(ToRadians(omega));

        var apparentLongitudeRadians = ToRadians(apparentLongitude);
        var obliquityRadians = ToRadians(obliquity);
        var declination = Math.Asin(Math.Sin(obliquityRadians) * Math.Sin(apparentLongitudeRadians));

        var y = Math.Pow(Math.Tan(obliquityRadians / 2), 2);
        var longitudeRadians = ToRadians(meanLongitude);
        var equationOfTimeRadians =
            y * Math.Sin(2 * longitudeRadians) -
            2 * eccentricity * Math.Sin(anomalyRadians) +
            4 * eccentricity * y * Math.Sin(anomalyRadians) * Math.Cos(2 * longitudeRadians) -
            0.5 * y * y * Math.Sin(4 * longitudeRadians) -
            1.25 * eccentricity * eccentricity * Math.Sin(2 * anomalyRadians);
        var equationOfTimeMinutes = 4 * ToDegrees(equationOfTimeRadians);

        var utcMinutes = utc.TimeOfDay.TotalMinutes;
        var trueSolarTimeMinutes = NormalizeMinutes(utcMinutes + equationOfTimeMinutes + 4 * location.LongitudeDegrees);
        var hourAngle = ToRadians(trueSolarTimeMinutes / 4 - 180);
        var latitude = ToRadians(location.LatitudeDegrees);

        var sinAltitude = Math.Sin(latitude) * Math.Sin(declination) +
                          Math.Cos(latitude) * Math.Cos(declination) * Math.Cos(hourAngle);
        var altitudeRadians = Math.Asin(Math.Clamp(sinAltitude, -1, 1));
        var azimuthRadians = Math.Atan2(
            Math.Sin(hourAngle),
            Math.Cos(hourAngle) * Math.Sin(latitude) - Math.Tan(declination) * Math.Cos(latitude)) + Math.PI;

        return new SunPosition(ToDegrees(altitudeRadians), NormalizeDegrees(ToDegrees(azimuthRadians)));
    }

    private static double NormalizeDegrees(double value) => ((value % 360) + 360) % 360;
    private static double NormalizeMinutes(double value) => ((value % 1440) + 1440) % 1440;
    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
    private static double ToDegrees(double radians) => radians * 180 / Math.PI;
}
