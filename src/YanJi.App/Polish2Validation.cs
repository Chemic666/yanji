using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Data.Sqlite;
using YanJi.Core;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
namespace YanJi.App;
internal static class Polish2Validation
{
    internal static async Task ProbeAsync(MainWindow main,string output)
    {
        Directory.CreateDirectory(output);
        var catalog=new KnowledgePackageWindow(main);catalog.Show();await catalog.Initialization;await Task.Delay(150);
        File.WriteAllText(Path.Combine(output,"probe.json"),JsonSerializer.Serialize(new{Catalog=catalog.Model.Catalog.Select(c=>new{c.Name,c.Package.PackageId,Nodes=c.Package.Nodes.Count}),AllFiveVisible=catalog.Model.Catalog.Count(c=>new[]{"org.yanji.reference.math1","org.yanji.reference.math2","org.yanji.reference.408","org.yanji.reference.english1","org.yanji.reference.english2"}.Contains(c.Package.PackageId))==5},new JsonSerializerOptions{WriteIndented=true}));
        NativeCapture.Save(catalog,Path.Combine(output,"package-before.png"));catalog.Close();
        var panel=new StackPanel{Margin=new Thickness(30)};panel.Children.Add(new TextBlock{Text="Quiet Focus · Soft Depth",FontSize=22,Margin=new Thickness(0,0,0,24)});
        panel.Children.Add(new Button{Content="开始学习",Style=(Style)main.FindResource("Primary"),HorizontalAlignment=HorizontalAlignment.Left,Width=150});
        var w=new Window{Owner=main,Title="研迹 · 按钮验收",Width=600,Height=300,Content=panel,WindowStartupLocation=WindowStartupLocation.CenterOwner};w.Show();await Task.Delay(180);NativeCapture.Save(w,Path.Combine(output,"button-before.png"));w.Close();
        main.ShowMiniTimer();await Task.Delay(180);NativeCapture.Save(main.MiniTimer!,Path.Combine(output,"mini-before.png"));main.MiniTimer!.Close();
    }
    private static IEnumerable<DependencyObject> Children(DependencyObject parent)
    {for(var i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++){var c=VisualTreeHelper.GetChild(parent,i);yield return c;foreach(var d in Children(c))yield return d;}}
    private static async Task Wait(Func<bool> condition)
    {var clock=Stopwatch.StartNew();while(!condition()&&clock.ElapsedMilliseconds<12000)await Task.Delay(30);if(!condition())throw new TimeoutException("RC-Polish.2 UI condition timed out");}
    internal static async Task RunAsync(MainWindow main,string output)
    {
        Directory.CreateDirectory(output);var m=main.Model;var d=m.Database;var app=(App)Application.Current;var checks=new List<object>();bool passed=true;
        void Write(string file,object data)=>File.WriteAllText(Path.Combine(output,file),JsonSerializer.Serialize(data,new JsonSerializerOptions{WriteIndented=true}));
        void Check(bool ok,string name){passed&=ok;checks.Add(new{Name=name,Status=ok?"PASS":"FAIL"});Write("checks.json",checks);}
        async Task Shot(Window w,string name){w.UpdateLayout();await Task.Delay(180);NativeCapture.Save(w,Path.Combine(output,name+".png"));}
        try
        {
            Write("provenance.json",new{Assembly=Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,DllSha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(App).Assembly.Location))),Data=d.DirectoryPath,Dpi=VisualTreeHelper.GetDpi(main).PixelsPerInchX,Method="real WPF HWND and controls; in-process routed commands, separate external OS input evidence"});
            main.Width=1200;main.Height=800;
            var packages=new KnowledgePackageWindow(main);packages.Show();await packages.Initialization;
            Check(packages.Model.Catalog.Count(c=>new[]{"org.yanji.reference.math1","org.yanji.reference.math2","org.yanji.reference.408","org.yanji.reference.english1","org.yanji.reference.english2"}.Contains(c.Package.PackageId))==5,"fresh archive shows mathematics1/2, English1/2 and408 together");
            await Shot(packages,"packages-all-new");var item=packages.Model.Catalog.Single(c=>c.Package.PackageId=="org.yanji.reference.408");packages.Model.SelectedCatalog=item;
            var old=KnowledgePackageFile.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"knowledge-packages/org.yanji.reference.408/1.0.0/package.yanji-knowledge.json")));
            Check(item.Package.Nodes.Count==177&&old.Nodes.All(o=>item.Package.Nodes.Any(n=>n.Id==o.Id)),"408 has177 nodes preserving all145 prior external IDs");
            await packages.Model.PreviewCommand.ExecuteAsync(null);Check(packages.Model.CanApply,"408 preview targets explicit profile andsubject");await Shot(packages,"408-preview");
            packages.Model.Confirm=message=>
            {
                var confirm=new ConfirmDialog(packages,"确认知识目录变更",message,"确认应用",false);
                async Task Accept(){await Task.Delay(100);NativeCapture.Save(confirm,Path.Combine(output,"408-confirm.png"));Children(confirm).OfType<Button>().Single(b=>b.Content?.ToString()=="确认应用").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));}
                _=Accept();return Task.FromResult(confirm.ShowDialog()==true);
            };
            await packages.Model.ApplyCommand.ExecuteAsync(null);Check(d.KnowledgeNodes("408").Count==177&&d.InstalledPackages().Count==1,"real file SQLite installs408 once after preview confirmation");
            var node=d.KnowledgeNodes("408").First(n=>n.Kind==KnowledgeNodeKind.KnowledgePoint);var identity=node.Id;
            d.SaveKnowledgeNodes([node with{Name=node.Name+" · 本地备注保护"}],"isolated local rename");
            d.SaveRecord(new("polish2-linked","408",identity,SessionSource.Manual,"408 isolation linkage",[TimeParts.Duration(m.LocalToday(),TimeSpan.FromMilliseconds(30123),m.ZoneId)]),"isolated408 study");
            packages.Model.TabIndex=0;packages.Model.SelectedCatalog=packages.Model.Catalog.Single(c=>c.Package.PackageId==item.Package.PackageId);Check(packages.Model.HasInstalledPackage&&packages.Model.SelectedCatalog.InstallStatus.Contains("已安装"),"installed408 remains visible with directory action");await Shot(packages,"408-installed");
            await packages.Model.PreviewCommand.ExecuteAsync(null);Check(!packages.Model.CanApply,"identical408 reinstall is no-op");
            Check(d.KnowledgeNodes("408").Count==177&&d.KnowledgeNodes().Single(n=>n.Id==identity).Name.EndsWith("本地备注保护")&&d.Records().Single().DurationMs==30123&&d.Records().Single().ChapterId==identity,"repeat preview preserves local modification, stable IDs and exact study facts");
            packages.Model.FilterSubject=packages.Model.CatalogSubjects.Single(s=>s.Id=="english");Check(packages.Model.Catalog.Count(c=>c.Package.PackageId.Contains("english"))==2&&!packages.Model.Catalog.Any(c=>c.Package.PackageId==item.Package.PackageId),"optional explicit subject filter works without deletion");
            packages.Model.FilterSubject=packages.Model.CatalogSubjects[0];main.Width=900;main.Height=600;packages.Width=900;packages.Height=600;m.DarkTheme=true;packages.Model.TabIndex=0;await Shot(packages,"packages-dark-900");packages.Close();m.DarkTheme=false;main.Width=1200;main.Height=800;m.Refresh();
            main.ShowMiniTimer();var mini=main.MiniTimer!;Check(mini.AllowsTransparency&&mini.WindowStyle==WindowStyle.None&&mini.Opacity==1&&((SolidColorBrush)mini.FloatingPanel.Background).Color.A is >0 and <255,"layered mini has translucent surface and opaque content");
            await Shot(mini,"mini-light");m.DarkTheme=true;await Shot(mini,"mini-dark");m.DarkTheme=false;
            var firstLeft=mini.Left;mini.Left=Math.Max(SystemParameters.WorkArea.Left,firstLeft-30);mini.Top+=12;mini.Topmost=false;await mini.PersistPreferencesAsync();
            var restored=new MiniTimerWindow(main);Check(Math.Abs(restored.Left-mini.Left)<1&&Math.Abs(restored.Top-mini.Top)<1&&!restored.Topmost,"mini position and pin preference reopen correctly");restored.ClosingForExit=true;restored.Close();
            var samples=new List<object>();
            foreach(var mode in new[]{0,1,2})
            {
                await Wait(()=>!m.Busy);m.FocusModeIndex=mode;m.FocusMinutes="1";m.BreakMinutes="1";m.TimerSubject=m.Subjects.Single(s=>s.Id=="408");m.TimerChapter=m.TimerChapters.FirstOrDefault(c=>c.Id==identity);await m.StartCommand.ExecuteAsync(null);Check(m.TimerRunning,"start focus mode"+mode);
                for(int i=0;i<3;i++){var before=m.Timer.DurationMs/1000;await Wait(()=>m.Timer.DurationMs/1000>before);var digits=Children(mini).OfType<TextBlock>().Single(t=>t.Name==""&&System.Windows.Data.BindingOperations.GetBinding(t,TextBlock.TextProperty)?.Path.Path=="TimerText").Text;samples.Add(new{Mode=mode,Utc=DateTimeOffset.UtcNow,Main=main.TimerDigits.Text,Mini=digits,RawMs=m.Timer.DurationMs});Check(main.TimerDigits.Text==digits,"actual main/mini second sample "+mode+"/"+i);}
                await m.PauseResumeCommand.ExecuteAsync(null);var pause=m.Timer.DurationMs;await Task.Delay(1100);Check(m.Timer.DurationMs==pause,"pause excludes elapsed time"+mode);await m.PauseResumeCommand.ExecuteAsync(null);
                if(mode==0)
                {
                    var seen=0;main.TestDialogShown=w=>{seen++;async Task Act(){await Task.Delay(100);NativeCapture.Save(w,Path.Combine(output,"exit-choices.png"));Children(w).OfType<Button>().Single(b=>b.Content?.ToString()=="取消").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));}_=Act();};
                    main.Close();main.Close();await Wait(()=>seen>0);await Task.Delay(180);Check(main.IsVisible&&m.TimerRunning&&seen==1,"close and duplicate close show one cancelable choice; timer keeps running");
                    main.TestDialogShown=w=>{async Task Act(){await Task.Delay(90);Children(w).OfType<Button>().Single(b=>b.Content?.ToString()=="继续在后台运行").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));}_=Act();};main.Close();await Wait(()=>!main.IsVisible);Check(m.TimerRunning&&main.ValidationTickEnabled,"continue background preserves timer and checkpoint tick");main.ShowFromTray();main.TestDialogShown=null;
                    mini.Close();Check(!mini.IsVisible&&m.TimerRunning,"mini close only hides and preserves timer");main.ShowMiniTimer();
                    using(var c=new SqliteConnection($"Data Source={d.DatabasePath};Pooling=False")){c.Open();using var cmd=c.CreateCommand();cmd.CommandText="CREATE TRIGGER polish2_fail BEFORE INSERT ON parts BEGIN SELECT RAISE(ABORT,'expected isolated exit failure'); END";cmd.ExecuteNonQuery();}
                    main.TestDialogShown=w=>{async Task Act(){await Task.Delay(80);Children(w).OfType<Button>().Single(b=>b.Content?.ToString()=="结束并保存后退出").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));}_=Act();};main.Close();await Wait(()=>m.NeedsReview);await Task.Delay(100);Check(main.IsVisible&&m.TimerActive,"failed SQLite exit save keeps process open and recoverable");
                    Write("expected-exit-failure.json",new{m.Status,State=m.Timer.State,Visible=main.IsVisible});
                    using(var c=new SqliteConnection($"Data Source={d.DatabasePath};Pooling=False")){c.Open();using var cmd=c.CreateCommand();cmd.CommandText="DROP TRIGGER polish2_fail";cmd.ExecuteNonQuery();}main.TestDialogShown=null;await m.ResolveAsync([]);
                }
                await m.FinishCommand.ExecuteAsync(null);Check(d.Records().Single(r=>r.Id==m.Timer.SessionId).DurationMs==m.Timer.DurationMs,"saved raw milliseconds agree mode"+mode);
            }
            Write("timer-samples.json",samples);await main.OpenKnowledgeAsync(identity);await Wait(()=>main.KnowledgeView.Model?.Selected?.Node.Id==identity&&!main.KnowledgeView.Model.KnowledgeBusy);var k=main.KnowledgeView.Model!;k.NodeName+=" · 未保存";
            bool warning=false;Window? unsaved=null;main.TestDialogShown=w=>{unsaved=w;warning=w.Title.Contains("未保存");async Task Cancel(){await Task.Delay(80);w.DialogResult=false;}_=Cancel();};main.Close();await Wait(()=>warning&&unsaved!=null&&!unsaved.IsVisible);await Task.Delay(80);Check(main.IsVisible&&k.HasUnsavedChanges,"closing warns about unsaved inline chapter and cancel preserves input");k.NodeName=k.Selected!.Node.Name;main.TestDialogShown=null;
            m.Page=0;main.MainScroll.ScrollToTop();await Shot(main,"today-light-1200");m.DarkTheme=true;main.Width=900;main.Height=600;await Shot(main,"today-dark-900");m.Page=2;await main.SimpleProgressView.RefreshAsync();await Shot(main,"progress-dark-900");m.DarkTheme=false;main.Width=1200;main.Height=800;m.Page=4;await Shot(main,"data-light-1200");
            Check(d.Integrity()=="ok"&&d.DailyTotals(m.LocalToday(),m.LocalToday()).Sum(t=>t.DurationMs)==d.Records().Sum(r=>r.DurationMs),"SQLite integrity and raw daily totals reconcile");
            var backup=Path.Combine(output,"isolated.yanjibak");d.Backup(backup);d.ValidateBackup(backup);Check(true,"existing complete backup validates with new UI preferences");
            Check(await PolishUiChecks.RunAsync(main,Path.Combine(output,"shortcuts")),"existing20 shortcut/IME composition/save failure checks");
            Write("result.json",new{Status=passed?"PASS":"FAIL",Checks=checks.Count,RawMs=d.Records().Sum(r=>r.DurationMs),Nodes408=d.KnowledgeNodes("408").Count,UserVersion=4,PhysicalIME="NOT TESTED"});app.ValidationFailed=!passed;
            main.Width=1200;main.Height=800;m.DarkTheme=false;m.Page=0;main.MainScroll.ScrollToTop();ShowButtonLab(main,output);Write("ready.json",new{Process=Environment.ProcessId,Status="READY FOR EXTERNAL WINDOWS INPUT",Data=d.DirectoryPath});
        }
        catch(Exception ex){Write("failure.json",new{Error=ex.ToString()});app.ValidationFailed=true;main.TestDialogShown=null;main.SmokeClose();}
    }
    private static void ShowButtonLab(MainWindow main,string output)
    {
        var root=new StackPanel{Margin=new Thickness(30),Focusable=true};root.Children.Add(new TextBlock{Text="Quiet Focus · Soft Depth",FontSize=22,Margin=new Thickness(0,0,0,24)});
        var sample=new Button{Content="开始学习",Style=(Style)main.FindResource("Primary"),Width=150,HorizontalAlignment=HorizontalAlignment.Left};System.Windows.Automation.AutomationProperties.SetAutomationId(sample,"StateSample");root.Children.Add(sample);
        var row=new WrapPanel{Margin=new Thickness(0,18,0,0)};
        var disable=new Button{Content="禁用 / 启用样本",Style=(Style)main.FindResource("SecondaryButton")};disable.Click+=(_,_)=>sample.IsEnabled=!sample.IsEnabled;row.Children.Add(disable);
        var dark=new Button{Content="浅色 / 深色",Style=(Style)main.FindResource("GhostButton")};dark.Click+=(_,_)=>{main.Model.DarkTheme=!main.Model.DarkTheme;root.Focus();root.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,new Action(()=>Capture("normal")));};row.Children.Add(dark);root.Children.Add(row);
        var material=new Button{Content="悬浮材质验收",Style=(Style)main.FindResource("GhostButton"),HorizontalAlignment=HorizontalAlignment.Left};root.Children.Add(material);
        material.Click+=(_,_)=>
        {
            bool darkBackground=false;var background=new Grid{Background=new SolidColorBrush(Color.FromRgb(243,233,211))};
            background.Children.Add(new TextBlock{Text="隔离背景 · A B C\n用于核对真实桌面透明叠加",FontSize=34,Foreground=new SolidColorBrush(Color.FromRgb(148,157,148)),Margin=new Thickness(30)});
            var swap=new Button{Content="切换背景明暗",VerticalAlignment=VerticalAlignment.Bottom,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(24),Style=(Style)main.FindResource("SecondaryButton")};background.Children.Add(swap);
            swap.Click+=(_,_)=>{darkBackground=!darkBackground;background.Background=new SolidColorBrush(darkBackground?Color.FromRgb(21,37,30):Color.FromRgb(243,233,211));};
            var backing=new Window{Owner=main,Title="研迹 · 悬浮材质验收",Width=640,Height=420,Content=background,WindowStartupLocation=WindowStartupLocation.CenterOwner};backing.Show();main.ShowMiniTimer();main.MiniTimer!.Topmost=true;main.MiniTimer.Left=backing.Left+80;main.MiniTimer.Top=backing.Top+100;
        };
        var lab=new Window{Owner=main,Title="研迹 · 按钮验收",Width=600,Height=300,Content=root,WindowStartupLocation=WindowStartupLocation.CenterOwner};
        var saved=new HashSet<string>();var events=new List<object>();
        void Capture(string state)
        {
            if(!lab.IsVisible||!saved.Add((ThemeService.Dark?"dark-":"light-")+state))return;
            lab.UpdateLayout();var file=(ThemeService.Dark?"dark-":"light-")+state+".png";NativeCapture.Save(lab,Path.Combine(output,file));events.Add(new{File=file,State=state,sample.IsPressed,sample.IsMouseOver,sample.IsKeyboardFocused,sample.IsEnabled,Utc=DateTimeOffset.UtcNow});File.WriteAllText(Path.Combine(output,"actual-button-states.json"),JsonSerializer.Serialize(events,new JsonSerializerOptions{WriteIndented=true}));
        }
        DependencyPropertyDescriptor.FromProperty(Button.IsPressedProperty,typeof(Button)).AddValueChanged(sample,(_,_)=>{if(sample.IsPressed)sample.Dispatcher.BeginInvoke(DispatcherPriority.Render,new Action(()=>{if(sample.IsPressed)Capture("pressed");}));});
        sample.MouseEnter+=async(_,_)=>{await Task.Delay(180);if(sample.IsMouseOver&&!sample.IsPressed)Capture("hover");};
        sample.IsKeyboardFocusedChanged+=(_,_)=>{if(sample.IsKeyboardFocused&&!sample.IsMouseOver)sample.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle,new Action(()=>Capture("focused")));};
        sample.IsEnabledChanged+=(_,_)=>{if(!sample.IsEnabled)sample.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle,new Action(()=>Capture("disabled")));};
        lab.Show();root.Focus();lab.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,new Action(()=>Capture("normal")));
    }

}
