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
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using YanJi.Core;
namespace YanJi.App;
internal static class V03CaptureRunner
{
    private static string Hash(string path)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    private static IEnumerable<DependencyObject> Children(DependencyObject root)
    {for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var child=VisualTreeHelper.GetChild(root,i);yield return child;foreach(var x in Children(child))yield return x;}}
    internal static async Task RunAsync(MainWindow w,string output)
    {
        var vm=w.Model;var app=(App)Application.Current;var checks=new List<object>();var shots=new List<object>();
        void Write(string name,object data)=>File.WriteAllText(Path.Combine(output,name),JsonSerializer.Serialize(data,new JsonSerializerOptions{WriteIndented=true}));
        void Check(bool ok,string name){checks.Add(new{Name=name,Status=ok?"PASS":"FAIL"});Write("checks.json",checks);if(!ok)throw new InvalidOperationException(name);}
        async Task Idle(){while(vm.Busy)await Task.Delay(50);w.UpdateLayout();await Task.Delay(100);}
        async Task Shot(Window window,string name)
        {
            window.UpdateLayout();await Task.Delay(100);var path=Path.Combine(output,name+".png");NativeCapture.Save(window,path);using var png=new System.Drawing.Bitmap(path);var dpi=VisualTreeHelper.GetDpi(window);
            shots.Add(new{File=name+".png",Sha256=Hash(path),Bytes=new FileInfo(path).Length,Width=window.ActualWidth,Height=window.ActualHeight,PixelWidth=png.Width,PixelHeight=png.Height,Dpi=dpi.PixelsPerInchX,OwnedHwnd=new WindowInteropHelper(window).Handle.ToInt64(),ProcessId=Environment.ProcessId,Dark=vm.DarkTheme,Timer=vm.Timer,vm.BreakActive});Write("screenshots.json",shots);
        }
        async Task Invoke(Button button)
        {var peer=new ButtonAutomationPeer(button);((IInvokeProvider)peer.GetPattern(PatternInterface.Invoke)!).Invoke();await Task.Delay(150);await Idle();}
        try
        {
            var owned=Path.GetFullPath("artifacts")+Path.DirectorySeparatorChar;
            if(!vm.Database.DirectoryPath.StartsWith(owned,StringComparison.OrdinalIgnoreCase)||!output.StartsWith(owned,StringComparison.OrdinalIgnoreCase)||Directory.Exists(output)&&Directory.EnumerateFileSystemEntries(output).Any())throw new InvalidOperationException("Owned fresh output required");
            Directory.CreateDirectory(output);var asm=typeof(App).Assembly;
            Write("provenance.json",new{Synthetic=true,ProcessId=Environment.ProcessId,Source=asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion,Executable=Environment.ProcessPath,ExeSha256=Hash(Environment.ProcessPath!),AssemblySha256=Hash(asm.Location),Runtime=System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,OS=System.Runtime.InteropServices.RuntimeInformation.OSDescription});
            var marker=Path.Combine(vm.Database.DirectoryPath,"v03-isolated.fixture");
            if(app.V03Reopen)
            {
                Check(File.Exists(marker)&&File.ReadAllText(marker)=="YanJi V0.3 synthetic validation","Only marked isolated data is reopened");
                var records=vm.Database.Records();Check(records.Count>=3&&records.Any(r=>r.Focus is {Mode:FocusMode.Pomodoro}&&r.DurationMs==60000),"Actual new process reloads the completed 60-second Pomodoro");
                Check(vm.Timer.State==TimerState.Paused&&vm.Timer.Focus?.TaskId=="unassigned","Actual process restart restores paused task association without adding time");
                Write("reopen.json",new{RawTotalMs=records.Sum(r=>r.DurationMs),Records=records.Count,Active=vm.Timer,Integrity=vm.Database.Integrity()});
                await Shot(w,"restart-paused");Write("result.json",new{Status="PASS",Checks=checks.Count,Screenshots=shots.Count});return;
            }
            File.WriteAllText(marker,"YanJi V0.3 synthetic validation");
            if(app.V03Layout)
            {
                w.Width=1200;w.Height=800;await Shot(w,"empty-light");vm.DarkTheme=true;await Idle();w.Width=900;w.Height=600;await Shot(w,"empty-dark-small");
                var layoutDay=vm.LocalToday();vm.Database.SaveTask(new("layout-task","数学 · 极限与连续：完成基础练习，并记录一个仍需澄清的知识点",layoutDay,false,"math"));vm.Database.SaveTask(new("layout-long","长文本样本 · 先整理条件再做题，完成后核对错误原因和适用范围，不把投入时长自动当作已经掌握的证明。",layoutDay,false,"english"));
                var begin=DateTimeOffset.UtcNow.AddMinutes(-2);var sample=new ClockSample(begin,0,0,"synthetic-layout");var t=new TimerMachine();t.Start("math",null,"仅用于实际窗口布局验收","Asia/Shanghai",sample,new(FocusMode.Pomodoro,1500000,300000,"layout-task","数学 · 极限与连续：完成基础练习，并记录一个仍需澄清的知识点"));t.Pause(sample with{Utc=begin.AddMinutes(1),AwakeMs=60000,BootMs=60000});vm.Database.SaveTimer(t.Snapshot,"合成布局样本");await vm.InitializeAsync();
                foreach(var dark in new[]{false,true}){vm.DarkTheme=dark;await Idle();foreach(var size in new[]{(1200d,800d),(900d,600d)}){w.Width=size.Item1;w.Height=size.Item2;w.MainScroll.ScrollToTop();await Shot(w,"final-today-"+(dark?"dark":"light")+"-"+size.Item1);var button=Children(w).OfType<Button>().Single(b=>ReferenceEquals(b.Command,vm.PauseResumeCommand));var position=button.TranslatePoint(new Point(0,0),w.MainScroll);Check(button.IsVisible&&position.Y+button.ActualHeight<=w.MainScroll.ViewportHeight,"Pause action is visible without scrolling at "+size.Item1+" "+(dark?"dark":"light"));}}
                w.ShowMiniTimer();foreach(var dark in new[]{false,true}){vm.DarkTheme=dark;await Idle();await Shot(w.MiniTimer!,"final-mini-"+(dark?"dark":"light"));}
                Write("result.json",new{Status="PASS",Checks=checks.Count,Screenshots=shots.Count,Note="Synthetic paused timing fixture for final layout; not an elapsed-time test"});return;
            }
            var day=vm.LocalToday();vm.Database.SaveTask(new("focus-task","数学 · 完成极限与连续基础题，并记录仍不清楚的适用条件",day,false,"math"));vm.Database.SaveTask(new("future","明天再做",day.AddDays(1),false,"english"));vm.Database.SaveTask(new("done","已完成的任务",day,true,"408"));vm.Database.SaveTask(new("unassigned","英语阅读 · 核对长难句与生词，计划未指定科目",day));
            vm.Database.SaveSubject(new("archived-focus","归档样本",true));vm.Database.SaveTask(new("archived-task","归档科目任务",day,false,"archived-focus"));vm.Refresh();
            Check(vm.TodayTasks.Any(t=>t.Id=="focus-task")&&!vm.TodayTasks.Any(t=>t.Id is "future" or "done"),"Today tasks include due work and omit future/completed work");
            await vm.StartTaskCommand.ExecuteAsync(vm.Tasks.First(t=>t.Id=="archived-task"));Check(!vm.TimerActive&&vm.Status.Contains("归档"),"Archived subject task cannot start a timer");
            vm.FocusModeIndex=2;vm.FocusMinutes="1";vm.BreakMinutes="1";
            await vm.StartTaskCommand.ExecuteAsync(vm.Tasks.First(t=>t.Id=="focus-task"));await Idle();var id=vm.Timer.SessionId;
            Check(vm.TimerRunning&&vm.Timer.Focus is {Mode:FocusMode.Pomodoro,TaskId:"focus-task"}&&vm.Database.LoadTimer()?.Focus==vm.Timer.Focus,"Task start persists mode and stable task association atomically");
            foreach(var dark in new[]{false,true}){vm.DarkTheme=dark;await Idle();foreach(var size in new[]{(1200d,800d),(900d,600d)}){w.Width=size.Item1;w.Height=size.Item2;w.MainScroll.ScrollToTop();await Shot(w,"today-"+(dark?"dark":"light")+"-"+size.Item1);}}
            w.ShowMiniTimer();Check(ReferenceEquals(w.MiniTimer!.DataContext,vm),"Mini window shares the one existing ViewModel and timer");
            foreach(var dark in new[]{false,true}){vm.DarkTheme=dark;await Idle();await Shot(w.MiniTimer!,"mini-"+(dark?"dark":"light"));}
            var pause=Children(w.MiniTimer!).OfType<Button>().Single(b=>ReferenceEquals(b.Command,vm.PauseResumeCommand));await Invoke(pause);var paused=vm.Timer.DurationMs;await Task.Delay(1000);await vm.TickAsync();Check(vm.Timer.State==TimerState.Paused&&vm.Timer.DurationMs==paused,"Real WPF UIA pause command excludes paused Windows time");
            await Invoke(pause);Check(vm.TimerRunning&&vm.Timer.SessionId==id,"Mini UIA resume continues the same StudySession");
            w.Close();Check(!w.IsVisible&&w.MiniTimer!.IsVisible&&vm.TimerRunning,"Active main X hides to tray while mini and shared timer remain active");w.ShowFromTray();
            w.MiniTimer!.Close();Check(!w.MiniTimer.IsVisible&&vm.TimerRunning,"Closing mini hides it without stopping the timer");
            var watch=Stopwatch.StartNew();while(vm.TimerRunning&&watch.Elapsed.TotalSeconds<80)await Task.Delay(250);await Idle();
            Check(vm.Timer.State==TimerState.Completed&&vm.Timer.DurationMs==60000&&vm.Database.LoadTimer()==null&&vm.BreakActive,"Actual Windows countdown auto-saves exactly 60 seconds and starts non-study rest");
            Check(!vm.Database.Tasks().Single(t=>t.Id=="focus-task").Done,"Timer completion does not invent task completion");
            w.ShowMiniTimer();vm.DarkTheme=false;await Idle();await Shot(w.MiniTimer!,"mini-rest");w.Width=1200;w.Height=800;w.MainScroll.ScrollToTop();await Shot(w,"pomodoro-rest");
            var count=vm.Database.Records().Count;var total=vm.Database.Records().Sum(r=>r.DurationMs);await vm.PauseResumeCommand.ExecuteAsync(null);await Task.Delay(1000);await vm.TickAsync();Check(count==vm.Database.Records().Count&&total==vm.Database.Records().Sum(r=>r.DurationMs),"Break and break pause never create or increase study records");
            await vm.StartCommand.ExecuteAsync(null);Check(vm.TimerRunning&&vm.Timer.SessionId!=id&&!vm.BreakActive,"Next round is explicit and has a new study identity");await Task.Delay(1100);await vm.FinishCommand.ExecuteAsync(null);await vm.CompleteFocusTaskCommand.ExecuteAsync(null);Check(vm.Database.Tasks().Single(t=>t.Id=="focus-task").Done,"Task completion requires the explicit user command");
            vm.FocusModeIndex=0;await vm.StartTaskCommand.ExecuteAsync(vm.Tasks.First(t=>t.Id=="unassigned"));Check(vm.Timer.SubjectId=="math"&&vm.Status.Contains("未指定科目"),"Unassigned task uses the visible selected subject with explanation");await Task.Delay(1100);await vm.PauseResumeCommand.ExecuteAsync(null);
            var backup=Path.Combine(output,"synthetic.yanjibak");vm.Database.Backup(backup);vm.Database.ValidateBackup(backup);Check(vm.Database.Integrity()=="ok","Full backup semantic validation accepts the active task metadata");
            using(var process=Process.GetCurrentProcess()){process.Refresh();Write("resources.json",new{WorkingSetBytes=process.WorkingSet64,PrivateBytes=process.PrivateMemorySize64,Note="One populated live-window observation; not a performance benchmark"});}
            Write("data-summary.json",new{RawTotalMs=vm.Database.Records().Sum(r=>r.DurationMs),Records=vm.Database.Records().Count,Active=vm.Timer});
            Write("result.json",new{Status="PASS",Checks=checks.Count,Screenshots=shots.Count,NotTested=new[]{"Real sleep/hibernate/reboot/time changes", "Other actual display DPI, IME and physical keyboard input", "winapp ui engine unavailable; UIA provider invocation is not physical pointer input"}});
        }
        catch(Exception e){app.ValidationFailed=true;Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"failure.txt"),e.ToString());Write("checks.json",checks);}
        finally{w.SmokeClose();}
    }
}
