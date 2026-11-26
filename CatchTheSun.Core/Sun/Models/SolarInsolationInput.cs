namespace CatchTheSun.Core.Sun.Models;

/// <summary>
/// A local calendar window whose boundaries are resolved with the timezone already carried by
/// <see cref="SolarLocation"/>. EndLocalTime earlier than StartLocalTime means the next local day.
/// Equal times are rejected rather than being interpreted implicitly as 24 hours.
/// </summary>
public sealed class SolarInsolationInput
{
    public SolarLocation Location { get; }
    public IReadOnlyList<BuildingGeometry> Buildings { get; }
    public IReadOnlyList<SolarReceiver> Receivers { get; }
    public DateOnly LocalDate { get; }
    public TimeOnly StartLocalTime { get; }
    public TimeOnly EndLocalTime { get; }
    public int TimeStepMinutes { get; }

    public SolarInsolationInput(
        SolarLocation location,
        IEnumerable<BuildingGeometry> buildings,
        IEnumerable<SolarReceiver> receivers,
        DateOnly localDate,
        TimeOnly startLocalTime,
        TimeOnly endLocalTime,
        int timeStepMinutes)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(buildings);
        ArgumentNullException.ThrowIfNull(receivers);
        if (startLocalTime == endLocalTime)
            throw new ArgumentException("Start and end times must differ; use explicit different times for a 24-hour period.");
        if (timeStepMinutes is not (10 or 15 or 30 or 60))
            throw new ArgumentOutOfRangeException(nameof(timeStepMinutes), "Supported steps are 10, 15, 30, and 60 minutes.");

        var buildingList = buildings.ToArray();
        if (buildingList.Any(x => x is null))
            throw new ArgumentException("Buildings must not contain null items.", nameof(buildings));
        if (buildingList.Select(x => x.Id).Distinct().Count() != buildingList.Length)
            throw new ArgumentException("Building ids must be unique.", nameof(buildings));

        var receiverList = receivers.ToArray();
        if (receiverList.Length == 0)
            throw new ArgumentException("At least one receiver is required.", nameof(receivers));
        if (receiverList.Any(x => x is null))
            throw new ArgumentException("Receivers must not contain null items.", nameof(receivers));
        if (receiverList.Select(x => x.Id).Distinct().Count() != receiverList.Length)
            throw new ArgumentException("Receiver ids must be unique.", nameof(receivers));

        Location = location;
        Buildings = Array.AsReadOnly(buildingList);
        Receivers = Array.AsReadOnly(receiverList);
        LocalDate = localDate;
        StartLocalTime = startLocalTime;
        EndLocalTime = endLocalTime;
        TimeStepMinutes = timeStepMinutes;
    }
}
