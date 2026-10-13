using System.Windows;
using System.Windows.Controls;
namespace YanJi.App;
public sealed class MetricCard : ContentControl
{
    public static readonly DependencyProperty LabelProperty=DependencyProperty.Register(nameof(Label),typeof(string),typeof(MetricCard),new PropertyMetadata(""));
    public static readonly DependencyProperty ValueProperty=DependencyProperty.Register(nameof(Value),typeof(string),typeof(MetricCard),new PropertyMetadata(""));
    public static readonly DependencyProperty HintProperty=DependencyProperty.Register(nameof(Hint),typeof(string),typeof(MetricCard),new PropertyMetadata(""));
    public string Label {get=>(string)GetValue(LabelProperty);set=>SetValue(LabelProperty,value);}
    public string Value {get=>(string)GetValue(ValueProperty);set=>SetValue(ValueProperty,value);}
    public string Hint {get=>(string)GetValue(HintProperty);set=>SetValue(HintProperty,value);}
}
