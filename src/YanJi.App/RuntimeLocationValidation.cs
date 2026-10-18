using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace YanJi.App;

/// <summary>Explicit --test-mode only; new or this probe's own previously marked database.</summary>
internal static class RuntimeLocationValidation
{
    internal static async Task RunAsync(MainWindow window,string output)
    {
        var app=(App)Application.Current;var m=window.Model;var d=m.Database;
        Directory.CreateDirectory(output);var checks=new List<string>();
        void Check(bool condition,string label){if(!condition)throw new InvalidOperationException(label);checks.Add(label);}
        void Write(string name,object data)=>File.WriteAllText(Path.Combine(output,name),JsonSerializer.Serialize(data,new JsonSerializerOptions{WriteIndented=true}));
        async Task Shot(Window w,string name){w.UpdateLayout();await Task.Delay(200);NativeCapture.Save(w,Path.Combine(output,name+".png"));}
        try
        {
            Check(app.TestMode&&window.Title.Contains("隔离测试")&&window.VersionHint.Text.Contains("隔离测试"),"test mode labelled in actual main window");
            Check(d.DirectoryPath==Path.GetFullPath(Environment.GetCommandLineArgs()[Array.IndexOf(Environment.GetCommandLineArgs(),"--data-dir")+1]),"actual SQLite uses explicit path");
            // Formal resolution is tested with a synthetic Windows user root, never the real user directory.
            var fakeLocal=Path.Combine(output,"not-created-user-root");
            var formal=RuntimeLocation.Resolve(false,null,false,fakeLocal);
            Check(formal.DataDirectory==Path.Combine(fakeLocal,"YanJi")&&!Directory.Exists(fakeLocal)&&formal.WindowTitle=="研迹 YanJi","formal path and title resolve without filesystem access");
            var expected=d.Setting("runtime-probe-total","");
            var reopened=expected.Length>0;
            long saved=0;
            if(reopened)Check(d.Records().Sum(r=>r.DurationMs)==long.Parse(expected),"new Windows process reopened same exact records");
            else
            {
                Check(d.Records(null,true).Count==0&&d.KnowledgeNodes().Count==0,"first startup initialized a clean isolated schema4");
                await Shot(window,"first-start");
                await m.StartCommand.ExecuteAsync(null);window.ShowMiniTimer();var mini=window.MiniTimer!;
                Check(ReferenceEquals(mini.DataContext,m)&&mini.Title.Contains("隔离测试"),"mini and main share one data context and environment label");
                var samples=new List<object>();
                for(var i=0;i<3;i++){await Task.Delay(1100);var small=AtelierValidation.Children(mini).OfType<TextBlock>().Single(x=>System.Windows.Automation.AutomationProperties.GetAutomationId(x)=="MiniDigits").Text;Check(window.TimerDigits.Text==small,"main/mini seconds synchronized sample "+i);samples.Add(new{Main=window.TimerDigits.Text,Mini=small,RawMs=m.Timer.DurationMs});}
                await m.PauseResumeCommand.ExecuteAsync(null);var held=m.Timer.DurationMs;await Task.Delay(1100);Check(held==m.Timer.DurationMs,"pause excluded");
                await m.PauseResumeCommand.ExecuteAsync(null);await Task.Delay(1100);await m.FinishCommand.ExecuteAsync(null);saved=m.Timer.DurationMs;
                Check(d.Records().Single().DurationMs==saved&&m.TodayDurationMs==saved,"timer record and aggregate reconcile exact raw milliseconds");
                Write("timer-samples.json",samples);await Shot(window,"saved-study");await Shot(mini,"mini-test");mini.Hide();
                var backup=Path.Combine(output,"isolated.yanjibak");d.Backup(backup);d.ValidateBackup(backup);Check(d.Integrity()=="ok","isolated backup validates and database is intact");
                d.SaveSetting("runtime-probe-total",saved.ToString());
            }
            m.Page=5;await Shot(window,"settings-test");m.Page=0;
            if(!reopened)await UiCopyValidation.RunAsync(window,output);
            Write("result.json",new{Status="PASS",Checks=checks,Reopened=reopened,RawMs=d.Records().Sum(r=>r.DurationMs),SavedMs=saved,Title=window.Title,Footer=window.VersionHint.Text,DataDirectory=d.DirectoryPath,Source=typeof(App).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,DllSha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(App).Assembly.Location))),Dpi=VisualTreeHelper.GetDpi(window).PixelsPerInchX,PhysicalIME="NOT TESTED",RealUserDirectoryAccess=false});
        }
        catch(Exception ex){app.ValidationFailed=true;Write("failure.json",new{Error=ex.ToString(),Checks=checks});}
        finally{window.SmokeClose();}
    }
}
