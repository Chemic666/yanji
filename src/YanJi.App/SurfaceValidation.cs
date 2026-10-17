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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using YanJi.Core;
namespace YanJi.App;
/// <summary>Owned-data surface review: real HWNDs, unchanged layout, actual focus and external pointer specimen.</summary>
internal static class SurfaceValidation
{
    internal static async Task RunAsync(MainWindow w,string output)
    {
        var m=w.Model;var d=m.Database;var app=(App)Application.Current;Directory.CreateDirectory(output);var shots=new List<object>();var checks=new List<object>();
        void Write(string f,object v)=>File.WriteAllText(Path.Combine(output,f),JsonSerializer.Serialize(v,new JsonSerializerOptions{WriteIndented=true}));
        void Check(bool ok,string name){checks.Add(new{Name=name,Status=ok?"PASS":"FAIL"});Write("checks.json",checks);if(!ok)throw new InvalidOperationException(name);}
        async Task Shot(Window window,string name){window.UpdateLayout();await Task.Delay(160);var file=name+".png";NativeCapture.Save(window,Path.Combine(output,file));shots.Add(new{File=file,Bytes=new FileInfo(Path.Combine(output,file)).Length,Sha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(output,file)))),Width=window.ActualWidth,Height=window.ActualHeight,Dark=m.DarkTheme,Dpi=VisualTreeHelper.GetDpi(window).PixelsPerInchX,Page=m.Page});Write("screenshots.json",shots);}
        try
        {
            Write("provenance.json",new{Assembly=typeof(App).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,DllSha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(App).Assembly.Location))),Process=Environment.ProcessId,DataDirectory=d.DirectoryPath,Before=app.SurfaceBefore,CaptureOnly=app.SurfaceCaptureOnly,RenderMode=RenderOptions.ProcessRenderMode.ToString(),Method="real HWND PrintWindow; actual keyboard focus; separate real pointer state specimen"});
            await w.ApplySimplePresetsAsync(false);var day=m.LocalToday();
            foreach(var subject in new[]{"math","english","408"})
            {
                var node=d.KnowledgeNodes(subject).First(n=>n.Kind==KnowledgeNodeKind.KnowledgePoint&&!n.Archived);
                d.SaveProgressNote(node.Id,node.Version,LearningState.Learning,"已学习基础概念，整理了需要复习的内容。","完成下一节练习并核对薄弱点。");
                d.SaveRecord(new("surface-"+subject,subject,node.Id,SessionSource.Manual,"隔离视觉验收",[TimeParts.Duration(day,TimeSpan.FromMilliseconds(subject=="math"?5400123:subject=="english"?1800321:3600757),"Asia/Shanghai")]),"isolated surface fixture");
            }
            for(int i=1;i<=12;i++)d.SaveRecord(new("surface-history-"+i,new[]{"math","english","408"}[i%3],null,SessionSource.Manual,"合成历史学习",[TimeParts.Duration(day.AddDays(-i),TimeSpan.FromMinutes(30+i*2),"Asia/Shanghai")]),"isolated surface fixture");
            m.Refresh();await m.CalendarTodayCommand.ExecuteAsync(null);m.StatsRange("month");var raw=d.Records().Sum(r=>r.DurationMs);
            foreach(var dark in new[]{false,true})foreach(var size in new[]{(1200,800),(900,600)})
            {
                m.DarkTheme=dark;while(m.Busy)await Task.Delay(25);w.Width=size.Item1;w.Height=size.Item2;
                foreach(var page in new[]{0,2,4}){m.Page=page;if(page==2)await w.SimpleProgressView.RefreshAsync();w.MainScroll.ScrollToTop();await Shot(w,$"{(page==0?"today":page==2?"progress":"data")}-{(dark?"dark":"light")}-{size.Item1}");}
            }
            w.Width=1200;w.Height=800;m.DarkTheme=false;while(m.Busy)await Task.Delay(25);m.Page=4;w.LearningCalendar.BringIntoView();await Shot(w,"calendar-closeup");
            Check(w.LearningCalendar.IsVisible&&w.ChapterAnalysis.Visibility==Visibility.Visible&&w.StudyReviewSection.Visibility==Visibility.Visible,"calendar and analytics remain directly displayed");
            m.Page=0;w.MainScroll.ScrollToTop();
            var panel=new StackPanel{Margin=new Thickness(24),Focusable=true};panel.Children.Add(new TextBlock{Text="输入与菜单 · 共用样式",Style=(Style)w.FindResource("Subheading")});
            var field=new TextBox{Text="函数与极限 · 中文输入",Style=(Style)w.FindResource("FocusTextBox")};panel.Children.Add(field);
            var multiline=new TextBox{Text="下一步整理错因\n再进行一组复习",AcceptsReturn=true,Height=80,Style=(Style)w.FindResource("FocusTextBox")};panel.Children.Add(multiline);
            var combo=new ComboBox{ItemsSource=new[]{"数学","英语","408"},SelectedIndex=0,Style=(Style)w.FindResource("FocusComboBox")};panel.Children.Add(combo);
            var editor=new Window{Owner=w,Title="研迹 · 输入边界验收",Width=560,Height=390,Content=panel,WindowStartupLocation=WindowStartupLocation.CenterOwner};
            editor.Show();editor.Activate();panel.Focus();await Shot(editor,"input-default");field.Focus();await Task.Delay(100);Check(field.IsKeyboardFocusWithin,"text input actual keyboard focus is retained");await Shot(editor,"input-focused");
            combo.IsDropDownOpen=true;await Task.Delay(160);var popup=combo.Template.FindName("PART_Popup",combo) as System.Windows.Controls.Primitives.Popup;if(popup?.Child is FrameworkElement child){NativeCapture.SavePopup(child,Path.Combine(output,"combo-popup.png"));Check(popup.IsOpen,"native combo popup remains usable");}combo.IsDropDownOpen=false;
            var menu=new ContextMenu{PlacementTarget=field};menu.Items.Add(new MenuItem{Header="查看记录"});menu.Items.Add(new MenuItem{Header="编辑备注"});menu.IsOpen=true;await Task.Delay(160);NativeCapture.SavePopup(menu,Path.Combine(output,"context-menu.png"));menu.IsOpen=false;editor.Close();
            if(!app.SurfaceBefore&&!app.SurfaceCaptureOnly)
            {
                var normal=(Style)w.FindResource("FocusButton");var button=new Button{Style=normal};Check(button.BorderThickness==new Thickness(0),"normal button has no decorative perimeter stroke");
                // A short focused integration run; unchanged timer engine has separate prior126s evidence.
                m.FocusModeIndex=0;await m.StartCommand.ExecuteAsync(null);w.ShowMiniTimer();var sample=new List<object>();var clocks=Stopwatch.StartNew();PerfTrace.Events.Clear();PerfTrace.Enabled=true;
                while(clocks.Elapsed.TotalSeconds<32){await Task.Delay(1000);sample.Add(new{AtMs=clocks.Elapsed.TotalMilliseconds,m.TimerText,RawMs=m.Timer.DurationMs});if(sample.Count%7==0){m.Page=4;await m.PreviousMonthCommand.ExecuteAsync(null);await m.CalendarTodayCommand.ExecuteAsync(null);}}
                PerfTrace.Enabled=false;var spans=PerfTrace.Events.ToArray();Check(spans.All(e=>e.Name!="Refresh.UI"),"15 second checkpoints do not rebuild entire UI with new styles");await m.PauseResumeCommand.ExecuteAsync(null);var held=m.Timer.DurationMs;await Task.Delay(1100);Check(m.Timer.DurationMs==held,"pause still excludes idle time");await m.FinishCommand.ExecuteAsync(null);Check(d.Records().Single(r=>r.Id==m.Timer.SessionId).DurationMs==m.Timer.DurationMs,"saved milliseconds remain exact");w.MiniTimer?.Close();
                Write("timer-check.json",new{Status="PASS",ObservedMs=clocks.Elapsed.TotalMilliseconds,Samples=sample,Spans=spans,FullRefreshDuringCheckpointCycles=spans.Count(e=>e.Name=="Refresh.UI")});
            }
            Check(d.Integrity()=="ok"&&d.Records().Where(r=>r.Id.StartsWith("surface-")).Sum(r=>r.DurationMs)==raw,"original synthetic facts and SQLite integrity unchanged");
            Write("result.json",new{Status="PASS",Checks=checks.Count,RawFixtureMs=raw,PhysicalIME="NOT TESTED",OtherSystemDpi="NOT TESTED"});
            m.Page=0;m.DarkTheme=false;w.MainScroll.ScrollToTop();Polish2Validation.ShowButtonLab(w,output);
            Write("ready.json",new{Status="READY FOR EXTERNAL POINTER STATES",Process=Environment.ProcessId});
        }
        catch(Exception ex){app.ValidationFailed=true;Write("failure.json",new{Error=ex.ToString()});w.SmokeClose();}
    }
}
