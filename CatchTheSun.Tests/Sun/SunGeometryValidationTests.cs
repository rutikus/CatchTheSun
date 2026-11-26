using CatchTheSun.Core.Sun.Models;
using Xunit;

namespace CatchTheSun.Tests.Sun;

public sealed class SunGeometryValidationTests
{
    [Fact]
    public void RejectsInvalidLocationAndNonFiniteGeometry()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SolarLocation(91, 0, "UTC"));
        Assert.Throws<ArgumentException>(() => new SolarLocation(0, 0, "Not/A_Timezone"));
        Assert.Throws<ArgumentException>(() => new BuildingFootprint([new(0, 0), new(double.NaN, 0), new(0, 1)]));
    }

    [Fact]
    public void RejectsDegenerateAndSelfIntersectingFootprints()
    {
        Assert.Throws<ArgumentException>(() => new BuildingFootprint([new(0, 0), new(1, 1), new(2, 2)]));
        Assert.Throws<ArgumentException>(() => new BuildingFootprint([
            new(0, 0), new(2, 2), new(0, 2), new(2, 0)]));
    }

    [Fact]
    public void AcceptsAndNormalizesRepeatedClosingVertex()
    {
        var footprint = new BuildingFootprint([
            new(0, 0), new(2, 0), new(2, 2), new(0, 2), new(0, 0)]);

        Assert.Equal(4, footprint.Vertices.Count);
    }

    [Fact]
    public void RejectsInvalidBuildingAndReceiverValues()
    {
        var footprint = new BuildingFootprint([new(0, 0), new(2, 0), new(2, 2), new(0, 2)]);

        Assert.Throws<ArgumentOutOfRangeException>(() => new BuildingGeometry(Guid.NewGuid(), footprint, 0, 0));
        Assert.Throws<ArgumentException>(() => new SolarReceiver(Guid.NewGuid(), new Point3D(0, 0, 0), new Vector3D(0, 0, 0)));
    }

    [Fact]
    public void SolarCalculationInputCopiesCollectionsAndRejectsDuplicateIds()
    {
        var receiver = new SolarReceiver(Guid.NewGuid(), new Point3D(0, 0, 0));
        var footprint = new BuildingFootprint([new(0, 0), new(1, 0), new(1, 1)]);
        var buildingId = Guid.NewGuid();
        var buildings = new List<BuildingGeometry> { new(buildingId, footprint, 0, 2) };
        var input = new SolarCalculationInput(
            new SolarLocation(0, 0, "UTC"), DateTimeOffset.Now, receiver, buildings);
        buildings.Clear();

        Assert.Single(input.Buildings);
        Assert.Equal(TimeSpan.Zero, input.Instant.Offset);
        Assert.Throws<ArgumentException>(() => new SolarCalculationInput(
            new SolarLocation(0, 0, "UTC"), DateTimeOffset.UtcNow, receiver,
            [new(buildingId, footprint, 0, 2), new(buildingId, footprint, 0, 3)]));
    }
}
