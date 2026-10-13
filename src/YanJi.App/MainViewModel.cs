using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YanJi.Core;
using YanJi.Data;

namespace YanJi.App;

public sealed record RecordRow(StudyRecord Record,string Subject,string Chapter,string Duration,string Allocation,string Source,long? Contribution=null,bool Pending=false,DateOnly? ScopeFrom=null,DateOnly? ScopeTo=null,PartKind? ScopeKind=null)
{
    public string Note=>Record.Note.Length==0?"无备注":Record.Note;
    public string State=>Record.Deleted?"已删除":Pending?"待核对":"有效";
    public string Color=>ThemeService.SubjectColor(Record.SubjectId);
    public bool Deleted=>Record.Deleted;
    public string ScopedDuration=>Contribution is long ms?DurationText.Format(ms):Duration;
    public string ScopeLabel=>Contribution!=null?"本页贡献 "+ScopedDuration+" · 整次 "+Duration:"整次 "+Duration;
    public string Dates=>string.Join("、",Record.Parts.Select(p=>p.Date).Distinct().Order().Select(d=>d.ToString("yyyy-MM-dd")));
    public string Summary=>Source+" · "+State+" · "+Record.Parts.Count+" 个片段 · "+Dates;
}
public sealed record CalendarDay(DateTime Date,string Day,string Duration,bool InMonth,bool Selected,string Status="尚无记录",string Color="#EDF3F5")
{ public string Label=>Day+"\n"+(Duration.Length>0?Duration:Status=="尚无记录"?"无记录":Status);public string Description=>$"{Date:yyyy-MM-dd} · {Status}"+(Duration.Length>0?" · "+Duration:""); }
public sealed record StatRow(string Label,string Duration,double Width);
public sealed partial class MainViewModel : ObservableObject
{
    public YanJiDatabase Database {get;}
    internal long TimerSaveCount {get;private set;}
    internal double TimerSaveTotalMilliseconds {get;private set;}
    private TimerMachine _timer=new();
    private TimerSnapshot? _durable;
    private DateTimeOffset _lastCheckpoint=DateTimeOffset.MinValue;
    private DateTimeOffset _timerRetryAt=DateTimeOffset.MinValue;
    private bool _loadingReflection;
    private bool _reflectionDirty;
    private DateOnly _reflectionDate=DateOnly.FromDateTime(DateTime.Today);
    private DateTimeOffset _reflectionRetryAt=DateTimeOffset.MinValue;
    public event Action<string>? Error;
    [ObservableProperty] private bool busy;
    [ObservableProperty] private int page;
    [ObservableProperty] private string status="离线准备就绪 · 数据仅保存在本机";
    [ObservableProperty] private string timerText="00:00:00";
    [ObservableProperty] private string timerStateText="选一个科目，从此刻开始";
    [ObservableProperty] private bool timerActive;
    [ObservableProperty] private bool timerRunning;
    [ObservableProperty] private bool needsReview;
    [ObservableProperty] private string reviewReason="";
    [ObservableProperty] private string reviewEvidence="";
    [ObservableProperty] private Subject? timerSubject;
    [ObservableProperty] private Chapter? timerChapter;
    [ObservableProperty] private string timerNote="";
    [ObservableProperty] private Subject? manualSubject;
    [ObservableProperty] private Chapter? manualChapter;
    [ObservableProperty] private DateTime manualDate=DateTime.Today;
    [ObservableProperty] private string manualMinutes="30";
    [ObservableProperty] private string manualNote="";
    [ObservableProperty] private string manualWarning="纯时长不生成起止时间；无法确定重叠时，不扣减学习时间。";
    [ObservableProperty] private DateTime selectedDate=DateTime.Today;
    [ObservableProperty] private DateTime calendarMonth=DateTime.Today;
    [ObservableProperty] private string monthLabel="";
    [ObservableProperty] private string reflection="";
    [ObservableProperty] private bool showDeleted;
    [ObservableProperty] private string todayTotal="0 分钟";
    [ObservableProperty] private string weekTotal="0 分钟";
    [ObservableProperty] private string monthTotal="0 分钟";
    [ObservableProperty] private DateTime statsFrom=DateTime.Today.AddDays(-29);
    [ObservableProperty] private DateTime statsTo=DateTime.Today;
    [ObservableProperty] private string statsTotal="0 分钟";
    [ObservableProperty] private string zoneId="Asia/Shanghai";
    [ObservableProperty] private string examDate="尚未设置考试日期";
    public ObservableCollection<Subject> Subjects {get;}=[];
    public ObservableCollection<Subject> TimerSubjects {get;}=[];
    public ObservableCollection<Subject> AllSubjects {get;}=[];
    public ObservableCollection<Chapter> TimerChapters {get;}=[];
    public ObservableCollection<Chapter> ManualChapters {get;}=[];
    public ObservableCollection<Chapter> AllChapters {get;}=[];
    public ObservableCollection<RecordRow> TodayRecords {get;}=[];
    public ObservableCollection<RecordRow> DayRecords {get;}=[];
    public ObservableCollection<CalendarDay> Days {get;}=[];
    public ObservableCollection<LearningTask> Tasks {get;}=[];
    public ObservableCollection<WeakPoint> WeakPoints {get;}=[];
    public ObservableCollection<StatRow> Stats {get;}=[];
    public TimerSnapshot Timer=>_timer.Snapshot;
    public MainViewModel(string dataDirectory){Database=new(dataDirectory);}
    public async Task InitializeAsync()
    {
        await RunAsync(()=>
        {
            var checkpoint=Database.LoadTimer();
            _timer=new();_durable=checkpoint;if(checkpoint!=null)_timer.Restore(checkpoint,WindowsClock.Sample());
            Database.AutoBackup();
        },"本地数据已加载");
        ManualDate=SelectedDate=CalendarMonth=LocalToday().ToDateTime(TimeOnly.MinValue);
        StatsTo=ManualDate;StatsFrom=ManualDate.AddDays(-29);Refresh();UpdateTimer();
    }
    public DateOnly LocalToday()=>DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow,TimeParts.ResolveZone(ZoneId)).DateTime);
    public static string Duration(long milliseconds)=>DurationText.Format(milliseconds);
    private static void Replace<T>(ObservableCollection<T> target,IEnumerable<T> source){target.Clear();foreach(var item in source)target.Add(item);}
    private void LoadChapters(ObservableCollection<Chapter> target,string? subject,bool archived=false)
    {Replace(target,new[]{new Chapter("",subject??"","不指定章节")}.Concat(subject==null?[]:Database.Chapters(subject,archived)));}
    private static string? ChapterId(Chapter? chapter)=>string.IsNullOrWhiteSpace(chapter?.Id)?null:chapter.Id;
    partial void OnTimerSubjectChanged(Subject? value){LoadChapters(TimerChapters,value?.Id,_timer.Snapshot.IsActive);TimerChapter=TimerChapters.FirstOrDefault();}
    partial void OnManualSubjectChanged(Subject? value){LoadChapters(ManualChapters,value?.Id);ManualChapter=ManualChapters.FirstOrDefault();UpdateWarning();}
    partial void OnManualDateChanged(DateTime value)=>UpdateWarning();
    partial void OnSelectedDateChanged(DateTime value){if(!Busy)LoadDay();}
    partial void OnShowDeletedChanged(bool value){if(!Busy)LoadDay();}
    partial void OnReflectionChanged(string value){if(!_loadingReflection)_reflectionDirty=true;}
    private void UpdateWarning()
    {if(ManualSubject==null)return;var day=DateOnly.FromDateTime(ManualDate);var any=Database.Records(day).Any(r=>r.SubjectId==ManualSubject.Id);
        ManualWarning=any?"这一天已有该科目记录。追加前请核对；纯时长可能重叠，但不会擅自扣减。":"纯时长不生成起止时间；无法确定重叠时，不扣减学习时间。";}
    private RecordRow Row(StudyRecord record,DateOnly? focus=null)=>new(record,AllSubjects.FirstOrDefault(s=>s.Id==record.SubjectId)?.Name??record.SubjectId,
        AllChapters.FirstOrDefault(c=>c.Id==record.ChapterId)?.Name??"未指定章节",Duration(record.DurationMs),
        string.Join(" · ",record.Parts.GroupBy(p=>p.Date).Select(g=>$"{g.Key:MM-dd} {Duration(g.Sum(p=>p.DurationMs))}")),record.Source==SessionSource.Timer?"计时":"补录",focus==null?null:record.Parts.Where(p=>p.Date==focus).Sum(p=>p.DurationMs),record.Id==Timer.SessionId&&Timer.State==TimerState.NeedsReview,focus,focus);
    public void Refresh()
    {
        var snapshot=_timer.Snapshot;var active=snapshot.IsActive;
        var timerId=active?snapshot.SubjectId:TimerSubject?.Id;var timerChapterId=active?snapshot.ChapterId:TimerChapter?.Id;
        var manualId=ManualSubject?.Id;var manualChapterId=ManualChapter?.Id;
        Replace(AllSubjects,Database.Subjects(true));Replace(Subjects,AllSubjects.Where(s=>!s.Archived));Replace(AllChapters,Database.Chapters(null,true));
        Replace(TimerSubjects,AllSubjects.Where(s=>!s.Archived||active&&s.Id==timerId));
        TimerSubject=TimerSubjects.FirstOrDefault(s=>s.Id==timerId)??TimerSubjects.FirstOrDefault();
        LoadChapters(TimerChapters,TimerSubject?.Id,active);TimerChapter=TimerChapters.FirstOrDefault(c=>c.Id==timerChapterId)??TimerChapters.FirstOrDefault();
        if(active)TimerNote=snapshot.Note;
        ManualSubject=Subjects.FirstOrDefault(s=>s.Id==manualId)??Subjects.FirstOrDefault();
        LoadChapters(ManualChapters,ManualSubject?.Id);ManualChapter=ManualChapters.FirstOrDefault(c=>c.Id==manualChapterId)??ManualChapters.FirstOrDefault();
        Replace(Tasks,Database.Tasks());Replace(WeakPoints,Database.WeakPoints());
        ZoneId=Database.Setting("zone","Asia/Shanghai");TimeParts.ResolveZone(ZoneId);
        ExamDate=Database.Setting("exam-date","") is string date&&date.Length>0?"考试日期 "+date:"2028 年入学 · 考试日期待定";
        var today=LocalToday();var week=today.AddDays(-(((int)today.DayOfWeek+6)%7));var month=new DateOnly(today.Year,today.Month,1);
        TodayTotal=Duration(Database.DailyTotals(today,today).Sum(x=>x.DurationMs));WeekTotal=Duration(Database.DailyTotals(week,today).Sum(x=>x.DurationMs));MonthTotal=Duration(Database.DailyTotals(month,today).Sum(x=>x.DurationMs));
        Replace(TodayRecords,Database.Records(today).OrderByDescending(r=>r.CreatedAt).Take(8).Select(r=>Row(r,today)));RefreshPresentation();LoadDay();UpdateWarning();
    }
    private void LoadDay()
    {
        var date=DateOnly.FromDateTime(SelectedDate);Replace(DayRecords,Database.Records(date,ShowDeleted).Select(r=>Row(r,date)));
        if(!_reflectionDirty)
        {_loadingReflection=true;Reflection=Database.Reflection(date);_reflectionDate=date;_loadingReflection=false;}
        var first=new DateOnly(CalendarMonth.Year,CalendarMonth.Month,1);var start=first.AddDays(-(((int)first.DayOfWeek+6)%7));
        var totals=Database.DailyTotals(start,start.AddDays(41)).ToDictionary(t=>t.Date,t=>t.DurationMs);
        MonthLabel=CalendarMonth.ToString("yyyy 年 M 月");Replace(Days,Enumerable.Range(0,42).Select(i=>{var d=start.AddDays(i);var ms=totals.GetValueOrDefault(d);return new CalendarDay(d.ToDateTime(TimeOnly.MinValue),d.Day.ToString(),ms>0?Duration(ms):"",d.Month==first.Month,d==date,DayStatus(d,ms),ms>0?ThemeService.SubjectColor("math"):ThemeService.Dark?"#223440":"#EDF3F5");}));RefreshDayPresentation();
    }
    public void RefreshStats()
    {
        BuildAnalytics();
    }
    public async Task<bool> RunAsync(Action operation,string success,bool refresh=true)
    {
        if(Busy)return false;Busy=true;
        try{await Task.Run(operation);Status=success;if(refresh)Refresh();return true;}
        catch(Exception exception){Status="操作未完成："+exception.Message;Error?.Invoke(Status);return false;}
        finally{Busy=false;UpdateTimer();}
    }
    private async Task PersistTimerAsync(string? reason=null)
    {
        var snapshot=_timer.Snapshot;
        try{var clock=Stopwatch.StartNew();var saved=await Task.Run(()=>Database.SaveTimer(snapshot,reason));TimerSaveCount++;TimerSaveTotalMilliseconds+=clock.Elapsed.TotalMilliseconds;_timer.ConfirmSaved(saved.Version);_durable=saved;_lastCheckpoint=DateTimeOffset.UtcNow;_timerRetryAt=DateTimeOffset.MinValue;}
        catch
        {
            _timer=new();if(_durable?.IsActive==true){_timer.Restore(_durable,WindowsClock.Sample());_timer.MarkUncertain("保存失败；仅保留上次成功检查点，请核对未保存时间。");}
            throw;
        }
    }
    public async Task TickAsync()
    {
        if(Busy)return;
        if(_reflectionDirty&&DateTimeOffset.UtcNow>=_reflectionRetryAt)await SaveReflectionAsync();
        if(Busy)return;
        var before=_timer.Snapshot.State;_timer.Advance(WindowsClock.Sample());UpdateTimer();
        if(!TimerActive)return;
        if(DateTimeOffset.UtcNow<_timerRetryAt)return;
        var interval=int.TryParse(Database.Setting("checkpoint-seconds","15"),out var number)?Math.Clamp(number,2,60):15;
        if(TimerRunning&&(DateTimeOffset.UtcNow-_lastCheckpoint).TotalSeconds>=interval || _durable?.State!=_timer.Snapshot.State)
        {
            Busy=true;try{await PersistTimerAsync(before==TimerState.Running&&NeedsReview?"异常待核对":null);Refresh();}
            catch(Exception e){_timerRetryAt=DateTimeOffset.UtcNow.AddSeconds(30);Status="计时保存失败："+e.Message;Error?.Invoke(Status);}finally{Busy=false;UpdateTimer();}
        }
    }
    private void UpdateTimer()
    {
        var s=_timer.Snapshot;TimerActive=s.IsActive;TimerRunning=s.State==TimerState.Running;NeedsReview=s.State==TimerState.NeedsReview;
        var elapsed=TimeSpan.FromMilliseconds(s.DurationMs);TimerText=$"{(long)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
        TimerStateText=s.State switch{TimerState.Running=>"正在学习 · 暂停时长不计入",TimerState.Paused=>"已暂停 · 可以继续或结束保存",TimerState.NeedsReview=>"待核对 · 未确认时间未计入",TimerState.Completed=>"已保存 · 下一段学习随时开始",_=>"选一个科目，从此刻开始"};ReviewReason=s.ReviewReason??"";
        ReviewEvidence=s.TrustedEndUtcMs is long end?$"最后确认到 {TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeMilliseconds(end),TimeParts.ResolveZone(s.ZoneId)):yyyy-MM-dd HH:mm:ss}（{s.ZoneId}）。已确认 {Duration(s.DurationMs)}。":"";
    }
    [RelayCommand] private async Task StartAsync()
    {
        if(Busy||TimerSubject==null||TimerActive)return;Busy=true;
        try{_timer.Start(TimerSubject.Id,ChapterId(TimerChapter),TimerNote,ZoneId,WindowsClock.Sample());await PersistTimerAsync("开始计时");Status="计时已开始；关闭窗口会进入托盘。";}
        catch(Exception e){Status=e.Message;Error?.Invoke(Status);}finally{Busy=false;UpdateTimer();}
    }
    [RelayCommand] private async Task PauseResumeAsync()
    {
        if(Busy||!TimerActive||NeedsReview)return;Busy=true;
        try{if(TimerRunning)_timer.Pause(WindowsClock.Sample());else _timer.Resume(WindowsClock.Sample());await PersistTimerAsync(TimerRunning?"暂停":"继续");Status="计时状态已保存";}
        catch(Exception e){Status=e.Message;Error?.Invoke(Status);}finally{Busy=false;UpdateTimer();}
    }
    [RelayCommand] private async Task FinishAsync()
    {
        if(Busy||!TimerActive||NeedsReview)return;Busy=true;
        try{_timer.Finish(WindowsClock.Sample());await PersistTimerAsync("结束保存");Refresh();Status="本次学习已保存";}
        catch(Exception e){Status=e.Message;Error?.Invoke(Status);}finally{Busy=false;UpdateTimer();}
    }
    public async Task<bool> PrepareExitAsync(bool finish)
    {
        if(Busy)return false;if(!TimerActive)return true;Busy=true;
        try
        {
            if(finish)_timer.Finish(WindowsClock.Sample());
            else if(TimerRunning)_timer.Pause(WindowsClock.Sample());
            await PersistTimerAsync(finish?"结束保存后退出":"暂停或待核对状态退出");return true;
        }
        catch(Exception e){Status="退出保存失败，应用保持打开："+e.Message;Error?.Invoke(Status);return false;}
        finally{Busy=false;UpdateTimer();}
    }
    public async Task MarkUncertainAsync(string reason)
    {
        // Called on the UI dispatcher. Freeze immediately, even if a checkpoint write is in flight.
        _timer.MarkUncertain(reason);UpdateTimer();
        if(Busy)return;Busy=true;try{if(TimerActive)await PersistTimerAsync("系统事件待核对");}catch(Exception e){Status=e.Message;}finally{Busy=false;UpdateTimer();}
    }
    public async Task ResolveAsync(IEnumerable<StudyPart> extra)
    {
        if(Busy||!NeedsReview)return;Busy=true;
        try{_timer.ResolveReview(extra,WindowsClock.Sample());await PersistTimerAsync("用户核对未知时间");Refresh();Status="核对完成，计时保持暂停；可以继续或结束。";}
        catch(Exception e){Status=e.Message;Error?.Invoke(Status);}finally{Busy=false;UpdateTimer();}
    }
    [RelayCommand] private async Task AddManualAsync()
    {
        if(ManualSubject==null||Busy)return;
        if(!double.TryParse(ManualMinutes,NumberStyles.Number,CultureInfo.CurrentCulture,out var minutes)||!double.IsFinite(minutes)||minutes<=0||minutes>1440){Status="请输入大于 0、最多 1440 的分钟数。";return;}
        try
        {await FlushTimerAsync();var record=new StudyRecord(Guid.NewGuid().ToString("N"),ManualSubject.Id,ChapterId(ManualChapter),SessionSource.Manual,ManualNote,
            [TimeParts.Duration(DateOnly.FromDateTime(ManualDate),TimeSpan.FromMinutes(minutes),ZoneId)]);
            if(await RunAsync(()=>Database.SaveRecord(record,"追加学习时长"),"补录已追加；已有记录可在每日详情中修正。"))ManualNote="";}
        catch(Exception e){Status="补录未完成："+e.Message;Error?.Invoke(Status);}
    }
    public async Task FlushTimerAsync(){if(Busy||!TimerActive)return;Busy=true;try{_timer.Advance(WindowsClock.Sample());await PersistTimerAsync();}finally{Busy=false;UpdateTimer();}}
    public async Task<bool> SaveReflectionAsync()
    {if(!_reflectionDirty)return true;if(Busy)return false;
        var date=_reflectionDate;var text=Reflection;
        if(await RunAsync(()=>Database.SaveReflection(date,text),"每日感想已保存",false)){_reflectionDirty=false;return true;}
        _reflectionRetryAt=DateTimeOffset.UtcNow.AddSeconds(30);return false;}
    [RelayCommand] private async Task SelectDayAsync(CalendarDay? day)
    {if(day==null||Busy||!await SaveReflectionAsync())return;SelectedDate=day.Date;LoadDay();}
    [RelayCommand] private async Task PreviousMonthAsync(){if(!await SaveReflectionAsync())return;CalendarMonth=CalendarMonth.AddMonths(-1);LoadDay();}
    [RelayCommand] private async Task NextMonthAsync(){if(!await SaveReflectionAsync())return;CalendarMonth=CalendarMonth.AddMonths(1);LoadDay();}
    [RelayCommand] private void Recalculate()=>RefreshStats();
    public async Task ChangeDayAsync(DateTime date){if(!await SaveReflectionAsync())return;SelectedDate=date;LoadDay();}
}
