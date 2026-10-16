using System.Text.Json;
using Microsoft.Data.Sqlite;
using YanJi.Core;
using YanJi.Data;

namespace YanJi.Tests;

public sealed class RCSimpleProgressTests : IDisposable
{
    private readonly string root = M1TestDirectories.NewDirectory();
    private readonly YanJiDatabase db;
    public RCSimpleProgressTests()
    {
        db = new(Path.Combine(root, "data"));
        db.SaveKnowledgeNodes([new("parent", "math", "父目录"), new("node", "math", "章节",
            Description: "模板说明", Mastery: Mastery.Weak)]);
    }
    private KnowledgeNode Node => db.KnowledgeNodes().Single(n => n.Id == "node");

    [Fact] public void IndependentSaveChangesStateButNeverMasteryOrStudyFacts()
    {
        Assert.Empty(db.ProgressNotes());
        var saved = db.SaveProgressNote("node", Node.Version, LearningState.Learned, "已学极限", "练习反例");
        Assert.Equal(1, saved.Version); Assert.NotNull(saved.UpdatedAt);
        Assert.Empty(db.Records()); Assert.Equal(100, Node.Progress);
        Assert.Equal(Mastery.Weak, Node.Mastery); Assert.Equal("模板说明", Node.Description);
        Assert.Equal(2, db.History("node").Count(r => r.Reason == "学习进度保存"));
        db.SaveRecord(new("record", "math", "node", SessionSource.Manual, "原学习备注",
            [new("part", new(2026, 10, 16), 12345, "Asia/Shanghai", PartKind.DurationOnly)]), "测试");
        var original = JsonSerializer.Serialize(db.Records());
        var version = Node.Version;
        var blank = db.SaveProgressNote("node", version, LearningState.Learning, "", "");
        Assert.Equal(2, blank.Version); Assert.Equal(99, Node.Progress);
        Assert.Throws<InvalidOperationException>(() => db.SaveProgressNote("node", version, LearningState.NotStarted, "过期", ""));
        Assert.Equal(original, JsonSerializer.Serialize(db.Records()));
        db.SaveProgressNote("node", Node.Version, LearningState.Deferred, "暂缓", "");
        Assert.Equal(99, Node.Progress);
        db.SaveProgressNote("node", Node.Version, LearningState.NotStarted, "", "下一步");
        Assert.Equal(0, Node.Progress); Assert.Equal(Mastery.Weak, Node.Mastery);
    }

