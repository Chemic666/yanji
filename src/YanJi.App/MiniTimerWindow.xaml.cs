using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
namespace YanJi.App;
public partial class MiniTimerWindow:Window
{
    private readonly MainWindow _main;
    internal bool ClosingForExit {get;set;}
    public MiniTimerWindow(MainWindow main){_main=main;InitializeComponent();DataContext=main.Model;Closing+=HideOnClose;}
    private void HideOnClose(object? sender,CancelEventArgs e){if(!ClosingForExit){e.Cancel=true;Hide();}}
    private void Main_Click(object sender,RoutedEventArgs e)=>_main.ShowFromTray();
    private async void Mini_KeyDown(object sender,KeyEventArgs e)
    {if(Keyboard.Modifiers!=ModifierKeys.Control)return;if(e.Key==Key.Space){e.Handled=true;await _main.Model.PauseResumeCommand.ExecuteAsync(null);}else if(e.Key==Key.Enter){e.Handled=true;await _main.Model.StartCommand.ExecuteAsync(null);}}
}
