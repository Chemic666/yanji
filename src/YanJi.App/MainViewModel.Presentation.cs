using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using YanJi.Core;

namespace YanJi.App;
public sealed record ChapterRow(Chapter Chapter,string MasteryLabel,string ArchiveLabel)
{public string Name=>Chapter.Name;public int Progress=>Chapter.Progress;}
public sealed record SubjectGroup(Subject Subject,List<ChapterRow> Children)
{public string Name=>Subject.Name;public string Detail=>$"{Children.Count} 个章节"+(Subject.Archived?" · 已归档":"");public string Color=>ThemeService.SubjectColor(Subject.Id);}
public sealed record WeakRow(WeakPoint Point,string Subject,string Chapter,string State,string Review,string History)
{public string Title=>Point.Title;public string Detail=>Point.Detail;public string Category=>Point.Category.Length==0?"未分类":Point.Category;public string Color=>ThemeService.SubjectColor(Point.SubjectId??"none");}
public sealed record PartRow(StudyPart Part,string Time,string Duration,bool Excluded)
{public string Date=>Part.Date.ToString("yyyy-MM-dd");public string State=>Excluded?"已排除，不计统计":Part.Kind==PartKind.Interval?"真实区间":"纯时长，无起止时间";public string Exact=>DurationText.Exact(Part.DurationMs);}
public sealed partial class MainViewModel
{
    private List<StudyRecord> _records=[];
    private Dictionary<string,string> _settings=[];
    [ObservableProperty] private bool darkTheme;
    [ObservableProperty] private long todayDurationMs;
    [ObservableProperty] private long weekDurationMs;
    [ObservableProperty] private long monthDurationMs;
    [ObservableProperty] private string recordSubjectId="";
    [ObservableProperty] private string recordChapterId="";
    [ObservableProperty] private string recordSource="";
    [ObservableProperty] private string recordState="active";
    [ObservableProperty] private DateTime recordFrom=DateTime.Today.AddDays(-29);
    [ObservableProperty] private DateTime recordTo=DateTime.Today;
    [ObservableProperty] private string recordSummary="";
    [ObservableProperty] private string statsSubjectId="";
    [ObservableProperty] private string trendPeriod="week";
    [ObservableProperty] private string weakSearch="";
    [ObservableProperty] private string weakSubjectId="";
    [ObservableProperty] private string weakState="";
    [ObservableProperty] private string naturalAverage="0 秒";
    [ObservableProperty] private string learningAverage="0 秒";
    [ObservableProperty] private string statsDays="0";
    [ObservableProperty] private string statsSessions="0";
    [ObservableProperty] private string subjectDisplayNote="";
    [ObservableProperty] private string dayTotal="0 秒";
    [ObservableProperty] private string dayState="尚无记录";
    [ObservableProperty] private bool restDay;
    [ObservableProperty] private string taskSummary="";
    [ObservableProperty] private string checkpointLabel="约 15 秒";
    [ObservableProperty] private RecordRow? selectedRecord;
    [ObservableProperty] private bool drawerOpen;
    [ObservableProperty] private string revisionText="";
    [ObservableProperty] private AnalyticsSnapshot? analysis;
    [ObservableProperty] private IReadOnlyList<PlotPoint> subjectPlot=Array.Empty<PlotPoint>();
    [ObservableProperty] private IReadOnlyList<PlotPoint> dailyPlot=Array.Empty<PlotPoint>();
    [ObservableProperty] private IReadOnlyList<PlotPoint> trendPlot=Array.Empty<PlotPoint>();
    [ObservableProperty] private IReadOnlyList<PlotPoint> heatmapPlot=Array.Empty<PlotPoint>();
    [ObservableProperty] private IReadOnlyList<PlotPoint> statsHeatmap=Array.Empty<PlotPoint>();
    [ObservableProperty] private IReadOnlyList<PlotPoint> timelinePlot=Array.Empty<PlotPoint>();
    [ObservableProperty] private IReadOnlyList<PlotPoint> miniTrend=Array.Empty<PlotPoint>();
    [ObservableProperty] private IReadOnlyList<PlotPoint> weakDistribution=Array.Empty<PlotPoint>();
    [ObservableProperty] private IReadOnlyList<PlotPoint> chapterPlot=Array.Empty<PlotPoint>();
    public ObservableCollection<RecordRow> FilteredRecords {get;}=[];
    public ObservableCollection<RecordRow> DurationRecords {get;}=[];
    public ObservableCollection<SubjectGroup> SubjectGroups {get;}=[];
    public ObservableCollection<WeakRow> FilteredWeakPoints {get;}=[];
    public ObservableCollection<PartRow> DetailParts {get;}=[];
    public ObservableCollection<PartRow> ExcludedDetailParts {get;}=[];
    public ObservableCollection<Choice> SubjectFilters {get;}=[];
    public ObservableCollection<Choice> ChapterFilters {get;}=[];
    public List<Choice> SourceFilters {get;}=[new("","全部来源"),new("manual","手动补录"),new("timer","实时计时")];
    public List<Choice> RecordStates {get;}=[new("active","有效记录"),new("deleted","已删除"),new("all","全部状态")];
    public List<Choice> WeakStates {get;}=[new("","全部状态"),new("0","待复习"),new("1","巩固中"),new("2","已验收")];
    private bool _themeLoading;
    private void RefreshPresentation()
    {
        _records=Database.Records(null,true);_settings=Database.Settings();
        var subject=RecordSubjectId;var stats=StatsSubjectId;var weak=WeakSubjectId;
        Replace(SubjectFilters,new[]{new Choice("","全部科目")}.Concat(AllSubjects.Select(s=>new Choice(s.Id,s.Name+(s.Archived?" · 归档":"")))));
        RecordSubjectId=subject;StatsSubjectId=stats;WeakSubjectId=weak;
        RefreshChapterFilters();
        _themeLoading=true;DarkTheme=_settings.GetValueOrDefault("theme")=="dark";_themeLoading=false;ThemeService.Apply(DarkTheme);
        CheckpointLabel="约 "+_settings.GetValueOrDefault("checkpoint-seconds","15")+" 秒 · 不是实时保证";
        Replace(SubjectGroups,AllSubjects.Select(s=>new SubjectGroup(s,AllChapters.Where(c=>c.SubjectId==s.Id).Select(c=>new ChapterRow(c,MasteryName(c.Mastery),c.Archived?"已归档":"使用中")).ToList())));
        TaskSummary=$"{Tasks.Count(t=>t.Done)} / {Tasks.Count} 项完成；计划不代表已学习";
        MiniTrend=DailyPoints(StudyAnalytics.Build(_records,AllSubjects,LocalToday().AddDays(-6),LocalToday(),LocalToday()).Daily);
        HeatmapPlot=Heatmap(LocalToday().AddDays(-364),LocalToday(),null);
        RefreshRecordFilters();RefreshWeakFilters();BuildAnalytics();
        if(DrawerOpen&&SelectedRecord is { } old){var current=_records.FirstOrDefault(r=>r.Id==old.Record.Id);if(current!=null){var row=Row(current);if(old.ScopeFrom is DateOnly a&&old.ScopeTo is DateOnly b)row=row with{ScopeFrom=a,ScopeTo=b,ScopeKind=old.ScopeKind,Contribution=current.Parts.Where(p=>p.Date>=a&&p.Date<=b&&(old.ScopeKind==null||p.Kind==old.ScopeKind)).Sum(p=>p.DurationMs)};OpenDetail(row);}}
    }
    partial void OnDarkThemeChanged(bool value)
    {ThemeService.Apply(value);if(!_themeLoading)_=RunAsync(()=>Database.SaveSetting("theme",value?"dark":"light"),"主题已保存");}
    private void RefreshChapterFilters()
    {var selected=RecordChapterId;var chapters=AllChapters.Where(c=>string.IsNullOrEmpty(RecordSubjectId)||c.SubjectId==RecordSubjectId);Replace(ChapterFilters,new[]{new Choice("","全部章节"),new Choice("__unspecified","未指定章节")}.Concat(chapters.Select(c=>new Choice(c.Id,(AllSubjects.FirstOrDefault(s=>s.Id==c.SubjectId)?.Name??c.SubjectId)+" · "+c.Name+(c.Archived?" · 归档":"")))));RecordChapterId=ChapterFilters.Any(c=>c.Value==selected)?selected:"";}
    partial void OnRecordSubjectIdChanged(string value){RefreshChapterFilters();RefreshRecordFilters();}
    partial void OnRecordChapterIdChanged(string value)=>RefreshRecordFilters();
    partial void OnRecordSourceChanged(string value)=>RefreshRecordFilters();
    partial void OnRecordStateChanged(string value)=>RefreshRecordFilters();
    partial void OnRecordFromChanged(DateTime value)=>RefreshRecordFilters();
    partial void OnRecordToChanged(DateTime value)=>RefreshRecordFilters();
    partial void OnStatsSubjectIdChanged(string value)=>BuildAnalytics();
    partial void OnStatsFromChanged(DateTime value)=>BuildAnalytics();
    partial void OnStatsToChanged(DateTime value)=>BuildAnalytics();
    partial void OnTrendPeriodChanged(string value)=>BuildAnalytics();
    partial void OnWeakSearchChanged(string value)=>RefreshWeakFilters();
    partial void OnWeakSubjectIdChanged(string value)=>RefreshWeakFilters();
    partial void OnWeakStateChanged(string value)=>RefreshWeakFilters();
    private void RefreshRecordFilters()
    {
        var from=DateOnly.FromDateTime(RecordFrom);var to=DateOnly.FromDateTime(RecordTo);
        if(to<from){Replace(FilteredRecords,Array.Empty<RecordRow>());RecordSummary="结束日期早于开始日期";return;}
        var rows=_records.Where(r=>(string.IsNullOrEmpty(RecordSubjectId)||r.SubjectId==RecordSubjectId)&&(string.IsNullOrEmpty(RecordChapterId)||(RecordChapterId=="__unspecified"?r.ChapterId==null:r.ChapterId==RecordChapterId))&&(string.IsNullOrEmpty(RecordSource)||(RecordSource=="timer")== (r.Source==SessionSource.Timer))
            &&(RecordState=="all"||(RecordState=="deleted")==r.Deleted)&&r.Parts.Any(p=>p.Date>=from&&p.Date<=to))
            .OrderByDescending(r=>r.Parts.Max(p=>p.Date)).ThenByDescending(r=>r.CreatedAt).Select(r=>Row(r) with{ScopeFrom=from,ScopeTo=to,Contribution=r.Parts.Where(p=>p.Date>=from&&p.Date<=to).Sum(p=>p.DurationMs)}).ToList();
        Replace(FilteredRecords,rows);RecordSummary=$"{rows.Count} 次学习 · 范围内有效贡献 {Duration(rows.Where(r=>!r.Deleted).Sum(r=>r.Contribution??0))} · 每行保留整次身份";
    }
    private static string MasteryName(Mastery state)=>state switch{Mastery.Unknown=>"掌握：未评估",Mastery.Weak=>"掌握：薄弱",Mastery.Reviewing=>"掌握：巩固中",_=>"掌握：已掌握"};
    private static string WeakName(WeakPointState state)=>state switch{WeakPointState.Open=>"待复习",WeakPointState.Reviewing=>"巩固中",_=>"已验收"};
    private void RefreshWeakFilters()
    {
        var rows=WeakPoints.Where(w=>(string.IsNullOrEmpty(WeakSubjectId)||w.SubjectId==WeakSubjectId)&&(string.IsNullOrEmpty(WeakState)||((int)w.State).ToString()==WeakState)
            &&(w.Title+" "+w.Detail+" "+w.Category).Contains(WeakSearch??"",StringComparison.CurrentCultureIgnoreCase)).Select(w=>new WeakRow(w,
                AllSubjects.FirstOrDefault(s=>s.Id==w.SubjectId)?.Name??"未指定科目",AllChapters.FirstOrDefault(c=>c.Id==w.ChapterId)?.Name??"未指定章节",WeakName(w.State),w.ReviewDate is DateOnly d?$"下次复习 {d:yyyy-MM-dd}"+(d<LocalToday()&&w.State!=WeakPointState.Verified?" · 已到期":""):"未安排下次复习",WeakHistory(w.Id))).ToList();
        Replace(FilteredWeakPoints,rows);
        WeakDistribution=Enum.GetValues<WeakPointState>().Select(s=>new PlotPoint(s.ToString(),WeakName(s),rows.Count(w=>w.Point.State==s),ThemeService.SubjectColor(s.ToString()))).ToList();
    }
    private string WeakHistory(string id)=>string.Join("\n",Database.History(id).Where(r=>r.EntityKind=="weakpoints").Take(10).Select(r=>
    {var after=JsonSerializer.Deserialize<WeakPoint>(r.AfterJson??"null");return $"{r.At.ToLocalTime():yyyy-MM-dd HH:mm} · {(after==null?r.Reason:WeakName(after.State))}";}));
    private void BuildAnalytics()
    {
        try
        {
            var from=DateOnly.FromDateTime(StatsFrom);var to=DateOnly.FromDateTime(StatsTo);
            Analysis=StudyAnalytics.Build(_records,AllSubjects,from,to,LocalToday(),string.IsNullOrEmpty(StatsSubjectId)?null:StatsSubjectId);
            StatsTotal=Duration(Analysis.DurationMs);NaturalAverage=Duration(DurationText.ForDisplay(Analysis.CalendarAverageMs));LearningAverage=Duration(DurationText.ForDisplay(Analysis.LearningAverageMs));StatsDays=Analysis.LearningDays.ToString();StatsSessions=Analysis.Sessions.ToString();
            SubjectDisplayNote=Analysis.SubjectDisplayCarrySeconds>0?$"各科目分别向下显示到秒；不足一秒的余量合并贡献 {Analysis.SubjectDisplayCarrySeconds} 秒，已包含在范围总计中。":"总计按原始时长汇总；科目分别显示到秒，不重新分配余量。";
            SubjectPlot=Analysis.Subjects.Select(s=>new PlotPoint(s.SubjectId,s.Name,s.DurationMs,ThemeService.SubjectColor(s.SubjectId),s.SubjectId,from,to,Status:"范围内已保存有效学习")).ToList();
            var scope=string.IsNullOrEmpty(StatsSubjectId)?null:StatsSubjectId;
            if(Analysis.Daily.Count<=62){DailyPlot=DailyPoints(Analysis.Daily,scope);ChartGranularity="每日科目投入";}
            else
            {
                var monthly=Analysis.Daily.Count>366;ChartGranularity=monthly?"每月科目投入 · 首尾仅含所选范围":"每周科目投入 · 首尾仅含所选范围";
                DailyPlot=Analysis.Daily.GroupBy(d=>monthly?new DateOnly(d.Date.Year,d.Date.Month,1):d.Date.AddDays(-(((int)d.Date.DayOfWeek+6)%7))).Select(g=>
                {var a=g.Min(d=>d.Date);var b=g.Max(d=>d.Date);var subjects=g.SelectMany(d=>d.Subjects).GroupBy(s=>s.Key).Select(s=>new PlotPoint(s.Key,(AllSubjects.FirstOrDefault(x=>x.Id==s.Key)?.Name??s.Key)+$" · {a:yyyy-MM-dd} — {b:yyyy-MM-dd}",s.Sum(x=>x.Value),ThemeService.SubjectColor(s.Key),s.Key,a,b)).ToList();return new PlotPoint(a.ToString("yyyy-MM-dd"),a.ToString(monthly?"yyyy-MM":"MM-dd"),subjects.Sum(s=>s.Value),ThemeService.SubjectColor("math"),scope,a,b,subjects,Status=$"{a:yyyy-MM-dd} — {b:yyyy-MM-dd}");}).ToList();
            }
            var buckets=TrendPeriod=="month"?Analysis.Months:Analysis.Weeks;
            TrendPlot=buckets.Select(b=>new PlotPoint(b.From.ToString("yyyy-MM-dd"),b.From.ToString("MM-dd"),b.DurationMs,ThemeService.SubjectColor(string.IsNullOrEmpty(StatsSubjectId)?"math":StatsSubjectId),SubjectId:string.IsNullOrEmpty(StatsSubjectId)?null:StatsSubjectId,From:b.From,To:b.To)).ToList();
            StatsHeatmap=Heatmap(from,to,string.IsNullOrEmpty(StatsSubjectId)?null:StatsSubjectId);
            ChapterPlot=_records.Where(r=>!r.Deleted&&(scope==null||r.SubjectId==scope)).SelectMany(r=>r.Parts.Where(p=>p.Date>=from&&p.Date<=to).Select(p=>(r,p))).GroupBy(x=>(x.r.SubjectId,x.r.ChapterId)).Select(g=>
            {var ch=AllChapters.FirstOrDefault(c=>c.Id==g.Key.ChapterId);var sub=AllSubjects.FirstOrDefault(s=>s.Id==g.Key.SubjectId);return new PlotPoint(g.Key.ChapterId??"__unspecified",(sub?.Name??g.Key.SubjectId)+(sub?.Archived==true?" · 科目归档":"")+" / "+(ch?.Name??"未指定章节")+(ch?.Archived==true?" · 章节归档":""),g.Sum(x=>x.p.DurationMs),ThemeService.SubjectColor(g.Key.SubjectId),g.Key.SubjectId,from,to);}).OrderByDescending(p=>p.Value).ToList();
            RefreshReviewMaterials();
        }
        catch(ArgumentException e){Status=e.Message;Analysis=null;SubjectPlot=DailyPlot=TrendPlot=StatsHeatmap=ChapterPlot=Array.Empty<PlotPoint>();StatsTotal=NaturalAverage=LearningAverage="—";StatsDays=StatsSessions="—";SubjectDisplayNote="";ReviewFactSummary="请先选择有效日期范围";ReviewSnapshotSummary="";}
    }
    private List<PlotPoint> DailyPoints(IEnumerable<DailyBreakdown> daily,string? subjectScope=null)=>daily.Select(d=>new PlotPoint(d.Date.ToString("yyyy-MM-dd"),d.Date.ToString("MM-dd"),d.DurationMs,ThemeService.SubjectColor("math"),SubjectId:subjectScope,From:d.Date,To:d.Date,
        Segments:d.Subjects.Select(s=>new PlotPoint(d.Date+"-"+s.Key,(AllSubjects.FirstOrDefault(x=>x.Id==s.Key)?.Name??s.Key)+" · "+d.Date.ToString("yyyy-MM-dd"),s.Value,ThemeService.SubjectColor(s.Key),s.Key,d.Date,d.Date)).ToList())).ToList();
    private List<PlotPoint> Heatmap(DateOnly from,DateOnly to,string? subject)
    {
        var start=from.AddDays(-(((int)from.DayOfWeek+6)%7));var dates=_records.Where(r=>!r.Deleted&&(subject==null||r.SubjectId==subject)).SelectMany(r=>r.Parts).Where(p=>p.Date>=from&&p.Date<=to).GroupBy(p=>p.Date).ToDictionary(g=>g.Key,g=>g.Sum(p=>p.DurationMs));
        return Enumerable.Range(0,to.DayNumber-start.DayNumber+1).Select(i=>{var d=start.AddDays(i);var ms=dates.GetValueOrDefault(d);return new PlotPoint(d.ToString("yyyy-MM-dd"),d.ToString("yyyy-MM-dd"),ms,ThemeService.SubjectColor("math"),subject,d,d,Status:d<from?"范围外":DayStatus(d,ms));}).ToList();
    }
    private string DayStatus(DateOnly date,long ms)
    {
        var pending=Timer.State==TimerState.NeedsReview&&(Timer.Parts.Any(p=>p.Date==date)||Timer.LastSample is { } sample&&DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(sample.Utc,TimeParts.ResolveZone(Timer.ZoneId)).DateTime)==date);
        var rest=_settings.GetValueOrDefault("rest-day:"+date.ToString("yyyy-MM-dd"))=="true";
        return (pending?(ms>0?"待核对 · 含已确认学习":"待核对 · 未知时长不计入"):ms>0?"学习日":"尚无记录")+(rest?" · 休息标记":"");
    }
    private void RefreshDayPresentation()
    {
        var day=DateOnly.FromDateTime(SelectedDate);var records=_records.Where(r=>!r.Deleted).ToList();var parts=records.SelectMany(r=>r.Parts.Where(p=>p.Date==day).Select(p=>(Record:r,Part:p))).ToList();
        DayTotal=Duration(parts.Sum(p=>p.Part.DurationMs));DayState=DayStatus(day,parts.Sum(p=>p.Part.DurationMs));RestDay=_settings.GetValueOrDefault("rest-day:"+day.ToString("yyyy-MM-dd"))=="true";
        Replace(DurationRecords,records.Where(r=>r.Parts.Any(p=>p.Date==day&&p.Kind==PartKind.DurationOnly)).Select(r=>Row(r,day) with{ScopeKind=PartKind.DurationOnly,Contribution=r.Parts.Where(p=>p.Date==day&&p.Kind==PartKind.DurationOnly).Sum(p=>p.DurationMs)}));
        TimelinePlot=parts.Where(p=>p.Part.Kind==PartKind.Interval).OrderBy(p=>p.Part.StartUtcMs).Select(x=>
        {
            var p=x.Part;var zone=TimeParts.ResolveZone(p.ZoneId);var start=DateTimeOffset.FromUnixTimeMilliseconds(p.StartUtcMs!.Value);var end=DateTimeOffset.FromUnixTimeMilliseconds(p.EndUtcMs!.Value);
            DateTimeOffset Boundary(DateOnly d){var local=d.ToDateTime(TimeOnly.MinValue);while(zone.IsInvalidTime(local))local=local.AddMinutes(1);return zone.IsAmbiguousTime(local)?new DateTimeOffset(local,zone.GetAmbiguousTimeOffsets(local).Max()):new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local,zone));}
            var a=Boundary(day);var b=Boundary(day.AddDays(1));
            return new PlotPoint(x.Record.Id,AllSubjects.FirstOrDefault(s=>s.Id==x.Record.SubjectId)?.Name??x.Record.SubjectId,p.DurationMs,ThemeService.SubjectColor(x.Record.SubjectId),x.Record.SubjectId,day,day,Status:$"{TimeZoneInfo.ConvertTime(start,zone):HH:mm:ss}–{TimeZoneInfo.ConvertTime(end,zone):HH:mm:ss}",Start:(start-a).TotalMilliseconds/(b-a).TotalMilliseconds,End:(end-a).TotalMilliseconds/(b-a).TotalMilliseconds);
        }).ToList();
    }
    public async Task ToggleRestDayAsync()=>await RunAsync(()=>Database.SaveSetting("rest-day:"+SelectedDate.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture),RestDay?"false":"true"),"休息日标记已更新；已有学习数据不被改变");
    public void OpenDetail(RecordRow row)
    {
        SelectedRecord=row;DrawerOpen=true;Replace(DetailParts,row.Record.Parts.Select(p=>Part(p,false)));Replace(ExcludedDetailParts,Database.ExcludedParts(row.Record.Id).Select(p=>Part(p,true)));
        RevisionText=string.Join("\n\n",Database.History(row.Record.Id).Where(r=>r.EntityKind=="session").Select(r=>
        {string Summary(string? json){if(json==null)return "无";var s=JsonSerializer.Deserialize<StudyRecord>(json)!;return $"{Duration(s.DurationMs)} · {s.Parts.Count} 片段 · {(s.Deleted?"已删除":"有效")} · {string.Join("、",s.Parts.Select(p=>p.Date).Distinct())}";}
            return $"{r.At.ToLocalTime():yyyy-MM-dd HH:mm:ss} · v{r.Version} · {(r.Reason.StartsWith("排除学习片段")?"排除单个片段":r.Reason.StartsWith("恢复学习片段")?"恢复单个片段":r.Reason)}\n{Summary(r.BeforeJson)} → {Summary(r.AfterJson)}";}));
    }
    private static PartRow Part(StudyPart p,bool excluded)=>new(p,p.Kind==PartKind.DurationOnly?"未提供时间位置":$"{TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeMilliseconds(p.StartUtcMs!.Value),TimeParts.ResolveZone(p.ZoneId)):HH:mm:ss} — {TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeMilliseconds(p.EndUtcMs!.Value),TimeParts.ResolveZone(p.ZoneId)):HH:mm:ss}",Duration(p.DurationMs),excluded);
    public void OpenRecords(DateOnly from,DateOnly to,string? subject=null,string? chapter=null)
    {DrawerOpen=false;RecordFrom=from.ToDateTime(TimeOnly.MinValue);RecordTo=to.ToDateTime(TimeOnly.MinValue);RecordSubjectId=subject??"";RecordChapterId=chapter??"";RecordSource="";RecordState="active";Page=6;RefreshRecordFilters();}
    public void StatsRange(string period)
    {var today=LocalToday();StatsTo=today.ToDateTime(TimeOnly.MinValue);StatsFrom=(period=="day"?today:period=="week"?today.AddDays(-(((int)today.DayOfWeek+6)%7)):period=="month"?new DateOnly(today.Year,today.Month,1):today.AddDays(-29)).ToDateTime(TimeOnly.MinValue);}
}
