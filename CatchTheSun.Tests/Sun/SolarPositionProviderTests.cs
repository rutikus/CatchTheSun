using CatchTheSun.Core.Sun.Algorithms;
using CatchTheSun.Core.Sun.Models;
using Xunit;

namespace CatchTheSun.Tests.Sun;

public sealed class SolarPositionProviderTests
{
    private readonly NoaaSolarPositionProvider _provider = new();

    [Fact]
    public void MatchesPublishedNrelSpaGoldenCaseWithinApproximationTolerance()
    {
        var location = new SolarLocation(39.742476, -105.1786, "America/Denver");
        var instant = new DateTimeOffset(2003, 10, 17, 19, 30, 30, TimeSpan.Zero);

        var result = _provider.GetPosition(location, instant);

        // NREL SPA gives apparent elevation 39.888378° and azimuth 194.340241° for this case.
        // This implementation returns geometric elevation (no refraction correction), so the
        // elevation tolerance includes the roughly 0.016° atmospheric-refraction difference.
        Assert.InRange(result.AltitudeDegrees, 39.85, 39.91);
        Assert.InRange(result.AzimuthDegrees, 194.33, 194.35);
        Assert.InRange(result.DirectionToSun.Length, 0.999999999, 1.000000001);
    }

    [Fact]
    public void SameUtcInstantProducesSamePositionRegardlessOfTimezoneMetadata()
    {
        var instant = new DateTimeOffset(2024, 6, 21, 12, 0, 0, TimeSpan.Zero);
        var denver = _provider.GetPosition(new SolarLocation(39, -105, "America/Denver"), instant);
        var utc = _provider.GetPosition(new SolarLocation(39, -105, "UTC"), instant);

        Assert.Equal(denver.AltitudeDegrees, utc.AltitudeDegrees, 12);
        Assert.Equal(denver.AzimuthDegrees, utc.AzimuthDegrees, 12);
    }

    [Theory]
    [InlineData(0, 30, 0, 0.866025403784, 0.5)]
    [InlineData(90, 0, 1, 0, 0)]
    [InlineData(180, 30, 0, -0.866025403784, 0.5)]
    [InlineData(270, -30, -0.866025403784, 0, -0.5)]
    public void DirectionUsesEastNorthUpAxes(
        double azimuth,
        double altitude,
        double expectedEast,
        double expectedNorth,
        double expectedUp)
    {
        var direction = new SunPosition(altitude, azimuth).DirectionToSun;

        Assert.Equal(expectedEast, direction.X, 10);
        Assert.Equal(expectedNorth, direction.Y, 10);
        Assert.Equal(expectedUp, direction.Z, 10);
    }

    [Fact]
    public void ResolvesLocalTimeUsingIanaTimezoneAndDaylightSavingRules()
    {
        var location = new SolarLocation(39, -105, "America/Denver");
        var local = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Unspecified);

        var utc = SolarTime.ResolveLocalTime(local, location);

        Assert.Equal(new DateTimeOffset(2024, 1, 15, 19, 0, 0, TimeSpan.Zero), utc);
    }

    [Fact]
    public void RejectsInvalidAndUnresolvedAmbiguousLocalTimes()
    {
        var location = new SolarLocation(40, -74, "America/New_York");
        var skipped = new DateTime(2024, 3, 10, 2, 30, 0, DateTimeKind.Unspecified);
        var ambiguous = new DateTime(2024, 11, 3, 1, 30, 0, DateTimeKind.Unspecified);

        Assert.Throws<ArgumentException>(() => SolarTime.ResolveLocalTime(skipped, location));
        Assert.Throws<ArgumentException>(() => SolarTime.ResolveLocalTime(ambiguous, location));

        var offsets = TimeZoneInfo.FindSystemTimeZoneById(location.TimeZoneId).GetAmbiguousTimeOffsets(ambiguous);
        foreach (var offset in offsets)
        {
            var instant = SolarTime.ResolveLocalTime(ambiguous, location, offset);
            Assert.Equal(ambiguous, TimeZoneInfo.ConvertTime(instant, TimeZoneInfo.FindSystemTimeZoneById(location.TimeZoneId)).DateTime);
        }
    }
}
