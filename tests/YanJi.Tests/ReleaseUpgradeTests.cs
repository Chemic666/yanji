using Microsoft.Data.Sqlite;
using YanJi.Core;
using YanJi.Data;
namespace YanJi.Tests;
public sealed class ReleaseUpgradeTests
{
    [Fact] public void TargetCreatedAfterPreflightMustNeverBeReused()
    {
        var source=LegacySchemaFixture.Create();var target=Target();string? protectedHash=null;
        Assert.Throws<IOException>(()=>YanJiDatabase.PrepareUpgradeCopy(source,target,phase=>{if(phase!="before-target-reservation")return;using(var occupant=new YanJiDatabase(target)){occupant.SaveRecord(new("occupant","math",null,SessionSource.Manual,"独立已有数据",[TimeParts.Duration(new(2026,10,15),TimeSpan.FromMilliseconds(12345),"UTC")]),"synthetic occupant");}protectedHash=YanJiDatabase.LegacyFactFingerprint(Path.Combine(target,"yanji.sqlite"));},null));
        Assert.NotNull(protectedHash);Assert.Equal(protectedHash,YanJiDatabase.LegacyFactFingerprint(Path.Combine(target,"yanji.sqlite")));Assert.False(File.Exists(Path.Combine(target,"upgrade-operation.json")));
    }
    [Fact] public void RollbackUsesTheSameValidatedSnapshotIfInputPathIsReplaced()
    {
        using var original=new YanJiDatabase(M1TestDirectories.NewDirectory());var input=Path.Combine(original.DirectoryPath,"input.yanjibak");original.Backup(input);var output=Target();
        YanJiDatabase.RestoreOriginalToNewDirectory(input,output,phase=>{if(phase!="after-backup-validation")return;var data=System.Text.Encoding.UTF8.GetBytes("invalid replacement");using var z=new System.IO.Compression.ZipArchive(File.Create(input),System.IO.Compression.ZipArchiveMode.Create);using(var stream=z.CreateEntry("database.sqlite").Open())stream.Write(data);using var writer=new StreamWriter(z.CreateEntry("manifest.json").Open());writer.Write(System.Text.Json.JsonSerializer.Serialize(new{Format=4,Schema=4,Application=1498040625,Sha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(data)),Sessions=0,DurationMs=0}));});
        using var restored=new YanJiDatabase(output);Assert.Equal("ok",restored.Integrity());Assert.Empty(restored.Records());
    }
    private static string Target()=>Path.Combine(M1TestDirectories.NewDirectory(),"destination");
    [Theory][InlineData(1)][InlineData(2)][InlineData(3)][InlineData(4)]
    public void FileUpgradeAndOriginalFormatRollbackPreserveEveryFact(int schema)
    {
        var source=schema==1?LegacySchemaFixture.Create():schema==2?Schema2Fixture.Create():schema==3?Schema3Fixture.Create():M1TestDirectories.NewDirectory();
        if(schema==4){using var initial=new YanJiDatabase(source);initial.ApplyPackage(initial.PreviewPackage(KnowledgePackageFormatTests.Example(),"legacy-default",KnowledgePackageLifecycleTests.Targets));var node=initial.KnowledgeNodes().Last();initial.SaveKnowledgeNodes([node with{Name="个人修改",Description="不能被升级覆盖"}]);initial.SaveRecord(new("s4","408",node.Id,SessionSource.Manual,"",[TimeParts.Duration(new(2026,10,15),TimeSpan.FromMilliseconds(23456),"UTC")]),"synthetic");}
        var old=YanJiDatabase.LegacyFactFingerprint(Path.Combine(source,"yanji.sqlite"));var target=Target();var result=YanJiDatabase.PrepareUpgradeCopy(source,target);
        Assert.Equal(schema,result.SourceSchema);Assert.Equal(result.BeforeFingerprint,result.AfterFingerprint);Assert.Equal(old,YanJiDatabase.LegacyFactFingerprint(Path.Combine(source,"yanji.sqlite")));YanJiDatabase.CheckUpgradeReady(target);
        using(var db=new YanJiDatabase(target)){Assert.Equal("ok",db.Integrity());db.ValidateBackup(result.BackupPath);var facts=YanJiDatabase.LegacyFactFingerprint(db.DatabasePath);db.RestoreBackup(result.BackupPath);Assert.Equal(facts,YanJiDatabase.LegacyFactFingerprint(db.DatabasePath));if(schema==4){Assert.Single(db.InstalledPackages());Assert.Contains(db.KnowledgeNodes(),n=>n.Name=="个人修改");}}
        var rollback=Target();YanJiDatabase.RestoreOriginalToNewDirectory(result.BackupPath,rollback);
        using(var c=new SqliteConnection($"Data Source={Path.Combine(rollback,"yanji.sqlite")};Mode=ReadOnly;Pooling=False")){c.Open();using var cmd=c.CreateCommand();cmd.CommandText="PRAGMA user_version";Assert.Equal((long)schema,cmd.ExecuteScalar());}
        Assert.Equal(old,YanJiDatabase.LegacyFactFingerprint(Path.Combine(rollback,"yanji.sqlite")));
        Assert.Throws<IOException>(()=>YanJiDatabase.PrepareUpgradeCopy(source,target));Assert.Throws<IOException>(()=>YanJiDatabase.RestoreOriginalToNewDirectory(result.BackupPath,rollback));
        M1TestDirectories.Report(target,"upgrade-result",result);
    }
    [Theory][InlineData("backup-validated")][InlineData("before-restore")][InlineData("after-restore")]
    public void FailureCannotChangeSourceOrAppearReady(string phase)
    {
        var source=LegacySchemaFixture.Create();var before=YanJiDatabase.LegacyFactFingerprint(Path.Combine(source,"yanji.sqlite"));var target=Target();
        Assert.Throws<IOException>(()=>YanJiDatabase.PrepareUpgradeCopy(source,target,s=>{if(s==phase)throw new IOException("synthetic failure "+phase);},null));
        Assert.Equal(before,YanJiDatabase.LegacyFactFingerprint(Path.Combine(source,"yanji.sqlite")));Assert.Throws<InvalidDataException>(()=>YanJiDatabase.CheckUpgradeReady(target));
        var retry=YanJiDatabase.PrepareUpgradeCopy(source,Target());Assert.Equal(retry.BeforeFingerprint,retry.AfterFingerprint);
    }
    [Fact] public void LowSpaceCorruptionAndExclusiveFileLockAreSafe()
    {
        var source=LegacySchemaFixture.Create();var file=Path.Combine(source,"yanji.sqlite");var before=YanJiDatabase.LegacyFactFingerprint(file);var target=Target();
        Assert.Throws<IOException>(()=>YanJiDatabase.PrepareUpgradeCopy(source,target,null,0));Assert.False(Directory.Exists(target));
        using(var locked=new FileStream(file,FileMode.Open,FileAccess.ReadWrite,FileShare.None))Assert.ThrowsAny<Exception>(()=>YanJiDatabase.PrepareUpgradeCopy(source,Target()));
        Assert.Equal(before,YanJiDatabase.LegacyFactFingerprint(file));var corrupt=M1TestDirectories.NewDirectory();File.WriteAllText(Path.Combine(corrupt,"yanji.sqlite"),"not SQLite");Assert.ThrowsAny<Exception>(()=>YanJiDatabase.PrepareUpgradeCopy(corrupt,Target()));
    }
    [Fact] public void ActiveTimerAndSourceReuseAreRejected()
    {
        var source=LegacySchemaFixture.Create(active:true);Assert.Throws<InvalidOperationException>(()=>YanJiDatabase.PrepareUpgradeCopy(source,Target()));
        Assert.ThrowsAny<Exception>(()=>YanJiDatabase.PrepareUpgradeCopy(source,source));
    }
}
