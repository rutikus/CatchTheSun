namespace CatchTheSun.Core.Sun.Models;

/// <summary>Explicit conversion of a local wall time to an unambiguous UTC instant.</summary>
public static class SolarTime
{
    public static DateTimeOffset ResolveLocalTime(
        DateTime localTime,
        SolarLocation location,
        TimeSpan? ambiguousTimeOffset = null)
    {
        ArgumentNullException.ThrowIfNull(location);
        if (localTime.Kind != DateTimeKind.Unspecified)
            throw new ArgumentException("Local wall time must have DateTimeKind.Unspecified.", nameof(localTime));

        var zone = TimeZoneInfo.FindSystemTimeZoneById(location.TimeZoneId);
        if (zone.IsInvalidTime(localTime))
            throw new ArgumentException("Local time does not exist because of a timezone transition.", nameof(localTime));

        TimeSpan offset;
        if (zone.IsAmbiguousTime(localTime))
        {
            var validOffsets = zone.GetAmbiguousTimeOffsets(localTime);
            if (ambiguousTimeOffset is null || !validOffsets.Contains(ambiguousTimeOffset.Value))
                throw new ArgumentException("Ambiguous local time requires one of the timezone's valid UTC offsets.", nameof(ambiguousTimeOffset));
            offset = ambiguousTimeOffset.Value;
        }
        else
        {
            if (ambiguousTimeOffset is not null)
                throw new ArgumentException("An ambiguous-time offset may only be supplied for an ambiguous local time.", nameof(ambiguousTimeOffset));
            offset = zone.GetUtcOffset(localTime);
        }

        return new DateTimeOffset(localTime, offset).ToUniversalTime();
    }
}
