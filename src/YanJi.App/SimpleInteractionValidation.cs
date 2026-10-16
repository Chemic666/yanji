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
internal static class SimpleInteractionValidation
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
        Button ButtonBy(string text,DependencyObject? root=null)=>Children(root??owner).OfType<Button>().First(b=>b.IsVisible&&b.Content?.ToString()==text);
        async Task MenuAction(string name,string header)
        {
            var button=ButtonBy(name);button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Task.Delay(80);
            Check(button.ContextMenu?.IsOpen==true&&button.Focusable,"keyboard-focusable action menu "+name);
            var item=button.ContextMenu!.Items.OfType<MenuItem>().Single(i=>i.Header.ToString()==header);
            Check(item.IsEnabled,"enabled menu action "+header);if(item.Command is {} command){Check(command.CanExecute(item.CommandParameter),"menu command available "+header);command.Execute(item.CommandParameter);}else item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));button.ContextMenu.IsOpen=false;
            await Task.Delay(160);
        }
        try
        {
            Write("provenance.json",new{Assembly=Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,BinarySha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(App).Assembly.Location))),Data=d.DirectoryPath,OS=Environment.OSVersion.ToString(),Runtime=Environment.Version.ToString(),Method="real owned HWND, actual WPF controls/commands, synthetic database; no physical IME or power changes"});
            owner.Width=1200;owner.Height=800;Check(d.Records().Count==0,"new isolated empty database");await Shot("today-empty-light-1200");
            m.Page=4;await Shot("data-empty-light-1200");m.Page=0;
            Fill(e=>{e.SetField("name","RC-Simple 第二轮隔离验收");e.SetField("presets","1");});await owner.ConfigureExamProfileAsync();owner.TestDialogShown=null;
            Check(d.InstalledPackages().Count==3,"three preset subjects retained");
            m.Page=2;await Wait(()=>owner.SimpleProgressView.Model!=null);await owner.SimpleProgressView.RefreshAsync();var view=owner.SimpleProgressView.Model!;
            var settings=Children(owner.SimpleProgressView).OfType<Expander>().Single();Check(!settings.IsExpanded&&!owner.KnowledgeExpander.IsExpanded,"scope settings and tree are secondary and initially collapsed");
            var math=d.KnowledgeNodes("math").First(n=>n.Name.Contains("极限")&&n.Kind==KnowledgeNodeKind.KnowledgePoint);
            Fill(e=>{e.SetField("node",math.Id);e.SetField("state","1");e.SetField("content","已学习函数极限，下一次整理连续与间断。");e.SetField("next","复习极限边界与连续性");});
            ButtonBy("更新进度").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Wait(()=>d.ProgressNotes().Count==1);owner.TestDialogShown=null;await owner.SimpleProgressView.RefreshAsync();
            await Wait(()=>!m.Busy&&view.Rows.Any(r=>r.Subject.Id=="math"&&r.NodeId==math.Id&&r.Next.Contains("连续性")));Check(d.Records().Count==0&&view.Rows.First(r=>r.Subject.Id=="math").NodeId==math.Id,"one update entry independently persists progress, zero study facts");
            await Shot("progress-light-1200");m.DarkTheme=true;owner.Width=900;owner.Height=600;await Shot("progress-dark-900");
            settings.IsExpanded=true;owner.UpdateLayout();await Task.Delay(100);Check(Children(settings).OfType<ComboBox>().Count()==2,"exam ranges remain accessible in subject settings");settings.IsExpanded=false;
            m.DarkTheme=false;owner.Width=1200;owner.Height=800;m.Page=0;owner.ShowMiniTimer();
            d.SaveTask(new("simple2-task","复习极限边界",SubjectId:"math",ChapterId:math.Id));m.Refresh();
            foreach(var mode in new[]{0,1,2})
            {
                await Wait(()=>!m.Busy);m.FocusModeIndex=mode;m.FocusMinutes="1";m.BreakMinutes="1";
                if(mode==0)await m.StartKnowledgeAsync(math.Id);else if(mode==2)await m.StartTaskCommand.ExecuteAsync(d.Tasks().Single());else{m.FocusTask=null;await m.StartCommand.ExecuteAsync(null);}
                Write("start-"+mode+".json",new{m.Status,m.Busy,m.TimerSubject,m.TimerChapter,Snapshot=m.Timer});if(!m.TimerRunning)throw new InvalidOperationException("未开始："+m.Status);Check(m.TimerRunning,"start mode "+mode);string? previous=null;
                for(int n=0;n<3;n++)
                {
                    var next=(m.Timer.DurationMs/1000+1)*1000;await Wait(()=>m.Timer.DurationMs>=next);var main=Digits(owner);var mini=Digits(owner.MiniTimer!);
                    samples.Add(new{Mode=mode,Utc=DateTimeOffset.UtcNow,Main=main,Mini=mini,RawMs=m.Timer.DurationMs,Ticks=owner.ValidationTickCount});Write("timer-samples.json",samples);
                    Check(main==mini&&main!=previous,"real second refresh and mini sync mode "+mode+" sample "+n);previous=main;
                }
                await m.PauseResumeCommand.ExecuteAsync(null);var paused=m.Timer.DurationMs;var text=Digits(owner);await Task.Delay(2100);
                Check(m.Timer.DurationMs==paused&&Digits(owner)==text&&Digits(owner.MiniTimer!)==text,"pause display and raw duration frozen mode "+mode);await m.PauseResumeCommand.ExecuteAsync(null);
                if(mode==0){owner.Close();Check(!owner.IsVisible&&m.TimerRunning,"close continues via tray");owner.ShowFromTray();owner.WindowState=WindowState.Minimized;await Task.Delay(1100);owner.ShowFromTray();m.Page=2;await owner.SimpleProgressView.RefreshAsync();m.Page=0;Check(m.TimerRunning,"minimize restore and page switch preserve running timer");}
                await m.FinishCommand.ExecuteAsync(null);
                Check(d.Records().Single(r=>r.Id==m.Timer.SessionId).DurationMs==m.Timer.DurationMs&&m.FocusSubjectLabel.StartsWith("上一段已保存")&&m.FocusStartLabel=="开始下一段"&&Digits(owner)==Digits(owner.MiniTimer!),"saved exact milliseconds and unambiguous completed state mode "+mode);
            }
            m.FocusModeIndex=0;await Shot("today-light-1200");m.DarkTheme=true;owner.Width=900;owner.Height=600;await Shot("today-dark-900");
            m.DarkTheme=false;owner.Width=1200;owner.Height=800;
            await MenuAction("任务操作 ⋯","解除任务关联");Check(m.FocusTask==null&&!m.HasLinkedTask,"unlink task menu retains existing command");
            m.FocusTask=d.Tasks().Single();await Wait(()=>m.HasLinkedTask);
            await MenuAction("任务操作 ⋯","标记任务完成");await Wait(()=>d.Tasks().Single().Done);Check(true,"task completion retained as explicit menu action");
            var raw=d.Records().Sum(r=>r.DurationMs);
            Fill(e=>e.SetField("note","由记录菜单修改备注"));await MenuAction("记录操作 ⋯","编辑记录");owner.TestDialogShown=null;
            await Wait(()=>d.Records().Any(r=>r.Note=="由记录菜单修改备注"));Check(d.Records().Sum(r=>r.DurationMs)==raw,"record menu edit preserves original milliseconds");
            owner.TestDialogShown=w=>{async Task Accept(){await Task.Delay(100);Check(Children(w).OfType<TextBlock>().Any(t=>t.Text.Contains("受影响日期")),"delete confirmation describes scope");Children(w).OfType<Button>().Single(b=>b.Content?.ToString()=="软删除整次记录").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));}_=Accept();};
            await MenuAction("记录操作 ⋯","删除记录…");owner.TestDialogShown=null;await Wait(()=>d.Records(includeDeleted:true).Any(r=>r.Deleted));
            var deleted=d.Records(includeDeleted:true).Single(r=>r.Deleted);await m.RunAsync(()=>d.RestoreRecord(deleted.Id,deleted.Version),"隔离恢复");Check(d.Records().Sum(r=>r.DurationMs)==raw,"menu soft delete and restore preserve canonical duration");
            m.Page=4;m.StatsRange("day");Check(!owner.StatsAdvancedExpander.IsExpanded,"one collapsed advanced analytics area");await Shot("data-light-1200");m.DarkTheme=true;owner.Width=900;owner.Height=600;await Shot("data-dark-900");
            ButtonBy("历史学习记录 →").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Wait(()=>m.Page==6);Check(true,"primary data history button opens records");m.Page=4;
            m.DarkTheme=false;owner.Width=1200;owner.Height=800;owner.StatsAdvancedExpander.IsExpanded=true;m.StatsView=1;owner.UpdateLayout();await Task.Delay(100);
            Check(owner.DailyStack.IsVisible&&owner.SubjectDonut.IsVisible&&Children(owner.StatsAdvancedExpander).OfType<Button>().Any(b=>b.Content?.ToString()=="学习复盘"),"advanced tabs retain primary charts and review entry");
            owner.StatsAdvancedExpander.BringIntoView();await Shot("detailed-analysis-light-1200");owner.StatsAdvancedExpander.IsExpanded=false;
            m.Page=7;owner.MainScroll.ScrollToTop();var pages=new[]{("历史学习记录",6),("备考历程与日历",1),("薄弱知识点",3),("设置、备份与恢复",5),("参考目录与知识包",2)};
            foreach(var (label,page) in pages){m.Page=7;ButtonBy(label).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Wait(()=>m.Page==page);Check(true,"more entry reachable "+label);}
            m.Page=7;Check(ButtonBy("计划与任务").IsEnabled,"legacy tasks editor entry retained");await Shot("more-light-1200");
            var backup=Path.Combine(output,"interaction.yanjibak");d.Backup(backup);d.ValidateBackup(backup);Check(d.Integrity()=="ok","isolated backup validation and integrity");
            Check(d.DailyTotals(m.LocalToday(),m.LocalToday()).Sum(x=>x.DurationMs)==raw,"exact millisecond statistics unchanged");
            if(app.PolishCheck)Check(await PolishUiChecks.RunAsync(owner,output),"polish shortcuts, persistence failure and button states");
            Write("summary.json",new{RawMilliseconds=raw,Records=d.Records().Count,Progress=d.ProgressNotes(),Schema=d.Diagnostics()});
            Write("result.json",new{Status=failed?"FAIL":"PASS",Checks=checks.Count,Images=images.Count,Samples=samples.Count,NotTested=new[]{"physical IME candidates","other physical DPI","sleep hibernate restart","clean-machine offline","real user upgrade","screen reader","long soak"}});app.ValidationFailed=failed;
        }
        catch(Exception ex){Write("failure.json",new{Error=ex.ToString()});app.ValidationFailed=true;}
        finally{owner.TestDialogShown=null;owner.SmokeClose();}
    }
}
