using YanJi.Core;
using YanJi.Data;

namespace YanJi.Tests;

public sealed class KnowledgeTreeTests
{
    private static KnowledgeNode Node(string id, string? parent = null) => new(id, "math", "中文" + id, parent);
    [Fact] public void CyclesAndCrossSubjectMovesAreRejected()
    {
        Assert.Throws<ArgumentException>(() => KnowledgeTree.Validate([Node("a", "b"), Node("b", "a")]));
        Assert.Throws<ArgumentException>(() => KnowledgeTree.Validate([Node("a"), Node("b", "a") with { SubjectId = "english" }]));
        KnowledgeTree.Validate([Node("a"), Node("b", "a"), Node("c", "b")]);
        Assert.Equal(new[] { "a", "b", "c" }, KnowledgeTree.Path([Node("a"), Node("b", "a"), Node("c", "b")], "c").Select(n => n.Id));
    }
    [Fact] public void FileTreePreservesLinksAndDeduplicatesAncestorSelections()
    {
        using var db = new YanJiDatabase(M1TestDirectories.NewDirectory());
        db.SaveKnowledgeNodes([Node("a"), Node("b", "a"), Node("c", "a")]);
        var day = new DateOnly(2026, 10, 14);
        db.SaveRecord(new("s1", "math", "a", SessionSource.Manual, "", [new("p1", day, 30001, "Asia/Shanghai", PartKind.DurationOnly)]), "测试");
        db.SaveRecord(new("s2", "math", "b", SessionSource.Manual, "", [new("p2", day, 20002, "Asia/Shanghai", PartKind.DurationOnly)]), "测试");
        var b = db.KnowledgeNodes().Single(n => n.Id == "b");
        db.SaveKnowledgeNodes([b with { Name = "新名称", ParentId = "c", Archived = true }]);
        Assert.Equal("b", db.Records().Single(r => r.Id == "s2").ChapterId);
        Assert.Equal(50003, db.KnowledgeDuration(["a", "c", "b"], day, day));
        Assert.Equal(20002, db.KnowledgeDuration(["b"], day, day));
        Assert.Throws<ArgumentException>(() => db.SaveKnowledgeNodes([db.KnowledgeNodes().Single(n => n.Id == "a") with { ParentId = "b" }]));
        Assert.Null(db.KnowledgeNodes().Single(n => n.Id == "a").ParentId);
        Assert.NotEmpty(db.History("b"));
    }
    [Fact] public void StateAndLegacyChapterEditingPreserveStructureAndMastery()
    {
        using var db = new YanJiDatabase(M1TestDirectories.NewDirectory());
        db.SaveKnowledgeNodes([Node("a"), Node("b", "a") with { Progress = 35, Mastery = Mastery.Weak, State = LearningState.Learning }]);
        var b = db.KnowledgeNodes().Single(n => n.Id == "b");
        db.SaveKnowledgeNodes([b with { State = LearningState.Deferred }]);
        db.SaveChapter(db.Chapters().Single(n => n.Id == "b") with { Name = "旧编辑器改名" });
        b = db.KnowledgeNodes().Single(n => n.Id == "b");
        Assert.Equal("a", b.ParentId); Assert.Equal(35, b.Progress);
        Assert.Equal(LearningState.Deferred, b.State); Assert.Equal(Mastery.Weak, b.Mastery);
        db.SetLearningState(b.Id, b.Version, LearningState.Learned);
        b = db.KnowledgeNodes().Single(n => n.Id == "b");
        Assert.Equal(100, b.Progress); Assert.Equal(Mastery.Weak, b.Mastery);
        Assert.Throws<InvalidOperationException>(() => db.SaveKnowledgeNodes([b with { Version = b.Version - 1 }]));
    }
    [Fact] public void ArchivedAncestorHidesNewLearningChoicesButKeepsHistoryAndBatchIsAtomic()
    {
        using var db = new YanJiDatabase(M1TestDirectories.NewDirectory());
        db.SaveKnowledgeNodes([Node("a") with { Archived = true }, Node("b", "a")]);
        Assert.Empty(db.Chapters("math")); Assert.Equal(2, db.Chapters("math", true).Count);
        Assert.Throws<ArgumentException>(() => db.SaveKnowledgeNodes([Node("ok"), Node("bad", "missing")]));
        Assert.DoesNotContain(db.KnowledgeNodes(), n => n.Id == "ok");
        Assert.Throws<ArgumentException>(() => db.SaveKnowledgeNodes([Node("contradiction") with { State = LearningState.Learned, Progress = 10 }]));
        var a = db.KnowledgeNodes().Single(n => n.Id == "a"); db.SaveKnowledgeNodes([a with { Archived = false }]);
        Assert.Equal(2, db.Chapters("math").Count);
    }
    [Fact] public void CorruptCycleCannotEscapeReadValidation()
    {
        using var db=new YanJiDatabase(M1TestDirectories.NewDirectory());db.SaveKnowledgeNodes([Node("a"),Node("b","a")]);
        using(var c=new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={db.DatabasePath};Pooling=False"))
        {c.Open();using var cmd=c.CreateCommand();cmd.CommandText="UPDATE chapters SET parent_id='b' WHERE id='a'";cmd.ExecuteNonQuery();}
        Assert.Throws<ArgumentException>(()=>db.KnowledgeNodes());
    }
}
