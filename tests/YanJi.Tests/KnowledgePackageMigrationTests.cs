using Microsoft.Data.Sqlite;
using YanJi.Data;
namespace YanJi.Tests;
public sealed class KnowledgePackageMigrationTests
{
    [Fact] public void FrozenSchema2UpgradePreservesAllPreviousFactsAndFormat2BackupRestores()
    {
        var root=Schema2Fixture.Create();var before=YanJiDatabase.Schema2Fingerprint(Path.Combine(root,"yanji.sqlite"));
        Assert.Throws<InvalidDataException>(()=>new YanJiDatabase(root));var result=YanJiDatabase.UpgradeSchema2Isolated(root);
        Assert.Equal(before,result.BeforeFingerprint);Assert.Equal(before,result.AfterFingerprint);
        using var d=new YanJiDatabase(root);d.ValidateBackup(result.BackupPath);Assert.Equal(2,d.KnowledgeNodes().Count);var parts=d.Records(null,true).SelectMany(r=>r.Parts).Select(p=>p.Id+":"+p.DurationMs).Order().ToArray();
        d.ApplyPackage(d.PreviewPackage(KnowledgePackageFormatTests.Example(),"legacy-default",KnowledgePackageLifecycleTests.Targets));d.RestoreBackup(result.BackupPath);
        Assert.Empty(d.InstalledPackages());Assert.Equal(parts,d.Records(null,true).SelectMany(r=>r.Parts).Select(p=>p.Id+":"+p.DurationMs).Order().ToArray());Assert.Equal(before,YanJiDatabase.Schema2Fingerprint(d.DatabasePath));
        M1TestDirectories.Report(root,"schema2-upgrade",new{result,Before=before,Parts=parts});
    }
    [Theory][InlineData("after-ddl")][InlineData("before-replace")][InlineData("after-replace")]
    public void MigrationFaultLeavesUsableUnchangedSchema2(string stage)
    {
        var root=Schema2Fixture.Create();var before=YanJiDatabase.Schema2Fingerprint(Path.Combine(root,"yanji.sqlite"));
        Assert.Throws<IOException>(()=>YanJiDatabase.UpgradeSchema2Isolated(root,s=>{if(s==stage)throw new IOException("synthetic failure");}));
        Assert.Equal(before,YanJiDatabase.Schema2Fingerprint(Path.Combine(root,"yanji.sqlite")));
        using var c=new SqliteConnection($"Data Source={Path.Combine(root,"yanji.sqlite")};Mode=ReadOnly;Pooling=False");c.Open();using var cmd=c.CreateCommand();cmd.CommandText="PRAGMA user_version";Assert.Equal(2L,cmd.ExecuteScalar());cmd.CommandText="PRAGMA integrity_check";Assert.Equal("ok",cmd.ExecuteScalar());
        M1TestDirectories.Report(root,"schema2-fault",new{stage,Before=before,After=YanJiDatabase.Schema2Fingerprint(Path.Combine(root,"yanji.sqlite")),Schema=2});
    }
}
