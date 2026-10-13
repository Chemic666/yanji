using System;
using System.Windows;
using System.Windows.Media;

namespace YanJi.App;
/// <summary>One authored vector family, rendered in DIP and inheriting semantic foreground.</summary>
public sealed class FocusIcon : FrameworkElement
{
    public static readonly DependencyProperty KindProperty=DependencyProperty.Register(nameof(Kind),typeof(string),typeof(FocusIcon),new FrameworkPropertyMetadata("record",FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty StrokeProperty=DependencyProperty.Register(nameof(Stroke),typeof(Brush),typeof(FocusIcon),new FrameworkPropertyMetadata(null,FrameworkPropertyMetadataOptions.AffectsRender));
    public string Kind {get=>(string)GetValue(KindProperty);set=>SetValue(KindProperty,value);}
    public Brush? Stroke {get=>(Brush?)GetValue(StrokeProperty);set=>SetValue(StrokeProperty,value);}
    protected override Size MeasureOverride(Size available)=>new(20,20);
    protected override void OnRender(DrawingContext dc)
    {
        var path=Kind switch
        {
            "timer"=>"M8,2 L12,2 M10,2 L10,4 M15,5 L17,3 M10,5 A6,6 0 1 1 9.99,5 M10,7 L10,11 L13,11",
            "calendar"=>"M3,5 L17,5 L17,17 L3,17 Z M3,8 L17,8 M6,2 L6,6 M14,2 L14,6",
            "plan"=>"M3,3 L7,3 L7,17 L3,17 Z M10,3 L14,2 L17,16 L13,17 Z",
            "weak"=>"M10,3 A7,7 0 1 1 9.99,3 M10,6 L10,11 M10,14 L10,14.5",
            "stats"=>"M3,3 L3,17 L18,17 M7,13 L7,9 M11,13 L11,5 M15,13 L15,7",
            "settings"=>"M3,5 L17,5 M3,10 L17,10 M3,15 L17,15 M7,3 L7,7 M13,8 L13,12 M8,13 L8,17",
            "plus"=>"M10,3 L10,17 M3,10 L17,10",
            "close"=>"M5,5 L15,15 M15,5 L5,15",
            _=>"M5,2 L15,2 L15,18 L5,18 Z M8,6 L12,6 M8,10 L12,10 M8,14 L12,14"
        };
        var scale=Math.Min(ActualWidth,ActualHeight)/20;
        dc.PushTransform(new ScaleTransform(scale,scale));
        dc.DrawGeometry(null,new Pen(Stroke??ThemeService.Brush("NavigationInk"),1.5){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round,LineJoin=PenLineJoin.Round},Geometry.Parse(path));dc.Pop();
    }
}
