using System;
using System.Collections.Generic;
using System.Globalization;

using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using YanJi.Core;
using YanJi.Data;
namespace YanJi.DesignLab;
internal static class Ui
{
 public static Brush Brush(string key)=>(Brush)Application.Current.Resources[key];
 public static T Color<T>(T element,DependencyProperty property,string key) where T:FrameworkElement{element.SetResourceReference(property,key);return element;}
 public static TextBlock Text(string value,double size=13,string color="LabInk",bool semibold=false){var t=new TextBlock{Text=value,TextDecorations=new TextDecorationCollection(),FontSize=size,FontWeight=semibold?FontWeights.SemiBold:FontWeights.Normal,VerticalAlignment=VerticalAlignment.Center};Color(t,TextBlock.ForegroundProperty,color);return t;}
 public static TextBlock Number(string value,double size=38,string color="LabInk"){var t=Text(value,size,color);t.FontFamily=new FontFamily("Segoe UI Variable Display, Microsoft YaHei UI");Typography.SetNumeralAlignment(t,FontNumeralAlignment.Tabular);return t;}
 public static StackPanel Stack(params UIElement[] children){var s=new StackPanel();foreach(var c in children)s.Children.Add(c);return s;}
 public static StackPanel Row(params UIElement[] children){var s=new StackPanel{Orientation=Orientation.Horizontal};foreach(var c in children)s.Children.Add(c);return s;}
 public static Border Surface(UIElement child,string key="LabSurface",double padding=24,double radius=18){var b=new Border{Child=child,Padding=new Thickness(padding),CornerRadius=new CornerRadius(radius)};Color(b,Border.BackgroundProperty,key);return b;}
 public static Border Line(double margin=16){var b=new Border{Height=1,Margin=new Thickness(0,margin,0,margin)};Color(b,Border.BackgroundProperty,"LabLine");return b;}
 public static Button Button(string text,Action action,string style="LabButton",string? id=null){var b=new Button{Content=text,Style=(Style)Application.Current.Resources[style]};if(style=="LabPrimary")b.Content=Text(text,13,"LabAccentInk",true);b.Click+=(_,_)=>action();AutomationProperties.SetName(b,text);if(id!=null)AutomationProperties.SetAutomationId(b,id);return b;}
 public static Grid Columns(UIElement left,UIElement right,double leftWeight=1,double rightWeight=1,double gap=24){var g=new Grid();g.ColumnDefinitions.Add(new(){Width=new GridLength(leftWeight,GridUnitType.Star)});g.ColumnDefinitions.Add(new(){Width=new GridLength(gap)});g.ColumnDefinitions.Add(new(){Width=new GridLength(rightWeight,GridUnitType.Star)});g.Children.Add(left);Grid.SetColumn(right,2);g.Children.Add(right);return g;}
 public static DockPanel Header(UIElement title,UIElement action){var g=new DockPanel{LastChildFill=true};if(action is FrameworkElement element)element.VerticalAlignment=VerticalAlignment.Center;DockPanel.SetDock(action,Dock.Right);g.Children.Add(action);g.Children.Add(title);return g;}
 public static void Bind(FrameworkElement element,DependencyProperty property,string path,object source,BindingMode mode=BindingMode.OneWay){element.SetBinding(property,new Binding(path){Source=source,Mode=mode,UpdateSourceTrigger=UpdateSourceTrigger.PropertyChanged});}
 public static FrameworkElement Icon(string name,double size=18){var paths=new Dictionary<string,string>{["today"]="M9,1 L9,4 M6,1 L12,1 M4,5 L2,3 M9,5 A6,6 0 1 1 8.99,5 M9,8 L9,12 L12,12",["stats"]="M2,2 L2,16 L17,16 M5,12 L5,8 M9,12 L9,5 M13,12 L13,3",["calendar"]="M2,4 L16,4 L16,16 L2,16 Z M2,8 L16,8 M5,1 L5,6 M13,1 L13,6",["plan"]="M3,2 L7,2 L7,16 L3,16 Z M10,2 L14,2 L16,15 L12,16 Z",["weak"]="M9,1 A8,8 0 1 1 8.99,1 M9,5 L9,10 M9,13 L9,14",["settings"]="M2,5 L16,5 M2,13 L16,13 M6,2 L6,8 M12,10 L12,16",["records"]="M3,1 L15,1 L15,17 L3,17 Z M6,5 L12,5 M6,9 L12,9 M6,13 L10,13",["sun"]="M9,4 A5,5 0 1 1 8.99,4 M9,0 L9,2 M9,16 L9,18 M0,9 L2,9 M16,9 L18,9",["moon"]="M12,1 A8,8 0 1 0 17,13 A9,9 0 0 1 12,1"};var p=new System.Windows.Shapes.Path{Data=Geometry.Parse(paths.GetValueOrDefault(name,paths["records"])),StrokeThickness=1.4,StrokeStartLineCap=PenLineCap.Round,StrokeEndLineCap=PenLineCap.Round,StrokeLineJoin=PenLineJoin.Round,Width=size,Height=size,Stretch=Stretch.Uniform};Color(p,Shape.StrokeProperty,"LabMuted");return p;}
 public static string Short(long ms){if(ms<60000)return DurationText.Format(ms);if(ms<3600000)return "≈ "+(ms/60000d).ToString("0.#",CultureInfo.InvariantCulture)+" 分";return "≈ "+(ms/3600000d).ToString("0.##",CultureInfo.InvariantCulture)+" 小时";}
 public static string Clock(long ms){var t=TimeSpan.FromMilliseconds(ms);return $"{(long)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}";}
 public static string SubjectColor(string id,bool dark){return (id,dark) switch{("math",false)=>"#6B8975",("english",false)=>"#7B9CB5",("408",false)=>"#A79ABB",("politics",false)=>"#C0A17C",("math",true)=>"#9FBAA4",("english",true)=>"#9EBED5",("408",true)=>"#BEAED3",("politics",true)=>"#D0B593",_=>dark?"#A7B6BC":"#85979A"};}
}
internal static class LabTheme
{
 public static void Apply(string direction,bool dark){var a=direction=="A";var colors=dark?(a?new[]{"#1C201D","#272D28","#222720","#E6ECE4","#ADB9AC","#A3C4A5","#203024","#344737","#3C473D"}:new[]{"#171C24","#222A35","#1D2530","#E5EBF3","#A5B5C6","#9DBDDD","#1B2D40","#2C4054","#384657"}):(a?new[]{"#F6F4EE","#FEFDF9","#EEEEE5","#29352E","#69766A","#456653","#FEFDF9","#E5ECDF","#D9DED3"}:new[]{"#F2F4F6","#FFFFFF","#E7EBEF","#293440","#647483","#476C8C","#FFFFFF","#E3EBF3","#D8E0E8"});var keys=new[]{"LabCanvas","LabSurface","LabRail","LabInk","LabMuted","LabAccent","LabAccentInk","LabSoft","LabLine"};for(var i=0;i<keys.Length;i++)Application.Current.Resources[keys[i]]=new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors[i]));Application.Current.Resources["LabDanger"]=new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark?"#E5A099":"#A94942"));}
}
internal static class DemoFixture
{
 public static void Seed(YanJiDatabase db,bool empty){if(db.Setting("visual-lab-seeded","")=="true")return;
  db.SaveChapter(new("math-integral","math","高等数学 · 定积分",68,Mastery.Reviewing));db.SaveChapter(new("english-reading","english","阅读理解 · 长难句",42,Mastery.Reviewing));db.SaveChapter(new("408-os","408","操作系统 · 进程与线程",54,Mastery.Weak));
  if(!empty){var today=DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow,TimeParts.ResolveZone("Asia/Shanghai")).DateTime);int[] mins={73,0,112,94,146,88,0,131,107,156,82,124,97,0,143,121,0,164,106,139,118,93,151,0,127,115,138,102};
   for(int i=0;i<28;i++){if(mins[i]==0)continue;var d=today.AddDays(i-28);string[] ids={"math","english","408","politics"};var id=ids[i%4];db.SaveRecord(new("demo-history-"+i,id,null,SessionSource.Manual,"示例记录 · 用于比较两套视觉方案",[TimeParts.Duration(d,TimeSpan.FromMinutes(mins[i]),"Asia/Shanghai")]),"视觉样板示例数据");}
   var start=new DateTimeOffset(today.ToDateTime(new TimeOnly(7,30)),TimeSpan.FromHours(8));var parts=TimeParts.Interval(start,start.AddMinutes(52).AddSeconds(18),"Asia/Shanghai");db.SaveRecord(new("demo-today-math","math","math-integral",SessionSource.Timer,"整理定积分的换元方法与典型题",parts),"视觉样板示例数据");
   db.SaveRecord(new("demo-today-english","english","english-reading",SessionSource.Manual,"精读一篇文章，复盘长难句",[TimeParts.Duration(today,TimeSpan.FromMinutes(36).Add(TimeSpan.FromSeconds(42)),"Asia/Shanghai")]),"视觉样板示例数据");
   db.SaveRecord(new("demo-today-408","408","408-os",SessionSource.Manual,"进程同步：整理信号量的使用条件",[TimeParts.Duration(today,TimeSpan.FromMinutes(27).Add(TimeSpan.FromSeconds(35)),"Asia/Shanghai")]),"视觉样板示例数据");
   db.SaveReflection(today,"定积分的思路比昨天更清楚了。明天先做两道换元题，再继续进程同步。");
  }db.SaveSetting("visual-lab-seeded","true");
 }
}
