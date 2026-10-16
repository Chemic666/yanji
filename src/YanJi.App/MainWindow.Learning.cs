using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using YanJi.Core;
namespace YanJi.App;
public partial class MainWindow
{
    private List<Choice> TaskChoices()=>new[]{new Choice("","不关联任务")}.Concat(Model.Database.Tasks().Select(t=>new Choice(t.Id,t.Title+(t.Done?" · 已完成":"")))).ToList();
    internal Task EditNodeWeakAsync(WeakPoint point)=>EditWeakAsync(point);
    internal async Task OpenKnowledgeAsync(string id)
    {
        Model.DrawerOpen=false;Model.Page=2;KnowledgeExpander.IsExpanded=true;await Task.Delay(50);
        var workspace=KnowledgeView.Model;if(workspace==null)return;var node=Model.Database.KnowledgeNodes().Single(n=>n.Id==id);
        workspace.ShowArchived=true;workspace.Subject=workspace.Subjects.First(s=>s.Id==node.SubjectId);await workspace.ReloadAsync(id);
    }
    private async void RecordKnowledge_Click(object sender,RoutedEventArgs e)
    {if(Model.SelectedRecord?.Record.ChapterId is {} id)await OpenKnowledgeAsync(id);else Model.Status="本次学习未指定知识节点，可在编辑整次记录中关联。";}
    private async void RecordWeakLinks_Click(object sender,RoutedEventArgs e)
    {if(Model.SelectedRecord is {} row)await EditStudyWeakAsync(row.Record);}
    internal async Task EditStudyWeakAsync(StudyRecord original)
    {
        if(Model.TimerActive&&Model.Timer.SessionId==original.Id){Model.Status="请先结束计时。";return;}
        var record=Model.Database.Records(null,true).Single(r=>r.Id==original.Id);
        var choices=new[]{new Choice("","不新增关联")}.Concat(Model.Database.WeakPoints().Where(w=>w.ProfileId==record.Context!.ProfileId&&(w.SubjectId==null||w.SubjectId==record.SubjectId)).Select(w=>new Choice(w.Id,w.Title))).ToList();
        var v=Edit("本次学习 · 薄弱点",[new("existing","关联现有薄弱点（可选）","",choices),new("remove","现有关联","keep",[new("keep","保留现有关联"),new("clear","解除现有关联（保留薄弱点和历史）")]),new("title","新薄弱点名称（可选）"),new("detail","困难与后续复习（可选）","",Multiline:true)],
            "关联不会改变学习时长或薄弱点状态。可选择现有薄弱点，或新建一个；本次操作会保留修订历史。",
            values=>values["existing"].Length>0&&values["title"].Trim().Length>0?"请分别执行关联现有薄弱点和新建薄弱点。":null);
        if(v==null)return;
        if(v["title"].Trim().Length>0)
        {await Model.RunAsync(()=>Model.Database.AddWeakPointToStudy(record.Id,record.Version,v["title"],v["detail"]),"薄弱点及关联已原子保存");return;}
        var ids=(v["remove"]=="clear"?Array.Empty<string>():record.Context!.WeakPoints?.Select(w=>w.Id).ToArray()??[]).Concat(v["existing"].Length==0?[]:new[]{v["existing"]}).Distinct().ToArray();
        await Model.RunAsync(()=>Model.Database.SetStudyLinks(record.Id,record.Version,record.Context!.ProfileId,record.ChapterId,record.Context.TaskId,ids),"薄弱点关联已更新，时长不变");
    }
    private async void CompleteStudy_Click(object sender,RoutedEventArgs e)
    {
        if(Model.CompletedStudy is not {} previous)return;
        var record=Model.Database.Records().SingleOrDefault(r=>r.Id==previous.Id);
        if(record==null){Model.CompletedStudy=null;Model.Status="这条学习记录已删除或不在当前数据中，未修改任何内容。";return;}
        var v=Edit("已保存 · 可选补充",[new("note","本次备注（可选）",record.Note,Multiline:true),new("state","知识节点学习状态","keep",[new("keep","不修改"),new("0","未开始"),new("1","学习中"),new("2","已学习"),new("3","暂缓")]),new("weak","继续记录薄弱点","0",[new("0","跳过"),new("1","打开薄弱点关联")])],"关闭即可跳过，实际学习时间已保存。进度、自评掌握和验收独立。知识进度作为独立用户操作记录。");
        if(v==null)return;
        if(!await Model.RunAsync(()=>{if(record.Note!=v["note"])Model.Database.SaveRecord(record with{Note=v["note"]},"学习结束补充备注");},"学习备注已保存"))return;
        if(v["state"]!="keep"&&record.ChapterId is {} nodeId)
        {await Model.RunAsync(()=>{var n=Model.Database.KnowledgeNodes().Single(n=>n.Id==nodeId);Model.Database.SetLearningState(n.Id,n.Version,(LearningState)int.Parse(v["state"]));},"学习状态已按你的选择更新，掌握程度未变化");}
        if(v["weak"]=="1")await EditStudyWeakAsync(record);Model.CompletedStudy=null;
    }
    internal async Task AddNodeTaskAsync(KnowledgeNode node)
    {await EditTaskAsync(new LearningTask(Guid.NewGuid().ToString("N"),"学习 · "+node.Name,Model.LocalToday(),SubjectId:node.SubjectId,ChapterId:node.Id,ProfileId:node.ProfileId));}
    internal async Task AddNodeWeakAsync(KnowledgeNode node)
    {await EditWeakAsync(new WeakPoint(Guid.NewGuid().ToString("N"),node.Name,"","",node.SubjectId,node.Id,ProfileId:node.ProfileId));}
}
