namespace CatchTheSun.Core.Sun.Models;

/// <summary>
/// Geographic position used for solar calculations. Latitude/longitude are WGS84 degrees;
/// longitude is positive east. TimeZoneId is an IANA timezone (for example, Asia/Yekaterinburg).
/// </summary>
public sealed record SolarLocation
{
    public double LatitudeDegrees { get; }
    public double LongitudeDegrees { get; }
    public string TimeZoneId { get; }

    public SolarLocation(double latitudeDegrees, double longitudeDegrees, string timeZoneId)
    {
        if (!double.IsFinite(latitudeDegrees) || latitudeDegrees is < -90 or > 90)
            throw new ArgumentOutOfRangeException(nameof(latitudeDegrees));
        if (!double.IsFinite(longitudeDegrees) || longitudeDegrees is < -180 or > 180)
            throw new ArgumentOutOfRangeException(nameof(longitudeDegrees));
        if (string.IsNullOrWhiteSpace(timeZoneId))
            throw new ArgumentException("An IANA timezone identifier is required.", nameof(timeZoneId));

        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException exception)
        {
            throw new ArgumentException($"Unknown timezone '{timeZoneId}'.", nameof(timeZoneId), exception);
        }

        LatitudeDegrees = latitudeDegrees;
        LongitudeDegrees = longitudeDegrees;
        TimeZoneId = timeZoneId;
    }
}
