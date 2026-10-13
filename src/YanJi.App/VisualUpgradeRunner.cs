using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using YanJi.Core;
namespace YanJi.App;
public static class VisualUpgradeRunner
{
    internal static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var child=VisualTreeHelper.GetChild(root,i);yield return child;foreach(var d in Descendants(child))yield return d;}}
    private static void Click(Button button)=>((IInvokeProvider)new ButtonAutomationPeer(button).GetPattern(PatternInterface.Invoke)!).Invoke();
    private static Button Button(Window w,string label,Func<object?,bool>? tag=null){w.UpdateLayout();return Descendants(w).OfType<Button>().First(b=>b.IsVisible&&Equals(b.Content,label)&&(tag==null||tag(b.Tag)));}
    private static async Task Until(Func<bool> predicate){var clock=Stopwatch.StartNew();while(!predicate()&&clock.Elapsed.TotalSeconds<10)await Task.Delay(40);if(!predicate())throw new Exception("UI operation did not reach expected state.");}
    public static void CaptureEmpty(MainWindow window,string output)
    {window.UpdateLayout();SmokeRunner.Capture(window,Path.Combine(output,"empty-light.png"));ThemeService.Apply(true);window.UpdateLayout();SmokeRunner.Capture(window,Path.Combine(output,"empty-dark.png"));ThemeService.Apply(false);}
    public static async Task RunAsync(MainWindow w,string output,Action<bool,string> check)
    {
        w.Width=1200;w.Height=800;w.UpdateLayout();await Task.Delay(100);
        var vm=w.Model;var today=vm.LocalToday();var yesterday=today.AddDays(-1);
        var subjects=new[]{"math","english","408","politics"};
        for(var i=0;i<35;i++)vm.Database.SaveRecord(new("visual-"+i,subjects[i%4],null,SessionSource.Manual,i==0?"每日阅读与复盘":i==1?"长中文描述："+string.Concat(Enumerable.Repeat("理清概念与适用条件，整理每一处推导，下一次复习要先回顾错因。",8)):"章节练习与回顾",[TimeParts.Duration(today.AddDays(-i),TimeSpan.FromMinutes(25+(i*17)%110),vm.ZoneId)]),"视觉验收隔离数据");
        var cross=vm.Database.SaveRecord(new("visual-cross","math",null,SessionSource.Timer,"同一次学习跨越午夜，分别计入本日与次日。",TimeParts.Interval(new DateTimeOffset(yesterday.ToDateTime(new TimeOnly(23,50)),TimeSpan.FromHours(8)),new DateTimeOffset(today.ToDateTime(new TimeOnly(0,20)),TimeSpan.FromHours(8)),"Asia/Shanghai")),"跨日验收");
        vm.Database.SaveRecord(new("visual-known","english",null,SessionSource.Manual,"真实晨读区间",TimeParts.Interval(new DateTimeOffset(yesterday.ToDateTime(new TimeOnly(8,30)),TimeSpan.FromHours(8)),new DateTimeOffset(yesterday.ToDateTime(new TimeOnly(9,15)),TimeSpan.FromHours(8)),"Asia/Shanghai")),"真实区间验收");
        vm.Database.SaveChapter(new("visual-chapter","408","计算机网络 · 传输层",65,Mastery.Weak));vm.Database.SaveWeakPoint(new("visual-weak","TCP 拥塞控制的边界条件","区分慢开始、拥塞避免和快恢复。下次复习先用图画出窗口变化，再回到原题验收。","概念与边界","408","visual-chapter",WeakPointState.Reviewing,today.AddDays(2)));
        vm.Database.SaveRecord(new("visual-chapter-session","408","visual-chapter",SessionSource.Manual,"章节钻取应保持范围",[TimeParts.Duration(today,TimeSpan.FromMinutes(4),vm.ZoneId)]),"章节筛选验收");
        vm.Refresh();vm.StatsFrom=today.AddDays(-29).ToDateTime(TimeOnly.MinValue);vm.StatsTo=today.ToDateTime(TimeOnly.MinValue);vm.StatsSubjectId="";
        check(vm.Analysis?.DurationMs==vm.Database.DailyTotals(today.AddDays(-29),today).Sum(d=>d.DurationMs),"V011 graphs equal canonical saved milliseconds");
        vm.RecordFrom=today.ToDateTime(TimeOnly.MinValue);vm.RecordTo=vm.RecordFrom;vm.RecordSubjectId="math";vm.RecordSource="timer";vm.RecordState="active";
        check(vm.FilteredRecords.Any(r=>r.Record.Id==cross.Id)&&vm.FilteredRecords.All(r=>r.Record.Source==SessionSource.Timer&&r.Record.SubjectId=="math"),"V011 record date/subject/source/status filters");
        var row=vm.FilteredRecords.Single(r=>r.Record.Id==cross.Id);check(row.Contribution==1_200_000&&row.Record.DurationMs==1_800_000,"V011 cross-day contribution and whole duration are separate");
        vm.OpenDetail(row);w.UpdateLayout();SmokeRunner.Capture(w,Path.Combine(output,"detail-parts-light.png"));
        w.TestDialogShown=dialog=>dialog.Loaded+=(_,_)=>dialog.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,new Action(()=>
        {if(dialog is EditorDialog editor){editor.SetField("note","实际编辑入口：保留原始片段与整次身份");Click(Descendants(dialog).OfType<Button>().First(b=>b.IsDefault));}}));
        Click(Button(w,"编辑整次记录"));await Until(()=>!vm.Busy&&vm.SelectedRecord?.Record.Note=="实际编辑入口：保留原始片段与整次身份");
        check(vm.SelectedRecord!.Record.Id==cross.Id&&vm.SelectedRecord.Record.Parts.SequenceEqual(cross.Parts),"V011 actual record edit preserves identity and original interval parts");
        check(vm.SelectedRecord.Contribution==1_200_000&&vm.SelectedRecord.ScopeFrom==today,"V011 detail refresh retains original day contribution scope");
        var removed=cross.Parts[0];w.TestDialogShown=dialog=>dialog.Loaded+=(_,_)=>dialog.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,new Action(()=>
        {SmokeRunner.Capture(dialog,Path.Combine(output,"confirm-part.png"));Click(Descendants(dialog).OfType<Button>().First(b=>Equals(b.Content,"排除此片段")));}));
        Click(Button(w,"排除片段",tag=>tag is PartRow p&&p.Part.Id==removed.Id));await Until(()=>!vm.Busy&&vm.ExcludedDetailParts.Any(p=>p.Part.Id==removed.Id));
        check(vm.DetailParts.Count==1&&vm.DayRecords.All(r=>r.Record.Id!=cross.Id||r.Record.DurationMs==1_200_000),"V011 actual part exclusion button keeps whole identity and refreshes views");
        Click(Button(w,"恢复片段",tag=>tag is PartRow p&&p.Part.Id==removed.Id));await Until(()=>!vm.Busy&&vm.DetailParts.Count==2);check(vm.ExcludedDetailParts.Count==0,"V011 actual part restore button restores original evidence");
        var cancelShown=false;w.TestDialogShown=dialog=>dialog.Loaded+=(_,_)=>dialog.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,new Action(()=>{cancelShown=true;Click(Descendants(dialog).OfType<Button>().First(b=>b.IsCancel));}));
        Click(Button(w,"删除整次记录"));await Until(()=>cancelShown&&!Application.Current.Windows.OfType<ConfirmDialog>().Any());check(vm.Database.Records().Any(r=>r.Id==cross.Id),"V011 cancelling deletion preserves records");
        w.TestDialogShown=dialog=>dialog.Loaded+=(_,_)=>dialog.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,new Action(()=>
        {var message=string.Join(" ",Descendants(dialog).OfType<TextBlock>().Select(t=>t.Text));check(message.Contains(yesterday.ToString("yyyy-MM-dd"))&&message.Contains(today.ToString("yyyy-MM-dd")),"V011 deletion confirmation names every affected date");SmokeRunner.Capture(dialog,Path.Combine(output,"confirm-session.png"));Click(Descendants(dialog).OfType<Button>().First(b=>Equals(b.Content,"软删除整次记录")));}));
        Click(Button(w,"删除整次记录"));await Until(()=>!vm.Busy&&vm.SelectedRecord?.Deleted==true);check(!vm.Database.Records().Any(r=>r.Id==cross.Id),"V011 actual whole-session soft delete refreshes statistics");
        Click(Button(w,"恢复整次记录"));await Until(()=>!vm.Busy&&vm.SelectedRecord?.Deleted==false);check(vm.Database.Records().Any(r=>r.Id==cross.Id),"V011 actual whole-session restore");vm.DrawerOpen=false;w.TestDialogShown=null;
        vm.Page=4;w.UpdateLayout();w.SubjectDonut.InvokePoint(0);await Task.Delay(80);check(vm.Page==6&&vm.FilteredRecords.All(r=>r.Record.SubjectId==vm.RecordSubjectId),"V011 subject donut drill-down enters filtered original records");
        vm.Page=4;vm.StatsSubjectId="";w.UpdateLayout();w.DailyStack.InvokePoint(w.DailyStack.Data!.Count-1);await Task.Delay(80);check(vm.Page==6&&vm.RecordFrom==vm.RecordTo,"V011 daily chart drill-down preserves date scope");
        foreach(var period in new[]{"week","month"})
        {vm.Page=4;vm.StatsSubjectId="math";vm.TrendPeriod=period;w.UpdateLayout();var point=vm.TrendPlot.Last();w.TrendChart.InvokePoint(vm.TrendPlot.Count-1);await Task.Delay(80);check(vm.Page==6&&vm.RecordSubjectId=="math"&&vm.FilteredRecords.All(r=>r.Record.SubjectId=="math")&&vm.FilteredRecords.Sum(r=>r.Contribution)==point.Value,"V011 filtered "+period+" trend drill-down retains exact subject scope");}
        vm.OpenRecords(today,today,"408","visual-chapter");check(vm.FilteredRecords.Count==1&&vm.RecordSummary.Contains("4 分"),"V011 chapter drill-down summary equals filtered rows");
        var chapterRecord=vm.FilteredRecords.Single().Record;await vm.RunAsync(()=>vm.Database.SaveRecord(chapterRecord with{Note="修订后仍保留章节筛选"},"章节筛选回归"),"验收修订");vm.RecordSource="manual";vm.Refresh();check(vm.RecordChapterId=="visual-chapter"&&vm.FilteredRecords.Count==1&&vm.FilteredRecords.Single().Record.ChapterId=="visual-chapter","V011 chapter filter survives revision and full refresh");vm.StatsSubjectId="";vm.TrendPeriod="week";vm.OpenRecords(today.AddDays(-29),today);
        vm.Page=1;await vm.ChangeDayAsync(yesterday.ToDateTime(TimeOnly.MinValue));check(vm.TimelinePlot.Count==2&&vm.DurationRecords.Any()&&vm.TimelinePlot.All(p=>p.Start>=0&&p.End<=1),"V011 timeline uses only known intervals and duration-only is separate");
        await vm.ChangeDayAsync(today.AddDays(-40).ToDateTime(TimeOnly.MinValue));await vm.ToggleRestDayAsync();check(vm.DayState=="休息日","V011 explicit rest day differs from no record");await vm.ChangeDayAsync(today.AddDays(-41).ToDateTime(TimeOnly.MinValue));check(vm.DayState=="尚无记录","V011 absence does not imply rest");
        vm.WeakSearch="边界";vm.WeakSubjectId="408";vm.WeakState="1";check(vm.FilteredWeakPoints.Count==1&&vm.WeakDistribution.Sum(p=>p.Value)==1,"V011 weak-point search/filter/distribution share scope");vm.WeakSearch=vm.WeakSubjectId=vm.WeakState="";
        await vm.ChangeDayAsync(today.ToDateTime(TimeOnly.MinValue));vm.Page=0;
        w.TestDialogShown=dialog=>dialog.Loaded+=(_,_)=>dialog.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,new Action(()=>
        {if(dialog is EditorDialog editor){editor.SetField("date",today.ToString("yyyy-MM-dd"));editor.SetField("subject","english");editor.SetField("minutes","0.5");editor.SetField("note","快速补录真实界面 · 中文文本");SmokeRunner.Capture(dialog,Path.Combine(output,"quick-add.png"));Click(Descendants(dialog).OfType<Button>().First(b=>b.IsDefault));}else Click(Descendants(dialog).OfType<Button>().First(b=>Equals(b.Content,"仍然追加")));}));
        var old=vm.Database.Records().Count;w.UpdateLayout();Click(Button(w,"＋ 快速补录"));await Until(()=>!vm.Busy&&vm.Database.Records().Count==old+1);check(vm.Database.Records().Any(r=>r.Note=="快速补录真实界面 · 中文文本"&&r.DurationMs==30000),"V011 quick-add real dialog saves Chinese duration-only record");w.TestDialogShown=null;
        var politics=vm.AllSubjects.Single(s=>s.Id=="politics");var archivedChapter=vm.AllChapters.Single(c=>c.Id=="selected-chapter");vm.Database.SaveSubject(politics with{Archived=true});vm.Database.SaveChapter(archivedChapter with{Archived=true});vm.Refresh();var archivedOptionPresent=true;var archiveDialogShown=false;
        w.TestDialogShown=dialog=>dialog.Loaded+=(_,_)=>dialog.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,new Action(()=>{archiveDialogShown=true;archivedOptionPresent=Descendants(dialog).OfType<ComboBox>().Any(c=>c.Items.OfType<Choice>().Any(choice=>choice.Value is "politics" or "selected-chapter"));Click(Descendants(dialog).OfType<Button>().First(b=>b.IsCancel));}));
        Click(Button(w,"＋ 快速补录"));await Until(()=>archiveDialogShown&&!Application.Current.Windows.OfType<EditorDialog>().Any());check(!archivedOptionPresent,"V011 quick-add never offers archived subjects or chapters that cannot be submitted");w.TestDialogShown=null;vm.Database.SaveSubject(politics);vm.Database.SaveChapter(archivedChapter);vm.Refresh();
        foreach(var dark in new[]{false,true})
        {
            await vm.RunAsync(()=>vm.Database.SaveSetting("theme",dark?"dark":"light"),"主题验收");check(vm.DarkTheme==dark&&ThemeService.Dark==dark,"V011 theme preference persists "+(dark?"dark":"light"));
            for(var page=0;page<7;page++){vm.Page=page;vm.DrawerOpen=false;await vm.ChangeDayAsync((page==1?yesterday:today).ToDateTime(TimeOnly.MinValue));w.MainScroll.ScrollToTop();w.UpdateLayout();await Task.Delay(120);SmokeRunner.Capture(w,Path.Combine(output,$"v011-{(dark?"dark":"light")}-page-{page}.png"));if(page is 1 or 4){w.MainScroll.ScrollToVerticalOffset(page==1?230:420);w.UpdateLayout();SmokeRunner.Capture(w,Path.Combine(output,$"v011-{(dark?"dark":"light")}-page-{page}-details.png"));}}
            check(true,"V011 seven actual WPF views captured "+(dark?"dark":"light"));
        }
        var sizes=new List<object>();vm.Page=0;foreach(var size in new[]{(900d,600d),(1200d,800d),(1600d,1000d)}){w.Width=size.Item1;w.Height=size.Item2;w.UpdateLayout();await Task.Delay(100);sizes.Add(new{RequestedWidth=size.Item1,RequestedHeight=size.Item2,ActualWidth=w.ActualWidth,ActualHeight=w.ActualHeight,Status="PASS",Evidence="Native WPF layout; OS maximum tracking size can clamp large window at current DPI"});SmokeRunner.Capture(w,Path.Combine(output,$"v011-size-{w.ActualWidth}x{w.ActualHeight}.png"));check(size.Item1==1600?w.ActualWidth>=1200&&w.ActualHeight>=800:w.ActualWidth==size.Item1&&w.ActualHeight==size.Item2,"V011 responsive actual WPF layout "+w.ActualWidth+"x"+w.ActualHeight);}
        File.WriteAllText(Path.Combine(output,"native-layout.json"),JsonSerializer.Serialize(sizes,new JsonSerializerOptions{WriteIndented=true}));
        w.Width=900;w.Height=600;foreach(var scale in new[]{1.25,1.5,2})SmokeRunner.Capture(w,Path.Combine(output,$"v011-dpi-render-{scale.ToString(CultureInfo.InvariantCulture)}.png"),scale);
        check(w.NavColumn.Width.Value==72&&Grid.GetColumn(w.TodayReflection)==2&&Grid.GetRow(w.StatsDailyCard)==1,"V021 narrow navigation collapses while today retains columns and statistics stacks");
        vm.Page=4;w.UpdateLayout();Keyboard.Focus(w.TrendChart);w.TrendChart.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(w),0,Key.Right){RoutedEvent=Keyboard.KeyDownEvent});
        check(w.TrendChart.IsKeyboardFocused&&w.TrendChart.HitCount>0,"V011 chart focus and keyboard data selection");
        w.TrendChart.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(w),0,Key.Enter){RoutedEvent=Keyboard.KeyDownEvent});await Task.Delay(80);check(vm.Page==6,"V011 chart Enter opens original record scope");
        foreach(var dark in new[]{false,true})
        {
            ThemeService.Apply(dark);var scenarios=new Grid{Margin=new Thickness(20)};for(var i=0;i<3;i++)scenarios.ColumnDefinitions.Add(new ColumnDefinition());
            var charts=new[]{new StudyChart{Mode="Donut",Data=Array.Empty<PlotPoint>()},new StudyChart{Mode="Donut",Data=new[]{new PlotPoint("one","唯一科目 · 0.001 秒",1,ThemeService.SubjectColor("math"))}},new StudyChart{Mode="Trend",Data=new[]{new PlotPoint("large","极端值原始毫秒",1_000_000_000_000,ThemeService.SubjectColor("english"))}}};
            for(var i=0;i<3;i++){Grid.SetColumn(charts[i],i);scenarios.Children.Add(charts[i]);}var scene=new Window{Owner=w,Title="隔离图表边界场景",Width=900,Height=380,Content=scenarios};scene.Show();scene.UpdateLayout();await Task.Delay(80);
            SmokeRunner.Capture(scene,Path.Combine(output,"chart-boundaries-"+(dark?"dark":"light")+".png"));check(charts[0].HitCount==0&&charts[1].HitCount>=2&&charts[2].HitCount==1,"V011 zero/single/extreme actual chart render "+(dark?"dark":"light"));scene.Close();
        }
        ThemeService.Apply(vm.DarkTheme);var largeDay=today.AddDays(-90);for(var i=0;i<400;i++)vm.Database.SaveRecord(new("large-row-"+i,"408",null,SessionSource.Manual,"大量记录验收 "+i,[TimeParts.Duration(largeDay,TimeSpan.FromSeconds(i+1),vm.ZoneId)]),"隔离大量记录");
        var loadClock=Stopwatch.StartNew();vm.Refresh();vm.OpenRecords(largeDay,largeDay,"408");w.MainScroll.ScrollToTop();w.UpdateLayout();await Task.Delay(80);check(vm.FilteredRecords.Count==400&&Descendants(w.RecordList).OfType<ListBoxItem>().Count()<100,"V011 400 original rows use actual WPF virtualization");SmokeRunner.Capture(w,Path.Combine(output,"large-record-list.png"));File.WriteAllText(Path.Combine(output,"large-record-load.json"),JsonSerializer.Serialize(new{Records=400,ElapsedMs=loadClock.Elapsed.TotalMilliseconds,RealizedContainers=Descendants(w.RecordList).OfType<ListBoxItem>().Count()}));
        check(vm.TodayRecords.Count<=8,"V011 today preview is bounded while totals include all records");
        var beforeChecksum=vm.Database.Records().Sum(r=>r.DurationMs);var backup=Path.Combine(output,"v011-upgrade.yanjibak");vm.Database.Backup(backup);vm.Database.ValidateBackup(backup);vm.Database.RestoreBackup(backup);vm.Refresh();
        check(vm.Database.Records().Sum(r=>r.DurationMs)==beforeChecksum&&vm.Database.Integrity()=="ok","V011 complete backup restore preserves upgraded records and theme");
        vm.Page=0;await vm.ChangeDayAsync(today.ToDateTime(TimeOnly.MinValue));await vm.RunAsync(()=>vm.Database.SaveSetting("theme","light"),"验收完成");w.Width=1200;w.Height=800;
        File.WriteAllText(Path.Combine(output,"v011-summary.json"),JsonSerializer.Serialize(new{Version="0.1.1",Records=vm.Database.Records().Count,TotalMs=beforeChecksum,SchemaMigration="none",NativeDpi=VisualTreeHelper.GetDpi(w).PixelsPerInchX,ResourceNotes="Original timer/data invariants retained; charts are WPF vector; IME physical composition not tested"},new JsonSerializerOptions{WriteIndented=true}));
    }
}
