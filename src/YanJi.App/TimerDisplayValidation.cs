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
using System.Windows.Data;
using System.Windows.Media;
using YanJi.Core;
namespace YanJi.App;
internal static class TimerDisplayValidation
{
    static IEnumerable<DependencyObject> Children(DependencyObject root)
    {for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var c=VisualTreeHelper.GetChild(root,i);yield return c;foreach(var x in Children(c))yield return x;}}
    static string Text(Window w)=>Children(w).OfType<TextBlock>().Single(t=>BindingOperations.GetBinding(t,TextBlock.TextProperty)?.Path.Path=="TimerText").Text;
    static string Digits(long ms)=>$"{ms/3600000:00}:{ms/60000%60:00}:{ms/1000%60:00}";
    internal static async Task RunAsync(MainWindow w,string output,bool probe)
    {
        var vm=w.Model;var app=(App)Application.Current;var checks=new List<object>();var samples=new List<object>();var shots=new List<object>();int notifications=0;
        void Write(string file,object value)=>File.WriteAllText(Path.Combine(output,file),JsonSerializer.Serialize(value,new JsonSerializerOptions{WriteIndented=true}));
        bool failed=false;
        void Check(bool ok,string name){checks.Add(new{Name=name,Status=ok?"PASS":"FAIL"});failed|=!ok;Write("checks.json",checks);}
        long Shown()=>vm.BreakActive?throw new InvalidOperationException("No rest in this scenario"):vm.Timer.IsActive&&vm.Timer.Focus is {Mode:not FocusMode.Free} f?Math.Max(0,f.TargetMs-vm.Timer.DurationMs):vm.Timer.DurationMs;
        async Task Read(string name,bool assert=true)
        {
            await Task.Delay(80);w.UpdateLayout();w.MiniTimer!.UpdateLayout();var main=Text(w);var mini=Text(w.MiniTimer);var expected=Digits(Shown());
            samples.Add(new{Name=name,Utc=DateTimeOffset.UtcNow,RawEffectiveMs=vm.Timer.DurationMs,State=vm.Timer.State.ToString(),Mode=vm.Timer.Focus?.Mode.ToString(),vm.TimerText,Main=main,Mini=mini,Expected=expected,DispatcherEnabled=w.ValidationTickEnabled,TickCount=w.ValidationTickCount,PropertyNotifications=notifications,Minimized=w.WindowState==WindowState.Minimized,vm.Page});Write("samples.json",samples);
            if(assert){Check(main==expected&&mini==expected&&vm.TimerText==expected,name+": rendered main/mini equal trusted projection");Check(w.ValidationTickEnabled,name+": DispatcherTimer enabled");}
        }
        async Task Reach(long ms)
        {var wait=Stopwatch.StartNew();while(vm.Timer.DurationMs<ms&&vm.TimerRunning&&wait.ElapsedMilliseconds<ms+5000)await Task.Delay(50);if(vm.NeedsReview)throw new InvalidOperationException("INTERRUPTED: actual system notification requires review; not automatically resolved");if(vm.Timer.DurationMs<ms)throw new TimeoutException("UI timer did not reach "+ms);}
        async Task Shot(Window window,string name)
        {window.UpdateLayout();await Task.Delay(100);var path=Path.Combine(output,name+".png");NativeCapture.Save(window,path);shots.Add(new{File=name+".png",Sha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))),Bytes=new FileInfo(path).Length,Dpi=VisualTreeHelper.GetDpi(window).PixelsPerInchX});Write("screenshots.json",shots);}
        try
        {
            var root=Path.GetFullPath("artifacts")+Path.DirectorySeparatorChar;
            if(!Path.GetFullPath(output).StartsWith(root,StringComparison.OrdinalIgnoreCase)||!vm.Database.DirectoryPath.StartsWith(root,StringComparison.OrdinalIgnoreCase)||Directory.Exists(output)&&Directory.EnumerateFileSystemEntries(output).Any()||vm.Database.Records().Count!=0)throw new InvalidOperationException("Fresh isolated artifact paths required");
            Directory.CreateDirectory(output);var asm=typeof(App).Assembly;
            Write("provenance.json",new{Synthetic=true,Source=asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion,Executable=Environment.ProcessPath,ExeSha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Environment.ProcessPath!))),AssemblySha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(asm.Location))),OS=System.Runtime.InteropServices.RuntimeInformation.OSDescription,Runtime=System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,Probe=probe});
            vm.PropertyChanged+=(_,e)=>{if(e.PropertyName==nameof(vm.TimerText))notifications++;};w.ShowMiniTimer();
            foreach(var mode in probe?new[]{0}:new[]{0,1,2})
            {
                vm.FocusModeIndex=mode;vm.FocusMinutes="2";vm.BreakMinutes="1";await vm.StartCommand.ExecuteAsync(null);
                for(var second=1;second<=3;second++){await Reach(second*1000);await Read(((FocusMode)mode)+"-second-"+second);}
                Check(w.ValidationTickCount>=3&&vm.Timer.DurationMs>=3000,"Dispatcher ticks advance real trusted samples");await Shot(w,((FocusMode)mode)+"-running-main");await Shot(w.MiniTimer!,((FocusMode)mode)+"-running-mini");
                if(!probe)
                {
                    await vm.PauseResumeCommand.ExecuteAsync(null);var paused=vm.Timer.DurationMs;var frozen=vm.TimerText;await Task.Delay(3100);await Read(((FocusMode)mode)+"-paused");Check(vm.Timer.DurationMs==paused&&vm.TimerText==frozen,"Pause 3 seconds freezes both time facts and digits");
                    await vm.PauseResumeCommand.ExecuteAsync(null);await Reach(paused+1000);await Read(((FocusMode)mode)+"-resumed");
                    var before=vm.Timer.DurationMs;w.WindowState=WindowState.Minimized;await Reach(before+2000);await Read(((FocusMode)mode)+"-minimized");w.WindowState=WindowState.Normal;
                    vm.Page=4;before=vm.Timer.DurationMs;await Reach(before+2000);await Read(((FocusMode)mode)+"-other-page");vm.Page=0;await Read(((FocusMode)mode)+"-restored-page");
                    await Reach(65000);await Read(((FocusMode)mode)+"-over-65-seconds");vm.DarkTheme=mode==1;w.Width=mode==1?900:1200;w.Height=mode==1?600:800;await Shot(w,((FocusMode)mode)+"-65seconds");
                }
                await vm.FinishCommand.ExecuteAsync(null);var saved=vm.Database.Records().Single(r=>r.Id==vm.Timer.SessionId);Check(saved.DurationMs==vm.Timer.DurationMs&&saved.Parts.Sum(p=>p.DurationMs)==saved.DurationMs,"Finished raw timer equals saved effective parts");
                var day=vm.LocalToday();var records=vm.Database.Records();var analytics=StudyAnalytics.Build(records,vm.Database.Subjects(),day,day,day);Check(analytics.DurationMs==records.SelectMany(r=>r.Parts).Where(p=>p.Date==day).Sum(p=>p.DurationMs),"Statistics aggregate original effective milliseconds");
                await Read(((FocusMode)mode)+"-finished");
            }
            if(!probe)Check(notifications>=65,"TimerText property notifications continuously refresh seconds");
            Write("result.json",new{Status=failed?"FAIL":"PASS",Checks=checks.Count,Samples=samples.Count,Notifications=notifications,Note="Real Windows clock; no manual Tick calls, no system time changes. HWND captures and actual bound TextBlock reads. UI commands scripted, not physical input."});app.ValidationFailed=failed;
        }
        catch(Exception e){Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"failure.txt"),e.ToString());Write("result.json",new{Status=e.Message.StartsWith("INTERRUPTED")?"INTERRUPTED":"FAIL",Error=e.Message});app.ValidationFailed=true;}
        finally{w.SmokeClose();}
    }
}
