using CatchTheSun.Core.Sun.Interfaces;
using CatchTheSun.Core.Sun.Models;

namespace CatchTheSun.Core.Sun.Algorithms;

/// <summary>
/// Estimates direct-sun duration by midpoint sampling. Each UTC interval contributes its exact
/// elapsed duration when its midpoint is sunlit. Local DST gaps are absent from UTC time and a
/// repeated local hour is represented by two distinct UTC intervals.
/// </summary>
public sealed class SampledSolarInsolationCalculator(
    ISolarPositionProvider solarPositionProvider,
    IShadowCalculator shadowCalculator) : ISolarInsolationCalculator
{
    public SolarInsolationOutput Calculate(
        SolarInsolationInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var zone = TimeZoneInfo.FindSystemTimeZoneById(input.Location.TimeZoneId);
        var localStart = input.LocalDate.ToDateTime(input.StartLocalTime, DateTimeKind.Unspecified);
        var localEndDate = input.EndLocalTime < input.StartLocalTime
            ? input.LocalDate.AddDays(1)
            : input.LocalDate;
        var localEnd = localEndDate.ToDateTime(input.EndLocalTime, DateTimeKind.Unspecified);

        // For an ambiguous start, select the first occurrence; for an ambiguous end, select
        // the second. This includes the repeated local-time span exactly once per UTC instant.
        var periodStartUtc = ResolveBoundaryUtc(localStart, zone, isStart: true);
        var periodEndUtc = ResolveBoundaryUtc(localEnd, zone, isStart: false);
        if (periodEndUtc <= periodStartUtc)
            throw new ArgumentException("The resolved UTC period must have positive duration.", nameof(input));

        var totals = input.Receivers.ToDictionary(receiver => receiver.Id, _ => 0d);
        var step = TimeSpan.FromMinutes(input.TimeStepMinutes);
        var intervalStart = periodStartUtc;
        while (intervalStart < periodEndUtc)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var intervalEnd = intervalStart + step;
            if (intervalEnd > periodEndUtc)
                intervalEnd = periodEndUtc;

            var intervalDurationMinutes = (intervalEnd - intervalStart).TotalMinutes;
            var midpointUtc = intervalStart + TimeSpan.FromTicks((intervalEnd - intervalStart).Ticks / 2);
            var sun = solarPositionProvider.GetPosition(input.Location, midpointUtc);
            if (sun.AltitudeDegrees > 0)
            {
                foreach (var receiver in input.Receivers)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (receiver.SurfaceNormal is { } normal && normal.Dot(sun.DirectionToSun) <= 1e-12)
                        continue;

                    var blockers = shadowCalculator.GetBlockingBuildingIds(receiver, sun, input.Buildings);
                    if (blockers.Count == 0)
                        totals[receiver.Id] += intervalDurationMinutes;
                }
            }

            intervalStart = intervalEnd;
        }

        return new SolarInsolationOutput(
            input.Location.TimeZoneId,
            localStart,
            localEnd,
            periodStartUtc,
            periodEndUtc,
            input.TimeStepMinutes,
            input.Receivers
                .OrderBy(receiver => receiver.Id)
                .Select(receiver => new SolarInsolationResult(
                    receiver.Id,
                    totals[receiver.Id],
                    periodStartUtc,
                    periodEndUtc,
                    input.TimeStepMinutes))
                .ToArray());
    }

    private static DateTimeOffset ResolveBoundaryUtc(DateTime local, TimeZoneInfo zone, bool isStart)
    {
        if (zone.IsInvalidTime(local))
            throw new ArgumentException($"Period boundary {local:O} does not exist in timezone '{zone.Id}'.");

        TimeSpan offset;
        if (zone.IsAmbiguousTime(local))
        {
            var offsets = zone.GetAmbiguousTimeOffsets(local);
            // UTC = local - offset: larger offset gives earlier UTC.
            offset = isStart ? offsets.Max() : offsets.Min();
        }
        else
        {
            offset = zone.GetUtcOffset(local);
        }

        return new DateTimeOffset(local, offset).ToUniversalTime();
    }
}
