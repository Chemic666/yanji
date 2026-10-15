using System.Text.Json;
using YanJi.Core;
using YanJi.Data;
namespace YanJi.Tests;
public sealed class LearningContextTests
{
    private static readonly DateOnly Day=new(2026,10,15);
    internal static YanJiDatabase Fixture()
    {
        var db=new YanJiDatabase(M1TestDirectories.NewDirectory());db.SaveKnowledgeNodes([new("root","math","数学主干"),new("leaf","math","极限","root"),new("other","math","积分"),new("foreign","english","阅读")]);
        db.SaveTask(new("task","复习极限",Day,SubjectId:"math",ChapterId:"leaf"));return db;
    }
    internal static StudyRecord Manual(string id="study",string? node="leaf",long ms=12345)=>new(id,"math",node,SessionSource.Manual,"",[new(id+"-part",Day,ms,"Asia/Shanghai",PartKind.DurationOnly)],Context:new(TaskId:"task"));
    [Fact] public void ManualContextIsPersistedAndLaterTaskOrNodeEditsDoNotRewriteEvidence()
    {
        using var db=Fixture();var saved=db.SaveRecord(Manual(),"补录");Assert.Equal("数学主干 / 极限",saved.Context?.NodePath);Assert.Equal("task",saved.Context?.TaskId);Assert.Equal("复习极限",saved.Context?.TaskTitle);
        var task=db.Tasks().Single();db.SaveTask(task with{Title="新任务标题",ChapterId="other"});var leaf=db.KnowledgeNodes().Single(n=>n.Id=="leaf");db.SaveKnowledgeNodes([leaf with{Name="新的极限",ParentId=null,Archived=true}]);
        var current=db.Records().Single();Assert.Equal(JsonSerializer.Serialize(saved.Context),JsonSerializer.Serialize(current.Context));Assert.Equal(saved.DurationMs,current.DurationMs);
        var edited=db.SaveRecord(current with{Note="只改备注"},"备注");Assert.Equal(saved.Context!.NodePath,edited.Context?.NodePath);
        var corrected=db.SetStudyLinks(edited.Id,edited.Version,"legacy-default","other",null,[]);Assert.Null(corrected.Context?.TaskId);Assert.Equal("积分",corrected.Context?.NodePath);Assert.Equal(saved.DurationMs,corrected.DurationMs);Assert.True(db.History(saved.Id).Count>=3);
    }
    [Fact] public void TaskAndWeakPointScopeAreCheckedWithoutCorruptingHistory()
    {
        using var db=Fixture();db.SaveTask(new("english-task","英语任务",SubjectId:"english"));
        Assert.Throws<ArgumentException>(()=>db.SaveRecord(Manual() with{Context=new(TaskId:"english-task")},"错误关联"));Assert.Empty(db.Records());
        var saved=db.SaveRecord(Manual(),"补录");db.SaveWeakPoint(new("english-weak","阅读",SubjectId:"english",ChapterId:"foreign"));
        Assert.Throws<ArgumentException>(()=>db.SetStudyLinks(saved.Id,saved.Version,"legacy-default","leaf","task",["english-weak"]));
        db.SaveTask(db.Tasks().Single(t=>t.Id=="task") with{SubjectId="english",ChapterId="foreign"});var backup=Path.Combine(db.DirectoryPath,"historical.yanjibak");db.Backup(backup);db.ValidateBackup(backup);db.RestoreBackup(backup);Assert.Equal("leaf",db.Records().Single().ChapterId);
    }
    [Fact] public void WeakPointCreationAndAssociationAreTraceableAndDoNotChangeTimeOrMastery()
    {
        using var db=Fixture();var saved=db.SaveRecord(Manual(),"补录");var weak=db.AddWeakPointToStudy(saved.Id,saved.Version,"等价无穷小适用条件","需复习");
        Assert.Equal("leaf",weak.ChapterId);Assert.Equal(WeakPointState.Open,weak.State);var current=db.Records().Single();Assert.Equal(weak.Id,Assert.Single(current.Context!.WeakPoints!).Id);Assert.Equal(saved.DurationMs,current.DurationMs);
        db.SetStudyLinks(current.Id,current.Version,"legacy-default","leaf","task",[]);Assert.Single(db.WeakPoints());Assert.Empty(db.Records().Single().Context!.WeakPoints!);Assert.Equal(Mastery.Unknown,db.KnowledgeNodes().Single(n=>n.Id=="leaf").Mastery);
    }
    [Fact] public void NodeStatisticsUsePartUnionAndKeepUnlinkedTime()
    {
        using var db=Fixture();db.SaveRecord(Manual(),"叶节点");db.SaveRecord(Manual("root-study","root",2000),"父节点");db.SaveRecord(Manual("unlinked",null,3000),"未关联");
        var summary=db.KnowledgeStudy("root",Day,Day);Assert.Equal(2000,summary.OwnDurationMs);Assert.Equal(14345,summary.SubtreeDurationMs);Assert.Equal(2,summary.SessionCount);Assert.Equal(3000,summary.UnlinkedDurationMs);
        Assert.Equal(14345,db.KnowledgeDuration(["root","leaf"],Day,Day));var leaf=db.Records().Single(r=>r.Id=="study");db.DeleteRecord(leaf.Id,leaf.Version);Assert.Equal(2000,db.KnowledgeStudy("root",Day,Day).SubtreeDurationMs);db.RestoreRecord(leaf.Id,leaf.Version+1);Assert.Equal(14345,db.KnowledgeStudy("root",Day,Day).SubtreeDurationMs);
    }
}
