using System.Text.Json;
using YanJi.Core;
using YanJi.Data;
namespace YanJi.Tests;
public sealed class KnowledgeRestoreTests
{
    [Fact] public void StaleEditAcrossRestoreCannotOverwriteSameVersionNode()
    {
        var root=M1TestDirectories.NewDirectory();using var db=new YanJiDatabase(root);
        db.SaveKnowledgeNodes([new("a","math","恢复目标")]);var snapshot=db.KnowledgeSnapshot();var old=snapshot.Nodes.Single();
        var backup=Path.Combine(root,"restore.yanjibak");db.Backup(backup);db.RestoreBackup(backup);
        Assert.Throws<InvalidOperationException>(()=>db.SaveKnowledgeNodes([old with{Name="陈旧编辑覆盖"}],expectedEpoch:snapshot.Epoch));
        Assert.Equal("恢复目标",db.KnowledgeNodes().Single().Name);
    }
    [Fact] public void Format2RestoresAllNodeFieldsAndAudit()
    {
        var root=M1TestDirectories.NewDirectory();using var db=new YanJiDatabase(root);
        db.SaveKnowledgeNodes([new("a","math","主干",Archived:true),new("c","math","另一个主干"),
            new("b","math","知识点","c",KnowledgeNodeKind.KnowledgePoint,7,35,Mastery.Weak,false,LearningState.Deferred,"长中文描述")]);
        var before=JsonSerializer.Serialize(db.KnowledgeNodes());var audit=JsonSerializer.Serialize(db.History("b"));
        var backup=Path.Combine(root,"nodes.yanjibak");db.Backup(backup);
        var b=db.KnowledgeNodes().Single(n=>n.Id=="b");db.SaveKnowledgeNodes([b with{Name="新名字",ParentId="a",Kind=KnowledgeNodeKind.Chapter,SortOrder=99,Progress=100,State=LearningState.Learned,Mastery=Mastery.Mastered,Archived=true,Description="被替换"}]);
        db.RestoreBackup(backup);Assert.Equal(before,JsonSerializer.Serialize(db.KnowledgeNodes()));Assert.Equal(audit,JsonSerializer.Serialize(db.History("b")));
        M1TestDirectories.Report(root,"full-node-restore",new{AllFieldsPreserved=true,AuditPreserved=true});
    }
    [Theory][InlineData("before-replace")][InlineData("after-replace")]
    public void RestoreFaultPreservesCurrentSchema2(string failAt)
    {
        var root=M1TestDirectories.NewDirectory();using var db=new YanJiDatabase(root);db.SaveKnowledgeNodes([new("a","math","备份内容")]);
        var backup=Path.Combine(root,"nodes.yanjibak");db.Backup(backup);var a=db.KnowledgeNodes().Single();db.SaveKnowledgeNodes([a with{Name="当前内容"}]);
        var before=JsonSerializer.Serialize(db.KnowledgeNodes());var fingerprint=YanJiDatabase.LegacyFactFingerprint(db.DatabasePath);
        db.RestoreCheckpoint=step=>{if(step==failAt)throw new IOException("Injected restore failure");};
        Assert.Throws<IOException>(()=>db.RestoreBackup(backup));db.RestoreCheckpoint=null;
        Assert.Equal(before,JsonSerializer.Serialize(db.KnowledgeNodes()));Assert.Equal(fingerprint,YanJiDatabase.LegacyFactFingerprint(db.DatabasePath));Assert.Equal("ok",db.Integrity());
        M1TestDirectories.Report(root,"restore-failure",new{Stage=failAt,CurrentSchema2Preserved=true});
    }
}
