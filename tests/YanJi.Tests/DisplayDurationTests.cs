using YanJi.Core;
namespace YanJi.Tests;
public sealed class DisplayDurationTests
{
    [Theory]
    [InlineData(0,"0 秒")]
    [InlineData(1,"0.001 秒")]
    [InlineData(27125,"27.125 秒")]
    [InlineData(60000,"1 分")]
    [InlineData(61000,"1 分 1 秒")]
    [InlineData(61001,"≈ 1 分 1 秒")]
    [InlineData(3600000,"1 小时")]
    [InlineData(3660000,"1 小时 1 分")]
    [InlineData(3660001,"≈ 1 小时 1 分")]
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
        Assert.Equal("1 小时 1 分 0.001 秒",DurationText.Format(record.DurationMs));
        Assert.Throws<ArgumentOutOfRangeException>(()=>DurationText.Compact(-1));
    }
}
