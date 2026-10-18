using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using YanJi.Core;

namespace YanJi.App;
public sealed class PageVisibilityConverter : IValueConverter
{
    public object Convert(object value,Type targetType,object parameter,CultureInfo culture)=>value is int index&&index==int.Parse(parameter.ToString()!,CultureInfo.InvariantCulture)?Visibility.Visible:Visibility.Collapsed;
    public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)=>Binding.DoNothing;
}
public sealed class InverseBooleanConverter : IValueConverter
{
    public object Convert(object value,Type targetType,object parameter,CultureInfo culture)=>value is bool flag&&!flag;
    public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)=>Binding.DoNothing;
}
public sealed class StateLabelConverter : IValueConverter
{
    public object Convert(object value,Type targetType,object parameter,CultureInfo culture)=>value switch
    {Mastery.Unknown=>"未评估",Mastery.Weak=>"薄弱",Mastery.Reviewing=>"复习中",Mastery.Mastered=>"已掌握",
        WeakPointState.Open=>"待复习",WeakPointState.Reviewing=>"复习中",WeakPointState.Verified=>"已验收",_=>value};
    public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)=>Binding.DoNothing;
}

/// <summary>Presentation-only radio selection; unchecking never overwrites the selected mode.</summary>
public sealed class FocusModeCheckedConverter : IValueConverter
{
 public object Convert(object value,Type targetType,object parameter,CultureInfo culture)=>value is int mode&&mode==int.Parse(parameter.ToString()!,CultureInfo.InvariantCulture);
 public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)=>value is true?int.Parse(parameter.ToString()!,CultureInfo.InvariantCulture):Binding.DoNothing;
}
