using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace YanJi.App;
public sealed record Choice(string Value,string Label);
public sealed record Field(string Key,string Label,string Value="",List<Choice>? Choices=null,bool Multiline=false,bool ReadOnly=false);
public sealed class EditorDialog : Window
{
    private readonly Dictionary<string,Control> _controls=[];
    public Dictionary<string,string>? Values {get;private set;}
    public EditorDialog(Window owner,string title,IEnumerable<Field> fields,string hint="",Func<Dictionary<string,string>,string?>? validate=null)
    {
        Style=(Style)FindResource(typeof(Window));Owner=owner;Title=title;Width=600;Height=660;MinWidth=480;MinHeight=420;MaxHeight=SystemParameters.WorkArea.Height;
        WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var root=new DockPanel{Margin=new Thickness(24)};
        var footer=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};DockPanel.SetDock(footer,Dock.Bottom);
        var cancel=new Button{Content="取消",IsCancel=true};footer.Children.Add(cancel);
        var save=new Button{Content="保存",IsDefault=true,Style=(Style)FindResource("Primary")};footer.Children.Add(save);root.Children.Add(footer);
        var content=new StackPanel();if(hint.Length>0)content.Children.Add(new TextBlock{Text=hint,Margin=new Thickness(0,0,0,16)});
        foreach(var field in fields)
        {
            content.Children.Add(new TextBlock{Text=field.Label});Control control;
            if(field.Choices!=null)
            {
                var box=new ComboBox{ItemsSource=field.Choices,DisplayMemberPath="Label",SelectedValuePath="Value",SelectedValue=field.Value};
                if(box.SelectedIndex<0&&field.Choices.Count>0)box.SelectedIndex=0;control=box;
            }
            else control=new TextBox{Text=field.Value,AcceptsReturn=field.Multiline,TextWrapping=TextWrapping.Wrap,IsReadOnly=field.ReadOnly,
                Height=field.Multiline?112:double.NaN,VerticalScrollBarVisibility=field.Multiline?ScrollBarVisibility.Auto:ScrollBarVisibility.Hidden};
            control.ToolTip=field.Label;_controls.Add(field.Key,control);content.Children.Add(control);
        }
        root.Children.Add(new ScrollViewer{Content=content,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});Content=root;
        save.Click+=(_,_)=>
        {
            var values=_controls.ToDictionary(pair=>pair.Key,pair=>pair.Value is TextBox text?text.Text:((ComboBox)pair.Value).SelectedValue?.ToString()??"");
            try{var error=validate?.Invoke(values);if(error!=null){MessageBox.Show(this,error,"请检查输入",MessageBoxButton.OK,MessageBoxImage.Information);return;}
                Values=values;DialogResult=true;}
            catch(Exception e){MessageBox.Show(this,e.Message,"请检查输入",MessageBoxButton.OK,MessageBoxImage.Information);}
        };
    }
}
