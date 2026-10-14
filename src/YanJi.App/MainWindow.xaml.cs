using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using Microsoft.Win32;
using YanJi.Core;
using Forms=System.Windows.Forms;

namespace YanJi.App;
public partial class MainWindow : Window
{
    public MainViewModel Model {get;}
    private readonly DispatcherTimer _tick=new(){Interval=TimeSpan.FromSeconds(1)};
    private readonly Forms.NotifyIcon _tray;
    private bool _exiting;
    private bool _exitBusy;
    private HwndSource? _source;
    [DllImport("wtsapi32.dll")]private static extern bool WTSRegisterSessionNotification(IntPtr window,int flags);
    [DllImport("wtsapi32.dll")]private static extern bool WTSUnRegisterSessionNotification(IntPtr window);
    public MainWindow(MainViewModel model)
    {
        Model=model;InitializeComponent();DataContext=model;
        ThemeService.Changed+=RefreshChartThemes;
        Width=Math.Min(1200,SystemParameters.WorkArea.Width);Height=Math.Min(800,SystemParameters.WorkArea.Height);
        var menu=new Forms.ContextMenuStrip();menu.Items.Add("打开研迹",null,(_,_)=>Dispatcher.Invoke(ShowFromTray));menu.Items.Add("退出应用",null,async(_,_)=>await Dispatcher.InvokeAsync(RequestExitAsync).Task.Unwrap());
        _tray=new Forms.NotifyIcon{Text="研迹 YanJi",Icon=System.Drawing.SystemIcons.Application,ContextMenuStrip=menu,Visible=true};
        _tray.DoubleClick+=(_,_)=>Dispatcher.Invoke(ShowFromTray);
        _tick.Tick+=async(_,_)=>{try{await Model.TickAsync();_tray.Text="研迹 · "+Model.TimerText;}catch(Exception e){Model.Status="自动保存未完成："+e.Message;}};
        Model.Error+=text=>{var smoke=((App)Application.Current).SmokePath;if(smoke!=null){Directory.CreateDirectory(smoke);File.AppendAllText(Path.Combine(smoke,"operation-errors.txt"),text+"\n");}else MessageBox.Show(this,text,"研迹",MessageBoxButton.OK,MessageBoxImage.Information);};
        SourceInitialized+=(_,_)=>{var handle=new WindowInteropHelper(this).Handle;_source=HwndSource.FromHwnd(handle);_source.AddHook(HandleMessage);WTSRegisterSessionNotification(handle,0);((App)Application.Current).RecordWindow(handle);};
        Loaded+=async(_,_)=>{await Model.InitializeAsync();_tick.Start();if(((App)Application.Current).V03Capture||((App)Application.Current).V03Reopen||((App)Application.Current).V03Layout)await V03CaptureRunner.RunAsync(this,((App)Application.Current).SmokePath!);else if(((App)Application.Current).V023Capture)await V023CaptureRunner.RunAsync(this,((App)Application.Current).SmokePath!);else if(((App)Application.Current).V022Capture)await V022CaptureRunner.RunAsync(this,((App)Application.Current).SmokePath!);else if(((App)Application.Current).V021Capture)await V021CaptureRunner.RunAsync(this,((App)Application.Current).SmokePath!,((App)Application.Current).V021Fixture!);else if(((App)Application.Current).SmokePath!=null)await SmokeRunner.RunAsync(this,((App)Application.Current).SmokePath!);};
        Closing+=OnClosing;
        Closed+=(_,_)=>{CloseMiniTimer();ThemeService.Changed-=RefreshChartThemes;_tick.Stop();if(_source!=null){WTSUnRegisterSessionNotification(_source.Handle);_source.RemoveHook(HandleMessage);}_tray.Dispose();Model.Database.Dispose();Application.Current.Shutdown();};
    }
    public void ShowFromTray(){Show();WindowState=WindowState.Normal;Activate();}
    private IntPtr HandleMessage(IntPtr hwnd,int message,IntPtr wParam,IntPtr lParam,ref bool handled)
    {
        if(message==((App)Application.Current).ShowMessage){ShowFromTray();handled=true;}
        if(message==0x218&&(wParam.ToInt64() is 4 or 6 or 7 or 18))
            _=Model.MarkUncertainAsync("收到电源状态变化通知；休眠与未知间隙未自动计入，请核对。");
        if(message==0x1E)_=Model.MarkUncertainAsync("收到系统时间变化通知；请核对未确认时间。");
        // WM_WTSSESSION_CHANGE lock/unlock intentionally does not change timer state.
        return IntPtr.Zero;
    }
    private async void OnClosing(object? sender,CancelEventArgs e)
    {
        if(_exiting)return;e.Cancel=true;
        if(Model.TimerActive){Hide();return;}
        await RequestExitAsync();
    }
    public async Task RequestExitAsync()
    {
        if(_exitBusy||Model.Busy)return;_exitBusy=true;_tick.Stop();
        try
        {
            if(Model.TimerActive)
            {
                ShowFromTray();var choice=ExitChoice();if(choice==0)return;
                if(choice==1&&Model.NeedsReview){MessageBox.Show(this,"请先核对未知时间，再结束保存。","研迹");return;}
                if(!await Model.PrepareExitAsync(choice==1))return;
                if(choice==1&&Model.TimerActive)return;
            }
            if(!await Model.SaveReflectionAsync())return;_exiting=true;Close();
        }
        catch(Exception e){MessageBox.Show(this,"退出前保存未完成："+e.Message,"研迹");}
        finally{_exitBusy=false;if(!_exiting)_tick.Start();}
    }
    private int ExitChoice()
    {
        var dialog=new Window{Owner=this,Title="退出研迹",Width=520,Height=230,WindowStartupLocation=WindowStartupLocation.CenterOwner,ResizeMode=ResizeMode.NoResize};var stack=new StackPanel{Margin=new Thickness(24)};var result=0;
        stack.Children.Add(new TextBlock{Text="活动计时尚未结束。请选择如何退出：",Margin=new Thickness(0,0,0,24)});
        foreach(var (label,value) in new[]{("结束并保存",1),("暂停后退出",2),("取消退出",0)}){var button=new Button{Content=label};button.Click+=(_,_)=>{result=value;dialog.Close();};stack.Children.Add(button);}dialog.Content=stack;dialog.Height=280;dialog.ShowDialog();return result;
    }
    private async void Exit_Click(object sender,RoutedEventArgs e)=>await RequestExitAsync();
    private async void Navigation_Changed(object sender,SelectionChangedEventArgs e){if(DataContext is MainViewModel vm){if(vm.Page==0)await vm.ChangeDayAsync(vm.LocalToday().ToDateTime(TimeOnly.MinValue));else await vm.SaveReflectionAsync();MainScroll.ScrollToTop();}}
    private async void TodayDetails_Click(object sender,RoutedEventArgs e){await Model.ChangeDayAsync(Model.LocalToday().ToDateTime(TimeOnly.MinValue));Model.Page=1;}
    private async void SaveReflection_Click(object sender,RoutedEventArgs e)=>await Model.SaveReflectionAsync();
    private async void KeepConfirmed_Click(object sender,RoutedEventArgs e)=>await Model.ResolveAsync([]);
    private static string Id()=>Guid.NewGuid().ToString("N");
    private List<Choice> SubjectChoices(bool optional=false,bool includeArchived=true)=> (optional?new List<Choice>{new("","不指定科目")}:[]).Concat(Model.AllSubjects.Where(s=>includeArchived||!s.Archived).Select(s=>new Choice(s.Id,s.Name+(s.Archived?"（归档）":"")))).ToList();
    private List<Choice> ChapterChoices(bool includeArchived=true)=>new List<Choice>{new("","不指定章节")}.Concat(Model.AllChapters.Where(c=>includeArchived||!c.Archived&&Model.Subjects.Any(s=>s.Id==c.SubjectId)).Select(c=>new Choice(c.Id,(Model.AllSubjects.FirstOrDefault(s=>s.Id==c.SubjectId)?.Name??"")+" · "+c.Name))).ToList();
    private static string? Null(string text)=>string.IsNullOrWhiteSpace(text)?null:text;
    private static DateOnly? Date(string text)=>Null(text)==null?null:DateOnly.ParseExact(text.Trim(),"yyyy-MM-dd",CultureInfo.InvariantCulture);
    private Dictionary<string,string>? Edit(string title,Field[] fields,string hint="",Func<Dictionary<string,string>,string?>? validate=null)
    {var dialog=new EditorDialog(this,title,fields,hint,validate);dialog.BindChapters(Model.AllChapters.ToList(),fields.FirstOrDefault(f=>f.Key=="chapter")?.Value);TestDialogShown?.Invoke(dialog);return dialog.ShowDialog()==true?dialog.Values:null;}
    private static string? Required(Dictionary<string,string> values,params string[] keys)=>keys.Any(key=>string.IsNullOrWhiteSpace(values[key]))?"请填写必填项。":null;
    private async void AddSubject_Click(object sender,RoutedEventArgs e)=>await EditSubjectAsync(null);
    private async void EditSubject_Click(object sender,RoutedEventArgs e)=>await EditSubjectAsync(((Button)sender).Tag as Subject);
    private async Task EditSubjectAsync(Subject? subject)
    {
        var v=Edit(subject==null?"新增科目":"编辑科目",[new("name","科目名称 *",subject?.Name??""),new("archived","状态",subject?.Archived==true?"1":"0",[new("0","使用中"),new("1","归档")])],"归档保留已有记录与统计；随时可取消归档。",v=>Required(v,"name"));
        if(v!=null)await Model.RunAsync(()=>Model.Database.SaveSubject(new(subject?.Id??Id(),v["name"],v["archived"]=="1")),"科目已保存");
    }
    private async void AddChapter_Click(object sender,RoutedEventArgs e)=>await EditChapterAsync(null);
    private async void EditChapter_Click(object sender,RoutedEventArgs e)=>await EditChapterAsync(((Button)sender).Tag as Chapter);
    private async Task EditChapterAsync(Chapter? chapter)
    {
        var v=Edit(chapter==null?"新增章节":"编辑章节",[new("subject","科目 *",chapter?.SubjectId??"math",chapter==null?SubjectChoices():SubjectChoices().Where(s=>s.Value==chapter.SubjectId).ToList()),new("name","章节名称 *",chapter?.Name??""),new("progress","完成百分比（0–100）",(chapter?.Progress??0).ToString()),new("mastery","掌握情况",((int)(chapter?.Mastery??Mastery.Unknown)).ToString(),[new("0","未评估"),new("1","薄弱"),new("2","复习中"),new("3","已掌握")]),new("archived","状态",chapter?.Archived==true?"1":"0",[new("0","使用中"),new("1","归档")])],"进度和掌握状态独立，不由学习时间自动推断。",v=>Required(v,"subject","name")??(!int.TryParse(v["progress"],out var n)||n<0||n>100?"进度必须为 0–100 的整数。":null));
        if(v!=null)await Model.RunAsync(()=>Model.Database.SaveChapter(new(chapter?.Id??Id(),v["subject"],v["name"],int.Parse(v["progress"]),(Mastery)int.Parse(v["mastery"]),v["archived"]=="1")),"章节已保存");
    }
    private async void AddTask_Click(object sender,RoutedEventArgs e)=>await EditTaskAsync(null);
    private async void EditTask_Click(object sender,RoutedEventArgs e)=>await EditTaskAsync(((Button)sender).Tag as LearningTask);
    private async Task EditTaskAsync(LearningTask? task)
    {
        var v=Edit(task==null?"新增学习任务":"编辑学习任务",[new("title","任务名称 *",task?.Title??""),new("date","计划日期（可空，yyyy-MM-dd）",task?.DueDate?.ToString("yyyy-MM-dd")??""),new("subject","科目（可选）",task?.SubjectId??"",SubjectChoices(true)),new("done","任务状态",task?.Done==true?"1":"0",[new("0","待完成"),new("1","已完成")])],validate:v=>{Date(v["date"]);return Required(v,"title");});
        if(v!=null)await Model.RunAsync(()=>Model.Database.SaveTask(new(task?.Id??Id(),v["title"],Date(v["date"]),v["done"]=="1",Null(v["subject"]))),"学习任务已保存");
    }
    private async void TaskDone_Click(object sender,RoutedEventArgs e){var box=(CheckBox)sender;var task=(LearningTask)box.Tag;if(!await Model.RunAsync(()=>Model.Database.SaveTask(task with{Done=box.IsChecked==true}),"任务状态已保存"))box.IsChecked=task.Done;}
    private async void AddWeak_Click(object sender,RoutedEventArgs e)=>await EditWeakAsync(null);
    private async void EditWeak_Click(object sender,RoutedEventArgs e)=>await EditWeakAsync(((Button)sender).Tag as WeakPoint);
    private async Task EditWeakAsync(WeakPoint? point)
    {
        var v=Edit(point==null?"记录薄弱知识点":"编辑薄弱知识点",[new("title","知识点 *",point?.Title??""),new("detail","困难与复习方式",point?.Detail??"",Multiline:true),new("category","错误分类",point?.Category??""),new("subject","科目（可选）",point?.SubjectId??"",SubjectChoices(true)),new("chapter","章节（须属于所选科目）",point?.ChapterId??"",ChapterChoices()),new("state","状态",((int)(point?.State??WeakPointState.Open)).ToString(),[new("0","待复习"),new("1","复习中"),new("2","已验收")]),new("date","后续复习日期（可空，yyyy-MM-dd）",point?.ReviewDate?.ToString("yyyy-MM-dd")??"")],validate:v=>{Date(v["date"]);return Required(v,"title")??CheckChapter(v);});
        if(v!=null)await Model.RunAsync(()=>Model.Database.SaveWeakPoint(new(point?.Id??Id(),v["title"],v["detail"],v["category"],Null(v["subject"]),Null(v["chapter"]),(WeakPointState)int.Parse(v["state"]),Date(v["date"]))),"薄弱点已保存");
    }
    private string? CheckChapter(Dictionary<string,string> values)=>Null(values["chapter"])!=null&&!Model.AllChapters.Any(c=>c.Id==values["chapter"]&&c.SubjectId==values["subject"])?"请选择属于该科目的章节。":null;
    private static List<StudyPart> Allocations(string text,string zone)
    {
        List<StudyPart> parts=[];
        foreach(var line in text.Split('\n',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries))
        {var pair=line.Split('=',2,StringSplitOptions.TrimEntries);if(pair.Length!=2)throw new ArgumentException("按“yyyy-MM-dd = 分钟数”填写，每个日期一行。");
            var day=Date(pair[0])!.Value;var minutes=double.Parse(pair[1],CultureInfo.InvariantCulture);if(!double.IsFinite(minutes)||minutes<=0||minutes>1440)throw new ArgumentException("每段分钟数须大于 0、最多 1440。");parts.Add(TimeParts.Duration(day,TimeSpan.FromMinutes(minutes),zone));}
        if(parts.Count==0)throw new ArgumentException("请至少填写一个日期与时长。");return parts;
    }
    private static List<StudyPart> Interval(Dictionary<string,string> values,string zone)
    {
        // Require an explicit offset; never infer ambiguous local wall time around DST.
        foreach(var key in new[]{"start","end"})if(!System.Text.RegularExpressions.Regex.IsMatch(values[key],@"(Z|[+-]\d{2}:\d{2})$"))throw new ArgumentException("起止时间须含 UTC 偏移，如 2026-10-13T08:00+08:00。");
        return TimeParts.Interval(DateTimeOffset.Parse(values["start"],CultureInfo.InvariantCulture),DateTimeOffset.Parse(values["end"],CultureInfo.InvariantCulture),zone);
    }
    private async void KnownInterval_Click(object sender,RoutedEventArgs e)
    {
        var v=Edit("按明确时间区间补录",[new("subject","科目 *",Model.ManualSubject?.Id??"math",SubjectChoices()),new("chapter","主要章节（可选）","",ChapterChoices()),new("start","开始时间（含偏移）",""),new("end","结束时间（含偏移）",""),new("note","备注","",Multiline:true)],"例：2026-10-13T08:00+08:00。跨日自动分配，已知重叠会阻止保存。",v=>{Interval(v,Model.ZoneId);return Required(v,"subject")??CheckChapter(v);});
        if(v==null)return;try{await Model.FlushTimerAsync();await Model.RunAsync(()=>Model.Database.SaveRecord(new(Id(),v["subject"],Null(v["chapter"]),SessionSource.Manual,v["note"],Interval(v,Model.ZoneId)),"追加明确时间区间"),"区间记录已追加");}catch(Exception exception){Model.Status=exception.Message;}
    }
    private async void EditRecord_Click(object sender,RoutedEventArgs e)
    {
        var record=((RecordRow)((Button)sender).Tag).Record;
        if(Model.TimerActive&&record.Id==Model.Timer.SessionId){MessageBox.Show(this,"请先结束活动计时，再修正这条记录。","研迹");return;}
        var allocation=string.Join('\n',record.Parts.Select(p=>p.Date).Distinct().Select(d=>$"{d:yyyy-MM-dd} = "));
        var v=Edit("每日记录 · 查看与修正",[new("subject","科目 *",record.SubjectId,SubjectChoices()),new("chapter","主要章节（可选）",record.ChapterId??"",ChapterChoices()),new("note","备注",record.Note,Multiline:true),new("mode","时间修正方式","keep",[new("keep","保留原有时间片段"),new("duration","改为按日期分配的纯时长"),new("interval","改为明确起止时间")]),new("allocation","日期 = 分钟（替换时请重新明确填写）",allocation,Multiline:true),new("start","开始时间（仅区间模式；含偏移）"),new("end","结束时间（仅区间模式；含偏移）")],$"这是修改已有记录，不会追加一条。整次 {MainViewModel.Duration(record.DurationMs)}；受影响日期：{string.Join("、",record.Parts.Select(p=>p.Date).Distinct())}。原始片段保留在历史中。",v=>{if(v["mode"]=="duration")Allocations(v["allocation"],Model.ZoneId);if(v["mode"]=="interval")Interval(v,Model.ZoneId);return Required(v,"subject")??CheckChapter(v);});
        if(v==null)return;try{await Model.FlushTimerAsync();await Model.RunAsync(()=>Model.Database.SaveRecord(record with{SubjectId=v["subject"],ChapterId=Null(v["chapter"]),Note=v["note"],Parts=v["mode"]=="keep"?record.Parts:v["mode"]=="duration"?Allocations(v["allocation"],Model.ZoneId):Interval(v,Model.ZoneId)},"用户修正已有记录"),"记录已修正，统计已更新");}catch(Exception exception){Model.Status=exception.Message;}
    }
    private void History_Click(object sender,RoutedEventArgs e)
    {
        var record=((RecordRow)((Button)sender).Tag).Record;ShowText("记录历史",string.Join("\n\n",Model.Database.History(record.Id).Select(r=>$"{r.At.ToLocalTime():yyyy-MM-dd HH:mm:ss} · 版本 {r.Version} · {r.Reason}\n修改前：\n{Describe(r.BeforeJson)}\n修改后：\n{Describe(r.AfterJson)}")));
    }
    private string Describe(string? text)
    {
        if(text==null)return "（无）";var record=JsonSerializer.Deserialize<StudyRecord>(text)!;
        var subject=Model.AllSubjects.FirstOrDefault(s=>s.Id==record.SubjectId)?.Name??record.SubjectId;
        var chapter=Model.AllChapters.FirstOrDefault(c=>c.Id==record.ChapterId)?.Name??"未指定章节";
        var lines=record.Parts.Select(p=>$"{p.Date:yyyy-MM-dd} · {MainViewModel.Duration(p.DurationMs)}"+(p.Kind==PartKind.DurationOnly?"（纯时长）":$" · {TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeMilliseconds(p.StartUtcMs!.Value),TimeParts.ResolveZone(p.ZoneId)):HH:mm:ss} — {TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeMilliseconds(p.EndUtcMs!.Value),TimeParts.ResolveZone(p.ZoneId)):HH:mm:ss}"));
        return $"科目：{subject} · 章节：{chapter}\n来源：{(record.Source==SessionSource.Timer?"计时":"补录")} · 状态：{(record.Deleted?"已删除":"有效")}\n总时长：{MainViewModel.Duration(record.DurationMs)}\n{string.Join('\n',lines)}\n备注：{record.Note}";
    }
    private void ShowText(string title,string text){var dialog=new Window{Owner=this,Title=title,Width=700,Height=600,WindowStartupLocation=WindowStartupLocation.CenterOwner};dialog.Content=new TextBox{Text=text,IsReadOnly=true,TextWrapping=TextWrapping.Wrap,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Margin=new Thickness(24)};dialog.ShowDialog();}
    private async void ReviewAdd_Click(object sender,RoutedEventArgs e)
    {
        var v=Edit("核对未知时间",[new("allocation","明确日期 = 实际学习分钟（每行一段）",$"{Model.LocalToday():yyyy-MM-dd} = ",Multiline:true)],"仅填写你确认学过的时间。未知间隙不会自动计入；跨日时请分别填写。",v=>{Allocations(v["allocation"],Model.Timer.ZoneId);return null;});
        if(v!=null)await Model.ResolveAsync(Allocations(v["allocation"],Model.Timer.ZoneId));
    }
    private async void Preferences_Click(object sender,RoutedEventArgs e)
    {
        var v=Edit("考试档案与偏好",[new("date","考试日期（可空，yyyy-MM-dd）",Model.Database.Setting("exam-date","")),new("zone","统计时区",Model.ZoneId,new[]{"Asia/Shanghai","UTC","Asia/Tokyo","America/New_York","Europe/London"}.Select(z=>new Choice(z,z)).ToList()),new("checkpoint","检查点间隔（秒，实验参数）",Model.Database.Setting("checkpoint-seconds","15"),[new("2","2 秒"),new("15","15 秒"),new("60","60 秒")])],"2028 指入学年份。时区变化不重分配历史日期，也不改变当前计时的时区。",v=>{Date(v["date"]);TimeParts.ResolveZone(v["zone"]);return null;});
        if(v!=null)await Model.RunAsync(()=>{Model.Database.SaveSetting("exam-date",v["date"]);Model.Database.SaveSetting("zone",v["zone"]);Model.Database.SaveSetting("checkpoint-seconds",v["checkpoint"]);},"偏好已保存");
    }
    private string? OutputFile(string filter,string extension)
    {var dialog=new SaveFileDialog{Filter=filter,FileName="YanJi-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+extension,OverwritePrompt=true};return dialog.ShowDialog(this)==true?dialog.FileName:null;}
    private string? BackupFile(){var dialog=new OpenFileDialog{Filter="研迹完整备份|*.yanjibak"};return dialog.ShowDialog(this)==true?dialog.FileName:null;}
    private async void Backup_Click(object sender,RoutedEventArgs e){var path=OutputFile("研迹完整备份|*.yanjibak",".yanjibak");if(path==null)return;try{await Model.FlushTimerAsync();await Model.RunAsync(()=>Model.Database.Backup(path),"完整备份已导出并校验",false);}catch(Exception exception){Model.Status=exception.Message;}}
    private async void ValidateBackup_Click(object sender,RoutedEventArgs e){var path=BackupFile();if(path!=null)await Model.RunAsync(()=>Model.Database.ValidateBackup(path),"备份结构、数据和完整性校验通过",false);}
    private async void RestoreBackup_Click(object sender,RoutedEventArgs e)
    {if(Model.TimerActive){MessageBox.Show(this,"请先结束活动计时，再恢复备份。","研迹");return;}var path=BackupFile();if(path==null)return;
        if(MessageBox.Show(this,"整体恢复会替换当前数据，不进行合并。当前数据会先完整备份。确认恢复？","恢复完整备份",MessageBoxButton.OKCancel,MessageBoxImage.Question)!=MessageBoxResult.OK)return;
        if(!await Model.SaveReflectionAsync())return;if(await Model.RunAsync(()=>Model.Database.RestoreBackup(path),"完整备份已恢复"))await Model.InitializeAsync();}
    private async void Export_Click(object sender,RoutedEventArgs e){var path=OutputFile("CSV 文件|*.csv",".csv");if(path!=null)await Model.RunAsync(()=>Model.Database.ExportCsv(path),"CSV 已导出",false);}
    private async void Integrity_Click(object sender,RoutedEventArgs e)=>await Model.RunAsync(()=>{if(Model.Database.Integrity()!="ok")throw new InvalidDataException("数据库检查失败。");},"数据库完整性检查通过",false);
    public void SmokeClose(){_exiting=true;Close();}
}
