using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Linq;
namespace YanJi.App;
public sealed class EmptyVisibilityConverter : IValueConverter
{
    public object Convert(object value,Type targetType,object parameter,CultureInfo culture)=>value is int count&&count==0?Visibility.Visible:Visibility.Collapsed;
    public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)=>Binding.DoNothing;
}
public sealed class HeatWidthConverter : IValueConverter
{
    public object Convert(object value,Type targetType,object parameter,CultureInfo culture)=>value is System.Collections.Generic.IEnumerable<PlotPoint> points&&points.Count(p=>p.Status!="范围外")<=14?double.NaN:Math.Max(400,30+Math.Ceiling((value is ICollection c?c.Count:0)/7d)*16);
    public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)=>Binding.DoNothing;
}
public sealed class AverageMillisecondsConverter : IValueConverter
{
    public object Convert(object value,Type targetType,object parameter,CultureInfo culture)=>value is double d?YanJi.Core.DurationText.ForDisplay(d):value is long l?l:0L;
    public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)=>Binding.DoNothing;
}
