using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;
using System.Threading.Tasks;

namespace YanJi.App;
public sealed record Choice(string Value,string Label);
public sealed record Field(string Key,string Label,string Value="",List<Choice>? Choices=null,bool Multiline=false,bool ReadOnly=false);
public sealed class EditorDialog : Window
{
    internal Func<Dictionary<string,string>,Task<bool>>? CommitAsync {get;set;}
    internal SaveShortcuts? Shortcuts {get;private set;}
    private bool _saving;
    internal Action<string>? NodeChanged {get;set;}
    private readonly Dictionary<string,Control> _controls=[];
    public Dictionary<string,string>? Values {get;private set;}
    internal void SetField(string key,string value){if(_controls[key] is TextBox text)text.Text=value;else ((ComboBox)_controls[key]).SelectedValue=value;}
    internal void BindChapters(List<YanJi.Core.Chapter> chapters,string? existing)
    {
        if(!_controls.TryGetValue("subject",out var subjectControl)||subjectControl is not ComboBox subject||!_controls.TryGetValue("chapter",out var chapterControl)||chapterControl is not ComboBox chapter)return;
        void Refresh(bool initial)
        {
            var selected=initial?existing:null;var owner=subject.SelectedValue?.ToString();
            var list=chapters.Where(c=>c.SubjectId==owner&&(!c.Archived||initial&&c.Id==existing)).ToList();
            chapter.ItemsSource=new[]{new Choice("",list.Count==0?"暂无章节 · 可不指定":"不指定章节")}.Concat(list.Select(c=>new Choice(c.Id,c.Name+(c.Archived?" · 已归档引用":"")))).ToList();
            chapter.SelectedValue=list.Any(c=>c.Id==selected)?selected:"";
        }
        Refresh(true);subject.SelectionChanged+=(_,_)=>Refresh(false);
        if(chapter.Parent is Panel panel)
        {var manage=new Button{Content="管理章节",Style=(Style)FindResource("QuietButton"),HorizontalAlignment=HorizontalAlignment.Left};manage.Click+=(_,_)=>{if(Owner is MainWindow w){w.Model.Page=2;w.MainScroll.ScrollToTop();}Close();};panel.Children.Insert(panel.Children.IndexOf(chapter)+1,manage);}
    }
    public EditorDialog(Window owner,string title,IEnumerable<Field> fields,string hint="",Func<Dictionary<string,string>,string?>? validate=null)
    {
        Style=(Style)FindResource(typeof(Window));Owner=owner;Title=title;Width=600;SizeToContent=SizeToContent.Height;MinWidth=480;MinHeight=300;MaxHeight=Math.Min(720,SystemParameters.WorkArea.Height);
        WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var root=new DockPanel{Margin=new Thickness(24)};
        var footer=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};DockPanel.SetDock(footer,Dock.Bottom);
        var cancel=new Button{Content="取消",IsCancel=true};footer.Children.Add(cancel);
        var save=new Button{Content="保存",IsDefault=false,Style=(Style)FindResource("Primary")};footer.Children.Add(save);root.Children.Add(footer);
        var errorText=new TextBlock{Foreground=ThemeService.Brush("Danger"),TextWrapping=TextWrapping.Wrap,Visibility=Visibility.Collapsed,Margin=new Thickness(0,0,0,12)};
        DockPanel.SetDock(errorText,Dock.Bottom);root.Children.Add(errorText);
        var content=new StackPanel();content.Children.Add(new TextBlock{Text="单行 Enter 保存 · 多行 Enter 换行 / Ctrl+Enter 保存",Style=(Style)FindResource("MutedText")});content.Children.Add(new TextBlock{Text=title,Style=(Style)FindResource("Subheading")});if(hint.Length>0)content.Children.Add(new TextBlock{Text=hint,Style=(Style)FindResource("MutedText"),Margin=new Thickness(0,0,0,20)});
        foreach(var field in fields)
        {
            content.Children.Add(new TextBlock{Text=field.Label});Control control;
            if(field.Choices!=null)
            {
                var box=new ComboBox{ItemsSource=field.Choices,DisplayMemberPath="Label",SelectedValuePath="Value",SelectedValue=field.Value};
                if(box.SelectedIndex<0&&field.Choices.Count>0)box.SelectedIndex=0;control=box;
                if(field.Key=="node")
                {
                    var search=new TextBox{Style=(Style)FindResource("FocusTextBox"),ToolTip="搜索章节或完整路径",Margin=new Thickness(0,4,0,0)};
                    SaveShortcuts.SetExcluded(search,true);AutomationProperties.SetName(search,"搜索章节或完整路径");content.Children.Add(search);
                    search.TextChanged+=(_,_)=>{var current=box.SelectedValue;box.ItemsSource=field.Choices.Where(c=>c.Label.Contains(search.Text,StringComparison.CurrentCultureIgnoreCase)).ToList();box.SelectedValue=current;};
                    var loadedId=field.Value;
                    box.SelectionChanged+=(_,_)=>{if(box.SelectedValue is string id&&id!=loadedId){loadedId=id;NodeChanged?.Invoke(id);}};
                }
            }
            else control=new TextBox{Text=field.Value,AcceptsReturn=field.Multiline,TextWrapping=TextWrapping.Wrap,IsReadOnly=field.ReadOnly,
                Height=field.Multiline?112:double.NaN,VerticalScrollBarVisibility=field.Multiline?ScrollBarVisibility.Auto:ScrollBarVisibility.Hidden};
            control.Style=(Style)FindResource(control is TextBox?"FocusTextBox":"FocusComboBox");control.ToolTip=field.Label;AutomationProperties.SetName(control,field.Label);_controls.Add(field.Key,control);content.Children.Add(control);
        }
        root.Children.Add(new ScrollViewer{Content=content,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,MaxHeight=Math.Min(530,SystemParameters.WorkArea.Height-190)});Content=root;
        async Task SaveAsync()
        {
            if(_saving)return;
            var values=_controls.ToDictionary(pair=>pair.Key,pair=>pair.Value is TextBox text?text.Text:((ComboBox)pair.Value).SelectedValue?.ToString()??"");
            try
            {
                var error=validate?.Invoke(values);if(error!=null){errorText.Text=error;errorText.Visibility=Visibility.Visible;return;}
                errorText.Visibility=Visibility.Collapsed;
                _saving=true;content.IsEnabled=false;save.IsEnabled=false;cancel.IsEnabled=false;ButtonFeedback.SetState(save,"Saving");
                if(CommitAsync!=null&&!await CommitAsync(values)){errorText.Text="未能保存，输入已保留。请检查提示后重试。";errorText.Visibility=Visibility.Visible;return;}
                Values=values;if(CommitAsync!=null){ButtonFeedback.SetState(save,"Success");await Task.Delay(120);}_saving=false;DialogResult=true;
            }
            catch(Exception e){errorText.Text="未能保存："+e.Message;errorText.Visibility=Visibility.Visible;}
            finally{_saving=false;content.IsEnabled=true;save.IsEnabled=true;cancel.IsEnabled=true;ButtonFeedback.SetState(save,"Idle");}
        }
        save.Click+=async(_,_)=>await SaveAsync();
        Shortcuts=new(this,SaveAsync,()=>!_saving);
        Closing+=(_,e)=>{if(_saving)e.Cancel=true;};
        Loaded+=(_,_)=>_controls.Values.FirstOrDefault(c=>c.IsEnabled)?.Focus();
    }
}
