using CatchTheSun.Core.Sun.Algorithms;
using CatchTheSun.Core.Sun.Interfaces;
using CatchTheSun.Core.Sun.Models;
using Xunit;

namespace CatchTheSun.Tests.Sun;

public sealed class SolarInsolationCalculatorTests
{
    private static readonly SolarLocation UtcLocation = new(0, 0, "UTC");
    private static readonly SolarReceiver Receiver = new(new Guid(1, 0, 0, new byte[8]), new Point3D(0, 0, 1));
    private static readonly DateOnly Date = new(2024, 6, 21);

    [Fact]
    public void UnobstructedReceiverCountsEntireDaytimePeriod()
    {
        var calculator = CreateCalculator(_ => new SunPosition(30, 180));

        var result = calculator.Calculate(Input("08:00", "10:00", 30));

        Assert.Equal(120, GetReceiver(result).DurationMinutes);
        Assert.Equal(2, GetReceiver(result).DurationHours);
    }

    [Fact]
    public void ReceiverBlockedForEverySampleHasZeroDuration()
    {
        var southBuilding = Building(10, Rectangle(-1, -5, 1, -4), 10);
        var calculator = CreateCalculator(_ => new SunPosition(30, 180));

        var result = calculator.Calculate(Input("08:00", "10:00", 30, [southBuilding]));

        Assert.Equal(0, GetReceiver(result).DurationMinutes);
    }

    [Fact]
    public void PartialShadingAccumulatesOnlyUnblockedIntervals()
    {
        var southBuilding = Building(10, Rectangle(-1, -5, 1, -4), 10);
        var switchTime = Utc(2024, 6, 21, 9, 0);
        var calculator = CreateCalculator(
            instant => instant < switchTime ? new SunPosition(30, 180) : new SunPosition(30, 0));

        var result = calculator.Calculate(Input("08:00", "10:00", 30, [southBuilding]));

        Assert.Equal(60, GetReceiver(result).DurationMinutes);
    }

    [Fact]
    public void MultipleBuildingsArePassedThroughShadowCalculation()
    {
        var buildingA = Building(10, Rectangle(-1, -5, 1, -4), 10);
        var buildingB = Building(11, Rectangle(-1, -8, 1, -7), 10);
        var recordingShadow = new RecordingShadowCalculator(new PrismaticShadowCalculator());
        var calculator = new SampledSolarInsolationCalculator(
            new FunctionalSolarPositionProvider(_ => new SunPosition(30, 180)), recordingShadow);

        var result = calculator.Calculate(Input("08:00", "09:00", 30, [buildingA, buildingB]));

        Assert.Equal(0, GetReceiver(result).DurationMinutes);
        Assert.All(recordingShadow.BuildingCounts, count => Assert.Equal(2, count));
        Assert.Equal(2, recordingShadow.BuildingCounts.Count);
    }

    [Fact]
    public void NightIntervalsDoNotAccumulateIllumination()
    {
        var calculator = CreateCalculator(_ => new SunPosition(-5, 180));

        var result = calculator.Calculate(Input("00:00", "02:00", 30));

        Assert.Equal(0, GetReceiver(result).DurationMinutes);
    }

    [Fact]
    public void PartialFinalIntervalIsCountedExactlyWithoutExtraMinutes()
    {
        var calculator = CreateCalculator(_ => new SunPosition(20, 180));

        var result = calculator.Calculate(Input("08:00", "09:05", 60));

        Assert.Equal(65, GetReceiver(result).DurationMinutes);
        Assert.Equal(Utc(2024, 6, 21, 8, 0), result.PeriodStartUtc);
        Assert.Equal(Utc(2024, 6, 21, 9, 5), result.PeriodEndUtc);
    }

    [Theory]
    [InlineData(10, 12)]
    [InlineData(15, 8)]
    [InlineData(30, 4)]
    [InlineData(60, 2)]
    public void SupportedStepsCoverThePeriodExactlyOnce(int stepMinutes, int expectedSampleCount)
    {
        var provider = new FunctionalSolarPositionProvider(_ => new SunPosition(20, 180));
        var calculator = new SampledSolarInsolationCalculator(provider, new PrismaticShadowCalculator());

        var result = calculator.Calculate(Input("08:00", "10:00", stepMinutes));

        Assert.Equal(120, GetReceiver(result).DurationMinutes);
        Assert.Equal(expectedSampleCount, provider.Instants.Count);
        Assert.Equal(provider.Instants.Count, provider.Instants.Distinct().Count());
        Assert.All(provider.Instants, instant => Assert.Equal(TimeSpan.Zero, instant.Offset));
        Assert.Equal(stepMinutes, result.TimeStepMinutes);
    }

