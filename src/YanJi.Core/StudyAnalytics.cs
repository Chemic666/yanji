namespace YanJi.Core;

public sealed record DailyBreakdown(DateOnly Date,Dictionary<string,long> Subjects)
{public long DurationMs=>Subjects.Values.Sum();}
public sealed record StudyBucket(DateOnly From,DateOnly To,long DurationMs);
public sealed record AnalyticsSnapshot(long DurationMs,int CalendarDays,int LearningDays,int Sessions,
    List<DailyBreakdown> Daily,List<SubjectTotal> Subjects,List<StudyBucket> Weeks,List<StudyBucket> Months,
    Dictionary<string,long> Chapters,long ElapsedDurationMs)
{
    public double CalendarAverageMs=>CalendarDays==0?0:(double)ElapsedDurationMs/CalendarDays;
    public double LearningAverageMs=>LearningDays==0?0:(double)DurationMs/LearningDays;
}
public static class StudyAnalytics
{
    public static AnalyticsSnapshot Build(IEnumerable<StudyRecord> records,IEnumerable<Subject> subjects,
        DateOnly from,DateOnly to,DateOnly today,string? subjectId=null)
    {
        if(to<from||to.DayNumber-from.DayNumber>3660)throw new ArgumentException("请选择顺序正确、最多十年的日期范围。");
        var source=records.Where(r=>!r.Deleted&&(subjectId==null||r.SubjectId==subjectId)).ToList();
        var parts=source.SelectMany(r=>r.Parts.Where(p=>p.Date>=from&&p.Date<=to).Select(p=>(Record:r,Part:p))).ToList();
        var dailyMap=parts.GroupBy(x=>x.Part.Date).ToDictionary(g=>g.Key,g=>g.GroupBy(x=>x.Record.SubjectId).ToDictionary(s=>s.Key,s=>s.Sum(x=>x.Part.DurationMs)));
        var daily=Enumerable.Range(0,to.DayNumber-from.DayNumber+1).Select(i=>new DailyBreakdown(from.AddDays(i),dailyMap.GetValueOrDefault(from.AddDays(i))??[])).ToList();
        var names=subjects.ToDictionary(s=>s.Id,s=>s.Name);
        var bySubject=parts.GroupBy(x=>x.Record.SubjectId).Select(g=>new SubjectTotal(g.Key,names.GetValueOrDefault(g.Key)??g.Key,g.Sum(x=>x.Part.DurationMs))).OrderByDescending(s=>s.DurationMs).ToList();
        List<StudyBucket> Buckets(Func<DateOnly,DateOnly> start)=>daily.GroupBy(d=>start(d.Date)).Select(g=>new StudyBucket(g.Min(x=>x.Date),g.Max(x=>x.Date),g.Sum(x=>x.DurationMs))).ToList();
        var elapsedEnd=to<today?to:today;
        return new(parts.Sum(x=>x.Part.DurationMs),Math.Max(0,elapsedEnd.DayNumber-from.DayNumber+1),daily.Count(x=>x.DurationMs>0),parts.Select(x=>x.Record.Id).Distinct().Count(),daily,bySubject,
            Buckets(d=>d.AddDays(-(((int)d.DayOfWeek+6)%7))),Buckets(d=>new(d.Year,d.Month,1)),
            parts.Where(x=>x.Record.ChapterId!=null).GroupBy(x=>x.Record.ChapterId!).ToDictionary(g=>g.Key,g=>g.Sum(x=>x.Part.DurationMs)),daily.Where(d=>d.Date<=today).Sum(d=>d.DurationMs));
    }
}
public static class DurationText
{
    public static string Format(long milliseconds)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(milliseconds);
        var seconds=(milliseconds%60000)/1000m;
        var secondText=seconds.ToString("0.###",System.Globalization.CultureInfo.InvariantCulture);
        if(milliseconds<60000)return secondText+" 秒";
        var hours=milliseconds/3600000;var minutes=milliseconds/60000%60;
        return (hours>0?hours+" 小时":"")+(minutes>0?(hours>0?" ":"")+minutes+" 分":"")+(seconds>0?" "+secondText+" 秒":"");
    }
    public static string Exact(long milliseconds)=>milliseconds.ToString("N0",System.Globalization.CultureInfo.GetCultureInfo("zh-CN"))+" 毫秒";
}
