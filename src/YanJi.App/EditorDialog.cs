using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;

namespace YanJi.App;
public sealed record Choice(string Value,string Label);
public sealed record Field(string Key,string Label,string Value="",List<Choice>? Choices=null,bool Multiline=false,bool ReadOnly=false);
public sealed class EditorDialog : Window
{
    private readonly Dictionary<string,Control> _controls=[];
    public Dictionary<string,string>? Values {get;private set;}
    internal void SetField(string key,string value){if(_controls[key] is TextBox text)text.Text=value;else ((ComboBox)_controls[key]).SelectedValue=value;}
    public EditorDialog(Window owner,string title,IEnumerable<Field> fields,string hint="",Func<Dictionary<string,string>,string?>? validate=null)
    {
        Style=(Style)FindResource(typeof(Window));Owner=owner;Title=title;Width=600;SizeToContent=SizeToContent.Height;MinWidth=480;MinHeight=300;MaxHeight=Math.Min(720,SystemParameters.WorkArea.Height);
        WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var root=new DockPanel{Margin=new Thickness(24)};
        var footer=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};DockPanel.SetDock(footer,Dock.Bottom);
        var cancel=new Button{Content="取消",IsCancel=true};footer.Children.Add(cancel);
        var save=new Button{Content="保存",IsDefault=true,Style=(Style)FindResource("Primary")};footer.Children.Add(save);root.Children.Add(footer);
        var errorText=new TextBlock{Foreground=ThemeService.Brush("Danger"),TextWrapping=TextWrapping.Wrap,Visibility=Visibility.Collapsed,Margin=new Thickness(0,0,0,12)};
        DockPanel.SetDock(errorText,Dock.Bottom);root.Children.Add(errorText);
        var content=new StackPanel();content.Children.Add(new TextBlock{Text=title,Style=(Style)FindResource("Subheading")});if(hint.Length>0)content.Children.Add(new TextBlock{Text=hint,Style=(Style)FindResource("MutedText"),Margin=new Thickness(0,0,0,20)});
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
            control.Style=(Style)FindResource(control is TextBox?"FocusTextBox":"FocusComboBox");control.ToolTip=field.Label;AutomationProperties.SetName(control,field.Label);_controls.Add(field.Key,control);content.Children.Add(control);
        }
        root.Children.Add(new ScrollViewer{Content=content,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,MaxHeight=Math.Min(530,SystemParameters.WorkArea.Height-190)});Content=root;
        save.Click+=(_,_)=>
        {
            var values=_controls.ToDictionary(pair=>pair.Key,pair=>pair.Value is TextBox text?text.Text:((ComboBox)pair.Value).SelectedValue?.ToString()??"");
            try{var error=validate?.Invoke(values);if(error!=null){errorText.Text=error;errorText.Visibility=Visibility.Visible;return;}
                Values=values;DialogResult=true;}
            catch(Exception e){errorText.Text=e.Message;errorText.Visibility=Visibility.Visible;}
        };
        Loaded+=(_,_)=>_controls.Values.FirstOrDefault(c=>c.IsEnabled)?.Focus();
    }
}
