using YanJi.Core;
using YanJi.Data;
namespace YanJi.Tests;
public sealed class KnowledgePackageLifecycleTests
{
    internal static Dictionary<string,string> Targets => new(){{"test","408"}};
    [Fact] public void FileSqliteInstallIsAtomicIdempotentAndStable()
    {
        var root=M1TestDirectories.NewDirectory();using var db=new YanJiDatabase(root);var p=KnowledgePackageFormatTests.Example();
        var plan=db.PreviewPackage(p,"legacy-default",Targets);Assert.Equal(2,plan.Added);Assert.Empty(db.KnowledgeNodes());
        var result=db.ApplyPackage(plan);Assert.False(result.NoOp);Assert.Equal(2,db.KnowledgeNodes().Count);
        db.ValidateBackup(result.BackupPath!);var before=db.KnowledgeNodes();var history=db.PackageHistory(result.InstallId).Count;
        Assert.True(db.ApplyPackage(db.PreviewPackage(p,"legacy-default",Targets)).NoOp);
        Assert.Equal(before,db.KnowledgeNodes());Assert.Equal(history,db.PackageHistory(result.InstallId).Count);
        using var reopened=new YanJiDatabase(root);Assert.Equal(before,reopened.KnowledgeNodes());
    }
    [Fact] public void ThreeWayUpdateAndScopedRollbackPreserveLaterLearningNotesAndLocalChildren()
    {
        var root=M1TestDirectories.NewDirectory();using var db=new YanJiDatabase(root);var v1=KnowledgePackageFormatTests.Example();
        var install=db.ApplyPackage(db.PreviewPackage(v1,"legacy-default",Targets)).InstallId;
        var parent=db.KnowledgeNodes().Single(n=>n.Name=="根目录");var leaf=db.KnowledgeNodes().Single(n=>n.Name=="知识点");
        db.SaveKnowledgeNodes([leaf with{Name="我的标题",Description="我的备注",State=LearningState.Learning,Progress=25}]);
        db.SaveKnowledgeNodes([new("my-child","408","自建孩子",leaf.Id)]);
        var v2=v1 with{PackageVersion="2.0.0",Nodes=[v1.Nodes[0] with{Name="新版根"},v1.Nodes[1] with{Name="新版标题",Description="模板备注"},new("new","test","root",KnowledgeNodeKind.KnowledgePoint,"新点","",1)]};
        var preview=db.PreviewPackage(v2,"legacy-default",Targets);Assert.Equal(2,preview.Conflicts.Count);db.ApplyPackage(preview);
        Assert.Equal(parent.Id,db.KnowledgeNodes().Single(n=>n.Name=="新版根").Id);Assert.Equal("我的备注",db.KnowledgeNodes().Single(n=>n.Id==leaf.Id).Description);
        var added=db.KnowledgeNodes().Single(n=>n.Name=="新点");db.SetLearningState(added.Id,added.Version,LearningState.Learned);
        var record=db.SaveRecord(new("later","408",added.Id,SessionSource.Manual,"后来学习",[new("later-part",new DateOnly(2026,10,14),12345,"Asia/Shanghai",PartKind.DurationOnly)]),"测试");
        var rollback=db.PreviewPackageRollback(install);db.ApplyPackage(rollback);
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(record),System.Text.Json.JsonSerializer.Serialize(db.Records().Single()));Assert.Equal(12345,db.Records().SelectMany(r=>r.Parts).Sum(p=>p.DurationMs));
        Assert.Equal("根目录",db.KnowledgeNodes().Single(n=>n.Id==parent.Id).Name);
        Assert.False(db.KnowledgeNodes().Single(n=>n.Id==added.Id).Archived);Assert.Equal(LearningState.Learned,db.KnowledgeNodes().Single(n=>n.Id==added.Id).State);
        Assert.Contains(db.KnowledgeNodes(),n=>n.Id=="my-child");Assert.Equal("我的标题",db.KnowledgeNodes().Single(n=>n.Id==leaf.Id).Name);
        M1TestDirectories.Report(root,"lifecycle",new{install,preview,rollback,After=db.KnowledgeNodes(),Records=db.Records(),History=db.PackageHistory(install)});
    }
}
