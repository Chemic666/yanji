using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using YanJi.Core;
namespace YanJi.App;
/// <summary>Fresh V022 native batch; every actual image, including detail/popup, has metadata.</summary>
internal static class V022CaptureRunner
{
 public static async Task RunAsync(MainWindow w,string output)
 {
  var shots=new List<object>();Directory.CreateDirectory(output);
  try
  {
   if(Directory.EnumerateFileSystemEntries(output).Any())throw new InvalidOperationException("Fresh V022 capture directory required.");
   var vm=w.Model;var day=vm.LocalToday();var asm=typeof(App).Assembly;
   File.WriteAllText(Path.Combine(output,"provenance.json"),JsonSerializer.Serialize(new{Batch=Path.GetFileName(output),Synthetic=true,Anchor=day,ProcessId=Environment.ProcessId,Executable=Environment.ProcessPath,ExeSha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Environment.ProcessPath!))),Assembly=asm.GetName().Version?.ToString(),Source=asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,AssemblySha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(asm.Location)))},new JsonSerializerOptions{WriteIndented=true}));
   async Task Shot(string name,Window? target=null)
   {
    var win=target??w;win.UpdateLayout();await win.Dispatcher.InvokeAsync(()=>{},DispatcherPriority.ApplicationIdle);await Task.Delay(90);
    var file=name+".png";NativeCapture.Save(win,Path.Combine(output,file));var dpi=VisualTreeHelper.GetDpi(win);
    using var png=new System.Drawing.Bitmap(Path.Combine(output,file));
    shots.Add(new{File=file,Page=vm.Page,StatsView=vm.StatsView,Dark=vm.DarkTheme,WindowWidth=win.ActualWidth,WindowHeight=win.ActualHeight,PixelWidth=png.Width,PixelHeight=png.Height,DpiX=dpi.PixelsPerInchX,DpiY=dpi.PixelsPerInchY,Scroll=w.MainScroll.VerticalOffset,OwnerProcessId=Environment.ProcessId,OwnedHwnd=new WindowInteropHelper(win).Handle.ToInt64(),ObservedAt=DateTimeOffset.UtcNow,Sha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(output,file))))});
   }
   async Task PopupShot(string name,System.Windows.Controls.ToolTip popup)
   {
    await Task.Delay(60);var file=name+".png";NativeCapture.SavePopup(popup,Path.Combine(output,file));var dpi=VisualTreeHelper.GetDpi(popup);using var png=new System.Drawing.Bitmap(Path.Combine(output,file));
    shots.Add(new{File=file,Page=vm.Page,StatsView=vm.StatsView,Dark=vm.DarkTheme,WindowWidth=popup.ActualWidth,WindowHeight=popup.ActualHeight,PixelWidth=png.Width,PixelHeight=png.Height,DpiX=dpi.PixelsPerInchX,DpiY=dpi.PixelsPerInchY,OwnerProcessId=Environment.ProcessId,OwnedHwnd=(PresentationSource.FromVisual(popup) as HwndSource)!.Handle.ToInt64(),ObservedAt=DateTimeOffset.UtcNow,Sha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(output,file))))});
   }
   async Task Matrix(string state)
   {
    foreach(var dark in new[]{false,true})foreach(var size in new[]{(1200d,800d),(900d,600d)})
    {
     vm.DarkTheme=dark;w.Width=size.Item1;w.Height=size.Item2;w.UpdateLayout();
     var suffix=$"{(dark?"dark":"light")}-{size.Item1}x{size.Item2}";
     for(var view=0;view<3;view++){vm.Page=4;vm.StatsView=view;w.MainScroll.ScrollToTop();await Shot($"{state}-stats-{view}-{suffix}");if(state=="filled"){w.MainScroll.ScrollToBottom();await Shot($"{state}-stats-{view}-lower-{suffix}");}}
     foreach(var page in new[]{0,1,2,3,5,6}){vm.Page=page;await vm.ChangeDayAsync(day.ToDateTime(TimeOnly.MinValue));w.MainScroll.ScrollToTop();await Shot($"{state}-page-{page}-{suffix}");}
     if(state=="filled"){vm.OpenRecords(day,day);vm.OpenDetail(vm.FilteredRecords.First());await Shot($"detail-{suffix}");vm.DrawerOpen=false;}
    }
   }
   await Matrix("empty");
   var emptyFrom=vm.StatsFrom;var emptyTo=vm.StatsTo;
   vm.StatsTo=day.AddDays(3).ToDateTime(TimeOnly.MinValue);vm.StatsFrom=day.AddDays(1).ToDateTime(TimeOnly.MinValue);vm.Page=4;vm.StatsView=0;
   w.Width=900;w.Height=600;
   foreach(var dark in new[]{false,true}){vm.DarkTheme=dark;w.MainScroll.ScrollToTop();await Shot($"empty-future-stats-0-{(dark?"dark":"light")}-900x600");}
   vm.StatsFrom=emptyFrom;vm.StatsTo=emptyTo;
   var db=vm.Database;
   var longText="先明确条件，再保留推导与错因。下一阶段用反例核对适用边界；投入时长不代表掌握。";
   db.SaveChapter(new("capture-math","math","函数与极限",60,Mastery.Reviewing));db.SaveChapter(new("capture-archive","408","旧章节 · 可读引用",Archived:true));
   db.SaveTask(new("capture-task","复习函数边界",day,false,"math"));db.SaveTask(new("capture-done","整理推导笔记",day.AddDays(-3),true,"408"));
   db.SaveWeakPoint(new("capture-weak","等价无穷小的适用条件",longText,"概念边界","math","capture-math",WeakPointState.Reviewing,day));db.SaveReflection(day,longText);
   db.SavePeriodReview(new("capture-review",day.AddDays(-6),day,null,"这一阶段完成了概念整理。下一步：每天用一道反例检查推导条件；仍需复习等价替换。"));
   var subjects=new[]{"math","english","408","politics"};
   for(var i=0;i<30;i++)db.SaveRecord(new("capture-"+i,subjects[i%4],i%4==0?"capture-math":i%4==2?"capture-archive":null,SessionSource.Manual,"练习与回顾",[new("capture-part-"+i,day.AddDays(-i),(25+(i*17)%110)*60000L+1234,"Asia/Shanghai",PartKind.DurationOnly)]),"synthetic capture");
   foreach(var pair in new[]{("tiny",1L),("short",999L),("seconds",61001L)})db.SaveRecord(new(pair.Item1,"math",null,SessionSource.Manual,"整数秒边界",[new(pair.Item1+"-p",day,pair.Item2,"Asia/Shanghai",PartKind.DurationOnly)]),"synthetic boundary");
   var start=new DateTimeOffset(day.AddDays(-1).ToDateTime(new TimeOnly(23,50)),TimeSpan.FromHours(8));db.SaveRecord(new("capture-cross","english",null,SessionSource.Manual,"跨日有效片段",TimeParts.Interval(start,start.AddMinutes(30),"Asia/Shanghai")),"synthetic interval");
   db.SaveSetting("rest-day:"+day.ToString("yyyy-MM-dd"),"true");vm.Refresh();vm.StatsFrom=day.AddDays(-29).ToDateTime(TimeOnly.MinValue);vm.StatsTo=day.ToDateTime(TimeOnly.MinValue);vm.OpenRecords(day.AddDays(-29),day);
   await Matrix("filled");
   vm.DarkTheme=false;w.Width=1200;w.Height=800;vm.Page=4;vm.StatsView=0;vm.StatsFrom=day.AddDays(-730).ToDateTime(TimeOnly.MinValue);w.MainScroll.ScrollToTop();await Shot("long-range-overview");w.MainScroll.ScrollToBottom();await Shot("long-range-heatmap");
   vm.StatsFrom=day.ToDateTime(TimeOnly.MinValue);vm.StatsSubjectId="math";await vm.StartCommand.ExecuteAsync(null);await Task.Delay(1100);await vm.MarkUncertainAsync("synthetic capture: unknown gap excluded");vm.Page=4;vm.StatsView=0;w.MainScroll.ScrollToBottom();await Shot("pending-prefix-rest-heatmap");await vm.ResolveAsync([]);await vm.FinishCommand.ExecuteAsync(null);
   vm.StatsFrom=day.AddDays(-29).ToDateTime(TimeOnly.MinValue);vm.StatsSubjectId="";vm.Page=4;vm.StatsView=0;w.MainScroll.ScrollToTop();w.SubjectDonut.BringIntoView();w.UpdateLayout();w.Activate();await Task.Delay(80);
   var pointer=NativeCapture.MoveToOwned(w.SubjectDonut,w.SubjectDonut.FirstHitCenter);
   try{await Task.Delay(350);if(!w.SubjectDonut.HoverTooltip.IsOpen)throw new InvalidOperationException("Actual donut hover was not observed.");await Shot("donut-hover");await PopupShot("donut-hover-popup",w.SubjectDonut.HoverTooltip);}
   finally{NativeCapture.RestoreCursor(pointer);}
   foreach(var raw in new[]{1L,1000000000000L})
   {
    var panel=new System.Windows.Controls.StackPanel{Margin=new Thickness(16)};panel.Children.Add(new NaturalDuration{Milliseconds=raw,FontSize=28,TextWrapping=TextWrapping.Wrap});panel.Children.Add(new StudyChart{Mode="Donut",Data=new[]{new PlotPoint("edge","唯一科目",raw,ThemeService.SubjectColor("math"))}});
    var host=new Window{Owner=w,Title="合成边界 · 单项整数秒",Content=panel,Width=600,Height=450};host.Show();await Shot(raw==1?"edge-single-one-ms":"edge-single-extreme",host);host.Close();
   }
   db.Backup(Path.Combine(output,"synthetic.yanjibak"));db.ExportCsv(Path.Combine(output,"raw-records.csv"));File.WriteAllText(Path.Combine(output,"canonical.json"),JsonSerializer.Serialize(db.Records(null,true),new JsonSerializerOptions{WriteIndented=true}));
   File.WriteAllText(Path.Combine(output,"screenshots.json"),JsonSerializer.Serialize(shots,new JsonSerializerOptions{WriteIndented=true}));
   File.WriteAllText(Path.Combine(output,"result.json"),JsonSerializer.Serialize(new{Status="PASS",Screenshots=shots.Count,Integrity=db.Integrity(),CurrentLocalToday=vm.LocalToday()}));
  }
  catch(Exception e){((App)Application.Current).ValidationFailed=true;File.WriteAllText(Path.Combine(output,"failure.txt"),e.ToString());File.WriteAllText(Path.Combine(output,"screenshots-partial.json"),JsonSerializer.Serialize(shots));}
  finally{w.SmokeClose();}
 }
}
