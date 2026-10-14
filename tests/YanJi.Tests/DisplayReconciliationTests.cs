using YanJi.Core;
namespace YanJi.Tests;
public sealed class DisplayReconciliationTests
{
    private static AnalyticsSnapshot Snapshot(params (string Subject,long Ms)[] values)
    {
        var day=new DateOnly(2026,10,14);
        return StudyAnalytics.Build(values.Select((v,i)=>new StudyRecord("r"+i,v.Subject,null,SessionSource.Manual,"",[new("p"+i,day,v.Ms,"UTC",PartKind.DurationOnly)])),[],day,day,day);
    }
    [Fact] public void ReviewedFixtureKeepsExactFactsAndExplainsThreeSecondCarry()
    {
        var s=Snapshot(("english",37209872),("math",33911873),("politics",31808638),("408",31268638));
        Assert.Equal(134199021,s.DurationMs);Assert.Equal(s.DurationMs,s.Subjects.Sum(x=>x.DurationMs));
        Assert.Equal(134196,s.Subjects.Sum(x=>x.DurationMs/1000));Assert.Equal(3,s.SubjectDisplayCarrySeconds);
        Assert.Equal("37 小时 16 分 39 秒",DurationText.Format(s.DurationMs));
        Assert.Equal(33911873,s.Subjects.Single(x=>x.SubjectId=="math").DurationMs);
    }
    [Fact] public void GroupFirstWithoutInventingASecondForEitherTinySubject()
    {
        var separate=Snapshot(("math",600),("english",600));
        Assert.Equal(1,separate.SubjectDisplayCarrySeconds);Assert.All(separate.Subjects,x=>Assert.Equal("不足 1 秒",DurationText.Format(x.DurationMs)));
        var same=Snapshot(("math",600),("math",600));
        Assert.Equal(0,same.SubjectDisplayCarrySeconds);Assert.Equal("1 秒",DurationText.Format(same.Subjects.Single().DurationMs));
    }
    [Fact] public void ZeroAndExactSecondsHaveNoDisplayCarry()
    {
        Assert.Equal(0,Snapshot().SubjectDisplayCarrySeconds);
        Assert.Equal(0,Snapshot(("math",3600000),("english",61000)).SubjectDisplayCarrySeconds);
        Assert.Equal("0 秒",DurationText.Format(0));
    }
    [Theory][InlineData(0d,"0 秒")][InlineData(0.002739726d,"不足 1 秒")][InlineData(999.9d,"不足 1 秒")][InlineData(1000.1d,"1 秒")]
    public void DerivedAveragesDoNotRoundTinyPositiveValuesToZeroOrUpToASecond(double ms,string expected)
        =>Assert.Equal(expected,DurationText.Format(DurationText.ForDisplay(ms)));
}