    [Fact]
    public void PeriodMayCrossLocalMidnight()
    {
        var location = UtcLocation;
        var calculator = CreateCalculator(_ => new SunPosition(20, 180));
        var input = new SolarInsolationInput(
            location, [], [Receiver], new DateOnly(2024, 6, 21), new TimeOnly(23, 30), new TimeOnly(0, 30), 30);

        var result = calculator.Calculate(input);

        Assert.Equal(60, GetReceiver(result).DurationMinutes);
        Assert.Equal(new DateTime(2024, 6, 22, 0, 30, 0, DateTimeKind.Unspecified), result.LocalEnd);
    }

    [Fact]
    public void SpringDstGapIsSkippedAndElapsedUtcDurationIsUsed()
    {
        var location = new SolarLocation(40.7, -74, "America/New_York");
        var provider = new FunctionalSolarPositionProvider(_ => new SunPosition(20, 180));
        var calculator = new SampledSolarInsolationCalculator(provider, new PrismaticShadowCalculator());
        var input = new SolarInsolationInput(
            location, [], [Receiver], new DateOnly(2024, 3, 10), new TimeOnly(0, 0), new TimeOnly(4, 0), 60);

        var result = calculator.Calculate(input);

        Assert.Equal(180, GetReceiver(result).DurationMinutes);
        Assert.Equal(TimeSpan.FromHours(3), result.PeriodEndUtc - result.PeriodStartUtc);
        Assert.Equal(3, provider.Instants.Count);
        Assert.Equal(provider.Instants.Count, provider.Instants.Distinct().Count());
    }

    [Fact]
    public void FallDstRepeatedHourIsCountedAsDistinctElapsedTimeWithoutDuplicateInstants()
    {
        var location = new SolarLocation(40.7, -74, "America/New_York");
        var provider = new FunctionalSolarPositionProvider(_ => new SunPosition(20, 180));
        var calculator = new SampledSolarInsolationCalculator(provider, new PrismaticShadowCalculator());
        var input = new SolarInsolationInput(
            location, [], [Receiver], new DateOnly(2024, 11, 3), new TimeOnly(0, 0), new TimeOnly(4, 0), 60);

        var result = calculator.Calculate(input);

        Assert.Equal(300, GetReceiver(result).DurationMinutes);
        Assert.Equal(TimeSpan.FromHours(5), result.PeriodEndUtc - result.PeriodStartUtc);
        Assert.Equal(5, provider.Instants.Count);
        Assert.Equal(provider.Instants.Count, provider.Instants.Distinct().Count());
    }

    [Fact]
    public void AmbiguousLocalWindowBoundariesIncludeTheWholeRepeatedSpan()
    {
        var location = new SolarLocation(40.7, -74, "America/New_York");
        var calculator = new SampledSolarInsolationCalculator(
            new FunctionalSolarPositionProvider(_ => new SunPosition(20, 180)), new PrismaticShadowCalculator());
        var input = new SolarInsolationInput(
            location, [], [Receiver], new DateOnly(2024, 11, 3), new TimeOnly(1, 0), new TimeOnly(1, 30), 15);

        var result = calculator.Calculate(input);

        Assert.Equal(90, GetReceiver(result).DurationMinutes);
    }

    [Fact]
    public void RejectsPeriodBoundaryInsideSpringForwardGap()
    {
        var location = new SolarLocation(40.7, -74, "America/New_York");
        var calculator = CreateCalculator(_ => new SunPosition(20, 180));
        var input = new SolarInsolationInput(
            location, [], [Receiver], new DateOnly(2024, 3, 10), new TimeOnly(2, 0), new TimeOnly(4, 0), 30);

        Assert.Throws<ArgumentException>(() => calculator.Calculate(input));
    }

