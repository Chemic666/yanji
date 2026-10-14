using YanJi.Core;
namespace YanJi.Tests;
public sealed class DisplayDurationTests
{
    [Theory]
    [InlineData(0,"0 秒")]
    [InlineData(1,"0 秒")]
    [InlineData(999,"0 秒")]
    [InlineData(27125,"27 秒")]
    [InlineData(60000,"1 分")]
    [InlineData(61000,"1 分 1 秒")]
    [InlineData(61001,"1 分 1 秒")]
    [InlineData(3600000,"1 小时")]
    [InlineData(3660000,"1 小时 1 分")]
    [InlineData(3660001,"1 小时 1 分")]
    [InlineData(3661001,"1 小时 1 分 1 秒")]
    [InlineData(86400000,"24 小时")]
    public void CompactDurationsAreNaturalAndDeclareOmittedPrecision(long ms,string expected)
        =>Assert.Equal(expected,DurationText.Compact(ms));

    [Fact] public void DisplayDoesNotChangeCanonicalAggregatesOrExactEvidence()
    {
        var day=new DateOnly(2026,10,13);
        var record=new StudyRecord("precision","math",null,SessionSource.Manual,"",[new StudyPart("p",day,3660001,"Asia/Shanghai",PartKind.DurationOnly)]);
        var before=StudyAnalytics.Build([record],[],day,day,day);
        _=DurationText.Compact(before.DurationMs);
        Assert.Equal(3660001,StudyAnalytics.Build([record],[],day,day,day).DurationMs);
        Assert.Equal("1 小时 1 分",DurationText.Format(record.DurationMs));
        Assert.Throws<ArgumentOutOfRangeException>(()=>DurationText.Compact(-1));
    }
    [Fact] public void FloorOnlyAfterAggregationAndRetainSubsecondFacts()
    {
        var day=new DateOnly(2026,10,14);
        StudyRecord R(string id,long ms)=>new(id,"math",null,SessionSource.Manual,"",[new(id,day,ms,"UTC",PartKind.DurationOnly)]);
        var snapshot=StudyAnalytics.Build([R("a",600),R("b",600)],[],day,day,day);
        Assert.Equal(1200,snapshot.DurationMs);Assert.Equal("1 秒",DurationText.Format(snapshot.DurationMs));
        var tiny=StudyAnalytics.Build([R("c",1)],[],day,day,day);
        Assert.Equal(1,tiny.LearningDays);Assert.Equal(1,tiny.Sessions);Assert.Equal(1,tiny.DurationMs);
    }
    [Theory][InlineData(0,"00:00")][InlineData(86399999,"23:59")][InlineData(86400000,"24:00")][InlineData(444599999,"123:29")]
    public void TimerHasTotalHoursAndMinutes(long ms,string expected)=>Assert.Equal(expected,DurationText.Timer(ms));
}
