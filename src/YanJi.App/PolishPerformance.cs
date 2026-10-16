using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using YanJi.Core;
namespace YanJi.App;
internal static class PolishPerformance
{
    private static string MiniDigits(DependencyObject node)
    {
        for(int i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)
        {var child=VisualTreeHelper.GetChild(node,i);if(child is TextBlock text&&System.Windows.Data.BindingOperations.GetBinding(text,TextBlock.TextProperty)?.Path.Path=="TimerText")return text.Text;var result=MiniDigits(child);if(result.Length>0)return result;}
        return "";
    }
    internal static async Task RunAsync(MainWindow window,string output)
    {
        bool synchronized=true;Directory.CreateDirectory(output);var m=window.Model;var d=m.Database;var app=(App)Application.Current;
        void Write(string file,object value)=>File.WriteAllText(Path.Combine(output,file),JsonSerializer.Serialize(value,new JsonSerializerOptions{WriteIndented=true}));
        List<object> heartbeat=[],samples=[],actions=[];var elapsed=new Stopwatch();long last=0;DispatcherTimer? pulse=null;
        try
        {
            await window.ApplySimplePresetsAsync(false);var date=m.LocalToday();
            await Task.Run(()=>{for(int i=0;i<300;i++)d.SaveRecord(new("perf-record-"+i,new[]{"math","english","408"}[i%3],null,SessionSource.Manual,"synthetic history",[TimeParts.Duration(date.AddDays(-1-i%60),TimeSpan.FromMinutes(20+i%40),"Asia/Shanghai")]),"isolated performance fixture");});m.Refresh();
            window.Width=1200;window.Height=800;m.FocusModeIndex=0;window.ShowMiniTimer();await m.StartCommand.ExecuteAsync(null);
            if(!m.TimerRunning)throw new InvalidOperationException(m.Status);
            var ui=Environment.CurrentManagedThreadId;elapsed.Start();var start=Stopwatch.GetTimestamp();last=start;PerfTrace.Events.Clear();PerfTrace.Enabled=true;
            pulse=new DispatcherTimer(DispatcherPriority.Input){Interval=TimeSpan.FromMilliseconds(20)};
            pulse.Tick+=(_,_)=>{var now=Stopwatch.GetTimestamp();heartbeat.Add(new{AtMs=elapsed.Elapsed.TotalMilliseconds,GapMs=(now-last)*1000d/Stopwatch.Frequency,Thread=Environment.CurrentManagedThreadId,Page=m.Page});last=now;};pulse.Start();
            int phase=0;bool paused=false;
            while(elapsed.Elapsed.TotalSeconds<125)
            {
                await Task.Delay(1000);var main=window.TimerDigits.Text;var mini=MiniDigits(window.MiniTimer!);synchronized&=main==mini;samples.Add(new{Utc=DateTimeOffset.UtcNow,AtMs=elapsed.Elapsed.TotalMilliseconds,Main=main,Mini=mini,ProjectedTodayMs=m.TodayDurationMs,VisibleRows=m.TodayRecords.Count,RawMs=m.Timer.DurationMs,State=m.Timer.State,Page=m.Page});
                if(!paused&&elapsed.Elapsed.TotalSeconds>=30)
                {var watch=Stopwatch.StartNew();await m.PauseResumeCommand.ExecuteAsync(null);actions.Add(new{Name="pause",Ms=watch.Elapsed.TotalMilliseconds,State=m.Timer.State});var raw=m.Timer.DurationMs;await Task.Delay(300);if(m.Timer.DurationMs!=raw)throw new InvalidOperationException("Pause gained time");watch.Restart();await m.PauseResumeCommand.ExecuteAsync(null);actions.Add(new{Name="resume",Ms=watch.Elapsed.TotalMilliseconds,State=m.Timer.State});paused=true;}
                if(phase==0&&elapsed.Elapsed.TotalSeconds>=42){phase=1;m.Page=2;window.KnowledgeExpander.IsExpanded=true;await window.SimpleProgressView.RefreshAsync();if(window.KnowledgeView.Model is {} k)await k.ReloadAsync(null);}
                if(phase==1&&elapsed.Elapsed.TotalSeconds>=84){phase=2;m.Page=4;m.StatsRange("30");}
            }
            PerfTrace.Enabled=false;pulse.Stop();var spans=PerfTrace.Events.ToArray();var watchEnd=Stopwatch.StartNew();await m.FinishCommand.ExecuteAsync(null);actions.Add(new{Name="finish",Ms=watchEnd.Elapsed.TotalMilliseconds,State=m.Timer.State});
            var exact=d.Records().Single(r=>r.Id==m.Timer.SessionId).DurationMs==m.Timer.DurationMs;
            Write("spans.json",spans);Write("heartbeat.json",heartbeat);Write("samples.json",samples);Write("actions.json",actions);
            Write("result.json",new{Status=exact&&!m.TimerActive&&synchronized&&m.TodayDurationMs==m.Timer.DurationMs?"PASS":"FAIL",ObservedMs=elapsed.Elapsed.TotalMilliseconds,StartTimestamp=start,Frequency=Stopwatch.Frequency,UiThread=ui,CheckpointSeconds=d.Setting("checkpoint-seconds","15"),RawMs=m.Timer.DurationMs,ExactSavedMilliseconds=exact,MainMiniSynchronized=synchronized,ProjectedFinalMs=m.TodayDurationMs,HistoryRecords=300,Nodes=d.KnowledgeNodes().Count,HeartbeatSamples=heartbeat.Count,TimerSamples=samples.Count,Assembly=Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,BinarySha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(App).Assembly.Location))),Dpi=VisualTreeHelper.GetDpi(window).PixelsPerInchX,RenderMode=System.Windows.Media.RenderOptions.ProcessRenderMode.ToString(),GC=new{Gen0=GC.CollectionCount(0),Gen1=GC.CollectionCount(1),Gen2=GC.CollectionCount(2)},PeakWorkingSet=Process.GetCurrentProcess().PeakWorkingSet64});app.ValidationFailed=!exact||!synchronized||m.TodayDurationMs!=m.Timer.DurationMs;
        }
        catch(Exception ex){Write("failure.json",new{Error=ex.ToString()});app.ValidationFailed=true;}
        finally{PerfTrace.Enabled=false;pulse?.Stop();window.SmokeClose();}
    }
}
