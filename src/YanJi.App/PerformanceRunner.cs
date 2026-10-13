using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace YanJi.App;
public static class PerformanceRunner
{
    public static async Task RunAsync(MainWindow window,string output)
    {
        var vm=window.Model;var rows=new List<object>();
        foreach(var interval in new[]{0,2,15,60})
        {
            if(interval>0){vm.Database.SaveSetting("checkpoint-seconds",interval.ToString());await vm.StartCommand.ExecuteAsync(null);if(!vm.TimerRunning)throw new Exception("Performance timer start failed.");}
            using var process=Process.GetCurrentProcess();process.Refresh();var cpu=process.TotalProcessorTime;var startVersion=vm.Timer.Version;
            var startSaves=vm.TimerSaveCount;var startSaveMs=vm.TimerSaveTotalMilliseconds;
            var clock=Stopwatch.StartNew();long peakWorking=0,peakPrivate=0,peakWal=0;var interrupted=false;
            while(clock.Elapsed.TotalSeconds<(interval==60?62:60))
            {
                await Task.Delay(1000);process.Refresh();peakWorking=Math.Max(peakWorking,process.WorkingSet64);peakPrivate=Math.Max(peakPrivate,process.PrivateMemorySize64);
                // Short-lived connections can remove WAL between existence and length sampling.
                try{peakWal=Math.Max(peakWal,new FileInfo(vm.Database.DatabasePath+"-wal").Length);}
                catch(FileNotFoundException){}catch(DirectoryNotFoundException){}
                if(interval>0&&!vm.TimerRunning){interrupted=true;break;}
            }
            var elapsed=clock.Elapsed.TotalSeconds;var cpuSeconds=(process.TotalProcessorTime-cpu).TotalSeconds;
            rows.Add(new{Status=interrupted?"INTERRUPTED":"PASS",Mode=interval==0?"idle":"timer",CheckpointSeconds=interval,SampleSeconds=elapsed,CpuSeconds=cpuSeconds,
                CpuPercentOneCore=cpuSeconds/elapsed*100,CpuPercentMachine=cpuSeconds/elapsed/Environment.ProcessorCount*100,
                PeakWorkingSetBytes=peakWorking,PeakPrivateBytes=peakPrivate,PeakWalBytes=peakWal,
                SavedCheckpoints=interval==0?0:vm.Timer.Version-startVersion,EffectiveMs=interval==0?0:vm.Timer.DurationMs,
                SuccessfulTimerSaves=vm.TimerSaveCount-startSaves,TimerSaveTotalMilliseconds=vm.TimerSaveTotalMilliseconds-startSaveMs,
                MeanTimerSaveMilliseconds=vm.TimerSaveCount==startSaves?0:(vm.TimerSaveTotalMilliseconds-startSaveMs)/(vm.TimerSaveCount-startSaves),
                DatabaseBytes=new FileInfo(vm.Database.DatabasePath).Length,TimerState=vm.Timer.State.ToString()});
            File.WriteAllText(Path.Combine(output,"performance.json"),JsonSerializer.Serialize(rows,new JsonSerializerOptions{WriteIndented=true}));
            if(interrupted)throw new InvalidOperationException("Continuous timer resource sample was interrupted in state "+vm.Timer.State+"; not a PASS. Reason: "+vm.ReviewReason);
            if(interval>0){await vm.FinishCommand.ExecuteAsync(null);if(vm.TimerActive)throw new Exception("Performance timer did not finish.");}
        }
    }
}
