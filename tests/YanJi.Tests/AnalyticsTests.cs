using YanJi.Core;
namespace YanJi.Tests;
public sealed class AnalyticsTests
{
    private static readonly DateOnly Day=new(2026,10,13);
    private static StudyRecord R(string id,string subject,DateOnly date,long ms,bool deleted=false)=>new(id,subject,"chapter",SessionSource.Manual,"",[new(id+"p",date,ms,"Asia/Shanghai",PartKind.DurationOnly)],Deleted:deleted);
    [Fact] public void ProjectionPreservesMillisecondsZerosSubjectFilterAndDeletedExclusion()
    {
        var r=new[]{R("a","math",Day,1555),R("b","english",Day,2445),R("c","math",Day.AddDays(2),60001),R("d","math",Day,999999,true)};
        var a=StudyAnalytics.Build(r,[new("math","数学"),new("english","英语")],Day,Day.AddDays(2),Day.AddDays(2));
        Assert.Equal(64001,a.DurationMs);Assert.Equal(3,a.CalendarDays);Assert.Equal(2,a.LearningDays);Assert.Equal(3,a.Sessions);Assert.Equal(0,a.Daily[1].DurationMs);
        Assert.Equal(61556,a.Subjects.Single(x=>x.SubjectId=="math").DurationMs);Assert.Equal(a.DurationMs,a.Weeks.Sum(x=>x.DurationMs));Assert.Equal(a.DurationMs,a.Months.Sum(x=>x.DurationMs));
        Assert.Equal(64001,a.Chapters["chapter"]);Assert.Equal(61556,StudyAnalytics.Build(r,[],Day,Day.AddDays(2),Day.AddDays(2),"math").DurationMs);
    }
    [Fact] public void FutureDaysDoNotInflateCalendarDenominatorAndEmptyRangeIsSafe()
    {
        var a=StudyAnalytics.Build([R("a","math",Day,120000),R("future","math",Day.AddDays(2),240000)],[],Day.AddDays(-1),Day.AddDays(6),Day);
        Assert.Equal(2,a.CalendarDays);Assert.Equal(60000,a.CalendarAverageMs);Assert.Equal(180000,a.LearningAverageMs);Assert.Equal(360000,a.DurationMs);
        var empty=StudyAnalytics.Build([],[],Day.AddDays(1),Day.AddDays(2),Day);Assert.Equal(0,empty.CalendarDays);Assert.Equal(0,empty.CalendarAverageMs);Assert.Empty(empty.Subjects);
        Assert.Throws<ArgumentException>(()=>StudyAnalytics.Build([],[],Day.AddDays(1),Day,Day));
    }
    [Theory][InlineData(1,"0 秒")][InlineData(1555,"1 秒")][InlineData(59000,"59 秒")][InlineData(60000,"1 分")][InlineData(61001,"1 分 1 秒")][InlineData(3600000,"1 小时")]
    public void NaturalDurationsDoNotTurnSecondsIntoZeroMinutes(long ms,string expected)=>Assert.Equal(expected,DurationText.Format(ms));
    [Fact] public void LargeValueAndSinglePointRetainActualScale()
    {var a=StudyAnalytics.Build([R("a","math",Day,1_000_000_000_000)],[],Day,Day,Day);Assert.Equal(1_000_000_000_000,a.DurationMs);Assert.Single(a.Daily);Assert.Equal(1_000_000_000_000,a.Subjects.Single().DurationMs);}
}
