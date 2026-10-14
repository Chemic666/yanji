using System.Windows;
using System.Windows.Controls;
namespace YanJi.App;
public sealed class ConfirmDialog : Window
{
    public ConfirmDialog(Window owner,string title,string message,string action,bool dangerous=true)
    {
        Owner=owner;Title=title;Width=540;MinWidth=480;SizeToContent=SizeToContent.Height;MaxHeight=SystemParameters.WorkArea.Height;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var root=new DockPanel{Margin=new Thickness(24)};var footer=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};DockPanel.SetDock(footer,Dock.Bottom);
        var cancel=new Button{Content="取消",IsCancel=true,IsDefault=true};footer.Children.Add(cancel);
        var accept=new Button{Content=action,Style=(Style)FindResource(dangerous?"DangerButton":"Primary")};accept.Click+=(_,_)=>DialogResult=true;footer.Children.Add(accept);root.Children.Add(footer);
        var content=new StackPanel{Margin=new Thickness(0,0,0,24)};content.Children.Add(new TextBlock{Text=title,Style=(Style)FindResource("Subheading")});content.Children.Add(new TextBlock{Text=message,TextWrapping=TextWrapping.Wrap});root.Children.Add(new ScrollViewer{Content=content,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,MaxHeight=SystemParameters.WorkArea.Height-180});Content=root;
    }
}
