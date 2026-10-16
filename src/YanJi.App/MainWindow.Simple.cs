using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using YanJi.Core;
namespace YanJi.App;
public partial class MainWindow
{
    private void OpenActions_Click(object sender,RoutedEventArgs e)
    {
        var button=(Button)sender;
        if(button.ContextMenu is {} menu){menu.PlacementTarget=button;menu.IsOpen=true;}
    }
    private async void MorePage_Click(object sender,RoutedEventArgs e)
    {Model.Page=int.Parse((string)((Button)sender).Tag);MainScroll.ScrollToTop();if(Model.Page==2)await SimpleProgressView.RefreshAsync();}
    private async void LegacyTasks_Click(object sender,RoutedEventArgs e){ShowLegacyProgress();await SimpleProgressView.RefreshAsync();}
    private void Navigation_Click(object sender,MouseButtonEventArgs e)
    {if(ItemsControl.ContainerFromElement(NavigationList,e.OriginalSource as DependencyObject) is ListBoxItem item&&NavigationList.Items.IndexOf(item)==3){Model.Page=7;MainScroll.ScrollToTop();}}
    private void Navigation_KeyDown(object sender,KeyEventArgs e)
    {if(e.Key==Key.Enter&&Keyboard.Modifiers==ModifierKeys.None&&NavigationList.SelectedIndex==3){Model.Page=7;MainScroll.ScrollToTop();e.Handled=true;}}
    private async void Presets_Click(object sender,RoutedEventArgs e)=>await ApplySimplePresetsAsync();
    internal async Task BrowseSimpleSubjectAsync(string subjectId,string? nodeId=null)
    {
        Model.Page=2;KnowledgeExpander.IsExpanded=true;await Task.Delay(60);
        if(KnowledgeView.Model is {} knowledge){knowledge.Subject=knowledge.Subjects.FirstOrDefault(s=>s.Id==subjectId);await knowledge.ReloadAsync(nodeId);}
        KnowledgeExpander.BringIntoView();
    }
    internal async Task ApplySimplePresetsAsync(bool confirm=true)
    {
        if(Model.TimerActive){Model.Status="请先结束或核对计时，再应用参考目录。";return;}
        var latest=await Task.Run(MainViewModel.LatestReferences);
        var existing=Model.Database.KnowledgeNodes();var installed=Model.Database.InstalledPackages();
        var choices=new[]{("math","math1"),("english","english1"),("408","408")};
        var targets=choices.Where(t=>Model.Subjects.Any(s=>s.Id==t.Item1)&&!existing.Any(n=>n.SubjectId==t.Item1)&&!installed.Any(i=>i.Targets.Values.Contains(t.Item1))).ToList();
        if(targets.Count==0){Model.Status="已有目录原样保留。需要更新时，请在完整知识目录中打开知识包，查看差异后确认。";return;}
        var text=string.Join("\n",targets.Select(t=>Model.Subjects.First(s=>s.Id==t.Item1).Name+" · "+latest.Single(p=>p.PackageId=="org.yanji.reference."+t.Item2).Nodes.Count+" 个参考节点"));
        if(confirm&&!Confirm("应用参考目录",text+"\n\n仅为上述空白科目应用原创参考目录。已有节点、进度及学习记录不会被覆盖；不是官方年度大纲。数学/英语尚未确定时先显示共同基础。","应用目录"))return;
        foreach(var target in targets)
        {
            var package=latest.Single(p=>p.PackageId=="org.yanji.reference."+target.Item2);
            if(!await Model.RunAsync(()=>Model.Database.ApplyPackage(Model.Database.PreviewPackage(package,"legacy-default",new Dictionary<string,string>{{target.Item1,target.Item1}})),"参考目录已应用；学习进度和时长保持独立"))break;
        }
        await SimpleProgressView.RefreshAsync();
        if(KnowledgeView.Model!=null)await KnowledgeView.Model.ReloadAsync();
    }
    internal async Task QuickProgressAsync(SimpleSubjectProgress row)
    {
        var workspace=SimpleProgressView.Model??new SimpleProgressViewModel(Model);
        var choices=await Task.Run(()=>workspace.Choices(row.Subject.Id));
        if(choices.Count==0){Model.Status="当前范围没有可选章节，可应用参考目录、切换范围或展开完整目录新增章节。";return;}
        var selected=choices.Any(c=>c.Id==row.NodeId)?row.NodeId:choices.First().Id;
        var node=Model.Database.KnowledgeNodes().Single(n=>n.Id==selected);
        var note=Model.Database.ProgressNotes().FirstOrDefault(n=>n.NodeId==selected);
        var dialog=new EditorDialog(this,"快速更新 · "+row.Name,[new("node","章节 *（最近使用优先）",selected!,choices.Select(c=>new Choice(c.Id,c.Label)).ToList()),new("state","学习进度",((int)node.State).ToString(),KnowledgeWorkspaceViewModel.StateLabels.Select((s,i)=>new Choice(i.ToString(),s)).ToList()),new("content","本次学到哪里（可选）",note?.Content??"",Multiline:true),new("next","下一步学什么（可选）",note?.Next??"")],"仅保存进度，不要求先计时，不增加学习时长，也不改变掌握程度。",values=>Required(values,"node"));
        dialog.NodeChanged=id=>
        {
            var n=Model.Database.KnowledgeNodes().Single(n=>n.Id==id);var p=Model.Database.ProgressNotes().FirstOrDefault(p=>p.NodeId==id);
            dialog.SetField("state",((int)n.State).ToString());dialog.SetField("content",p?.Content??"");dialog.SetField("next",p?.Next??"");node=n;
        };
        TestDialogShown?.Invoke(dialog);
        dialog.CommitAsync=v=>Model.RunAsync(()=>Model.Database.SaveProgressNote(v["node"],node.Version,(LearningState)int.Parse(v["state"]),v["content"],v["next"]),"学习进度已保存 · 无需计时");
        dialog.ShowDialog();
    }
}
