using Microsoft.Data.Sqlite;
using YanJi.Core;
using YanJi.Data;
namespace YanJi.Tests;
public sealed class LearningMigrationTests
{
    [Fact] public void Schema3UpgradePreservesFactsPackagesAndOldBackup()
    {
        var root=Schema3Fixture.Create();var before=YanJiDatabase.Schema3Fingerprint(Path.Combine(root,"yanji.sqlite"));
        Assert.Throws<InvalidDataException>(()=>new YanJiDatabase(root));
        var result=YanJiDatabase.UpgradeSchema3Isolated(root);
        Assert.Equal(before,result.BeforeFingerprint);Assert.Equal(before,result.AfterFingerprint);
        using var d=new YanJiDatabase(root);Assert.All(d.Records(null,true),r=>{Assert.NotNull(r.Context);Assert.Equal(ContextCapture.Legacy,r.Context.Capture);Assert.Null(r.Context.NodePath);});
        Assert.Equal("old-task",d.Records(null,true).Single(r=>r.Id=="old-deleted-timer").Context!.TaskId);
        d.ValidateBackup(result.BackupPath);d.RestoreBackup(result.BackupPath);Assert.Equal(before,YanJiDatabase.Schema3Fingerprint(d.DatabasePath));
        var modern=Path.Combine(root,"modern.yanjibak");d.Backup(modern);d.ValidateBackup(modern);d.RestoreBackup(modern);
        M1TestDirectories.Report(root,"schema3-upgrade",new{result,Before=before,Schema=4});
    }
    [Theory][InlineData("after-ddl")][InlineData("before-replace")][InlineData("after-replace")]
    public void FailedUpgradeKeepsUsableSchema3(string stage)
    {
        var root=Schema3Fixture.Create();var path=Path.Combine(root,"yanji.sqlite");var before=YanJiDatabase.Schema3Fingerprint(path);
        Assert.Throws<IOException>(()=>YanJiDatabase.UpgradeSchema3Isolated(root,s=>{if(s==stage)throw new IOException("synthetic "+stage);}));
        Assert.Equal(before,YanJiDatabase.Schema3Fingerprint(path));using var c=new SqliteConnection($"Data Source={path};Mode=ReadOnly;Pooling=False");c.Open();using var cmd=c.CreateCommand();cmd.CommandText="PRAGMA user_version";Assert.Equal(3L,cmd.ExecuteScalar());cmd.CommandText="PRAGMA integrity_check";Assert.Equal("ok",cmd.ExecuteScalar());
        M1TestDirectories.Report(root,"schema3-fault",new{stage,Before=before,Schema=3});
    }
}
