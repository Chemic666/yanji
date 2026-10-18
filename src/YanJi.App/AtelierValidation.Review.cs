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
using System.Windows.Media;
using System.Windows.Threading;
using YanJi.Core;
namespace YanJi.App;
internal static partial class AtelierValidation
{
 internal static async Task ReviewAsync(MainWindow w,string output)
 {
  var app=(App)Application.Current;var m=w.Model;var d=m.Database;
  var shots=new List<object>();var checks=new List<object>();var samples=new List<object>();var pulses=new List<(double At,double Gap,int Phase)>();var actions=new List<object>();DispatcherTimer? pulse=null;Window? backing=null;
  void Write(string name,object v)=>File.WriteAllText(Path.Combine(output,name),JsonSerializer.Serialize(v,new JsonSerializerOptions{WriteIndented=true}));
  void Check(bool ok,string name){checks.Add(new{Name=name,Status=ok?"PASS":"FAIL"});Write("checks.json",checks);if(!ok)throw new InvalidOperationException(name);}
  async Task Theme(bool dark){m.DarkTheme=dark;while(m.Busy)await Task.Delay(25);await Task.Delay(50);}
  async Task Shot(Window window,string name){window.UpdateLayout();await Task.Delay(180);var f=name+".png";NativeCapture.Save(window,Path.Combine(output,f));shots.Add(new{File=f,Bytes=new FileInfo(Path.Combine(output,f)).Length,Sha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(output,f)))),Width=window.ActualWidth,Height=window.ActualHeight,Dark=m.DarkTheme,Dpi=VisualTreeHelper.GetDpi(window).PixelsPerInchX});Write("screenshots.json",shots);}
  string Mini()=>Children(w.MiniTimer!).OfType<TextBlock>().Single(t=>System.Windows.Data.BindingOperations.GetBinding(t,TextBlock.TextProperty)?.Path.Path=="TimerText").Text;
  try{
   await Theme(false);m.Page=0;await Shot(w,"empty-light-1200");
   d.SaveChapter(new("atelier-node","math","一元函数微分学 · 导数的概念与几何意义"));var day=m.LocalToday();
   foreach(var item in new[]{("math",5400123L),("english",1800321L),("408",3600757L)})d.SaveRecord(new("atelier-"+item.Item1,item.Item1,item.Item1=="math"?"atelier-node":null,SessionSource.Manual,"隔离视觉验收 · 只记录实际投入",[TimeParts.Duration(day,TimeSpan.FromMilliseconds(item.Item2),m.ZoneId)]),"isolated atmosphere review");
   m.Refresh();m.TimerSubject=m.Subjects.Single(s=>s.Id=="math");m.TimerChapter=m.TimerChapters.Single(c=>c.Id=="atelier-node");var fixture=d.Records().Sum(r=>r.DurationMs);
   foreach(var dark in new[]{false,true})foreach(var size in new[]{(1200,800),(900,600)}){
    await Theme(dark);w.Width=size.Item1;w.Height=size.Item2;w.MainScroll.ScrollToTop();await Shot(w,$"today-{(dark?"dark":"light")}-{size.Item1}");
    Check(w.TimerDigits.IsVisible&&w.AtelierStart.IsVisible&&w.AtelierStart.ActualWidth>=170,"timer and primary visible "+dark+"/"+size.Item1);
    Check(w.StudioTodayTitle.DesiredSize.Width<=w.AtelierHeading.ActualWidth+1,"Chinese title fits "+dark+"/"+size.Item1);
   }
   await Theme(false);w.Width=900;w.Height=600;
   foreach(var mode in new[]{1,2}){var radio=Children(w).OfType<RadioButton>().Single(b=>b.GroupName=="AtelierMode"&&b.Content?.ToString()==(mode==1?"倒计时":"番茄钟"));radio.SetCurrentValue(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty,true);await Task.Delay(50);Check(m.FocusModeIndex==mode&&w.FocusModeSelector.SelectedIndex==mode,"native radio uses existing focus mode "+mode);await Shot(w,$"mode-{mode}-900");}
   m.BreakActive=true;w.UpdateLayout();Check(w.AtelierPause.IsVisible,"break state retains pause control when study timer is inactive");m.BreakActive=false;
   m.FocusModeIndex=0;w.Width=1200;w.Height=800;w.SetAtmosphere(0);await Shot(w,"background-off");w.SetAtmosphere(1);
   w.AtelierStart.Focus();await Shot(w,"primary-focused");Check(w.AtelierStart.IsKeyboardFocused,"primary retains real keyboard focus");
   var menu=new ContextMenu{PlacementTarget=w.AtelierStart};foreach(var label in new[]{"自然光","柔和光","纯色"})menu.Items.Add(new MenuItem{Header=label});menu.IsOpen=true;await Task.Delay(100);NativeCapture.SavePopup(menu,Path.Combine(output,"surface-menu.png"));menu.IsOpen=false;
   for(int i=0;i<3;i++){w.MainScroll.ScrollToVerticalOffset((i+1)*480);await Shot(w,"today-scroll-"+i);}w.MainScroll.ScrollToTop();
   backing=new Window{Owner=w,Title="研迹 · 透明材质隔离背景",Width=620,Height=390,Left=w.Left+520,Top=w.Top+160,Background=new SolidColorBrush(Color.FromRgb(232,235,221)),Content=new Border{Focusable=true}};backing.Show();
   w.ShowMiniTimer();var mini=w.MiniTimer!;mini.Left=backing.Left+80;mini.Top=backing.Top+50;mini.Topmost=true;
   var miniChecks=new List<object>();
   foreach(var dark in new[]{false,true}){
    await Theme(dark);backing.Background=new SolidColorBrush(dark?Color.FromRgb(27,39,33):Color.FromRgb(232,235,221));
    backing.Activate();((Border)backing.Content).Focus();await Task.Delay(150);await Shot(mini,"mini-"+(dark?"dark":"light")+"-idle");
    mini.Activate();Children(mini).OfType<Button>().First(b=>b.Command==m.StartCommand).Focus();await Task.Delay(150);await Shot(mini,"mini-"+(dark?"dark":"light")+"-focused");
    Check(mini.ToolbarHint.Visibility==Visibility.Hidden&&mini.Toolbar.IsKeyboardFocusWithin,"mini hint hidden while toolbar active "+dark);
    Check(Children(mini).OfType<TextBlock>().Count(t=>System.Windows.Data.BindingOperations.GetBinding(t,TextBlock.TextProperty)?.Path.Path=="TimerText")==1,"one clock text in mini "+dark);
    miniChecks.Add(new{Dark=dark,Hint=mini.ToolbarHint.Visibility.ToString(),ToolbarOpacity=mini.Toolbar.Opacity,ForegroundOpacity=1,OpaqueToolbar=((SolidColorBrush)mini.Toolbar.Background).Color.A});
   }
   Write("mini-layout.json",miniChecks);await Theme(false);backing.Hide();mini.Hide();
   if(app.SurfaceCaptureOnly){
    // Visual-only acceptance: one short real segment supplies a genuine saved state.
    // No long performance rerun; core/timing code is byte-identical to rc.quiet.2.
    m.FocusModeIndex=0;await m.StartCommand.ExecuteAsync(null);var visible=new List<string>();
    for(int second=0;second<4;second++){await Task.Delay(1000);visible.Add(w.TimerDigits.Text);Check(w.TimerDigits.Text==Mini(),"visual timer sample agrees with mini "+second);}
    await m.PauseResumeCommand.ExecuteAsync(null);var held=m.Timer.DurationMs;await Task.Delay(1100);Check(m.Timer.DurationMs==held,"short visual pause preserves raw time");await m.FinishCommand.ExecuteAsync(null);
    Check(visible.Distinct().Count()>=3&&d.Records().Single(r=>r.Id==m.Timer.SessionId).DurationMs==m.Timer.DurationMs&&m.TodayDurationMs==fixture+m.Timer.DurationMs,"short visual timer refresh and exact saved milliseconds");
    Write("visual-timer.json",new{Samples=visible,SavedMs=m.Timer.DurationMs,TodayMs=m.TodayDurationMs,FixtureMs=fixture,Scope="short visual smoke only;126s unchanged RC2 evidence reused"});
    foreach(var dark in new[]{false,true})foreach(var size in new[]{(1200,800),(900,600)}){
     await Theme(dark);w.Width=size.Item1;w.Height=size.Item2;w.MainScroll.ScrollToTop();await Shot(w,$"saved-{(dark?"dark":"light")}-{size.Item1}");
     var y=w.TodayRecordList.TranslatePoint(new Point(),w.MainScroll).Y+w.MainScroll.VerticalOffset;w.MainScroll.ScrollToVerticalOffset(Math.Max(0,y-48));await Shot(w,$"records-{(dark?"dark":"light")}-{size.Item1}");
    }
    w.Width=1200;w.Height=800;w.MainScroll.ScrollToTop();await Theme(false);
   }
   if(!app.SurfaceCaptureOnly){
    w.MainScroll.ScrollToTop();m.FocusModeIndex=0;var saves=m.TimerSaveCount;await m.StartCommand.ExecuteAsync(null);Check(m.TimerRunning,"free timer starts with new Today view");
    var clock=Stopwatch.StartNew();var last=Stopwatch.GetTimestamp();var phase=0;w.SetAtmosphere(0);PerfTrace.Events.Clear();PerfTrace.Enabled=true;
    pulse=new DispatcherTimer(DispatcherPriority.Input){Interval=TimeSpan.FromMilliseconds(20)};pulse.Tick+=(_,_)=>{var now=Stopwatch.GetTimestamp();pulses.Add((clock.Elapsed.TotalMilliseconds,Stopwatch.GetElapsedTime(last,now).TotalMilliseconds,phase));last=now;};pulse.Start();
    mini.Show();bool sync=true,paused=false;var displays=new List<string>();
    while(clock.Elapsed.TotalSeconds<126){
     await Task.Delay(1000);var main=w.TimerDigits.Text;var floating=Mini();sync&=main==floating;displays.Add(main);samples.Add(new{AtMs=clock.Elapsed.TotalMilliseconds,Main=main,Mini=floating,RawMs=m.Timer.DurationMs,Phase=phase,Page=m.Page,Checkpoints=m.TimerSaveCount});
     if(!paused&&clock.Elapsed.TotalSeconds>=20){var watch=Stopwatch.StartNew();await m.PauseResumeCommand.ExecuteAsync(null);actions.Add(new{Name="pause",Ms=watch.Elapsed.TotalMilliseconds});var held=m.Timer.DurationMs;var heldText=w.TimerDigits.Text;await Task.Delay(3000);Check(m.Timer.DurationMs==held&&w.TimerDigits.Text==heldText,"three seconds paused changes neither raw time nor display");watch.Restart();await m.PauseResumeCommand.ExecuteAsync(null);actions.Add(new{Name="resume",Ms=watch.Elapsed.TotalMilliseconds});paused=true;}
     if(phase==0&&clock.Elapsed.TotalSeconds>=63){phase=1;w.SetAtmosphere(1);}
     if(clock.Elapsed.TotalSeconds>=88&&clock.Elapsed.TotalSeconds<89){m.Page=2;await w.SimpleProgressView.RefreshAsync();}
     if(clock.Elapsed.TotalSeconds>=102&&clock.Elapsed.TotalSeconds<103)m.Page=4;
     if(clock.Elapsed.TotalSeconds>=113&&clock.Elapsed.TotalSeconds<114){m.Page=0;w.MainScroll.ScrollToTop();}
    }
    pulse.Stop();PerfTrace.Enabled=false;var spans=PerfTrace.Events.ToArray();var watchEnd=Stopwatch.StartNew();await m.FinishCommand.ExecuteAsync(null);actions.Add(new{Name="finish",Ms=watchEnd.Elapsed.TotalMilliseconds});
    Check(sync&&displays.Distinct().Count()>110,"real per-second display and main/mini synchronization over126s");
    Check(m.TimerSaveCount-saves>=7&&spans.Count(e=>e.Name=="Refresh.UI")==1,"15 second background saves do not rebuild the page; explicit pause refresh only");
    Check(d.Records().Single(r=>r.Id==m.Timer.SessionId).DurationMs==m.Timer.DurationMs&&m.TodayDurationMs==fixture+m.Timer.DurationMs,"timer persisted and aggregate raw milliseconds reconcile exactly");
    Check(m.FocusSubjectLabel.Contains("已保存")&&m.FocusStartLabel=="开始下一段","completed segment is labelled saved before next start");
    m.Page=0;mini.Hide();await Shot(w,"completed-study");
    object Summary(int p){var a=pulses.Where(x=>x.Phase==p).Select(x=>x.Gap).Order().ToArray();return new{Phase=p,Background=p==1?"natural light":"off",Samples=a.Length,MaxMs=a.Max(),P95Ms=a[(int)(a.Length*.95)],Over50=a.Count(x=>x>50)};}
    Write("performance.json",new{ObservedMs=clock.Elapsed.TotalMilliseconds,UiThread=Environment.CurrentManagedThreadId,MainMiniSynchronized=sync,Checkpoints=m.TimerSaveCount-saves,RefreshCount=spans.Count(e=>e.Name=="Refresh.UI"),Phases=new[]{Summary(0),Summary(1)},Actions=actions,Spans=spans,Pulses=pulses.Select(x=>new{x.At,x.Gap,x.Phase}),PeakWorkingSet=Process.GetCurrentProcess().PeakWorkingSet64});Write("timer-samples.json",samples);
    foreach(var mode in new[]{1,2}){m.FocusModeIndex=mode;m.FocusMinutes="1";await m.StartCommand.ExecuteAsync(null);var first=w.TimerDigits.Text;await Task.Delay(3200);Check(w.TimerDigits.Text!=first&&w.TimerDigits.Text==Mini(),"remaining time has real second updates mode "+mode);await m.FinishCommand.ExecuteAsync(null);Check(d.Records().Single(r=>r.Id==m.Timer.SessionId).DurationMs==m.Timer.DurationMs,"exact saved raw milliseconds mode "+mode);}
   }
   Check(d.Integrity()=="ok"&&d.Records().Where(r=>r.Id.StartsWith("atelier-")).Sum(r=>r.DurationMs)==fixture,"original synthetic facts remain unchanged and SQLite intact");
   m.FocusModeIndex=0;m.Page=0;w.SetAtmosphere(1);w.MainScroll.ScrollToTop();await Theme(false);
   Write("result.json",new{Status="PASS",Checks=checks.Count,Preview=app.SurfaceCaptureOnly,PhysicalIME="NOT TESTED",OtherDpi="NOT TESTED",Dpi=VisualTreeHelper.GetDpi(w).PixelsPerInchX,FixtureMs=fixture});
   if(!app.SurfaceCaptureOnly)Polish2Validation.ShowButtonLab(w,output,"AtelierPrimary");
   backing.Show();w.ShowMiniTimer();mini.Topmost=true;backing.Activate();((Border)backing.Content).Focus();
   Write("ready.json",new{Process=Environment.ProcessId,Status="READY FOR EXTERNAL WGC AND POINTER VALIDATION"});
   while(!File.Exists(Path.Combine(output,"continue.request")))await Task.Delay(250);
   backing.Close();w.SmokeClose();
  }catch(Exception e){app.ValidationFailed=true;Write("failure.json",new{Error=e.ToString()});Write("result.json",new{Status="FAIL",Checks=checks.Count});pulse?.Stop();PerfTrace.Enabled=false;backing?.Close();w.SmokeClose();}
 }
}
