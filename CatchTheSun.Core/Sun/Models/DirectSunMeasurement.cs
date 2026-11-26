namespace CatchTheSun.Core.Sun.Models;

/// <summary>A single Ladybug Direct Sun Hours sensor sample. RawRow preserves every source value verbatim.</summary>
public sealed record DirectSunMeasurement(
    string SensorId, Guid FeatureId, string RecreationalType, double XMetres, double YMetres,
    double ZMetres, double HeightAboveZoneMetres, double GridSpacingMetres,
    double Longitude, double Latitude, double DirectSunHoursTestDay,
    int SunlitSamples, int SampleCountDaylight, string RawRow);
