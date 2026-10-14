using System.Diagnostics;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using YanJi.Core;
using YanJi.Data;
namespace YanJi.Tests;
public sealed class KnowledgePackageSafetyTests
{
    [Fact] public void InitialInstallDirectoryUndoKeepsUsedNodesAndStableArchiveMappings()
    {
        using var d=new YanJiDatabase(M1TestDirectories.NewDirectory());var p=KnowledgePackageFormatTests.Example();var id=d.ApplyPackage(d.PreviewPackage(p,"legacy-default",KnowledgePackageLifecycleTests.Targets)).InstallId;
        var leaf=d.KnowledgeNodes().Single(n=>n.Name=="知识点");d.SaveRecord(new("first-learning","408",leaf.Id,SessionSource.Manual,"",[new("first-part",new DateOnly(2026,10,15),5000,"Asia/Shanghai",PartKind.DurationOnly)]),"测试");
        d.ApplyPackage(d.PreviewPackageRollback(id));Assert.Equal(5000,d.Records().Single().DurationMs);Assert.False(d.KnowledgeNodes().Single(n=>n.Id==leaf.Id).Archived);Assert.Equal(2,d.KnowledgeNodes().Count);
        Assert.True(d.ApplyPackage(d.PreviewPackage(p,"legacy-default",KnowledgePackageLifecycleTests.Targets)).NoOp);Assert.Contains(d.PackageHistory(id),o=>o.Kind=="install"&&o.RolledBack);
    }
    private static string Nodes(YanJiDatabase d)=>JsonSerializer.Serialize(d.KnowledgeNodes());
    [Fact] public void ActiveTimerBlocksPackageApplicationButNotReadOnlyPreview()
    {
        using var d=new YanJiDatabase(M1TestDirectories.NewDirectory());var timer=new TimerMachine();var epoch=DateTimeOffset.Parse("2026-10-15T08:00Z");timer.Start("408",null,"","Asia/Shanghai",new(epoch,0,0,"test"));d.SaveTimer(timer.Snapshot);
        var plan=d.PreviewPackage(KnowledgePackageFormatTests.Example(),"legacy-default",KnowledgePackageLifecycleTests.Targets);Assert.Equal(2,plan.Added);Assert.Throws<InvalidOperationException>(()=>d.ApplyPackage(plan));Assert.Empty(d.InstalledPackages());Assert.NotNull(d.LoadTimer());
    }
    [Theory][InlineData("after-nodes")][InlineData("before-commit")]
    public void FailedInstallRollsBackEveryDatabaseWriteAndCanRetry(string stage)
    {
        var root=M1TestDirectories.NewDirectory();using var d=new YanJiDatabase(root);var plan=d.PreviewPackage(KnowledgePackageFormatTests.Example(),"legacy-default",KnowledgePackageLifecycleTests.Targets);
        var before=YanJiDatabase.LegacyFactFingerprint(d.DatabasePath);d.PackageCheckpoint=s=>{if(s==stage)throw new IOException("synthetic failure");};
        Assert.Throws<IOException>(()=>d.ApplyPackage(plan));Assert.Empty(d.InstalledPackages());Assert.Empty(d.KnowledgeNodes());Assert.Equal(before,YanJiDatabase.LegacyFactFingerprint(d.DatabasePath));
        d.PackageCheckpoint=null;d.ApplyPackage(plan);Assert.Equal(2,d.KnowledgeNodes().Count);
        M1TestDirectories.Report(root,"atomic-retry",new{stage,before,After=d.KnowledgeNodes(),History=d.PackageHistory(plan.InstallId)});
    }
    [Fact] public void ExplicitConflictResolutionAndLaterEditRollbackConflictArePreserved()
    {
        using var d=new YanJiDatabase(M1TestDirectories.NewDirectory());var p=KnowledgePackageFormatTests.Example();var install=d.ApplyPackage(d.PreviewPackage(p,"legacy-default",KnowledgePackageLifecycleTests.Targets)).InstallId;
        var leaf=d.KnowledgeNodes().Single(n=>n.Name=="知识点");d.SaveKnowledgeNodes([leaf with{Name="我的标题"}]);
        var v2=p with{PackageVersion="2.0.0",Nodes=[p.Nodes[0],p.Nodes[1] with{Name="新模板"}]};var plan=d.PreviewPackage(v2,"legacy-default",KnowledgePackageLifecycleTests.Targets);
        var choices=new Dictionary<string,PackageChoice>{{"leaf:Name",PackageChoice.UseTemplate}};Assert.Single(plan.Conflicts);d.ApplyPackage(plan,choices);
        leaf=d.KnowledgeNodes().Single(n=>n.Id==leaf.Id);Assert.Equal("新模板",leaf.Name);d.SaveKnowledgeNodes([leaf with{Name="后来标题",Description="后来备注",State=LearningState.Learning,Progress=42}]);
        var undo=d.PreviewPackageRollback(install);Assert.Contains(undo.Conflicts,c=>c.Key=="leaf:Name");d.ApplyPackage(undo);
        leaf=d.KnowledgeNodes().Single(n=>n.Id==leaf.Id);Assert.Equal("后来标题",leaf.Name);Assert.Equal("后来备注",leaf.Description);Assert.Equal(42,leaf.Progress);
    }
    [Fact] public void RemovalPreservesUsedNodesLocalChildrenAndExplicitArchiveAndReintroductionIsNotSilent()
    {
        using var d=new YanJiDatabase(M1TestDirectories.NewDirectory());var p=KnowledgePackageFormatTests.Example();d.ApplyPackage(d.PreviewPackage(p,"legacy-default",KnowledgePackageLifecycleTests.Targets));
        var leaf=d.KnowledgeNodes().Single(n=>n.Name=="知识点");d.SaveKnowledgeNodes([new("own","408","本地孩子",leaf.Id)]);
        var v2=p with{PackageVersion="2.0.0",Nodes=[]};var remove=d.PreviewPackage(v2,"legacy-default",KnowledgePackageLifecycleTests.Targets);Assert.Equal(0,remove.Archived);d.ApplyPackage(remove);Assert.False(d.KnowledgeNodes().Single(n=>n.Id==leaf.Id).Archived);
        var v3=p with{PackageVersion="3.0.0",Nodes=[p.Nodes[0],p.Nodes[1] with{Name="上游重引入"}]};var reintroduce=d.PreviewPackage(v3,"legacy-default",KnowledgePackageLifecycleTests.Targets);Assert.Contains(reintroduce.Conflicts,c=>c.Field=="Binding");d.ApplyPackage(reintroduce);
        Assert.Equal("知识点",d.KnowledgeNodes().Single(n=>n.Id==leaf.Id).Name);Assert.Contains(d.KnowledgeNodes(),n=>n.Id=="own");
        leaf=d.KnowledgeNodes().Single(n=>n.Id==leaf.Id);d.SaveKnowledgeNodes([leaf with{Archived=true}]);var v4=v3 with{PackageVersion="4.0.0"};d.ApplyPackage(d.PreviewPackage(v4,"legacy-default",KnowledgePackageLifecycleTests.Targets));Assert.True(d.KnowledgeNodes().Single(n=>n.Id==leaf.Id).Archived);
    }
    [Fact] public void StalePreviewAndForgedChangesCannotBeApplied()
    {
        using var d=new YanJiDatabase(M1TestDirectories.NewDirectory());var p=KnowledgePackageFormatTests.Example();var plan=d.PreviewPackage(p,"legacy-default",KnowledgePackageLifecycleTests.Targets);
        var forged=plan with{Changes=[new(null,new("forged","408","篡改"),"篡改")]};d.ApplyPackage(forged);Assert.DoesNotContain(d.KnowledgeNodes(),n=>n.Id=="forged");
        var v2=p with{PackageVersion="2.0.0"};plan=d.PreviewPackage(v2,"legacy-default",KnowledgePackageLifecycleTests.Targets);var node=d.KnowledgeNodes()[0];d.SaveKnowledgeNodes([node with{Description="修改"}]);Assert.Throws<InvalidOperationException>(()=>d.ApplyPackage(plan));
        var backup=Path.Combine(d.DirectoryPath,"restore.yanjibak");d.Backup(backup);plan=d.PreviewPackage(v2,"legacy-default",KnowledgePackageLifecycleTests.Targets);d.RestoreBackup(backup);Assert.Throws<InvalidOperationException>(()=>d.ApplyPackage(plan));
    }
    [Fact] public void IdentityVersionTargetAndMergedCycleConflictsAreRejected()
    {
        using var d=new YanJiDatabase(M1TestDirectories.NewDirectory());var p=KnowledgePackageFormatTests.Example();d.ApplyPackage(d.PreviewPackage(p,"legacy-default",KnowledgePackageLifecycleTests.Targets));
        Assert.Throws<InvalidDataException>(()=>d.PreviewPackage(p with{Name="同版本篡改"},"legacy-default",KnowledgePackageLifecycleTests.Targets));
        Assert.Throws<InvalidDataException>(()=>d.PreviewPackage(p with{PackageVersion="2.0.0",Maintainer="不同维护方"},"legacy-default",KnowledgePackageLifecycleTests.Targets));
        Assert.Throws<InvalidDataException>(()=>d.PreviewPackage(p,"missing",KnowledgePackageLifecycleTests.Targets));
        Assert.Throws<InvalidDataException>(()=>d.PreviewPackage(p,"legacy-default",new Dictionary<string,string>{{"test","missing"}}));
        var v2=p with{PackageVersion="2.0.0",Nodes=[..p.Nodes,new("other","test","root",KnowledgeNodeKind.KnowledgePoint,"另一个","",1)]};d.ApplyPackage(d.PreviewPackage(v2,"legacy-default",KnowledgePackageLifecycleTests.Targets));
        var leaf=d.KnowledgeNodes().Single(n=>n.Name=="知识点");var other=d.KnowledgeNodes().Single(n=>n.Name=="另一个");d.SaveKnowledgeNodes([leaf with{ParentId=other.Id}]);
        // B: siblings; L: leaf under other; U: other under leaf. Each tree is legal; the merge is cyclic.
        var v3=v2 with{PackageVersion="3.0.0",Nodes=[..p.Nodes,v2.Nodes[2] with{ParentId="leaf",Order=0}]};
        var preview=d.PreviewPackage(v3,"legacy-default",KnowledgePackageLifecycleTests.Targets);Assert.NotNull(preview.Error);var unchanged=Nodes(d);Assert.Throws<InvalidDataException>(()=>d.ApplyPackage(preview));Assert.Equal(unchanged,Nodes(d));
    }
    [Fact] public void PackageHistoryAndBindingsSurviveFullBackupRestore()
    {
        using var d=new YanJiDatabase(M1TestDirectories.NewDirectory());var p=KnowledgePackageFormatTests.Example();var id=d.ApplyPackage(d.PreviewPackage(p,"legacy-default",KnowledgePackageLifecycleTests.Targets)).InstallId;
        var backup=Path.Combine(d.DirectoryPath,"packages.yanjibak");var nodes=Nodes(d);var history=JsonSerializer.Serialize(d.PackageHistory(id));d.Backup(backup);d.ValidateBackup(backup);
        var leaf=d.KnowledgeNodes()[1];d.SaveKnowledgeNodes([leaf with{Name="改动"}]);d.RestoreBackup(backup);
        Assert.Equal(nodes,Nodes(d));Assert.Equal(history,JsonSerializer.Serialize(d.PackageHistory(id)));Assert.Equal(p.PackageVersion,d.InstalledPackages().Single().Version);
    }
    [Fact] public void TenThousandNodePackageUsesFileSqliteAndHasBoundedCost()
    {
        var root=M1TestDirectories.NewDirectory();using var d=new YanJiDatabase(root);var p=KnowledgePackageFormatTests.Example() with{Nodes=Enumerable.Range(0,10000).Select(i=>new PackageNode("node"+i,"test",null,KnowledgeNodeKind.KnowledgePoint,"知识点"+i,"",i)).ToList()};
        var clock=Stopwatch.StartNew();var preview=d.PreviewPackage(p,"legacy-default",KnowledgePackageLifecycleTests.Targets);var previewMs=clock.Elapsed.TotalMilliseconds;clock.Restart();d.ApplyPackage(preview);var applyMs=clock.Elapsed.TotalMilliseconds;
        clock.Restart();d.Backup(Path.Combine(root,"scale.yanjibak"));var backupMs=clock.Elapsed.TotalMilliseconds;
        Assert.Equal(10000,d.KnowledgeNodes().Count);Assert.True(previewMs<5000);Assert.True(applyMs<15000);Assert.True(backupMs<10000);
        M1TestDirectories.Report(root,"package-performance",new{Nodes=10000,PreviewMs=previewMs,ApplyMs=applyMs,BackupMs=backupMs,RealFileSqlite=true});
    }
}
