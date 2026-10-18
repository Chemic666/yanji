using System;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
namespace YanJi.App;
public partial class MiniTimerWindow:Window
{
    private readonly MainWindow _main;
    private bool _ready;
    private Task _preferencesWrite=Task.CompletedTask;
    internal bool ClosingForExit {get;set;}
    public MiniTimerWindow(MainWindow main)
    {
        _main=main;InitializeComponent();DataContext=main.Model;Closing+=HideOnClose;
        if(((App)Application.Current).TestMode){Title="研迹 · 悬浮专注 · 隔离测试";RuntimeHint.Text="研迹 · 隔离测试";}
        var work=SystemParameters.WorkArea;Left=work.Right-Width-28;Top=work.Top+60;
        var stored=main.Model.Database.Setting("mini-position","").Split(';');
        if(stored.Length==2&&double.TryParse(stored[0],NumberStyles.Float,CultureInfo.InvariantCulture,out var x)&&double.TryParse(stored[1],NumberStyles.Float,CultureInfo.InvariantCulture,out var y)&&double.IsFinite(x)&&double.IsFinite(y))
        {Left=Math.Clamp(x,work.Left,Math.Max(work.Left,work.Right-Width));Top=Math.Clamp(y,work.Top,Math.Max(work.Top,work.Bottom-Height));}
        Topmost=main.Model.Database.Setting("mini-topmost","true")=="true";
        Loaded+=(_,_)=>_ready=true;
    }
    private void HideOnClose(object? sender,CancelEventArgs e){if(!ClosingForExit){e.Cancel=true;Hide();}}
    private void Hide_Click(object sender,RoutedEventArgs e)=>Close();
    private void Main_Click(object sender,RoutedEventArgs e)=>_main.ShowFromTray();
    private async void Pin_Changed(object sender,RoutedEventArgs e){if(_ready)try{await PersistPreferencesAsync();}catch(Exception ex){_main.Model.Status="迷你窗偏好尚未保存："+ex.Message;}}
    private async void DragPanel_MouseLeftButtonDown(object sender,MouseButtonEventArgs e)
    {
        if(e.ChangedButton!=MouseButton.Left)return;
        for(DependencyObject? current=e.OriginalSource as DependencyObject;current!=null;current=VisualTreeHelper.GetParent(current))if(current is ButtonBase)return;
        try{DragMove();await PersistPreferencesAsync();}catch(InvalidOperationException){}catch(Exception ex){_main.Model.Status="迷你窗位置尚未保存："+ex.Message;}
    }
    internal async Task PersistPreferencesAsync()
    {
        var position=Left.ToString("R",CultureInfo.InvariantCulture)+";"+Top.ToString("R",CultureInfo.InvariantCulture);var pinned=Topmost;
        var previous=_preferencesWrite;
        async Task SaveAfterAsync()
        {
            try{await previous;}catch(Exception){} // A later explicit preference edit may retry a failed write.
            await Task.Run(()=>{_main.Model.Database.SaveSetting("mini-position",position);_main.Model.Database.SaveSetting("mini-topmost",pinned?"true":"false");});
        }
        _preferencesWrite=SaveAfterAsync();await _preferencesWrite;
    }
    internal Task FlushPreferencesAsync()=>_preferencesWrite;
    private async void Mini_KeyDown(object sender,KeyEventArgs e)
    {if(e.Key==Key.Escape){e.Handled=true;Hide();return;}if(Keyboard.Modifiers!=ModifierKeys.Control)return;if(e.Key==Key.Space){e.Handled=true;await _main.Model.PauseResumeCommand.ExecuteAsync(null);}else if(e.Key==Key.Enter){e.Handled=true;await _main.Model.StartCommand.ExecuteAsync(null);}}
}
