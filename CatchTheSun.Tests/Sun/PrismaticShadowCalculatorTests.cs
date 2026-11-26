using CatchTheSun.Core.Sun.Algorithms;
using CatchTheSun.Core.Sun.Models;
using Xunit;

namespace CatchTheSun.Tests.Sun;

public sealed class PrismaticShadowCalculatorTests
{
    private readonly PrismaticShadowCalculator _shadowCalculator = new();
    private readonly SolarReceiver _receiver = new(Guid.NewGuid(), new Point3D(0, 0, 1));
    private readonly SunPosition _southernSun = new(30, 180);

    [Fact]
    public void OpenSpaceWithoutBuildingsIsSunlit()
    {
        var output = Calculate(_receiver, _southernSun, []);

        Assert.True(output.IsSunlit);
        Assert.Empty(output.BlockingBuildingIds);
    }

    [Fact]
    public void BuildingDirectlyOnRayBlocksSun()
    {
        var building = Building(1, Rectangle(-1, -5, 1, -4), 10);

        var output = Calculate(_receiver, _southernSun, [building]);

        Assert.False(output.IsSunlit);
        Assert.Equal([building.Id], output.BlockingBuildingIds);
    }

    [Fact]
    public void BuildingOutsideRayDoesNotBlockSun()
    {
        var building = Building(2, Rectangle(5, -5, 6, -4), 10);

        var output = Calculate(_receiver, _southernSun, [building]);

        Assert.True(output.IsSunlit);
        Assert.Empty(output.BlockingBuildingIds);
    }

    [Fact]
    public void ReportsMultipleBuildingsIntersectedBySameRayInStableOrder()
    {
        var farther = Building(2, Rectangle(-1, -8, 1, -7), 10);
        var nearer = Building(1, Rectangle(-1, -5, 1, -4), 10);

        var output = Calculate(_receiver, _southernSun, [farther, nearer]);

        Assert.False(output.IsSunlit);
        Assert.Equal(new[] { nearer.Id, farther.Id }, output.BlockingBuildingIds);
    }

    [Fact]
    public void TallerBuildingBlocksRayThatShortBuildingDoesNot()
    {
        var low = Building(1, Rectangle(-1, -5, 1, -4), 0.5);
        var tall = Building(2, Rectangle(-1, -5, 1, -4), 10);

        Assert.True(Calculate(_receiver, _southernSun, [low]).IsSunlit);
        Assert.False(Calculate(_receiver, _southernSun, [tall]).IsSunlit);
    }

    [Fact]
    public void NightAndSunOnHorizonDoNotIlluminateReceiver()
    {
        Assert.False(Calculate(_receiver, new SunPosition(-5, 180), []).IsSunlit);
        Assert.False(Calculate(_receiver, new SunPosition(0, 180), []).IsSunlit);
    }

    [Fact]
    public void ReceiverSurfaceMustFaceSun()
    {
        var southFacing = new SolarReceiver(Guid.NewGuid(), _receiver.Position, new Vector3D(0, -1, 0));
        var northFacing = new SolarReceiver(Guid.NewGuid(), _receiver.Position, new Vector3D(0, 1, 0));

        Assert.True(Calculate(southFacing, _southernSun, []).IsSunlit);
        Assert.False(Calculate(northFacing, _southernSun, []).IsSunlit);
    }

    [Fact]
    public void FootprintBoundaryTouchIsConservativelyTreatedAsOcclusion()
    {
        var building = Building(1, Rectangle(-1, -5, 0, -4), 10);

        var output = Calculate(_receiver, _southernSun, [building]);

        Assert.False(output.IsSunlit);
        Assert.Contains(building.Id, output.BlockingBuildingIds);
    }

    [Fact]
    public void BuildingBehindReceiverDoesNotBlockRayTowardSun()
    {
        var building = Building(1, Rectangle(-1, 4, 1, 5), 10);

        Assert.True(Calculate(_receiver, _southernSun, [building]).IsSunlit);
    }

    [Fact]
    public void SolarCalculationCanBeCalledWithoutInfrastructure()
    {
        var calculator = new SunCalculator(new NoaaSolarPositionProvider(), _shadowCalculator);
        var input = new SolarCalculationInput(
            new SolarLocation(0, 0, "UTC"),
            new DateTimeOffset(2024, 6, 21, 12, 0, 0, TimeSpan.Zero),
            _receiver,
            []);

        var output = calculator.Calculate(input);

        Assert.True(output.IsSunlit);
        Assert.Equal(input.Instant, output.InstantUtc);
        Assert.Equal(_receiver.Id, output.ReceiverId);
    }

    private SolarCalculationOutput Calculate(
        SolarReceiver receiver,
        SunPosition sun,
        IReadOnlyList<BuildingGeometry> buildings)
    {
        var blockers = _shadowCalculator.GetBlockingBuildingIds(receiver, sun, buildings);
        var facing = receiver.SurfaceNormal is not { } normal || normal.Dot(sun.DirectionToSun) > 1e-12;
        return new SolarCalculationOutput(
            DateTimeOffset.UnixEpoch,
            sun,
            receiver.Id,
            sun.AltitudeDegrees > 0 && facing && blockers.Count == 0,
            blockers);
    }

    private static BuildingGeometry Building(int id, BuildingFootprint footprint, double height) =>
        new(new Guid(id, 0, 0, new byte[8]), footprint, 0, height);

    private static BuildingFootprint Rectangle(double minX, double minY, double maxX, double maxY) =>
        new([new(minX, minY), new(maxX, minY), new(maxX, maxY), new(minX, maxY)]);
}
