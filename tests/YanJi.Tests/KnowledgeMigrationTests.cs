using Microsoft.Data.Sqlite;
using YanJi.Core;
using YanJi.Data;

namespace YanJi.Tests;

public sealed class KnowledgeMigrationTests
{
    [Fact] public void FileMigrationPreservesCanonicalFactsAndLegacyBackupRestores()
    {
        var root = LegacySchemaFixture.Create();
        var before = YanJiDatabase.LegacyFactFingerprint(Path.Combine(root, "yanji.sqlite"));
        Assert.Throws<InvalidDataException>(() => new YanJiDatabase(root));
        var result = YanJiDatabase.UpgradeLegacyIsolated(root);
        M1TestDirectories.Report(root, "migration-success", result);
        Assert.Equal(before, result.BeforeFingerprint); Assert.Equal(before, result.AfterFingerprint);
        Assert.True(File.Exists(result.BackupPath)); Assert.True(File.Exists(result.RollbackPath));
        using var db = new YanJiDatabase(root);
        Assert.Equal(12345, db.Records().Single().DurationMs);
        Assert.Equal("old-chapter", db.Records().Single().ChapterId);
        Assert.Equal("old-chapter", db.WeakPoints().Single().ChapterId);
        Assert.Equal("old-task", db.Tasks().Single().Id);
        Assert.Equal(2,db.Records(includeDeleted:true).Count);
        Assert.Equal(2,db.Records(includeDeleted:true).Single(r=>r.Id=="old-deleted-timer").Parts.Count);
        Assert.Equal("old-task",db.Records(includeDeleted:true).Single(r=>r.Id=="old-deleted-timer").Focus!.TaskId);
        Assert.Contains(db.History("old-session"),r=>r.Id=="old-revision");
        var n = db.KnowledgeNodes().Single(n => n.Id == "old-chapter");
        Assert.Equal(35, n.Progress); Assert.Equal(Mastery.Weak, n.Mastery); Assert.Equal(LearningState.Learning, n.State);
        Assert.True(db.KnowledgeNodes().Single(n => n.Id == "old-archived").Archived);
        db.ValidateBackup(result.BackupPath);
        db.SaveKnowledgeNodes([n with { Name = "更新后的名字" }]);
        db.RestoreBackup(result.BackupPath);
        Assert.Equal(before, YanJiDatabase.LegacyFactFingerprint(db.DatabasePath));
        Assert.Equal("旧中文章节", db.KnowledgeNodes().Single(n => n.Id == "old-chapter").Name);
        var format2 = Path.Combine(root, "schema2.yanjibak"); db.Backup(format2); db.ValidateBackup(format2);
        n = db.KnowledgeNodes().Single(n => n.Id == "old-chapter");
        Assert.Throws<ArgumentException>(() => db.SaveKnowledgeNodes([n with { ParentId = "old-archived" }]));
        db.RestoreBackup(format2); Assert.Equal(before, YanJiDatabase.LegacyFactFingerprint(db.DatabasePath));
    }
    [Theory]
    [InlineData("after-ddl")]
    [InlineData("before-replace")]
    [InlineData("after-replace")]
    public void MigrationFailureLeavesUsableSchema1(string failAt)
    {
        var root = LegacySchemaFixture.Create();
        var before = YanJiDatabase.LegacyFactFingerprint(Path.Combine(root, "yanji.sqlite"));
        Assert.Throws<IOException>(() => YanJiDatabase.UpgradeLegacyIsolated(root, step => { if (step == failAt) throw new IOException("Injected " + step); }));
        Assert.Equal(before, YanJiDatabase.LegacyFactFingerprint(Path.Combine(root, "yanji.sqlite")));
        M1TestDirectories.Report(root, "migration-failure", new { Stage=failAt, Fingerprint=before, LogicalSourcePreserved=true });
        using var c = new SqliteConnection($"Data Source={Path.Combine(root, "yanji.sqlite")};Pooling=False"); c.Open();
        using var cmd = c.CreateCommand(); cmd.CommandText = "PRAGMA user_version"; Assert.Equal(1L, cmd.ExecuteScalar());
        cmd.CommandText = "INSERT INTO chapters VALUES('usable','math','仍可写',0,0,0)"; Assert.Equal(1, cmd.ExecuteNonQuery());
    }
    [Fact] public void ActiveLegacyCheckpointAndUnmarkedDirectoryCannotUpgrade()
    {
        var root=LegacySchemaFixture.Create(active:true);var path=Path.Combine(root,"yanji.sqlite");var before=YanJiDatabase.LegacyFactFingerprint(path);
        Assert.Throws<InvalidOperationException>(()=>YanJiDatabase.UpgradeLegacyIsolated(root));Assert.Equal(before,YanJiDatabase.LegacyFactFingerprint(path));
        var plain=M1TestDirectories.NewDirectory();Assert.Throws<InvalidOperationException>(()=>YanJiDatabase.UpgradeLegacyIsolated(plain));Assert.False(File.Exists(Path.Combine(plain,"yanji.sqlite")));
    }
    [Fact] public void ValidLongLegacyChapterNameIsNotTruncatedOrRejected()
    {
        var root=LegacySchemaFixture.Create();var name=new string('中',300);
        using(var c=new SqliteConnection($"Data Source={Path.Combine(root,"yanji.sqlite")};Pooling=False"))
        {c.Open();using var cmd=c.CreateCommand();cmd.CommandText="UPDATE chapters SET name=$name WHERE id='old-chapter'";cmd.Parameters.AddWithValue("$name",name);cmd.ExecuteNonQuery();}
        var result=YanJiDatabase.UpgradeLegacyIsolated(root);Assert.Equal(result.BeforeFingerprint,result.AfterFingerprint);
        using var db=new YanJiDatabase(root);Assert.Equal(name,db.KnowledgeNodes().Single(n=>n.Id=="old-chapter").Name);
    }
}