    [Fact]
    public void IdenticalInputProducesIdenticalResults()
    {
        var calculator = CreateCalculator(_ => new SunPosition(20, 180));
        var input = Input("08:00", "10:00", 15);

        var first = calculator.Calculate(input);
        var second = calculator.Calculate(input);

        Assert.Equal(first.PeriodStartUtc, second.PeriodStartUtc);
        Assert.Equal(first.PeriodEndUtc, second.PeriodEndUtc);
        Assert.Equal(first.Receivers, second.Receivers);
    }

    [Fact]
    public void FinerStepIsCloserToKnownTransitionDurationThanHourlySampling()
    {
        var threshold = Utc(2024, 6, 21, 6, 37);
        var provider = new FunctionalSolarPositionProvider(instant =>
            instant < threshold ? new SunPosition(-1, 180) : new SunPosition(20, 180));
        var calculator = new SampledSolarInsolationCalculator(provider, new PrismaticShadowCalculator());
        var exactDuration = 83d; // From 06:37 through 08:00 UTC.

        var hourly = calculator.Calculate(Input("06:00", "08:00", 60));
        var tenMinute = calculator.Calculate(Input("06:00", "08:00", 10));
        var hourlyError = Math.Abs(GetReceiver(hourly).DurationMinutes - exactDuration);
        var tenMinuteError = Math.Abs(GetReceiver(tenMinute).DurationMinutes - exactDuration);

        Assert.True(tenMinuteError < hourlyError);
        Assert.InRange(tenMinuteError, 0, 5);
    }

    [Fact]
    public void CancellationIsObservedBetweenIntervals()
    {
        using var cancellation = new CancellationTokenSource();
        var provider = new FunctionalSolarPositionProvider(_ =>
        {
            cancellation.Cancel();
            return new SunPosition(20, 180);
        });
        var calculator = new SampledSolarInsolationCalculator(provider, new PrismaticShadowCalculator());

        Assert.Throws<OperationCanceledException>(() => calculator.Calculate(
            Input("08:00", "10:00", 30), cancellation.Token));
    }

    private static SolarInsolationInput Input(
        string start,
        string end,
        int step,
        IReadOnlyList<BuildingGeometry>? buildings = null,
        SolarReceiver? receiver = null) =>
        new(
            UtcLocation,
            buildings ?? [],
            [receiver ?? Receiver],
            Date,
            TimeOnly.Parse(start),
            TimeOnly.Parse(end),
            step);

    private static SampledSolarInsolationCalculator CreateCalculator(
        Func<DateTimeOffset, SunPosition> positionAt) =>
        new(new FunctionalSolarPositionProvider(positionAt), new PrismaticShadowCalculator());

    private static SolarInsolationResult GetReceiver(SolarInsolationOutput output) =>
        Assert.Single(output.Receivers);

    private static DateTimeOffset Utc(int year, int month, int day, int hour, int minute = 0) =>
        new(year, month, day, hour, minute, 0, TimeSpan.Zero);

    private static BuildingGeometry Building(int id, BuildingFootprint footprint, double height) =>
        new(new Guid(id, 0, 0, new byte[8]), footprint, 0, height);

    private static BuildingFootprint Rectangle(double minX, double minY, double maxX, double maxY) =>
        new([new(minX, minY), new(maxX, minY), new(maxX, maxY), new(minX, maxY)]);

    private sealed class FunctionalSolarPositionProvider(Func<DateTimeOffset, SunPosition> positionAt)
        : ISolarPositionProvider
    {
        public List<DateTimeOffset> Instants { get; } = [];

        public SunPosition GetPosition(SolarLocation location, DateTimeOffset instant)
        {
            Instants.Add(instant);
            return positionAt(instant);
        }
    }

    private sealed class RecordingShadowCalculator(IShadowCalculator inner) : IShadowCalculator
    {
        public List<int> BuildingCounts { get; } = [];

        public IReadOnlyList<Guid> GetBlockingBuildingIds(
            SolarReceiver receiver,
            SunPosition sunPosition,
            IReadOnlyList<BuildingGeometry> buildings)
        {
            BuildingCounts.Add(buildings.Count);
            return inner.GetBlockingBuildingIds(receiver, sunPosition, buildings);
        }
    }
}
