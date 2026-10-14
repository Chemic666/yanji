using System.Diagnostics;
using YanJi.Core;
using YanJi.Data;
namespace YanJi.Tests;
public sealed class KnowledgeScaleTests
{
    [Theory]
    [InlineData(1000)]
    [InlineData(10000)]
    public void FileBatchAndSubtreeQueryAtScale(int count)
    {
        var root = M1TestDirectories.NewDirectory(); using var db = new YanJiDatabase(root);
        var nodes = Enumerable.Range(0, count).Select(i => new KnowledgeNode("n" + i, "math", "知识节点" + i,
            ParentId: i % 100 == 0 ? null : "n" + (i / 100 * 100), SortOrder: i)).ToList();
        var clock = Stopwatch.StartNew(); db.SaveKnowledgeNodes(nodes); var batchMs = clock.Elapsed.TotalMilliseconds;
        clock.Restart(); var loaded = db.KnowledgeNodes(); var loadMs = clock.Elapsed.TotalMilliseconds;
        Assert.Equal(count, loaded.Count); KnowledgeTree.Validate(loaded);
        var date = new DateOnly(2026, 10, 14);
        db.SaveRecord(new("scale-session", "math", "n1", SessionSource.Manual, "", [new("scale-part", date, 98765, "Asia/Shanghai", PartKind.DurationOnly)]), "测试");
        Assert.Equal(98765, db.KnowledgeDuration(["n0", "n1"], date, date));
        Assert.Equal("ok", db.Integrity());
        M1TestDirectories.Report(root, "performance", new { Nodes = count, BatchMs = batchMs, LoadMs = loadMs, Database = db.DatabasePath, Synthetic = true });
    }
}
