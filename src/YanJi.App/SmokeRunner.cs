using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Threading;
using Microsoft.Data.Sqlite;
using YanJi.Core;

namespace YanJi.App;
/// <summary>Owned-data WPF/VM integration evidence, not a substitute for real IME or power testing.</summary>
public static class SmokeRunner
{
    public static async Task RunAsync(MainWindow window,string output)
    {
        Directory.CreateDirectory(output);List<object> results=[];
        void Assert(bool success,string name){results.Add(new{Name=name,Status=success?"PASS":"FAIL"});if(!success)throw new InvalidOperationException(name);}
        try
        {
            var vm=window.Model;Assert(!vm.Busy&&vm.Subjects.Count>=4,"WPF startup and SQLite initialization");
            if(((App)Application.Current).StartupOnly)
            {window.UpdateLayout();await window.Dispatcher.InvokeAsync(()=>{},DispatcherPriority.ApplicationIdle);
                File.WriteAllText(Path.Combine(output,"startup.json"),JsonSerializer.Serialize(new{Status="PASS",UiReadyMs=((App)Application.Current).StartupClock.Elapsed.TotalMilliseconds,Runtime=System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,RuntimeDirectory=System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(),SQLite=vm.Database.Diagnostics(),ProcessId=Environment.ProcessId}));return;}
            if(((App)Application.Current).PerformanceOnly)
            {await PerformanceRunner.RunAsync(window,output);Assert(true,"Fresh self-contained process performance completed");return;}
            VisualUpgradeRunner.CaptureEmpty(window,output);
            vm.ManualMinutes="37";vm.ManualNote="中文输入验证：高等数学 · 极限";await vm.AddManualCommand.ExecuteAsync(null);
            Assert(vm.Database.Records().Any(r=>r.Note=="中文输入验证：高等数学 · 极限"&&r.DurationMs==2220000),"Manual command with Chinese note persisted");
            await vm.StartCommand.ExecuteAsync(null);Assert(vm.TimerRunning,"Start timer via command");await Task.Delay(1200);await vm.TickAsync();
            await vm.PauseResumeCommand.ExecuteAsync(null);var prefix=vm.Timer.DurationMs;await Task.Delay(1200);await vm.TickAsync();Assert(vm.Timer.DurationMs==prefix,"Pause excludes elapsed time in Windows process");
            await vm.PauseResumeCommand.ExecuteAsync(null);await Task.Delay(1100);await vm.FinishCommand.ExecuteAsync(null);Assert(!vm.TimerActive&&vm.Timer.DurationMs>=prefix+1000,"Resume and finish persist");
            vm.ManualChapter=null;
            vm.Database.SaveChapter(new("selected-chapter","math","选中章节",20,Mastery.Reviewing));vm.Refresh();
            vm.ManualChapter=vm.ManualChapters.First(c=>c.Id=="selected-chapter");vm.Refresh();
            Assert(vm.ManualChapter?.Id=="selected-chapter","Checkpoint refresh preserves manual chapter selection");
            vm.Reflection="未保存输入仍保留";vm.Refresh();Assert(vm.Reflection=="未保存输入仍保留","Refresh preserves unsaved reflection");await vm.SaveReflectionAsync();
            await vm.ChangeDayAsync(vm.LocalToday().ToDateTime(TimeOnly.MinValue));vm.Reflection="今天完成了复习，明天继续。";await vm.SaveReflectionAsync();Assert(vm.Database.Reflection(vm.LocalToday())=="今天完成了复习，明天继续。","Chinese reflection persistence");
            vm.Database.SaveChapter(new("smoke-chapter","math","函数与极限",60,Mastery.Weak));vm.Database.SaveTask(new("smoke-task","复习极限",vm.LocalToday(),false,"math"));
            vm.Database.SaveWeakPoint(new("smoke-weak","等价无穷小","复习使用条件","概念理解","math","smoke-chapter",WeakPointState.Reviewing,vm.LocalToday().AddDays(2)));vm.Refresh();
            for(var page=0;page<6;page++)
            {vm.Page=page;window.UpdateLayout();await Task.Delay(150);Capture(window,Path.Combine(output,$"page-{page}.png"));Assert(window.ActualWidth>=900&&window.ActualHeight>=600,$"Page {page} layout rendered");}
            window.Width=900;window.Height=600;vm.Page=0;window.UpdateLayout();await Task.Delay(150);Capture(window,Path.Combine(output,"minimum-900x600.png"));
            Assert(window.ActualWidth==900&&window.ActualHeight==600,"Minimum window renders");
            foreach(var scale in new[]{1.25,1.5,2.0})Capture(window,Path.Combine(output,$"render-scale-{scale.ToString(System.Globalization.CultureInfo.InvariantCulture)}.png"),scale);
            Assert(true,"125/150/200 percent bitmap render variants (not system DPI changes)");
            await vm.StartCommand.ExecuteAsync(null);window.Close();Assert(!window.IsVisible&&vm.TimerRunning,"X hides running window and keeps timer");window.ShowFromTray();await vm.FinishCommand.ExecuteAsync(null);
            window.Hide();var secondStart=new ProcessStartInfo(Environment.ProcessPath!){UseShellExecute=false,CreateNoWindow=true};
            if(string.Equals(Path.GetFileNameWithoutExtension(Environment.ProcessPath),"dotnet",StringComparison.OrdinalIgnoreCase))secondStart.ArgumentList.Add(typeof(App).Assembly.Location);
            secondStart.ArgumentList.Add("--data-dir");secondStart.ArgumentList.Add(vm.Database.DirectoryPath);
            using(var second=Process.Start(secondStart)!)
            {var finished=await Task.WhenAny(second.WaitForExitAsync(),Task.Delay(5000));if(!second.HasExited){second.Kill();throw new InvalidOperationException("Owned second instance did not exit.");}await Task.Delay(200);Assert(second.ExitCode==0&&window.IsVisible,"Actual second process activates first instance and exits");}
            vm.TimerSubject=vm.Subjects.First(s=>s.Id=="english");vm.TimerNote="恢复时显示正确科目";await vm.StartCommand.ExecuteAsync(null);await vm.InitializeAsync();
            Assert(vm.TimerSubject?.Id=="english"&&vm.TimerNote=="恢复时显示正确科目"&&vm.NeedsReview,"Restart displays recovered timer metadata");await vm.ResolveAsync([]);await vm.FinishCommand.ExecuteAsync(null);
            await vm.StartCommand.ExecuteAsync(null);await Task.Delay(1100);
            using(var c=new SqliteConnection($"Data Source={vm.Database.DatabasePath};Pooling=False"))
            {c.Open();using var cmd=c.CreateCommand();cmd.CommandText="CREATE TRIGGER smoke_fail BEFORE INSERT ON parts BEGIN SELECT RAISE(ABORT,'expected smoke write failure'); END";cmd.ExecuteNonQuery();}
            Assert(!await vm.PrepareExitAsync(false)&&window.IsVisible&&vm.NeedsReview,"Failed exit persistence leaves application open with review");
            using(var c=new SqliteConnection($"Data Source={vm.Database.DatabasePath};Pooling=False"))
            {c.Open();using var cmd=c.CreateCommand();cmd.CommandText="DROP TRIGGER smoke_fail";cmd.ExecuteNonQuery();}
            await vm.ResolveAsync([]);Assert(await vm.PrepareExitAsync(true)&&!vm.TimerActive,"Successful exit preparation persists final state");
            var editor=new EditorDialog(window,"键盘与中文文本验证",[new("text","文本 *","中文测试")]);
            editor.Loaded+=(_,_)=>editor.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,new Action(()=>
            {
                var button=Descendants(editor).OfType<Button>().First(b=>b.IsDefault);
                var textbox=Descendants(editor).OfType<TextBox>().First();Keyboard.Focus(textbox);
                Assert(textbox.IsKeyboardFocused,"WPF text field receives keyboard focus");
                Assert(textbox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next)),"WPF next-focus traversal works");
                ((IInvokeProvider)new ButtonAutomationPeer(button).GetPattern(PatternInterface.Invoke)!).Invoke();
            }));
            Assert(editor.ShowDialog()==true&&editor.Values?["text"]=="中文测试","Actual editor default-save action preserves Chinese text");
            var backup=Path.Combine(output,"smoke.yanjibak");vm.Database.Backup(backup);vm.Database.ValidateBackup(backup);vm.Database.ExportCsv(Path.Combine(output,"smoke.csv"));Assert(File.Exists(backup),"UI process backup and CSV");
            await VisualUpgradeRunner.RunAsync(window,output,Assert);
            var dpi=VisualTreeHelper.GetDpi(window);using var process=Process.GetCurrentProcess();
            File.WriteAllText(Path.Combine(output,"environment.json"),JsonSerializer.Serialize(new{Runtime=System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,RuntimeDirectory=System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(),OS=Environment.OSVersion.ToString(),ClockSources=WindowsClock.Sources,SQLite=vm.Database.Diagnostics(),DpiX=dpi.PixelsPerInchX,DpiY=dpi.PixelsPerInchY,WorkingSet=process.WorkingSet64,PrivateBytes=process.PrivateMemorySize64,DataDirectory=vm.Database.DirectoryPath},new JsonSerializerOptions{WriteIndented=true}));
            if(((App)Application.Current).Measure)await PerformanceRunner.RunAsync(window,output);
        }
        catch(Exception exception){((App)Application.Current).ValidationFailed=true;results.Add(new{Name="Unhandled smoke failure",Status="FAIL",Details=exception.ToString()});}
        finally{File.WriteAllText(Path.Combine(output,"results.json"),JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));window.SmokeClose();}
    }
    internal static void Capture(Window window,string path,double scale=1)
    {
        var bitmap=new RenderTargetBitmap((int)(window.ActualWidth*scale),(int)(window.ActualHeight*scale),96*scale,96*scale,PixelFormats.Pbgra32);bitmap.Render(window);
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var stream=new FileStream(path,FileMode.CreateNew);encoder.Save(stream);
    }
    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {for(var i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++){var child=VisualTreeHelper.GetChild(parent,i);yield return child;foreach(var nested in Descendants(child))yield return nested;}}
}
