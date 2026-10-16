using YanJi.Core;
using YanJi.Data;
using System.Text.Json;
namespace YanJi.Tests;

public sealed class SimpleReferenceTests
{
    private static string Root => Path.Combine(AppContext.BaseDirectory, "knowledge-packages");
    private static KnowledgePackage Package(string key, string version = "1.1.0") => KnowledgePackageFile.Parse(File.ReadAllBytes(Path.Combine(Root, "org.yanji.reference." + key, version, "package.yanji-knowledge.json")));

    [Fact] public void LatestIndexVerifiesEveryPackageAndPreservesHistoricalExternalIds()
    {
        var index = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "index.json")));
        Assert.Equal(7, index.RootElement.GetArrayLength());
        foreach (var item in index.RootElement.EnumerateArray())
        {
            var path = item.GetProperty("Path").GetString()!.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
            var p = KnowledgePackageFile.Parse(File.ReadAllBytes(Path.Combine(Root, path)));
            Assert.Equal(p.ContentHash, item.GetProperty("ContentHash").GetString());
            Assert.Equal(p.Nodes.Count, item.GetProperty("Nodes").GetInt32());
        }
        foreach (var key in new[] { "408", "math1", "math2", "english1", "english2" })
        {
            var old = Package(key, "1.0.0"); var latest = Package(key);
            Assert.Equal(old.PackageId, latest.PackageId);
            Assert.All(old.Nodes, n => Assert.Contains(latest.Nodes, x => x.Id == n.Id));
            Assert.Equal(old.Maintainer, latest.Maintainer);
            Assert.Equal(old.Sources.Select(s => (s.Id, s.Url)), latest.Sources.Select(s => (s.Id, s.Url)));
        }
        Assert.Equal(145, Package("408", "1.0.0").Nodes.Count);
        Assert.Contains(Package("408").Nodes, n => n.Name.Contains("KMP"));
    }

    [Fact] public void VariantScopesHaveAncestorClosureAndCorrectMathAndEnglishDifferences()
    {
        var scopes = ReferenceScopes.Load(Root);
        foreach (var key in new[] { "math1", "math2", "english1", "english2" })
        {
            var p = Package(key);
            Assert.Equal(p.Nodes.Count, scopes.Entries.Count(e => e.PackageId == p.PackageId));
            foreach (var variant in new[] { "common", "math1", "math2", "english1", "english2", "all" })
                foreach (var n in p.Nodes.Where(n => scopes.Includes(p.PackageId, n.Id, variant) && n.ParentId != null))
                    Assert.True(scopes.Includes(p.PackageId, n.ParentId!, variant), $"{key}/{n.Id}/{variant} has hidden parent");
            if (key.StartsWith("math"))
            {
                var m1 = p.Nodes.Where(n => scopes.Includes(p.PackageId, n.Id, "math1")).Select(n => n.Name).ToList();
                var m2 = p.Nodes.Where(n => scopes.Includes(p.PackageId, n.Id, "math2")).Select(n => n.Name).ToList();
                Assert.Contains(m1, n => n.Contains("概率")); Assert.Contains(m1, n => n.Contains("曲面积分")); Assert.Contains(m1, n => n.Contains("级数"));
                Assert.DoesNotContain(m2, n => n.Contains("概率") || n.Contains("曲面积分") || n.Contains("曲线积分") || n.Contains("三重积分") || n.Contains("空间解析几何") || n.Contains("级数"));
                Assert.Contains(m2, n => n.Contains("二重积分"));
            }
            else
            {
                string[] Names(string variant) => p.Nodes.Where(n => scopes.Includes(p.PackageId, n.Id, variant)).Select(n => n.Name).ToArray();
                Assert.Contains(Names("english1"), n => n.Contains("排序")); Assert.Contains(Names("english1"), n => n.Contains("选句"));
                Assert.Contains(Names("english1"), n => n.Contains("小标题")); Assert.Contains(Names("english2"), n => n.Contains("小标题"));
                Assert.Contains(Names("english2"), n => n.Contains("多项匹配"));
                Assert.DoesNotContain(Names("english2"), n => n.Contains("排序") || n.Contains("选句"));
                Assert.Contains(Names("english1"), n => n.Contains("图画")); Assert.DoesNotContain(Names("english2"), n => n.Contains("图画"));
                Assert.Contains(Names("english2"), n => n.Contains("图表"));
            }
            Assert.True(scopes.Includes(p.PackageId, "my-custom-point", "math2"));
        }
        Assert.True(scopes.Includes("custom", "anything", "english1"));
    }

    [Fact] public void ScopeReaderClosesAncestorsAndRejectsBrokenReferences()
    {
        var directory = M1TestDirectories.NewDirectory(); var file = Path.Combine(directory, "reference-scopes.json");
        File.WriteAllText(file, """
            {"SchemaVersion":1,"Entries":[
              {"PackageId":"test","ExternalNodeId":"root","ParentId":null,"Variants":["math2"]},
              {"PackageId":"test","ExternalNodeId":"leaf","ParentId":"root","Variants":["math1"]}]}
            """);
        var scopes = ReferenceScopes.Load(directory);
        Assert.True(scopes.Includes("test", "root", "math1")); Assert.False(scopes.Includes("test", "leaf", "math2"));
        Assert.True(scopes.Includes("test", "local", "math2")); Assert.True(scopes.Includes("test", "leaf", "all"));
        File.WriteAllText(file, File.ReadAllText(file).Replace("\"ParentId\":\"root\"", "\"ParentId\":\"missing\""));
        Assert.Throws<InvalidDataException>(() => ReferenceScopes.Load(directory));
    }
    [Fact] public void Real408UpgradeIsIdempotentAndPreservesLocalRenameStateAndStudyFacts()
    {
        var directory = M1TestDirectories.NewDirectory(); using var db = new YanJiDatabase(directory);
        var targets = new Dictionary<string, string> { ["408"] = "408" };
        var installed = db.ApplyPackage(db.PreviewPackage(Package("408", "1.0.0"), "legacy-default", targets));
        var before = db.KnowledgeNodes().ToDictionary(n => n.Id);
        var leaf = before.Values.First(n => n.Kind == KnowledgeNodeKind.KnowledgePoint);
        db.SaveKnowledgeNodes([leaf with { Name = "我的408复习标题", Progress = 37, State = LearningState.Learning, Description = "本地笔记" }]);
        db.SaveRecord(new("simple-ref-record", "408", leaf.Id, SessionSource.Manual, "参考包升级保留", [new("simple-ref-part", new DateOnly(2026, 10, 16), 45678, "Asia/Shanghai", PartKind.DurationOnly)]), "验证");
        var preview = db.PreviewPackage(Package("408"), "legacy-default", targets);
        Assert.Equal(Package("408").Nodes.Count - 145, preview.Added); Assert.Equal(0, preview.Archived);
        var update = db.ApplyPackage(preview); Assert.Equal(installed.InstallId, update.InstallId);
        Assert.Single(db.InstalledPackages()); Assert.Equal("1.1.0", db.InstalledPackages().Single().Version);
        var after = db.KnowledgeNodes(); Assert.All(before.Keys, id => Assert.Contains(after, n => n.Id == id));
        var local = after.Single(n => n.Id == leaf.Id); Assert.Equal("我的408复习标题", local.Name); Assert.Equal(37, local.Progress); Assert.Equal("本地笔记", local.Description);
        Assert.Equal(45678, db.Records().SelectMany(r => r.Parts).Sum(p => p.DurationMs));
        var count = db.PackageHistory(installed.InstallId).Count;
        Assert.True(db.ApplyPackage(db.PreviewPackage(Package("408"), "legacy-default", targets)).NoOp);
        Assert.Equal(count, db.PackageHistory(installed.InstallId).Count);
        using var reopened = new YanJiDatabase(directory); Assert.Equal(after, reopened.KnowledgeNodes());
        M1TestDirectories.Report(directory, "408-upgrade", new { preview.Added, OldNodes = before.Count, NewNodes = after.Count, installed.InstallId, DurationMs = 45678 });
    }

    [Theory] [InlineData("math2", "math", "math")] [InlineData("english2", "english", "english")]
    public void ScopeSwitchOnOneInstallationKeepsCommonLocalIdsAndNeverReinstalls(string key, string subjectKey, string target)
    {
        using var db = new YanJiDatabase(M1TestDirectories.NewDirectory()); var p = Package(key); var scopes = ReferenceScopes.Load(Root);
        var plan = db.PreviewPackage(p, "legacy-default", new Dictionary<string, string> { [subjectKey] = target }); db.ApplyPackage(plan);
        var before = db.KnowledgeNodes(); var bindings = plan.Bindings;
        var variants = key.StartsWith("math") ? new[] { "math1", "math2" } : new[] { "english1", "english2" };
        var common = bindings.Where(b => scopes.Includes(p.PackageId, b.SourceId, "common")).Select(b => b.NodeId).ToHashSet();
        Assert.NotEmpty(common);
        foreach (var variant in variants)
        {
            var visible = bindings.Where(b => scopes.Includes(p.PackageId, b.SourceId, variant)).Select(b => b.NodeId).ToHashSet();
            Assert.Subset(visible, common);
        }
        Assert.Single(db.InstalledPackages()); Assert.Single(db.PackageHistory(plan.InstallId)); Assert.Equal(before, db.KnowledgeNodes());
    }
}
