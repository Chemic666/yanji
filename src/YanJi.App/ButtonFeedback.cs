using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
namespace YanJi.App;
public static class ButtonFeedback
{
    public static readonly DependencyProperty DepthProperty=DependencyProperty.RegisterAttached("Depth",typeof(bool),typeof(ButtonFeedback),new PropertyMetadata(true));
    public static bool GetDepth(DependencyObject o)=>(bool)o.GetValue(DepthProperty);
    public static void SetDepth(DependencyObject o,bool value)=>o.SetValue(DepthProperty,value);
    public static readonly DependencyProperty StateProperty=DependencyProperty.RegisterAttached("State",typeof(string),typeof(ButtonFeedback),new PropertyMetadata("Idle",StateChanged));
    private static readonly DependencyProperty SavedContentProperty=DependencyProperty.RegisterAttached("SavedContent",typeof(object),typeof(ButtonFeedback));
    private static void StateChanged(DependencyObject o,DependencyPropertyChangedEventArgs e)
    {
        if(o is not Button button)return;
        if((string)e.OldValue=="Idle")button.SetValue(SavedContentProperty,button.Content);
        var state=(string)e.NewValue;
        button.SetCurrentValue(ContentControl.ContentProperty,state=="Saving"?"保存中…":state=="Success"?"已保存":button.GetValue(SavedContentProperty));
        if(state=="Idle")button.ClearValue(SavedContentProperty);
    }
    public static string GetState(DependencyObject o)=>(string)o.GetValue(StateProperty);
    public static void SetState(DependencyObject o,string value)=>o.SetValue(StateProperty,value);
    public static readonly DependencyProperty AnimateProperty=DependencyProperty.RegisterAttached("Animate",typeof(bool),typeof(ButtonFeedback),new PropertyMetadata(false,Changed));
    public static bool GetAnimate(DependencyObject o)=>(bool)o.GetValue(AnimateProperty);
    public static void SetAnimate(DependencyObject o,bool value)=>o.SetValue(AnimateProperty,value);
    private static void Changed(DependencyObject o,DependencyPropertyChangedEventArgs e)
    {
        if(o is not Button b||!(bool)e.NewValue)return;
        void Update()
        {
            if(b.Template?.FindName("Feedback",b) is not FrameworkElement layer)return;
            var value=!b.IsEnabled?0:b.IsPressed?.18:b.IsMouseOver?.12:0;
            layer.BeginAnimation(UIElement.OpacityProperty,null);
            if(SystemParameters.ClientAreaAnimation&&!SystemParameters.HighContrast)
                layer.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(value,System.TimeSpan.FromMilliseconds(140)){FillBehavior=FillBehavior.HoldEnd});
            else layer.Opacity=value;
        }
        b.MouseEnter+=(_,_)=>Update();b.MouseLeave+=(_,_)=>Update();b.IsEnabledChanged+=(_,_)=>Update();
        b.PreviewMouseLeftButtonDown+=(_,_)=>b.Dispatcher.BeginInvoke(new System.Action(Update));b.PreviewMouseLeftButtonUp+=(_,_)=>b.Dispatcher.BeginInvoke(new System.Action(Update));
    }
}
