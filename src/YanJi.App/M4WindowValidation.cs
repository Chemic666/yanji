using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using YanJi.Core;
namespace YanJi.App;
internal static class M4WindowValidation
{
    private static IEnumerable<DependencyObject> Children(DependencyObject root){for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var c=VisualTreeHelper.GetChild(root,i);yield return c;foreach(var x in Children(c))yield return x;}}
    private static string Digits(Window w)=>Children(w).OfType<TextBlock>().Single(t=>BindingOperations.GetBinding(t,TextBlock.TextProperty)?.Path.Path=="TimerText").Text;
    internal static async Task RunAsync(MainWindow owner,string output)
    {
        Directory.CreateDirectory(output);var m=owner.Model;var d=m.Database;var app=(App)Application.Current;List<object> checks=[],images=[],samples=[];bool failed=false;KnowledgePackageWindow? packages=null;
        void Write(string name,object data)=>File.WriteAllText(Path.Combine(output,name),JsonSerializer.Serialize(data,new JsonSerializerOptions{WriteIndented=true}));
        void Check(bool ok,string name){checks.Add(new{Name=name,Status=ok?"PASS":"FAIL"});failed|=!ok;Write("checks.json",checks);}
        async Task Wait(Func<bool> done){var s=Stopwatch.StartNew();while(!done()&&s.ElapsedMilliseconds<8000)await Task.Delay(40);if(!done())throw new TimeoutException("WPF operation did not complete");}
        async Task Shot(Window w,string name){w.Show();w.WindowState=WindowState.Normal;w.UpdateLayout();await Task.Delay(200);var path=Path.Combine(output,name+".png");NativeCapture.Save(w,path);using var b=System.Drawing.Image.FromFile(path);var dpi=VisualTreeHelper.GetDpi(w);if(b.Width<w.ActualWidth*dpi.DpiScaleX*.9||b.Height<w.ActualHeight*dpi.DpiScaleY*.9)throw new IOException("Minimized capture rejected");images.Add(new{File=Path.GetFileName(path),Bytes=new FileInfo(path).Length,Sha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))),Width=w.ActualWidth,Height=w.ActualHeight,PixelWidth=b.Width,PixelHeight=b.Height,Dpi=dpi.PixelsPerInchX,Dark=ThemeService.Dark});Write("screenshots.json",images);}
        void Click(Window w,string label)=>Children(w).OfType<Button>().First(b=>b.Content?.ToString()==label&&b.IsVisible).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        void EditNext(Action<EditorDialog> fill){owner.TestDialogShown=w=>{async Task Fill(){await Task.Delay(150);var editor=(EditorDialog)w;fill(editor);Click(editor,"保存");} _=Fill();};}
        try
        {
            Write("provenance.json",new{Assembly=Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,BinarySha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Assembly.GetExecutingAssembly().Location))),OS=Environment.OSVersion.ToString(),Runtime=Environment.Version.ToString(),Data=d.DatabasePath,At=DateTimeOffset.UtcNow,Method="native HWND, scripted WPF controls/commands; no physical IME or power change"});
            owner.Width=1200;owner.Height=800;m.Page=5;await Shot(owner,"settings-empty-light-1200");
            EditNext(editor=>{editor.SetField("name","2028 考研 · 集成验收");editor.SetField("date","");editor.SetField("subject:408","1");});Click(owner,"配置考试档案与科目");await Wait(()=>!m.Busy&&d.Setting("onboarding-complete","")=="true");owner.TestDialogShown=null;
            Check(d.ExamProfiles().Single().Name=="2028 考研 · 集成验收"&&d.Subjects().Any(s=>s.Id=="408"),"native first-use profile and subject setup");
            m.Page=2;await Task.Delay(100);var k=owner.KnowledgeView.Model!;k.Subject=k.Subjects.Single(s=>s.Id=="408");await k.ReloadAsync();Check(k.ProfileLabel==d.ExamProfiles().Single().Name,"profile heading reflects configured first-use name");packages=new(owner);packages.Show();await Task.Delay(100);await packages.Initialization;var p=packages.Model;p.FilterSubject=p.Subjects.Single(s=>s.Id=="408");p.SelectedCatalog=p.Catalog.Single(c=>c.Package.PackageId.EndsWith(".408"));var v1=p.SelectedCatalog.Package;
            p.Confirm=message=>{var dialog=new ConfirmDialog(packages,"确认知识目录变更",message,"确认应用",false);async Task Accept(){await Task.Delay(150);Click(dialog,"确认应用");} _=Accept();return Task.FromResult(dialog.ShowDialog()==true);};
            await p.PreviewCommand.ExecuteAsync(null);await Shot(packages,"408-install-preview-light-1200");await p.ApplyCommand.ExecuteAsync(null);Check(d.KnowledgeNodes().Count==145,"actual 408 install creates 145 stable reference nodes");packages.Close();packages=null;
            m.Refresh();await k.ReloadAsync();var root=d.KnowledgeNodes().Single(n=>n.Name=="数据结构");var leaf=d.KnowledgeNodes().First(n=>n.ParentId==root.Id);k.Search=leaf.Name;await Task.Delay(300);Check(k.SearchResults.Any(r=>r.Node.Id==leaf.Id),"native Chinese search finds knowledge node");k.Search="";k.Locate(leaf.Id);k.NodeName="我的 "+leaf.Name;k.Description="个人复习重点 · 中文长文本，不允许模板静默覆盖。";await k.SaveCommand.ExecuteAsync(null);leaf=d.KnowledgeNodes().Single(n=>n.Id==leaf.Id);
            await Shot(owner,"knowledge-learning-light-1200");EditNext(editor=>{editor.SetField("title","408 每日知识复习");});Click(owner,"添加任务");await Wait(()=>!m.Busy&&d.Tasks().Any());owner.TestDialogShown=null;Check(d.Tasks().Single().ChapterId==leaf.Id,"native node task editor preserves stable binding");
            await Task.Run(()=>d.SaveKnowledgeNodes(Enumerable.Range(0,10000).Select(i=>new KnowledgeNode("m4-load-"+i,"math","负载节点 "+i,i==0?null:"m4-load-"+((i-1)/20))).ToArray(),"synthetic 10k load"));m.Refresh();
            owner.ShowMiniTimer();
            foreach(int mode in new[]{0,1,2})
            {
                m.FocusModeIndex=mode;m.FocusMinutes="1";m.BreakMinutes="1";
                if(mode==0)await m.StartKnowledgeAsync(leaf.Id);else await m.StartTaskCommand.ExecuteAsync(d.Tasks().Single());
                var session=m.Timer.SessionId;Check(m.TimerRunning&&m.Timer.Focus?.Mode==(FocusMode)mode&&m.Timer.ChapterId==leaf.Id,"knowledge/task actual start mode "+mode);
                if(mode==0){var watch=Stopwatch.StartNew();m.Page=2;k.Subject=k.Subjects.Single(s=>s.Id=="math");await k.ReloadAsync();k.Roots[0].Expanded=true;k.Search="负载节点 9999";await Task.Delay(300);Check(k.SearchResults.Count==1,"10k native search and lazy expansion while timer runs");Write("ui-performance.json",new{Nodes=10000,LoadSearchMs=watch.Elapsed.TotalMilliseconds,SearchMs=k.LastSearchMilliseconds,Dpi=VisualTreeHelper.GetDpi(owner).PixelsPerInchX});k.Search="";k.Subject=k.Subjects.Single(s=>s.Id=="408");await k.ReloadAsync(leaf.Id);m.Page=0;}
                string? last=null;for(int n=1;n<=3;n++){var threshold=(m.Timer.DurationMs/1000+1)*1000;await Wait(()=>m.Timer.DurationMs>=threshold);var watch=Stopwatch.StartNew();await Task.Run(()=>d.KnowledgeStudy(root.Id,DateOnly.MinValue,DateOnly.MaxValue));owner.UpdateLayout();owner.MiniTimer!.UpdateLayout();var main=Digits(owner);var mini=Digits(owner.MiniTimer);samples.Add(new{Mode=mode,Sample=n,Utc=DateTimeOffset.UtcNow,Main=main,Mini=mini,RawMs=m.Timer.DurationMs,Ticks=owner.ValidationTickCount,SummaryMs=watch.Elapsed.TotalMilliseconds});Write("timer-samples.json",samples);Check(main==mini&&main!=last,"actual seconds main/mini mode "+mode+" sample "+n);last=main;}
                await m.PauseResumeCommand.ExecuteAsync(null);var paused=m.Timer.DurationMs;await Task.Delay(2100);Check(m.Timer.DurationMs==paused,"actual pause excludes wait mode "+mode);await m.PauseResumeCommand.ExecuteAsync(null);
                if(mode==0){owner.Close();Check(!owner.IsVisible&&m.TimerRunning,"active X hides to tray and keeps timer running");owner.ShowFromTray();owner.WindowState=WindowState.Minimized;await Task.Delay(1100);owner.ShowFromTray();await m.FinishCommand.ExecuteAsync(null);}
                else{var end=Stopwatch.StartNew();while((m.TimerActive||m.Busy)&&end.ElapsedMilliseconds<70000)await Task.Delay(100);}
                Check(!m.TimerActive&&d.Records().Single(r=>r.Id==session).DurationMs==m.Timer.DurationMs,"durable actual finish/target mode "+mode);
                if(mode==2){var before=d.Records().Sum(r=>r.DurationMs);await Task.Delay(2100);Check(m.BreakActive&&before==d.Records().Sum(r=>r.DurationMs),"actual pomodoro rest never contributes");m.SkipBreakCommand.Execute(null);}
            }
            m.FocusModeIndex=0;var recent=m.RecentLearningItems.First();await m.ContinueRecentCommand.ExecuteAsync(recent);await Wait(()=>m.Timer.DurationMs>=1000);await m.FinishCommand.ExecuteAsync(null);Check(m.Timer.ChapterId==leaf.Id,"recent shortcut starts a new correctly scoped session");
            m.ManualDate=m.LocalToday().AddDays(-1).ToDateTime(TimeOnly.MinValue);m.ManualSubject=m.Subjects.Single(s=>s.Id=="408");m.ManualChapter=m.ManualChapters.Single(c=>c.Id==leaf.Id);m.ManualTask=d.Tasks().Single();m.ManualMinutes="0.5";m.ManualNote="手动补录历史学习";await m.AddManualCommand.ExecuteAsync(null);var manual=d.Records().Single(r=>r.Note=="手动补录历史学习");Check(manual.Parts.Single().Kind==PartKind.DurationOnly&&manual.Parts.Single().StartUtcMs==null,"historical manual entry has no fabricated interval");
            m.OpenKnowledgeRecord(manual);owner.UpdateLayout();EditNext(editor=>{editor.SetField("title","408 边界条件薄弱点");editor.SetField("detail","后续复习并独立验收");});Click(owner,"薄弱点关联");await Wait(()=>!m.Busy&&d.WeakPoints().Any());owner.TestDialogShown=null;Check(d.Records().Single(r=>r.Id==manual.Id).Context!.WeakPoints!.Count==1,"native record dialog creates linked weak point atomically");
            EditNext(editor=>editor.SetField("remove","clear"));Click(owner,"薄弱点关联");await Wait(()=>!m.Busy&&d.Records().Single(r=>r.Id==manual.Id).Context!.WeakPoints!.Count==0);owner.TestDialogShown=null;Check(d.WeakPoints().Single().State==WeakPointState.Open,"native unlink preserves weak point and state");
            EditNext(editor=>editor.SetField("note","用户修改补录备注"));Click(owner,"编辑整次记录");await Wait(()=>!m.Busy&&d.Records().Any(r=>r.Note=="用户修改补录备注"));owner.TestDialogShown=null;manual=d.Records().Single(r=>r.Id==manual.Id);Check(manual.DurationMs==30000,"native edit preserves exact milliseconds");
            owner.TestDialogShown=w=>{async Task Accept(){await Task.Delay(120);Click(w,"软删除整次记录");} _=Accept();};Click(owner,"删除整次记录");await Wait(()=>!m.Busy&&!d.Records().Any(r=>r.Id==manual.Id));owner.TestDialogShown=null;Check(d.Records(null,true).Single(r=>r.Id==manual.Id).Deleted,"native soft delete retains history");
            var deleted=d.Records(null,true).Single(r=>r.Id==manual.Id);await m.RunAsync(()=>d.RestoreRecord(deleted.Id,deleted.Version),"恢复合成记录");await owner.OpenKnowledgeAsync(leaf.Id);await Task.Delay(150);Check(k.LinkedRecords.Any(r=>r.Record.Id==manual.Id)&&k.StudySummary.Contains("次有效学习"),"restored record and chapter investment refresh");
            m.OpenRecords(m.LocalToday().AddDays(-1),m.LocalToday(),"408",leaf.Id);Check(m.FilteredRecords.Any(r=>r.Record.Id==manual.Id),"date/subject/node drilldown reaches canonical original records");m.Page=4;m.StatsFrom=m.LocalToday().AddDays(-1).ToDateTime(TimeOnly.MinValue);m.StatsTo=m.LocalToday().ToDateTime(TimeOnly.MinValue);m.RefreshStats();await Shot(owner,"statistics-light-1200");m.DarkTheme=true;owner.Width=900;owner.Height=600;await Shot(owner,"statistics-dark-900");
            packages=new(owner);packages.Show();await Task.Delay(100);await packages.Initialization;p=packages.Model;p.SelectPackage(v1 with{PackageVersion="2.0.0",Nodes=v1.Nodes.Select(n=>n.Id==v1.Nodes.First(x=>x.Name==leaf.Name[3..]).Id?n with{Name="模板新版名称",Description="上游备注"}:n).ToList()});
            p.Confirm=_=>Task.FromResult(true);await p.PreviewCommand.ExecuteAsync(null);Check(p.Conflicts.Count>=1,"package update detects real local modification conflict");await Shot(packages,"package-conflict-dark-900");await p.ApplyCommand.ExecuteAsync(null);Check(d.KnowledgeNodes().Single(n=>n.Id==leaf.Id).Name==leaf.Name,"package update keeps chosen local title and stable identity");packages.Close();packages=null;
            var total=d.Records().Sum(r=>r.DurationMs);var backup=Path.Combine(output,"complete-synthetic.yanjibak");d.Backup(backup);d.ValidateBackup(backup);await owner.RestoreDataAsync(backup);Check(total==d.Records().Sum(r=>r.DurationMs)&&d.InstalledPackages().Single().Version=="2.0.0","complete WPF backup/restore retains facts and package version");
            m.DarkTheme=false;owner.Width=1200;owner.Height=800;m.Page=5;await Shot(owner,"settings-ready-light-1200");
            Write("result.json",new{Status=failed?"FAIL":"PASS",Checks=checks.Count,Images=images.Count,Samples=samples.Count,NotTested=new[]{"real user upgrade","physical IME","other physical DPI","sleep/hibernate/reboot","clean machine/install/signing/public release"}});app.ValidationFailed=failed;
        }
        catch(Exception e){Write("failure.json",new{Error=e.ToString()});app.ValidationFailed=true;}
        finally{owner.TestDialogShown=null;packages?.Close();owner.SmokeClose();}
    }
}
