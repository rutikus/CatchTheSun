namespace CatchTheSun.Core.Sun.Models;

/// <summary>One instantaneous point-in-time sunlight query. DateTimeOffset preserves the instant; the provider normalizes it to UTC.</summary>
public sealed class SolarCalculationInput
{
    public SolarLocation Location { get; }
    public DateTimeOffset Instant { get; }
    public SolarReceiver Receiver { get; }
    public IReadOnlyList<BuildingGeometry> Buildings { get; }

    public SolarCalculationInput(
        SolarLocation location,
        DateTimeOffset instant,
        SolarReceiver receiver,
        IEnumerable<BuildingGeometry> buildings)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(receiver);
        ArgumentNullException.ThrowIfNull(buildings);
        var buildingList = buildings.ToArray();
        if (buildingList.Any(x => x is null))
            throw new ArgumentException("Buildings must not contain null items.", nameof(buildings));
        if (buildingList.Select(x => x.Id).Distinct().Count() != buildingList.Length)
            throw new ArgumentException("Building ids must be unique.", nameof(buildings));

        Location = location;
        Instant = instant.ToUniversalTime();
        Receiver = receiver;
        Buildings = Array.AsReadOnly(buildingList);
    }
}
