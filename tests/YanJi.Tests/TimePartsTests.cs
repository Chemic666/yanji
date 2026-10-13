using YanJi.Core;

namespace YanJi.Tests;

public class TimePartsTests
{
    [Fact]
    public void DurationOnlyHasNoInventedInterval()
    {
        var part = TimeParts.Duration(new DateOnly(2026, 10, 13), TimeSpan.FromMinutes(45), "Asia/Shanghai");
        Assert.Equal(2_700_000, part.DurationMs);
        Assert.Null(part.StartUtcMs);
        Assert.Null(part.EndUtcMs);
        Assert.Equal(PartKind.DurationOnly, part.Kind);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RejectsNonpositiveDuration(int minutes) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => TimeParts.Duration(
            new DateOnly(2026, 10, 13), TimeSpan.FromMinutes(minutes), "Asia/Shanghai"));

    [Fact]
    public void SplitsShanghaiMidnightWithoutDoubleCounting()
    {
        var parts = TimeParts.Interval(DateTimeOffset.Parse("2026-10-13T15:50:00Z"),
            DateTimeOffset.Parse("2026-10-13T16:20:00Z"), "Asia/Shanghai");
        Assert.Equal(2, parts.Count);
        Assert.Equal(new DateOnly(2026, 10, 13), parts[0].Date);
        Assert.Equal(600_000, parts[0].DurationMs);
        Assert.Equal(new DateOnly(2026, 10, 14), parts[1].Date);
        Assert.Equal(1_200_000, parts[1].DurationMs);
        Assert.Equal(1_800_000, parts.Sum(x => x.DurationMs));
    }

    [Fact]
    public void TouchingIntervalsDoNotOverlapButIntersectingIntervalsDo()
    {
        var date = new DateOnly(2026, 10, 13);
        var a = new StudyPart("a", date, 1000, "UTC", PartKind.Interval, 1000, 2000);
        var b = new StudyPart("b", date, 1000, "UTC", PartKind.Interval, 2000, 3000);
        var c = new StudyPart("c", date, 1000, "UTC", PartKind.Interval, 1500, 2500);
        Assert.False(TimeParts.Overlaps(a, b));
        Assert.True(TimeParts.Overlaps(a, c));
        Assert.False(TimeParts.Overlaps(a, TimeParts.Duration(date, TimeSpan.FromMinutes(5), "UTC")));
    }

    [Fact]
    public void RejectsReversedInterval() => Assert.Throws<ArgumentOutOfRangeException>(() =>
        TimeParts.Interval(DateTimeOffset.Parse("2026-10-13T01:00:00Z"),
            DateTimeOffset.Parse("2026-10-13T00:00:00Z"), "UTC"));
}
