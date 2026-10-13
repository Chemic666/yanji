using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using YanJi.Core;
namespace YanJi.DesignLab;
public sealed class LabChart : FrameworkElement
{
 public AnalyticsSnapshot Data {get;set;}=StudyAnalytics.Build([],[],DateOnly.FromDateTime(DateTime.Today),DateOnly.FromDateTime(DateTime.Today),DateOnly.FromDateTime(DateTime.Today));
 public string Mode {get;set;}="Daily";public bool Dark {get;set;}
 public Action<DateOnly?,string?>? Drill {get;set;}
 private sealed record Hit(Geometry Area,string Label,DateOnly? Date,string? Subject);
 private readonly List<Hit> _hits=[];private int _selected=-1;public bool PreviewMode {get;set;}
 public LabChart(){Focusable=true;Cursor=Cursors.Hand;ToolTip="悬停查看精确时长；方向键选择，Enter 查看原始记录";MinHeight=80;}
 private static SolidColorBrush Color(string hex)=>new((Color)ColorConverter.ConvertFromString(hex));
 private void Text(DrawingContext dc,string text,double x,double y,double size=11,string resource="LabMuted",bool numeric=false){var f=new FormattedText(text,CultureInfo.GetCultureInfo("zh-CN"),FlowDirection.LeftToRight,new Typeface(numeric?"Consolas":"Segoe UI Variable Text, Microsoft YaHei UI"),size,Ui.Brush(resource),VisualTreeHelper.GetDpi(this).PixelsPerDip);dc.DrawText(f,new Point(x,y));}
 protected override void OnRender(DrawingContext dc)
 {
  base.OnRender(dc);_hits.Clear();var w=ActualWidth;var h=ActualHeight;if(w<100||h<50)return;dc.DrawRectangle(Brushes.Transparent,null,new Rect(0,0,w,h));
  if(Data.DurationMs==0){Text(dc,"还没有学习记录",24,h/2-18,16,"LabInk");Text(dc,"开始一段计时，或补录已完成的学习。",24,h/2+10,12);return;}
  if(Mode=="Donut"){
   var radius=Math.Min(w,h)*.40;var center=new Point(w/2,h/2);var angle=-90d;
   foreach(var s in Data.Subjects){var span=360d*s.DurationMs/Data.DurationMs;var pen=new Pen(Color(Ui.SubjectColor(s.SubjectId,Dark)),radius*.27){StartLineCap=PenLineCap.Flat,EndLineCap=PenLineCap.Flat};var end=angle+span;
    if(span>=359.99){var geometry=new EllipseGeometry(center,radius,radius);dc.DrawGeometry(null,pen,geometry);_hits.Add(new(new CombinedGeometry(GeometryCombineMode.Exclude,new EllipseGeometry(center,radius+pen.Thickness/2,radius+pen.Thickness/2),new EllipseGeometry(center,radius-pen.Thickness/2,radius-pen.Thickness/2)),s.Name+" · "+DurationText.Format(s.DurationMs)+$" · {s.DurationMs*100d/Data.DurationMs:0.0}%",null,s.SubjectId));}
    else{Point P(double a)=>new(center.X+radius*Math.Cos(a*Math.PI/180),center.Y+radius*Math.Sin(a*Math.PI/180));var geo=new StreamGeometry();using(var c=geo.Open()){c.BeginFigure(P(angle),false,false);c.ArcTo(P(end),new Size(radius,radius),0,span>180,SweepDirection.Clockwise,true,false);}dc.DrawGeometry(null,pen,geo);_hits.Add(new(geo.GetWidenedPathGeometry(pen),s.Name+" · "+DurationText.Format(s.DurationMs)+$" · {s.DurationMs*100d/Data.DurationMs:0.0}%",null,s.SubjectId));}angle=end;
   }Text(dc,"科目投入",center.X-28,center.Y-20,12);Text(dc,Data.Subjects.Count+" 个科目",center.X-28,center.Y+5,14,"LabInk");
  }else if(Mode=="Heat"){
   var days=Data.Daily;var gap=4d;var cw=(w-gap*(days.Count-1))/days.Count;var max=Math.Max(1,days.Max(d=>d.DurationMs));for(int i=0;i<days.Count;i++){var d=days[i];var r=new Rect(i*(cw+gap),18,Math.Max(2,cw),28);var brush=Ui.Brush(d.DurationMs>0?"LabAccent":"LabLine").Clone();brush.Opacity=d.DurationMs>0?.22+.78*d.DurationMs/max:.5;dc.DrawRoundedRectangle(brush,null,r,4,4);_hits.Add(new(new RectangleGeometry(r),$"{d.Date:yyyy-MM-dd} · {DurationText.Format(d.DurationMs)}",d.Date,null));if(i==0||i==days.Count-1)Text(dc,d.Date.ToString("MM.dd"),Math.Min(r.X,w-36),54,10);}
  }else{
   var daily=Data.Daily;var x0=42d;var y0=20d;var bottom=h-30;var height=bottom-y0;var width=w-x0-12;var n=daily.Count;var max=daily.Max(d=>d.DurationMs);var ceiling=Math.Max(60000,Math.Ceiling(max/1800000d)*1800000);var pitch=width/n;
   for(int i=0;i<=3;i++){var y=bottom-height*i/3;dc.DrawLine(new Pen(Ui.Brush("LabLine"),.6),new Point(x0,y),new Point(w-8,y));var v=ceiling*i/3;Text(dc,v>=3600000?(v/3600000d).ToString("0.#")+"h":(v/60000d).ToString("0")+"m",0,y-7,10,numeric:true);}
   var points=new List<Point>();
   for(int i=0;i<n;i++){var d=daily[i];var x=x0+pitch*(i+.5);var y=bottom-height*d.DurationMs/ceiling;points.Add(new Point(x,y));var r=new Rect(x0+pitch*i,y0,Math.Max(1,pitch),height);
    if(Mode=="Daily"){var barW=Math.Max(2,Math.Min(20,pitch*.52));double by=bottom;foreach(var s in d.Subjects.OrderBy(s=>s.Key)){var bh=height*s.Value/ceiling;dc.DrawRoundedRectangle(Color(Ui.SubjectColor(s.Key,Dark)),null,new Rect(x-barW/2,by-bh,barW,Math.Max(.4,bh)),2,2);by-=bh;}}
    _hits.Add(new(new RectangleGeometry(r),$"{d.Date:yyyy-MM-dd} · {DurationText.Format(d.DurationMs)}\n"+string.Join("\n",d.Subjects.Select(s=>(Data.Subjects.FirstOrDefault(t=>t.SubjectId==s.Key)?.Name??s.Key)+" · "+DurationText.Format(s.Value))),d.Date,null));
    if(i==0||i==n-1||i%(Math.Max(1,n/5))==0)Text(dc,d.Date.ToString("MM.dd"),Math.Max(x0,x-15),bottom+12,10);
   }
   if(Mode=="Trend"){
    var line=new StreamGeometry();using(var c=line.Open()){c.BeginFigure(points[0],false,false);if(points.Count>1)c.PolyLineTo(points.Skip(1).ToArray(),true,false);}var area=new StreamGeometry();using(var c=area.Open()){c.BeginFigure(new Point(points[0].X,bottom),true,true);c.PolyLineTo(points.ToArray(),true,false);c.LineTo(new Point(points[^1].X,bottom),true,false);}var fill=Ui.Brush("LabAccent").Clone();fill.Opacity=.08;dc.DrawGeometry(fill,null,area);dc.DrawGeometry(null,new Pen(Ui.Brush("LabAccent"),2),line);foreach(var p in points)dc.DrawEllipse(Ui.Brush("LabSurface"),new Pen(Ui.Brush("LabAccent"),1.6),p,2.8,2.8);
   }
  }
  if(!PreviewMode&&_selected>=0&&_selected<_hits.Count)dc.DrawGeometry(null,new Pen(Ui.Brush("LabAccent"),1.5),_hits[_selected].Area);
 }
 protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);var p=e.GetPosition(this);var i=_hits.FindIndex(x=>x.Area.FillContains(p));if(i==_selected)return;if(ToolTip is System.Windows.Controls.ToolTip previous)previous.IsOpen=false;_selected=i;ToolTip=i>=0?_hits[i].Label+"\n点击查看原始记录":"悬停查看精确时长";InvalidateVisual();}
 protected override void OnMouseLeave(MouseEventArgs e){base.OnMouseLeave(e);_selected=-1;InvalidateVisual();}
 protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e){base.OnMouseLeftButtonDown(e);Focus();if(_selected>=0&&_selected<_hits.Count){var hit=_hits[_selected];Drill?.Invoke(hit.Date,hit.Subject);e.Handled=true;}}
 protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);if(_hits.Count==0)return;if(ToolTip is System.Windows.Controls.ToolTip old)old.IsOpen=false;switch(e.Key){case Key.Right:_selected=Math.Min(_hits.Count-1,_selected+1);break;case Key.Left:_selected=Math.Max(0,_selected-1);break;case Key.Home:_selected=0;break;case Key.End:_selected=_hits.Count-1;break;case Key.Enter:if(_selected>=0){var item=_hits[_selected];Drill?.Invoke(item.Date,item.Subject);}e.Handled=true;return;default:return;}if(_selected>=0){ToolTip=new System.Windows.Controls.ToolTip{Content=_hits[_selected].Label,PlacementTarget=this,Placement=System.Windows.Controls.Primitives.PlacementMode.Top,IsOpen=true,StaysOpen=true};System.Windows.Automation.AutomationProperties.SetHelpText(this,_hits[_selected].Label);}InvalidateVisual();e.Handled=true;}
 protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e){if(ToolTip is System.Windows.Controls.ToolTip tip)tip.IsOpen=false;base.OnLostKeyboardFocus(e);}
}
