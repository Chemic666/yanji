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
internal static class M3WindowValidation
{
    private static IEnumerable<DependencyObject> Children(DependencyObject root)
    {for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var c=VisualTreeHelper.GetChild(root,i);yield return c;foreach(var d in Children(c))yield return d;}}
    private static string Digits(Window w)=>Children(w).OfType<TextBlock>().Single(t=>BindingOperations.GetBinding(t,TextBlock.TextProperty)?.Path.Path=="TimerText").Text;
    internal static async Task RunAsync(MainWindow owner,string output)
    {
        Directory.CreateDirectory(output);var app=(App)Application.Current;List<object> checks=[],images=[],samples=[];bool failed=false;var model=owner.Model;var db=model.Database;
        void Write(string file,object value)=>File.WriteAllText(Path.Combine(output,file),JsonSerializer.Serialize(value,new JsonSerializerOptions{WriteIndented=true}));
        void Check(bool pass,string name){checks.Add(new{Name=name,Status=pass?"PASS":"FAIL"});failed|=!pass;Write("checks.json",checks);}
        async Task Shot(string name){owner.UpdateLayout();await Task.Delay(180);var path=Path.Combine(output,name+".png");NativeCapture.Save(owner,path);images.Add(new{File=Path.GetFileName(path),Bytes=new FileInfo(path).Length,Sha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))),Width=owner.ActualWidth,Height=owner.ActualHeight,Dark=ThemeService.Dark,Dpi=VisualTreeHelper.GetDpi(owner).PixelsPerInchX});Write("screenshots.json",images);}
        async Task Reach(long milliseconds){var watch=Stopwatch.StartNew();while(model.Timer.DurationMs<milliseconds&&model.Timer.State==TimerState.Running&&watch.ElapsedMilliseconds<8000)await Task.Delay(40);}
        try
        {
            Write("provenance.json",new{Assembly=Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,BinarySha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Assembly.GetExecutingAssembly().Location))),Environment.OSVersion,Runtime=Environment.Version.ToString(),Dpi=VisualTreeHelper.GetDpi(owner).PixelsPerInchX,Data=db.DatabasePath,At=DateTimeOffset.UtcNow,Method="native HWND capture; scripted WPF commands/focus, not physical IME"});
            owner.Width=1200;owner.Height=800;model.Page=2;await Task.Delay(120);var knowledge=owner.KnowledgeView.Model!;await knowledge.ReloadAsync();await Shot("knowledge-empty-light-1200");
            var nodes=new[]{new KnowledgeNode("m3-root","math","高等数学 · 多级知识学习闭环"),new KnowledgeNode("m3-leaf","math","一元函数微分学：导数、微分与实际应用的长标题中文阅读验证","m3-root"),new KnowledgeNode("m3-other","math","后来修改的任务节点")}
                .Concat(Enumerable.Range(0,10000).Select(i=>new KnowledgeNode("load"+i,"math","合成负载节点 "+i,i==0?null:"load"+((i-1)/20)))).ToArray();
            await Task.Run(()=>db.SaveKnowledgeNodes(nodes,"M3 synthetic tree"));db.SaveTask(new("m3-task","复习微分学 · 完成基础练习",model.LocalToday(),SubjectId:"math",ChapterId:"m3-leaf"));model.Refresh();knowledge.Subject=knowledge.Subjects.Single(s=>s.Id=="math");await knowledge.ReloadAsync("m3-leaf");await Task.Delay(180);
            await Shot("knowledge-detail-light-1200");model.DarkTheme=true;await Shot("knowledge-detail-dark-1200");owner.Width=900;owner.Height=600;await Shot("knowledge-detail-dark-900");model.DarkTheme=false;
            Check(knowledge.Selected?.Node.Id=="m3-leaf"&&knowledge.Roots.Count==3,"10k tree retains selected stable ID with lazy roots");
            owner.UpdateLayout();await Task.Delay(120);var start=Children(owner.KnowledgeView).OfType<Button>().Single(b=>b.Content?.ToString()=="开始学习 →");start.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var timeout=Stopwatch.StartNew();while(!model.TimerActive&&timeout.ElapsedMilliseconds<5000)await Task.Delay(40);
            Check(model.Timer.ChapterId=="m3-leaf"&&model.Timer.Context?.TaskId==null,"native knowledge action starts correct node without stale task");
            await Reach(1200);await model.FinishCommand.ExecuteAsync(null);Check(model.CompletedStudy?.ChapterId=="m3-leaf"&&model.HasCompletedStudy,"optional completion appears after durable save");await Shot("completion-light-900");
            owner.ShowMiniTimer();owner.Width=1200;owner.Height=800;
            foreach(var mode in new[]{0,1,2})
            {
                model.FocusModeIndex=mode;model.FocusMinutes="1";model.BreakMinutes="1";
                await model.StartTaskCommand.ExecuteAsync(db.Tasks().Single(t=>t.Id=="m3-task"));
                Check(model.Timer.ChapterId=="m3-leaf"&&model.Timer.Context?.TaskId=="m3-task","task binds node and frozen context mode "+mode);
                string? previous=null;for(int second=1;second<=3;second++)
                {
                    await Reach(second*1000);var watch=Stopwatch.StartNew();var summary=await Task.Run(()=>db.KnowledgeStudy("m3-root",DateOnly.MinValue,DateOnly.MaxValue));owner.UpdateLayout();owner.MiniTimer!.UpdateLayout();var main=Digits(owner);var mini=Digits(owner.MiniTimer);
                    samples.Add(new{Mode=mode,Second=second,Utc=DateTimeOffset.UtcNow,Main=main,Mini=mini,Vm=model.TimerText,RawMs=model.Timer.DurationMs,Ticks=owner.ValidationTickCount,SummaryMs=watch.Elapsed.TotalMilliseconds});Write("timer-samples.json",samples);
                    Check(main==mini&&main==model.TimerText&&main!=previous&&main!="00:00:00","continuous actual main/mini seconds during 10k summary mode "+mode+" sample "+second);previous=main;
                }
                await model.PauseResumeCommand.ExecuteAsync(null);var frozen=model.TimerText;var raw=model.Timer.DurationMs;await Task.Delay(3100);Check(model.TimerText==frozen&&model.Timer.DurationMs==raw,"actual pause excludes three seconds mode "+mode);
                await model.PauseResumeCommand.ExecuteAsync(null);model.Page=2;await knowledge.ReloadAsync("m3-leaf");await Task.Delay(1100);model.Page=0;
                owner.WindowState=WindowState.Minimized;await Task.Delay(1100);owner.WindowState=WindowState.Normal;await Reach(raw+2000);
                Check(Digits(owner)==model.TimerText&&Digits(owner.MiniTimer!)==model.TimerText,"page/minimize/restore maintains shared digits mode "+mode);
                if(mode==0)await model.FinishCommand.ExecuteAsync(null);
                else {var end=Stopwatch.StartNew();while((model.TimerActive||model.Busy)&&end.ElapsedMilliseconds<70000)await Task.Delay(100);}
                var saved=db.Records().Single(r=>r.Id==model.Timer.SessionId);Check(!model.TimerActive&&saved.DurationMs==model.Timer.DurationMs&&saved.Context!.TaskId=="m3-task"&&saved.ChapterId=="m3-leaf","finish or target cap stores exact context and duration mode "+mode);
                if(mode==2){var duration=saved.DurationMs;await Task.Delay(3100);Check(model.BreakActive&&db.Records().Single(r=>r.Id==saved.Id).DurationMs==duration,"actual pomodoro rest adds no study facts");await Shot("pomodoro-rest-light-1200");model.SkipBreakCommand.Execute(null);}
            }
            model.ManualSubject=model.Subjects.Single(s=>s.Id=="math");model.ManualChapter=model.ManualChapters.Single(c=>c.Id=="m3-leaf");model.ManualTask=db.Tasks().Single();model.ManualMinutes="0.25";model.ManualNote="隔离补录：仅时长，不定位";await model.AddManualCommand.ExecuteAsync(null);
            var manual=db.Records().Single(r=>r.Note=="隔离补录：仅时长，不定位");Check(manual.Context?.TaskId=="m3-task"&&manual.Parts.All(p=>p.Kind==PartKind.DurationOnly&&p.StartUtcMs==null),"WPF manual context never invents intervals");
            var weak=db.AddWeakPointToStudy(manual.Id,manual.Version,"链式法则易错","需要复习复合函数求导");db.SaveTask(db.Tasks().Single() with{ChapterId="m3-other",Title="后来改变的任务"});var leaf=db.KnowledgeNodes().Single(n=>n.Id=="m3-leaf");db.SaveKnowledgeNodes([leaf with{Name=leaf.Name+" · 已重命名",ParentId=null,Archived=true}]);
            var historical=db.Records().Single(r=>r.Id==manual.Id);Check(historical.Context!.TaskTitle=="复习微分学 · 完成基础练习"&&!historical.Context.NodePath!.Contains("已重命名")&&historical.ChapterId==leaf.Id,"task/node changes do not rewrite historical captured context");
            model.Refresh();await owner.OpenKnowledgeAsync(leaf.Id);await Task.Delay(180);Check(knowledge.LinkedWeakPoints.Any(w=>w.Id==weak.Id)&&knowledge.LinkedRecords.Any(r=>r.Record.Id==manual.Id),"archived node still exposes historical records and weak points");await Shot("knowledge-history-light-1200");model.DarkTheme=true;owner.Width=900;owner.Height=600;await Shot("knowledge-history-dark-900");
            model.OpenKnowledgeRecord(historical);await Shot("record-context-dark-900");model.DarkTheme=false;owner.Width=1200;owner.Height=800;await Shot("record-context-light-1200");
            var before=db.DailyTotals(DateOnly.MinValue,DateOnly.MaxValue).Sum(t=>t.DurationMs);var backup=Path.Combine(output,"m3-window-synthetic.yanjibak");db.Backup(backup);db.ValidateBackup(backup);db.RestoreBackup(backup);Check(before==db.DailyTotals(DateOnly.MinValue,DateOnly.MaxValue).Sum(t=>t.DurationMs)&&db.Records().Single(r=>r.Id==manual.Id).Context!.WeakPoints!.Single().Id==weak.Id,"schema4 backup restore preserves facts and context");
            Check(owner.ValidationTickEnabled&&owner.ValidationTickCount>15,"actual DispatcherTimer remains enabled and triggered");
            Write("result.json",new{Status=failed?"FAIL":"PASS",Checks=checks.Count,Images=images.Count,Samples=samples.Count,NotTested=new[]{"physical Chinese IME / physical keyboard","DPI other than actual192","sleep/hibernate/reboot","real user migration","M4 formal release"}});app.ValidationFailed=failed;
        }
        catch(Exception e){Write("failure.json",new{Error=e.ToString()});app.ValidationFailed=true;}
        finally{owner.SmokeClose();}
    }
}
