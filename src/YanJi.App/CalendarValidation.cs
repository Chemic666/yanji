using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using YanJi.Core;
namespace YanJi.App;
/// <summary>Focused integration check. Fresh project artifacts only; actual HWND screenshots and real-time sampling.</summary>
internal static class CalendarValidation
{
    private static IEnumerable<DependencyObject> Children(DependencyObject p)
    {for(int i=0;i<VisualTreeHelper.GetChildrenCount(p);i++){var c=VisualTreeHelper.GetChild(p,i);yield return c;foreach(var d in Children(c))yield return d;}}
    internal static async Task RunAsync(MainWindow w,string output)
    {
        var m=w.Model;var d=m.Database;var app=(App)Application.Current;Directory.CreateDirectory(output);
        var checks=new List<object>();var shots=new List<object>();var samples=new List<object>();var actions=new List<object>();var gaps=new List<double>();DispatcherTimer? pulse=null;
        void Write(string file,object data)=>File.WriteAllText(Path.Combine(output,file),JsonSerializer.Serialize(data,new JsonSerializerOptions{WriteIndented=true}));
        void Check(bool ok,string name){checks.Add(new{Name=name,Status=ok?"PASS":"FAIL"});Write("checks.json",checks);if(!ok)throw new InvalidOperationException(name);}
        async Task Shot(string name){w.UpdateLayout();await Task.Delay(180);var file=name+".png";NativeCapture.Save(w,Path.Combine(output,file));shots.Add(new{File=file,Width=w.ActualWidth,Height=w.ActualHeight,Dark=m.DarkTheme,Dpi=VisualTreeHelper.GetDpi(w).PixelsPerInchX,Scroll=w.MainScroll.VerticalOffset,Date=m.SelectedDate,Month=m.MonthLabel,Sha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(output,file))))});Write("screenshots.json",shots);}
        long CalendarTotal(DateOnly date)=>m.Days.Single(x=>DateOnly.FromDateTime(x.Date)==date).Milliseconds;
        string Mini()=>Children(w.MiniTimer!).OfType<TextBlock>().First(t=>System.Windows.Data.BindingOperations.GetBinding(t,TextBlock.TextProperty)?.Path.Path=="TimerText").Text;
        try
        {
            Write("provenance.json",new{Assembly=typeof(App).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,DllSha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(App).Assembly.Location))),DataDirectory=d.DirectoryPath,Method="real WPF HWND PrintWindow; in-process commands; separate external pointer test",Dpi=VisualTreeHelper.GetDpi(w).PixelsPerInchX,RenderMode=RenderOptions.ProcessRenderMode.ToString()});
            m.Page=4;w.Width=1200;w.Height=800;w.MainScroll.ScrollToTop();await Shot("empty-light-1200");
            Check(w.LearningCalendar.IsVisible&&!w.StatsAdvancedExpander.IsExpanded,"calendar is directly visible while custom parameters remain secondary");
            var date=m.LocalToday();d.SaveChapter(new("calendar-chapter","math","函数、极限与连续 · 长章节名称用于中文排版检查"));
            var original=new StudyRecord("calendar-math","math","calendar-chapter",SessionSource.Manual,"已完成极限练习；明天复习连续与间断。",[TimeParts.Duration(date,TimeSpan.FromMilliseconds(900123),m.ZoneId)]);
            d.SaveRecord(original,"isolated fixture");d.SaveRecord(new("calendar-english","english",null,SessionSource.Manual,"阅读与长难句",[TimeParts.Duration(date,TimeSpan.FromMilliseconds(420987),m.ZoneId)]),"isolated fixture");
            var midnight=new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue),TimeSpan.FromHours(8));
            d.SaveRecord(new("calendar-cross","408",null,SessionSource.Timer,"真实跨午夜时间段",TimeParts.Interval(midnight.AddSeconds(-30).AddMilliseconds(-123),midnight.AddSeconds(40).AddMilliseconds(456),m.ZoneId)),"isolated fixture");
            for(int i=1;i<=28;i++)d.SaveRecord(new("history-"+i,new[]{"math","english","408"}[i%3],null,SessionSource.Manual,"隔离学习记录",[TimeParts.Duration(date.AddDays(-i),TimeSpan.FromMilliseconds(600000+i*123),m.ZoneId)]),"isolated fixture");
            m.Refresh();await m.CalendarTodayCommand.ExecuteAsync(null);m.StatsRange("month");
            var raw=d.DailyTotals(date,date).Single().DurationMs;
            Check(CalendarTotal(date)==raw&&raw==1361566,"today calendar preserves raw milliseconds across subjects and midnight split");
            Check(m.DayRecords.Count==3&&m.DayRecords.Single(r=>r.Record.Id=="calendar-cross").Contribution==40456,"selected day shows exact contribution and whole-session identity");
            Check(m.DurationRecords.Count==2&&m.TimelinePlot.Count==1,"manual durations never receive fabricated timeline positions");
            Check(m.Days.Count==42&&m.Days.Count(x=>x.IsToday)==1&&m.Days.Single(x=>x.IsToday).Selected,"today marker and selected date are distinct state fields");
            await m.PreviousMonthCommand.ExecuteAsync(null);Check(m.CalendarMonth.Month==date.AddMonths(-1).Month,"previous month command");await m.NextMonthCommand.ExecuteAsync(null);Check(CalendarTotal(date)==raw,"next month returns correct aggregates");
            await m.ChangeDayAsync(date.AddDays(6).ToDateTime(TimeOnly.MinValue));Check(m.DayRecords.Count==0&&m.DayTotal=="0 秒","empty date has no invented records");await Shot("empty-selected-day");
            await m.CalendarTodayCommand.ExecuteAsync(null);m.Reflection="隔离每日感想 · 切换日期后保留";await m.ChangeDayAsync(date.AddDays(-1).ToDateTime(TimeOnly.MinValue));await m.CalendarTodayCommand.ExecuteAsync(null);
            Check(m.Reflection=="隔离每日感想 · 切换日期后保留"&&d.Reflection(date)==m.Reflection,"reflection saves before date navigation and cached value stays correct");
            m.OpenDetail(m.DayRecords.First());Check(m.DetailParts.Count>0&&m.DrawerOpen,"calendar row opens existing part and revision drawer");await Shot("day-record-detail");m.DrawerOpen=false;
            var delete=d.Records().Single(r=>r.Id==original.Id);await m.RunAsync(()=>d.DeleteRecord(delete.Id,delete.Version),"isolated soft delete");
            Check(CalendarTotal(date)==raw-900123&&m.TodayDurationMs==raw-900123&&!m.DayRecords.Any(r=>r.Record.Id==delete.Id),"soft deletion updates calendar, daily rows and KPI");
            var removed=d.Records(null,true).Single(r=>r.Id==original.Id);await m.RunAsync(()=>d.RestoreRecord(removed.Id,removed.Version),"isolated restore");Check(CalendarTotal(date)==raw&&m.TodayDurationMs==raw,"restore updates all dependent displays without loss");
            m.ManualDate=date.ToDateTime(TimeOnly.MinValue);m.ManualSubject=m.Subjects.Single(s=>s.Id=="math");m.ManualMinutes="0.5";
            Check(await m.AddManualRecordAsync("calendar-manual-new"),"existing manual workflow succeeds");raw+=30000;
            Check(CalendarTotal(date)==raw&&m.TodayDurationMs==raw&&m.SubjectPlot.Sum(p=>p.Value)==m.Analysis!.DurationMs&&m.ChapterPlot.Sum(p=>p.Value)==m.Analysis.DurationMs,"manual addition refreshes exact calendar and chart aggregates");
            var edit=d.Records().Single(r=>r.Id==original.Id);var oldPart=edit.Parts.Single();
            await m.RunAsync(()=>d.SaveRecord(edit with{Note="修正后的学习内容",Parts=[oldPart with{DurationMs=oldPart.DurationMs+1234}]},"isolated edit"),"isolated edit");raw+=1234;
            Check(CalendarTotal(date)==raw&&m.TodayDurationMs==raw&&m.DayRecords.Single(r=>r.Record.Id==original.Id).Record.Note=="修正后的学习内容","record correction refreshes exact calendar and rows");
            var cross=d.Records().Single(r=>r.Id=="calendar-cross");await m.ChangeDayAsync(date.AddDays(-1).ToDateTime(TimeOnly.MinValue));Check(m.DayRecords.Single(r=>r.Record.Id==cross.Id).Contribution==30123,"previous day contributes only its actual midnight fragment");await m.CalendarTodayCommand.ExecuteAsync(null);
            Check(w.ChapterAnalysis.Visibility==Visibility.Visible&&w.StudyReviewSection.Visibility==Visibility.Visible,"chapter and review results are not hidden by view selectors");
            foreach(var dark in new[]{false,true})foreach(var size in new[]{(1200,800),(900,600)})
            {
                m.DarkTheme=dark;while(m.Busy)await Task.Delay(30);w.Width=size.Item1;w.Height=size.Item2;w.MainScroll.ScrollToTop();await Shot($"data-{(dark?"dark":"light")}-{size.Item1}");
            }
            m.DarkTheme=false;while(m.Busy)await Task.Delay(30);w.Width=1200;w.Height=800;w.UpdateLayout();w.MainScroll.ScrollToTop();await Shot("data-complete-00");
            int index=1;double lastOffset=-1;while(w.MainScroll.VerticalOffset+10<w.MainScroll.ScrollableHeight&&index<12){lastOffset=w.MainScroll.VerticalOffset;w.MainScroll.ScrollToVerticalOffset(lastOffset+500);await Shot("data-complete-"+index++.ToString("00"));if(w.MainScroll.VerticalOffset==lastOffset)break;}
            if(app.CalendarPreview){IconValidation.Capture(w,output);Write("result.json",new{Status="PASS",Checks=checks.Count,Performance="NOT RUN in preview; final candidate runs continuous125s check"});return;}
            // Input-priority heartbeat over multiple checkpoint cycles; no screenshot encoding in measured window.
            m.FocusModeIndex=0;m.Page=4;w.MainScroll.ScrollToTop();w.ShowMiniTimer();await m.StartCommand.ExecuteAsync(null);Check(m.TimerRunning,"free timer starts with calendar page active");
            var startSaveCount=m.TimerSaveCount;PerfTrace.Events.Clear();PerfTrace.Enabled=true;var clock=Stopwatch.StartNew();var last=Stopwatch.GetTimestamp();
            pulse=new DispatcherTimer(DispatcherPriority.Input){Interval=TimeSpan.FromMilliseconds(20)};pulse.Tick+=(_,_)=>{var now=Stopwatch.GetTimestamp();gaps.Add(Stopwatch.GetElapsedTime(last,now).TotalMilliseconds);last=now;};pulse.Start();
            var synchronized=true;bool paused=false;var changed=new List<string>();
            while(clock.Elapsed.TotalSeconds<125)
            {
                await Task.Delay(1000);var main=w.TimerDigits.Text;var mini=Mini();synchronized&=main==mini;changed.Add(main);samples.Add(new{AtMs=clock.Elapsed.TotalMilliseconds,Main=main,Mini=mini,RawMs=m.Timer.DurationMs,State=m.Timer.State,Checkpoints=m.TimerSaveCount});
                if(samples.Count%7==0){var sw=Stopwatch.StartNew();await m.PreviousMonthCommand.ExecuteAsync(null);await m.NextMonthCommand.ExecuteAsync(null);await m.CalendarTodayCommand.ExecuteAsync(null);actions.Add(new{Action="month roundtrip and today",Ms=sw.Elapsed.TotalMilliseconds});}
                if(!paused&&clock.Elapsed.TotalSeconds>=35){await m.PauseResumeCommand.ExecuteAsync(null);var held=m.Timer.DurationMs;await Task.Delay(3000);Check(m.Timer.DurationMs==held,"three second pause does not accumulate study");await m.PauseResumeCommand.ExecuteAsync(null);paused=true;}
            }
            pulse.Stop();PerfTrace.Enabled=false;var spans=PerfTrace.Events.ToArray();
            Check(synchronized&&changed.Distinct().Count()>100,"main and mini timer stay synchronized with real per-second changes");
            Check(m.TimerSaveCount-startSaveCount>=7,"multiple 15 second checkpoints observed during calendar navigation");
            Check(spans.Count(e=>e.Name=="Refresh.UI")==1,"only explicit pause refreshes summary; checkpoint cycles do not rebuild page");
            await m.FinishCommand.ExecuteAsync(null);Check(d.Records().Single(r=>r.Id==m.Timer.SessionId).DurationMs==m.Timer.DurationMs&&m.TodayDurationMs==raw+m.Timer.DurationMs&&CalendarTotal(date)==m.TodayDurationMs,"finish persists exact timer milliseconds and refreshes calendar/KPI");
            Check(d.Integrity()=="ok","real SQLite remains intact");IconValidation.Capture(w,output);
            Write("performance.json",new{ObservedMs=clock.Elapsed.TotalMilliseconds,Checkpoints=m.TimerSaveCount-startSaveCount,RefreshCount=spans.Count(e=>e.Name=="Refresh.UI"),HeartbeatCount=gaps.Count,MaxGapMs=gaps.Max(),P95GapMs=gaps.Order().ElementAt((int)(gaps.Count*.95)),Over50ms=gaps.Count(g=>g>50),Gaps=gaps,Spans=spans,Actions=actions,MainMiniSynchronized=synchronized,PeakWorkingSet=Process.GetCurrentProcess().PeakWorkingSet64});Write("timer-samples.json",samples);
            Write("result.json",new{Status="PASS",Checks=checks.Count,RawTodayMs=m.TodayDurationMs,MonthScopeMs=m.Analysis!.DurationMs,Schema=d.Diagnostics(),PhysicalIME="NOT TESTED",OtherDpi="NOT TESTED",SystemTaskbarTray="see external capture or NOT TESTED"});
        }
        catch(Exception e){app.ValidationFailed=true;Write("failure.json",new{Error=e.ToString()});Write("result.json",new{Status="FAIL",Checks=checks.Count});}
        finally{PerfTrace.Enabled=false;pulse?.Stop();w.SmokeClose();}
    }
}
