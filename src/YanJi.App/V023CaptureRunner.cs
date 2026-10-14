using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using YanJi.Core;
namespace YanJi.App;
internal static class V023CaptureRunner
{
    internal static async Task RunAsync(MainWindow w,string output)
    {
        Directory.CreateDirectory(output);var shots=new List<object>();var checks=new List<object>();var notTested=new List<string>();
        try
        {
            if(Directory.EnumerateFileSystemEntries(output).Any())throw new InvalidOperationException("Fresh evidence required");
            var vm=w.Model;var day=vm.LocalToday();var asm=typeof(App).Assembly;
            string Hash(string path)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
            void Write(string name,object value)=>File.WriteAllText(Path.Combine(output,name),JsonSerializer.Serialize(value,new JsonSerializerOptions{WriteIndented=true}));
            Write("provenance.json",new{Synthetic=true,Anchor=day,ProcessId=Environment.ProcessId,Executable=Environment.ProcessPath,ExeSha256=Hash(Environment.ProcessPath!),Source=asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,AssemblySha256=Hash(asm.Location)});
            await V023NativeRunner.RunAsync(w,(success,name)=>{checks.Add(new{Name=name,Status=success?"PASS":"FAIL"});if(!success)throw new InvalidOperationException(name);});
            Write("native-checks.json",checks);
            async Task Shot(string name,FrameworkElement? popup=null)
            {
                w.UpdateLayout();await Task.Delay(100);var file=name+".png";var path=Path.Combine(output,file);var element=popup??w;
                if(popup==null)NativeCapture.Save(w,path);else NativeCapture.SavePopup(popup,path);
                using var png=new System.Drawing.Bitmap(path);var dpi=VisualTreeHelper.GetDpi(element);var handle=popup==null?new WindowInteropHelper(w).Handle:(PresentationSource.FromVisual(popup) as HwndSource)!.Handle;
                shots.Add(new{File=file,Sha256=Hash(path),Bytes=new FileInfo(path).Length,PixelWidth=png.Width,PixelHeight=png.Height,DpiX=dpi.PixelsPerInchX,DpiY=dpi.PixelsPerInchY,OwnedHwnd=handle.ToInt64(),OwnerProcessId=Environment.ProcessId,ObservedAt=DateTimeOffset.UtcNow,WindowWidth=element.ActualWidth,WindowHeight=element.ActualHeight,Page=vm.Page,StatsView=vm.StatsView,Dark=vm.DarkTheme,From=vm.StatsFrom,To=vm.StatsTo,Subject=vm.StatsSubjectId,ScopeRawMs=vm.Analysis?.DurationMs,Subjects=vm.Analysis?.Subjects,Carry=vm.Analysis?.SubjectDisplayCarrySeconds});
            }
            var subjects=new[]{"math","english","408","politics"};
            for(var i=0;i<30;i++)vm.Database.SaveRecord(new("review023-"+i,subjects[i%4],null,SessionSource.Manual,"隔离验证样本",[new("review023-p-"+i,day.AddDays(-i),(25+(i*17)%110)*60000L+1234,"Asia/Shanghai",PartKind.DurationOnly)]),"isolated capture");
            foreach(var x in new[]{("tiny",1L),("short",999L),("seconds",61001L)})vm.Database.SaveRecord(new(x.Item1,"math",null,SessionSource.Manual,"显示边界",[new(x.Item1+"-p",day,x.Item2,"Asia/Shanghai",PartKind.DurationOnly)]),"isolated boundary");
            var start=new DateTimeOffset(day.AddDays(-1).ToDateTime(new TimeOnly(23,50)),TimeSpan.FromHours(8));vm.Database.SaveRecord(new("review023-cross","english",null,SessionSource.Manual,"跨日有效片段",TimeParts.Interval(start,start.AddMinutes(30),"Asia/Shanghai")),"isolated interval");
            vm.Database.SaveSetting("rest-day:"+day.ToString("yyyy-MM-dd"),"true");
            vm.Database.SaveTask(new("review023-task","复习概念边界",day,false,"math"));vm.Database.SaveWeakPoint(new("review023-weak","概念适用条件","需核对实际错因","概念","math",null,WeakPointState.Open,day));
            vm.Refresh();vm.Page=4;vm.StatsFrom=day.AddDays(-29).ToDateTime(TimeOnly.MinValue);vm.StatsTo=day.ToDateTime(TimeOnly.MinValue);
            Write("reconciliation.json",vm.Analysis!);
            foreach(var dark in new[]{false,true}){vm.DarkTheme=dark;w.Width=1200;w.Height=800;vm.StatsView=0;w.MainScroll.ScrollToTop();w.SubjectDonut.BringIntoView();await Shot("overview-"+(dark?"dark":"light"));}
            foreach(var dark in new[]{false,true}){vm.DarkTheme=dark;w.Width=900;w.Height=600;vm.StatsFrom=day.ToDateTime(TimeOnly.MinValue);w.StatsHeatmapChart.BringIntoView();await Shot("single-day-"+(dark?"dark":"light"));}
            vm.DarkTheme=false;vm.StatsFrom=day.AddDays(-6).ToDateTime(TimeOnly.MinValue);w.StatsHeatmapChart.BringIntoView();await Shot("week-light");
            vm.DarkTheme=true;w.Width=1200;w.Height=800;vm.StatsFrom=day.AddDays(-13).ToDateTime(TimeOnly.MinValue);w.StatsHeatmapChart.BringIntoView();await Shot("fortnight-dark");
            vm.StatsFrom=day.AddDays(-29).ToDateTime(TimeOnly.MinValue);vm.StatsView=2;
            foreach(var dark in new[]{false,true}){vm.DarkTheme=dark;w.Width=dark?900:1200;w.Height=dark?600:800;w.MainScroll.ScrollToTop();await Shot("review-"+(dark?"dark":"light"));}
            vm.Database.SaveSubject(new("tiny-only","极短时长"));vm.Database.SaveRecord(new("only-one-ms","tiny-only",null,SessionSource.Manual,"真实非零",[new("only-one-ms-p",day,1,"UTC",PartKind.DurationOnly)]),"isolated tiny");vm.Refresh();vm.StatsSubjectId="tiny-only";vm.StatsFrom=day.ToDateTime(TimeOnly.MinValue);vm.StatsView=0;
            foreach(var dark in new[]{false,true}){vm.DarkTheme=dark;w.Width=900;w.Height=600;w.SubjectDonut.BringIntoView();await Shot("tiny-"+(dark?"dark":"light"));}
            NativeCapture.PointerRestore? pointer=null;
            try{pointer=NativeCapture.MoveToOwned(w.SubjectDonut,w.SubjectDonut.FirstHitCenter);await Task.Delay(200);if(!w.SubjectDonut.HoverTooltip.IsOpen)throw new InvalidOperationException("Actual hover not observed");await Shot("tiny-hover-popup",w.SubjectDonut.HoverTooltip);}
            catch(NativeHoverUnavailableException e){notTested.Add(e.Message);}
            finally{if(pointer!=null)NativeCapture.RestoreCursor(pointer);}
            vm.StatsTo=day.AddDays(3).ToDateTime(TimeOnly.MinValue);vm.StatsFrom=vm.StatsTo;vm.DarkTheme=false;w.StatsHeatmapChart.BringIntoView();await Shot("empty-single-day");
            Write("screenshots.json",shots);Write("result.json",new{Status=notTested.Count==0?"PASS":"PASS_WITH_LIMITATIONS",Screenshots=shots.Count,Checks=checks.Count,NotTested=notTested,Integrity=vm.Database.Integrity()});
        }
        catch(Exception e){((App)Application.Current).ValidationFailed=true;File.WriteAllText(Path.Combine(output,"failure.txt"),e.ToString());File.WriteAllText(Path.Combine(output,"native-checks.json"),JsonSerializer.Serialize(checks));File.WriteAllText(Path.Combine(output,"screenshots-partial.json"),JsonSerializer.Serialize(shots));}
        finally{w.SmokeClose();}
    }
}
