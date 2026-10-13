using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Input;
using System.Windows.Media;
using YanJi.Core;

namespace YanJi.App;
public sealed record PlotPoint(string Key,string Label,long Value,string Color,string? SubjectId=null,
    DateOnly? From=null,DateOnly? To=null,List<PlotPoint>? Segments=null,string Status="",double Start=0,double End=1)
{public string Duration=>DurationText.Format(Value);}
/// <summary>Business charts drawn from immutable canonical projections. Geometry is display only.</summary>
public sealed class StudyChart : FrameworkElement
{
    public static readonly DependencyProperty DataProperty=DependencyProperty.Register(nameof(Data),typeof(IReadOnlyList<PlotPoint>),typeof(StudyChart),new FrameworkPropertyMetadata(null,FrameworkPropertyMetadataOptions.AffectsRender|FrameworkPropertyMetadataOptions.AffectsMeasure));
    public static readonly DependencyProperty ModeProperty=DependencyProperty.Register(nameof(Mode),typeof(string),typeof(StudyChart),new FrameworkPropertyMetadata("Trend",FrameworkPropertyMetadataOptions.AffectsRender|FrameworkPropertyMetadataOptions.AffectsMeasure));
    public static readonly DependencyProperty UnitProperty=DependencyProperty.Register(nameof(Unit),typeof(string),typeof(StudyChart),new FrameworkPropertyMetadata("time",FrameworkPropertyMetadataOptions.AffectsRender));
    public IReadOnlyList<PlotPoint>? Data {get=>(IReadOnlyList<PlotPoint>?)GetValue(DataProperty);set=>SetValue(DataProperty,value);}
    public string Mode {get=>(string)GetValue(ModeProperty);set=>SetValue(ModeProperty,value);}
    public string Unit {get=>(string)GetValue(UnitProperty);set=>SetValue(UnitProperty,value);}
    public event Action<PlotPoint>? PointInvoked;
    private readonly List<(Geometry Shape,PlotPoint Point)> _hits=[];
    private int _selected=-1;
    public int HitCount=>_hits.Count;
    public StudyChart(){Focusable=true;Cursor=Cursors.Hand;SnapsToDevicePixels=true;AutomationProperties.SetName(this,"学习图表，左右箭头选择数据，Enter 查看原始记录");}
    protected override AutomationPeer OnCreateAutomationPeer()=>new FrameworkElementAutomationPeer(this);
    protected override Size MeasureOverride(Size available)=>new(double.IsInfinity(available.Width)?500:available.Width,
        Mode=="Heatmap"?154:Mode=="Timeline"?Math.Max(96,(Data?.Count??0)*52+28):Mode=="Donut"?Math.Max(220,(Data?.Count??0)*44+12):Mode=="Sparkline"?44:220);
    public void RefreshTheme()=>InvalidateVisual();
    private static Brush ColorBrush(string color)=>new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
    private static string Value(PlotPoint p,string unit)=>unit=="time"?DurationText.Format(p.Value):p.Value+" "+unit;
    private string Description(PlotPoint p)=>p.Label+" · "+Value(p,Unit)+(Unit=="time"?"（"+DurationText.Exact(p.Value)+"）":"")+(p.Status.Length>0?" · "+p.Status:"");
    private static void Text(DrawingContext dc,string text,double x,double y,double size=11,Brush? brush=null,double width=1000)
    {if(width<2)return;var f=new FormattedText(text,CultureInfo.GetCultureInfo("zh-CN"),FlowDirection.LeftToRight,new Typeface("Microsoft YaHei UI"),size,brush??ThemeService.Brush("Muted"),1){MaxTextWidth=width,MaxTextHeight=size*1.8,Trimming=TextTrimming.CharacterEllipsis};dc.DrawText(f,new(x,y));}
    private void Hit(Geometry g,PlotPoint p)=>_hits.Add((g,p));
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);_hits.Clear();dc.DrawRectangle(Brushes.Transparent,null,new(0,0,ActualWidth,ActualHeight));
        var data=Data??[];var width=ActualWidth;var height=ActualHeight;if(width<30||height<20)return;
        if(data.Count==0||(Mode is "Donut" or "Trend" or "Stacked" or "Sparkline")&&data.All(p=>p.Value==0))
        {Text(dc,"尚无有效学习数据",16,height/2-12,14,ThemeService.Brush("Muted"),width-32);return;}
        if(Mode=="Donut")
        {
            var sum=data.Sum(p=>p.Value);var cx=86d;var cy=105d;var radius=62d;var angle=-Math.PI/2;
            dc.DrawEllipse(null,new Pen(ThemeService.Brush("Soft"),20),new(cx,cy),radius,radius);
            for(var i=0;i<data.Count;i++)
            {
                var p=data[i];var sweep=2*Math.PI*p.Value/sum;Geometry g;
                if(sweep>=2*Math.PI-0.000001)g=new EllipseGeometry(new(cx,cy),radius,radius);
                else {var path=new StreamGeometry();using(var c=path.Open()){c.BeginFigure(new(cx+radius*Math.Cos(angle),cy+radius*Math.Sin(angle)),false,false);c.ArcTo(new(cx+radius*Math.Cos(angle+sweep),cy+radius*Math.Sin(angle+sweep)),new(radius,radius),0,sweep>Math.PI,SweepDirection.Clockwise,true,false);}g=path;}
                if(p.Value>0){dc.DrawGeometry(null,new Pen(ColorBrush(p.Color),20),g);Hit(g.GetWidenedPathGeometry(new Pen(Brushes.Black,28)),p);}
                var legend=new Rect(180,10+i*44,Math.Max(1,width-180),40);dc.DrawRoundedRectangle(ColorBrush(p.Color),null,new(180,17+i*44,8,8),3,3);
                Text(dc,p.Label,196,10+i*44,12,ThemeService.Brush("Ink"),width-196);
                Text(dc,$"{Value(p,Unit)} · {(100d*p.Value/sum):0.#}%",196,28+i*44,10,null,width-196);Hit(new RectangleGeometry(legend),p);angle+=sweep;
            }
            var center=Unit!="time"?sum+" "+Unit:sum>=3600000?$"≈ {sum/3600000d:0.##} 小时":sum>=60000?$"≈ {sum/60000d:0.##} 分钟":DurationText.Format(sum);
            Text(dc,"范围总计",cx-38,cy-21,11,null,90);Text(dc,center,cx-48,cy+2,14,ThemeService.Brush("Ink"),100);
            Hit(new EllipseGeometry(new(cx,cy),48,48),new PlotPoint("total","范围总计",sum,data[0].Color,data.Select(p=>p.SubjectId).Distinct().Count()==1?data[0].SubjectId:null));
        }
        else if(Mode=="Heatmap")
        {
            var max=Math.Max(1,data.Max(v=>v.Value));const double cell=12,gap=4;for(var i=0;i<data.Count;i++){var p=data[i];var x=30+(i/7)*(cell+gap);var y=26+(i%7)*(cell+gap);var rect=new Rect(x,y,cell,cell);
                var intensity=p.Value==0?0:Math.Clamp(p.Value/(double)max,.18,1);
                var fill=p.Value==0?ThemeService.Brush("Soft"):ColorBrush(p.Color);dc.PushOpacity(p.Value==0?1:.3+.7*intensity);dc.DrawRoundedRectangle(fill,null,rect,3,3);dc.Pop();
                if(p.Status.Contains("休息"))Text(dc,"−",x+2,y-2,10,null,12);if(p.Status.Contains("待核对"))dc.DrawRoundedRectangle(null,new Pen(ThemeService.Brush("Warning"),2),rect,3,3);
                if(p.From?.Day==1)Text(dc,p.From.Value.ToString("M月"),x,1,10,null,40);Hit(new RectangleGeometry(rect),p);}
            Text(dc,"一",0,25,10);Text(dc,"三",0,57,10);Text(dc,"五",0,89,10);
        }
        else if(Mode=="Timeline")
        {
            Text(dc,"00:00",100,0,10);Text(dc,"次日 00:00",Math.Max(100,width-76),0,10);
            for(var i=0;i<data.Count;i++){var p=data[i];var y=28+i*52;Text(dc,p.Label,0,y,11,ThemeService.Brush("Ink"),96);
                var bar=new Rect(104,y+1,Math.Max(1,width-110),18);dc.DrawRoundedRectangle(ThemeService.Brush("Soft"),null,bar,4,4);
                var r=new Rect(bar.Left+bar.Width*p.Start,y+1,Math.Max(1,bar.Width*(p.End-p.Start)),18);dc.DrawRoundedRectangle(ColorBrush(p.Color),null,r,4,4);Hit(new RectangleGeometry(new Rect(r.Left,y,r.Width,44)),p);
                Text(dc,p.Status+" · "+DurationText.Format(p.Value),104,y+23,10,null,width-110);}
        }
        else
        {
            var spark=Mode=="Sparkline";var left=spark?4:72;var top=spark?3:12;var bottom=spark?height-3:height-33;var right=width-10;var max=Math.Max(1,data.Max(p=>p.Value));var areaH=bottom-top;var areaW=Math.Max(1,right-left);
            if(!spark)for(var tick=0;tick<=3;tick++){var y=bottom-areaH*tick/3;dc.DrawLine(new Pen(ThemeService.Brush("Line"),1),new(left,y),new(right,y));var divisor=max>=3600000?3600000d:max>=60000?60000d:1000d;var unit=max>=3600000?" h":max>=60000?" min":" s";var value=max*tick/3d/divisor;Text(dc,Unit=="time"?value.ToString(value>=1_000_000?"0.##E+0":"0.#",CultureInfo.InvariantCulture)+unit:((long)(max*tick/3d))+" "+Unit,0,y-7,10,null,left-6);}
            var step=areaW/data.Count;var points=new List<Point>();
            for(var i=0;i<data.Count;i++)
            {
                var p=data[i];var x=left+(i+.5)*step;var y=bottom-areaH*p.Value/max;
                if(Mode=="Stacked")
                {var cursor=bottom;foreach(var segment in p.Segments??[]){var h=areaH*segment.Value/max;var rect=new Rect(left+i*step+step*.15,cursor-h,Math.Max(.3,step*.7),Math.Max(.3,h));dc.DrawRectangle(ColorBrush(segment.Color),null,rect);Hit(new RectangleGeometry(rect),segment);cursor-=h;}}
                else {points.Add(new(x,y));var hit=new EllipseGeometry(new(x,y),8,8);Hit(hit,p);if(!spark)dc.DrawEllipse(ColorBrush(p.Color),null,new(x,y),3,3);}
                if(!spark&&(i%Math.Max(1,data.Count/6)==0||i==data.Count-1))Text(dc,p.Label,Math.Max(left,x-22),bottom+9,10,null,Math.Min(60,right-x+22));
                if(p.Value==0)Hit(new RectangleGeometry(new Rect(left+i*step,bottom-10,step,10)),p);
            }
            if(points.Count>1){var g=new StreamGeometry();using(var c=g.Open()){c.BeginFigure(points[0],false,false);c.PolyLineTo(points.Skip(1).ToArray(),true,false);}dc.DrawGeometry(null,new Pen(ThemeService.Brush("Accent"),spark?2:2.5),g);}
            if(points.Count==1)dc.DrawEllipse(ThemeService.Brush("Accent"),null,points[0],4,4);
        }
        if(IsKeyboardFocused&&_selected>=0&&_selected<_hits.Count){dc.DrawGeometry(null,new Pen(ThemeService.Brush("Ink"),2),_hits[_selected].Shape);Text(dc,Description(_hits[_selected].Point),0,Math.Max(0,height-18),10,ThemeService.Brush("Ink"),width);}
    }
    protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);var p=e.GetPosition(this);var hit=_hits.FindLastIndex(h=>h.Shape.FillContains(p));if(hit!=_selected){_selected=hit;ToolTip=hit<0?null:Description(_hits[hit].Point);InvalidateVisual();}}
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e){base.OnMouseLeftButtonDown(e);Focus();var hit=_hits.FindLastIndex(h=>h.Shape.FillContains(e.GetPosition(this)));if(hit>=0){_selected=hit;PointInvoked?.Invoke(_hits[hit].Point);e.Handled=true;}}
    protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);if(_hits.Count==0)return;
        if(e.Key is Key.Left or Key.Right or Key.Home or Key.End){_selected=e.Key==Key.Home?0:e.Key==Key.End?_hits.Count-1:Math.Clamp(_selected+(e.Key==Key.Left?-1:1),0,_hits.Count-1);AutomationProperties.SetHelpText(this,Description(_hits[_selected].Point));InvalidateVisual();e.Handled=true;}
        if(e.Key==Key.Enter&&_selected>=0&&_selected<_hits.Count){PointInvoked?.Invoke(_hits[_selected].Point);e.Handled=true;}}
    protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e){base.OnGotKeyboardFocus(e);if(_selected<0)_selected=0;InvalidateVisual();}
    protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e){base.OnLostKeyboardFocus(e);InvalidateVisual();}
    public void InvokePoint(int index){if(Data!=null&&index>=0&&index<Data.Count)PointInvoked?.Invoke(Data[index]);}
}
