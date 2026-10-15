using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Data.Sqlite;
using YanJi.Core;
using YanJi.Data;
namespace YanJi.Tests;
public sealed class KnowledgePackageReviewTests
{
    [Fact] public void RebindingTombstoneUsesPreviousTemplateBaselineAndProtectsPersonalMetadata()
    {
        using var d=new YanJiDatabase(M1TestDirectories.NewDirectory());var v1=KnowledgePackageFormatTests.Example();d.ApplyPackage(d.PreviewPackage(v1,"legacy-default",KnowledgePackageLifecycleTests.Targets));
        var leaf=d.KnowledgeNodes().Single(n=>n.Name=="知识点");d.SaveKnowledgeNodes([leaf with{Name="个人标题",Description="个人备注",SortOrder=5}]);
        d.ApplyPackage(d.PreviewPackage(v1 with{PackageVersion="2.0.0",Nodes=[v1.Nodes[0]]},"legacy-default",KnowledgePackageLifecycleTests.Targets));
        var v3=v1 with{PackageVersion="3.0.0",Nodes=[v1.Nodes[0],v1.Nodes[1] with{Name="新模板标题",Description="新模板备注",Order=9}]};
        var choices=new Dictionary<string,PackageChoice>{{"leaf:Binding",PackageChoice.UseTemplate}};
        var plan=d.PreviewPackage(v3,"legacy-default",KnowledgePackageLifecycleTests.Targets,choices);
        Assert.Contains(plan.Conflicts,c=>c.Key=="leaf:Name");Assert.Contains(plan.Conflicts,c=>c.Key=="leaf:Description");Assert.Contains(plan.Conflicts,c=>c.Key=="leaf:SortOrder");d.ApplyPackage(plan,choices);
        var result=d.KnowledgeNodes().Single(n=>n.Id==leaf.Id);Assert.Equal("个人标题",result.Name);Assert.Equal("个人备注",result.Description);Assert.Equal(5,result.SortOrder);
    }
    [Fact] public void ArchivedSubjectRemainsValidInHistoricalPackagesAndFullBackups()
    {
        using var d=new YanJiDatabase(M1TestDirectories.NewDirectory());var p=KnowledgePackageFormatTests.Example();d.ApplyPackage(d.PreviewPackage(p,"legacy-default",KnowledgePackageLifecycleTests.Targets));
        d.SaveSubject(d.Subjects().Single(s=>s.Id=="408") with{Archived=true});var backup=Path.Combine(d.DirectoryPath,"archived.yanjibak");d.Backup(backup);d.ValidateBackup(backup);d.RestoreBackup(backup);
        Assert.True(d.Subjects(true).Single(s=>s.Id=="408").Archived);Assert.Single(d.InstalledPackages());Assert.Throws<InvalidDataException>(()=>d.PreviewPackage(p with{PackageVersion="2.0.0"},"legacy-default",KnowledgePackageLifecycleTests.Targets));
    }
    [Theory][InlineData("null-bindings")][InlineData("invalid-binding")][InlineData("wrong-package")]
    public void CorruptPackageHistoryFailsSemanticBackupValidationBeforeRestore(string corruption)
    {
        using var d=new YanJiDatabase(M1TestDirectories.NewDirectory());var p=KnowledgePackageFormatTests.Example();d.ApplyPackage(d.PreviewPackage(p,"legacy-default",KnowledgePackageLifecycleTests.Targets));d.ApplyPackage(d.PreviewPackage(p with{PackageVersion="2.0.0"},"legacy-default",KnowledgePackageLifecycleTests.Targets));
        var good=Path.Combine(d.DirectoryPath,"good.yanjibak");d.Backup(good);var snapshot=Path.Combine(d.DirectoryPath,"corrupt-snapshot.sqlite");JsonObject manifest;
        using(var zip=ZipFile.OpenRead(good)){zip.GetEntry("database.sqlite")!.ExtractToFile(snapshot);using var r=new StreamReader(zip.GetEntry("manifest.json")!.Open());manifest=JsonNode.Parse(r.ReadToEnd())!.AsObject();}
        using(var c=new SqliteConnection($"Data Source={snapshot};Pooling=False"))
        {
            c.Open();using var cmd=c.CreateCommand();cmd.CommandText="SELECT id,payload FROM knowledge_package_operations ORDER BY at DESC LIMIT 1";string id,raw;using(var r=cmd.ExecuteReader()){r.Read();id=r.GetString(0);raw=r.GetString(1);}var op=JsonNode.Parse(raw)!.AsObject();
            if(corruption=="null-bindings")op["BeforeBindings"]=null;
            if(corruption=="invalid-binding")op["BeforeBindings"]![0]!["NodeId"]="missing-node";
            if(corruption=="wrong-package")op["AfterPayload"]=KnowledgePackageFile.Encode(p with{PackageId="org.yanji.other",PackageVersion="2.0.0"});
            cmd.CommandText="UPDATE knowledge_package_operations SET payload=$p WHERE id=$id";cmd.Parameters.AddWithValue("$p",op.ToJsonString());cmd.Parameters.AddWithValue("$id",id);cmd.ExecuteNonQuery();
        }
        manifest["Sha256"]=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(snapshot)));var corrupt=Path.Combine(d.DirectoryPath,"bad.yanjibak");using(var zip=ZipFile.Open(corrupt,ZipArchiveMode.Create)){zip.CreateEntryFromFile(snapshot,"database.sqlite");using var writer=new StreamWriter(zip.CreateEntry("manifest.json").Open());writer.Write(manifest.ToJsonString());}
        var before=YanJiDatabase.LegacyFactFingerprint(d.DatabasePath);Assert.Throws<InvalidDataException>(()=>d.ValidateBackup(corrupt));Assert.Throws<InvalidDataException>(()=>d.RestoreBackup(corrupt));Assert.Equal(before,YanJiDatabase.LegacyFactFingerprint(d.DatabasePath));
    }
    [Fact] public void RollbackNeverCommitsUntrustedCallerPayloadInsteadOfRegeneratedPlan()
    {
        using var d=new YanJiDatabase(M1TestDirectories.NewDirectory());var p=KnowledgePackageFormatTests.Example();var id=d.ApplyPackage(d.PreviewPackage(p,"legacy-default",KnowledgePackageLifecycleTests.Targets)).InstallId;
        d.ApplyPackage(d.PreviewPackage(p with{PackageVersion="2.0.0"},"legacy-default",KnowledgePackageLifecycleTests.Targets));
        var plan=d.PreviewPackageRollback(id);var forged=KnowledgePackageFile.Encode(p with{PackageVersion="999.0.0",Nodes=[..p.Nodes,new("forged","test",null,KnowledgeNodeKind.Chapter,"伪造节点","",10)]});
        d.ApplyPackage(plan with{Payload=forged});Assert.Equal("1.0.0",d.InstalledPackages().Single().Version);
        var backup=Path.Combine(d.DirectoryPath,"after-undo.yanjibak");d.Backup(backup);d.ValidateBackup(backup);
    }
}
