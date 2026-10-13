namespace YanJi.Core;

public static class TimeParts
{
    public static TimeZoneInfo ResolveZone(string zoneId)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(zoneId); }
        catch (TimeZoneNotFoundException) when (TimeZoneInfo.TryConvertIanaIdToWindowsId(zoneId, out _))
        {
            TimeZoneInfo.TryConvertIanaIdToWindowsId(zoneId, out var windowsId);
            return TimeZoneInfo.FindSystemTimeZoneById(windowsId!);
        }
    }
    public static StudyPart Duration(DateOnly date, TimeSpan duration, string zoneId) =>
        duration.TotalMilliseconds < 1 ? throw new ArgumentOutOfRangeException(nameof(duration)) :
        new(Guid.NewGuid().ToString("N"), date, checked((long)duration.TotalMilliseconds),
            ResolveZone(zoneId) is not null ? zoneId : throw new ArgumentException(nameof(zoneId)), PartKind.DurationOnly);
    public static List<StudyPart> Interval(DateTimeOffset start, DateTimeOffset end, string zoneId)
    {
        var zone = ResolveZone(zoneId);
        var cursor = start.ToUnixTimeMilliseconds();
        var finish = end.ToUnixTimeMilliseconds();
        if (finish <= cursor) throw new ArgumentOutOfRangeException(nameof(end));
        List<StudyPart> result = [];
        while (cursor < finish)
        {
            var date = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeMilliseconds(cursor), zone).DateTime);
            var midnight = date.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
            // Some zones skip midnight. Move to the first valid instant of the next local day.
            while (zone.IsInvalidTime(midnight)) midnight = midnight.AddMinutes(1);
            var boundary = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(midnight, zone)).ToUnixTimeMilliseconds();
            if (zone.IsAmbiguousTime(midnight))
                boundary = zone.GetAmbiguousTimeOffsets(midnight).Select(offset => new DateTimeOffset(midnight, offset).ToUnixTimeMilliseconds())
                    .Where(ms => ms > cursor).Min();
            var stop = Math.Min(finish, boundary);
            if (stop <= cursor) throw new InvalidOperationException("无法确定日期边界。");
            result.Add(new(Guid.NewGuid().ToString("N"), date, stop - cursor, zoneId, PartKind.Interval, cursor, stop));
            cursor = stop;
        }
        return result;
    }
    public static bool Overlaps(StudyPart first, StudyPart second) =>
        first.Kind == PartKind.Interval && second.Kind == PartKind.Interval &&
        first.StartUtcMs < second.EndUtcMs && second.StartUtcMs < first.EndUtcMs;
}
