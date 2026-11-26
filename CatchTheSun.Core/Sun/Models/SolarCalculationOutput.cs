namespace CatchTheSun.Core.Sun.Models;

public sealed record SolarCalculationOutput(
    DateTimeOffset InstantUtc,
    SunPosition SunPosition,
    Guid ReceiverId,
    bool IsSunlit,
    IReadOnlyList<Guid> BlockingBuildingIds);
