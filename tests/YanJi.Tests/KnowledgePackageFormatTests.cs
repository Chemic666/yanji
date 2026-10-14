using System.Text;
using System.Text.Json;
using YanJi.Core;
namespace YanJi.Tests;

public sealed class KnowledgePackageFormatTests
{
    internal static KnowledgePackage Example(string version = "1.0.0") => new("org.yanji.test", version, 1, "隔离演示", "参考目录", "测试维护方", "cn-postgraduate", [2028],
        [new("source-test", "自编演示")], new("CC0-1.0", "原创测试数据"), [new("test", "测试科目")],
        [new("root", "test", null, KnowledgeNodeKind.Chapter, "根目录", "", 0), new("leaf", "test", "root", KnowledgeNodeKind.KnowledgePoint, "知识点", "", 0)]);
    [Fact] public void ValidPackageRoundTripsWithVerifiedContentHash()
    {
        var text = KnowledgePackageFile.Encode(Example()); var package = KnowledgePackageFile.Parse(Encoding.UTF8.GetBytes(text));
        Assert.Equal(2, package.Nodes.Count); Assert.Equal(64, package.ContentHash.Length);
        Assert.Equal(text, KnowledgePackageFile.Encode(package));
    }
    [Theory]
    [InlineData("duplicate-id")][InlineData("missing-parent")][InlineData("cycle")][InlineData("schema")]
    [InlineData("version")][InlineData("control-text")][InlineData("depth")][InlineData("source")][InlineData("duplicate-order")]
    public void InvalidStructureIsRejectedBeforeImport(string kind)
    {
        var p = Example();
        p = kind switch {
            "duplicate-id" => p with { Nodes = [p.Nodes[0], p.Nodes[0]] },
            "missing-parent" => p with { Nodes = [p.Nodes[1] with { ParentId = "missing" }] },
            "cycle" => p with { Nodes = [p.Nodes[0] with { ParentId = "leaf" }, p.Nodes[1]] },
            "schema" => p with { SchemaVersion = 2 }, "version" => p with { PackageVersion = "1.x.0" },
            "control-text" => p with { Name = "坏\u202e文本" },
            "source" => p with { Sources = [] },
            "duplicate-order" => p with { Nodes = [p.Nodes[0], p.Nodes[1] with { ParentId = null }] },
            "depth" => p with { Nodes = Enumerable.Range(0, 33).Select(i => new PackageNode("n"+i,"test",i==0?null:"n"+(i-1),KnowledgeNodeKind.Chapter,"节点","",0)).ToList() }, _ => p };
        Assert.Throws<InvalidDataException>(() => KnowledgePackageFile.Encode(p));
    }
    [Fact] public void HashTamperingUnknownFieldsDuplicatePropertiesWrongTypesAndInvalidUtf8AreRejected()
    {
        var valid = KnowledgePackageFile.Encode(Example());
        foreach (var text in new[] { valid.Replace("根目录", "改目录"), valid.Replace("\"SchemaVersion\": 1", "\"SchemaVersion\": \"1\""), "{\"PackageId\":\"a\",\"PackageId\":\"b\"}", valid.Replace("\"Name\":", "\"Progress\": 100, \"Name\":") })
            Assert.Throws<InvalidDataException>(() => KnowledgePackageFile.Parse(Encoding.UTF8.GetBytes(text)));
        Assert.Throws<InvalidDataException>(() => KnowledgePackageFile.Parse([0xff,0xfe]));
        Assert.Throws<InvalidDataException>(() => KnowledgePackageFile.Parse(new byte[KnowledgePackageFile.MaximumBytes+1]));
    }
}
