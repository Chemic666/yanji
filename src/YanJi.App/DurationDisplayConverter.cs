using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using YanJi.Core;

namespace YanJi.App;
public sealed class DurationDisplayConverter : IValueConverter
{
    public object Convert(object value,Type targetType,object parameter,CultureInfo culture)
    {
        if(value==null||value==DependencyProperty.UnsetValue)return "0 秒";
        var raw=System.Convert.ToDouble(value,CultureInfo.InvariantCulture);
        if(!double.IsFinite(raw)||raw<0)return "—";
        var ms=value is long exact?exact:(long)Math.Min(Math.Floor(raw),long.MaxValue);
        var text=DurationText.Compact(ms);
        return text;
    }
    public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)=>Binding.DoNothing;
}
