using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
namespace YanJi.App;
public partial class SimpleProgressWorkspace : UserControl
{
    internal SimpleProgressViewModel? Model {get;private set;}
    private MainWindow OwnerWindow=>(MainWindow)Window.GetWindow(this);
    private bool _loading;
    public SimpleProgressWorkspace(){InitializeComponent();}
    private async void Workspace_Loaded(object sender,RoutedEventArgs e)
    {
        if(Model!=null||Window.GetWindow(this) is not MainWindow w)return;
        Model=new(w.Model);DataContext=Model;w.Model.DataRefreshed+=DataRefreshed;await RefreshAsync();
    }
    private async void DataRefreshed()
    {
        if(Model?.Owner.Page!=2)return;
        try
        {
            await RefreshAsync();
            var window=OwnerWindow;
            if(window.KnowledgeExpander.IsExpanded&&window.KnowledgeView.Model is {KnowledgeBusy:false,Selected:{} selected} tree)
            {
                var live=await Task.Run(()=>Model.Owner.Database.KnowledgeNodes(tree.Subject?.Id));
                if(Model.Owner.Page==2&&!tree.KnowledgeBusy&&ReferenceEquals(tree.Selected,selected)&&(!tree.MatchesSnapshot(live)||Model.Owner.AllSubjects.FirstOrDefault(s=>s.Id==tree.Subject?.Id)!=tree.Subject))await tree.ReloadAsync(selected.Node.Id);
            }
        }
        catch(Exception ex){Model.Owner.Status="进度摘要尚未刷新："+ex.Message;}
    }
    internal async Task RefreshAsync()
    {
        if(Model==null)return;_loading=true;
        try
        {
            MathRange.SelectedIndex=Array.IndexOf(new[]{"common","math1","math2","all"},Model.Owner.ReferenceVariant("math"));
            EnglishRange.SelectedIndex=Array.IndexOf(new[]{"common","english1","english2","all"},Model.Owner.ReferenceVariant("english"));
            await Model.RefreshAsync();
        }
        finally{_loading=false;}
    }
    private async void Range_Changed(object sender,SelectionChangedEventArgs e)
    {
        if(_loading||Model==null||((ComboBox)sender).SelectedIndex<0)return;
        var math=sender==MathRange;var variants=math?new[]{"common","math1","math2","all"}:new[]{"common","english1","english2","all"};
        try{Model.Owner.SetReferenceVariant(math?"math":"english",variants[((ComboBox)sender).SelectedIndex]);await RefreshAsync();}
        catch(Exception ex){Model.Owner.Status="范围未变更："+ex.Message;}
    }
    private async void Presets_Click(object sender,RoutedEventArgs e){await OwnerWindow.ApplySimplePresetsAsync();await RefreshAsync();}
    private async void Update_Click(object sender,RoutedEventArgs e)
    {if(((Button)sender).Tag is SimpleSubjectProgress row){await OwnerWindow.QuickProgressAsync(row);await RefreshAsync();}}
    private async void Start_Click(object sender,RoutedEventArgs e)
    {
        if(((Button)sender).Tag is not SimpleSubjectProgress row||Model==null)return;
        if(row.NodeId!=null)await Model.Owner.StartKnowledgeAsync(row.NodeId);
        else{Model.Owner.TimerSubject=Model.Owner.Subjects.FirstOrDefault(s=>s.Id==row.Subject.Id);Model.Owner.TimerChapter=Model.Owner.TimerChapters.FirstOrDefault();Model.Owner.FocusTask=null;Model.Owner.Page=0;await Model.Owner.StartCommand.ExecuteAsync(null);}
    }
    private async void Browse_Click(object sender,RoutedEventArgs e)
    {if(((Button)sender).Tag is SimpleSubjectProgress row)await OwnerWindow.BrowseSimpleSubjectAsync(row.Subject.Id,row.NodeId);}
}
