using YanJi.Core;
using YanJi.Data;
namespace YanJi.Tests;
public sealed class KnowledgePackageBuiltinTests
{
    [Fact] public void EveryShippedReferencePackageVerifiesAnd408HasFourUsableModules()
    {
        var packages=Directory.EnumerateFiles(Path.Combine(AppContext.BaseDirectory,"knowledge-packages"),"*.yanji-knowledge.json",SearchOption.AllDirectories).Select(f=>KnowledgePackageFile.Parse(File.ReadAllBytes(f))).ToList();
        Assert.Equal(7,packages.Count);Assert.All(packages,p=>{Assert.Contains("不是官方",p.Description);Assert.Equal("CC0-1.0",p.License.Id);});
        var p408=packages.Single(p=>p.PackageId.EndsWith(".408"));Assert.Equal(new[]{"数据结构","计算机组成原理","操作系统","计算机网络"},p408.Nodes.Where(n=>n.ParentId==null).Select(n=>n.Name));Assert.True(p408.Nodes.Count>=140);
        using var d=new YanJiDatabase(M1TestDirectories.NewDirectory());d.ApplyPackage(d.PreviewPackage(p408,"legacy-default",new Dictionary<string,string>{{"408","408"}}));Assert.Equal(145,d.KnowledgeNodes().Count);
    }
}
