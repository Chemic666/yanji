using System.Windows;
using System.Windows.Input;
namespace YanJi.App;
public partial class MainWindow
{
    internal MiniTimerWindow? MiniTimer {get;private set;}
    internal void ShowMiniTimer(){MiniTimer??=new(this);MiniTimer.Show();MiniTimer.Activate();}
    private void ShowMini_Click(object sender,RoutedEventArgs e)=>ShowMiniTimer();
    private void CloseMiniTimer(){if(MiniTimer!=null){MiniTimer.ClosingForExit=true;MiniTimer.Close();}}
}
