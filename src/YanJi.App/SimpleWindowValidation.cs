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
using System.Windows.Input;
using YanJi.Core;
namespace YanJi.App;

/// <summary>Owned HWND only, synthetic explicit data directory; never physical IME or power tests.</summary>
internal static class SimpleWindowValidation
{
    private static IEnumerable<DependencyObject> Children(DependencyObject root)
    {for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var c=VisualTreeHelper.GetChild(root,i);yield return c;foreach(var child in Children(c))yield return child;}}
    private static string Digits(Window w)=>Children(w).OfType<TextBlock>().Single(t=>BindingOperations.GetBinding(t,TextBlock.TextProperty)?.Path.Path=="TimerText").Text;
    internal static async Task RunAsync(MainWindow owner,string output)
    {
        var m=owner.Model;var d=m.Database;var app=(App)Application.Current;
        Directory.CreateDirectory(output);List<object> checks=[],images=[],samples=[];bool failed=false;
        void Write(string file,object value)=>File.WriteAllText(Path.Combine(output,file),JsonSerializer.Serialize(value,new JsonSerializerOptions{WriteIndented=true}));
        void Check(bool value,string name){checks.Add(new{Name=name,Status=value?"PASS":"FAIL"});failed|=!value;Write("checks.json",checks);}
        async Task Wait(Func<bool> done){var watch=Stopwatch.StartNew();while(!done()&&watch.ElapsedMilliseconds<12000)await Task.Delay(40);if(!done())throw new TimeoutException("等待界面状态超时");}
        async Task Shot(string name)
        {
            owner.ShowFromTray();owner.UpdateLayout();await Task.Delay(220);var file=Path.Combine(output,name+".png");NativeCapture.Save(owner,file);
            using var image=(System.Drawing.Bitmap)System.Drawing.Image.FromFile(file);var dpi=VisualTreeHelper.GetDpi(owner);
            var colors=new HashSet<int>();for(int x=image.Width/24;x<image.Width-image.Width/24;x+=Math.Max(1,image.Width/24))for(int y=image.Height/24;y<image.Height-image.Height/24;y+=Math.Max(1,image.Height/24))colors.Add(image.GetPixel(x,y).ToArgb());
            if(colors.Count<5)throw new IOException("原生截图近乎纯色，拒绝将空白采集计为视觉通过。");
            Check(image.Width>=owner.ActualWidth*dpi.DpiScaleX*.95&&image.Height>=owner.ActualHeight*dpi.DpiScaleY*.95,"non-minimized actual HWND capture "+name);
            images.Add(new{File=Path.GetFileName(file),Bytes=new FileInfo(file).Length,Sha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file))),Width=owner.ActualWidth,Height=owner.ActualHeight,PixelWidth=image.Width,PixelHeight=image.Height,Dpi=dpi.PixelsPerInchX,Dark=ThemeService.Dark});Write("screenshots.json",images);
        }
        void Fill(Action<EditorDialog> action)=>owner.TestDialogShown=w=>{async Task Complete(){await Task.Delay(100);var dialog=(EditorDialog)w;action(dialog);Children(w).OfType<Button>().Single(b=>b.Content?.ToString()=="保存").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));} _=Complete();};
        try
        {
            Write("provenance.json",new{Assembly=Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,BinarySha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(App).Assembly.Location))),Data=d.DirectoryPath,OS=Environment.OSVersion.ToString(),Runtime=Environment.Version.ToString(),Method="real native HWND + WPF controls and commands; synthetic database; no physical IME/system changes"});
            Check(d.KnowledgeNodes().Count==0&&d.Records().Count==0,"new isolated profile begins empty");
            Check(owner.NavigationList.Items.Count==4&&m.NavigationIndex==0,"three daily destinations plus more, no mode switch");
            owner.Width=1200;owner.Height=800;await Shot("today-empty-light-1200");
            Fill(e=>{e.SetField("name","RC-Simple 隔离验收");e.SetField("presets","1");});await owner.ConfigureExamProfileAsync();owner.TestDialogShown=null;
            Check(new[]{"math","english","408"}.All(s=>d.KnowledgeNodes(s).Count>0),"first-use explicit choice applies three usable preset subjects");
            var initial=d.KnowledgeNodes().Select(n=>n.Id).Order().ToArray();await owner.ApplySimplePresetsAsync(false);
            Check(initial.SequenceEqual(d.KnowledgeNodes().Select(n=>n.Id).Order())&&d.InstalledPackages().Count==3,"repeated preset action preserves existing nodes and package identities");
            m.Page=2;await Wait(()=>owner.SimpleProgressView.Model!=null);await owner.SimpleProgressView.RefreshAsync();
            Check(!owner.KnowledgeExpander.IsExpanded,"full tree is optional, not default progress layout");
            var view=owner.SimpleProgressView.Model!;var mathNode=d.KnowledgeNodes("math").First(n=>n.Name.Contains("极限")&&n.Kind==KnowledgeNodeKind.KnowledgePoint);
            var beforeRaw=d.Records().Sum(r=>r.DurationMs);
            Fill(e=>{e.SetField("node",mathNode.Id);e.SetField("state","1");e.SetField("content","完成函数极限基础，需继续整理边界条件。");e.SetField("next","下次复习连续与间断 · 中文长备注验收");});
            await owner.QuickProgressAsync(view.Rows.First(r=>r.Subject.Id=="math"));owner.TestDialogShown=null;await owner.SimpleProgressView.RefreshAsync();
            Check(d.ProgressNotes().Single().NodeId==mathNode.Id&&d.Records().Sum(r=>r.DurationMs)==beforeRaw,"native progress editor independently saves content and next without time facts");
            Check(view.Rows.First(r=>r.Subject.Id=="math").NodeId==mathNode.Id,"subject summary reflects latest explicit progress");
            await Shot("progress-common-light-1200");
            var ids=d.KnowledgeNodes().Select(n=>n.Id).Order().ToArray();m.SetReferenceVariant("math","math2");await owner.SimpleProgressView.RefreshAsync();
            var visible=m.VisibleReferenceIds("math");Check(!d.KnowledgeNodes("math").Any(n=>visible.Contains(n.Id)&&n.Name=="概率论与数理统计"),"math2 display excludes math1-only probability");
            m.SetReferenceVariant("math","math1");Check(d.KnowledgeNodes("math").Any(n=>m.VisibleReferenceIds("math").Contains(n.Id)&&n.Name=="概率论与数理统计"),"math1 display includes probability");
            m.SetReferenceVariant("english","english1");var e1=m.VisibleReferenceIds("english");m.SetReferenceVariant("english","english2");var e2=m.VisibleReferenceIds("english");
            Check(!e1.SetEquals(e2)&&ids.SequenceEqual(d.KnowledgeNodes().Select(n=>n.Id).Order()),"exam ranges differ without reinstall or deleting learning history");
            m.SetReferenceVariant("math","math2");m.DarkTheme=true;owner.Width=900;owner.Height=600;await owner.SimpleProgressView.RefreshAsync();await Shot("progress-math2-dark-900");
            var mathRow=view.Rows.First(r=>r.Subject.Id=="math");Check(view.Choices("math","极限").Any(),"quick progress choices support Chinese path search");
            var prepare=Stopwatch.StartNew();await Task.Run(()=>d.SaveKnowledgeNodes(Enumerable.Range(0,10000).Select(i=>new KnowledgeNode("simple-load-"+i,"math","Simple负载"+i,i==0?null:"simple-load-0")).ToArray(),"isolated 10k fixture before active timer"));
            Write("fixture-performance.json",new{SyntheticNodes=10000,CreationMs=prepare.Elapsed.TotalMilliseconds});
            owner.ShowMiniTimer();
            foreach(var mode in new[]{0,1,2})
            {
                m.FocusModeIndex=mode;m.FocusMinutes="1";m.BreakMinutes="1";
                if(mode==0)await m.StartKnowledgeAsync(mathNode.Id);else if(mode==1){m.TimerSubject=m.Subjects.First(s=>s.Id=="english");m.TimerChapter=m.TimerChapters.First();m.FocusTask=null;await m.StartCommand.ExecuteAsync(null);}else{d.SaveTask(new("simple-task","复习极限边界",SubjectId:"math",ChapterId:mathNode.Id));m.Refresh();await m.StartTaskCommand.ExecuteAsync(d.Tasks().Single());}
                Check(m.TimerRunning&&(mode!=1||m.Timer.ChapterId==null),"start focus mode and optional chapter "+mode);
                if(mode==0)
                {
                    var load=Stopwatch.StartNew();
                    await owner.SimpleProgressView.RefreshAsync();var search=Stopwatch.StartNew();var match=await Task.Run(()=>view.Choices("math","Simple负载9999"));
                    Check(match.Count==1&&m.TimerRunning,"10k progress search while real timer continues");Write("performance.json",new{SyntheticNodes=10000,LoadAndRefreshMs=load.Elapsed.TotalMilliseconds,SearchMs=search.Elapsed.TotalMilliseconds,Dpi=VisualTreeHelper.GetDpi(owner).PixelsPerInchX});
                }
                string? previous=null;
                for(var n=0;n<3;n++){var next=(m.Timer.DurationMs/1000+1)*1000;await Wait(()=>m.Timer.DurationMs>=next);var main=Digits(owner);var mini=Digits(owner.MiniTimer!);samples.Add(new{Mode=mode,Utc=DateTimeOffset.UtcNow,Main=main,Mini=mini,RawMs=m.Timer.DurationMs,Ticks=owner.ValidationTickCount});Write("timer-samples.json",samples);Check(main==mini&&main!=previous,"actual main mini seconds "+mode+" sample "+n);previous=main;}
                await m.PauseResumeCommand.ExecuteAsync(null);var paused=m.Timer.DurationMs;await Task.Delay(2100);Check(m.Timer.DurationMs==paused,"pause excluded mode "+mode);await m.PauseResumeCommand.ExecuteAsync(null);
                if(mode==0){owner.Close();Check(!owner.IsVisible&&m.TimerRunning,"active close hides to tray");owner.ShowFromTray();owner.WindowState=WindowState.Minimized;await Task.Delay(1100);owner.ShowFromTray();}
                await m.FinishCommand.ExecuteAsync(null);Check(d.Records().Single(r=>r.Id==m.Timer.SessionId).DurationMs==m.Timer.DurationMs,"exact milliseconds finish mode "+mode);
            }
            m.FocusModeIndex=0;m.Page=0;m.DarkTheme=false;owner.Width=1200;owner.Height=800;await Shot("today-light-1200");m.DarkTheme=true;owner.Width=900;owner.Height=600;await Shot("today-dark-900");
            Check(m.TimerChapters.Skip(1).First().Id==mathNode.Id,"recent chapter appears first for its subject");
            m.ManualSubject=m.Subjects.First(s=>s.Id=="math");m.ManualChapter=m.ManualChapters.First(c=>c.Id==mathNode.Id);m.ManualMinutes="0.5";m.ManualNote="隔离历史补录";await m.AddManualCommand.ExecuteAsync(null);
            Check(d.Records().Single(r=>r.Note=="隔离历史补录").Parts.All(p=>p.Kind==PartKind.DurationOnly&&p.StartUtcMs==null),"manual entry does not invent intervals");
            var total=d.Records().Sum(r=>r.DurationMs);m.Refresh();m.Page=4;m.StatsFrom=m.LocalToday().ToDateTime(TimeOnly.MinValue);m.StatsTo=m.StatsFrom;m.RefreshStats();
            Check(d.DailyTotals(m.LocalToday(),m.LocalToday()).Sum(x=>x.DurationMs)==total,"raw millisecond daily totals equal canonical records");
            m.DarkTheme=false;owner.Width=1200;owner.Height=800;await Shot("data-light-1200");m.DarkTheme=true;owner.Width=900;owner.Height=600;await Shot("data-dark-900");
            var backup=Path.Combine(output,"simple-complete.yanjibak");d.Backup(backup);d.ValidateBackup(backup);await owner.RestoreDataAsync(backup);
            Check(d.Records().Sum(r=>r.DurationMs)==total&&d.ProgressNotes().Single().Next.Contains("连续"),"backup restore preserves independent progress and exact milliseconds");
            var started=Stopwatch.StartNew();await owner.BrowseSimpleSubjectAsync("math",mathNode.Id);Check(owner.KnowledgeExpander.IsExpanded&&owner.KnowledgeView.Model?.Selected?.Node.Id==mathNode.Id,"advanced tree and historical node remain accessible");
            var tree=owner.KnowledgeView.Model!;tree.NodeName+=" · 更新后";await tree.SaveCommand.ExecuteAsync(null);
            await Wait(()=>view.Rows.Any(r=>r.Subject.Id=="math"&&r.Current.Contains("更新后")));Check(true,"same-page tree edit refreshes simple progress summary");
            var activeNode=d.KnowledgeNodes().Single(n=>n.Id==mathNode.Id);await m.RunAsync(()=>d.SaveProgressNote(activeNode.Id,activeNode.Version,LearningState.Learning,"继续巩固极限","下一步复习连续"),"隔离快速进度更新");
            await Wait(()=>tree.Selected?.Node.Version==d.KnowledgeNodes().Single(n=>n.Id==mathNode.Id).Version);Check(true,"quick progress refreshes expanded tree version without stale editor");
            var parent=d.KnowledgeNodes().Single(n=>n.Id==mathNode.ParentId);await m.RunAsync(()=>d.SaveKnowledgeNodes([parent with{Archived=true}]),"隔离祖先归档");
            await Wait(()=>view.Rows.All(r=>r.NodeId!=mathNode.Id));Check(true,"archived ancestor removes descendant from current-progress summary, not history");
            parent=d.KnowledgeNodes().Single(n=>n.Id==parent.Id);Check(await m.RunAsync(()=>d.SaveKnowledgeNodes([parent with{Archived=false}]),"隔离重新启用"),"reactivation transaction commits");
            m.Page=6;owner.NavigationList.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(owner),Environment.TickCount,Key.Enter){RoutedEvent=Keyboard.PreviewKeyDownEvent});Check(m.Page==7&&owner.NavigationList.SelectedIndex==3,"selected more navigation returns from a drilldown via keyboard");
            m.DarkTheme=false;owner.Width=1200;owner.Height=800;m.Page=2;owner.KnowledgeExpander.IsExpanded=false;await owner.SimpleProgressView.RefreshAsync();
            await Wait(()=>view.Rows.Any(r=>r.NodeId==mathNode.Id&&r.Next.Contains("连续")));Check(true,"reactivated ancestor restores stable current progress before visual capture");
            await Shot("progress-filled-light-1200");
            Write("summary.json",new{RawMilliseconds=total,Records=d.Records().Count,Nodes=d.KnowledgeNodes().Count,Progress=d.ProgressNotes(),Installed=d.InstalledPackages(),Schema=d.Diagnostics(),DirectoryRoundTripMs=started.Elapsed.TotalMilliseconds});
            Write("result.json",new{Status=failed?"FAIL":"PASS",Checks=checks.Count,Images=images.Count,Samples=samples.Count,NotTested=new[]{"physical IME candidate input","other physical DPI","sleep hibernate reboot","clean-machine offline","real user upgrade","multi-day soak"}});app.ValidationFailed=failed;
        }
        catch(Exception ex){Write("failure.json",new{Error=ex.ToString()});app.ValidationFailed=true;}
        finally{owner.TestDialogShown=null;owner.SmokeClose();}
    }
}