    [Fact] public void RenameMoveAndArchiveRetainHistoricalNotesAndBackupRoundTrips()
    {
        var saved = db.SaveProgressNote("node", Node.Version, LearningState.Learning, "进度\n第二行", "下次\t练习");
        db.SaveKnowledgeNodes([Node with { Name = "自定义名称", ParentId = "parent" }]);
        Assert.Equal(saved, Assert.Single(db.ProgressNotes()));
        var parent = db.KnowledgeNodes().Single(n => n.Id == "parent");
        db.SaveKnowledgeNodes([parent with { Archived = true }]);
        Assert.Throws<InvalidOperationException>(() => db.SaveProgressNote("node", Node.Version, LearningState.Learned, "", ""));
        Assert.Equal(saved, Assert.Single(db.ProgressNotes()));
        var backup = Path.Combine(root, "progress.yanjibak"); db.Backup(backup); db.ValidateBackup(backup);
        using var restored = new YanJiDatabase(Path.Combine(root, "restored")); restored.RestoreBackup(backup);
        Assert.Equal(saved, Assert.Single(restored.ProgressNotes()));
        Assert.Equal("自定义名称", restored.KnowledgeNodes().Single(n => n.Id == "node").Name);
        Assert.Throws<InvalidOperationException>(() => restored.SaveProgressNote("node", Node.Version, LearningState.Learned, "", ""));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void AuditFailureRollsBackNodeNoteAndBothRevisions(bool editing)
    {
        if (editing) db.SaveProgressNote("node", Node.Version, LearningState.Learning, "原进度", "原下一步");
        var node = Node; var notes = db.ProgressNotes(); var history = db.History("node").Count;
        Execute("""
            CREATE TRIGGER reject_progress_audit BEFORE INSERT ON revisions
            WHEN NEW.kind='study-progress'
            BEGIN SELECT RAISE(ABORT,'isolated progress audit fault'); END;
            """);
        Assert.Throws<SqliteException>(() => db.SaveProgressNote("node", node.Version, LearningState.Learned, "不应保存", ""));
        Assert.Equal(node, Node); Assert.Equal(notes, db.ProgressNotes()); Assert.Equal(history, db.History("node").Count);
        Execute("DROP TRIGGER reject_progress_audit");
        Assert.Equal(editing ? 2 : 1, db.SaveProgressNote("node", node.Version, LearningState.Learned, "恢复保存", "").Version);
    }

    [Theory]
    [InlineData("missing")] [InlineData("version")] [InlineData("text")] [InlineData("control")] [InlineData("timestamp")] [InlineData("identity")]
    public void InvalidStoredPayloadRefusesBackup(string fault)
    {
        var item = new NodeProgressNote("node", "进度", "", 1, DateTimeOffset.UtcNow);
        item = fault switch
        {
            "missing" => item with { NodeId = "missing" }, "version" => item with { Version = 99 },
            "text" => item with { Next = new string('x', 2001) }, "control" => item with { Content = "bad\0" },
            "timestamp" => item with { UpdatedAt = null }, "identity" => item with { NodeId = "parent" }, _ => item
        };
        db.SaveSetting("study-progress:v1:" + (fault == "missing" ? "missing" : "node"), JsonSerializer.Serialize(item));
        Assert.Throws<InvalidDataException>(() => db.Backup(Path.Combine(root, "invalid.yanjibak")));
    }

    [Fact] public void InvalidInputAndArchivedSubjectNeverWrite()
    {
        foreach (var text in new[] { "bad\0", new string('x', 20001) })
            Assert.Throws<ArgumentException>(() => db.SaveProgressNote("node", Node.Version, LearningState.Learning, text, ""));
        Assert.Throws<ArgumentException>(() => db.SaveProgressNote("node", Node.Version, (LearningState)99, "", ""));
        Assert.Throws<ArgumentException>(() => db.SaveProgressNote("missing", 1, LearningState.Learning, "", ""));
        db.SaveSubject(db.Subjects().Single(s => s.Id == "math") with { Archived = true });
        Assert.Throws<InvalidOperationException>(() => db.SaveProgressNote("node", Node.Version, LearningState.Learning, "", ""));
        Assert.Empty(db.ProgressNotes());
        Assert.Throws<ArgumentException>(() => db.PackageBindings("missing"));
    }

    [Fact] public void PreviousSchemaFourWithoutNotesRemainsBackupCompatible()
    {
        var backup = Path.Combine(root, "previous.yanjibak"); db.Backup(backup);
        using var restored = new YanJiDatabase(Path.Combine(root, "previous")); restored.RestoreBackup(backup);
        Assert.Empty(restored.ProgressNotes()); Assert.Equal(2, restored.KnowledgeNodes().Count);
    }

    [Fact] public void CopyUpgradePreservesNotesAndPackageBindingsAreReadOnly()
    {
        var package = KnowledgePackageFormatTests.Example();
        var install = db.ApplyPackage(db.PreviewPackage(package, "legacy-default", KnowledgePackageLifecycleTests.Targets)).InstallId;
        var bindings = db.PackageBindings(install);
        Assert.Equal(2, bindings.Count); Assert.All(bindings, b => Assert.Contains(db.KnowledgeNodes(), n => n.Id == b.NodeId));
        var saved = db.SaveProgressNote("node", Node.Version, LearningState.Learning, "独立进度", "下一步");
        var copied = YanJiDatabase.PrepareUpgradeCopy(db.DirectoryPath, Path.Combine(root, "copy"));
        Assert.Equal(copied.BeforeFingerprint, copied.AfterFingerprint);
        using var upgraded = new YanJiDatabase(copied.TargetDirectory);
        Assert.Equal(saved, Assert.Single(upgraded.ProgressNotes()));
        Assert.Equal(bindings, upgraded.PackageBindings(install));
    }

    private void Execute(string sql)
    {
        using var c = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = db.DatabasePath, Pooling = false }.ToString());
        c.Open(); using var command = c.CreateCommand(); command.CommandText = sql; command.ExecuteNonQuery();
    }
    public void Dispose() => db.Dispose();
}
