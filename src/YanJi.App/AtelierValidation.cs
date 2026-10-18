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
using System.Windows.Input;
using System.Windows.Media;
using YanJi.Core;
namespace YanJi.App;
internal static partial class AtelierValidation
{
 internal static IEnumerable<DependencyObject> Children(DependencyObject root){for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var c=VisualTreeHelper.GetChild(root,i);yield return c;foreach(var d in Children(c))yield return d;}}
 internal static async Task RunAsync(MainWindow w,string output)
 {
  var app=(App)Application.Current;var m=w.Model;var d=m.Database;Directory.CreateDirectory(output);var shots=new List<object>();
  void Write(string name,object value)=>File.WriteAllText(Path.Combine(output,name),JsonSerializer.Serialize(value,new JsonSerializerOptions{WriteIndented=true}));
  async Task Shot(Window window,string name){window.UpdateLayout();await Task.Delay(180);var path=Path.Combine(output,name+".png");NativeCapture.Save(window,path);shots.Add(new{File=name+".png",Bytes=new FileInfo(path).Length,Sha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))),Width=window.ActualWidth,Height=window.ActualHeight,Dark=m.DarkTheme,Dpi=VisualTreeHelper.GetDpi(window).PixelsPerInchX});Write("screenshots.json",shots);}
  try{
   Write("provenance.json",new{Assembly=typeof(App).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,DllSha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(App).Assembly.Location))),Process=Environment.ProcessId,Data=d.DirectoryPath,Before=app.AtelierBefore,Method="real HWND capture and actual WPF focus/layout; separate Windows.Graphics.Capture observation",RenderMode=RenderOptions.ProcessRenderMode.ToString()});
   if(app.AtelierCheck){await ReviewAsync(w,output);return;}
   d.SaveChapter(new("atelier-node","math","一元函数微分学 · 导数的概念与几何意义"));var day=m.LocalToday();
   foreach(var item in new[]{("math",5400123L),("english",1800321L),("408",3600757L)})d.SaveRecord(new("atelier-"+item.Item1,item.Item1,item.Item1=="math"?"atelier-node":null,SessionSource.Manual,"隔离视觉验收 · 只记录实际投入",[TimeParts.Duration(day,TimeSpan.FromMilliseconds(item.Item2),m.ZoneId)]),"isolated atmosphere review");
   m.Refresh();m.TimerSubject=m.Subjects.Single(s=>s.Id=="math");m.TimerChapter=m.TimerChapters.Single(c=>c.Id=="atelier-node");m.Page=0;w.MainScroll.ScrollToTop();
   await Shot(w,"today-light-1200");
   var backing=new Window{Owner=w,Title="研迹 · 透明材质隔离背景",Width=620,Height=390,Left=w.Left+520,Top=w.Top+160,Background=new SolidColorBrush(Color.FromRgb(232,235,221)),Content=new Border{Focusable=true}};backing.Show();
   w.ShowMiniTimer();var mini=w.MiniTimer!;mini.Left=backing.Left+80;mini.Top=backing.Top+50;mini.Topmost=true;
   var details=new List<object>();
   foreach(var dark in new[]{false,true}){
    m.DarkTheme=dark;while(m.Busy)await Task.Delay(25);backing.Background=new SolidColorBrush(dark?Color.FromRgb(27,39,33):Color.FromRgb(232,235,221));
    backing.Activate();((Border)backing.Content).Focus();await Task.Delay(200);await Shot(mini,"mini-"+(dark?"dark":"light")+"-idle");
    mini.Activate();var button=Children(mini).OfType<Button>().First(b=>b.Command==m.StartCommand);button.Focus();await Task.Delay(200);await Shot(mini,"mini-"+(dark?"dark":"light")+"-focused");
    var text=Children(mini).OfType<TextBlock>().ToArray();var hint=text.Single(t=>t.Text.StartsWith("悬停或 Tab"));var toolbar=mini.Toolbar;
    Rect Bounds(FrameworkElement e)=>new(e.TranslatePoint(new Point(),mini),new Size(e.ActualWidth,e.ActualHeight));
    var overlap=Rect.Intersect(Bounds(hint),Bounds(toolbar));
    details.Add(new{Dark=dark,HintVisible=hint.Visibility.ToString(),HintOpacity=hint.Opacity,ToolbarOpacity=toolbar.Opacity,ToolbarKeyboardFocus=toolbar.IsKeyboardFocusWithin,OverlapWidth=overlap.Width,OverlapHeight=overlap.Height,TimerTextBlocks=text.Count(t=>System.Windows.Data.BindingOperations.GetBinding(t,TextBlock.TextProperty)?.Path.Path=="TimerText"),Text=text.Select(t=>new{t.Text,t.ActualWidth,t.ActualHeight,Visibility=t.Visibility.ToString()})});
   }
   Write("mini-layout.json",details);m.DarkTheme=false;while(m.Busy)await Task.Delay(25);
   Write("ready.json",new{Status="READY FOR ACTUAL WINDOWS CAPTURE",Process=Environment.ProcessId});
   while(!File.Exists(Path.Combine(output,"continue.request")))await Task.Delay(250);
   mini.Close();backing.Close();w.SmokeClose();
  }catch(Exception e){app.ValidationFailed=true;Write("failure.json",new{Error=e.ToString()});w.SmokeClose();}
 }
}
