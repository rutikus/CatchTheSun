namespace CatchTheSun.Core.Sun.Models;

public sealed record SolarInsolationResult(
    Guid ReceiverId,
    double DurationMinutes,
    DateTimeOffset PeriodStartUtc,
    DateTimeOffset PeriodEndUtc,
    int TimeStepMinutes)
{
    public double DurationHours => DurationMinutes / 60d;
}
