namespace CatchTheSun.Core.Sun.Models;

/// <summary>Totals for all receivers over one exact local window and its resolved UTC interval.</summary>
public sealed record SolarInsolationOutput(
    string TimeZoneId,
    DateTime LocalStart,
    DateTime LocalEnd,
    DateTimeOffset PeriodStartUtc,
    DateTimeOffset PeriodEndUtc,
    int TimeStepMinutes,
    IReadOnlyList<SolarInsolationResult> Receivers);
